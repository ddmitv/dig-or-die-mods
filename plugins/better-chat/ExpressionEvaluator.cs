
// 1+2 => 3
// 2+2*3 => 8
// unit(#3).hp => 200
// unit("hound").hp => 10
// unit("hound").tier => 1
// unit(#5).speed => 4
// pi*e => 8.53973422
// sin(pi) => 0
// sin(pi*3/2) => -1
// {1,2}*2 => {2,4}
// cellat(500,200).flags => 3
// cellat({600,700}).hp => 50
// cellat(500,500).water => 0.5

using ModUtils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public sealed class ExpressionEvaluator {
    [Serializable]
    public sealed class EvaluationException : Exception {
        public EvaluationException(string message) : base(message) { }
    }
    [Serializable]
    public sealed class ParsingException : Exception {
        public ParsingException(string message) : base(message) { }
    }
    [Serializable]
    public sealed class TokenizingException : Exception {
        public TokenizingException(string message) : base(message) { }
    }

    private enum TokenType {
        Value,
        Identifier,
        Plus,
        Minus,
        Multiply,
        Divide,
        Modulo,
        Exponent,
        LeftParenthesis,
        RightParenthesis,
        Comma,
        VectorBegin,
        VectorEnd,
        FieldAccess,
        AbsoluteValue,
        PlayerPosition,
        EndOfExpression,
    }
    private interface IValue {
        string TypeName();
        string String();
    }
    private static string IValueString(IValue iValue) {
        return iValue.String();
    }
    private interface FieldAccessibleValue {
        IValue ReadField(string name);
        string[] FieldNames();
    }

    private sealed class FloatValue(double value) : IValue {
        public double Value = value;

        string IValue.TypeName() => "float";
        string IValue.String() => Value.ToString(CultureInfo.InvariantCulture);
    }
    private sealed class IntValue(long value) : IValue {
        public long Value = value;

        string IValue.TypeName() => "integer";
        string IValue.String() => Value.ToString(CultureInfo.InvariantCulture);
    }
    private sealed class StringValue(string value) : IValue {
        public string Value = value;
        string IValue.TypeName() => "string";
        string IValue.String() => Value;
    }
    private sealed class IntVectorValue(int2 value) : IValue, FieldAccessibleValue {
        public int2 Value = value;

        public IntVectorValue(int x, int y) : this(new int2(x, y)) { }

        string IValue.TypeName() => "int2";
        string IValue.String() => $"{{{Value.x}, {Value.y}}}";

        string[] FieldAccessibleValue.FieldNames() { return ["x", "y"]; }
        IValue FieldAccessibleValue.ReadField(string name) {
            return name switch {
                "x" => new IntValue(Value.x),
                "y" => new IntValue(Value.y),
                _ => null,
            };
        }
    }
    private sealed class IdValue(ushort value) : IValue {
        public ushort Value = value;
        string IValue.TypeName() => "ID";
        string IValue.String() => "#" + Value.ToString(CultureInfo.InvariantCulture);
    }
    private sealed class UnitValue(CUnit.CDesc unitDesc) : IValue, FieldAccessibleValue {
        public StringValue CodeName = new(unitDesc.m_codeName);
        public StringValue Name = new(unitDesc.GetName());
        public IdValue Id = new(unitDesc.m_id);
        public IntValue Tier = new(unitDesc.m_tier);
        public FloatValue SpeedMax = new(unitDesc.m_speedMax);
        public FloatValue HpMax = new(unitDesc.m_hpMax);
        public IntValue Armor = new(unitDesc.m_armor);
        public FloatValue RegenSpeed = new(unitDesc.m_regenSpeed);
        string IValue.TypeName() => "unit";
        string IValue.String() =>
            $"unit{{codename={IValueString(CodeName)},name={IValueString(Name)},id={IValueString(Id)}," +
            $"tier={IValueString(Tier)},speed={IValueString(SpeedMax)},hp={IValueString(HpMax)}," +
            $"armor={IValueString(Armor)},regen={IValueString(RegenSpeed)}}}";

        string[] FieldAccessibleValue.FieldNames() {
            return ["codename", "name", "id", "tier", "speed", "hp", "armor", "regen"];
        }
        IValue FieldAccessibleValue.ReadField(string name) {
            return name switch {
                "codename" => CodeName,
                "name" => Name,
                "id" => Id,
                "tier" => Tier,
                "speed" => SpeedMax,
                "hp" => HpMax,
                "armor" => Armor,
                "regen" => RegenSpeed,
                _ => null,
            };
        }
    }
    private sealed class CellValue(in CCell cell) : IValue, FieldAccessibleValue {
        public IntValue Flags = new(cell.m_flags);
        public IntValue Hp = new(cell.m_contentHP);
        public FloatValue Water = new(cell.m_water);
        public IdValue Id = new(cell.m_contentId);
        public IntVectorValue Force = new(cell.m_forceX, cell.m_forceY);
        public StringValue CodeName = cell.m_contentId != 0 ? new(cell.GetContent().m_codeName) : new("-");
        public StringValue Name = cell.m_contentId != 0 ? new(cell.GetContent().m_name) : new("-");

        string IValue.TypeName() => "cell";
        string IValue.String() =>
            $"cell{{flags={IValueString(Flags)},hp={IValueString(Hp)},water={IValueString(Water)}," +
            $"id={IValueString(Id)},force={IValueString(Force)},codename={IValueString(CodeName)},name={IValueString(Name)}}}";

        string[] FieldAccessibleValue.FieldNames() {
            return ["flags", "hp", "water", "id", "force", "codename", "name"];
        }
        IValue FieldAccessibleValue.ReadField(string name) {
            return name switch {
                "flags" => Flags,
                "hp" => Hp,
                "water" => Water,
                "id" => Id,
                "force" => Force,
                "codename" => CodeName,
                "name" => Name,
                _ => null,
            };
        }
    }
    private sealed class ItemValue(CItem item) : IValue, FieldAccessibleValue {
        public IdValue Id = new(item.m_id);
        public StringValue Codename = new(item.m_codeName);
        public StringValue Name = new(item.m_name);
        public StringValue Desc = new(item.m_desc);
        public StringValue TextureIcon = new($"{item.m_tileIcon.m_tileIndex} name={item.m_tileIcon.m_textureName}");

        string IValue.TypeName() => "item";
        string IValue.String() =>
            $"item{{id={IValueString(Id)},codename={IValueString(Codename)},name={IValueString(Name)}}}";

        string[] FieldAccessibleValue.FieldNames() {
            return ["id", "codename", "name", "desc", "textureIcon"];
        }
        IValue FieldAccessibleValue.ReadField(string name) {
            return name switch {
                "id" => Id,
                "codename" => Codename,
                "name" => Name,
                "desc" => Desc,
                "textureIcon" => TextureIcon,
                _ => null,
            };
        }
    }
    private sealed class IdentifierValue(string identifier) : IValue {
        public string Identifier = identifier;
        string IValue.TypeName() => throw new NotSupportedException();
        string IValue.String() => throw new NotSupportedException();
    }

    private struct Token {
        public TokenType Type;
        public IValue Value = null;

        public Token(TokenType type) { Type = type; }
        public Token(TokenType type, IValue value) {
            if (value is null) { throw new ArgumentNullException(nameof(value)); }
            Type = type; Value = value;
        }
    }

    private sealed class Tokenizer(string exprStr) {
        private string _str = exprStr;
        private int _position = 0;

        private char CurrentChar => _str[_position];

        public Token NextToken() {
            _position += _str.Skip(_position).TakeWhile(ch => char.IsWhiteSpace(ch)).Count();
            if (_position >= _str.Length) {
                return new Token(TokenType.EndOfExpression);
            }

            if (CurrentChar == '\"') {
                Advance(expected: '\"');
                return new Token(TokenType.Value, ParseString());
            }
            if (char.IsDigit(CurrentChar)) {
                return new Token(TokenType.Value, ParseNumber());
            }
            if (char.IsLetter(CurrentChar)) {
                return new Token(TokenType.Identifier, ParseIdentifier());
            }
            if (CurrentChar == '#') {
                return new Token(TokenType.Value, ParseId());
            }
            char tokenCh = GetCurrentAndAdvance();
            switch (tokenCh) {
            case '+': return new Token(TokenType.Plus);
            case '-': return new Token(TokenType.Minus);
            case '*': return new Token(TokenType.Multiply);
            case '/': return new Token(TokenType.Divide);
            case '%': return new Token(TokenType.Modulo);
            case '^': return new Token(TokenType.Exponent);
            case '(': return new Token(TokenType.LeftParenthesis);
            case ')': return new Token(TokenType.RightParenthesis);
            case ',': return new Token(TokenType.Comma);
            case '{': return new Token(TokenType.VectorBegin);
            case '}': return new Token(TokenType.VectorEnd);
            case '.': return new Token(TokenType.FieldAccess);
            case '|': return new Token(TokenType.AbsoluteValue);
            case '~': return new Token(TokenType.PlayerPosition);
            default: throw new TokenizingException($"Unknown character: '{tokenCh}'");
            }
        }
        private IValue ParseNumber() {
            string numStr = SubstringAndAdvanceUntil(
                ch => Uri.IsHexDigit(ch) || ch is '.' or 'e' or 'E' or 'b' or 'x' or 'X' or '_'
            );
            numStr = numStr.Replace("_", "");

            if (numStr.StartsWith("0x")) {
                if (long.TryParse(numStr.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long intValue)) {
                    return new IntValue(intValue);
                }
            } else if (numStr.StartsWith("0b")) {
                if (Utils.TryParseBinary(numStr.Substring(2), out long intValue)) {
                    return new IntValue(intValue);
                }
            } else {
                if (long.TryParse(numStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out long intValue)) {
                    return new IntValue(intValue);
                }
            }
            if (double.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double floatValue)) {
                return new FloatValue(floatValue);
            }
            throw new TokenizingException($"Invalid number: '{numStr}'");
        }
        private IdentifierValue ParseIdentifier() {
            string identifierStr = SubstringAndAdvanceUntil(ch => char.IsLetterOrDigit(ch));

            return new IdentifierValue(identifierStr);
        }
        private StringValue ParseString() {
            string str = SubstringAndAdvanceUntil(ch => ch != '\"');
            Advance(expected: '\"');
            return new StringValue(str);
        }
        private IdValue ParseId() {
            Advance('#');
            string numStr = SubstringAndAdvanceUntil(ch => char.IsDigit(ch));
            if (!ushort.TryParse(numStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort resultId)) {
                throw new TokenizingException($"Invalid id: '{numStr}'");
            }
            return new IdValue(resultId);
        }

        private string SubstringAndAdvanceUntil(Func<char, bool> predicate) {
            int start = _position;
            while (_position < _str.Length && predicate(_str[_position])) {
                _position += 1;
            }
            return _str.Substring(start, _position - start);
        }
        private char GetCurrentAndAdvance() {
            char ch = _str[_position];
            _position += 1;
            return ch;
        }
        private void Advance(char expected) {
            if (_position >= _str.Length) {
                throw new TokenizingException($"Expected character <{expected}>, but the expression is ended");
            }
            if (_str[_position] != expected) {
                throw new TokenizingException($"Expected character <{expected}>, got <{_str[_position]}>");
            }
            _position += 1;
        }
    }

    private class EvaluationEnv {
        public Dictionary<string, IValue> Variables = [];
        public Dictionary<string, Func<IValue[], IValue>> Functions = [];
    }

    private interface IExpression {
        IValue Evaluate(EvaluationEnv env);
    }
    private sealed class BinaryExpression(IExpression left, IExpression right, TokenType tokenType) : IExpression {
        private readonly IExpression leftExpr = left;
        private readonly IExpression rightExpr = right;
        private readonly TokenType tokenType = tokenType;

        IValue IExpression.Evaluate(EvaluationEnv env) {
            IValue l = leftExpr.Evaluate(env);
            IValue r = rightExpr.Evaluate(env);
            return tokenType switch {
                TokenType.Plus => EvaluateAdd(l, r),
                TokenType.Minus => EvaluateSub(l, r),
                TokenType.Multiply => EvaluateMul(l, r),
                TokenType.Divide => EvaluateDiv(l, r),
                TokenType.Modulo => EvaluateMod(l, r),
                TokenType.Exponent => EvaluatePow(l, r),
                _ => throw new NotSupportedException("Expected only operator tokens")
            };
        }
        private static IValue EvaluateAdd(IValue l, IValue r) {
            switch (l) {
            case FloatValue lFloat when r is FloatValue rFloat:
                return new FloatValue(lFloat.Value + rFloat.Value);
            case FloatValue lFloat when r is IntValue rInt:
                return new FloatValue(lFloat.Value + rInt.Value);
            case IntValue lInt when r is FloatValue rFloat:
                return new FloatValue(lInt.Value + rFloat.Value);
            case IntValue lInt when r is IntValue rInt:
                return new IntValue(lInt.Value + rInt.Value);
            case IntVectorValue lInt when r is IntVectorValue rInt:
                return new IntVectorValue(lInt.Value + rInt.Value);
            }
            throw new EvaluationException($"Addition operation is not supported for types: '{l.TypeName()}' and '{r.TypeName()}'");
        }
        private static IValue EvaluateSub(IValue l, IValue r) {
            switch (l) {
            case FloatValue lFloat when r is FloatValue rFloat:
                return new FloatValue(lFloat.Value - rFloat.Value);
            case FloatValue lFloat when r is IntValue rInt:
                return new FloatValue(lFloat.Value - rInt.Value);
            case IntValue lInt when r is FloatValue rFloat:
                return new FloatValue(lInt.Value - rFloat.Value);
            case IntValue lInt when r is IntValue rInt:
                return new IntValue(lInt.Value - rInt.Value);
            case IntVectorValue lInt when r is IntVectorValue rInt:
                return new IntVectorValue(lInt.Value - rInt.Value);
            }
            throw new EvaluationException($"Subtraction operation is not supported for types: '{l.TypeName()}' and '{r.TypeName()}'");
        }
        private static IValue EvaluateMul(IValue l, IValue r) {
            switch (l) {
            case FloatValue lFloat when r is FloatValue rFloat:
                return new FloatValue(lFloat.Value * rFloat.Value);
            case FloatValue lFloat when r is IntValue rInt:
                return new FloatValue(lFloat.Value * rInt.Value);
            case IntValue lInt when r is FloatValue rFloat:
                return new FloatValue(lInt.Value * rFloat.Value);
            case IntValue lInt when r is IntValue rInt:
                return new IntValue(lInt.Value * rInt.Value);
            case IntValue lInt when r is IntVectorValue rIntVec:
                return new IntVectorValue((int)lInt.Value * rIntVec.Value);
            case IntVectorValue lIntVec when r is IntValue rInt:
                return new IntVectorValue(lIntVec.Value * (int)rInt.Value);
            }
            throw new EvaluationException($"Multiplication is not supported for types: '{l.TypeName()}' and '{r.TypeName()}'");
        }
        private static IValue EvaluateDiv(IValue l, IValue r) {
            switch (l) {
            case FloatValue lFloat when r is FloatValue rFloat:
                return new FloatValue(lFloat.Value / rFloat.Value);
            case FloatValue lFloat when r is IntValue rInt:
                return new FloatValue(lFloat.Value / rInt.Value);
            case IntValue lInt when r is FloatValue rFloat:
                return new FloatValue(lInt.Value / rFloat.Value);
            case IntValue lInt when r is IntValue rInt:
                return new FloatValue((double)lInt.Value / rInt.Value);
            }
            throw new EvaluationException($"Division operation is not supported for types: '{l.TypeName()}' and '{r.TypeName()}'");
        }
        private static IValue EvaluateMod(IValue l, IValue r) {
            switch (l) {
            case FloatValue lFloat when r is FloatValue rFloat:
                return new FloatValue(lFloat.Value % rFloat.Value);
            case FloatValue lFloat when r is IntValue rInt:
                return new FloatValue(lFloat.Value % rInt.Value);
            case IntValue lInt when r is FloatValue rFloat:
                return new FloatValue(lInt.Value % rFloat.Value);
            case IntValue lInt when r is IntValue rInt:
                return new IntValue(lInt.Value % rInt.Value);
            case IntVectorValue lIntVec when r is IntValue rInt:
                return new IntVectorValue(new int2(lIntVec.Value.x % (int)rInt.Value, lIntVec.Value.y % (int)rInt.Value));
            }
            throw new EvaluationException($"Modulo operation is not supported for types: '{l.TypeName()}' and '{r.TypeName()}'");
        }
        private static long LongPow(long x, ulong pow) {
            long ret = 1;
            while (pow != 0) {
                if ((pow & 1) == 1)
                    ret *= x;
                x *= x;
                pow >>= 1;
            }
            return ret;
        }
        private static IValue EvaluatePow(IValue l, IValue r) {
            switch (l) {
            case FloatValue lFloat when r is FloatValue rFloat:
                return new FloatValue(Math.Pow(lFloat.Value, rFloat.Value));
            case FloatValue lFloat when r is IntValue rInt:
                return new FloatValue(Math.Pow(lFloat.Value, rInt.Value));
            case IntValue lInt when r is FloatValue rFloat:
                return new FloatValue(Math.Pow(lInt.Value, rFloat.Value));
            case IntValue lInt when r is IntValue rInt:
                if (rInt.Value >= 0) {
                    return new IntValue(LongPow(lInt.Value, (ulong)rInt.Value));
                }
                return new FloatValue(Math.Pow(lInt.Value, rInt.Value));
            }
            throw new EvaluationException($"Exponentiation is not supported for types: '{l.TypeName()}' and '{r.TypeName()}'");
        }
    }
    private sealed class UnaryExpression(IExpression expr, TokenType tokenType) : IExpression {
        private readonly IExpression expr = expr;
        private readonly TokenType tokenType = tokenType;

        IValue IExpression.Evaluate(EvaluationEnv env) {
            IValue value = expr.Evaluate(env);
            return tokenType switch {
                TokenType.Minus when value is IntValue @int => new IntValue(-@int.Value),
                TokenType.Minus when value is FloatValue @float => new FloatValue(-@float.Value),
                TokenType.Minus when value is IntVectorValue intVec => new IntVectorValue(-intVec.Value),
                _ => value,
            };
        }
    }
    private sealed class LiteralExpression(IValue value) : IExpression {
        private readonly IValue value = value;

        IValue IExpression.Evaluate(EvaluationEnv env) {
            return value;
        }
    }
    private sealed class VariableExpression(string variableName) : IExpression {
        private readonly string variableName = variableName;

        IValue IExpression.Evaluate(EvaluationEnv env) {
            if (!env.Variables.TryGetValue(variableName, out IValue varValue)) {
                string closestVarName = Utils.ClosestStringMatch(variableName, env.Variables.Keys);
                throw new EvaluationException($"Undefined variable with name '{variableName}'. Did you mean '{closestVarName}'?");
            }
            return varValue;
        }
    }
    private sealed class FunctionCallExpression(string functionName, List<IExpression> arguments) : IExpression {
        private readonly string functionName = functionName;
        private readonly List<IExpression> arguments = arguments;

        IValue IExpression.Evaluate(EvaluationEnv env) {
            if (!env.Functions.TryGetValue(functionName, out var function)) {
                string closestFuncName = Utils.ClosestStringMatch(functionName, env.Functions.Keys);
                throw new EvaluationException($"Undefined function with name '{functionName}'. Did you mean '{closestFuncName}'?");
            }
            var evalArguments = arguments.Select(expr => expr.Evaluate(env)).ToArray();
            try {
                return function.Invoke(evalArguments);
            } catch (EvaluationException evaluationException) {
                throw new EvaluationException($"Function '{functionName}': {evaluationException.Message}");
            }
        }
    }
    private sealed class VectorExpression(IExpression xExpr, IExpression yExpr) : IExpression {
        private readonly IExpression xExpr = xExpr;
        private readonly IExpression yExpr = yExpr;

        IValue IExpression.Evaluate(EvaluationEnv env) {
            IValue xVal = xExpr.Evaluate(env);
            IValue yVal = yExpr.Evaluate(env);
            if (xVal is IntValue xInt && yVal is IntValue yInt) {
                return new IntVectorValue((int)xInt.Value, (int)yInt.Value);
            }
            throw new EvaluationException($"The vector can only be created with integer values, got '{xVal.TypeName()}' as first and '{yVal.TypeName()}' as second");
        }
    }
    private sealed class AbsoluteValueExpression(IExpression expr) : IExpression {
        private readonly IExpression expr = expr;

        IValue IExpression.Evaluate(EvaluationEnv env) {
            IValue val = expr.Evaluate(env);
            return val switch {
                IntValue intVal => new IntValue(Math.Abs(intVal.Value)),
                FloatValue floatVal => new FloatValue(Math.Abs(floatVal.Value)),
                IntVectorValue intVectorVal => new FloatValue(Utils.Hypot(intVectorVal.Value)),
                StringValue strVal => new IntValue(strVal.Value.Length),
                _ => throw new EvaluationException($"Absoulte value operation is not supported for '{val.TypeName()}'"),
            };
        }
    }
    private sealed class FieldAccessExpression(IExpression expr, string fieldName) : IExpression {
        private readonly IExpression expr = expr;
        private readonly string fieldName = fieldName;

        IValue IExpression.Evaluate(EvaluationEnv env) {
            IValue generalVal = expr.Evaluate(env);
            if (generalVal is not FieldAccessibleValue val) {
                throw new EvaluationException($"Type '{generalVal.TypeName()}' is not field accessible");
            }
            IValue fieldVal = val.ReadField(fieldName);
            if (fieldVal is null) {
                string closestFieldName = Utils.ClosestStringMatch(fieldName, val.FieldNames());
                throw new EvaluationException($"Type '{generalVal.TypeName()}' doesn't have field with name '{fieldName}'. Did you mean '{closestFieldName}'?");
            }
            return fieldVal;
        }
    }

    private EvaluationEnv evaluationEnv = new();
    private Tokenizer tokenizer = null;
    private Token currentToken;

    private Token ConsumeToken() {
        Token tmp = currentToken;
        currentToken = tokenizer.NextToken();
        return tmp;
    }
    private bool CheckAndConsumeToken(TokenType expected) {
        if (currentToken.Type != expected) {
            return false;
        }
        currentToken = tokenizer.NextToken();
        return true;
    }

    private IExpression ParseExpression() {
        IExpression left = ParseTerm();
        while (currentToken.Type is TokenType.Plus or TokenType.Minus) {
            Token opToken = ConsumeToken();
            IExpression right = ParseTerm();
            left = new BinaryExpression(left, right, opToken.Type);
        }
        return left;
    }
    private IExpression ParseTerm() {
        IExpression left = ParseUnary();
        while (currentToken.Type is TokenType.Multiply or TokenType.Divide or TokenType.Modulo) {
            Token opToken = ConsumeToken();
            IExpression right = ParseUnary();
            left = new BinaryExpression(left, right, opToken.Type);
        }
        return left;
    }
    private IExpression ParseUnary() {
        if (currentToken.Type is TokenType.Minus or TokenType.Plus) {
            Token opToken = ConsumeToken();
            IExpression expr = ParseUnary();
            return new UnaryExpression(expr, opToken.Type);
        }
        return ParseExponent();
    }
    private IExpression ParseExponent() {
        IExpression left = FieldAccess();
        if (currentToken.Type == TokenType.Exponent) {
            ConsumeToken();
            IExpression right = ParseUnary();
            return new BinaryExpression(left, right, TokenType.Exponent);
        }
        return left;
    }
    private IExpression FieldAccess() {
        IExpression left = ParsePrimary();
        while (currentToken.Type is TokenType.FieldAccess) {
            ConsumeToken();
            Token fieldName = ConsumeToken();
            if (fieldName.Type != TokenType.Identifier) {
                throw new ParsingException($"Expected identifier after field access operator, got '{fieldName.Type}'");
            }
            left = new FieldAccessExpression(left, ((IdentifierValue)fieldName.Value).Identifier);
        }
        return left;
    }
    private IExpression ParsePrimary() {
        Token primaryToken = ConsumeToken();
        switch (primaryToken.Type) {
        case TokenType.Value: {
            return new LiteralExpression(primaryToken.Value);
        }
        case TokenType.Identifier when currentToken.Type == TokenType.LeftParenthesis: {
            ConsumeToken();
            string functionName = ((IdentifierValue)primaryToken.Value).Identifier;
            List<IExpression> args = [];
            if (CheckAndConsumeToken(TokenType.RightParenthesis)) {
                return new FunctionCallExpression(functionName, args);
            }
            args.Add(ParseExpression()); // parse first argument
            while (currentToken.Type == TokenType.Comma) {
                ConsumeToken();
                args.Add(ParseExpression());
            }
            if (!CheckAndConsumeToken(TokenType.RightParenthesis)) {
                throw new ParsingException($"Expected right parenthesis at the end of function call '{functionName}', got '{currentToken.Type}'");
            }
            return new FunctionCallExpression(functionName, args);
        }
        case TokenType.Identifier: {
            return new VariableExpression(((IdentifierValue)primaryToken.Value).Identifier);
        }
        case TokenType.LeftParenthesis: {
            IExpression expr = ParseExpression();
            if (!CheckAndConsumeToken(TokenType.RightParenthesis)) {
                throw new ParsingException($"Expected right parenthesis at the end of parentheses expression, got '{currentToken.Type}'");
            }
            return expr;
        }
        case TokenType.VectorBegin: {
            IExpression firstExpr = ParseExpression();
            if (!CheckAndConsumeToken(TokenType.Comma)) {
                throw new ParsingException($"Expected comma after first vector argument, got '{currentToken.Type}'");
            }
            IExpression secondExpr = ParseExpression();
            if (!CheckAndConsumeToken(TokenType.VectorEnd)) {
                throw new ParsingException($"Expected vector ending parenthesis '}}' after second vector argument, got '{currentToken.Type}'");
            }
            return new VectorExpression(firstExpr, secondExpr);
        }
        case TokenType.AbsoluteValue: {
            IExpression expr = ParseExpression();
            if (!CheckAndConsumeToken(TokenType.AbsoluteValue)) {
                throw new ParsingException($"Expected absolute value ending, got '{currentToken}'");
            }
            return new AbsoluteValueExpression(expr);
        }
        case TokenType.PlayerPosition: {
            CPlayer player = SNetwork.GetMyPlayer();
            return new LiteralExpression(new IntVectorValue(player.m_unitPlayer.PosCell));
        }
        case TokenType.Exponent: {
            return new LiteralExpression(new IntVectorValue(SGame.MouseWorldPosInt));
        }
        default: {
            throw new ParsingException($"Unexpected token: '{primaryToken.Type}'");
        }
        }
    }

    public string Evaluate(string expression) {
        tokenizer = new Tokenizer(expression);
        currentToken = tokenizer.NextToken();

        if (currentToken.Type == TokenType.EndOfExpression) {
            return "";
        }
        IExpression expr = ParseExpression();
        if (currentToken.Type != TokenType.EndOfExpression) {
            throw new ParsingException($"Expected end of the expression token, got '{currentToken.Type}'");
        }
        IValue result = expr.Evaluate(evaluationEnv);
        return result.String();
    }
    public void AddBuiltinVariables() {
        var vars = evaluationEnv.Variables;
        vars.Add("pi", new FloatValue(Math.PI));
        vars.Add("tau", new FloatValue(Math.PI * 2));
        vars.Add("e", new FloatValue(Math.E));
        vars.Add("phi", new FloatValue(1.61803398874989484820));
        vars.Add("invpi", new FloatValue(0.31830988618379067153));
        vars.Add("invsqrtpi", new FloatValue(0.564189583547756286948));
        vars.Add("egamma", new FloatValue(0.577215664901532860606512090082));
        vars.Add("inf", new FloatValue(double.PositiveInfinity));
        vars.Add("nan", new FloatValue(double.NaN));
    }
    public void AddBuiltinFunctions() {
        AddMathFunction("sin", Math.Sin);
        AddMathFunction("cos", Math.Cos);
        AddMathFunction("tan", Math.Tan);
        AddMathFunction("sqrt", Math.Sqrt);
        AddMathFunction("ln", Math.Log);
        AddMathFunction("exp", Math.Exp);
        AddMathFunction("acos", Math.Acos);
        AddMathFunction("asin", Math.Asin);
        AddMathFunction("atan", Math.Atan);
        AddMathFunction("ceil", Math.Ceiling);
        AddMathFunction("floor", Math.Floor);
        AddMathFunction("log10", Math.Log10);
        AddMathFunction("round", Math.Round);
        AddMathFunction("truncate", Math.Truncate);

        evaluationEnv.Functions.Add("typename", args => {
            if (args.Length != 1) { throw new EvaluationException("Expected 1 argument"); }
            return new StringValue(args[0].TypeName());
        });

        // https://stackoverflow.com/a/9331125
        evaluationEnv.Functions.Add("comb", args => {
            if (args.Length != 2) { throw new EvaluationException($"Expected 2 arguments, got {args.Length}"); }
            if (args[0] is not IntValue nValue || args[1] is not IntValue kValue) {
                throw new EvaluationException($"First and second arguments must be of type 'int'");
            }
            var n = nValue.Value;
            var k = kValue.Value;
            if (n < 0 || k < 0) { throw new EvaluationException("Negative argument"); }
            if (k > n) { return new IntValue(0); }
            if (k * 2 > n) { k = n - k; }
            if (k == 0) { return new IntValue(1); }

            ulong result = (ulong)n;
            for (uint i = 2; i <= k; ++i) {
                result *= ((ulong)n - i + 1);
                result /= i;
            }
            return new IntValue((long)result);
        });
        evaluationEnv.Functions.Add("factorial", args => {
            if (args.Length != 1) { throw new EvaluationException("Expected 1 argument"); }
            if (args[0] is not IntValue n) { throw new EvaluationException("First argument should be an integer"); }
            if (n.Value < 0) { throw new EvaluationException("Negative first argument"); }
            long result = 1;
            for (uint i = 2; i <= n.Value; ++i) {
                result *= i;
            }
            return new IntValue(result);
        });
        evaluationEnv.Functions.Add("max", args => {
            if (args.Length == 0) { throw new EvaluationException($"Invalid number of arguments. Expected greater than 0 arguments"); }
            IValue current = args[0];
            if (current is not (FloatValue or IntValue)) {
                throw new EvaluationException("Only 'int' and 'float' types are supported");
            }
            for (int i = 0; i < args.Length; ++i) {
                IValue val = args[i];

                if (val is not (FloatValue or IntValue)) {
                    throw new EvaluationException("Only 'int' and 'float' types are supported");
                }
                bool updateCurrent = current switch {
                    FloatValue resFlt when val is FloatValue fltVal => fltVal.Value > resFlt.Value,
                    FloatValue resFlt when val is IntValue intVal => intVal.Value > resFlt.Value,
                    IntValue resInt when val is FloatValue fltVal => fltVal.Value > resInt.Value,
                    IntValue resInt when val is IntValue intVal => intVal.Value > resInt.Value,
                    _ => false
                };
                if (updateCurrent) { current = val; }
            }
            return current;
        });
        evaluationEnv.Functions.Add("min", args => {
            if (args.Length == 0) {
                throw new EvaluationException($"Invalid number of arguments. Expected greater than 0 arguments");
            }
            IValue current = args[0];
            if (current is not (FloatValue or IntValue)) {
                throw new EvaluationException("Only 'int' and 'float' types are supported");
            }
            for (int i = 0; i < args.Length; ++i) {
                IValue val = args[i];

                if (val is not (FloatValue or IntValue)) {
                    throw new EvaluationException("Only 'int' and 'float' types are supported");
                }
                bool updateCurrent = current switch {
                    FloatValue resFlt when val is FloatValue fltVal => fltVal.Value < resFlt.Value,
                    FloatValue resFlt when val is IntValue intVal => intVal.Value < resFlt.Value,
                    IntValue resInt when val is FloatValue fltVal => fltVal.Value < resInt.Value,
                    IntValue resInt when val is IntValue intVal => intVal.Value < resInt.Value,
                    _ => false
                };
                if (updateCurrent) { current = val; }
            }
            return current;
        });

        evaluationEnv.Functions.Add("cellat", args => {
            if (args.Length == 2) {
                if (args[0] is IntValue cellX && args[1] is IntValue cellY) {
                    if (!Utils.IsInWorld((int)cellX.Value, (int)cellY.Value)) {
                        throw new EvaluationException($"Cell position is not in the world grid");
                    }
                    return new CellValue(in SWorld.Grid[cellX.Value, cellY.Value]);
                }
                throw new EvaluationException($"Expected 'int' and 'int', got '{args[0].TypeName()}' and '{args[1].TypeName()}'");
            }
            if (args.Length == 1) {
                if (args[0] is IntVectorValue cellPos) {
                    if (!Utils.IsInWorld(cellPos.Value)) {
                        throw new EvaluationException($"Cell position is not in the world grid");
                    }
                    return new CellValue(in SWorld.Grid[cellPos.Value.x, cellPos.Value.y]);
                }
                throw new EvaluationException($"Expected 'int2', got '{args[0].TypeName()}'");
            }
            throw new EvaluationException($"Invalid number of arguments. Expected 1 or 2 arguments");
        });
        evaluationEnv.Functions.Add("unit", args => {
            if (args.Length != 1) { throw new EvaluationException($"Expected 1 argument, got {args.Length}"); }

            if (args[0] is IdValue id) {
                if (id.Value == 0 || id.Value >= GUnits.UDescs.Count) {
                    throw new EvaluationException($"Id is out of range. Maximum id is {GUnits.UDescs.Count - 1}");
                }
                return new UnitValue(GUnits.UDescs[id.Value]);
            }
            if (args[0] is StringValue codeName) {
                var unit = GUnits.UDescs.Skip(1).FirstOrDefault(x => x.m_codeName == codeName.Value);
                if (unit is null) {
                    var closestCodeName = Utils.ClosestStringMatch(codeName.Value, GUnits.UDescs.Skip(1).Select(x => x.m_codeName));
                    throw new EvaluationException($"Unknown unit code name. Did you mean '{closestCodeName}'?");
                }
                return new UnitValue(unit);
            }
            throw new EvaluationException($"Invalid argument type. Expected either 'id' or 'string'");
        });
        evaluationEnv.Functions.Add("item", args => {
            if (args.Length != 1) { throw new EvaluationException($"Expected 1 argument, got {args.Length}"); }

            if (args[0] is IdValue id) {
                if (id.Value == 0 || id.Value >= GItems.Items.Count) {
                    throw new EvaluationException($"Id is out of range. Maximum id is {GItems.Items.Count - 1}");
                }
                return new ItemValue(GItems.Items[id.Value]);
            }
            if (args[0] is StringValue codeName) {
                var item = GItems.Items.Skip(1).FirstOrDefault(x => x.m_codeName == codeName.Value);
                if (item is null) {
                    var closestCodeName = Utils.ClosestStringMatch(codeName.Value, GItems.Items.Skip(1).Select(x => x.m_codeName));
                    throw new EvaluationException($"Unknown item code name. Did you mean '{closestCodeName}'?");
                }
                return new ItemValue(item);
            }
            throw new EvaluationException($"Invalid argument type. Expected either 'id' or 'string'");
        });
    }
    private void AddMathFunction(string name, Func<double, double> func) {
        evaluationEnv.Functions.Add(name, (IValue[] args) => {
            if (args.Length != 1) { throw new EvaluationException($"Expected 1 argument, got {args.Length}"); }
            return new FloatValue(args[0] switch {
                FloatValue floatValue => func(floatValue.Value),
                IntValue intValue => func((double)intValue.Value),
                _ => throw new EvaluationException($"Expected number in first argument")
            });
        });
    }
}



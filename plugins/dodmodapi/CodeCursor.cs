
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;

namespace DODModAPI;

// replacement for HarmonyLib.CodeMatcher with improved API (because CodeMatcher API sucks and i hate it)
public sealed class CodeCursor {
    private readonly ILGenerator _generator;
    private readonly List<CodeInstruction> _codes;
    private int _pos;

    public class MatchFailedException(string message) : Exception(message);

    public CodeCursor(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        _generator = generator ?? throw new ArgumentNullException(nameof(generator));
        _codes = instructions.Select(c => new CodeInstruction(c)).ToList();
        _pos = 0;
    }

    public CodeCursor FindNext(params CodeInstruction[] pattern) {
        MatchImpl(pattern, 1, false);
        return this;
    }
    public CodeCursor FindNextEnd(params CodeInstruction[] pattern) {
        MatchImpl(pattern, 1, true);
        return this;
    }
    public CodeCursor FindPrevious(params CodeInstruction[] pattern) {
        MatchImpl(pattern, -1, false);
        return this;
    }
    public CodeCursor FindPreviousEnd(params CodeInstruction[] pattern) {
        MatchImpl(pattern, -1, true);
        return this;
    }

    public CodeCursor FindNext(out uint patternLength, params CodeInstruction[] pattern) {
        MatchImpl(pattern, 1, false);
        patternLength = (uint)pattern.Length;
        return this;
    }
    public CodeCursor FindNextEnd(out uint patternLength, params CodeInstruction[] pattern) {
        MatchImpl(pattern, 1, true);
        patternLength = (uint)pattern.Length;
        return this;
    }
    public CodeCursor FindPrevious(out uint patternLength, params CodeInstruction[] pattern) {
        MatchImpl(pattern, -1, false);
        patternLength = (uint)pattern.Length;
        return this;
    }
    public CodeCursor FindPreviousEnd(out uint patternLength, params CodeInstruction[] pattern) {
        MatchImpl(pattern, -1, true);
        patternLength = (uint)pattern.Length;
        return this;
    }

    public CodeCursor RepeatNTimes(uint count, Action<CodeCursor> action) {
        if (action is null) { throw new ArgumentNullException(nameof(action)); }

        for (uint i = 0; i < count; i++) {
            action(this);
        }
        return this;
    }

    public CodeCursor Advance(int offset) {
        _pos += offset;
        if (_pos < 0 || _pos > _codes.Count) {
            throw new ArgumentOutOfRangeException(nameof(offset), offset, $"Offset {offset} moves cursor to position {_pos}, which is outside the valid range [0..{_codes.Count})");
        }
        return this;
    }
    public CodeCursor MoveToStart() {
        _pos = 0;
        return this;
    }
    public CodeCursor MoveToEnd() {
        if (_codes.Count > 0) {
            _pos = _codes.Count - 1;
        }
        return this;
    }

    public CodeCursor Insert(params CodeInstruction[] instructions) {
        if (instructions is null || instructions.Length == 0) {
            return this;
        }
        _codes.InsertRange(_pos, instructions);
        _pos += instructions.Length;
        return this;
    }

    public CodeCursor Insert(in OpCode opcode, object? operand = null) {
        _codes.Insert(_pos, new(opcode, operand));
        _pos += 1;
        return this;
    }


    public CodeCursor InsertBranch(OpCode opcode, int offset) {
        if (opcode.OperandType != OperandType.InlineBrTarget && opcode.OperandType != OperandType.ShortInlineBrTarget) {
            throw new ArgumentException($"OpCode '{opcode}' is not a branch instruction", nameof(opcode));
        }
        int targetPos = _pos + offset;
        if (targetPos < 0 || targetPos >= _codes.Count) {
            throw new ArgumentOutOfRangeException(nameof(offset), offset, $"Branch target position {targetPos} is out of bounds [0..{_codes.Count})");
        }
        var label = _generator.DefineLabel();
        _codes[targetPos].labels.Add(label);
        _codes.Insert(_pos, new CodeInstruction(opcode, label));
        _pos++;
        return this;
    }
    public CodeCursor Remove() {
        if (_pos >= _codes.Count) {
            throw new InvalidOperationException($"Cursor position {_pos} is out of bounds [0..{_codes.Count})");
        }
        _codes.RemoveAt(_pos);
        return this;
    }
    public CodeCursor Remove(uint count) {
        if (_pos + count > _codes.Count) {
            throw new ArgumentOutOfRangeException(nameof(count), count, $"Cannot remove {count} instructions starting at position {_pos}; only {_codes.Count - _pos} remain");
        }
        _codes.RemoveRange(_pos, (int)count);
        return this;
    }
    public CodeCursor RemoveAndCollectLabels(uint count, out List<Label> labels) {
        if (_pos + count > _codes.Count) {
            throw new ArgumentOutOfRangeException(nameof(count), count, $"Cannot remove {count} instructions starting at position {_pos}; only {_codes.Count - _pos} remain");
        }
        labels = new List<Label>();
        for (int i = _pos; i < _pos + (int)count; i++) {
            labels.AddRange(_codes[i].labels);
        }
        _codes.RemoveRange(_pos, (int)count);
        return this;
    }
    public CodeCursor RemovePreservingLabels(uint count) {
        if (_pos + count > _codes.Count) {
            throw new ArgumentOutOfRangeException(nameof(count), count, $"Cannot remove {count} instructions starting at position {_pos}; only {_codes.Count - _pos} remain");
        }
        var labels = new List<Label>();
        var blocks = new List<ExceptionBlock>();

        for (int i = _pos; i < _pos + (int)count; i++) {
            labels.AddRange(_codes[i].labels);
            blocks.AddRange(_codes[i].blocks);
        }
        _codes.RemoveRange(_pos, (int)count);
        if (_pos >= _codes.Count) {
            throw new InvalidOperationException($"Nowhere to put preserved labels from removing because position {_pos} it is out of bounds [0..{_codes.Count})");
        }

        _codes[_pos].labels.AddRange(labels);
        _codes[_pos].blocks.AddRange(blocks);
        return this;
    }
    public CodeCursor Inject(params CodeInstruction[] instructions) {
        if (_pos >= _codes.Count) {
            Insert(instructions);
            return this;
        }
        List<Label> labels = _codes[_pos].labels;

        CollectionHelpers.Partition(_codes[_pos].blocks,
            b => b.blockType == ExceptionBlockType.BeginExceptionBlock ||
                 b.blockType == ExceptionBlockType.BeginFinallyBlock,
            out var openingBlocks, out var closingBlocks);

        _codes[_pos].labels = [];
        _codes[_pos].blocks = closingBlocks;

        _codes.InsertRange(_pos, instructions);

        _codes[_pos].labels = labels;
        _codes[_pos].blocks.AddRange(openingBlocks);

        _pos += instructions.Length;
        return this;
    }

    public CodeCursor InjectWithLabel(Func<Label, CodeInstruction[]> instructionsFactory) {
        if (instructionsFactory is null) { throw new ArgumentNullException(nameof(instructionsFactory)); }

        Label skipLabel = _generator.DefineLabel();
        CodeInstruction[] instructions = instructionsFactory(skipLabel);
        if (instructions is null || instructions.Length == 0) { return this; }

        Inject(instructions);

        if (_pos >= _codes.Count) {
            throw new InvalidOperationException($"There is no instruction after position {_pos}, unable to place skip label");
        }
        _codes[_pos].labels.Add(skipLabel);

        return this;
    }

    public CodeCursor CreateLabel(int offset, out Label label) {
        int targetPos = _pos + offset;
        if (targetPos < 0 || targetPos >= _codes.Count) {
            throw new ArgumentOutOfRangeException(nameof(offset), offset, $"Offset {offset} leads to position {targetPos}, which is out of bounds [0..{_codes.Count})");
        }
        label = _generator.DefineLabel();
        _codes[targetPos].labels.Add(label);
        return this;
    }

    public CodeCursor DeclareLabel(out Label label) {
        label = _generator.DefineLabel();
        return this;
    }

    public CodeCursor AssertInstruction(int offset, CodeInstruction instr) {
        int targetPos = _pos + offset;
        if (targetPos < 0 || targetPos >= _codes.Count) {
            throw new ArgumentOutOfRangeException(nameof(offset), offset, $"Offset {offset} leads to position {targetPos}, which is out of bounds [0..{_codes.Count})");
        }
        CodeInstruction actual = _codes[targetPos];
        if (!InstructionMatches(instr, actual)) {
            throw new MatchFailedException(
                $"{nameof(AssertInstruction)} failed at offset {offset} (position {targetPos}).\n" +
                $"Expected: {instr}\nActual: {actual}");
        }
        return this;
    }

    public CodeCursor GetOperand<T>(int offset, out T value) {
        int targetPos = _pos + offset;
        if (targetPos < 0 || targetPos >= _codes.Count) {
            throw new ArgumentOutOfRangeException(nameof(offset), offset, $"Offset {offset} leads to position {targetPos}, which is out of bounds [0..{_codes.Count})");
        }
        if (_codes[targetPos].operand is not T operand) {
            throw new InvalidCastException($"Cannot cast operand type \"{_codes[targetPos].operand.GetType().FullName}\" to \"{typeof(T).FullName}\" at offset {offset} (position {targetPos})");
        }
        value = operand;
        return this;
    }

    public CodeCursor ReplaceOpcode(in OpCode newOpcode, out OpCode oldOpcode) {
        if (_pos < 0 || _pos >= _codes.Count) {
            throw new InvalidOperationException($"Current position {_pos} is out of bounds [0..{_codes.Count})");
        }
        oldOpcode = _codes[_pos].opcode;
        _codes[_pos].opcode = newOpcode;

        _pos += 1;
        return this;
    }

    public CodeCursor Replace(CodeInstruction newInstr, out CodeInstruction oldInstr) {
        if (_pos < 0 || _pos >= _codes.Count) {
            throw new InvalidOperationException($"Current position {_pos} is out of bounds [0..{_codes.Count})");
        }
        oldInstr = _codes[_pos];

        AppendList(ref newInstr.labels, oldInstr.labels);
        AppendList(ref newInstr.blocks, oldInstr.blocks);

        _codes[_pos] = newInstr;
        _pos += 1;
        return this;
    }
    public CodeCursor Replace(in OpCode opcode, object? operand = null) {
        if (_pos < 0 || _pos >= _codes.Count) {
            throw new InvalidOperationException($"Position {_pos} which is out of bounds [0..{_codes.Count})");
        }
        _codes[_pos].opcode = opcode;
        _codes[_pos].operand = operand;
        _pos += 1;
        return this;
    }

    public CodeCursor When(bool condition, Action<CodeCursor> fn) {
        if (condition) {
            if (fn is null) { throw new ArgumentNullException(nameof(fn)); }
            fn(this);
        }
        return this;
    }

    public CodeCursor GetPos(out int pos) {
        pos = _pos;
        return this;
    }
    public CodeCursor SetPos(int pos) {
        if (pos < 0 || pos > _codes.Count) {
            throw new ArgumentOutOfRangeException(nameof(pos), pos, $"Position {pos} is outside the valid range [0..{_codes.Count}]");
        }
        _pos = pos;
        return this;
    }

    public CodeCursor AddLabels(List<Label> labels) {
        if (_pos < 0 || _pos >= _codes.Count) {
            throw new InvalidOperationException($"Position {_pos} is outside the valid range [0..{_codes.Count})");
        }
        _codes[_pos].labels.AddRange(labels);
        return this;
    }

    public List<CodeInstruction> Finish() {
        return _codes;
    }

    private void MatchImpl(CodeInstruction[] pattern, int step, bool toEnd) {
        if (pattern is null) { throw new ArgumentNullException(nameof(pattern)); }
        if (pattern.Length == 0) { throw new ArgumentException("pattern cannot be empty", nameof(pattern)); }

        int startPos = _pos;
        while (_pos >= 0 && _pos < _codes.Count) {
            if (MatchSequence(pattern)) {
                if (toEnd) { _pos += pattern.Length; }
                return;
            }
            _pos += step;
        }
        throw new MatchFailedException(
            $"Failed to match pattern sequence searching {(step > 0 ? "forward" : "backward")} " +
            $"from position {startPos} with pattern:\n    ! {Misc.StringJoin(pattern, delimiter: "\n    ! ")}");
    }
    private bool MatchSequence(CodeInstruction[] pattern) {
        if (_pos > _codes.Count - pattern.Length) {
            return false;
        }
        for (int i = 0; i < pattern.Length; ++i) {
            if (!InstructionMatches(pattern[i], _codes[_pos + i])) {
                return false;
            }
        }
        return true;
    }
    private static bool InstructionMatches(CodeInstruction pattern, CodeInstruction actual) {
        if (pattern.opcode != actual.opcode) {
            return false;
        }
        if (pattern.operand is null) {
            return true;
        }
        if (TryCastToLocalIndex(pattern.operand, out int localIndex) && actual.operand is LocalBuilder lb) {
            return localIndex == lb.LocalIndex;
        }
        return object.Equals(pattern.operand, actual.operand);
    }

    private static bool TryCastToLocalIndex(object operand, out int result) {
        switch (operand) {
        case int i: result = i; return true;
        case byte b: result = b; return true;
        case sbyte sb: result = sb; return true;
        case short s: result = s; return true;
        case ushort us: result = us; return true;
        case uint ui when ui <= int.MaxValue: result = (int)ui; return true;
        default: result = 0; return false;
        }
    }

    private static void AppendList<T>(ref List<T> a, List<T> b) {
        if (a.Count == 0) {
            a = b;
        } else {
            a.AddRange(b);
        }
    }
}

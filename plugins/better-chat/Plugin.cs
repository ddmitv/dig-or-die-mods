using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using ModUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using ModUtils.Extensions;

[Serializable]
public class InvalidCommandArgument : Exception {
    public int? argIndex = null;

    public InvalidCommandArgument(string message) : base(message) {}

    public InvalidCommandArgument(string message, int argIndex) : base(message) {
        this.argIndex = argIndex;
    }
}

public static class CustomCommandsPatch {
    public delegate void ExecCommandFn(string[] args, CPlayer playerSender);
    public delegate List<string> TabCommandFn(int argIndex);

    public static readonly Dictionary<string, ExecCommandFn> customCommands = new();

    public static readonly Dictionary<string, TabCommandFn> customTabCommands = new();

    private static string[] ParseArgs(string text) {
        return text.Split([' ', '\t', '\r', '\n', '\v', '\f'], StringSplitOptions.RemoveEmptyEntries);
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(SNetworkCommands), nameof(SNetworkCommands.ProcessCommand))]
    private static IEnumerable<CodeInstruction> SNetworkCommands_ProcessCommand(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        static bool ExecCustomCommand(string text, CPlayer playerSender) {
            string[] commandAndArgs = ParseArgs(text);
            string command = commandAndArgs[0];
            if (!customCommands.TryGetValue(command, out ExecCommandFn fn)) {
                return false;
            }
            string[] args = commandAndArgs.Skip(1).ToArray();

            try {
                fn(args, playerSender);
            } catch (InvalidCommandArgument exception) {
                if (!playerSender.IsMe()) { return true; }

                string errorMessage = exception.argIndex switch {
                    null => $"{command}: {exception.Message}",
                    int idx => $"{command}: {exception.Message} (argument {idx})"
                }; 
                SSingletonScreen<SScreenHudChat>.Inst.AddChatMessage_Local(null, errorMessage, false);
            }
            return true;
        }
        
        var codeMatcher = new CodeMatcher(instructions, generator);
        codeMatcher.Start()
            .MatchForward(useEnd: false,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldsfld, typeof(System.String).StaticField("Empty")),
                new(OpCodes.Ldarg_2),
                new(OpCodes.Ldc_I4_1),
                new(OpCodes.Call, typeof(SNetworkCommands).Method("DrawHelp_IfSenderIsMe")),
                new(OpCodes.Ret))
            .CreateLabelAtOffset(5, out Label exitLabel)
            .InjectAndAdvance(OpCodes.Ldarg_1)
            .Insert(
                new(OpCodes.Ldarg_2),
                Transpilers.EmitDelegate(ExecCustomCommand),
                new(OpCodes.Brtrue, exitLabel));

        return codeMatcher.Instructions();
    }
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SNetworkCommands), nameof(SNetworkCommands.TabCommand))]
    private static void SNetworkCommands_TabCommand(SNetworkCommands __instance, string input, ref string __result) {
        string[] commandAndArgs = ParseArgs(input);
        string command = commandAndArgs[0];

        if (!customTabCommands.TryGetValue(command, out TabCommandFn tabCommand)) {
            return;
        }
        string arg = commandAndArgs.Length <= 1 ? "" : commandAndArgs[1];
        List<string> argList = tabCommand(commandAndArgs.Length);
        __result = __instance.TabOnList(__result, command, arg, argList);
    }
}

public static class RepeatLastCommandPatch {
    [HarmonyPatch(typeof(SScreenHudChat), nameof(SScreenHudChat.OnUpdate))]
    [HarmonyPostfix]
    private static void SScreenHudChat_OnUpdate() {
        if (ExtraCommands.configRepeatLastCommand.Value.IsDown()) {
            var networkCommands = SSingleton<SNetworkCommands>.Inst;

            if (networkCommands.m_historyCommands.Count == 0) { return; }

            string prevCommand = networkCommands.m_historyCommands[networkCommands.m_historyIndex - 1];
            networkCommands.ProcessCommand(prevCommand, SNetwork.GetMyPlayer());
        }
    }
}

public static class ChatExpressionEvaluationPatch {
    private static int FindEndParenthesis(string str, int start) {
        int count = 0;
        for (int i = start; i < str.Length; ++i) {
            if (str[i] == '(') {
                count += 1;
            }
            if (str[i] == ')') {
                count -= 1;
                if (count < 0) { return i; }
            }
        }
        return -1;
    }
    [HarmonyPatch(typeof(SScreenHudChat), nameof(SScreenHudChat.OnUpdate))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> SScreenHudChat_OnUpdate(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        static bool PatchChatMessage(SScreenHudChat screenHudChat) {
            ref string text = ref screenHudChat.m_inputChat.m_text;
            string prefix = ExtraCommands.configChatExpressionEvaluatorPrefix.Value;

            for (int i = 0; i < text.Length; ++i) {
                if (string.Compare(text, i, prefix, 0, prefix.Length) != 0) {
                    continue;
                }
                if (i - 1 >= 0 && text[i - 1] == '\\') {
                    text = text.Remove(i - 1, 1);
                    i -= 1;
                    continue;
                }
                bool displayExpression = i + prefix.Length < text.Length && text[i + prefix.Length] == '=';

                int leftParenthesisPos = i + prefix.Length + (displayExpression ? 1 : 0);
                if (leftParenthesisPos >= text.Length || text[leftParenthesisPos] != '(') {
                    continue;
                }

                int start = i;
                int exprStart = i + prefix.Length + 1 + (displayExpression ? 1 : 0);
                i = FindEndParenthesis(text, exprStart) + 1;
                if (i <= 0) {
                    Utils.AddChatMessageLocal("Expression evaluator has an unclosed parenthesis");
                    return false;
                }

                string exprString = text.Substring(exprStart, i - exprStart - 1); // also remove ending ')'
                string exprResult;
                try {
                    exprResult = ExtraCommands.expressionEvaluator.Evaluate(exprString);
                } catch (ExpressionEvaluator.EvaluationException evaluationException) {
                    Utils.AddChatMessageLocal($"Evaluation error: {evaluationException.Message}");
                    return false;
                } catch (ExpressionEvaluator.ParsingException parseingException) {
                    Utils.AddChatMessageLocal($"Parsing error: {parseingException.Message}");
                    return false;
                } catch (ExpressionEvaluator.TokenizingException tokenizingException) {
                    Utils.AddChatMessageLocal($"Tokenizing error: {tokenizingException.Message}");
                    return false;
                }
                text = text.Remove(start, i - start).Insert(start, exprResult);
                i += -(i - start) + exprResult.Length; // adjust index for evaluated expression character difference
                if (displayExpression) {
                    text = text.Insert(start, exprString + "=");
                    i += exprString.Length + 1;
                }
            }
            return true;
        }
        var codeMatcher = new CodeMatcher(instructions, generator);

        codeMatcher
            .MatchForward(useEnd: false,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(SScreenHudChat).Field("m_inputChat")),
                new(OpCodes.Ldfld, typeof(CGuiInput).Field("m_text")),
                new(OpCodes.Ldstr, "/"),
                new(OpCodes.Callvirt, typeof(string).Method("StartsWith", [typeof(string)])),
                new(OpCodes.Brfalse))
            .ThrowIfInvalid("(1)")
            .GetOperandAtOffset(-1, out Label skipMessage)
            .Insert(
                new(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate(PatchChatMessage),
                new(OpCodes.Brfalse, skipMessage));
        return codeMatcher.Instructions();
    }
}

public static class FullChatHistoryPatch {
    [HarmonyPatch(typeof(SNetworkCommands), nameof(SNetworkCommands.ProcessCommand))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> SNetworkCommands_ProcessCommand(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var codeMatcher = new CodeMatcher(instructions, generator);

        // Disable updating command history so that it doesn't interfere with new approach
        codeMatcher
            .MatchForward(useEnd: false,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(SNetworkCommands).Field("m_historyCommands")),
                new(OpCodes.Ldarg_1),
                new(OpCodes.Callvirt, typeof(List<string>).Method("Add")),
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(SNetworkCommands).Field("m_historyCommands")),
                new(OpCodes.Callvirt, typeof(List<string>).Method("get_Count")),
                new(OpCodes.Stfld, typeof(SNetworkCommands).Field("m_historyIndex")))
            .ThrowIfInvalid("(1)")
            .RemoveInstructions(9);
        return codeMatcher.Instructions();
    }
    [HarmonyPatch(typeof(SScreenHudChat), nameof(SScreenHudChat.OnUpdate))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> SScreenHudChat_OnUpdate(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        static void AddToChatHistory(SScreenHudChat self) {
            var networkCommands = SSingleton<SNetworkCommands>.Inst;
            networkCommands.m_historyCommands.Add(self.m_inputChat.m_text);
            networkCommands.m_historyIndex = networkCommands.m_historyCommands.Count;
        }

        var codeMatcher = new CodeMatcher(instructions, generator);

        codeMatcher.
            MatchForward(useEnd: true,
                new(OpCodes.Ldc_I4_S, (sbyte)13),
                new(OpCodes.Call, typeof(SInputs).Method("GetKeyDown")),
                new(OpCodes.Brfalse),
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(SScreenHudChat).Field("m_inputChat")),
                new(OpCodes.Ldfld, typeof(CGuiInput).Field("m_text")),
                new(OpCodes.Ldsfld, typeof(string).StaticField("Empty")),
                new(OpCodes.Call, typeof(string).Method("op_Inequality")),
                new(OpCodes.Brfalse))
            .ThrowIfInvalid("(1)")
            .Advance(1)
            .Insert(
                new(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate(AddToChatHistory));

        return codeMatcher.Instructions();
    }
}

// expression evaluator test string:
// $(((3 + 4 * 2 - (10 % 3) + (5 - 3 - 1) * (-3^2) / 0.5) ^ (2 % 3)) % 100 + (12 / 3 * 4 - 12 / (3 * 4)) * (3.5 / 0.5) - (2.1 + 3.2 - 1.5) * (2 ^ -3 ^ 2 * 100) + ((10.5 % 3) * (3 ^ -2) / (5 - 3 - 1)) + (-5 + 3) * (-4^2) - (100 / (2 + 3)^2) + ((2 ^ -3) ^ 2) * 1000)
// this should return ~212.0494

[BepInPlugin("extra-commands", "Extra Commands", "1.0.0")]
public class ExtraCommands : BaseUnityPlugin {
    public static ConfigEntry<KeyboardShortcut> configRepeatLastCommand = null;
    public static ConfigEntry<string> configChatExpressionEvaluatorPrefix = null;

    public static ExpressionEvaluator expressionEvaluator = null;

    private void Start() {
        configRepeatLastCommand = Config.Bind<KeyboardShortcut>(
            section: "General", key: "RepeatLastCommand",
            defaultValue: new KeyboardShortcut(KeyCode.BackQuote, KeyCode.LeftControl),
            description: "Keyboard shortcut for running last command"
        );
        var configChatExpressionEvaluatorEnable = Config.Bind<bool>(
            section: "ChatExpressionEvaluator", key: "Enable",
            defaultValue: true, description: "Enable expression evaluator in chat"
        );
        configChatExpressionEvaluatorPrefix = Config.Bind<string>(
            section: "ChatExpressionEvaluator", key: "Prefix",
            defaultValue: "$", description: "Prefix for expression evaluation syntax"
        );
        var configFullChatHistory = Config.Bind<bool>(
            section: "General", key: "FullChatHistory",
            defaultValue: true, description: "Tracks every message in the chat and adds it to the history"
        );

        expressionEvaluator = new ExpressionEvaluator();
        expressionEvaluator.AddBuiltinVariables();
        expressionEvaluator.AddBuiltinFunctions();

        var harmony = new Harmony("extra-commands");
        harmony.PatchAll(typeof(CustomCommandsPatch));
        if (configChatExpressionEvaluatorEnable.Value) {
            harmony.PatchAll(typeof(ChatExpressionEvaluationPatch));
        }
        if (configFullChatHistory.Value) {
            harmony.PatchAll(typeof(FullChatHistoryPatch)); // must be after ChatExpressionEvaluationPatch
        }
        if (configRepeatLastCommand.Value.MainKey != KeyCode.None) {
            harmony.PatchAll(typeof(RepeatLastCommandPatch));
        }

        CustomCommands.AddCustomCommands();
    }
}


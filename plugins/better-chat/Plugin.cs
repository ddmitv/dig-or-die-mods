using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using UnityEngine;

[Serializable]
public class InvalidCommandArgument : Exception {
    public int? argIndex = null;

    public InvalidCommandArgument(string message) : base(message) {}

    public InvalidCommandArgument(string message, int argIndex) : base(message) {
        this.argIndex = argIndex;
    }
}

// expression evaluator test string:
// $(((3 + 4 * 2 - (10 % 3) + (5 - 3 - 1) * (-3^2) / 0.5) ^ (2 % 3)) % 100 + (12 / 3 * 4 - 12 / (3 * 4)) * (3.5 / 0.5) - (2.1 + 3.2 - 1.5) * (2 ^ -3 ^ 2 * 100) + ((10.5 % 3) * (3 ^ -2) / (5 - 3 - 1)) + (-5 + 3) * (-4^2) - (100 / (2 + 3)^2) + ((2 ^ -3) ^ 2) * 1000)
// this should return ~212.0494

[BepInPlugin("extra-commands", "Extra Commands", "1.0.1")]
public class BetterChat : BaseUnityPlugin {
    public static ConfigEntry<KeyboardShortcut> configRepeatLastCommand = null;
    public static ConfigEntry<string> configChatExpressionEvaluatorPrefix = null;
    public static ConfigEntry<bool> configDisableAchievementsOnCommand = null;

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
        configDisableAchievementsOnCommand = Config.Bind<bool>(
            section: "General", key: "DisableAchievementsOnCommand",
            defaultValue: true, description: "Disables achievements on any command (similar to /event and /param)"
        );

        expressionEvaluator = new ExpressionEvaluator();
        expressionEvaluator.AddBuiltinVariables();
        expressionEvaluator.AddBuiltinFunctions();

        var harmony = new Harmony(Info.Metadata.GUID);
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
        harmony.PatchAll(typeof(FreecamModePatch));
        harmony.PatchAll(typeof(ClockCommandPatch));

        CustomCommands.AddCustomCommands();
    }
}


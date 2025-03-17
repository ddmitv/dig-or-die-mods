using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using ModUtils;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

public static class EnableDebugModePatch {
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(SScreenDebug), nameof(SScreenDebug.OnUpdate))]
    private static IEnumerable<CodeInstruction> SScreenDebug_OnUpdate(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var codeMatcher = new CodeMatcher(instructions, generator);

        codeMatcher.Start()
            .MatchForward(useEnd: false,
                new(OpCodes.Call, AccessTools.PropertyGetter(typeof(Application), nameof(Application.isEditor))),
                new(OpCodes.Brtrue),

                new(OpCodes.Call, AccessTools.PropertyGetter(typeof(SNetwork), nameof(SNetwork.MySteamID))),
                new(OpCodes.Ldc_I8),
                new(OpCodes.Beq),

                new(OpCodes.Call, AccessTools.PropertyGetter(typeof(SNetwork), nameof(SNetwork.MySteamID))),
                new(OpCodes.Ldc_I8),
                new(OpCodes.Bne_Un))
            .ThrowIfInvalid("(1)")
            .RemoveInstructions(8);

        return codeMatcher.Instructions();
    }
}
public static class ApplicationIsEditorPatch {
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Application), nameof(Application.isEditor), MethodType.Getter)]
    private static IEnumerable<CodeInstruction> Application_isEditor() {
        return [new(OpCodes.Ldc_I4_1), new(OpCodes.Ret)];
    }
}
public static class NoWorldPresimulationPatch {
    [HarmonyTranspiler]
    [HarmonyDebug]
    [HarmonyPatch(typeof(SGameStartEnd), nameof(SGameStartEnd.GenerateWorld), MethodType.Enumerator)]
    private static IEnumerable<CodeInstruction> SGameStartEnd_GenerateWorld(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var codeMatcher = new CodeMatcher(instructions, generator);

        codeMatcher.Start()
            .MatchForward(useEnd: false,
                new(OpCodes.Call, typeof(UnityEngine.Application).GetMethod("get_isEditor")),
                new(OpCodes.Brfalse),
                new(OpCodes.Ldsfld, typeof(G).GetField("m_autoCreateMode")),
                new(OpCodes.Brfalse),
                new(OpCodes.Ldsfld, typeof(G).GetField("m_autoCreateMode_Fast")),
                new(OpCodes.Brfalse),
                new(OpCodes.Call, typeof(UnityEngine.Time).GetMethod("get_time")),
                new(OpCodes.Ldc_R4, 5.0f),
                new(OpCodes.Bge_Un),
                new(OpCodes.Br))
            .ThrowIfInvalid("(1)")
            .CollapseInstructions(9) // keep last `br` instruction to skip loop body
            .MatchForward(useEnd: false,
                new(OpCodes.Call, typeof(UnityEngine.Application).GetMethod("get_isEditor")),
                new(OpCodes.Brfalse),
                new(OpCodes.Ldsfld, typeof(G).GetField("m_autoCreateMode")),
                new(OpCodes.Brfalse),
                new(OpCodes.Ldsfld, typeof(G).GetField("m_autoCreateMode_Fast")),
                new(OpCodes.Brfalse),
                new(OpCodes.Call, typeof(UnityEngine.Time).GetMethod("get_time")),
                new(OpCodes.Ldc_R4, 5.0f),
                new(OpCodes.Bge_Un),
                new(OpCodes.Br))
            .ThrowIfInvalid("(2)")
            .CollapseInstructions(9); // keep last `br` instruction to skip loop body

        return codeMatcher.Instructions();
    }
}

[BepInPlugin("debug-mode", "Debug Mode", "1.0.0")]
public class DebugMode : BaseUnityPlugin
{
    private void OnChangedSetting() {
        var configDrawAllBackgrounds = Config.Bind<bool>(
            section: "Debug", key: "drawAllBackgrounds", defaultValue: false
        );
        var configBullets = Config.Bind<bool>(
            section: "Debug", key: "bullets", defaultValue: false
        );
        var configPathfinding = Config.Bind<bool>(
            section: "Debug", key: "pathfinding", defaultValue: false
        );
        var configPathfindingDetails = Config.Bind<bool>(
            section: "Debug", key: "pathfindingDetails", defaultValue: false
        );
        var configCollisions = Config.Bind<bool>(
            section: "Debug", key: "collisions", defaultValue: false
        );
        var configUnits = Config.Bind<bool>(
            section: "Debug", key: "units", defaultValue: false
        );
        var configUnitNetworkControl = Config.Bind<bool>(
            section: "Debug", key: "unitNetworkControl", defaultValue: false
        );
        var configDefenses = Config.Bind<bool>(
            section: "Debug", key: "defenses", defaultValue: false
        );
        var configWater = Config.Bind<bool>(
            section: "Debug", key: "water", defaultValue: false
        );
        var configLight = Config.Bind<bool>(
            section: "Debug", key: "light", defaultValue: false
        );
        var configCrashes = Config.Bind<bool>(
            section: "Debug", key: "crashes", defaultValue: false
        );
        var configCrashesFull = Config.Bind<bool>(
            section: "Debug", key: "crashesFull", defaultValue: false
        );
        G.m_debugDrawAllBackgrounds = configDrawAllBackgrounds.Value;
        G.m_debugBullets = configBullets.Value;
        G.m_debugPF = configPathfinding.Value;
        G.m_debugPFDetails = configPathfindingDetails.Value;
        G.m_debugCols = configCollisions.Value;
        G.m_debugUnits = configUnits.Value;
        G.m_debugUnitNetworkControl = configUnitNetworkControl.Value;
        G.m_debugDefenses = configDefenses.Value;
        G.m_debugWater = configWater.Value;
        G.m_debugLight = configLight.Value;
        G.m_debugCrashes = configCrashes.Value;
        G.m_debugCrashesFull = configCrashesFull.Value;
    }

    private void Start() {
        var configEnable = Config.Bind<bool>(
            section: "General", key: "Enable", defaultValue: true,
            description: "Enables the plugin"
        );
        var configIsEditor = Config.Bind<bool>(
            section: "StartUp", key: "IsEditor", defaultValue: true,
            description: "Forces `Application.isEditor` to always return `true`"
        );
        var configNoWorldPresimulation = Config.Bind<bool>(
            section: "StartUp", key: "NoWorldPresimulation", defaultValue: false,
            description: "Disables world presimulation (e.g. no initial water and plants are generated)"
        );
        if (!configEnable.Value) {
            return;
        }
        var harmony = new Harmony("debug-mode");

        harmony.PatchAll(typeof(EnableDebugModePatch));
        if (configNoWorldPresimulation.Value) {
            harmony.PatchAll(typeof(NoWorldPresimulationPatch));
        }
        if (configIsEditor.Value) {
            harmony.PatchAll(typeof(ApplicationIsEditorPatch));
        }

        OnChangedSetting();
        Config.SettingChanged += (object _, SettingChangedEventArgs _) => {
            OnChangedSetting();
        };
    }
}


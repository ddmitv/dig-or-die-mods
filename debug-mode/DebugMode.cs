using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using ModUtils;
using Mono.Cecil;
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
        Utils.UniqualizeVersionBuild(ref G.m_versionBuild, this);

        var harmony = new Harmony("debug-mode");

        harmony.PatchAll(typeof(EnableDebugModePatch));

        OnChangedSetting();
        Config.SettingChanged += (object _, SettingChangedEventArgs _) => {
            OnChangedSetting();
        };
    }
}


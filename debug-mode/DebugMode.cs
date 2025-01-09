using BepInEx;
using HarmonyLib;
using ModUtils;
using Mono.Cecil;
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
    private void Start() {
        Utils.UniqualizeVersionBuild(ref G.m_versionBuild, this);

        var harmony = new Harmony("debug-mode");
        harmony.PatchAll(typeof(EnableDebugModePatch));
    }
}


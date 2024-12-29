using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Mono.Cecil;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace ultra_hardcore;

public static class CodeMatcherExtensions {
    public static CodeMatcher GetLabels(this CodeMatcher self, out List<Label> labels) {
        labels = self.Labels;
        return self;
    }
}

public static class HpMaxPatch {
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(CUnitPlayer.CDesc), nameof(CUnitPlayer.CDesc.GetHpMax))]
    private static IEnumerable<CodeInstruction> CUnitPlayer_CDesc_GetHpMax(IEnumerable<CodeInstruction> instructions) {
        return [
            new CodeInstruction(OpCodes.Ldc_R4, UltraHardcorePlugin.configPlayerHpMax.Value), 
            new CodeInstruction(OpCodes.Ret)
        ];
    }
}
public static class PermanentMistPatch {
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(CBackground), nameof(CBackground.DrawBackgrounds))]
    [HarmonyPatch(typeof(SDrawWorld), "OnUpdate")]
    [HarmonyPatch(typeof(SWorld), "ProcessLighting_DynamicUnits")]
    [HarmonyPatch(typeof(SWorld), "UpdateLightSunValue")]
    private static IEnumerable<CodeInstruction> AlwaysMistTranspiler(IEnumerable<CodeInstruction> instructions) {
        var codeMatcher = new CodeMatcher(instructions);
        codeMatcher.Start()
            .MatchForward(useEnd: true,
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(SEnvironment), nameof(SEnvironment.GetEnvironmentCurrent))),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(CEnvironment), nameof(CEnvironment.m_mist))),
                new CodeMatch(OpCodes.Brfalse))
            .ThrowIfInvalid("Match failed 1")
            .SetAndAdvance(OpCodes.Pop, null)
            .MatchForward(useEnd: false,
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(SEnvironment), nameof(SEnvironment.GetEnvironmentCurrent))),
                new CodeMatch(OpCodes.Ldc_R4, 5f),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(CEnvironment), nameof(CEnvironment.GetBeginEndSmoothingValue))))
            .ThrowIfInvalid("Match failed 2")
            .SetAndAdvance(OpCodes.Ldc_R4, 1f)
            .RemoveInstructions(2);
        return codeMatcher.Instructions();
    }
}
public static class PermanentDarkPatch {
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SWorld), nameof(SWorld.UpdateLightSunValue))]
    private static void SWorld_UpdateLightSunValue() {
        G.m_sunLight = 0f;
    }
}
public static class NoRainPatch {
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(SWorld), "OnUpdateSimu")]
    private static IEnumerable<CodeInstruction> SWorld_OnUpdateSimu(IEnumerable<CodeInstruction> instructions) {
        var codeMatcher = new CodeMatcher(instructions);
        codeMatcher.Start()
            .MatchForward(useEnd: false,
                new CodeMatch(OpCodes.Ldc_I4_1))
            .ThrowIfInvalid("Match failed")
            .Set(OpCodes.Ldc_I4_0, null);
        return codeMatcher.Instructions();
    }
}
public static class InverseNightPatch {
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SGame), nameof(SGame.IsNight))]
    private static void SGame_IsNight(ref bool __result) {
        __result = !__result;
    }
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SGame), nameof(SGame.GetNightDurationLeft))]
    private static bool SGame_GetNightDurationLeft(ref float __result) {
        __result = 10f;
        return false;
    }
}
public static class PermanentAcidWaterPatch {
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(CUnit), "Update")]
    private static IEnumerable<CodeInstruction> CUnit_Update(IEnumerable<CodeInstruction> instructions) {
        var codeMatcher = new CodeMatcher(instructions);
        codeMatcher.Start()
            .MatchForward(useEnd: false,
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(SEnvironment), nameof(SEnvironment.GetEnvironmentCurrent))),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(CEnvironment), nameof(CEnvironment.m_acidWater))),
                new CodeMatch(OpCodes.Brfalse))
            .ThrowIfInvalid("Match failed")
            .GetLabels(out List<Label> labels)
            .RemoveInstructions(3)
            .AddLabels(labels);
        return codeMatcher.Instructions();
    }
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(CEnvironment), nameof(CEnvironment.GetWaterAcidRatio))]
    private static IEnumerable<CodeInstruction> CEnvironment_GetWaterAcidRatio(IEnumerable<CodeInstruction> instructions) {
        return [
            new CodeInstruction(OpCodes.Ldc_R4, 1f),
            new CodeInstruction(OpCodes.Ret)
        ];
    }
}

[BepInPlugin("ultra-hardcore", "Ultra Hardcore", "0.0.0")]
public class UltraHardcorePlugin : BaseUnityPlugin
{
    public static ConfigEntry<float> configPlayerHpMax;

    private void Start() {
        configPlayerHpMax = Config.Bind<float>(
            section: "UltraHardcore", key: "HpMax", defaultValue: 1f,
            configDescription: new ConfigDescription(
                "Maximum health for the player",
                new AcceptableValueRange<float>(0, float.MaxValue)
            )
        );
        var configPermanentMist = Config.Bind<bool>(
            section: "UltraHardcore", key: "PermanentMist", defaultValue: false,
            description: "Makes the mist permanent, regardless of the current events"
        );
        var configPermanentDarkness = Config.Bind<bool>(
            section: "UltraHardcore", key: "PermanentDarkness", defaultValue: false,
            description: "Makes the night permanent, but without monsters"
        );
        var configNoRain = Config.Bind<bool>(
            section: "UltraHardcore", key: "NoRain", defaultValue: false,
            description: "Removes all rains from the game"
        );
        var configInverseNight = Config.Bind<bool>(
            section: "UltraHardcore", key: "InverseNight", defaultValue: false,
            description: "Monsters attack during the day, but stop at night"
        );
        var configPermanentAcidWater = Config.Bind<bool>(
            section: "UltraHardcore", key: "PermanentAcidWater", defaultValue: false,
            description: "Makes event 'ACIDIC WATERS' always active"
        );

        var harmony = new Harmony("ultra-hardcore");

        harmony.PatchAll(typeof(HpMaxPatch));
        if (configPermanentMist.Value) {
            harmony.PatchAll(typeof(PermanentMistPatch));
        }
        if (configPermanentDarkness.Value) {
            harmony.PatchAll(typeof(PermanentDarkPatch));
        }
        if (configNoRain.Value) {
            harmony.PatchAll(typeof(NoRainPatch));
        }
        if (configInverseNight.Value) {
            harmony.PatchAll(typeof(InverseNightPatch));
        }
        if (configPermanentAcidWater.Value) {
            harmony.PatchAll(typeof(PermanentAcidWaterPatch));
        }
    }
}

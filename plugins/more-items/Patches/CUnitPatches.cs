
using HarmonyLib;
using ModUtils;
using ModUtils.Extensions;
using System.Collections.Generic;
using System.Reflection.Emit;

[HarmonyPatch(typeof(CUnit))]
internal static class CUnitPatches {
    private static Dictionary<ushort, double> lastRadiationHitDict = new();

    [HarmonyPrefix]
    [HarmonyPatch(nameof(CUnit.Damage_Local))]
    private static bool CUnit_Damage_Local(CUnit __instance) {
        var content = SWorld.Grid[__instance.PosCell.x, __instance.PosCell.y].GetContent();
        if (content is ExtCItem_Explosive citem && citem.indestructible) {
            return false;
        }
        return true;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(nameof(CUnit.Update))]
    private static IEnumerable<CodeInstruction> UnclampUnitSpeed(IEnumerable<CodeInstruction> instructions) {
        var codeMatcher = new CodeMatcher(instructions);

        codeMatcher.Start()
            .MatchForward(useEnd: false,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldflda, typeof(CUnit).Field("m_speed")),
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldflda, typeof(CUnit).Field("m_speed")),
                new(OpCodes.Ldfld, typeof(UnityEngine.Vector2).Field("x")),
                new(OpCodes.Ldc_R4, -30f),
                new(OpCodes.Ldc_R4, 30f),
                new(OpCodes.Call, typeof(UnityEngine.Mathf).Method<float, float, float>("Clamp")),
                new(OpCodes.Stfld, typeof(UnityEngine.Vector2).Field("x")))
            .ThrowIfInvalid("(1)")
            .RemoveInstructions(18);

        return codeMatcher.Instructions();
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(CUnit.Update))]
    private static void CUnit_Update_Prefix(CUnit __instance) {
        const int EffectRadius = 15;
        const float RadiationDamage = 10f;

        if (!__instance.IsAlive() || GVars.m_simuTimeD <= lastRadiationHitDict.GetValueSafe(__instance.m_id)) { return; }

        int2 pos = Utils.FindInCircleClamped(range: EffectRadius, __instance.PosCell, (int x, int y) => {
            return SWorld.Grid[x, y].GetContent() == CustomItems.RTG.Item;
        });
        if (pos == int2.negative) { return; }

        lastRadiationHitDict[__instance.m_id] = GVars.m_simuTimeD + 0.5;

        float distanceFactor = 1f - (pos - __instance.PosCell).sqrMagnitude / (float)(EffectRadius * EffectRadius);
        if (__instance is CUnitPlayer) {
            __instance.Damage(distanceFactor * RadiationDamage, showDamage: true);
        } else {
            __instance.Damage(distanceFactor * RadiationDamage / 3f);
        }
    }
}



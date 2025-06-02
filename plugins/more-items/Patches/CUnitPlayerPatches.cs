using HarmonyLib;
using ModUtils;
using ModUtils.Extensions;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

[HarmonyPatch]
internal static class CUnitPlayerPatches {
    [HarmonyPatch(typeof(CUnitPlayer), nameof(CUnitPlayer.Damage_Local))]
    [HarmonyPrefix]
    private static void CUnitPlayer_Damage_Local(CUnitPlayer __instance, ref float damage, string damageCause) {
        if (damageCause is not ("hit_up" or "hit_down" or "hit_side")) { return; }

        var player = __instance.GetPlayer();
        var impactShield = (ExtCItem_ImpactShield)player?.m_inventory?.GetBestActiveOfGroup(ExtCItem_ImpactShield.GroupId);
        if (impactShield is null) { return; }
        damage *= 1f - impactShield.m_customValue;
    }

    [HarmonyPatch(typeof(CUnitPlayerLocal), nameof(CUnitPlayerLocal.Update))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> CUnitPlayerLocal_Update(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        static float ModifyJetpackPowerUsage(float oldPowerUsage, CItem_Device jetpackDevice) {
            if (jetpackDevice is not ExtCItem_JetpackDevice jetpack) { return oldPowerUsage; }

            return jetpack.jetpackEnergyUsageMultiplier;
        }
        static float ModifyJetpackFlyForce(float oldFlyForce, CItem_Device jetpackDevice) {
            if (jetpackDevice is not ExtCItem_JetpackDevice jetpack) { return oldFlyForce; }

            return jetpack.jetpackFlyForce;
        }

        var codeMatcher = new CodeMatcher(instructions, generator);
        codeMatcher.Start()
            .MatchForward(useEnd: true,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldflda, typeof(CUnit).Field("m_forces")),
                new(OpCodes.Dup),
                new(OpCodes.Ldfld, typeof(Vector2).Field("y")),
                new(OpCodes.Ldc_R4, 85f)) // advance after this
            .ThrowIfInvalid("(1)")
            .Advance(1)
            .Insert(
                new(OpCodes.Ldloc_1),
                Transpilers.EmitDelegate(ModifyJetpackFlyForce));

        codeMatcher // continue after previous patch 
            .MatchForward(useEnd: false,
                new CodeMatch(OpCodes.Ldloc_S).LocalIndex(6),
                new(OpCodes.Ldc_R4, 0.0f),
                new(OpCodes.Ldc_R4, 0.19f), // advance after this
                new(OpCodes.Call, typeof(SMain).Method("get_SimuDeltaTime")),
                new(OpCodes.Mul))
            .ThrowIfInvalid("(2)")
            .GetInstruction(out CodeInstruction inst)
            .Advance(3)
            .Insert(
                new(OpCodes.Ldloc_1),
                Transpilers.EmitDelegate(ModifyJetpackPowerUsage));

        return codeMatcher.Instructions();
        // ldarg.0
        // ldflda CUnit::m_forces
        // dup
        // ldfld UnityEngine.Vector2::y
        // ldc.r4 85
        // |> ldloc.1
        // |> call Patches.CUnitPlayerLocal_Update.ModifyJetpackFlyForce(float, CItem_Device)
        // call SMain::get_SimuDeltaTime
        // mul

        // old: ... = m_forces.y + 85f * SMain.SimuDeltaTime * ...;
        // new: ... = m_forces.y + ModifyJetpackFlyForce(85f, V_1) * SMain.SimuDeltaTime * ...;

        // ldloc.s V_6
        // ldc.r4 0.0
        // ldc.r4 0.19
        // |> ldloc.1
        // |> call Patches.CUnitPlayerLocal_Update.ModifyJetpackPowerUsage(float, CItem_Device)
        // call SMain::get_SimuDeltaTime()
        // mul
        // ldsfld SInputs::shift
        // callvirt SInputs.KeyBinding::IsKey()

        // old: ... = Mathf.MoveTowards(..., 0f, 0.19f * SMain.SimuDeltaTime * ...);
        // new: ... = Mathf.MoveTowards(..., 0f, ModifyJetpackPowerUsage(0.19f, V_1) * SMain.SimuDeltaTime * ...);
    }
}

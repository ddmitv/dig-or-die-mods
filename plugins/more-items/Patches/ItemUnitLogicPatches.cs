using DODModAPI;
using HarmonyLib;
using DODModAPI.Extensions;
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
        if (player?.m_inventory?.GetBestActiveOfGroup(ExtCItem_ImpactShield.GroupId) is ExtCItem_ImpactShield impactShield) {
            damage *= 1f - impactShield.m_customValue;
        }
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
        return new CodeCursor(instructions, generator)
            .FindNextEnd(
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldflda, typeof(CUnit).Field("m_forces")),
                new(OpCodes.Dup),
                new(OpCodes.Ldfld, typeof(Vector2).Field("y")),
                new(OpCodes.Ldc_R4, 85f)
                // insert here
                )
            .Insert(
                new(OpCodes.Ldloc_1),
                Transpilers.EmitDelegate(ModifyJetpackFlyForce))
            // continue after previous patch 
            .FindNext(
                new(OpCodes.Ldloc_S, 6),
                new(OpCodes.Ldc_R4, 0.0f),
                new(OpCodes.Ldc_R4, 0.19f),
                // insert here
                new(OpCodes.Call, typeof(SMain).Method("get_SimuDeltaTime")),
                new(OpCodes.Mul))
            .Advance(3)
            .Insert(
                new(OpCodes.Ldloc_1),
                Transpilers.EmitDelegate(ModifyJetpackPowerUsage))
            .Finish();

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

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SUnits), nameof(SUnits.OnUpdateSimu))]
    private static void SUnits_OnUpdateSimu(SUnits __instance) {
        if (SNetwork.IsClient()) { return; }

        byte tag = (byte)(UnityEngine.Time.frameCount % 255);
        foreach (CUnit unit in __instance.m_units) {
            if (unit is not ExtCUnitWaterVaporizer) { continue; }

            if (SWorld.Grid[unit.PosCell.x, unit.PosCell.y].GetContent() is not ExtCItem_WaterVaporizer) {
                SUnits.RemoveUnit(unit);
                continue;
            }
            SWorld.Grid[unit.PosCell.x, unit.PosCell.y].m_temp.r = tag;
        }
        foreach (CPlayer player in SNetwork.Players) {
            RectInt updateRect = DODModAPI.Misc.ClampRect(player.GetRectAroundScreen(12), 0, 0, SWorld.Gs.x, SWorld.Gs.y);
            for (int x = updateRect.x; x < updateRect.xMax; x++) {
                for (int y = updateRect.y; y < updateRect.yMax; y++) {
                    if (SWorld.Grid[x, y].GetContent() is ExtCItem_WaterVaporizer && SWorld.Grid[x, y].m_temp.r != tag) {
                        SUnits.SpawnUnit(uDesc: CustomUnits.waterVaporizer.UnitDesc, new Vector2(x + 0.5f, y));
                    }
                }
            }
        }
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(SUnits), nameof(SUnits.OnUpdateSimu))]
    private static IEnumerable<CodeInstruction> SUnits_OnUpdateSimu_BossRespawnDelay(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        return new CodeCursor(instructions, generator)
            .FindNext(out uint firstRemoveNum,
                new(OpCodes.Call, typeof(SOutgame).Method("get_Params")),
                new(OpCodes.Ldfld, typeof(CParams).Field("m_bossRespawnDelay")),
                new(OpCodes.Ldc_R4, 0f),
                new(OpCodes.Blt_Un))
            .RemovePreservingLabels(firstRemoveNum)
            .FindNext(out uint secondRemoveNum,
                new(OpCodes.Call, typeof(SOutgame).Method("get_Params")),
                new(OpCodes.Ldfld, typeof(CParams).Field("m_bossRespawnDelay")))
            .Remove(secondRemoveNum)
            .Insert(OpCodes.Ldc_R4, MoreItemsPlugin.configBossRespawnDelay.Value)
            .Finish();
    }

    private static readonly WeakTable<CUnit, double> lastRadiationHitDict = [];

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CUnit), nameof(CUnit.Damage_Local))]
    private static bool CUnit_Damage_Local(CUnit __instance) {
        var content = SWorld.Grid[__instance.PosCell.x, __instance.PosCell.y].GetContent();
        if (content is ExtCItem_Explosive citem && citem.indestructible) {
            return false;
        }
        return true;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(CUnit), nameof(CUnit.Update))]
    private static IEnumerable<CodeInstruction> UnclampUnitSpeed(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        return new CodeCursor(instructions, generator)
            .FindNext(
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldflda, typeof(CUnit).Field("m_speed")),
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldflda, typeof(CUnit).Field("m_speed")),
                new(OpCodes.Ldfld, typeof(UnityEngine.Vector2).Field("x")),
                new(OpCodes.Ldc_R4, -30f),
                new(OpCodes.Ldc_R4, 30f),
                new(OpCodes.Call, typeof(UnityEngine.Mathf).Method<float, float, float>("Clamp")),
                new(OpCodes.Stfld, typeof(UnityEngine.Vector2).Field("x")))
            .Remove(18)
            .Finish();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CUnit), nameof(CUnit.Update))]
    private static void CUnit_Update_Prefix(CUnit __instance) {
        const int EffectRadius = 15;
        const float RadiationDamage = 10f;

        if (!__instance.IsAlive() || GVars.m_simuTimeD <= lastRadiationHitDict.GetValueOrDefault(__instance)) {
            return;
        }

        int2 pos = ItemHelpers.FindInCircleClamped(range: EffectRadius, __instance.PosCell, static (int x, int y) => {
            return SWorld.Grid[x, y].GetContent() == CustomItems.RTG.item;
        });
        if (pos == int2.negative) { return; }

        lastRadiationHitDict.AddOrUpdate(__instance, GVars.m_simuTimeD + 0.5);

        float distanceFactor = 1f - (pos - __instance.PosCell).sqrMagnitude / (float)(EffectRadius * EffectRadius);
        if (__instance is CUnitPlayer) {
            __instance.Damage(distanceFactor * RadiationDamage, showDamage: true);
        } else {
            __instance.Damage(distanceFactor * RadiationDamage / 3f);
        }
    }
}

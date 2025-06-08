using HarmonyLib;
using ModUtils;
using ModUtils.Extensions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

[HarmonyPatch]
internal static class CUnitPlayerPatches {
    [HarmonyPostfix]
    [HarmonyPatch(nameof(SUnits.OnInit))]
    private static void SUnits_OnInit() {
        foreach (var uDescField in typeof(CustomUnits).GetFields(BindingFlags.Static | BindingFlags.Public)) {
            var uDesc = (CUnit.CDesc)uDescField.GetValue(null);
            if (uDesc.m_codeName is null) { throw new InvalidOperationException($"{uDescField.DeclaringType.FullName}.{uDescField.Name}.m_codeName is null"); }
            uDesc.m_id = (byte)GUnits.UDescs.Count;
            GUnits.UDescs.Add(uDesc);
            if (GUnits.UDescs.Count >= 255) {
                throw new InvalidOperationException($"GUnits.UDescs can only have 255 elements");
            }
        }
    }

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

    [HarmonyPrefix]
    [HarmonyPatch(nameof(SUnits.OnUpdateSimu))]
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
            RectInt updateRect = Utils.ClampRect(player.GetRectAroundScreen(12), 0, 0, SWorld.Gs.x, SWorld.Gs.y);
            for (int x = updateRect.x; x < updateRect.xMax; x++) {
                for (int y = updateRect.y; y < updateRect.yMax; y++) {
                    if (SWorld.Grid[x, y].GetContent() is ExtCItem_WaterVaporizer waterVaporizerItem
                        && SWorld.Grid[x, y].m_temp.r != tag) {
                        var unit = (ExtCUnitWaterVaporizer)SUnits.SpawnUnit(
                            uDesc: CustomUnits.unitDesc, new UnityEngine.Vector2(x + 0.5f, y));
                        unit.waterVaporizerItem = waterVaporizerItem;
                    }
                }
            }
        }
    }

    [HarmonyTranspiler]
    [HarmonyPatch(nameof(SUnits.OnUpdateSimu))]
    private static IEnumerable<CodeInstruction> SUnits_OnUpdateSimu_BossRespawnDelay(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        return new CodeMatcher(instructions, generator)
            .MatchForward(useEnd: false,
                new(OpCodes.Call, typeof(SOutgame).Method("get_Params")),
                new(OpCodes.Ldfld, typeof(CParams).Field("m_bossRespawnDelay")),
                new(OpCodes.Ldc_R4, 0f),
                new(OpCodes.Blt_Un))
            .ThrowIfInvalid("(1)")
            .CollapseInstructions(4)
            .MatchForward(useEnd: false,
                new(OpCodes.Call, typeof(SOutgame).Method("get_Params")),
                new(OpCodes.Ldfld, typeof(CParams).Field("m_bossRespawnDelay")))
            .ThrowIfInvalid("(2)")
            .RemoveInstructions(2)
            .Insert(
                new CodeInstruction(OpCodes.Ldc_R4, MoreItemsPlugin.configBossRespawnDelay.Value))
            .Instructions();
    }

    private static readonly Dictionary<ushort, double> lastRadiationHitDict = new();

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

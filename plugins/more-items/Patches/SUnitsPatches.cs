
using HarmonyLib;
using ModUtils;
using ModUtils.Extensions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

[HarmonyPatch(typeof(SUnits))]
internal static class SUnitsPatches {
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
}



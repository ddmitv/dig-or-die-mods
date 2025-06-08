
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using ModUtils.Extensions;
using ModUtils;

[HarmonyPatch(typeof(CUnitDefense))]
internal static class CUnitDefensePatches {
    [HarmonyPatch(typeof(CUnitDefense), nameof(CUnitDefense.GetUnitTargetPos))]
    [HarmonyPrefix]
    private static bool CUnitDefense_GetUnitTargetPos(CUnitDefense __instance, ref Vector2 __result) {
        if (__instance.m_item is ExtCItem_Collector) {
            __result = GetCollectorTargetPos(__instance);

            return false;
        }
        return true;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(CUnitDefense), nameof(CUnitDefense.Update))]
    private static IEnumerable<CodeInstruction> CUnitDefense_Update(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var codeMatcher = new CodeMatcher(instructions, generator);

        PatchTeslaTurretMK2(codeMatcher);
        PatchExplosive(codeMatcher);
        PatchCollector(codeMatcher);
        PatchSpikesTurretClass(codeMatcher);

        return codeMatcher.Instructions();
    }
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CUnitDefense), nameof(CUnitDefense.OnDisplayWorld))]
    private static void CUnitDefense_OnDisplayWorld(CUnitDefense __instance) {
        if (__instance.m_item is ExtCItem_Explosive item && __instance.m_lastFireTime > 0f && GVars.m_simuTimeD > (double)__instance.m_lastFireTime) {
            CMesh<CMeshText>.Get("ITEMS").Draw(
                text: Mathf.CeilToInt(__instance.m_lastFireTime + item.explosionTime - GVars.SimuTime).ToString(),
                pos: __instance.m_pos + new Vector2(0f, 0.4f),
                size: 0.3f,
                color: item.timerColor
            );
        }
    }
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CUnitDefense), nameof(CUnitDefense.OnActivate))]
    private static void CUnitDefense_OnActivate(CUnitDefense __instance) {
        if (__instance.m_item is ExtCItem_Explosive expItem && __instance.m_lastFireTime < 0f) {
            __instance.m_lastFireTime = GVars.SimuTime;

            if (expItem.indestructible) {
                SSingleton<SWorld>.Inst.SetContent(
                    pos: __instance.PosCell - int2.up,
                    item: (CItemCell)CustomItems.indestructibleLavaOld.Item
                );
            }
            ExtCItem_Explosive.lastTimeMap[__instance.Id] = 0f;
        }
    }
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(CUnitDefense), nameof(CUnitDefense.OnDisplayWorld))]
    [HarmonyPatch(typeof(CUnitDefense), nameof(CUnitDefense.Update))]
    private static IEnumerable<CodeInstruction> CUnitDefense_OnDisplayWorld(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        return new CodeMatcher(instructions, generator).Start()
            .MatchForward(useEnd: false,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(CUnitDefense).Field("m_item")),
                new(OpCodes.Ldsfld, typeof(GItems).StaticField("turretCeiling")),
                new(OpCodes.Bne_Un))
            .CreateLabelAtOffset(4, out Label successLabel)
            .InjectAndAdvance(OpCodes.Ldarg_0)
            .Insert(
                new(OpCodes.Ldfld, typeof(CUnitDefense).Field("m_item")),
                new(OpCodes.Isinst, typeof(ExtCItem_CeilingTurret)),
                new(OpCodes.Brtrue, successLabel))
            .Instructions();
    }

    private static void PatchExplosive(CodeMatcher codeMatcher) {
        static void ExplosiveLogic(CUnitDefense self) {
            if (self.GetLastFireTime() <= 0f) {
                return;
            }
            var item = (ExtCItem_Explosive)self.m_item;

            ref var currentCell = ref SWorld.Grid[self.PosCell.x, self.PosCell.y];

            if (item.lavaReleaseTime >= 0
                && GVars.SimuTime >= self.GetLastFireTime() + item.lavaReleaseTime
                && GVars.SimuTime > ExtCItem_Explosive.lastTimeMap[self.Id]
            ) {
                const float dt = ExtCItem_Explosive.deltaTime;
                ExtCItem_Explosive.lastTimeMap[self.Id] = GVars.SimuTime + dt;

                float releaseTime = GVars.SimuTime - (self.GetLastFireTime() + item.lavaReleaseTime);
                float completionPercentage = releaseTime / (item.explosionTime - item.lavaReleaseTime);

                Utils.AddLava(ref currentCell, item.lavaQuantity * Mathf.Pow(3f, releaseTime));

                var fireRange = Mathf.Lerp(0f, item.m_attack.m_range * 5f, completionPercentage);
                SWorld.SetFireAround(self.PosCell, fireRange);

                var evaporationRange = Mathf.Lerp(0f, item.m_attack.m_range * 4f, completionPercentage);
                Utils.EvaporateWaterAround(Mathf.CeilToInt(evaporationRange), self.PosCell, evaporationRate: 0.6f);

                var destructionRange = Mathf.CeilToInt(Mathf.Lerp(0f, item.m_attack.m_range / 3f, completionPercentage));
                SWorld.DoDamageAOE(self.Pos, destructionRange, Utils.CeilDiv(item.m_attack.m_damage, 20));
            }
            if (GVars.m_simuTimeD <= (double)(self.GetLastFireTime() + item.explosionTime)) {
                return;
            }
            item.DestoryItself(self.PosCell);
            item.DoDamageAround(self.PosCenter, item.m_attack);
            item.PlayExplosionSound(item.m_attack.Sound, self.PosCenter);
            item.StartVolcanoEruption();
            item.DoExplosionBgChange(self.PosCell);
            item.DoExplosionLavaRelease(ref currentCell);
            item.DoFlashEffect(self.PosCenter);
            item.DoShockWave(self.m_pos);
            item.DoFireAround(self.m_pos);
        }

        codeMatcher.Start()
            .MatchForward(useEnd: false,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(CUnitDefense).Field("m_item")),
                new(OpCodes.Ldsfld, typeof(GItems).StaticField("explosive")),
                new(OpCodes.Bne_Un))
            .ThrowIfInvalid("(1)")
            .Advance(1)
            .Insert(new CodeInstruction(OpCodes.Ldarg_0))
            .CreateLabel(out var nextLabel)
            .Insert(
                new(OpCodes.Ldfld, typeof(CUnitDefense).Field("m_item")),
                new(OpCodes.Isinst, typeof(ExtCItem_Explosive)),
                new(OpCodes.Ldnull),
                new(OpCodes.Beq, nextLabel),
                new(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate(ExplosiveLogic));
    }

    private static void PatchCollector(CodeMatcher codeMatcher) {
        static void CollectorLogic(CUnitDefense self, Vector2 targetPos) {
            int particlesCount = (int)(GVars.m_simuTimeD * 15.0) - (int)((GVars.m_simuTimeD - SMain.SimuDeltaTimeD) * 15.0);
            SSingleton<SParticles>.Inst.EmitMultiple(
                count: particlesCount,
                origin: new Rect(targetPos.x - 0.3f, targetPos.y - 0.3f, 0.6f, 0.6f),
                speed: 10f,
                color: self.m_item.m_mainColor,
                type: SParticles.Type.Reparator,
                paramVector: new Rect(self.PosFire.x, self.PosFire.y, 0f, 0f)
            );

            self.m_timeRepaired += SMain.SimuDeltaTime;
            if (self.m_timeRepaired > self.m_item.m_attack.m_cooldown) {

                self.m_timeRepaired -= self.m_item.m_attack.m_cooldown;
                SSingleton<SWorld>.Inst.DoDamageToCell(new int2(targetPos), ((ExtCItem_Collector)self.m_item).collectorDamage, 2, true);
            }
        }
        codeMatcher.Start()
            .MatchForward(useEnd: true,
                new(OpCodes.Call, typeof(Mathf).Method("MoveTowardsAngle")),
                new(OpCodes.Stfld, typeof(CUnitDefense).Field("m_angleDeg")))
            .ThrowIfInvalid("(1)")
            .Advance(1)
            .CreateLabel(out var skipLabel)
            .Insert(
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(CUnitDefense).Field("m_item")),
                new(OpCodes.Isinst, typeof(ExtCItem_Collector)),
                new(OpCodes.Ldnull),
                new(OpCodes.Beq, skipLabel),
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldloc_S, (byte)4),
                Transpilers.EmitDelegate(CollectorLogic),
                new(OpCodes.Ldc_I4_1), // flag = true
                new(OpCodes.Stloc_2));
    }
    private static void PatchTeslaTurretMK2(CodeMatcher codeMatcher) {
        codeMatcher.Start()
            .MatchForward(useEnd: false,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(CUnitDefense).Field("m_item")),
                new(OpCodes.Ldsfld, typeof(GItems).StaticField("turretTesla")),
                new(OpCodes.Bne_Un))
            .ThrowIfInvalid("(1)")
            .CreateLabelAtOffset(4, out var teslaCond) // after bne.un
            .Advance(1)
            .InsertAndAdvance(
                new(OpCodes.Ldfld, typeof(CUnitDefense).Field("m_item")),
                new(OpCodes.Ldfld, typeof(CItem_Defense).Field("m_codeName")),
                new(OpCodes.Ldstr, "turretTeslaMK2"),
                new(OpCodes.Beq, teslaCond),
                new(OpCodes.Ldarg_0));
    }
    private static Vector2 GetCollectorTargetPos(CUnitDefense self) {
        int range = Mathf.FloorToInt(self.m_item.m_attack.m_range);
        float closestDist = float.MaxValue;
        Vector2 result = Vector2.zero;
        bool isBasaltCollector = ((ExtCItem_Collector)self.m_item).isBasaltCollector;

        for (int i = self.PosCell.x - range; i <= self.PosCell.x + range; ++i) {
            for (int j = self.PosCell.y - range; j <= self.PosCell.y + range; ++j) {
                if (i == self.PosCell.x && j == self.PosCell.y) { continue; }

                int2 relative = new int2(i, j) - self.PosCell;

                if (relative.sqrMagnitude <= range * range) {
                    if (!Utils.IsValidCell(i, j)) { continue; }

                    CItemCell content = SWorld.Grid[i, j].GetContent();
                    if (isBasaltCollector ? ReferenceEquals(content, GItems.lava) : content is CItem_Plant
                        && relative.sqrMagnitude < closestDist) {
                        closestDist = relative.sqrMagnitude;
                        result = new Vector2(i + 0.5f, j + 0.5f);
                    }
                }
            }
        }
        return result;
    }
    private static void PatchSpikesTurretClass(CodeMatcher codeMatcher) {
        codeMatcher.Start()
            .MatchForward(useEnd: false,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(CUnitDefense).Field("m_item")),
                new(OpCodes.Ldsfld, typeof(GItems).StaticField("turretSpikes")),
                new(OpCodes.Bne_Un))
            .ThrowIfInvalid("(1)")
            .CreateLabelAtOffset(4, out Label successLabel)
            .InjectAndAdvance(OpCodes.Ldarg_0)
            .Insert(
                new(OpCodes.Ldfld, typeof(CUnitDefense).Field("m_item")),
                new(OpCodes.Isinst, typeof(ExtCItem_SpikesTurret)),
                new(OpCodes.Brtrue, successLabel));
    }
}

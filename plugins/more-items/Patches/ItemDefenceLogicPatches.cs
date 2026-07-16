
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using DODModAPI.Extensions;
using DODModAPI;

[HarmonyPatch]
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
        var codeCursor = new CodeCursor(instructions, generator);

        PatchTeslaTurretMK2(codeCursor);
        PatchExplosive(codeCursor);
        PatchCollector(codeCursor);
        PatchSpikesTurretClass(codeCursor);

        return codeCursor.Finish();
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
                    item: (CItemCell)CustomItems.indestructibleLavaOld.item
                );
            }
            ExtCItem_Explosive.lastTimeWeakTable.AddOrUpdate(__instance, 0f);
        }
    }
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(CUnitDefense), nameof(CUnitDefense.OnDisplayWorld))]
    [HarmonyPatch(typeof(CUnitDefense), nameof(CUnitDefense.Update))]
    private static IEnumerable<CodeInstruction> CUnitDefense_OnDisplayWorld(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        return new CodeCursor(instructions, generator)
            .FindNext(
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(CUnitDefense).Field("m_item")),
                new(OpCodes.Ldsfld, typeof(GItems).StaticField("turretCeiling")),
                new(OpCodes.Bne_Un))
            .CreateLabel(offset: 4, out Label successLabel)
            .Inject(
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(CUnitDefense).Field("m_item")),
                new(OpCodes.Isinst, typeof(ExtCItem_CeilingTurret)),
                new(OpCodes.Brtrue, successLabel))
            .Finish();
    }
    enum DuoTurretState : byte { Left, Right }
    private static readonly DODModAPI.WeakTable<CUnitDefense, DuoTurretState> duoTurretStates = new();

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SBullets), nameof(SBullets.FireBullet))]
    private static bool SBullets_FireBullet(SBullets __instance, CAttackDesc attackDesc, CUnit attacker, Vector2 firePos, Vector2 aimedPos) {
        const float fireDisplacement = 0.06f;

        if (attackDesc is not ExtDuoCAttackDesc || attacker is not CUnitDefense defenseAttacker) { return true; }
        DuoTurretState state = duoTurretStates.GetOrAdd(defenseAttacker, DuoTurretState.Left);

        Vector2 vector = aimedPos - (attacker is not CUnitPlayer ? firePos : attacker.PosCenter);
        float angle = Mathf.Atan2(vector.y, vector.x);
        Vector2 normalizedDisplacedFirePos = (state == DuoTurretState.Left ? Misc.RotateLeft(firePos) : Misc.RotateRight(firePos)).normalized;
        Vector2 displacedFirePos = firePos + normalizedDisplacedFirePos * fireDisplacement;

        __instance.m_bullets.Add(new CBullet(attackDesc, attacker, displacedFirePos, angle, aimedPos));

        duoTurretStates.AddOrUpdate(defenseAttacker, state == DuoTurretState.Left ? DuoTurretState.Right : DuoTurretState.Left);

        return false;
    }

    private static void PatchExplosive(CodeCursor codeCursor) {
        codeCursor.MoveToStart()
            .FindNext(
                // inject here
            // skipLabel:
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(CUnitDefense).Field("m_item")),
                new(OpCodes.Ldsfld, typeof(GItems).StaticField("explosive")),
                new(OpCodes.Bne_Un))
            .DeclareLabel(out var skipLabel)
            .InjectWithLabel(skipLabel,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(CUnitDefense).Field("m_item")),
                new(OpCodes.Isinst, typeof(ExtCItem_Explosive)),
                new(OpCodes.Ldnull),
                new(OpCodes.Beq, skipLabel),
                new(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate(ExtCItem_Explosive.ExplosiveLogic));
    }

    private static void PatchCollector(CodeCursor codeCursor) {
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
        codeCursor.MoveToStart()
            .FindNextEnd(
                new(OpCodes.Call, typeof(Mathf).Method("MoveTowardsAngle")),
                new(OpCodes.Stfld, typeof(CUnitDefense).Field("m_angleDeg"))
            // skipLabel:
            )
            .CreateLabel(offset: 0, out var skipLabel)
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
    private static void PatchTeslaTurretMK2(CodeCursor codeCursor) {
        codeCursor.MoveToStart()
            .FindNext(
                // inject here
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(CUnitDefense).Field("m_item")),
                new(OpCodes.Ldsfld, typeof(GItems).StaticField("turretTesla")),
                new(OpCodes.Bne_Un)
            // teslaCond:
            )
            .CreateLabel(offset: 4, out var teslaCond) // after bne.un
            .Inject(
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(CUnitDefense).Field("m_item")),
                new(OpCodes.Ldsfld, typeof(CustomItems).StaticField(nameof(CustomItems.turretTeslaMK2))),
                new(OpCodes.Ldfld, typeof(ModItem).Field(nameof(ModItem.item))),
                new(OpCodes.Beq, teslaCond));
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
                    if (!Misc.IsInWorld(i, j)) { continue; }

                    CItemCell content = SWorld.Grid[i, j].GetContent();
                    if (isBasaltCollector ? content == GItems.lava : content is CItem_Plant
                        && relative.sqrMagnitude < closestDist) {
                        closestDist = relative.sqrMagnitude;
                        result = new Vector2(i + 0.5f, j + 0.5f);
                    }
                }
            }
        }
        return result;
    }
    private static void PatchSpikesTurretClass(CodeCursor codeCursor) {
        codeCursor.MoveToStart()
            .FindNext(
                // inject here
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(CUnitDefense).Field("m_item")),
                new(OpCodes.Ldsfld, typeof(GItems).StaticField("turretSpikes")),
                new(OpCodes.Bne_Un)
            // successLabel:
            )
            .CreateLabel(offset: 4, out Label successLabel)
            .Inject(
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(CUnitDefense).Field("m_item")),
                new(OpCodes.Isinst, typeof(ExtCItem_SpikesTurret)),
                new(OpCodes.Brtrue, successLabel));
    }
}

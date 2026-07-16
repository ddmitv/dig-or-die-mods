using DODModAPI;
using DODModAPI.Extensions;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

[HarmonyPatch]
internal static class ItemLogicPatches {
    [HarmonyPatch(typeof(CItem_Device), nameof(CItem_Device.OnUpdate))]
    [HarmonyPrefix]
    private static bool CItem_Device_OnUpdate(CItem_Device __instance) {
        // Original CItem_Device.OnUpdate uses `this == GItems.defenseShield` to check for Shield item
        if (__instance.m_groupId == DeviceGroupIds.shield) {
            CItemVars myVars = __instance.GetMyVars();
            if (GVars.m_simuTimeD > (double)(myVars.ShieldLastHitTime + 0.5f)) {
                myVars.ShieldValue = Mathf.Min(myVars.ShieldValue + SMain.SimuDeltaTime * 0.5f * __instance.m_customValue * G.m_player.GetHpMax(), __instance.m_customValue * G.m_player.GetHpMax());
            }
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(CItem_Weapon), nameof(CItem_Weapon.Use_Local))]
    [HarmonyPostfix]
    private static void CItem_Weapon_Use_Local_Postfix(CItem_Weapon __instance, CPlayer player, Vector2 worldPos, bool isShift) {
        if (__instance == CustomItems.gunZF0Shotgun.item && SWorld.GridRectCam.Contains(worldPos) && isShift) {
            CItemVars vars = GItems.gunZF0.GetVars(player);
            vars.ZF0TargetLastTimeHit = float.MinValue;
            vars.ZF0TargetId = ushort.MaxValue;
        }
    }

    [HarmonyPatch(typeof(CItem_Device), nameof(CItem_Device.Use_Local))]
    [HarmonyPostfix]
    private static void CItem_Device_Use_Local(CItem_Device __instance, CPlayer player, Vector2 mousePos, bool isShift) {
        if (__instance == CustomItems.portableTeleport.item) {
            CItem_MachineTeleport.m_teleportersPos.Clear();
            CItem_MachineTeleport.m_teleportersPos.Add(player.m_unitPlayer.PosCell);
            for (int i = 0; i < SWorld.Gs.x; i++) {
                for (int j = 0; j < SWorld.Gs.y; j++) {
                    if (SWorld.Grid[i, j].GetContent() == GItems.teleport) {
                        CItem_MachineTeleport.m_teleportersPos.Add(new int2(i, j));
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(CItem_MachineTeleport), nameof(CItem_MachineTeleport.Teleport))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> CItem_MachineTeleport_Teleport_PortableTeleport(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        return new CodeCursor(instructions, generator)
            .FindNext(
            // teleportLabel:
                new(OpCodes.Ldsfld, typeof(G).StaticField("m_player")),
                new(OpCodes.Ldloc_1),
                new(OpCodes.Call, typeof(int2).Method<int2>("op_Implicit")))
            .CreateLabel(offset: 0, out Label teleportLabel)

            .MoveToStart()
            .FindNext(
                // insert here
                new(OpCodes.Call, typeof(SWorld).Method("get_Grid")),
                new(OpCodes.Ldloca_S),
                new(OpCodes.Ldfld, typeof(int2).Field("x")),
                new(OpCodes.Ldloca_S),
                new(OpCodes.Ldfld, typeof(int2).Field("y")),
                new(OpCodes.Call, typeof(CCell[,]).Method<int, int>("Address")),

                new(OpCodes.Call, typeof(CCell).Method("GetContent")),
                new(OpCodes.Ldsfld, typeof(GItems).StaticField("teleport")),
                new(OpCodes.Bne_Un))
            .Insert(
                Transpilers.EmitDelegate(static () => {
                    return G.m_player.PosCell == CItem_MachineTeleport.m_teleportersPos[0];
                }),
                new(OpCodes.Brtrue, teleportLabel))
            .Finish();
    }

    [HarmonyPatch(typeof(SDrawWorld), nameof(SDrawWorld.DrawElectricLightIFN))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> SDrawWorld_DrawElectricLightIFN(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        return new CodeCursor(instructions, generator)
            //     for (int k = 0; k < num % 10; k++)
            //          ^^^^^^^^^
            .FindNext(
                // inject here
                new(OpCodes.Ldc_I4_0),
                new(OpCodes.Stloc_3),
                new(OpCodes.Br))
            //     num = Math.Min(num, 5);
            //     +++++++++++++++++++++++
            //     for (int k = 0; k < num % 10; k++)
            .Inject(
                new(OpCodes.Ldloc_0),
                new(OpCodes.Ldc_I4_S, (sbyte)5),
                new(OpCodes.Call, typeof(Math).Method<int, int>("Min")),
                new(OpCodes.Stloc_0))
            .Finish();
    }

    [HarmonyPatch(typeof(SWorld), nameof(SWorld.DoDamageToCell))]
    [HarmonyPrefix]
    private static bool SWorld_DoDamageToCell(ref int2 cellPos, ref bool __result) {
        var content = SWorld.Grid[cellPos.x, cellPos.y].GetContent();
        if ((content is ExtCItem_Explosive citem && citem.indestructible) || content is ExtCItem_IndestructibleMineral) {
            __result = false;
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(SWorldDll), nameof(SWorldDll.ProcessSimu))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> SWorldDll_ProcessSimu(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        static float GetPlantGrowChance(float origChance, CItem_Mineral mineral) {
            if (mineral is ExtCItem_FertileMineralDirt fertileDirt) {
                return fertileDirt.plantGrowChange;
            }
            return origChance;
        }
        return new CodeCursor(instructions, generator)
            //     if (citem_Mineral.m_plantsSupported != null && grid[x, y + 1].GetContent() == null && global::UnityEngine.Random.value < 0.15f)
            //                                                                                           ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            .FindNext(
                new(OpCodes.Call, typeof(UnityEngine.Random).Method("get_value")),
                new(OpCodes.Ldc_R4, 0.15f)
                // insert here
            )
            .Advance(2)
            //     if (... && global::UnityEngine.Random.value < 0.15f GetPlantGrowChance(0.15f, citem_Mineral))
            //                                                   ----- ++++++++++++++++++++++++++++++++++++++++
            .Insert(
                new(OpCodes.Ldloc_S, (byte)8), // citem_Mineral local (assigned to grid[x, y].GetContent() as CItem_Mineral)
                Transpilers.EmitDelegate(GetPlantGrowChance))
            .Finish();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(SItems), nameof(SItems.OnUpdateSimu))]
    private static IEnumerable<CodeInstruction> SItems_OnUpdateSimu_PortableTeleport(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        return new CodeCursor(instructions, generator)
            .FindNextEnd(
                new(OpCodes.Call, typeof(SSingleton<SWorld>).Method("get_Inst")),
                new(OpCodes.Ldloc_S),
                new(OpCodes.Callvirt, typeof(SWorld).Method<int2>("GetCellContent")),
                new(OpCodes.Isinst),
                new(OpCodes.Brtrue) // get this operand (skipLabel)
                // insert here
            )
            .GetOperand(offset: -1, out Label skipLabel)
            .Insert(
                Transpilers.EmitDelegate(static () => {
                    return G.m_player.PosCell == CItem_MachineTeleport.m_teleportersPos[0];
                }),
                new(OpCodes.Brtrue, skipLabel))
            .Finish();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(SItems), nameof(SItems.OnUpdateSimu))]
    private static IEnumerable<CodeInstruction> SItems_OnUpdateSimu_MiniaturizorMK6(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        static bool MiniaturizorMK6CustomLogic(CItem_Device cItem_Device, CItemCell content) {
            if (cItem_Device != CustomItems.miniaturizorMK6.item) { return false; }

            return content.m_hpMax > GItems.miniaturizorMK5.m_customValue;
        }

        return new CodeCursor(instructions, generator)
            // if (content != null && !(content is CItem_Wall) && (float)content.m_hpMax > customValue)
            .FindNextEnd(
                new(OpCodes.Ldloc_S),
                new(OpCodes.Brfalse),

                new(OpCodes.Ldloc_S),
                new(OpCodes.Isinst, typeof(CItem_Wall)),
                new(OpCodes.Brtrue),
                // insert here
                new(OpCodes.Ldloc_S),
                new(OpCodes.Ldfld, typeof(CItemCell).Field("m_hpMax")),
                new(OpCodes.Conv_R4),
                new(OpCodes.Ldloc_S),
                new(OpCodes.Ble_Un)
            // cannotDigLabel:
            )
            .CreateLabel(offset: 0, out Label cannotDigLabel)
            .Advance(-5)
            .Insert(
                new(OpCodes.Ldloc_S, (byte)16),
                new(OpCodes.Ldloc_S, (byte)18),
                Transpilers.EmitDelegate(MiniaturizorMK6CustomLogic),
                new(OpCodes.Brtrue, cannotDigLabel))
            .Finish();

        //            if (content != null && ... && ...)
        // ldloc.s V_18
        // brfalse         --------------------------------------------------------|
        //            if (... && !(content is CItem_Wall) && ...)                  |
        // ldloc.s V_18                                                            |
        // isinst CItem_Wall                                                       |
        // brtrue          --------------------------------------------------------|
        //                                                                         |
        // |> ldloc.s V_16 (citem_Device: CItem_Device)                            |
        // |> ldloc.s V_18 (content: CItemCell)                                    |
        // |> call MiniaturizorMK6CustomLogic                                      |
        // |> brtrue       -----------------------------------------------------|  |
        //            if (... && ... && (float)content.m_hpMax > customValue)   |  |
        // ldloc.s V_18                                                         |  |
        // ldfld CItemCell::m_hpMax                                             |  |
        // conv.r4                                                              |  |
        // ldloc.s V_20                                                         |  |
        // ble.un          -----------------------------------------------------|--|
        //                 vvv Handle message cannot dig (basalt)               |  |
        // call SSingletonScreen<SScreenMessages>.get_Inst() <-------------------  |
        // ldloc.s V_18                                                            |
        // ldfld CItemCell::m_hpMax                                                |
        // ...                                                                     |
        //                 vvv Handle miniturizor mining                           |
        // ldloc.s V_18            <------------------------------------------------
        // brfalse ...
        // 
        // ldloc.s V_18
        // ldfld CItemCell::m_mainColor
        // br
        // ...
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(SScreenCrafting), nameof(SScreenCrafting.OnUpdate))]
    private static IEnumerable<CodeInstruction> SScreenCrafting_OnUpdate_ConsumableWeapon(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        // Allows to craft multiple times ExtCItem_ConsumableWeapon item (in not m_filterAdds mode) unlike CItem_Weapon

        return new CodeCursor(instructions, generator)
            .FindNextEnd(
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(SScreenCrafting).Field("m_filterAdds")),
                new(OpCodes.Brtrue)) // -> successLabel
            .GetOperand(offset: -1, out Label successLabel)
            .FindNext(
                // insert here
                new(OpCodes.Ldloc_S, 17),
                new(OpCodes.Isinst, typeof(CItem_Weapon)),
                new(OpCodes.Brtrue))
            .Insert(
                new(OpCodes.Ldloc_S, (byte)17),
                new(OpCodes.Isinst, typeof(ExtCItem_ConsumableWeapon)),
                new(OpCodes.Brtrue, successLabel))
            .Finish();

        //          if (m_filterAdds || ...)
        // ldarg.0
        // ldfld     SScreenCrafting::m_filterAdds
        // brtrue       --------------------------------------------------|
        //          if (... || item is ExtCItem_ConsumableWeapon || ...)  |
        // |> ldloc.s V_17                                                |
        // |> isinst ExtCItem_ConsumableWeapon                            |
        // |> brtrue    --------------------------------------------------|
        //          if (... || (!(item is CItem_Weapon) && ...) || ...)   |
        // ldloc.s V_17                                                   |
        // isinst CItem_Weapon                                            |
        // brtrue ...                                                     |
        // ...                                                            |
        //          if (m_filterAdds || item != GItems.autoBuilderMK1)    |
        // ldarg.0      <--------------------------------------------------
        // ldfld SScreenCrafting::m_filterAdds
        // brtrue ...
        // ldloc.s V_17
        // ldsfld GItems::autoBuilderMK1
        // bne.un ...
        // br ...
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(SScreenCrafting), nameof(SScreenCrafting.OnUpdate))]
    private static IEnumerable<CodeInstruction> SScreenCrafting_OnUpdate(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        static bool IsAutobuilderPowered(CItem_MachineAutoBuilder autobuilderItem) {
            if (autobuilderItem is not ExtCItem_ConditionalMachineAutoBuilder conditionalAutobuilder) {
                return true;
            }
            var autobuilderPos = new int2(SItems.ActivableItemPos);
            bool isPowered = SWorld.Grid[autobuilderPos.x, autobuilderPos.y].IsPowered();

            if (conditionalAutobuilder.checkCondition is null) {
                return isPowered;
            }
            return isPowered && conditionalAutobuilder.checkCondition(autobuilderPos.x, autobuilderPos.y);
        }

        return new CodeCursor(instructions, generator)
            .FindNextEnd(
                new(OpCodes.Ldsfld, typeof(SInputs).StaticField("use")),
                new(OpCodes.Callvirt, typeof(SInputs.KeyBinding).Method("IsKeyDown")),
                new(OpCodes.Brfalse) // -> skipOpenPanel
                // insert here
            )
            .GetOperand(offset: -1, out Label skipOpenPanel)
            .Insert(
                new(OpCodes.Ldloc_2),
                Transpilers.EmitDelegate(IsAutobuilderPowered),
                new(OpCodes.Brfalse, skipOpenPanel))
            .Finish();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(CBullet), nameof(CBullet.CheckColWithUnits))]
    private static IEnumerable<CodeInstruction> CBullet_CheckColWithUnits(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var codeCursor = new CodeCursor(instructions, generator);

        PatchZF0Bullet(codeCursor);
        PatchZF0Bullet(codeCursor);
        PatchZF0Bullet(codeCursor);

        return codeCursor.Finish();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(CBullet), nameof(CBullet.Update))]
    private static IEnumerable<CodeInstruction> CBullet_Update(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var codeCursor = new CodeCursor(instructions, generator);

        codeCursor
            .FindNextEnd(
                new(OpCodes.Ldarg_0),
                new(OpCodes.Call, typeof(CBullet).Method("get_Desc")),
                new(OpCodes.Ldfld, typeof(CBulletDesc).Field("m_lavaQuantity")),
                new(OpCodes.Ldc_R4, 0.0f),
                new(OpCodes.Ble_Un) // -> failLabel
                // insert here
            )
            .GetOperand(offset: -1, out Label failLabel)
            .Insert(
                new(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate(static (CBullet self) => {
                    if (self.Desc is ExtCBulletDesc cbulletdesc) {
                        return cbulletdesc.emitLavaBurstParticles;
                    }
                    return true;
                }),
                new(OpCodes.Brfalse, failLabel)
            );

        codeCursor.MoveToStart();
        PatchZF0Bullet(codeCursor);

        return codeCursor.Finish();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CBullet), nameof(CBullet.Explosion))]
    private static void CBullet_Explosion(CBullet __instance, CUnit unitHit) {
        if (__instance.Desc is not ExtCBulletDesc extDesc) { return; }

        if (extDesc.explosionBasaltBgRadius > 0) {
            ItemHelpers.ForEachInCircleClamped(extDesc.explosionBasaltBgRadius, new int2(__instance.m_pos), (int x, int y) => {
                if (SWorld.Grid[x, y].GetBgSurface() != null) {
                    SWorld.Grid[x, y].SetBgSurface(GSurfaces.bgLava);
                }
            });
        }
        if (extDesc.shockWaveRange > 0) {
            ItemHelpers.DoShockWave(__instance.m_pos, extDesc.shockWaveRange, extDesc.shockWaveDamage, extDesc.shockWaveKnockback);
        }
        if (extDesc.explosionEnergyRadius > 0f) {
            extDesc.DoEnergyExplosion(__instance);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CUnitPlayerLocal), nameof(CUnitPlayerLocal.Update))]
    private static void CUnitPlayerLocal_Update(CUnitPlayerLocal __instance) {
        var betterPotionHpRegenItem = (CItem_Device)CustomItems.betterPotionHpRegen.item;
        if (!__instance.IsAlive() || !betterPotionHpRegenItem.IsDurationItemActive(__instance.GetPlayer()) || GVars.m_simuTimeD <= (double)(__instance.m_lastTimeRegen + 1.6f)) {
            return;
        }
        __instance.m_lastTimeRegen = GVars.SimuTime;
        int hpDelta = Mathf.RoundToInt(
            Mathf.Min(__instance.GetHpMax() - __instance.m_hp, __instance.GetHpMax() * betterPotionHpRegenItem.m_customValue / (betterPotionHpRegenItem.m_duration / 1.6f))
        );
        if (hpDelta > 0) {
            SSingletonScreen<SScreenHud>.Inst.AddPopText("+" + hpDelta, __instance.Pos, SScreenHud.PopTextType.Bonus);
            __instance.m_hp += (float)hpDelta;
        }
    }

    private static void PatchZF0Bullet(CodeCursor codeCursor) {
        codeCursor
            .FindNext(
                // inject here
                new(OpCodes.Ldarg_0),
                new(OpCodes.Call, typeof(CBullet).Method("get_Desc")),
                new(OpCodes.Ldsfld, typeof(GBullets).StaticField("zf0bullet")),
                new(OpCodes.Bne_Un)
            // successLabel:
            )
            .CreateLabel(offset: 4, out Label successLabel)
            .Inject(
                new(OpCodes.Ldarg_0),
                new(OpCodes.Call, typeof(CBullet).Method("get_Desc")),
                new(OpCodes.Ldsfld, typeof(CustomBullets).StaticField("zf0shotgunBullet")),
                new(OpCodes.Beq, successLabel));
    }
}

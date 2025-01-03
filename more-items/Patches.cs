
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using System;
using UnityEngine;
using ModUtils;

public class Patches {
    [HarmonyPatch(typeof(UnityEngine.Resources), nameof(UnityEngine.Resources.Load), [typeof(string)])]
    [HarmonyPrefix]
    private static bool Resources_Load(string path, ref UnityEngine.Object __result) {
        if (path == $"Textures/{CustomCTile.texturePath}") {
            __result = CustomCTile.texture;
            return false;
        }
        return true;
    }
    [HarmonyPatch(typeof(UnityEngine.Resources), nameof(UnityEngine.Resources.LoadAll), [typeof(string), typeof(Type)])]
    [HarmonyPrefix]
    private static bool Resources_LoadAll(string path, ref UnityEngine.Object[] __result) {
        Sprite CreateSprite(string name, Rect rect) {
            var pivot = new Vector2(0.5f, 0.5f);

            var spriteRect = new Rect(rect.x, CustomCTile.texture.height - rect.yMax, rect.width, rect.height);
            var sprite = Sprite.Create(CustomCTile.texture, spriteRect, pivot, 100, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            return sprite;
        }

        if (path == $"Textures/{CustomCTile.texturePath}") {
            __result = [
                CreateSprite("meltdownSnipe", rect: new Rect(0, 128, 255, 119)),
                CreateSprite("particlesSnipTurretMK2", rect: new Rect(0, 128 + 119, 209, 98)),
            ];
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(SItems), nameof(SItems.OnInit))]
    [HarmonyPostfix]
    private static void SItems_OnInit() {
        foreach (var itemField in typeof(CustomItems).GetFields()) {
            var item = (CustomItem)itemField.GetValue(null);
            item.AddToGItems();
        }
        SLoc.ReprocessTexts();
    }
    [HarmonyPatch(typeof(CItem_Device), nameof(CItem_Device.OnUpdate))]
    [HarmonyPrefix]
    private static bool CItem_Device_OnUpdate(CItem_Device __instance) {
        // Original CItem_Device.OnUpdate uses `this == GItems.defenseShield` to check for Shield item
        if (__instance.m_groupId == "Shield") {
            CItemVars myVars = __instance.GetMyVars();
            if (GVars.m_simuTimeD > (double)(myVars.ShieldLastHitTime + 0.5f)) {
                myVars.ShieldValue = Mathf.Min(myVars.ShieldValue + SMain.SimuDeltaTime * 0.5f * __instance.m_customValue * G.m_player.GetHpMax(), __instance.m_customValue * G.m_player.GetHpMax());
            }
            return false;
        }
        return true;
    }
    [HarmonyPatch(typeof(CTile), nameof(CTile.CreateSprite))]
    [HarmonyPrefix]
    private static void CTile_CreateSprite(CTile __instance, ref string textureName) {
        if (__instance.m_textureName != null) {
            textureName = __instance.m_textureName;
        }
    }
    [HarmonyPatch(typeof(CItem), nameof(CItem.Init))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> CItem_Init(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var codeMatcher = new CodeMatcher(instructions, generator);

        codeMatcher.End()
            .MatchBack(useEnd: true,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, AccessTools.Field(typeof(CItem), nameof(CItem.m_tile))),
                new(OpCodes.Brfalse))
            .GetOperand(out Label failLabel)
            .Advance(1)
            .Insert(
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, AccessTools.Field(typeof(CItem), nameof(CItem.m_tileIcon))),
                new(OpCodes.Brtrue, failLabel));

        return codeMatcher.Instructions();
    }
    [HarmonyPatch(typeof(SDrawWorld), nameof(SDrawWorld.DrawElectricLightIFN))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> SDrawWorld_DrawElectricLightIFN(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var codeMatcher = new CodeMatcher(instructions, generator);

        codeMatcher.Start()
            .MatchForward(useEnd: false,
                new(OpCodes.Ldc_I4_0),
                new(OpCodes.Stloc_3),
                new(OpCodes.Br))
            .InjectAndAdvance(OpCodes.Ldloc_0)
            .Insert(
                new(OpCodes.Ldc_I4_S, (sbyte)5),
                new(OpCodes.Call, AccessTools.Method(typeof(Math), nameof(Math.Min), [typeof(int), typeof(int)])),
                new(OpCodes.Stloc_0));

        return codeMatcher.Instructions();
    }
    private static void PatchExplosive(CodeMatcher codeMatcher) {
        void ExplosiveLogic(CUnitDefense self) {
            if (self.GetLastFireTime() <= 0f) {
                return;
            }
            var item = (CItem_Explosive)self.m_item;

            ref var current_cell = ref SWorld.Grid[self.PosCell.x, self.PosCell.y];

            if (item.lavaReleaseTime >= 0
                && GVars.SimuTime >= self.GetLastFireTime() + item.lavaReleaseTime
                && GVars.SimuTime > CItem_Explosive.lastTimeMap[self.Id]
            ) {
                const float dt = CItem_Explosive.deltaTime;
                CItem_Explosive.lastTimeMap[self.Id] = GVars.SimuTime + dt;

                float releaseTime = GVars.SimuTime - (self.GetLastFireTime() + item.lavaReleaseTime);
                float completionPercentage = releaseTime / (item.explosionTime - item.lavaReleaseTime);

                Utils.AddLava(ref current_cell,
                    item.lavaQuantity * Mathf.Pow(3f, releaseTime)
                );
                // Console.WriteLine($"a: {item.lavaQuantity}, time: {releaseTime}, +: {item.lavaQuantity * Mathf.Pow(3f, releaseTime)}, lava: {current_cell.m_water}");

                var fireRange = Mathf.Lerp(0f, item.m_attack.m_range * 5f, completionPercentage);
                SWorld.SetFireAround(self.PosCell, fireRange);

                var evaporationRange = Mathf.Lerp(0f, item.m_attack.m_range * 4f, completionPercentage);
                Utils.ApplyInCircle(Mathf.CeilToInt(evaporationRange), self.PosCell, (int x, int y) => {
                    if (!Utils.IsValidCell(x, y)) { return; }

                    ref var cell = ref SWorld.Grid[x, y];
                    if (!cell.IsLava() && cell.m_water > 0) {
                        cell.m_water = Mathf.Max(0f, cell.m_water - SMain.SimuDeltaTime * 0.6f);
                    }
                });
            }
            if (GVars.m_simuTimeD <= (double)(self.GetLastFireTime() + item.explosionTime)) {
                return;
            }

            var attack = item.m_attack;

            Vector2 explosionPos = self.PosCell + int2.up * 0.4f;

            SSingleton<SWorld>.Inst.DestroyCell(self.PosCell, 0, false, null);
            SSingleton<SWorld>.Inst.DestroyCell(self.PosCell - int2.up, 0, false, null);

            SUnits.DoDamageAOE(explosionPos, attack.m_range, attack.m_damage);
            SWorld.DoDamageAOE(explosionPos, (int)attack.m_range, attack.m_damage);
            SParticles.common_Explosion.EmitNb(explosionPos, 100, false, 10f);
            attack.Sound.Play(explosionPos, item.explosionSoundMultiplier);

            if (item.alwaysStartEruption && (GVars.m_eruptionTime == 0f || GVars.SimuTime > GVars.m_eruptionTime + SOutgame.Params.m_eruptionDurationTotal)) {
                SAudio.Get("lavaEruption").Play(G.m_player.Pos, 1.5f);
                GVars.m_eruptionStartPressure = SGame.LavaPressure;
                GVars.m_eruptionTime = GVars.SimuTime;
            }
            if (item.destroyBackgroundRadius > 0 || item.explosionBasaltBgRadius > 0) {
                var range = item.destroyBackgroundRadius + item.explosionBasaltBgRadius;

                for (int i = self.PosCell.x - range; i <= self.PosCell.x + range; ++i) {
                    for (int j = self.PosCell.y - range; j <= self.PosCell.y + range; ++j) {
                        int2 relative = new int2(i, j) - self.PosCell;
                        if (relative.sqrMagnitude > range * range) {
                            continue;
                        }
                        if (!Utils.IsValidCell(i, j)) { return; }

                        ref var cell = ref SWorld.Grid[i, j];
                        if (relative.sqrMagnitude > item.destroyBackgroundRadius * item.destroyBackgroundRadius) {
                            if (cell.GetBgSurface() != null) {
                                cell.SetBgSurface(GSurfaces.bgLava);
                            }
                        } else {
                            cell.SetBgSurface(null);
                        }
                    }
                }
            }
            if (item.lavaReleaseTime < 0) {
                Utils.AddLava(ref current_cell, item.lavaQuantity);
            }
        }

        codeMatcher.Start()
            .MatchForward(useEnd: false,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, AccessTools.Field(typeof(CUnitDefense), "m_item")),
                new(OpCodes.Ldsfld, AccessTools.Field(typeof(GItems), nameof(GItems.explosive))),
                new(OpCodes.Bne_Un))
            .Advance(1)
            .Insert(new CodeInstruction(OpCodes.Ldarg_0))
            .CreateLabel(out var nextLabel)
            .Insert(
                new(OpCodes.Ldfld, AccessTools.Field(typeof(CUnitDefense), "m_item")),
                new(OpCodes.Isinst, typeof(CItem_Explosive)),
                new(OpCodes.Ldnull),
                new(OpCodes.Beq, nextLabel),
                new(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate(ExplosiveLogic));
    }

    private static void PatchCollector(CodeMatcher codeMatcher) {
        void CollectorLogic(CUnitDefense self, Vector2 targetPos) {
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
                SSingleton<SWorld>.Inst.DoDamageToCell(new int2(targetPos), ((CItem_Collector)self.m_item).collectorDamage, 2, true);
            }
        }
        codeMatcher.Start()
            .MatchForward(useEnd: true,
                new(OpCodes.Call, AccessTools.Method(typeof(Mathf), nameof(Mathf.MoveTowardsAngle))),
                new(OpCodes.Stfld, AccessTools.Field(typeof(CUnitDefense), "m_angleDeg")))
            .Advance(1)
            .CreateLabel(out var skipLabel)
            .Insert(
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, AccessTools.Field(typeof(CUnitDefense), "m_item")),
                new(OpCodes.Isinst, typeof(CItem_Collector)),
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
                new(OpCodes.Ldfld, AccessTools.Field(typeof(CUnitDefense), "m_item")),
                new(OpCodes.Ldsfld, AccessTools.Field(typeof(GItems), nameof(GItems.turretTesla))),
                new(OpCodes.Bne_Un))
            .CreateLabelAt(codeMatcher.Pos + 4, out var teslaCond) // after bne.un
            .Advance(1)
            .InsertAndAdvance(
                new(OpCodes.Ldfld, AccessTools.Field(typeof(CUnitDefense), "m_item")),
                new(OpCodes.Ldfld, AccessTools.Field(typeof(CItem_Defense), nameof(CItem.m_codeName))),
                new(OpCodes.Ldstr, "turretTeslaMK2"),
                new(OpCodes.Beq, teslaCond),
                new(OpCodes.Ldarg_0));
    }
    private static Vector2 GetCollectorTargetPos(CUnitDefense self) {
        int range = Mathf.FloorToInt(self.m_item.m_attack.m_range);
        float closestDist = float.MaxValue;
        Vector2 result = Vector2.zero;
        bool isBasaltCollector = ((CItem_Collector)self.m_item).isBasaltCollector;

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

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CUnitDefense), nameof(CUnitDefense.GetUnitTargetPos))]
    private static bool CUnitDefense_GetUnitTargetPos(CUnitDefense __instance, ref Vector2 __result) {
        if (__instance.m_item is CItem_Collector) {
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

        return codeMatcher.Instructions();
    }
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CUnitDefense), nameof(CUnitDefense.OnDisplayWorld))]
    private static void CUnitDefense_OnDisplayWorld(CUnitDefense __instance, float ___m_lastFireTime, Vector2 ___m_pos) {
        if (__instance.m_item is CItem_Explosive item && ___m_lastFireTime > 0f && GVars.m_simuTimeD > (double)___m_lastFireTime) {
            CMesh<CMeshText>.Get("ITEMS").Draw(
                text: Mathf.CeilToInt(___m_lastFireTime + item.explosionTime - GVars.SimuTime).ToString(),
                pos: ___m_pos + Vector2.up * 0.4f,
                size: 0.3f,
                color: item.timerColor
            );
        }
    }
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CUnitDefense), nameof(CUnitDefense.OnActivate))]
    private static void CUnitDefense_OnActivate(CUnitDefense __instance, ref float ___m_lastFireTime) {
        if (__instance.m_item is CItem_Explosive && ___m_lastFireTime < 0f) {
            ___m_lastFireTime = GVars.SimuTime;

            SSingleton<SWorld>.Inst.SetContent(
                pos: __instance.PosCell - int2.up,
                item: (CItemCell)CustomItems.indestructibleLavaOld.item
            );
            CItem_Explosive.lastTimeMap[__instance.Id] = 0f;
        }
    }
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CBullet), nameof(CBullet.Explosion))]
    private static void CBullet_Explosion(CBullet __instance) {
        if (__instance.Desc is CustomCBulletDesc cbulletdesc && cbulletdesc.explosionBasaltBgRadius > 0) {
            Utils.ApplyInCircle(cbulletdesc.explosionBasaltBgRadius, new int2(__instance.m_pos), (int x, int y) => {
                if (!Utils.IsValidCell(x, y)) { return; }

                if (SWorld.Grid[x, y].GetBgSurface() != null) {
                    SWorld.Grid[x, y].SetBgSurface(GSurfaces.bgLava);
                }
            });
        }
    }
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(CBullet), nameof(CBullet.Update))]
    private static IEnumerable<CodeInstruction> CBullet_Update(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var codeMatcher = new CodeMatcher(instructions, generator);

        codeMatcher.Start()
            .MatchForward(useEnd: true,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Call, AccessTools.PropertyGetter(typeof(CBullet), nameof(CBullet.Desc))),
                new(OpCodes.Ldfld, AccessTools.Field(typeof(CBulletDesc), nameof(CBulletDesc.m_lavaQuantity))),
                new(OpCodes.Ldc_R4, 0.0f),
                new(OpCodes.Ble_Un))
            .GetOperand(out Label failLabel)
            .Advance(1)
            .Insert(
                new(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate((CBullet self) => {
                    if (self.Desc is CustomCBulletDesc cbulletdesc) {
                        return cbulletdesc.emitLavaBurstParticles;
                    }
                    return true;
                }),
                new(OpCodes.Brfalse, failLabel)
            );

        return codeMatcher.Instructions();
    }
    [HarmonyPatch(typeof(SWorld), nameof(SWorld.DoDamageToCell))]
    [HarmonyPrefix]
    private static bool SWorld_DoDamageToCell(ref int2 cellPos, ref bool __result) {
        var content = SWorld.Grid[cellPos.x, cellPos.y].GetContent();
        if ((content is CItem_Explosive citem && citem.indestructible) || content is CItem_IndestructibleMineral) {
            __result = false;
            return false;
        }
        return true;
    }
    [HarmonyPatch(typeof(CUnit), nameof(CUnit.Damage_Local))]
    [HarmonyPrefix]
    private static bool CUnit_Damage_Local(CUnit __instance) {
        var content = SWorld.Grid[__instance.PosCell.x, __instance.PosCell.y].GetContent();
        if (content is CItem_Explosive citem && citem.indestructible) {
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(CInventory), "InventorySorting")]
    [HarmonyPrefix]
    private static bool CInventory_InventorySorting(CStack a, CStack b, ref int __result) {
        static int categoryToOrdinal(string categoryId) {
            if (!categoryId.StartsWith("CITEM_")) { return 7; }
            return categoryId.Substring(6) switch {
                "DEVICE" => 0,
                "WEAPON" => 1,
                "DEFENSE" => 2,
                "WALL" => 3,
                "MACHINE" => 4,
                "MINERAL" => 5,
                "MATERIAL" => 6,
                _ => 7,
            };
        }
        const ushort lastItemId = 201;

        var a_item = a.m_item;
        var b_item = b.m_item;
        if (a_item.m_id > lastItemId || b_item.m_id > lastItemId) {
            if (a_item.m_categoryId == b_item.m_categoryId) {
                __result = a_item.m_id - b_item.m_id;
            } else {
                __result = categoryToOrdinal(a_item.m_categoryId) - categoryToOrdinal(b_item.m_categoryId);
            }
            return false;
        }
        return true;
    }
}

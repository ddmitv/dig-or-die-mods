
using HarmonyLib;
using ModUtils;
using ModUtils.Extensions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

public class Patches {
    [HarmonyPatch(typeof(CTile), nameof(CTile.CreateSprite))]
    [HarmonyPrefix]
    private static void CTile_CreateSprite(CTile __instance, ref string textureName) {
        if (__instance.m_textureName != null) {
            textureName = __instance.m_textureName;
        }
    }
    [HarmonyPatch(typeof(CSurface), nameof(CSurface.InitSprites))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> CSurface_InitSprites(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        static string ReplaceTextureStr(string origStr, CSurface self) {
            if (self is ModCSurface.TaggedCSurface) {
                return $"{ModCSurface.surfacePath}/{ModCSurface.surfaceTopsPath}";
            } else {
                return origStr;
            }
        }
        return new CodeMatcher(instructions, generator)
            .Start()
            .MatchForward(useEnd: false,
                new CodeMatch(OpCodes.Ldstr, "surfaces/_surface_tops"))
            .ThrowIfInvalid("(1)")
            .Advance(1)
            .Insert(
                new(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate(ReplaceTextureStr))
            .Instructions();
    }

    [HarmonyPatch(typeof(UnityEngine.Resources), nameof(UnityEngine.Resources.Load), [typeof(string)])]
    [HarmonyPrefix]
    private static bool Resources_Load(string path, ref UnityEngine.Object __result) {
        if (!path.StartsWith("Textures/")) { return true; }
        string prefixlessPath = path.Substring("Textures/".Length);

        if (prefixlessPath == ModCTile.texturePath) {
            __result = ModCTile.texture;
            return false;
        }
        if (prefixlessPath == $"{ModCSurface.surfacePath}/{ModCSurface.fertileDirtTexturePath}") {
            __result = ModCSurface.fertileDirtTexture;
            return false;
        }
        if (prefixlessPath == $"{ModCSurface.surfacePath}/{ModCSurface.surfaceTopsPath}") {
            __result = ModCSurface.surfaceTops;
            return false;
        }
        return true;
    }
    [HarmonyPatch(typeof(UnityEngine.Resources), nameof(UnityEngine.Resources.LoadAll), [typeof(string), typeof(Type)])]
    [HarmonyPrefix]
    private static bool Resources_LoadAll(string path, ref UnityEngine.Object[] __result) {
        static Sprite CreateSprite(string name, Rect rect) {
            var pivot = new Vector2(0.5f, 0.5f);

            var spriteRect = new Rect(rect.x, CustomBullets.particlesTexture.height - rect.yMax, rect.width, rect.height);
            var sprite = Sprite.Create(CustomBullets.particlesTexture, spriteRect, pivot, 100, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            return sprite;
        }

        if (path == $"Textures/{CustomBullets.particlesPath}") {
            __result = [
                CreateSprite("meltdownSnipe", rect: new Rect(0, 0, 255, 119)),
                CreateSprite("particlesSnipTurretMK2", rect: new Rect(255, 0, 209, 98)),
            ];
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(SItems), nameof(SItems.OnInit))]
    [HarmonyPostfix]
    private static void SItems_OnInit() {
        foreach (var itemField in typeof(CustomItems).GetFields(BindingFlags.Static | BindingFlags.Public)) {
            var modItem = (ModItem)itemField.GetValue(null);
            modItem.Item.m_tileTextureName = ModCTile.texturePath;
            ItemTools.RegisterItem(modItem.Item);
        }
        SLoc.ReprocessTexts();
    }
    [HarmonyPatch(typeof(SDataLua), nameof(SDataLua.OnInit))]
    [HarmonyPostfix]
    private static void SDataLua_OnInit() {
        // SOutgame.Mode is "Solo"

        foreach (var recipeGroupField in typeof(CustomRecipeGroups).GetFields(BindingFlags.Static | BindingFlags.Public)) {
            var recipeGroup = (ModRecipeGroup)recipeGroupField.GetValue(null);
            ItemTools.RegisterRecipeGroup(recipeGroup.GroupId, recipeGroup.Autobuilders);
        }
        foreach (var itemField in typeof(CustomItems).GetFields(BindingFlags.Static | BindingFlags.Public)) {
            var modItem = (ModItem)itemField.GetValue(null);
            if (modItem.Recipe is null) { continue; }
            ItemTools.RegisterRecipe(modItem.Recipe);
        }
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
    [HarmonyPatch(typeof(CItem), nameof(CItem.Init))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> CItem_Init(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var codeMatcher = new CodeMatcher(instructions, generator);

        codeMatcher.End()
            .MatchBack(useEnd: true,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(CItem).Field("m_tile")),
                new(OpCodes.Brfalse))
            .GetOperand(out Label failLabel)
            .Advance(1)
            .Insert(
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(CItem).Field("m_tileIcon")),
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
                new(OpCodes.Call, typeof(Math).Method("Min", [typeof(int), typeof(int)])),
                new(OpCodes.Stloc_0));

        return codeMatcher.Instructions();
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
                SSingleton<SWorld>.Inst.DoDamageToCell(new int2(targetPos), ((ExtCItem_Collector)self.m_item).collectorDamage, 2, true);
            }
        }
        codeMatcher.Start()
            .MatchForward(useEnd: true,
                new(OpCodes.Call, typeof(Mathf).Method("MoveTowardsAngle")),
                new(OpCodes.Stfld, typeof(CUnitDefense).Field("m_angleDeg")))
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
            .CreateLabelAt(codeMatcher.Pos + 4, out var teslaCond) // after bne.un
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

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CUnitDefense), nameof(CUnitDefense.GetUnitTargetPos))]
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

        return codeMatcher.Instructions();
    }
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CUnitDefense), nameof(CUnitDefense.OnDisplayWorld))]
    private static void CUnitDefense_OnDisplayWorld(CUnitDefense __instance, float ___m_lastFireTime, Vector2 ___m_pos) {
        if (__instance.m_item is ExtCItem_Explosive item && ___m_lastFireTime > 0f && GVars.m_simuTimeD > (double)___m_lastFireTime) {
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
        if (__instance.m_item is ExtCItem_Explosive expItem && ___m_lastFireTime < 0f) {
            ___m_lastFireTime = GVars.SimuTime;

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
    [HarmonyPatch(typeof(CBullet), nameof(CBullet.Update))]
    private static IEnumerable<CodeInstruction> CBullet_Update(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var codeMatcher = new CodeMatcher(instructions, generator);

        codeMatcher.Start()
            .MatchForward(useEnd: true,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Call, typeof(CBullet).Method("get_Desc")),
                new(OpCodes.Ldfld, typeof(CBulletDesc).Field("m_lavaQuantity")),
                new(OpCodes.Ldc_R4, 0.0f),
                new(OpCodes.Ble_Un))
            .ThrowIfInvalid("(1)")
            .GetOperand(out Label failLabel)
            .Advance(1)
            .Insert(
                new(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate((CBullet self) => {
                    if (self.Desc is ExtCBulletDesc cbulletdesc) {
                        return cbulletdesc.emitLavaBurstParticles;
                    }
                    return true;
                }),
                new(OpCodes.Brfalse, failLabel)
            );

        PatchZF0Bullet(codeMatcher.Start(), "(2)");

        return codeMatcher.Instructions();
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
    [HarmonyPatch(typeof(CUnit), nameof(CUnit.Damage_Local))]
    [HarmonyPrefix]
    private static bool CUnit_Damage_Local(CUnit __instance) {
        var content = SWorld.Grid[__instance.PosCell.x, __instance.PosCell.y].GetContent();
        if (content is ExtCItem_Explosive citem && citem.indestructible) {
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

    [HarmonyPatch(typeof(CUnit), nameof(CUnit.Update))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> UnclampUnitSpeed(IEnumerable<CodeInstruction> instructions) {
        var codeMatcher = new CodeMatcher(instructions);

        codeMatcher.Start()
            .MatchForward(useEnd: false,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldflda, typeof(CUnit).Field("m_speed")),
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldflda, typeof(CUnit).Field("m_speed")),
                new(OpCodes.Ldfld, typeof(Vector2).Field("x")),
                new(OpCodes.Ldc_R4, -30f),
                new(OpCodes.Ldc_R4, 30f),
                new(OpCodes.Call, typeof(Mathf).Method("Clamp", [typeof(float), typeof(float), typeof(float)])),
                new(OpCodes.Stfld, typeof(Vector2).Field("x")))
            .ThrowIfInvalid("(1)")
            .RemoveInstructions(18);

        return codeMatcher.Instructions();
    }

    private static void PatchZF0Bullet(CodeMatcher codeMatcher, string explanation) {
        codeMatcher
            .MatchForward(useEnd: true,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Call, typeof(CBullet).Method("get_Desc")),
                new(OpCodes.Ldsfld, typeof(GBullets).StaticField("zf0bullet")),
                new(OpCodes.Bne_Un))
            .ThrowIfInvalid(explanation)
            .Advance(1)
            .CreateLabel(out Label successLabel)
            .Advance(-4)
            .InjectAndAdvance(OpCodes.Ldarg_0)
            .InsertAndAdvance(
                new(OpCodes.Call, typeof(CBullet).Method("get_Desc")),
                new(OpCodes.Ldsfld, typeof(CustomBullets).StaticField("zf0shotgunBullet")),
                new(OpCodes.Beq, successLabel))
            .Advance(4);
    }

    [HarmonyPatch(typeof(CBullet), nameof(CBullet.CheckColWithUnits))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> CBullet_CheckColWithUnits(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var codeMatcher = new CodeMatcher(instructions, generator);

        PatchZF0Bullet(codeMatcher, "(1)");
        PatchZF0Bullet(codeMatcher, "(2)");
        PatchZF0Bullet(codeMatcher, "(3)");

        return codeMatcher.Instructions();
    }

    [HarmonyPatch(typeof(CItem_Weapon), nameof(CItem_Weapon.Use_Local))]
    [HarmonyPostfix]
    private static void CItem_Weapon_Use_Local_Postfix(CItem_Weapon __instance, CPlayer player, Vector2 worldPos, bool isShift) {
        if (__instance == CustomItems.gunZF0Shotgun.Item
            && SWorld.GridRectCam.Contains(worldPos)
            && isShift) {
            CItemVars vars = GItems.gunZF0.GetVars(player);
            vars.ZF0TargetLastTimeHit = float.MinValue;
            vars.ZF0TargetId = ushort.MaxValue;
        }
    }

    [HarmonyPatch(typeof(SItems), nameof(SItems.OnUpdateSimu))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> SItems_OnUpdateSimu_MiniaturizorMK6(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        static bool MiniaturizorMK6CustomLogic(CItem_Device cItem_Device, CItemCell content) {
            if (!ReferenceEquals(cItem_Device, CustomItems.miniaturizorMK6.Item)) { return false; }
            
            return content.m_hpMax > GItems.miniaturizorMK5.m_customValue;
        }

        var codeMatcher = new CodeMatcher(instructions, generator);
        codeMatcher.Start()
            // if (content != null && !(content is CItem_Wall) && (float)content.m_hpMax > customValue)
            .MatchForward(useEnd: true,
                new(OpCodes.Ldloc_S),
                new(OpCodes.Brfalse),

                new(OpCodes.Ldloc_S),
                new(OpCodes.Isinst, typeof(CItem_Wall)),
                new(OpCodes.Brtrue),
                // (1)
                new(OpCodes.Ldloc_S),
                new(OpCodes.Ldfld, typeof(CItemCell).Field("m_hpMax")),
                new(OpCodes.Conv_R4),
                new(OpCodes.Ldloc_S),
                new(OpCodes.Ble_Un))
            .ThrowIfInvalid("(1)")
            .Advance(1)
            .CreateLabel(out Label cannotDigLabel)
            .Advance(-5) // Put cursor at (1)
            .Insert(
                new(OpCodes.Ldloc_S, (byte)16),
                new(OpCodes.Ldloc_S, (byte)18),
                Transpilers.EmitDelegate(MiniaturizorMK6CustomLogic),
                new(OpCodes.Brtrue, cannotDigLabel));
        return codeMatcher.Instructions();

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

    [HarmonyPatch(typeof(CItem_Device), nameof(CItem_Device.Use_Local))]
    [HarmonyPostfix]
    private static void CItem_Device_Use_Local(CItem_Device __instance, CPlayer player, Vector2 mousePos, bool isShift) {
        if (__instance == CustomItems.portableTeleport.Item) {
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
    [HarmonyPatch(typeof(SItems), nameof(SItems.OnUpdateSimu))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> SItems_OnUpdateSimu_PortableTeleport(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var codeMatcher = new CodeMatcher(instructions, generator);
        codeMatcher.Start()
            .MatchForward(useEnd: true,
                new(OpCodes.Call, typeof(SSingleton<SWorld>).Method("get_Inst")),
                new(OpCodes.Ldloc_S),
                new(OpCodes.Callvirt, typeof(SWorld).Method("GetCellContent", [typeof(int2)])),
                new(OpCodes.Isinst),
                new(OpCodes.Brtrue))
            .ThrowIfInvalid("(1)")
            .GetOperand(out Label skipLabel)
            .Advance(1)
            .Insert(
                Transpilers.EmitDelegate(() => {
                    return G.m_player.PosCell == CItem_MachineTeleport.m_teleportersPos[0];
                }),
                new(OpCodes.Brtrue, skipLabel));
        return codeMatcher.Instructions();
    }
    [HarmonyPatch(typeof(CItem_MachineTeleport), nameof(CItem_MachineTeleport.Teleport))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> CItem_MachineTeleport_Teleport_PortableTeleport(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var codeMatcher = new CodeMatcher(instructions, generator);

        codeMatcher.Start()
            .MatchForward(useEnd: false,
                new(OpCodes.Ldsfld, typeof(G).StaticField("m_player")),
                new(OpCodes.Ldloc_1),
                new(OpCodes.Call, typeof(int2).Method("op_Implicit", [typeof(int2)])))
            .ThrowIfInvalid("(1)")
            .CreateLabel(out Label teleportLabel)
            .Start()
            .MatchForward(useEnd: false,
                new(OpCodes.Call, typeof(SWorld).Method("get_Grid")),
                new(OpCodes.Ldloca_S),
                new(OpCodes.Ldfld, typeof(int2).Field("x")),
                new(OpCodes.Ldloca_S),
                new(OpCodes.Ldfld, typeof(int2).Field("y")),
                new(OpCodes.Call, typeof(CCell[,]).Method("Address", [typeof(int), typeof(int)])),

                new(OpCodes.Call, typeof(CCell).Method("GetContent")),
                new(OpCodes.Ldsfld, typeof(GItems).StaticField("teleport")),
                new(OpCodes.Bne_Un))
            .ThrowIfInvalid("(2)")
            .Insert(
                Transpilers.EmitDelegate(() => {
                    return G.m_player.PosCell == CItem_MachineTeleport.m_teleportersPos[0];
                }),
                new(OpCodes.Brtrue, teleportLabel));

        return codeMatcher.Instructions();
    }
    [HarmonyPatch(typeof(SWorldDll), nameof(SWorldDll.ProcessSimu))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> SWorldDll_ProcessSimu(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        static float ComputePlantGrowChange(CItem_Mineral mineral) {
            if (mineral is ExtCItem_FertileMineralDirt fertileDirt) {
                return fertileDirt.plantGrowChange;
            }
            return 0.15f;
        }
        var codeMatcher = new CodeMatcher(instructions, generator);
        codeMatcher.Start()
            .MatchForward(useEnd: false,
                new(OpCodes.Call, typeof(CCell).Method("GetContent")),
                new(OpCodes.Brtrue),
                new(OpCodes.Call, typeof(UnityEngine.Random).Method("get_value")),
                new(OpCodes.Ldc_R4, 0.15f),
                new(OpCodes.Bge_Un))
            .ThrowIfInvalid("(1)")
            .Advance(3)
            .RemoveInstruction()
            .Insert(
                new(OpCodes.Ldloc_S, (byte)8),
                Transpilers.EmitDelegate(ComputePlantGrowChange));
        return codeMatcher.Instructions();
    }
    [HarmonyPatch(typeof(SScreenCrafting), nameof(SScreenCrafting.OnUpdate))]
    [HarmonyTranspiler]
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

        return new CodeMatcher(instructions, generator)
            .Start()
            .MatchForward(useEnd: true,
                new(OpCodes.Ldsfld, typeof(SInputs).StaticField("use")),
                new(OpCodes.Callvirt, typeof(SInputs.KeyBinding).Method("IsKeyDown")),
                new(OpCodes.Brfalse))
            .ThrowIfInvalid("(1)")
            .GetOperand(out Label skipOpenPanel)
            .Advance(1)
            .Insert(
                new(OpCodes.Ldloc_2),
                Transpilers.EmitDelegate(IsAutobuilderPowered),
                new(OpCodes.Brfalse, skipOpenPanel))
            .Instructions();
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


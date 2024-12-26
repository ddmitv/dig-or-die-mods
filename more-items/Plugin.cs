using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace more_items;

public static class CodeMatcherExtensions {
    public static CodeMatcher Inject(this CodeMatcher self, OpCode opcode, object operand = null) {
        var prevInstruction = self.Instruction.Clone();
        self.SetAndAdvance(opcode, operand);
        self.Insert(prevInstruction);
        return self;
    }
    public static CodeMatcher GetOperand<T>(this CodeMatcher self, out T result) {
        result = (T)self.Operand;
        return self;
    }
}

public class CustomCTile : CTile {
    public static string texturePath = "mod-more-items";
    public static Texture2D texture = null;

    public CustomCTile(int i, int j, int images = 1, int sizeX = 128, int sizeY = 128)
        : base(i, j, images, sizeX, sizeY) {
        base.m_textureName = texturePath;
        base.m_textureSize = 1500;
    }
}

public class CustomItem {
    public CustomItem(string name, CItem item) {
        item.m_codeName = name;
        item.m_tileTextureName = CustomCTile.texturePath;
        item.m_locTextId = $"I_{name}";
        this.item = item;
    }

    static private CItem_PluginData MakeItemsPluginData(CItem item) {
        // Copied from SItems_OnInit
        CItem_PluginData itemsPluginData = default(CItem_PluginData);
        CItemCell citemCell = item as CItemCell;
        if (citemCell == null) { return itemsPluginData; }

        var conditions_field = typeof(CItem_Plant).GetField("m_conditions", BindingFlags.NonPublic | BindingFlags.Instance);

        itemsPluginData.m_weight = ((!(citemCell is CItem_Wall)) ? 0f : (citemCell as CItem_Wall).m_weight);
        itemsPluginData.m_electricValue = citemCell.m_electricValue;
        itemsPluginData.m_electricOutletFlags = citemCell.m_electricityOutletFlags;
        itemsPluginData.m_elecSwitchType = ((citemCell != GItems.elecCross) ? ((citemCell != GItems.elecSwitchRelay) ? ((citemCell != GItems.elecSwitch) ? ((citemCell != GItems.elecSwitchPush) ? 0 : 4) : 3) : 2) : 1);
        itemsPluginData.m_elecVariablePower = ((!citemCell.m_electricVariablePower) ? 0 : 1);
        itemsPluginData.m_anchor = (int)citemCell.m_anchor;
        itemsPluginData.m_light = citemCell.m_light;
        itemsPluginData.m_isBlock = ((!citemCell.IsBlock()) ? 0 : 1);
        itemsPluginData.m_isBlockDoor = ((!citemCell.IsBlockDoor()) ? 0 : 1);
        itemsPluginData.m_isReceivingForces = ((!citemCell.IsReceivingForces()) ? 0 : 1);
        itemsPluginData.m_isMineral = ((!(citemCell is CItem_Mineral)) ? 0 : 1);
        itemsPluginData.m_isDirt = ((!(citemCell is CItem_MineralDirt)) ? 0 : 1);
        itemsPluginData.m_isPlant = ((!(citemCell is CItem_Plant)) ? 0 : 1);
        itemsPluginData.m_isFireProof = ((!citemCell.m_fireProof && (!(citemCell is CItem_Plant) || !((CLifeConditions)conditions_field.GetValue(citemCell as CItem_Plant)).m_isFireProof)) ? 0 : 1);
        itemsPluginData.m_isWaterGenerator = ((citemCell != GItems.generatorWater) ? 0 : 1);
        itemsPluginData.m_isWaterPump = ((citemCell != GItems.waterPump) ? 0 : 1);
        itemsPluginData.m_isLightGenerator = ((citemCell != GItems.generatorSun) ? 0 : 1);
        itemsPluginData.m_isBasalt = ((citemCell != GItems.lava) ? 0 : 1);
        itemsPluginData.m_isLightonium = ((citemCell != GItems.lightonium) ? 0 : 1);
        itemsPluginData.m_isOrganicHeart = ((citemCell != GItems.organicRockHeart) ? 0 : 1);
        itemsPluginData.m_isSunLamp = ((citemCell != GItems.lightSun) ? 0 : 1);
        itemsPluginData.m_isAutobuilder = ((!(citemCell is CItem_MachineAutoBuilder)) ? 0 : 1);
        itemsPluginData.m_customValue = ((!(citemCell is CItem_Machine)) ? 0f : (citemCell as CItem_Machine).m_customValue);
        return itemsPluginData;
    }

    public void AddToItems(List<CItem> items) {
        item.m_id = (ushort)items.Count;
        items.Add(item);

        item.Init();

        var SItems_inst = Utils.SSingleton_Inst<SItems>();
        var itemsPluginData_field = typeof(SItems).GetField("m_itemsPluginData", BindingFlags.NonPublic | BindingFlags.Instance);

        var itemsPluginData = (CItem_PluginData[])itemsPluginData_field.GetValue(SItems_inst);
        Array.Resize(ref itemsPluginData, itemsPluginData.Length + 1);
        itemsPluginData[itemsPluginData.Length - 1] = MakeItemsPluginData(item);
        itemsPluginData_field.SetValue(SItems_inst, itemsPluginData);
    }

    private CItem item;
}

public static class Utils {
    public static void ApplyInCircle(int range, int2 pos, Action<int, int> fn) {
        int sqrRange = range * range;
        for (int i = pos.x - range; i <= pos.x + range; ++i) {
            for (int j = pos.y - range; j <= pos.y + range; ++j) {
                int2 relative = new int2(i, j) - pos;
                if (relative.sqrMagnitude <= sqrRange) {
                    fn(i, j);
                }
            }
        }
    }
    public static T SSingleton_Inst<T>() where T : class, new() {
        var inst = typeof(SSingleton<T>).GetProperty("Inst", BindingFlags.NonPublic | BindingFlags.Static);
        return (T)inst.GetValue(null, []);
    }
    public static bool IsValidCell(int x, int y) {
        return x >= 0 && y >= 0 && x < SWorld.Gs.x && y < SWorld.Gs.y;
    }
    public static void AddLava(ref CCell cell, float lavaQuantity) {
        if (!cell.IsPassable()) { return; }

        if (!cell.IsLava()) {
            cell.m_water = 0;
        }
        cell.m_water += lavaQuantity;
        cell.SetFlag(CCell.Flag_IsLava, true);
    }
}

[HarmonyPatch(typeof(CUnitDefense))]
public class CUnitDefense_Patches {
    private static void PatchExplosive(CodeMatcher codeMatcher) {
        void ExplosiveLogic(CUnitDefense self) {
            var item = (CItem_Explosive)self.m_item;
            if (self.GetLastFireTime() <= 0f) {
                return;
            }
            ref var current_cell = ref SWorld.Grid[self.PosCell.x, self.PosCell.y];

            current_cell.m_contentHP = current_cell.GetContent().m_hpMax;

            if (item.lavaReleaseTime >= 0
                && GVars.SimuTime >= self.GetLastFireTime() + item.lavaReleaseTime
                && GVars.SimuTime > CItem_Explosive.lastTimeMap[self.Id]
            ) {
                const float dt = CItem_Explosive.deltaTime;
                CItem_Explosive.lastTimeMap[self.Id] = GVars.SimuTime + dt;
                
                float releaseTime = GVars.SimuTime - (self.GetLastFireTime() + item.lavaReleaseTime);
                float completionPercentage = releaseTime / (item.explosionTime - item.lavaReleaseTime);

                Utils.AddLava(ref current_cell,
                    // 1/2f * dt * item.lavaQuantity * (Mathf.Pow(3f, releaseTime + dt) + Mathf.Pow(3f, releaseTime))
                    item.lavaQuantity * Mathf.Pow(3f, releaseTime)
                );
                Console.WriteLine($"a: {item.lavaQuantity}, time: {releaseTime}, +: {item.lavaQuantity * Mathf.Pow(3f, releaseTime)}, lava: {current_cell.m_water}");

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
            Utils.SSingleton_Inst<SWorld>().DestroyCell(self.PosCell, 0, false, null);
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
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(CUnitDefense), "m_item")),
                new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(GItems), nameof(GItems.explosive))),
                new CodeMatch(OpCodes.Bne_Un))
            .Advance(1)
            .Insert(new CodeInstruction(OpCodes.Ldarg_0))
            .CreateLabel(out var nextLabel)
            .Insert(
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CUnitDefense), "m_item")),
                new CodeInstruction(OpCodes.Isinst, typeof(CItem_Explosive)),
                new CodeInstruction(OpCodes.Ldnull),
                new CodeInstruction(OpCodes.Beq, nextLabel),
                new CodeInstruction(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate(ExplosiveLogic));
    }

    private static void PatchCollector(CodeMatcher codeMatcher) {
        void CollectorLogic(CUnitDefense self, Vector2 targetPos) {
            ref var timeRepaired = ref AccessTools.FieldRefAccess<CUnitDefense, float>(self, "m_timeRepaired");

            int particlesCount = (int)(GVars.m_simuTimeD * 15.0) - (int)((GVars.m_simuTimeD - SMain.SimuDeltaTimeD) * 15.0);
            Utils.SSingleton_Inst<SParticles>().EmitMultiple(
                count: particlesCount,
                origin: new Rect(targetPos.x - 0.3f, targetPos.y - 0.3f, 0.6f, 0.6f),
                speed: 10f,
                color: self.m_item.m_mainColor,
                type: SParticles.Type.Reparator,
                paramVector: new Rect(self.PosFire.x, self.PosFire.y, 0f, 0f)
            );

            timeRepaired += SMain.SimuDeltaTime;
            if (timeRepaired > self.m_item.m_attack.m_cooldown) {

                timeRepaired -= self.m_item.m_attack.m_cooldown;
                Utils.SSingleton_Inst<SWorld>().DoDamageToCell(new int2(targetPos), ((CItem_Collector)self.m_item).collectorDamage, 2, true);
            }
        }
        codeMatcher.Start()
            .MatchForward(useEnd: true,
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Mathf), nameof(Mathf.MoveTowardsAngle))),
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(CUnitDefense), "m_angleDeg")))
            .Advance(1)
            .CreateLabel(out var skipLabel)
            .Insert(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CUnitDefense), "m_item")),
                new CodeInstruction(OpCodes.Isinst, typeof(CItem_Collector)),
                new CodeInstruction(OpCodes.Ldnull),
                new CodeInstruction(OpCodes.Beq, skipLabel),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldloc_S, (byte)4),
                Transpilers.EmitDelegate(CollectorLogic),
                new CodeInstruction(OpCodes.Ldc_I4_1), // flag = true
                new CodeInstruction(OpCodes.Stloc_2));
    }
    private static void PatchTeslaTurretMK2(CodeMatcher codeMatcher) {
        codeMatcher.Start()
            .MatchForward(useEnd: false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(CUnitDefense), "m_item")),
                new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(GItems), nameof(GItems.turretTesla))),
                new CodeMatch(OpCodes.Bne_Un))
            .CreateLabelAt(codeMatcher.Pos + 4, out var teslaCond) // after bne.un
            .Advance(1)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CUnitDefense), "m_item")),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CItem_Defense), nameof(CItem.m_codeName))),
                new CodeInstruction(OpCodes.Ldstr, "turretTeslaMK2"),
                new CodeInstruction(OpCodes.Beq, teslaCond),
                new CodeInstruction(OpCodes.Ldarg_0));
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
    [HarmonyPatch("GetUnitTargetPos")]
    private static bool CUnitDefense_GetUnitTargetPos(CUnitDefense __instance, ref Vector2 __result) {
        if (__instance.m_item is CItem_Collector) {
            __result = GetCollectorTargetPos(__instance);

            return false;
        }
        return true;
    }

    [HarmonyTranspiler]
    [HarmonyPatch("Update")]
    private static IEnumerable<CodeInstruction> CUnitDefense_Update(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var codeMatcher = new CodeMatcher(instructions, generator);

        PatchTeslaTurretMK2(codeMatcher);
        PatchExplosive(codeMatcher);
        PatchCollector(codeMatcher);

        return codeMatcher.Instructions();
    }
    [HarmonyPostfix]
    [HarmonyPatch("OnDisplayWorld")]
    private static void CUnitDefense_OnDisplayWorld(CUnitDefense __instance, float ___m_lastFireTime, Vector2 ___m_pos) {
        if (__instance.m_item is CItem_Explosive item && ___m_lastFireTime > 0f && GVars.m_simuTimeD > (double)___m_lastFireTime) {
            CMesh<CMeshText>.Get("ITEMS").Draw(
                text: Mathf.CeilToInt(___m_lastFireTime + item.explosionTime - GVars.SimuTime).ToString(),
                pos: ___m_pos + Vector2.up * 0.4f,
                size: 0.3f,
                color: Color.red * 0.4f
            );
        }
    }
    [HarmonyPostfix]
    [HarmonyPatch("OnActivate")]
    private static void CUnitDefense_OnActivate(CUnitDefense __instance, ref float ___m_lastFireTime) {
        if (__instance.m_item is CItem_Explosive && ___m_lastFireTime < 0f) {
            ___m_lastFireTime = GVars.SimuTime;

            Utils.SSingleton_Inst<SWorld>().SetContent(
                pos: __instance.PosCell - int2.up,
                item: GItems.lavaOld
            );
            CItem_Explosive.lastTimeMap[__instance.Id] = 0f;
        }
    }
    [HarmonyPatch(typeof(CBullet), "Explosion")]
    [HarmonyPostfix]
    private static void CBullet_Explosion(CBullet __instance) {
        if (__instance.Desc == MoreItemsPlugin.meltdownSnipe) {
            var range = ((CustomCBulletDesc)MoreItemsPlugin.meltdownSnipe).explosionBasaltBgRadius;

            Utils.ApplyInCircle(range, new int2(__instance.m_pos), (int x, int y) => {
                if (!Utils.IsValidCell(x, y)) { return; }

                if (SWorld.Grid[x, y].GetBgSurface() != null) {
                    SWorld.Grid[x, y].SetBgSurface(GSurfaces.bgLava);
                }
            });
        }
    }
    [HarmonyPatch(typeof(CBullet), nameof(CBullet.Update))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> CBullet_Update(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var codeMatcher = new CodeMatcher(instructions, generator);

        codeMatcher.Start()
            .MatchForward(useEnd: true,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(CBullet), nameof(CBullet.Desc))),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(CBulletDesc), nameof(CBulletDesc.m_lavaQuantity))),
                new CodeMatch(OpCodes.Ldc_R4, 0.0f),
                new CodeMatch(OpCodes.Ble_Un))
            .GetOperand(out Label failLabel)
            .Advance(1)
            .Insert(
                new CodeInstruction(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate((CBullet self) => {
                    if (self.Desc is CustomCBulletDesc cbulletdesc) {
                        return cbulletdesc.emitLavaBurstParticles;
                    }
                    return true;
                }),
                new CodeInstruction(OpCodes.Brfalse, failLabel)
            );

        return codeMatcher.Instructions();
    }
}

public class CItem_Collector : CItem_Defense {
    public CItem_Collector(CTile tile, CTile tileIcon, ushort hpMax, uint mainColor, float rangeDetection, float angleMin, float angleMax, CAttackDesc attack, CTile tileUnit)
        : base(tile, tileIcon, hpMax, mainColor, rangeDetection, angleMin, angleMax, attack, tileUnit) {
        m_attack.m_damage = 0;
    }

    public ushort collectorDamage = 0;
    public bool isBasaltCollector = false;
}
public class CItem_Explosive : CItem_Defense {
    public CItem_Explosive(CTile tile, CTile tileIcon, ushort hpMax, uint mainColor, float rangeDetection, float angleMin, float angleMax, CAttackDesc attack, CTile tileUnit)
        : base(tile, tileIcon, hpMax, mainColor, rangeDetection, angleMin, angleMax, attack, tileUnit) {}

    public const float deltaTime = 0.1f;

    public static float CalculateLavaQuantityStep(float totalQuantity, float time) {
        var t = Mathf.Pow(3, deltaTime);
        return totalQuantity * (1 - t) / (1 - Mathf.Pow(t, time / deltaTime + 1));
    }

    public float explosionTime = 5f;
    public float explosionSoundMultiplier = 1f;
    public bool alwaysStartEruption = false;
    public int destroyBackgroundRadius = 0;
    public int explosionBasaltBgRadius = 0;
    public float lavaQuantity = 0;
    public float lavaReleaseTime = -1f;

    public static Dictionary<ushort, float> lastTimeMap = new Dictionary<ushort, float>();
}
public class CustomCBulletDesc : CBulletDesc {
    public CustomCBulletDesc(string spriteTextureName, string spriteName, float radius, float dispersionAngleRad, float speedStart, float speedEnd, uint light = 0)
        : base(spriteTextureName, spriteName, radius, dispersionAngleRad, speedStart, speedEnd, light) {}

    public int explosionBasaltBgRadius = 0;
    public bool emitLavaBurstParticles = true;
}

[BepInPlugin("more-items", "More Items", "0.0.0")]
public class MoreItemsPlugin : BaseUnityPlugin {
    public static CustomItem[] customItems = null;
    public static CBulletDesc meltdownSnipe = null;

    private void Awake() {
        ThreadingHelper.Instance.StartSyncInvoke(() => {
            CustomCTile.texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            CustomCTile.texture.LoadImage(ModResources.Textures);
            CustomCTile.texture.filterMode = FilterMode.Trilinear;
            CustomCTile.texture.wrapMode = TextureWrapMode.Clamp;

            Harmony.CreateAndPatchAll(typeof(CUnitDefense_Patches));
        });
        Harmony.CreateAndPatchAll(typeof(MoreItemsPlugin));

        customItems = [
            new CustomItem(name: "flashLightMK3",
                item: new CItem_Device(tile: new CustomCTile(0, 0), tileIcon: new CustomCTile(0, 0),
                    groupId: "FlashLight", type: CItem_Device.Type.Passive, customValue: 10f
                )
            ),
            new CustomItem(name: "miniaturizorMK6",
                item: new CItem_Device(tile: new CustomCTile(2, 0), tileIcon: new CustomCTile(1, 0),
                    groupId: "Miniaturizor", type: CItem_Device.Type.None, customValue: 999f
                    // Above 999 the miniaturizor would break Ancient Basalt (oldLava)
                ){ m_pickupDuration = -1 }
            ),
            new CustomItem(name: "betterPotionHpRegen",
                item: new CItem_Device(tile: new CustomCTile(3, 0), tileIcon: new CustomCTile(3, 0),
                    groupId: "potionHpRegen", type: CItem_Device.Type.Consumable, customValue: 3f
                    // "potionHpRegen" has 1.5f customValue
                ){ m_cooldown = 120f, m_duration = 60f }
            ),
            new CustomItem(name: "defenseShieldMK2",
                item: new CItem_Device(tile: new CustomCTile(4, 0), tileIcon: new CustomCTile(4, 0),
                    groupId: "Shield", type: CItem_Device.Type.Passive, customValue: 1f
                    // "defenseShield" has 0.5f customValue
                )
            ),
            new CustomItem(name: "waterBreatherMK2",
                item: new CItem_Device(tile: new CustomCTile(5, 0), tileIcon: new CustomCTile(5, 0),
                    groupId: "WaterBreather", type: CItem_Device.Type.Passive, customValue: 7f
                    // "waterBreather" has 3f customValue
                )
            ),
            new CustomItem(name: "jetpackMK2",
                item: new CItem_Device(tile: new CustomCTile(6, 0), tileIcon: new CustomCTile(6, 0),
                    groupId: "Jetpack", type: CItem_Device.Type.Passive, customValue: 1f
                )
            ),
            new CustomItem(name: "antiGravityWall",
                item: new CItem_Wall(tile: new CustomCTile(7, 0), tileIcon: new CustomCTile(7, 0),
                    hpMax: 100, mainColor: 12173251U, forceResist: int.MaxValue - 10000, weight: 1000f, type: CItem_Wall.Type.WallBlock
                )
            ),
            new CustomItem(name: "turretReparatorMK3",
                item: new CItem_Defense(tile: new CustomCTile(10, 0), tileIcon: new CustomCTile(9, 0),
                    hpMax: 200, mainColor: 8947848U, rangeDetection: 8.5f,
                    angleMin: -9999f, angleMax: 9999f,
                    attack: new CAttackDesc(
                        range: 7.5f,
                        damage: -10,
                        nbAttacks: 0,
                        cooldown: 0.5f,
                        knockbackOwn: 0f, knockbackTarget: 0f,
                        projDesc: null, sound: null
                    ),
                    tileUnit: new CustomCTile(8, 0)
                ) {
                    m_displayRangeOnCells = true,
                    m_electricValue = -2,
                    m_light = new Color24(10329710U),
                    m_neverUnspawn = true
                }
            ),
            new CustomItem(name: "megaExplosive",
                item: new CItem_Explosive(tile: new CustomCTile(11, 0), tileIcon: new CustomCTile(11, 0),
                    hpMax: 250, mainColor: 8947848U, rangeDetection: 0f, angleMin: 0f, angleMax: 360f,
                    attack: new CAttackDesc(
                        range: 10f,
                        damage: 3000,
                        nbAttacks: 0,
                        cooldown: -1f,
                        knockbackOwn: 0f,
                        knockbackTarget: 10f,
                        projDesc: null,
                        sound: "rocketExplosion"
                    ),
                    tileUnit: null
                ) {
                    m_isActivable = true,
                    m_neverUnspawn = true,
                    explosionTime = 6f,
                    explosionSoundMultiplier = 5f,
                    destroyBackgroundRadius = 2,
                    explosionBasaltBgRadius = 5,
                    m_light = new Color24(10, 240, 71)
                }
            ),
            new CustomItem(name: "turretParticlesMK2",
                item: new CItem_Defense(tile: new CTile(0, 0) { m_textureName = "items_defenses" }, tileIcon: new CustomCTile(12, 0),
                    hpMax: 350, mainColor: 8947848U, rangeDetection: 10f,
                    angleMin: -9999f, angleMax: 9999f,
                    attack: new CAttackDesc(
                        range: 12f,
                        damage: 50,
                        nbAttacks: 1,
                        cooldown: 0.5f,
                        knockbackOwn: 0f, knockbackTarget: 3f,
                        projDesc: GBullets.particlesSnipTurret,
                        sound: "particleTurret"
                    ),
                    tileUnit: new CustomCTile(13, 0)
                ) {
                    m_anchor = CItemCell.Anchor.Everyside_Small
                }
            ),
            new CustomItem(name: "turretTeslaMK2",
                item: new CItem_Defense(tile: new CustomCTile(14, 0), tileIcon: new CustomCTile(14, 0),
                    hpMax: 350, mainColor: 8947848U, rangeDetection: 12.5f,
                    angleMin: -9999f, angleMax: 9999f,
                    attack: new CAttackDesc(
                        range: 12f,
                        damage: 200,
                        nbAttacks: 1,
                        cooldown: 2f,
                        knockbackOwn: 0f, knockbackTarget: 10f,
                        projDesc: null,
                        sound: "storm"
                    ),
                    tileUnit: null
                ) {
                    m_electricValue = -5,
                    m_light = new Color24(16, 133, 235)
                }
            ),
            new CustomItem(name: "collector",
                item: new CItem_Collector(tile: new CustomCTile(15, 0), tileIcon: new CustomCTile(16, 0),
                    hpMax: 100, mainColor: 8947848U, rangeDetection: 5f,
                    angleMin: -9999f, angleMax: 9999f,
                    attack: new CAttackDesc(
                        range: 5.5f,
                        damage: 0,
                        nbAttacks: 0,
                        cooldown: 0.5f,
                        knockbackOwn: 0f, knockbackTarget: 0f,
                        projDesc: null, sound: null
                    ),
                    tileUnit: new CustomCTile(17, 0)
                ) {
                    m_anchor = CItemCell.Anchor.Everyside_Small,
                    m_displayRangeOnCells = true,
                    m_neverUnspawn = true,
                    collectorDamage = 10,
                    m_electricValue = -2
                }
            ),
            new CustomItem(name: "blueLightSticky",
                item: new CItem_Machine(tile: new CustomCTile(18, 0), tileIcon: new CustomCTile(18, 0),
                    hpMax: 100, mainColor: 10066329U, anchor: CItemCell.Anchor.Everywhere_Small
                ) {
                    m_light = new Color24(20, 20, 220)
                }
            ),
            new CustomItem(name: "redLightSticky",
                item: new CItem_Machine(tile: new CustomCTile(20, 0), tileIcon: new CustomCTile(20, 0),
                    hpMax: 100, mainColor: 10066329U, anchor: CItemCell.Anchor.Everywhere_Small
                ) {
                    m_light = new Color24(220, 20, 20)
                }
            ),
            new CustomItem(name: "greenLightSticky",
                item: new CItem_Machine(tile: new CustomCTile(22, 0), tileIcon: new CustomCTile(22, 0),
                    hpMax: 100, mainColor: 10066329U, anchor: CItemCell.Anchor.Everywhere_Small
                ) {
                    m_light = new Color24(20, 220, 20)
                }
            ),
            new CustomItem(name: "basaltCollector",
                item: new CItem_Collector(tile: new CustomCTile(15, 0), tileIcon: new CustomCTile(24, 0),
                    hpMax: 100, mainColor: 8947848U, rangeDetection: 5f,
                    angleMin: -9999f, angleMax: 9999f,
                    attack: new CAttackDesc(
                        range: 5.5f,
                        damage: 0,
                        nbAttacks: 0,
                        cooldown: 0.5f,
                        knockbackOwn: 0f, knockbackTarget: 0f,
                        projDesc: null, sound: null
                    ),
                    tileUnit: new CustomCTile(25, 0)
                ) {
                    m_anchor = CItemCell.Anchor.Everyside_Small,
                    m_displayRangeOnCells = true,
                    m_neverUnspawn = true,
                    collectorDamage = 100,
                    isBasaltCollector = true,
                    m_electricValue = -5
                }
            ),
            new CustomItem(name: "turretLaser360",
                item: new CItem_Defense(tile: new CTile(0, 0) { m_textureName = "items_defenses" }, tileIcon: new CustomCTile(26, 0),
                    hpMax: 250, mainColor: 8947848U, rangeDetection: 10f,
                    angleMin: -9999f, angleMax: 9999f,
                    attack: new CAttackDesc(
                        range: 10f,
                        damage: 20,
                        nbAttacks: 1,
                        cooldown: 0.3f,
                        knockbackOwn: 0f, knockbackTarget: 0f,
                        projDesc: GBullets.laser, sound: "laser"
                    ),
                    tileUnit: new CTile(2, 2) { m_textureName = "items_defenses" }
                )
            ),
            new CustomItem(name: "gunMeltdown",
                item: new CItem_Weapon(tile: new CustomCTile(27, 0), tileIcon: new CustomCTile(28, 0),
                    heatingPerShot: 2f, isAuto: false,
                    attackDesc: new CAttackDesc(
                        range: 50f,
                        damage: 1500,
                        nbAttacks: 1,
                        cooldown: 3f,
                        knockbackOwn: 60f,
                        knockbackTarget: 100f,
                        projDesc: (meltdownSnipe = new CustomCBulletDesc(
                            CustomCTile.texturePath, "meltdownSnipe",
                            radius: 0.7f, dispersionAngleRad: 0.1f,
                            speedStart: 50f, speedEnd: 30f, light: 0xC0A57u
                        ) {
                            m_lavaQuantity = 40f,
                            m_explosionRadius = 6f,
                            m_hasSmoke = true,
                            m_explosionSetFire = true,
                            m_light = new Color24(240, 40, 40),
                            explosionBasaltBgRadius = 4,
                            emitLavaBurstParticles = false,
                        }),
                        sound: "plasmaSnipe"
                    )
                )
            ),
            new CustomItem(name: "volcanicExplosive",
                item: new CItem_Explosive(tile: new CustomCTile(29, 0), tileIcon: new CustomCTile(29, 0),
                    hpMax: 500, mainColor: 8947848U, rangeDetection: 0f, angleMin: 0f, angleMax: 360f,
                    attack: new CAttackDesc(
                        range: 25f,
                        damage: 5000,
                        nbAttacks: 0,
                        cooldown: -1f,
                        knockbackOwn: 0f,
                        knockbackTarget: 500f,
                        projDesc: null,
                        sound: "rocketExplosion"
                    ),
                    tileUnit: null
                ) {
                    m_isActivable = true,
                    m_neverUnspawn = true,
                    explosionTime = 10f,
                    explosionSoundMultiplier = 30f,
                    alwaysStartEruption = true,
                    destroyBackgroundRadius = 3,
                    explosionBasaltBgRadius = 18,
                    lavaQuantity = CItem_Explosive.CalculateLavaQuantityStep(totalQuantity: 1500f, time: 5f),
                    lavaReleaseTime = 5f,
                    m_light = new Color24(240, 38, 38),
                    m_fireProof = true,
                }
            ),
            new CustomItem(name: "wallCompositeReinforced",
                item: new CItem_Wall(tile: new CustomCTile(30, 0), tileIcon: new CustomCTile(30, 0),
                    hpMax: 700, mainColor: 12039872U, forceResist: 11000, weight: 560f,
                    type: CItem_Wall.Type.WallBlock
                )
            ),
            new CustomItem(name: "gunNukeLauncher",
                item: new CItem_Weapon(tile: new CustomCTile(31, 0), tileIcon: new CustomCTile(32, 0),
                    heatingPerShot: 0f, isAuto: false,
                    attackDesc: new CAttackDesc(
                        range: 100f,
                        damage: 1000,
                        nbAttacks: 1,
                        cooldown: 0f,
                        knockbackOwn: 100f,
                        knockbackTarget: 200f,
                        projDesc: new CustomCBulletDesc(
                            "particles/particles", "grenade",
                            radius: 0.5f,
                            dispersionAngleRad: 0f,
                            speedStart: 20f,
                            speedEnd: 15f,
                            light: 0x005E19
                        ) {
                            m_grenadeYSpeed = -15f,
                            m_explosionRadius = 15f,
                            m_lavaQuantity = 1f,
                            emitLavaBurstParticles = false,
                        },
                        sound: "rocketFire"
                    )
                )
            ),
            new CustomItem(name: "generatorSunMK2",
                item: new CItem_Machine(tile: new CustomCTile(33, 0), tileIcon: new CustomCTile(33, 0),
                    hpMax: 200, mainColor: 10066329U,
                    anchor: CItemCell.Anchor.Bottom_Small
                ) {
                    m_electricValue = 3
                }
            ),
            new CustomItem(name: "RTG",
                item: new CItem_Machine(tile: new CustomCTile(34, 0), tileIcon: new CustomCTile(34, 0),
                    hpMax: 200, mainColor: 10066329U,
                    anchor: CItemCell.Anchor.Bottom_Small
                ) {
                    m_light = new Color24(0xED0CE9),
                    m_electricValue = 15
                }
            ),
            new CustomItem(name: "gunPlasmaThrower",
                item: new CItem_Weapon(tile: new CustomCTile(35, 0), tileIcon: new CustomCTile(36, 0),
                    heatingPerShot: 0f, isAuto: true,
                    attackDesc: new CAttackDesc(
                        range: 16f,
                        damage: 20,
                        nbAttacks: 1,
                        cooldown: 0.1f,
                        knockbackOwn: 0f,
                        knockbackTarget: 1f,
                        projDesc: new CustomCBulletDesc(
                            CustomCTile.texturePath, "particlePlasmaCloud",
                            radius: 0.5f, dispersionAngleRad: 0.1f,
                            speedStart: 25f, speedEnd: 15f, light: 0x770BDB
                        ) {
                            m_goThroughEnnemies = true,
                            m_pierceArmor = true,
                            m_inflame = true,
                        },
                        sound: null
                    )
                )
            )
        ];

        System.Console.WriteLine("Plugin more-items loaded!");
    }

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
            var particlePlasmaCloud = (Sprite)AccessTools.Method(typeof(Sprite), "MemberwiseClone").Invoke(GBullets.flamethrower.m_sprite.Sprite, []);
            particlePlasmaCloud.name = "particlePlasmaCloud";

            __result = [
                CreateSprite("meltdownSnipe", rect: new Rect(0, 128, 255, 119)),
                // CreateSprite("particlePlasmaCloud", rect: new Rect(0, 247, 256, 256))
                particlePlasmaCloud
            ];
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(SItems), "OnInit")]
    [HarmonyPostfix]
    private static void SItems_OnInit() {
        foreach (var item in customItems) {
            item.AddToItems(GItems.Items);
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
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(CItem), nameof(CItem.m_tile))),
                new CodeMatch(OpCodes.Brfalse))
            .GetOperand(out Label failLabel)
            .Advance(1)
            .Insert(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CItem), nameof(CItem.m_tileIcon))),
                new CodeInstruction(OpCodes.Brtrue, failLabel));

        return codeMatcher.Instructions();
    }
    [HarmonyPatch(typeof(SDrawWorld), "DrawElectricLightIFN")]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> SDrawWorld_DrawElectricLightIFN(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var codeMatcher = new CodeMatcher(instructions, generator);

        codeMatcher.Start()
            .MatchForward(useEnd: false,
                new CodeMatch(OpCodes.Ldc_I4_0),
                new CodeMatch(OpCodes.Stloc_3),
                new CodeMatch(OpCodes.Br))
            .Inject(OpCodes.Ldloc_0)
            .Insert(
                new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)5),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Math), nameof(Math.Min), [typeof(int), typeof(int)])),
                new CodeInstruction(OpCodes.Stloc_0));

        return codeMatcher.Instructions();
    }
}

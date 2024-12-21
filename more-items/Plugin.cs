using BepInEx;
using HarmonyLib;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.VR;

namespace more_items;

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

        // Hack?
        item.m_tile.CreateSprite(item.m_tile.m_textureName);
        item.m_tileIcon.CreateSprite(item.m_tileIcon.m_textureName);
        if (item is CItemCell) {
            if (((CItemCell)item).m_electricValue != 0 && ((CItemCell)item).m_electricityOutletFlags == 0) {
                ((CItemCell)item).m_electricityOutletFlags = 1;
            }
        }

        var SItems_inst = (SItems)(typeof(SSingleton<SItems>).GetProperty("Inst", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null, []));
        var itemsPluginData_field = typeof(SItems).GetField("m_itemsPluginData", BindingFlags.NonPublic | BindingFlags.Instance);

        var itemsPluginData = (CItem_PluginData[])itemsPluginData_field.GetValue(SItems_inst);
        Array.Resize(ref itemsPluginData, itemsPluginData.Length + 1);
        itemsPluginData[itemsPluginData.Length - 1] = MakeItemsPluginData(item);
        itemsPluginData_field.SetValue(SItems_inst, itemsPluginData);
    }

    private CItem item;
}

[HarmonyPatch(typeof(CUnitDefense))]
public class CUnitDefense_Patches {
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Method Declaration", "Harmony003:Harmony non-ref patch parameters modified", Justification = "<Pending>")]
    private static void ApplyInCircle(int range, int2 pos, Action<int2> fn) {
        int sqrRange = range * range;
        for (int i = pos.x - range; i <= pos.x + range; ++i) {
            for (int j = pos.y - range; j <= pos.y + range; ++j) {
                int2 relative = new int2(i, j) - pos;
                if (relative.sqrMagnitude <= sqrRange) {
                    fn(new int2(i, j));
                }
            }
        }
    }

    private static void PatchExplosive(CodeMatcher codeMatcher) {
        void ExplosiveLogic(CUnitDefense self) {
            var item = (CItem_Explosive)self.m_item;
            if (!(self.GetLastFireTime() > 0f
                && GVars.m_simuTimeD > (double)(self.GetLastFireTime() + item.explosionTimer))) { return; }

            var attack = item.m_attack;

            Vector2 explosionPos = self.PosCell + int2.up * 0.4f;
            SUnits.DoDamageAOE(explosionPos, attack.m_range, attack.m_damage);
            SWorld.DoDamageAOE(explosionPos, (int)attack.m_range, attack.m_damage);
            SParticles.common_Explosion.EmitNb(explosionPos, 100, false, 10f);
            attack.Sound.Play(explosionPos, item.explosionSoundMultiplier);

            if (item.alwaysStartEruption && (GVars.m_eruptionTime == 0f || GVars.SimuTime > GVars.m_eruptionTime + SOutgame.Params.m_eruptionDurationTotal)) {
                SAudio.Get("lavaEruption").Play(G.m_player.Pos, 1.5f);
                GVars.m_eruptionStartPressure = SGame.LavaPressure;
                GVars.m_eruptionTime = GVars.SimuTime;
            }
            if (item.destroyBackgroundRadius > 0) {
                ApplyInCircle(item.destroyBackgroundRadius, self.PosCell, (int2 pos) => {
                    SWorld.Grid[pos.x, pos.y].SetBgSurface(null);
                });
            }
            if (item.explosionLavaQuantity > 0) {
                ref var cell = ref SWorld.Grid[self.PosCell.x, self.PosCell.y];
                cell.m_water = item.explosionLavaQuantity;
                cell.SetFlag(CCell.Flag_IsLava, true);
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
            var SWorld_inst = (SWorld)(typeof(SSingleton<SWorld>).GetProperty("Inst", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null, []));
            ref var timeRepaired = ref AccessTools.FieldRefAccess<CUnitDefense, float>(self, "m_timeRepaired");

            timeRepaired += SMain.SimuDeltaTime;
            if (timeRepaired > self.m_item.m_attack.m_cooldown) {

                timeRepaired -= self.m_item.m_attack.m_cooldown;
                SWorld_inst.DoDamageToCell(new int2(targetPos), ((CItem_Collector)self.m_item).collectorDamage, 2, true);
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
        int rangeDetection = Mathf.FloorToInt(self.m_item.m_attack.m_range);
        float closestDist = float.MaxValue;
        Vector2 result = Vector2.zero;
        bool isBasaltCollector = ((CItem_Collector)self.m_item).isBasaltCollector;

        for (int i = self.PosCell.x - rangeDetection; i <= self.PosCell.x + rangeDetection; ++i) {
            for (int j = self.PosCell.y - rangeDetection; j <= self.PosCell.y + rangeDetection; ++j) {
                if (i == self.PosCell.x && j == self.PosCell.y) { continue; }

                CItemCell content = SWorld.Grid[i, j].GetContent();
                int2 relative = new int2(i, j) - self.PosCell;

                if ((relative.sqrMagnitude <= rangeDetection * rangeDetection)
                    && (isBasaltCollector ? ReferenceEquals(content, GItems.lava) : content is CItem_Plant)
                    && (relative.sqrMagnitude < closestDist)) {
                    closestDist = relative.sqrMagnitude;
                    result = new Vector2(i + 0.5f, j + 0.5f);
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
                text: Mathf.CeilToInt(___m_lastFireTime + item.explosionTimer - GVars.SimuTime).ToString(),
                pos: ___m_pos + Vector2.up * 0.4f,
                size: 0.3f,
                color: Color.red
            );
        }
    }
    [HarmonyPostfix]
    [HarmonyPatch("OnActivate")]
    private static void CUnitDefense_OnActivate(CUnitDefense __instance, ref float ___m_lastFireTime) {
        if (__instance.m_item is CItem_Explosive && ___m_lastFireTime < 0f) {
            ___m_lastFireTime = GVars.SimuTime;
        }
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

    public float explosionTimer = 5f;
    public float explosionSoundMultiplier = 1f;
    public bool alwaysStartEruption = false;
    public int destroyBackgroundRadius = 0;
    public float explosionLavaQuantity = 0;
}

[BepInPlugin("more-items", "More Items", "0.0.0")]
public class MoreItemsPlugin : BaseUnityPlugin {
    public static CustomItem[] customItems = null;

    private void Awake() {
        ThreadingHelper.Instance.StartSyncInvoke(() => {
            CustomCTile.texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            CustomCTile.texture.filterMode = FilterMode.Trilinear;
            CustomCTile.texture.LoadImage(ModResources.Textures);

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
                    explosionTimer = 6f,
                    explosionSoundMultiplier = 5f,
                    destroyBackgroundRadius = 3
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
                    m_light = new Color24(9724047U)
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
                        projDesc: new CBulletDesc(
                            CustomCTile.texturePath, "meltdownSnipe",
                            radius: 0.5f, dispersionAngleRad: 0.1f,
                            speedStart: 40f, speedEnd: 30f, light: 0xC0A57u
                        ) {
                            m_lavaQuantity = 40f,
                            m_explosionRadius = 7f,
                            m_hasTrail = true,
                            m_hasSmoke = true,
                            m_explosionSetFire = true,
                        },
                        sound: "plasmaSnipe"
                    )
                )
            ),
            new CustomItem(name: "volcanicExplosive",
                item: new CItem_Explosive(tile: new CustomCTile(29, 0), tileIcon: new CustomCTile(29, 0),
                    hpMax: 500, mainColor: 8947848U, rangeDetection: 0f, angleMin: 0f, angleMax: 360f,
                    attack: new CAttackDesc(
                        range: 20f,
                        damage: 5000,
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
                    explosionTimer = 10f,
                    explosionSoundMultiplier = 30f,
                    alwaysStartEruption = true,
                    destroyBackgroundRadius = 9,
                    explosionLavaQuantity = 900f
                }
            ),
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
        Sprite CreateSprite(string name, Rect rect, Vector2 pivot) {
            var relPivot = new Vector2(pivot.x / rect.width, pivot.y / rect.height);
            var sprite = Sprite.Create(CustomCTile.texture, rect, relPivot, 100, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            return sprite;
        }

        if (path == $"Textures/{CustomCTile.texturePath}") {
            __result = [
                CreateSprite("meltdownSnipe", rect: new Rect(0, 0, 255, 119), pivot: new Vector2(178.6f, 59.4f))
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
}

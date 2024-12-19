using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using UnityEngine;

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
public class SyncPatches {
    private static void PatchExplosive(CodeMatcher codeMatcher) {
        Label explosiveCond;
        codeMatcher.Start()
            .MatchForward(useEnd: false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(CUnitDefense), "m_item")),
                new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(GItems), nameof(GItems.explosive))),
                new CodeMatch(OpCodes.Bne_Un))
            .CreateLabelAt(codeMatcher.Pos + 4, out explosiveCond)
            .Advance(1)
            .InsertAndAdvance(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CUnitDefense), "m_item")),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CItem_Defense), nameof(CItem.m_codeName))),
                new CodeInstruction(OpCodes.Ldstr, "megaExplosive"),
                new CodeInstruction(OpCodes.Beq, explosiveCond),
                new CodeInstruction(OpCodes.Ldarg_0));
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(CUnitDefense), "Update")]
    private static IEnumerable<CodeInstruction> CUnitDefense_Update(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var codeMatcher = new CodeMatcher(instructions, generator);
        Label teslaCond;

        codeMatcher
            .MatchForward(useEnd: false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(CUnitDefense), "m_item")),
                new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(GItems), nameof(GItems.turretTesla))),
                new CodeMatch(OpCodes.Bne_Un))
            .CreateLabelAt(codeMatcher.Pos + 4, out teslaCond) // after bne.un
            .Advance(1)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CUnitDefense), "m_item")),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CItem_Defense), nameof(CItem.m_codeName))),
                new CodeInstruction(OpCodes.Ldstr, "turretTeslaMK2"),
                new CodeInstruction(OpCodes.Beq, teslaCond),
                new CodeInstruction(OpCodes.Ldarg_0));
        PatchExplosive(codeMatcher);

        return codeMatcher.Instructions();
    }
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(CUnitDefense), "OnActivate")]
    [HarmonyPatch(typeof(CUnitDefense), "OnDisplayWorld")]
    private static IEnumerable<CodeInstruction> CUnitDefense_OnActivate(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var codeMatcher = new CodeMatcher(instructions, generator);
        PatchExplosive(codeMatcher);
        return codeMatcher.Instructions();
    }
}

[BepInPlugin("more-items", "More Items", "0.0.0")]
public class MoreItemsPlugin : BaseUnityPlugin {
    public static CustomItem[] customItems = null;

    private void Awake() {
        ThreadingHelper.Instance.StartSyncInvoke(() => {
            CustomCTile.texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            CustomCTile.texture.filterMode = FilterMode.Trilinear;
            CustomCTile.texture.LoadImage(ModResources.Textures);

            Harmony.CreateAndPatchAll(typeof(SyncPatches));
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
                item: new CItem_Defense(tile: new CustomCTile(11, 0), tileIcon: new CustomCTile(11, 0),
                    hpMax: 250, mainColor: 8947848U, rangeDetection: 0f, angleMin: 0f, angleMax: 360f,
                    attack: new CAttackDesc(
                        range: 15f,
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
                    m_neverUnspawn = true
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
    // [HarmonyPatch(typeof(CUnitDefense), "Update")]
    // [HarmonyPostfix]
    // private static void CUnitDefense_Update() {
    //     Console.WriteLine("inside CUnitDefense_Update");
    // }
}

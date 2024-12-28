using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
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

    public void AddToGItems() {
        item.m_id = (ushort)GItems.Items.Count;
        GItems.Items.Add(item);

        item.Init();

        var SItems_inst = Utils.SSingleton_Inst<SItems>();
        var itemsPluginData_field = typeof(SItems).GetField("m_itemsPluginData", BindingFlags.NonPublic | BindingFlags.Instance);

        var itemsPluginData = (CItem_PluginData[])itemsPluginData_field.GetValue(SItems_inst);
        Array.Resize(ref itemsPluginData, itemsPluginData.Length + 1);
        itemsPluginData[itemsPluginData.Length - 1] = MakeItemsPluginData(item);
        itemsPluginData_field.SetValue(SItems_inst, itemsPluginData);
    }

    public CItem item{ get; private set; }
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
    public static T MakeMemberwiseClone<T>(T obj) where T : class {
        return (T)AccessTools.Method(typeof(T), "MemberwiseClone").Invoke(obj, []);
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
    public bool indestructible = false;
    public Color timerColor = Color.red;

    public static Dictionary<ushort, float> lastTimeMap = new Dictionary<ushort, float>();
}
public class CustomCBulletDesc : CBulletDesc {
    public CustomCBulletDesc(string spriteTextureName, string spriteName, float radius, float dispersionAngleRad, float speedStart, float speedEnd, uint light = 0)
        : base(spriteTextureName, spriteName, radius, dispersionAngleRad, speedStart, speedEnd, light) {}

    public int explosionBasaltBgRadius = 0;
    public bool emitLavaBurstParticles = true;
}
public class CItem_IndestructibleMineral : CItem_Mineral {
    public CItem_IndestructibleMineral(CTile tile, CTile tileIcon, ushort hpMax, uint mainColor, CSurface surface, bool isReplacable = false)
        : base(tile, tileIcon, hpMax, mainColor, surface, isReplacable) {}
}

[BepInPlugin("more-items", "More Items", "0.0.0")]
public class MoreItemsPlugin : BaseUnityPlugin {
    private void Start() {
        CustomCTile.texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
        CustomCTile.texture.LoadImage(ModResources.Textures);
        CustomCTile.texture.filterMode = FilterMode.Trilinear;
        CustomCTile.texture.wrapMode = TextureWrapMode.Clamp;

        Harmony.CreateAndPatchAll(typeof(Patches));

        RuntimeHelpers.RunClassConstructor(typeof(CustomItems).TypeHandle);
    }
}

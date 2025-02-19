using BepInEx;
using HarmonyLib;
using ModUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

public class CustomCTile : CTile {
    public static string texturePath = "mod-more-items";
    public static Texture2D texture = null;

    public CustomCTile(int i, int j, int images = 1, int sizeX = 128, int sizeY = 128)
        : base(i, j, images, sizeX, sizeY) {
        base.m_textureName = texturePath;
    }
}

public class CustomItem {
    public CustomItem(string name, CItem item) {
        item.m_codeName = name;
        item.m_tileTextureName = CustomCTile.texturePath;
        item.m_locTextId = $"I_{name}";
        this.item = item;
    }

    static private CItem_PluginData MakeItemPluginData(CItem item) {
        // Copied from SItems.OnInit
        CItem_PluginData itemPluginData = default;
        CItemCell citemCell = item as CItemCell;
        if (citemCell == null) { return itemPluginData; }

        itemPluginData.m_weight = ((!(citemCell is CItem_Wall)) ? 0f : (citemCell as CItem_Wall).m_weight);
        itemPluginData.m_electricValue = citemCell.m_electricValue;
        itemPluginData.m_electricOutletFlags = citemCell.m_electricityOutletFlags;
        itemPluginData.m_elecSwitchType = ((citemCell != GItems.elecCross) ? ((citemCell != GItems.elecSwitchRelay) ? ((citemCell != GItems.elecSwitch) ? ((citemCell != GItems.elecSwitchPush) ? 0 : 4) : 3) : 2) : 1);
        itemPluginData.m_elecVariablePower = ((!citemCell.m_electricVariablePower) ? 0 : 1);
        itemPluginData.m_anchor = (int)citemCell.m_anchor;
        itemPluginData.m_light = citemCell.m_light;
        itemPluginData.m_isBlock = ((!citemCell.IsBlock()) ? 0 : 1);
        itemPluginData.m_isBlockDoor = ((!citemCell.IsBlockDoor()) ? 0 : 1);
        itemPluginData.m_isReceivingForces = ((!citemCell.IsReceivingForces()) ? 0 : 1);
        itemPluginData.m_isMineral = ((!(citemCell is CItem_Mineral)) ? 0 : 1);
        itemPluginData.m_isDirt = ((!(citemCell is CItem_MineralDirt)) ? 0 : 1);
        itemPluginData.m_isPlant = ((!(citemCell is CItem_Plant)) ? 0 : 1);
        itemPluginData.m_isFireProof = ((!citemCell.m_fireProof && (!(citemCell is CItem_Plant) || !(citemCell as CItem_Plant).m_conditions.m_isFireProof)) ? 0 : 1);
        itemPluginData.m_isWaterGenerator = ((citemCell != GItems.generatorWater) ? 0 : 1);
        itemPluginData.m_isWaterPump = ((citemCell != GItems.waterPump) ? 0 : 1);
        itemPluginData.m_isLightGenerator = ((citemCell != GItems.generatorSun) ? 0 : 1);
        itemPluginData.m_isBasalt = ((citemCell != GItems.lava) ? 0 : 1);
        itemPluginData.m_isLightonium = ((citemCell != GItems.lightonium) ? 0 : 1);
        itemPluginData.m_isOrganicHeart = ((citemCell != GItems.organicRockHeart) ? 0 : 1);
        itemPluginData.m_isSunLamp = ((citemCell != GItems.lightSun) ? 0 : 1);
        itemPluginData.m_isAutobuilder = ((!(citemCell is CItem_MachineAutoBuilder)) ? 0 : 1);
        itemPluginData.m_customValue = ((!(citemCell is CItem_Machine)) ? 0f : (citemCell as CItem_Machine).m_customValue);
        return itemPluginData;
    }

    public void AddToGItems() {
        item.m_id = (ushort)GItems.Items.Count;
        GItems.Items.Add(item);

        item.Init();

        ref var itemsPluginData = ref SSingleton<SItems>.Inst.m_itemsPluginData;
        Utils.ArrayAppend(ref itemsPluginData, MakeItemPluginData(item));
    }

    public CItem item{ get; private set; }
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
    public float shockWaveRange = 0f;
    public float shockWaveKnockback = 0f;
    public float shockWaveDamage = 0f;

    public static Dictionary<ushort, float> lastTimeMap = new Dictionary<ushort, float>();
}
public class CustomCBulletDesc : CBulletDesc {
    public CustomCBulletDesc(string spriteTextureName, string spriteName, float radius, float dispersionAngleRad, float speedStart, float speedEnd, uint light = 0)
        : base(spriteTextureName, spriteName, radius, dispersionAngleRad, speedStart, speedEnd, light) {}

    public int explosionBasaltBgRadius = 0;
    public bool emitLavaBurstParticles = true;
    public float shockWaveRange = 0f;
    public float shockWaveKnockback = 0f;
    public float shockWaveDamage = 0f;
}
public class CItem_IndestructibleMineral : CItem_Mineral {
    public CItem_IndestructibleMineral(CTile tile, CTile tileIcon, ushort hpMax, uint mainColor, CSurface surface, bool isReplacable = false)
        : base(tile, tileIcon, hpMax, mainColor, surface, isReplacable) {}
}

[BepInPlugin("more-items", "More Items", "0.0.0")]
public class MoreItemsPlugin : BaseUnityPlugin {
    private void Start() {
        Utils.UniqualizeVersionBuild(ref G.m_versionBuild, this);

        using var textureStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("more-items.textures.combined_textures.png");

        CustomCTile.texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
        CustomCTile.texture.LoadImage(Utils.ReadAllBytes(textureStream));
        CustomCTile.texture.filterMode = FilterMode.Trilinear;
        CustomCTile.texture.wrapMode = TextureWrapMode.Clamp;

        Harmony.CreateAndPatchAll(typeof(Patches));

        Utils.RunStaticConstructor(typeof(CustomItems));
    }
}

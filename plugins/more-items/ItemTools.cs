
using ModUtils;
using System.Runtime.Serialization;
using System.Collections.Generic;

public static class ItemTools {
    public static void AddItemNameDesc(string id, string name, string description) {
        SSingleton<SLoc>.Inst.m_dico.Add(id, new SLoc.CSentence(id, name + '|' + description));
    }
    public static CItem_PluginData MakeItemPluginData(CItem item) {
        // Copied from SItems.OnInit
        CItem_PluginData itemPluginData = default;
        CItemCell citemCell = item as CItemCell;
        if (citemCell == null) return itemPluginData;

        itemPluginData.m_weight = citemCell is not CItem_Wall ? 0f : (citemCell as CItem_Wall).m_weight;
        itemPluginData.m_electricValue = citemCell.m_electricValue;
        itemPluginData.m_electricOutletFlags = citemCell.m_electricityOutletFlags;
        itemPluginData.m_elecSwitchType = citemCell != GItems.elecCross ? citemCell != GItems.elecSwitchRelay ? citemCell != GItems.elecSwitch ? citemCell != GItems.elecSwitchPush ? 0 : 4 : 3 : 2 : 1;
        itemPluginData.m_elecVariablePower = citemCell.m_electricVariablePower ? 1 : 0;
        itemPluginData.m_anchor = (int)citemCell.m_anchor;
        itemPluginData.m_light = citemCell.m_light;
        itemPluginData.m_isBlock = citemCell.IsBlock() ? 1 : 0;
        itemPluginData.m_isBlockDoor = citemCell.IsBlockDoor() ? 1 : 0;
        itemPluginData.m_isReceivingForces = citemCell.IsReceivingForces() ? 1 : 0;
        itemPluginData.m_isMineral = citemCell is CItem_Mineral ? 1 : 0;
        itemPluginData.m_isDirt = citemCell is CItem_MineralDirt ? 1 : 0;
        itemPluginData.m_isPlant = citemCell is CItem_Plant ? 1 : 0;
        itemPluginData.m_isFireProof = citemCell.m_fireProof || citemCell is CItem_Plant && (citemCell as CItem_Plant).m_conditions.m_isFireProof ? 1 : 0;
        itemPluginData.m_isWaterGenerator = citemCell == GItems.generatorWater ? 1 : 0;
        itemPluginData.m_isWaterPump = citemCell == GItems.waterPump ? 1 : 0;
        itemPluginData.m_isLightGenerator = citemCell == GItems.generatorSun ? 1 : 0;
        itemPluginData.m_isBasalt = citemCell == GItems.lava ? 1 : 0;
        itemPluginData.m_isLightonium = citemCell == GItems.lightonium ? 1 : 0;
        itemPluginData.m_isOrganicHeart = citemCell == GItems.organicRockHeart ? 1 : 0;
        itemPluginData.m_isSunLamp = citemCell == GItems.lightSun ? 1 : 0;
        itemPluginData.m_isAutobuilder = citemCell is CItem_MachineAutoBuilder ? 1 : 0;
        itemPluginData.m_customValue = citemCell is CItem_Machine citemMachine ? citemMachine.m_customValue : 0f;
        return itemPluginData;
    }
    public static void RegisterItem(CItem item) {
        item.m_id = (ushort)GItems.Items.Count;
        GItems.Items.Add(item);

        item.Init();

        var itemPluginData = MakeItemPluginData(item);
        Utils.ArrayAppend(array: ref SSingleton<SItems>.Inst.m_itemsPluginData, value: itemPluginData);
    }
    public static void RegisterRecipe(CRecipe recipe) {
        foreach (CRecipesGroup recipeGroup in SDataLua.GetDescList<CRecipesGroup>("list_recipesgroups")) {
            if (recipeGroup.m_groupId != recipe.m_groupId) { continue; }

            recipeGroup.m_recipes.Add(recipe);
        }
    }
    public static void RegisterRecipeGroup(string groupId, List<CItem_MachineAutoBuilder> autobuilders) {
        var descList = SDataLua.GetDescList<CRecipesGroup>("list_recipesgroups");
        var recipeGroup = new CRecipesGroup() {
            m_groupId = groupId,
            m_recipes = [],
            m_autobuilders = autobuilders,
            m_id = "",
            m_mod = "",
        };
        descList.Add(recipeGroup);
    }
}

public static class CItemDeviceGroupIds {
    public static readonly string miniaturizor = "Miniaturizor";
    public static readonly string potionHP = "PotionHP";
    public static readonly string potionHPRegen = "PotionHPRegen";
    public static readonly string potionArmor = "PotionArmor";
    public static readonly string potionPheromones = "PotionPheromones";
    public static readonly string potionCritics = "PotionCritics";
    public static readonly string potionInvisibility = "PotionInvisibility";
    public static readonly string potionSpeed = "PotionSpeed";
    public static readonly string armor = "Armor";
    public static readonly string shield = "Shield";
    public static readonly string drone = "Drone";
    public static readonly string flashLight = "FlashLight";
    public static readonly string minimapper = "Minimapper";
    public static readonly string effeilGlasses = "EffeilGlasses";
    public static readonly string metalDetector = "MetalDetector";
    public static readonly string waterDetector = "WaterDetector";
    public static readonly string waterBreather = "WaterBreather";
    public static readonly string jetpack = "Jetpack";
    public static readonly string invisibility = "Invisibility";
    public static readonly string brush = "Brush";
}

public static class SoundIds {
    public static readonly string jump = "jump";
    public static readonly string fall = "fall";
    public static readonly string fall_water = "fall_water";
    public static readonly string hurt = "hurt";
    public static readonly string outOfAmmo = "outOfAmmo";
    public static readonly string plasma = "plasma";
    public static readonly string shotgun = "shotgun";
    public static readonly string plasmaSnipe = "plasmaSnipe";
    public static readonly string laser = "laser";
    public static readonly string particle = "particle";
    public static readonly string particleShotgun = "particleShotgun";
    public static readonly string storm = "storm";
    public static readonly string stormLight = "stormLight";
    public static readonly string rocketFire = "rocketFire";
    public static readonly string rocketHit = "rocketHit";
    public static readonly string defensePlasma = "defensePlasma";
    public static readonly string particleTurret = "particleTurret";
    public static readonly string mine = "mine";
    public static readonly string ceilingTurret = "ceilingTurret";
    public static readonly string firefly = "firefly";
    public static readonly string hound = "hound";
    public static readonly string dweller = "dweller";
    public static readonly string dwellerBoss = "dwellerBoss";
    public static readonly string fish = "fish";
    public static readonly string birdBomb = "birdBomb";
    public static readonly string monsterBat = "monsterBat";
    public static readonly string ant = "ant";
    public static readonly string bossCrab = "bossCrab";
    public static readonly string bossCrabScream = "bossCrabScream";
    public static readonly string bossBird = "bossBird";
    public static readonly string miniBalrog = "miniBalrog";
    public static readonly string spiders = "spiders";
    public static readonly string balrog = "balrog";
    public static readonly string monsterParticleGround = "monsterParticleGround";
    public static readonly string monsterParticle = "monsterParticle";
    public static readonly string miniaturizor = "miniaturizor";
    public static readonly string rain = "rain";
    public static readonly string rocketCinematic = "rocketCinematic";
    public static readonly string rocketExplosion = "rocketExplosion";
    public static readonly string jetpack = "jetpack";
    public static readonly string waterfall = "waterfall";
    public static readonly string lava = "lava";
    public static readonly string fireForest = "fireForest";
    public static readonly string doorOpen = "doorOpen";
    public static readonly string doorClose = "doorClose";
    public static readonly string teleport = "teleport";
    public static readonly string potions = "potions";
    public static readonly string fireImpact = "fireImpact";
    public static readonly string lavaEruption = "lavaEruption";
    public static readonly string alarm = "alarm";
}

public sealed class ModItem {
    public sealed class ItemRecipe(string groupId, bool isUpgrade = false) {
        public int nbOut = 1;
        public CItem in1 = null;
        public int nb1 = 0;
        public CItem in2 = null;
        public int nb2 = 0;
        public CItem in3 = null;
        public int nb3 = 0;

        public CRecipe ToCRecipe(CItem idOut) {
            return new CRecipe(idOut, nbOut, in1, nb1, in2, nb2, in3, nb3, isUpgrade) {
                m_groupId = groupId
            };
        }
    }

    public ModItem(string codeName, string name, string description, CItem item, ItemRecipe recipe = null) {
        Item = item;
        Recipe = recipe?.ToCRecipe(item);

        item.m_codeName = $"more-items_{codeName}";
        item.m_locTextId = $"more-items_I_{codeName}";

        ItemTools.AddItemNameDesc(item.m_locTextId, name, description);
    }

    public CItem Item { get; private set; }
    public CRecipe Recipe { get; private set; }

    public static implicit operator CItem(ModItem self) => self.Item;
}

public sealed class ModCSurface {
    public static readonly string surfacePath = "more-items_surfaces";

    public static UnityEngine.Texture2D surfaceTops = null;
    public static readonly string surfaceTopsPath = $"surface_tops";

    public static UnityEngine.Texture2D fertileDirtTexture = null;
    public static readonly string fertileDirtTexturePath = "fertileDirt";

    public sealed class TaggedCSurface(string surfaceTexture, int surfaceSortingOrder, int topTileI = -1, int topTileJ = -1, bool hasAltTop = false, CSurface surfaceGrass = null, CSurface surfaceGrassWet = null, bool isGrassWet = false)
        : CSurface(surfaceTexture, surfaceSortingOrder, topTileI, topTileJ, hasAltTop, surfaceGrass, surfaceGrassWet, isGrassWet) { }

    private readonly TaggedCSurface _surface = null;

    public ModCSurface(string surfaceTexture, int surfaceSortingOrder, int topTileI, int topTileJ, bool hasAltTop = false, CSurface surfaceGrass = null, CSurface surfaceGrassWet = null, bool isGrassWet = false) {
        _surface = (TaggedCSurface)FormatterServices.GetUninitializedObject(typeof(TaggedCSurface));

        _surface.m_surfaceTexture = $"{ModCSurface.surfacePath}/{surfaceTexture}";
        _surface.m_surfaceMat = SResources.GetMaterial("SurfaceOpaque", _surface.m_surfaceTexture);
        _surface.m_matTop = SResources.GetMaterial("SurfaceBorders", $"{ModCSurface.surfacePath}/{ModCSurface.surfaceTopsPath}");
        _surface.m_sortingOrder = surfaceSortingOrder;
        _surface.m_hasAltTop = hasAltTop;
        _surface.m_isGrassWet = isGrassWet;
        _surface.m_topTileCoords = new int2(topTileI, topTileJ);
        _surface.m_surfaceGrass = surfaceGrass;
        _surface.m_surfaceGrassWet = surfaceGrassWet;
    }

    public static implicit operator CSurface(ModCSurface self) => self._surface;
}

public sealed class ModRecipeGroup {
    public string GroupId { get; private set; }
    public List<CItem_MachineAutoBuilder> Autobuilders { get; private set; }

    public ModRecipeGroup(string groupId, List<CItem_MachineAutoBuilder> autoBuilders) {
        GroupId = groupId;
        Autobuilders = autoBuilders;
    }
}

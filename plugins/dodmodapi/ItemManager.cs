
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace DODModAPI;

public sealed class ModItem {
    public readonly record struct Ingredient(CItem Item, int Count);

    public sealed class ItemRecipe(string groupId, bool isUpgrade = false) {
        public int nbOut = 1;
        public Ingredient? in1 = null;
        public Ingredient? in2 = null;
        public Ingredient? in3 = null;

        public CRecipe ToCRecipe(CItem idOut) {
            return new CRecipe(
                idOut,
                nbOut,
                in1?.Item, in1?.Count ?? 0,
                in2?.Item, in2?.Count ?? 0,
                in3?.Item, in3?.Count ?? 0,
                isUpgrade
            ) {
                m_groupId = groupId
            };
        }
    }

    public delegate void PluginDataOverwriterFn(ref CItem_PluginData pluginData);

    public ModItem(string codeName, string name, string description, CItem item, ItemRecipe? recipe = null) {
        this.item = item;
        this.recipe = recipe?.ToCRecipe(item);

        item.m_name = name;
        item.m_desc = description;
        item.m_codeName = codeName;
        item.m_locTextId = $"I_{codeName}";
    }

    public readonly CItem item;
    public readonly CRecipe? recipe;
    public PluginDataOverwriterFn? PluginDataOverwriter { set; get; } = null;
}

public sealed class ModRecipeGroup {
    public string GroupId { get; private set; }
    public List<CItem_MachineAutoBuilder> Autobuilders { get; private set; }

    public ModRecipeGroup(string groupId, List<CItem_MachineAutoBuilder> autoBuilders) {
        GroupId = groupId;
        Autobuilders = autoBuilders;
    }
}

public static class ItemManager {
    private static readonly List<ModItem> _items = new();
    private static readonly List<ModRecipeGroup> _recipeGroups = new();

    private static bool _itemsLocked = false;
    private static bool _recipeGroupsLocked = false;

    public static event Action? OnPostModItemsInit;
    public static event Action? OnPostItemsInit;

    public static void RegisterItem(ModItem modItem) {
        LateRegistrationException.ThrowIfLocked(_itemsLocked);
        _items.Add(modItem);
    }

    public static void RegisterAllItems(Type type) {
        LateRegistrationException.ThrowIfLocked(_itemsLocked);
        foreach (var itemField in type.GetFields(BindingFlags.Static | BindingFlags.Public)) {
            _items.Add((ModItem)itemField.GetValue(null));
        }
    }

    public static void RegisterRecipeGroup(ModRecipeGroup recipeGroup) {
        LateRegistrationException.ThrowIfLocked(_recipeGroupsLocked);
        _recipeGroups.Add(recipeGroup);
    }

    public static void RegisterAllRecipeGroups(Type type) {
        LateRegistrationException.ThrowIfLocked(_recipeGroupsLocked);
        foreach (var recipeGroupField in type.GetFields(BindingFlags.Static | BindingFlags.Public)) {
            _recipeGroups.Add((ModRecipeGroup)recipeGroupField.GetValue(null));
        }
    }

    public static CItem_PluginData MakeItemPluginData(ModItem modItem) {
        // Copied from SItems.OnInit

        if (modItem.item is not CItemCell itemCell) {
            return default;
        }
        CItem_PluginData pluginData = new() {
            m_weight = itemCell is CItem_Wall wall ? wall.m_weight : 0f,
            m_electricValue = itemCell.m_electricValue,
            m_electricOutletFlags = itemCell.m_electricityOutletFlags,
            // GItems.elecCross => 1, GItems.elecSwitchRelay => 2, GItems.elecSwitch => 3, GItems.elecSwitchPush => 4, other => 0
            m_elecSwitchType = itemCell != GItems.elecCross ? itemCell != GItems.elecSwitchRelay ? itemCell != GItems.elecSwitch ? itemCell != GItems.elecSwitchPush ? 0 : 4 : 3 : 2 : 1,
            m_elecVariablePower = itemCell.m_electricVariablePower ? 1 : 0,
            m_anchor = (int)itemCell.m_anchor,
            m_light = itemCell.m_light,
            m_isBlock = itemCell.IsBlock() ? 1 : 0,
            m_isBlockDoor = itemCell.IsBlockDoor() ? 1 : 0,
            m_isReceivingForces = itemCell.IsReceivingForces() ? 1 : 0,
            m_isMineral = itemCell is CItem_Mineral ? 1 : 0,
            m_isDirt = itemCell is CItem_MineralDirt ? 1 : 0,
            m_isPlant = itemCell is CItem_Plant ? 1 : 0,
            m_isFireProof = itemCell.m_fireProof || itemCell is CItem_Plant plant && plant.m_conditions.m_isFireProof ? 1 : 0,
            m_isWaterGenerator = itemCell == GItems.generatorWater ? 1 : 0,
            m_isWaterPump = itemCell == GItems.waterPump ? 1 : 0,
            m_isLightGenerator = itemCell == GItems.generatorSun ? 1 : 0,
            m_isBasalt = itemCell == GItems.lava ? 1 : 0,
            m_isLightonium = itemCell == GItems.lightonium ? 1 : 0,
            m_isOrganicHeart = itemCell == GItems.organicRockHeart ? 1 : 0,
            m_isSunLamp = itemCell == GItems.lightSun ? 1 : 0,
            m_isAutobuilder = itemCell is CItem_MachineAutoBuilder ? 1 : 0,
            m_customValue = itemCell is CItem_Machine citemMachine ? citemMachine.m_customValue : 0f,
        };
        modItem.PluginDataOverwriter?.Invoke(ref pluginData);
        return pluginData;
    }

    internal static class Patches {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SItems), nameof(SItems.OnInit))]
        private static void SItems_OnInit() {
            OnPostItemsInit?.Invoke();

            foreach (var modItem in _items) {
                CItem item = modItem.item;
                item.m_id = (ushort)GItems.Items.Count;
                GItems.Items.Add(item);

                item.Init();

                CItem_PluginData itemPluginData = MakeItemPluginData(modItem);
                Misc.ArrayAppend(ref SItems.Inst.m_itemsPluginData, itemPluginData);

                // just in case someone calls SLoc.ReprocessTexts() later, reconstruct localization strings
                // (don't really need to do this since m_name and m_desc are inited at this point)
                SLoc.Inst.m_dico.Add(item.m_locTextId, new SLoc.CSentence(item.m_locTextId, $"{item.m_name}|{item.m_desc}"));
            }
            OnPostModItemsInit?.Invoke();

            _itemsLocked = true;
            DODModAPIPlugin.Log.LogInfo($"Added {_items.Count} custom items");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SDataLua), nameof(SDataLua.OnInit))]
        private static void SDataLua_OnInit() {
            // SOutgame.Mode == "Solo"

            var descList = SDataLua.GetDescList<CRecipesGroup>("list_recipesgroups");

            foreach (var modRecipeGroup in _recipeGroups) {
                descList.Add(new CRecipesGroup() {
                    m_groupId = modRecipeGroup.GroupId,
                    m_recipes = [],
                    m_autobuilders = modRecipeGroup.Autobuilders,
                    m_id = "",
                    m_mod = "",
                });
            }
            foreach (var modItem in _items) {
                if (modItem.recipe is null) { continue; }

                foreach (CRecipesGroup recipeGroup in descList) {
                    if (recipeGroup.m_groupId != modItem.recipe.m_groupId) { continue; }

                    recipeGroup.m_recipes.Add(modItem.recipe);
                }
            }
            _recipeGroupsLocked = true;
            DODModAPIPlugin.Log.LogInfo($"Added {_recipeGroups.Count} custom recipe groups");
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CInventory), "InventorySorting")]
        private static bool CInventory_InventorySorting(CStack a, CStack b, ref int __result) {
            static int CategoryToOrdinal(string categoryId) {
                return categoryId switch {
                    "CITEM_DEVICE" => 0,
                    "CITEM_WEAPON" => 1,
                    "CITEM_DEFENSE" => 2,
                    "CITEM_WALL" => 3,
                    "CITEM_MACHINE" => 4,
                    "CITEM_MINERAL" => 5,
                    "CITEM_MATERIAL" => 6,
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
                    __result = CategoryToOrdinal(a_item.m_categoryId) - CategoryToOrdinal(b_item.m_categoryId);
                }
                return false;
            }
            return true;
        }
    }
}

public static class DeviceGroupIds {
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

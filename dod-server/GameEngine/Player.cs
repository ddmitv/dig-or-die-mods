
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameEngine;

public sealed class CStack {
    public readonly CItem m_item;
    public int m_nb = 1;
    public readonly float m_probability = 1f;

    public CStack(CItem item, int nb = 1) {
        m_item = item;
        m_nb = nb;
    }
    public CStack(CItem item, float probability) {
        m_item = item;
        m_probability = probability;
    }

    public override string ToString() {
        if (m_probability == 1f) {
            return $"{m_item} (x{m_nb})";
        } else {
            return $"{m_item} ({m_probability * 100f}%)";
        }
    }
}
public sealed class CRecipe {
    public readonly CStack m_out;
    public readonly CStack? m_in1;
    public readonly CStack? m_in2;
    public readonly CStack? m_in3;
    public readonly bool m_isUpgrade;

    public CRecipe(CItem idOut, int nbOut, bool isUpgrade = false) {
        m_out = new(idOut, nbOut);
        m_isUpgrade = isUpgrade;
    }
    public CRecipe(CItem idOut, int nbOut, CItem idIn1, int nbIn1, bool isUpgrade = false) {
        m_out = new(idOut, nbOut);
        m_in1 = new(idIn1, nbIn1);
        m_isUpgrade = isUpgrade;
    }
    public CRecipe(CItem idOut, int nbOut, CItem idIn1, int nbIn1, CItem idIn2, int nbIn2, bool isUpgrade = false) {
        m_out = new(idOut, nbOut);
        m_in1 = new(idIn1, nbIn1);
        m_in2 = new(idIn2, nbIn2);
        m_isUpgrade = isUpgrade;
    }
    public CRecipe(CItem idOut, int nbOut, CItem idIn1, int nbIn1, CItem idIn2, int nbIn2, CItem idIn3, int nbIn3, bool isUpgrade = false) {
        m_out = new(idOut, nbOut);
        m_in1 = new(idIn1, nbIn1);
        m_in2 = new(idIn2, nbIn2);
        m_in3 = new(idIn3, nbIn3);
        m_isUpgrade = isUpgrade;
    }

    public bool IsAutoBuilderUpgrade(int2 autoBuilderPos) {
        return m_isUpgrade && m_out.m_item is CItem_MachineAutoBuilder && m_in1?.m_item == World.Grid[autoBuilderPos].GetContent();
    }
    public bool IsSameRecipe(CRecipe r) {
        return r.m_out.m_item == m_out.m_item && r.m_out.m_nb == m_out.m_nb
            && r.m_in1?.m_item == m_in1?.m_item && r.m_in1?.m_nb == m_in1?.m_nb
            && r.m_in2?.m_item == m_in2?.m_item && r.m_in2?.m_nb == m_in2?.m_nb
            && r.m_in3?.m_item == m_in3?.m_item && r.m_in3?.m_nb == m_in3?.m_nb;
    }
    public override string ToString() {
        if (m_in1 is null) {
            return $"{m_out} <= (nothing)";
        } else if (m_in2 is null) {
            return $"{m_out} <= {m_in1}";
        } else if (m_in3 is null) {
            return $"{m_out} <= {m_in1} + {m_in2}";
        } else {
            return $"{m_out} <= {m_in1} + {m_in2} + {m_in3}";
        }
    }
}
public sealed class CItemVars {
    public Dictionary<string, float> Dico { get; } = [];

    public float Heating {
        get => GetVar("heating", 0f);
        set => SetVar("heating", value);
    }
    public ushort ZF0TargetId {
        get => (ushort)GetVar("m_zf0target", 0f);
        set => SetVar("m_zf0target", value);
    }
    public float ZF0TargetLastTimeHit {
        get => GetVar("zf0targetLastTimeHit", 0f);
        set => SetVar("zf0targetLastTimeHit", value);
    }
    public int ZF0TargetNbHit {
        get => (int)GetVar("zf0TargetNbHit", 0f);
        set => SetVar("zf0TargetNbHit", value);
    }
    public float ShieldValue {
        get => GetVar("shieldValue", 0f);
        set => SetVar("shieldValue", value);
    }
    public float ShieldLastHitTime {
        get => GetVar("shieldLastHitTime", 0f);
        set => SetVar("shieldLastHitTime", value);
    }
    public float ShieldLastHitAngle {
        get => GetVar("shieldLastHitAngle", 0f);
        set => SetVar("shieldLastHitAngle", value);
    }
    public bool IsPassiveItemOn {
        get => GetVar("isPassiveItemOn", 1f) != 0f;
        set => SetVar("isPassiveItemOn", value ? 1 : 0);
    }
    public bool IsInRocketTop {
        get => GetVar("inRocketTop", 0f) != 0f;
        set => SetVar("inRocketTop", value ? 1 : 0);
    }
    public bool IsJetpackActive {
        get => GetVar("JetpackActive", 0f) != 0f;
        set => SetVar("JetpackActive", value ? 1 : 0);
    }
    public float StormGunLastTimeRain {
        get => GetVar("stormGunLastTimeRain", 0f);
        set => SetVar("stormGunLastTimeRain", value);
    }
    public float TimeLastUse { get; set; } = float.MinValue;
    public float TimeActivation { get; set; } = float.MinValue;

    public float GetVar(string name, float defaultValue = 0f) {
        return Dico.GetValueOrDefault(name, defaultValue);
    }

    public void SetVar(string name, float value) {
        Dico[name] = value;
    }

    public void Reset(bool keepPassiveItemsOn = false) {
        bool isPassiveItemOn = IsPassiveItemOn;
        Dico.Clear();
        if (keepPassiveItemsOn && !isPassiveItemOn) {
            IsPassiveItemOn = false;
        }
        TimeLastUse = float.MinValue;
        TimeActivation = float.MinValue;
    }
}

public sealed class CInventory {
    private readonly CPlayer m_player;
    public readonly List<CStack> m_items = new(capacity: 64);
    public readonly CStack?[] m_barItems = new CStack?[20];
    public CStack? m_itemSelected = null;

    public CInventory(CPlayer player) {
        m_player = player;
    }
    public CStack? GetStack(CItem? item) => item is null ? null : m_items.Find(x => x.m_item == item);
    public CStack? GetStack(ushort itemId) => m_items.Find(x => x.m_item.m_id == itemId);
    public bool IsInInventory(CItem item) => GetStack(item)?.m_nb >= 1;
    public CStack? GetItemInSlot(int slot) => slot < 0 ? null : m_barItems[slot];
    public CItem? GetItemSelected() => m_itemSelected?.m_item;
    public int GetSlotSelected() => m_itemSelected is null ? -1 : Array.FindIndex(m_barItems, x => x == m_itemSelected);
    public int GetItemBarSlot(CStack stack) => Array.FindIndex(m_barItems, x => x == stack);

    public CStack AddToInventory(CItem item, int nb = 1, bool selectIt = false, bool addItToBarIFN = true) {
        CStack? stack = GetStack(item);
        if (stack is not null) {
            stack.m_nb += nb;
            MessageProcessing.SendToAll(new MessageInventory(m_player, stack));
        } else {
            stack = new CStack(item, nb);
            m_items.Add(stack);
            SortInventory();
            MessageProcessing.SendToAll(new MessageInventory(m_player, stack));
            if (addItToBarIFN) {
                AddItemToBarIFP(stack, selectIt, true, false);
            }
        }
        if (stack.m_item == GItems.autoBuilderMK1) {
            GVars.m_autoBuilderLevelBuilt = 1;
            // SSteamStats.SetStat("progress", 1, false);
        } else if (stack.m_item == GItems.autoBuilderUltimate) {
            GVars.m_autoBuilderLevelBuilt = 6;
        }
        return stack;
    }
    private bool AddItemToBarIFP(CStack stack, bool select, bool skipMaterialsAndMinerals = false, bool activatePassiveItems = false) {
        if (skipMaterialsAndMinerals && (stack.m_item is CItem_Material || stack.m_item is CItem_Mineral)) {
            return false;
        }
        if (Array.Exists(m_barItems, x => x == stack)) {
            return false;
        }
        for (int i = 0; i < m_barItems.Length; ++i) {
            if (m_barItems[i] is not null) { continue; }
            SetItemToBar(stack, i, activatePassiveItems);
            if (select) {
                m_itemSelected = stack;
            }
            return true;
        }
        return false;
    }
    public void SetItemToBar(CStack stack, int slot, bool toggleOnDevices = false) {
        m_barItems[slot] = stack;
        if (toggleOnDevices && stack != null && stack?.m_item is CItem_Device itemDevice && itemDevice.m_type == CItem_Device.Type.Passive) {
            CItemVars itemVars = itemDevice.GetVars(m_player);
            itemVars.IsPassiveItemOn = true;
            if (itemDevice.m_group == CItem_Device.Group.Jetpack && itemDevice.m_customValue == 0f) {
                itemVars.SetVar("JetpackEnergy", 0f);
            }
        }
        MessageProcessing.SendToAll(new MessageItemsBar(m_player, slot));
    }
    public bool RemoveFromInventory(CItem item, int nb = 1, bool alsoRemoveStack0 = false) {
        CStack? stack = GetStack(item);
        if (stack is null || stack.m_nb < nb) {
            return false;
        }
        stack.m_nb -= nb;
        if (alsoRemoveStack0 && stack.m_nb == 0) {
            RemoveItemFromBar(stack);
            m_items.Remove(stack);
        }
        if (stack.m_nb <= 0 && m_itemSelected == stack && GetSlotSelected() < 0) {
            m_itemSelected = null;
        }
        MessageProcessing.SendToAll(new MessageInventory(m_player, stack));
        return true;
    }
    public int RemoveItemFromBar(CStack stack) {
        for (int i = 0; i < m_barItems.Length; i++) {
            if (m_barItems[i] == stack) {
                m_barItems[i] = null;
                MessageProcessing.SendToAll(new MessageItemsBar(m_player, i));
                return i;
            }
        }
        return -1;
    }
    public int Craft(CRecipe recipe, int nbToCraft, int2 openAutobuilderPos, bool isAllFree) {
        if (openAutobuilderPos == int2.negative || World.Grid[openAutobuilderPos].GetContent() is not CItem_MachineAutoBuilder itemAutobuilder) {
            Logging.Warning($"Trying to craft but autobuiler doesn't exists at {openAutobuilderPos}");
            return 0;
        }
        if (itemAutobuilder.m_allFree != isAllFree) {
            Logging.Warning($"Mismatch of 'all free' autobuiler flag (autobuiler={itemAutobuilder}, autobuiler.allFree={itemAutobuilder.m_allFree}, isAllFree={isAllFree})");
            return 0;
        }
        CRecipe[] autobuilderRecipes = itemAutobuilder.recipes;
        if (!autobuilderRecipes.Any(x => x.IsSameRecipe(recipe))) {
            Logging.Warning($"Trying to craft an item from an unexisting recipe ({recipe})");
            return 0;
        }
        int craftedCount = 0;
        for (int i = 0; i < nbToCraft; i++) {
            bool isAutobuilderUpgrade = recipe.IsAutoBuilderUpgrade(openAutobuilderPos);
            bool in1Avaliable = IsStackInInventory(recipe.m_in1, isAllFree || isAutobuilderUpgrade);
            bool in2Avaliable = IsStackInInventory(recipe.m_in2, isAllFree);
            bool in3Avaliable = IsStackInInventory(recipe.m_in3, isAllFree);
            if (in1Avaliable && in2Avaliable && in3Avaliable) {
                if (!isAllFree) {
                    if (!isAutobuilderUpgrade && recipe.m_in1 is not null) {
                        RemoveFromInventory(recipe.m_in1.m_item, recipe.m_in1.m_nb, alsoRemoveStack0: recipe.m_isUpgrade);
                    }
                    if (recipe.m_in2 is not null) {
                        RemoveFromInventory(recipe.m_in2.m_item, recipe.m_in2.m_nb);
                    }
                    if (recipe.m_in3 is not null) {
                        RemoveFromInventory(recipe.m_in3.m_item, recipe.m_in3.m_nb);
                    }
                }
                if (!isAutobuilderUpgrade) {
                    AddToInventory(recipe.m_out.m_item, recipe.m_out.m_nb, selectIt: true, addItToBarIFN: true);
                    // GVars.m_achievNoCraft = false;
                } else {
                    if (GetStack(itemAutobuilder) is CStack stackAutobuilder) {
                        RemoveItemFromBar(stackAutobuilder);
                    }
                    World.Grid[openAutobuilderPos].m_contentId = recipe.m_out.m_item.m_id;
                    World.OnSetContent(openAutobuilderPos);
                    // if (recipe.m_out.m_item == GItems.autoBuilderMK2) {
                    //     GVars.m_autoBuilderLevelBuilt = 2;
                    //     SSteamStats.SetStat("progress", 2, false);
                    //     GVars.m_achievNoMK2 = false;
                    // } else if (recipe.m_out.m_item == GItems.autoBuilderMK3) {
                    //     GVars.m_autoBuilderLevelBuilt = 3;
                    //     SSteamStats.SetStat("progress", 3, false);
                    // } else if (recipe.m_out.m_item == GItems.autoBuilderMK4) {
                    //     GVars.m_autoBuilderLevelBuilt = 4;
                    //     SSteamStats.SetStat("progress", 4, false);
                    // } else if (recipe.m_out.m_item == GItems.autoBuilderMK5) {
                    //     GVars.m_autoBuilderLevelBuilt = 5;
                    //     SSteamStats.SetStat("progress", 5, false);
                    // }
                }
                craftedCount++;
            }
        }
        return craftedCount;
    }
    public bool IsStackInInventory(CStack? stack, bool skipTests = false) {
        if (stack is null || skipTests) { return true; }

        foreach (CStack invStacks in m_items) {
            if (invStacks.m_item == stack.m_item && invStacks.m_nb >= stack.m_nb) {
                return true;
            }
        }
        return false;
    }
    public void SortInventory() {
        m_items.Sort((a, b) => a.m_item.m_id - b.m_item.m_id);
    }

    public CItem_Device? GetBestActiveOfGroup(CItem_Device.Group group, bool skipInactive = true) {
        CItem_Device? resultItem = null;
        foreach (CStack cstack in m_items) {
            if (cstack?.m_item is CItem_Device item
                && item.m_group == group
                && cstack.m_nb > 0
                && (resultItem is null || item.m_customValue - resultItem.m_customValue > 0f)
                && (!skipInactive
                    || (item.m_type == CItem_Device.Type.Passive && item.GetVars(m_player).IsPassiveItemOn)
                    || ((item.m_type == CItem_Device.Type.Activable || item.m_type == CItem_Device.Type.Consumable) && item.IsDurationItemActive(m_player)))) {
                resultItem = item;
            }
        }
        return resultItem;
    }
}

public class CPlayer {
    public const float PingRefreshTime = 5f;
    public const float PingRefreshTimeHidden = 20f;
    public const int NbHairStyle = 5;

    public NetworkClient? networkClient;
    public ulong m_steamId;

    public string m_name = null!;
    public bool m_skinIsFemale;
    public float m_skinColorSkin = 0.7f;
    public int m_skinHairStyle = -1;
    public Color24 m_skinColorHair = new(85, 67, 27);
    public Color24 m_skinColorEyes = new(0, 51, 102);
    public CUnitPlayer? m_unitPlayer;
    public ushort m_unitPlayerId;
    public Vector2 m_posSaved = Vector2.zero;
    public float m_cameraAspect = GameState.cameraAspect;
    public CInventory m_inventory;
    public float[,]? m_chunksSendTime;
    public List<float> m_pickupsSendTime = [];
    // public Color32[]? m_minimapViewPixels;
    public readonly List<CItemVars?> m_itemVars = [];
    public double m_lastTimeMessagePosReceived;
    public int m_mouseOverLastTime = -1;
    public int m_mouseOverDraggingLastTime = -1;

    public CPlayer() {
        m_inventory = new CInventory(this);
        m_lastTimeMessagePosReceived = Game.SimuTime;
    }

    public bool IsAFK() {
        return GVars.SimuTimeD > m_lastTimeMessagePosReceived + 2.0;
    }
    public RectInt GetRectAroundScreen(int distance) => GetRectAroundScreen(new int2(distance, distance));

    public RectInt GetRectAroundScreen(int2 distance) {
        RectInt result = default;
        if (m_unitPlayer is not null) {
            Vector2 unitPosCenter = m_unitPlayer.PosCenter;
            Vector2 vector = 12f * new Vector2(m_cameraAspect, 1f);
            result = new(x: (int)(unitPosCenter.x - vector.x) - distance.x,
                         y: (int)(unitPosCenter.y - vector.y) - distance.y,
                         width: (int)MathF.Ceiling(2f * vector.x + 1f) + 2 * distance.x,
                         height: (int)MathF.Ceiling(2f * vector.y + 1f) + 2 * distance.y);
        }
        result.x = Math.Max(result.x, 0);
        result.width = Math.Min(result.width, World.Gs.x - result.x);
        result.y = Math.Max(result.y, 0);
        result.height = Math.Min(result.height, World.Gs.y - result.y);
        return result;
    }
    public bool IsInRectAroundScreen(int distance, Vector2 pos) {
        return IsInRectAroundScreen(new int2(distance, distance), pos);
    }
    public bool IsInRectAroundScreen(int2 distance, Vector2 pos) {
        if (m_unitPlayer is null) {
            return false;
        }
        Vector2 unitPosCenter = m_unitPlayer.PosCenter;
        Vector2 vector = 12f * new Vector2(m_cameraAspect, 1f);
        float rectX = (unitPosCenter.x - vector.x) - distance.x;
        float rectY = (unitPosCenter.y - vector.y) - distance.y;
        // Math.FusedMultiplyAdd should be faster than x*y+z?
        return pos.x >= rectX
            && pos.x <= rectX + MathF.FusedMultiplyAdd(2f, vector.x, 1f) + 2 * distance.x
            && pos.y >= rectY
            && pos.y <= rectY + MathF.FusedMultiplyAdd(2f, vector.y, 1f) + 2 * distance.y;
    }

    public CItemVars GetItemVars(ushort itemId) {
        // could be optimized
        while (m_itemVars.Count <= itemId) {
            m_itemVars.Add(null);
        }
        if (m_itemVars[itemId] is null) {
            m_itemVars[itemId] = new CItemVars();
        }
        return m_itemVars[itemId]!;
    }
    public CItemVars GetItemVars(CItem item) {
        return GetItemVars(item.m_id);
    }
    public void CleanItemVars(bool keepPassiveItemsOn = false) {
        if (keepPassiveItemsOn) {
            foreach (CItemVars? citemVars in m_itemVars) {
                citemVars?.Reset(keepPassiveItemsOn);
            }
        } else {
            m_itemVars.Clear();
        }
    }

    public override string ToString() {
        return $"{{name={m_name}, steam id={m_steamId}}}";
    }

    public bool HasUnitPlayerAlive() {
        return m_unitPlayer is not null && m_unitPlayer.IsAlive();
    }

    public bool HasUnitPlayer() {
        return m_unitPlayer is not null;
    }
}

public static class PlayerManager {
    public readonly static List<CPlayer> players = [];

    public static CPlayer? GetPlayerByInstanceId(ushort instanceId) {
        foreach (var player in players) {
            if (player.m_unitPlayerId == instanceId) {
                return player;
            }
        }
        return null;
    }
    public static CPlayer? GetPlayerByUnit(CUnitPlayer unit) {
        foreach (var player in players) {
            if (player.m_unitPlayer == unit) {
                return player;
            }
        }
        return null;
    }
    public static CPlayer? GetPlayerBySteamId(ulong steamId) {
        foreach (var player in players) {
            if (player.m_steamId == steamId) {
                return player;
            }
        }
        return null;
    }
    public static (CPlayer player, bool alreadyExists) FindOrAddPlayer(ulong steamId) {
        for (int i = 0; i < players.Count; ++i) {
            if (players[i].m_steamId == steamId) {
                return (players[i], true);
            }
        }
        var newPlayer = new CPlayer() {
            m_steamId = steamId,
            m_unitPlayerId = (ushort)(players.Count + 10u)
        };
        players.Add(newPlayer);
        return (newPlayer, false);
    }
    public static int GetNbPlayersConnected() {
        return players.Count(player => player.HasUnitPlayer() && player.networkClient is null);
    }
}


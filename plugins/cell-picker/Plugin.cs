using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using ModUtils;
using UnityEngine;

internal static class PickCellPatch {
    private static void PickMouseCellItem() {
        CItemCell mouseCellContent = SGame.MouseCell.GetContent();
        if (mouseCellContent is null) {
            CItem_Wall backwallItem = SGame.MouseCell.GetBackwall();
            if (backwallItem is null) { return; }
            mouseCellContent = backwallItem;
        }

        var inventory = SItems.GetMyInventory();
        var stack = inventory?.GetStack(mouseCellContent);
        if (CellPicker.configIgnoreEmptyItem.Value && stack is null) { return; }

        inventory.ItemSelected = stack;
    }
    private static void PickElectricWire() {
        var inventory = SItems.GetMyInventory();
        var stack = inventory?.GetStack(GItems.electricWire);
        if (stack is null) { return; }

        inventory.ItemSelected = stack;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SGame), nameof(SGame.OnUpdate))]
    private static void SGame_OnUpdate() {
        if (CellPicker.pickCell.IsKeyDown()) {
            PickMouseCellItem();
        } else if (CellPicker.configPickWire.Value.IsDown()) {
            PickElectricWire();
        } else if (CellPicker.configClearSelectedItem.Value.IsDown()) {
            var inventory = SItems.GetMyInventory();
            if (inventory is not null) {
                inventory.ItemSelected = null;
            }
        }
    }
}

[BepInPlugin("cell-picker", "Cell Picker", "1.1.0")]
public class CellPicker : BaseUnityPlugin {
    public static SInputs.KeyBinding pickCell = null;
    public static ConfigEntry<bool> configIgnoreEmptyItem = null;
    public static ConfigEntry<KeyboardShortcut> configPickWire = null;
    public static ConfigEntry<KeyboardShortcut> configClearSelectedItem = null;

    private void Start() {
        configIgnoreEmptyItem = Config.Bind<bool>("General", "IgnoreEmptyItem", defaultValue: true);
        var configEnabled = Config.Bind<bool>("General", "Enabled", defaultValue: true);
        configPickWire = Config.Bind<KeyboardShortcut>("General", "PickWire", defaultValue: KeyboardShortcut.Empty);
        configClearSelectedItem = Config.Bind<KeyboardShortcut>("General", "ClearSelectedItem", defaultValue: KeyboardShortcut.Empty);

        if (!configEnabled.Value) { return; }

        Utils.AddLocalizationText("INPUT_CELL_PICKER_PickCell", "Pick Cell");
        pickCell = new(name: "CELL_PICKER_PickCell", defaultKey0: KeyCode.Mouse2);

        var harmony = new Harmony(Info.Metadata.GUID);
        harmony.PatchAll(typeof(PickCellPatch));
    }
}


using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using ModUtils;
using UnityEngine;

internal static class PickCellPatch {
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SGame), nameof(SGame.OnUpdate))]
    private static void SGame_OnUpdate() {
        if (CellPicker.pickCell.IsKeyDown()) {
            var mouseCellContent = SGame.MouseCell.GetContent();
            if (mouseCellContent is null) { return; }

            var inventory = SItems.GetMyInventory();
            var stack = inventory.GetStack(mouseCellContent);
            if (CellPicker.configIgnoreEmptyItem.Value && stack is null) { return; }
            inventory.ItemSelected = stack;
        }
    }
}

[BepInPlugin("cell-picker", "Cell Picker", "1.0.0")]
public class CellPicker : BaseUnityPlugin {
    public static SInputs.KeyBinding pickCell = null;
    public static ConfigEntry<bool> configIgnoreEmptyItem = null;

    private void Start() {
        configIgnoreEmptyItem = Config.Bind<bool>("General", "IgnoreEmptyItem", defaultValue: true);
        var configEnabled = Config.Bind<bool>("General", "Enabled", defaultValue: true);
        if (!configEnabled.Value) { return; }

        Utils.AddLocalizationText("CELL_PICKER_PickCell", "Pick Cell");
        pickCell = new(name: "CELL_PickCell", defaultKey0: KeyCode.Mouse2);

        var harmony = new Harmony(Info.Metadata.GUID);
        harmony.PatchAll(typeof(PickCellPatch));
    }
}


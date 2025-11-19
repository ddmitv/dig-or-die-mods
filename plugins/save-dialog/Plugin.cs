using BepInEx;
using HarmonyLib;
using System.IO;
using System.Linq;
using System;

[BepInPlugin("save-dialog", ThisPluginInfo.Name, ThisPluginInfo.Version)]
public class SaveDialog : BaseUnityPlugin {
    private static CGuiButton btSaveLoadDialog;
    private static string getSaveFolderReturnValue = null;

    private const int SaveLoadDialogYOffset = 40;
    private const string SaveToTxt = "SAVE TO";
    private const string LoadFromTxt = "LOAD FROM";

    void Awake() {
        var harmony = new Harmony(Info.Metadata.GUID);
        harmony.PatchAll(typeof(SaveDialog));
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SScreenSave), nameof(SScreenSave.OnInit))]
    private static void SScreenSave_OnInit(SScreenSave __instance) {
        btSaveLoadDialog = new CGuiButton(__instance, __instance.m_guiRoot, x: -480, y: SaveLoadDialogYOffset) {
            m_width = 380,
            m_height = 80,
        };
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SScreenSave), nameof(SScreenSave.Open))]
    private static void SScreenSave_Open(bool isSaveMode) {
        btSaveLoadDialog.SetText(isSaveMode ? SaveToTxt : LoadFromTxt);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SScreenSave), nameof(SScreenSave.OnActivate))]
    private static void SScreenSave_OnActivate(SScreenSave __instance) {
        int yOffset = SOutgame.IsModeCustom() ? 250 : 0;
        btSaveLoadDialog.m_y = SaveLoadDialogYOffset - yOffset;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SScreenSave), nameof(SScreenSave.OnUpdate))]
    private static void SScreenSave_OnUpdate(SScreenSave __instance) {
        if (__instance.m_isSaveMode) {
            if (btSaveLoadDialog.IsClicked()) {
                string path = FileDialogHelper.ShowSaveDialog(
                    title: "Save as", filter: "*.save", initialDir: SOutgame.GetSaveFolder(),
                    defaultExt: "save"
                );
                if (!string.IsNullOrEmpty(path)) {
                    getSaveFolderReturnValue = Path.GetDirectoryName(path);
                    SGameStartEnd.SaveGame(SDataSave.SaveType.Named, Path.GetFileNameWithoutExtension(path));
                }
            }
        } else {
            if (btSaveLoadDialog.IsClicked()) {
                string path = FileDialogHelper.ShowOpenDialog(
                    title: "Select save file", filter: "*.save", initialDir: SOutgame.GetSaveFolder()
                );
                if (!string.IsNullOrEmpty(path)) {
                    getSaveFolderReturnValue = Path.GetDirectoryName(path);
                    SGameStartEnd.LoadGame(SOutgame.Mode.m_name, name: Path.GetFileNameWithoutExtension(path));
                }
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SOutgame), nameof(SOutgame.GetSaveFolder))]
    private static bool SOutgame_GetSaveFolder(ref string __result) {
        if (getSaveFolderReturnValue is null) { return true; }
        __result = getSaveFolderReturnValue;
        getSaveFolderReturnValue = null;
        return false;
    }
}

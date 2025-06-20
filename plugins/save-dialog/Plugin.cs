using BepInEx;
using HarmonyLib;
using System.IO;
using System.Linq;
using System;

[BepInPlugin("save-dialog", "Save Dialog", "1.0.0")]
public class SaveDialog : BaseUnityPlugin {
    private static CGuiButton btSaveLoadDialog;

    private const int SaveLoadDialogYOffset = 40;
    private const string SaveToTxt = "SAVE TO";
    private const string LoadFromTxt = "LOAD FROM";

    void Awake() {
        var harmony = new Harmony(Info.Metadata.GUID);
        harmony.PatchAll(typeof(SaveDialog));
    }

    private static string GetRelativePathToRoot(string path) {
        string fullPath = Path.GetFullPath(path);
        string root = Path.GetPathRoot(fullPath);

        if (string.IsNullOrEmpty(root)) { return ""; }

        string[] parts = fullPath.Substring(root.Length)
            .Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0) { return ""; }

        return string.Join(Path.DirectorySeparatorChar.ToString(), Enumerable.Repeat("..", parts.Length).ToArray());
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
                    string prefixPath = GetRelativePathToRoot(SOutgame.GetSaveFolder());
                    string savePath = Path.GetFileNameWithoutExtension(Path.Combine(prefixPath, path));
                    SGameStartEnd.SaveGame(SDataSave.SaveType.Named, savePath);
                }
            }
        } else {
            if (btSaveLoadDialog.IsClicked()) {
                string path = FileDialogHelper.ShowOpenDialog(
                    title: "Select save file", filter: "*.save", initialDir: SOutgame.GetSaveFolder()
                );
                if (!string.IsNullOrEmpty(path)) {
                    string prefixPath = GetRelativePathToRoot(SOutgame.GetSaveFolder());
                    string savePath = Path.GetFileNameWithoutExtension(Path.Combine(prefixPath, path));
                    SGameStartEnd.LoadGame(SOutgame.Mode.m_name, savePath);
                }
            }
        }
    }
}

using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using ModUtils;
using ModUtils.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;

internal class ScreenOnQuitPopup : SSingletonScreen<ScreenOnQuitPopup> {
    public CGuiBitmap bmpBack = new();
    public CGuiPanel panel = new();
    public CGuiText txtConfirm = new();
    public CGuiButton btBack = new();
    public CGuiButton btSave = new();
    public CGuiButton btQuit = new();

    public static bool hasBeenActive = false;

    public override void OnInit() {
        m_isModal = true;

        bmpBack.m_sprite = SResources.GetSprite("UI/gui", "Black75p").Sprite;
        bmpBack.m_scale = new(200f, 200f);

        panel.m_width = 1000;
        panel.m_height = 500;

        txtConfirm.m_x = 0;
        txtConfirm.m_y = 60;

        btBack.m_x = 350;
        btBack.m_y = -160;
        btBack.m_width = 250;
        btBack.m_textId = "AUTOSAVE_TWEAKS_BACK";

        btSave.m_x = 50;
        btSave.m_y = -160;
        btSave.m_width = 300;
        btSave.m_textId = "AUTOSAVE_TWEAKS_SAVE";

        btQuit.m_x = -300;
        btQuit.m_y = -160;
        btQuit.m_width = 350;
        btQuit.m_textId = "AUTOSAVE_TWEAKS_QUIT";
    }

    public override void OnUpdate() {
        if (btBack.IsClicked() || SInputs.IsEscapePressedInScreen(this)) {
            Deactivate();
        }
        if (btSave.IsClicked()) {
            if (SInputs.GetKeyShift() ^ AutosaveTweaks.configQuickSaveIsDefault.Value) {
                SGameStartEnd.SaveGame(SDataSave.SaveType.AutoSave);
                Deactivate();
                SScreenPause.Inst.OnQuitConfirmed();
            } else {
                if (SGame.Inst.CheckSave()) {
                    hasBeenActive = true;
                    SScreenSave.Inst.Open(true);
                }
                Deactivate();
            }
        }
        if (btQuit.IsClicked()) {
            Deactivate();
            if (AutosaveTweaks.configExitOnShift.Value && SInputs.GetKeyShift()) {
                hasBeenActive = true;
                Screen.SetResolution(624, 384, fullscreen: false);
                Application.Quit();
            } else {
                SScreenPause.Inst.OnQuitConfirmed();
            }
        }
    }

    public void Show() {
        string timeSinceLastSave = FormatDuration(TimeSpan.FromSeconds(Time.time - AutosaveTweaks.lastSaveTime));
        txtConfirm.SetText($"You have unsaved progress.\nAre you sure you want to quit without saving?\n\nTime since last save:\n{timeSinceLastSave}");
        Activate();
    }

    private static string FormatDuration(TimeSpan duration) {
        StringBuilder sb = new(50);

        int hours = (int)duration.TotalHours;
        if (hours > 0) { sb.Append($"{hours} hour{(hours == 1 ? "" : "s")}, "); }
        int minutes = duration.Minutes;
        if (hours > 0 || minutes > 0) { sb.Append($"{minutes} minute{(minutes == 1 ? "" : "s")}, "); }
        int seconds = duration.Seconds;
        sb.Append($"{seconds} second{(seconds == 1 ? "" : "s")}");
        return sb.ToString();
    }
}

[BepInPlugin("autosave-tweaks", ThisPluginInfo.Name, ThisPluginInfo.Version)]
public class AutosaveTweaks : BaseUnityPlugin {
    public static ConfigEntry<bool> configQuickSaveIsDefault;
    public static ConfigEntry<float> configUnsavedTimeThreshold;
    public static ConfigEntry<bool> configShowQuitConfirmation;
    public static ConfigEntry<int> configAutosaveSlots;
    public static ConfigEntry<int> configQuicksaveSlots;
    public static ConfigEntry<bool> configEnableRecoverySave;
    public static ConfigEntry<bool> configExitOnShift;

    public static float lastSaveTime;

    private void Awake() {
        configQuickSaveIsDefault = Config.Bind<bool>(section: "General", key: "QuickSaveIsDefault",
            defaultValue: false, description: "When enabled, cliking 'SAVE AND QUIT' button performs a quicksave. Hold Shift to open the full save menu instead"
        );
        configUnsavedTimeThreshold = Config.Bind<float>(section: "General", key: "UnsavedTimeThreshold",
            defaultValue: 5f, description: "Show quit confirmation when you have unsaved progress older than this duration. Set to 0 to always show it"
        );
        configShowQuitConfirmation = Config.Bind<bool>(section: "General", key: "ShowQuitConfirmation",
            defaultValue: true, description: "Show confirmation popup when quitting with unsaved progress"
        );
        configAutosaveSlots = Config.Bind<int>(section: "General", key: "AutosaveSlots",
            defaultValue: 5, configDescription: new ConfigDescription(
                "Maximum number of Autosave slots (A-Z)",
                new AcceptableValueRange<int>(1, 26)
            )
        );
        configQuicksaveSlots = Config.Bind<int>(section: "General", key: "QuicksaveSlots",
            defaultValue: 3, configDescription: new ConfigDescription(
                "Maximum number of Quicksave slots (A-Z)",
                new AcceptableValueRange<int>(1, 26)
            )
        );
        configEnableRecoverySave = Config.Bind<bool>(section: "General", key: "EnableRecoverySave",
            defaultValue: false, description: "Create a recovery save file when closing the game with unsaved progress (DOES NOT protect against crashes)"
        );
        configExitOnShift = Config.Bind<bool>(section: "General", key: "ExitOnShift",
            defaultValue: true, description: "When 'SAVE AND QUIT' button with Shift modifier is pressed, the game immediately exits"
        );

        Utils.AddLocalizationText("AUTOSAVE_TWEAKS_QUIT", "QUIT WITHOUT SAVE");
        Utils.AddLocalizationText("AUTOSAVE_TWEAKS_SAVE", "SAVE AND QUIT");
        Utils.AddLocalizationText("AUTOSAVE_TWEAKS_BACK", "BACK");

        var harmony = new Harmony(Info.Metadata.GUID);
        harmony.PatchAll(typeof(AutosaveTweaks));
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SMain), nameof(SMain.OnApplicationQuit))]
    private static void SMain_OnApplicationQuit() {
        if (configEnableRecoverySave.Value && SGame.Playing && !ScreenOnQuitPopup.hasBeenActive && Time.time - lastSaveTime > configUnsavedTimeThreshold.Value) {
            SDataSave.Inst.Save(SDataSave.SaveType.Named, $"RecoverySave");
        }
        ScreenOnQuitPopup.hasBeenActive = false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SGameStartEnd), nameof(SGameStartEnd.StartGame_FinalStep))]
    private static void SGameStartEnd_StartGame_FinalStep() {
        lastSaveTime = Time.time;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SGameStartEnd), nameof(SGameStartEnd.SaveGame))]
    private static void SGameStartEnd_SaveGame() {
        if (ScreenOnQuitPopup.hasBeenActive) {
            ScreenOnQuitPopup.hasBeenActive = false;
            SScreenPause.Inst.OnQuitConfirmed();
        }
        lastSaveTime = Time.time;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SScreen), nameof(SScreen.InitScreens))]
    private static void SScreen_InitScreens() {
        var screensParent = GameObject.Find("_Screens").transform;

        var screenObj = new GameObject("150_ScreenOnQuitPopup");
        screenObj.transform.SetParent(screensParent, worldPositionStays: false);

        screenObj.AddComponent<ScreenOnQuitPopup>();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(SScreenPause), nameof(SScreenPause.OnUpdate))]
    private static IEnumerable<CodeInstruction> SScreenPause_OnUpdate(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        static void OnQuit() {
            if (!configShowQuitConfirmation.Value) {
                SScreenPause.Inst.OnQuitConfirmed();
                return;
            }

            if (Time.time - lastSaveTime > configUnsavedTimeThreshold.Value) {
                ScreenOnQuitPopup.Inst.Show();
            } else {
                SScreenPause.Inst.OnQuitConfirmed();
            }
        }

        return new CodeMatcher(instructions, generator).End()
            .MatchBack(useEnd: false,
                new(OpCodes.Call, typeof(SSingletonScreen<SScreenPopup>).Method("get_Inst")),
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldftn, typeof(SScreenPause).Method("OnQuitConfirmed")),
                new(OpCodes.Newobj),
                new(OpCodes.Ldstr, "QUIT_CONFIRM"),
                new(OpCodes.Ldstr, "COMMON_OK"),
                new(OpCodes.Ldstr, "COMMON_CANCEL"),
                new(OpCodes.Ldc_I4_0),
                new(OpCodes.Ldnull),
                new(OpCodes.Ldnull),
                new(OpCodes.Callvirt, typeof(SScreenPopup).Method("Show")))
            .ThrowIfInvalid("(1)")
            .RemoveInstructions(11)
            .Insert(Transpilers.EmitDelegate(OnQuit))
            .Instructions();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SDataSave), nameof(SDataSave.GetAutosaveName))]
    private static bool SDataSave_GetAutosaveName(SDataSave __instance, SDataSave.SaveType saveType, out string __result) {
        bool isQuicksave = saveType == SDataSave.SaveType.QuickSave;
        string baseName = isQuicksave ? "QuickSave" : "AutoSave";
        int maxSlots = isQuicksave ? configQuicksaveSlots.Value : configAutosaveSlots.Value;
        // maxSlots should never be 0

        bool IsValidSaveName(FileInfo fileInfo) {
            string fileName = fileInfo.Name;
            if (fileName.Length != baseName.Length + 1 + ".save".Length) { return false; }
            char slotLetter = fileName[baseName.Length];
            return slotLetter >= 'A' && slotLetter < (char)('A' + maxSlots);
        }
        FileInfo[] filesByDate = Array.FindAll(__instance.GetFilesByDate(baseName + "?.save"), IsValidSaveName);
        if (filesByDate.Length >= maxSlots && filesByDate.Length > 0) {
            __result = Path.GetFileNameWithoutExtension(filesByDate[0].Name);
            return false;
        }
        bool[] usedLetters = new bool[maxSlots];
        foreach (FileInfo file in filesByDate) {
            char slotLetter = file.Name[baseName.Length];
            usedLetters[slotLetter - 'A'] = true;
        }
        int freeSlot = Array.IndexOf<bool>(usedLetters, false);
        if (freeSlot != -1) {
            __result = baseName + (char)('A' + freeSlot);
            return false;
        }
        // fallback
        if (filesByDate.Length > 0) {
            __result = Path.GetFileNameWithoutExtension(filesByDate[0].Name);
            return false;
        }
        __result = baseName + 'A';
        return false;
    }
}

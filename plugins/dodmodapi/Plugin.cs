
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace DODModAPI;

[BepInPlugin(GUID, ThisPluginInfo.Name, ThisPluginInfo.Version)]
public class DODModAPIPlugin : BaseUnityPlugin {
    internal static ManualLogSource Log { get; private set; } = null!;

    public const string GUID = "dodmodapi";
    public const string Version = ThisPluginInfo.Version;

    private void Awake() {
        Log = base.Logger;

        var harmony = new Harmony(Info.Metadata.GUID);
        harmony.PatchAll(typeof(ModeManager.Patches));
        harmony.PatchAll(typeof(CommandManager.Patches));
        harmony.PatchAll(typeof(ScreenManager.Patches));
        harmony.PatchAll(typeof(SpriteManager.Patches));
        harmony.PatchAll(typeof(ItemManager.Patches));
        harmony.PatchAll(typeof(SurfaceManager.Patches));
        harmony.PatchAll(typeof(UnitManager.Patches));
        harmony.PatchAll(typeof(SaveManager.Patches));
        harmony.PatchAll(typeof(EventManager.Patches));
        harmony.PatchAll(typeof(NetworkManager.Patches));
    }
}

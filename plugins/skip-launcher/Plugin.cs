using BepInEx;
using HarmonyLib;
using DODModAPI.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using DODModAPI;

[BepInPlugin("skip-launcher", ThisPluginInfo.Name, ThisPluginInfo.Version)]
[BepInDependency(DODModAPIPlugin.GUID)]
public class SkipLauncher : BaseUnityPlugin {
    private static int resolutionWidth;
    private static int resolutionHeight;
    private static bool isFullscreen;

    private void Awake() {
        var configEnable = Config.Bind<bool>(section: "General", key: "Enable",
            defaultValue: true, description: "Enables the plugin"
        );
        var configResolutionWidth = Config.Bind<int>(section: "Startup", key: "ResolutionWidth",
            defaultValue: SDataOptions.ResX.Value, description: "Screen width applied at startup. Invalid screen resolutions will auto-reset to current monitor resolution"
        );
        resolutionWidth = configResolutionWidth.Value;
        var configResolutionHeight = Config.Bind<int>(section: "Startup", key: "ResolutionHeight",
            defaultValue: SDataOptions.ResY.Value, description: "Screen height applied at startup. Invalid screen resolutions auto-reset to current monitor resolution"
        );
        resolutionHeight = configResolutionHeight.Value;
        isFullscreen = Config.Bind<bool>(section: "Startup", key: "Fullscreen",
            defaultValue: SDataOptions.Fullscreen.Value, description: "Fullscreen mode applied at startup"
        ).Value;

        if (!Screen.resolutions.Any(res => res.width == resolutionWidth && res.height == resolutionHeight)) {
            var currScreenRes = Screen.currentResolution;

            Logger.LogWarning($"Invalid screen resolution: {resolutionWidth}x{resolutionHeight}; resetting to {currScreenRes.width}x{currScreenRes.height}");

            configResolutionWidth.Value = currScreenRes.width;
            configResolutionHeight.Value = currScreenRes.height;
            resolutionWidth = currScreenRes.width;
            resolutionHeight = currScreenRes.height;
        }

        if (!configEnable.Value) { return; }

        var harmony = new Harmony(Info.Metadata.GUID);
        harmony.PatchAll(typeof(SkipLauncher));
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(SMain), nameof(SMain.InitApp))]
    private static IEnumerable<CodeInstruction> SMain_InitApp(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        static void OnScreenLauncherActivate() {
            SDataOptions.ResX.Value = resolutionWidth;
            SDataOptions.ResY.Value = resolutionHeight;
            SDataOptions.Fullscreen.Value = isFullscreen;

            SScreenOptions.Inst.SetDisplayParams();
            SScreenHome.Inst.Activate();
        }
        return new CodeCursor(instructions, generator)
            .MoveToEnd()
            .FindPrevious(
                new(OpCodes.Call, typeof(SSingletonScreen<SScreenLauncher>).Method("get_Inst")),
                new(OpCodes.Ldc_I4_0),
                new(OpCodes.Callvirt, typeof(SScreen).Method("Activate")))
            .Replace(Transpilers.EmitDelegate(OnScreenLauncherActivate), out _)
            .Remove(2)
            .Finish();
    }
}

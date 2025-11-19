using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;

internal static class AssertPatch {
    [HarmonyPatch(typeof(SMisc), nameof(SMisc.Assert), [typeof(bool), typeof(string)])]
    [HarmonyPrefix]
    private static bool SMisc_Assert(bool test, string errorMessage) {
        if (test) { return false; }
        EnableAsserts.AssertFail(errorMessage);
        return true;
    }
    [HarmonyPatch(typeof(SMisc), nameof(SMisc.Assert), [typeof(bool)])]
    [HarmonyPrefix]
    private static bool SMisc_Assert(bool test) {
        if (test) { return false; }
        EnableAsserts.AssertFail("Assertation failed");
        return true;
    }
}

[BepInPlugin("enable-asserts", ThisPluginInfo.Name, ThisPluginInfo.Version)]
public class EnableAsserts : BaseUnityPlugin {
    public static ManualLogSource Log = null;
    public static ConfigEntry<bool> configIsFatal = null;

    public static void AssertFail(string message) {
        var stacktrace = new System.Diagnostics.StackTrace(3, true);
        Log.LogFatal($"{message}\n{stacktrace.ToString()}");
        if (configIsFatal.Value) {
            Environment.Exit(1);
        }
    }

    private void Awake() {
        Log = base.Logger;

        configIsFatal = Config.Bind<bool>(section: "General", key: "IsFatal", defaultValue: true);

        var harmony = new Harmony(Info.Metadata.GUID);
        harmony.PatchAll(typeof(AssertPatch));
    }
}


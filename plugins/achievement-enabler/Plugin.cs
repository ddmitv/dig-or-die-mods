using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using DODModAPI.Extensions;

internal static class WithEventsPatch {
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SGameStartEnd), nameof(SGameStartEnd.StartNewGame_Coroutine))]
    private static void SGameStartEnd_StartNewGame_Coroutine_Prefix(out bool __state) {
        __state = SOutgame.Params.m_eventsActive;
        SOutgame.Params.m_eventsActive = false;
    }
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SGameStartEnd), nameof(SGameStartEnd.StartNewGame_Coroutine))]
    private static IEnumerator SGameStartEnd_StartNewGame_Coroutine_Postfix(IEnumerator result, bool __state) {
        yield return result;

        SOutgame.Params.m_eventsActive = __state;
    }
}
internal static class InCustomModePatch {
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SGameStartEnd), nameof(SGameStartEnd.StartNewGame_Coroutine))]
    private static void SGameStartEnd_StartNewGame_Coroutine_Prefix(out string __state) {
        __state = SOutgame.Mode.m_name;
        SOutgame.Mode.m_name = "Solo";
    }
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SGameStartEnd), nameof(SGameStartEnd.StartNewGame_Coroutine))]
    private static IEnumerator SGameStartEnd_StartNewGame_Coroutine_Postfix(IEnumerator result, string __state) {
        yield return result;

        SOutgame.Mode.m_name = __state;
    }
}

[BepInPlugin("achievement-enabler", ThisPluginInfo.Name, ThisPluginInfo.Version)]
[BepInDependency(DODModAPI.DODModAPIPlugin.GUID)]
public class AchievementEnabler : BaseUnityPlugin
{
    private static ConfigEntry<bool> configInMultiplayer = null;
    private static ConfigEntry<bool> configAfterCheats = null;
    private static ConfigEntry<bool> configInPostGameAlways = null;

    private void Awake() {
        var configEnabled = Config.Bind<bool>(
            section: "General", key: "Enabled", defaultValue: true,
            description: "Enables the plugin"
        );
        configInMultiplayer = Config.Bind<bool>(
            section: "EnableAchievements", key: "InMultiplayer", defaultValue: true,
            description: "Makes achievements accessible in multiplayer"
        );
        configAfterCheats = Config.Bind<bool>(
            section: "EnableAchievements", key: "AfterCheats", defaultValue: false,
            description: "Makes achievements accessible after using /event and /param commands, modified params in Solo/Multi modes, or starting with cheats/events active"
        );
        var configWithEvents = Config.Bind<bool>(
            section: "EnableAchievements", key: "WithEvents", defaultValue: true,
            description: "Makes achievements accessible after starting a game with events enabled"
        );
        var configInCustomMode = Config.Bind<bool>(
            section: "EnableAchievements", key: "InCustomMode", defaultValue: false,
            description: "Makes achievements accessible in custom game modes (not only in Solo/Multi modes)"
        );
        configInPostGameAlways = Config.Bind<bool>(
            section: "EnableAchievements", key: "InPostGameAlways", defaultValue: false,
            description: "Force enables the achievements in post game even if `skipInPostGame` parameter (in SSteamStats.SetStat) is `true`"
        );
        if (!configEnabled.Value) { return; }

        var harmony = new Harmony(Info.Metadata.GUID);
        harmony.PatchAll(typeof(AchievementEnabler));

        if (configWithEvents.Value) {
            harmony.PatchAll(typeof(WithEventsPatch));
        }
        if (configInCustomMode.Value) {
            harmony.PatchAll(typeof(InCustomModePatch));
        }
    }

    [HarmonyPatch(typeof(SSteamStats), nameof(SSteamStats.SetStat))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> SSteamStats_SetStat(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        return new DODModAPI.CodeCursor(instructions, generator)
            .When(configInMultiplayer.Value, c => c
                .MoveToStart()
                .FindNext(out uint n,
                    // if (... && !SNetwork.IsMulti(false))
                    new(OpCodes.Ldc_I4_0),
                    new(OpCodes.Call, typeof(SNetwork).Method("IsMulti")),
                    new(OpCodes.Brtrue))
                .Remove(n))
            .When(configAfterCheats.Value, c => c
                .MoveToStart()
                .FindNext(out uint n,
                    // if (... && !GVars.m_achievementsLocked && ...)
                    new(OpCodes.Ldsfld, typeof(GVars).StaticField("m_achievementsLocked")),
                    new(OpCodes.Brtrue))
                .Remove(n))
            .When(configInPostGameAlways.Value, c => c
                .MoveToStart()
                .FindNext(out uint n,
                    // if (... && (!skipInPostGame || !GVars.m_postGame) ...)
                    new(OpCodes.Ldarg_2), // parameter `skipInPostGame`
                    new(OpCodes.Brfalse),

                    new(OpCodes.Ldsfld, typeof(GVars).StaticField("m_postGame")),
                    new(OpCodes.Brtrue))
                .Remove(n))
            .Finish();
    }
}

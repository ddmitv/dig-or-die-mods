using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using ModUtils.Extensions;

[BepInPlugin("achievement-enabler", "Achievement Enabler", "1.0.0")]
public class AchievementEnabler: BaseUnityPlugin
{
    private static ConfigEntry<bool> configInMultiplayer = null;
    private static ConfigEntry<bool> configAfterCommand = null;
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
        configAfterCommand = Config.Bind<bool>(
            section: "EnableAchievements", key: "AfterCommand", defaultValue: false,
            description: "Makes achievements accessible even after using some of the commands"
        );
        configInPostGameAlways = Config.Bind<bool>(
            section: "EnableAchievements", key: "InPostGameAlways", defaultValue: false,
            description: "Force enables the achievements in post game even if `skipInPostGame` parameter (in SSteamStats.SetStat) is `true`"
        );
        if (!configEnabled.Value) { return; }

        var harmony = new Harmony("achievement-enabler");
        harmony.PatchAll(typeof(AchievementEnabler));
    }

    [HarmonyPatch(typeof(SSteamStats), nameof(SSteamStats.SetStat))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> SSteamStats_SetStat(IEnumerable<CodeInstruction> instructions) {
        var codeMatcher = new CodeMatcher(instructions);

        if (configInMultiplayer.Value) {
            codeMatcher.Start()
                .MatchForward(useEnd: false,
                    // if (... && !SNetwork.IsMulti(false))
                    new(OpCodes.Ldc_I4_0),
                    new(OpCodes.Call, typeof(SNetwork).Method("IsMulti")),
                    new(OpCodes.Brtrue))
                .ThrowIfInvalid("(1)")
                .RemoveInstructions(3);
        }
        if (configAfterCommand.Value) {
            codeMatcher.Start()
                .MatchForward(useEnd: false,
                    // if (... && !GVars.m_achievementsLocked && ...)
                    new(OpCodes.Ldsfld, typeof(GVars).StaticField("m_achievementsLocked")),
                    new(OpCodes.Brtrue))
                .ThrowIfInvalid("(2)")
                .RemoveInstructions(2);
        }
        if (configInPostGameAlways.Value) {
            codeMatcher.Start()
                .MatchForward(useEnd: false,
                    new(OpCodes.Ldarg_2), // parameter `skipInPostGame`
                    new(OpCodes.Brfalse),

                    new(OpCodes.Ldsfld, typeof(GVars).StaticField("m_postGame")),
                    new(OpCodes.Brtrue))
                .ThrowIfInvalid("(3)")
                .RemoveInstructions(4);
        }
        return codeMatcher.Instructions();
    }
}

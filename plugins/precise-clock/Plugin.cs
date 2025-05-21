using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using ModUtils;
using ModUtils.Extensions;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection.Emit;
using UnityEngine;

public static class PreciseTimePatch {
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(SScreenMessages), nameof(SScreenMessages.OnUpdate))]
    private static IEnumerable<CodeInstruction> SScreenMessages_OnUpdate(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        static string ChangeEnvExpectedStr(string str) {
            var eventStartDelta = SEnvironment.GetEventStartTime() - GVars.SimuTime;
            var timeStr = PreciseClock.ToTimeString(
                (GVars.m_clock + eventStartDelta / SOutgame.Params.m_dayDurationTotal) % 1f
            );
            return $"{str} ({timeStr})";
        }
        static string ChangeEnvOnGoingStr(string str) {
            var eventStartDelta = SEnvironment.GetEventEndTime() - GVars.SimuTime;
            var timeStr = PreciseClock.ToTimeString(
                (GVars.m_clock + eventStartDelta / SOutgame.Params.m_dayDurationTotal) % 1f
            );
            return $"{str} ({timeStr})";
        }

        var codeMatcher = new CodeMatcher(instructions);

        codeMatcher.Start()
            .MatchForward(useEnd: false,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(SScreenMessages).Field("m_guiEnvironment")),
                new(OpCodes.Ldstr, "ENV_EXPECTED"),
                new(OpCodes.Ldc_I4_0),
                new(OpCodes.Ldstr, "ENV_"),
                new(OpCodes.Ldloc_0),
                new(OpCodes.Ldfld, typeof(CDesc).Field("m_id")))
            .ThrowIfInvalid("(1)")
            .Advance(27)
            .Insert(Transpilers.EmitDelegate(ChangeEnvExpectedStr))
            .MatchForward(useEnd: false,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(SScreenMessages).Field("m_guiEnvironment")),
                new(OpCodes.Ldstr, "ENV_ONGOING"),
                new(OpCodes.Ldc_I4_0),
                new(OpCodes.Ldstr, "ENV_"),
                new(OpCodes.Ldloc_0),
                new(OpCodes.Ldfld, typeof(CDesc).Field("m_id")))
            .ThrowIfInvalid("(2)")
            .Advance(27)
            .Insert(Transpilers.EmitDelegate(ChangeEnvOnGoingStr));

        return codeMatcher.Instructions();
    }


    private static CGuiText txtClockTime = null;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SScreenHud), nameof(SScreenHud.OnInit))]
    private static void SScreenHud_OnInit(SScreenHud __instance) {
        int2 pos = PreciseClock.configClockPosition.Value switch {
            ClockPosition.Bottom => new int2(-155, 20),
            ClockPosition.AboveTimer => new int2(-72, 154),
            _ => throw new InvalidEnumArgumentException(),
        };

        txtClockTime = new CGuiText(
            screen: __instance,
            parent: __instance.m_guiRoot,
            parentAnchor: EAnchor.LowerRight,
            x: pos.x, y: pos.y,
            textSize: 25f, shadow: true
        ) {
            m_textColor = PreciseClock.configColor.Value
        };
        if (PreciseClock.configClockPosition.Value == ClockPosition.AboveTimer) {
            __instance.m_txtWarning.m_y += 27;
        }

    }
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SScreenHud), nameof(SScreenHud.OnUpdate))]
    private static void SScreenHud_OnUpdate(SScreenHud __instance) {
        txtClockTime.SetVisible(!SGame.HideUI);
        txtClockTime.m_str.Set(PreciseClock.ToTimeString(GVars.m_clock));
        txtClockTime.OnUpdate();
    }
}

public enum ClockPosition {
    Bottom,
    AboveTimer,
}
public enum TimeFormat {
    H12AmPm,
    H24,
    Decimal,
}

[BepInPlugin("precise-clock", "Precise Clock", "1.0.0")]
public class PreciseClock : BaseUnityPlugin {
    public static ConfigEntry<UnityEngine.Color> configColor = null;
    public static ConfigEntry<ClockPosition> configClockPosition = null;
    private static ConfigEntry<TimeFormat> configTimeFormat = null;

    public static string ToTimeString(float clock) {
        if (clock < 0f) { clock += 1f; }

        if (configTimeFormat.Value == TimeFormat.H12AmPm) {
            var hour = clock * 24f;
            return $"{Utils.PosMod((int)hour - 1, 12) + 1}:{(int)((hour % 1f) * 60f):00} {(hour < 12f ? "AM" : "PM")}";
        } else if (configTimeFormat.Value == TimeFormat.Decimal) {
            return $"{(int)(clock * 10f)}.{(int)(clock * 1000f) % 100:00} ({clock})";
        } else {
            var hour = clock * 24f;
            return $"{(int)hour}:{(int)((hour % 1f) * 60f):00}";
        }
    }

    private void Start() {
        configColor = Config.Bind<UnityEngine.Color>(
            section: "General", key: "Color", defaultValue: GColors.m_textGray170.Color,
            description: "The color of the text used by the clock"
        );
        configClockPosition = Config.Bind<ClockPosition>(
            section: "General", key: "ClockPosition", defaultValue: ClockPosition.Bottom,
            description: "Location of the clock"
        );
        configTimeFormat = Config.Bind<TimeFormat>(
            section: "General", key: "TimeFormat", defaultValue: TimeFormat.H24,
            description: "Format of the time in clock"
        );
        var configEnabled = Config.Bind<bool>(
            section: "General", key: "Enabled", defaultValue: true
        );
        if (!configEnabled.Value) { return; }

        var harmony = new Harmony("precise-clock");
        harmony.PatchAll(typeof(PreciseTimePatch));
    }
}


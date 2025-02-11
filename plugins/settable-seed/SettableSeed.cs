using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using ModUtils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;

public static class InputSeedPatch {
    private static CGuiOptionInput multiGuiSeed = null;
    private static CGuiOptionInput singleGuiSeed = null;

    [HarmonyPatch(typeof(SScreenChooseMultiOptions), nameof(SScreenChooseMultiOptions.OnInit))]
    [HarmonyPostfix]
    private static void SScreenChooseMultiOptions_OnInit(SScreenChooseMultiOptions __instance) {
        multiGuiSeed = new CGuiOptionInput(__instance, __instance.m_guiRoot, EAnchor.Center, x: 750, y: 200,
            labelTextId: "OPTIONS_SEED", labelWidth: 120, controlWidth: 300
        );
    }
    [HarmonyPatch(typeof(SScreenChooseMultiOptions), nameof(SScreenChooseMultiOptions.Refresh))]
    [HarmonyPostfix]
    private static void SScreenChooseMultiOptions_Refresh() {
        multiGuiSeed.RefreshLabelText();
    }
    [HarmonyPatch(typeof(SScreenChooseMultiOptions), nameof(SScreenChooseMultiOptions.StartGame))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> SScreenChooseMultiOptions_StartGame(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        static bool SetSeed(CParams cparams) {
            int? seed = SettableSeed.SeedFromString(multiGuiSeed.Input.m_text);
            if (seed is null) {
                return false;
            }
            cparams.m_seed = (int)seed;
            return true;
        }
        var codeMatcher = new CodeMatcher(instructions, generator);

        codeMatcher.Start()
            .MatchForward(useEnd: false,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(SScreenChooseMultiOptions).GetField("m_startCheated")),
                new(OpCodes.Brfalse))
            .ThrowIfNotMatch("(1)")
            .CreateLabel(out Label skipRet)
            .Insert(
                new(OpCodes.Ldloc_1),
                Transpilers.EmitDelegate(SetSeed),
                new(OpCodes.Brtrue, skipRet),
                new(OpCodes.Ret)
            );

        return codeMatcher.Instructions();
    }

    [HarmonyPatch(typeof(SScreenChooseDifficulty), nameof(SScreenChooseDifficulty.OnInit))]
    [HarmonyPostfix]
    private static void SScreenChooseDifficulty_OnInit(SScreenChooseDifficulty __instance) {
        singleGuiSeed = new CGuiOptionInput(__instance, __instance.m_guiRoot, EAnchor.Center, x: 0, y: 400,
            labelTextId: "OPTIONS_SEED", labelWidth: 400, controlWidth: 400
        );
    }
    [HarmonyPatch(typeof(SScreenChooseDifficulty), nameof(SScreenChooseDifficulty.Refresh))]
    [HarmonyPostfix]
    private static void SScreenChooseDifficulty_Refresh() {
        singleGuiSeed.RefreshLabelText();
    }
    [HarmonyPatch(typeof(SScreenChooseDifficulty), nameof(SScreenChooseDifficulty.StartNewGame))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> SScreenChooseDifficulty_StartGame(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        static bool SetSeed() {
            int? seed = SettableSeed.SeedFromString(singleGuiSeed.Input.m_text);
            if (seed is null) {
                return false;
            }
            SOutgame.Params.m_seed = (int)seed;
            return true;
        }
        var codeMatcher = new CodeMatcher(instructions, generator);

        codeMatcher.Start()
            .MatchForward(useEnd: false,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(SScreenChooseDifficulty).GetField("m_startCheated")),
                new(OpCodes.Brfalse))
            .ThrowIfNotMatch("(1)")
            .CreateLabel(out Label skipRet)
            .Insert(
                Transpilers.EmitDelegate(SetSeed),
                new(OpCodes.Brtrue, skipRet),
                new(OpCodes.Ret)
            );

        return codeMatcher.Instructions();
    }

    [HarmonyPatch(typeof(SGameStartEnd), nameof(SGameStartEnd.StartNewGame_Coroutine), MethodType.Enumerator)]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> SGameStartEnd_StartNewGame_Coroutine(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var codeMatcher = new CodeMatcher(instructions, generator);

        codeMatcher.Start()
            .MatchForward(useEnd: false,
                new(OpCodes.Call, typeof(SOutgame).GetMethod("get_Params")),
                new(OpCodes.Ldc_I4_0),
                new(OpCodes.Ldc_I4, 100000),
                new(OpCodes.Call, typeof(UnityEngine.Random).GetMethod("Range", [typeof(int), typeof(int)])),
                new(OpCodes.Stfld, typeof(CParams).GetField("m_seed")))
            .ThrowIfNotMatch("(1)")
            .CollapseInstructions(5)
            .Insert(
                new(OpCodes.Call, typeof(SOutgame).GetMethod("get_Params")),
                new(OpCodes.Ldfld, typeof(CParams).GetField("m_seed")),
                new(OpCodes.Call, typeof(UnityEngine.Random).GetMethod("InitState")));

        return codeMatcher.Instructions();
    }
}

[BepInPlugin("settable-seed", "Settable Seed", "0.0.0")]
public class SettableSeed : BaseUnityPlugin
{
    private static ConfigEntry<int> configMaxSeed = null;

    private static void AddLocText(string id, string text) {
        SSingleton<SLoc>.Inst.m_dico.Add(id, new SLoc.CSentence(id, text));
    }
    public static int? SeedFromString(string str) {
        if (str.Length == 0) {
            return UnityEngine.Random.Range(0, configMaxSeed.Value);
        }
        if (int.TryParse(str, out int seed) && seed < configMaxSeed.Value) {
            return seed;
        } else {
            return null;
        }
    }

    private void Start() {
        configMaxSeed = Config.Bind<int>(section: "General", key: "MaxSeed", defaultValue: 100000,
            new ConfigDescription("", new AcceptableValueRange<int>(0, int.MaxValue))
        );

        AddLocText("OPTIONS_SEED", "Seed:");

        var harmony = new Harmony("settable-seed");
        harmony.PatchAll(typeof(InputSeedPatch));
    }
}


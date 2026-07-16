using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using DODModAPI;
using DODModAPI.Extensions;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

internal static class HpMaxPatch {
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(CUnitPlayer.CDesc), nameof(CUnitPlayer.CDesc.GetHpMax))]
    private static IEnumerable<CodeInstruction> CUnitPlayer_CDesc_GetHpMax(IEnumerable<CodeInstruction> instructions) {
        return [
            new(OpCodes.Ldc_R4, UltraHardcorePlugin.configPlayerHpMax.Value),
            new(OpCodes.Ret)
        ];
    }
}
internal static class PermanentMistPatch {
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(CBackground), nameof(CBackground.DrawBackgrounds))]
    [HarmonyPatch(typeof(SDrawWorld), nameof(SDrawWorld.OnUpdate))]
    [HarmonyPatch(typeof(SWorld), nameof(SWorld.ProcessLighting_DynamicUnits))]
    private static IEnumerable<CodeInstruction> AlwaysMistTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        return new CodeCursor(instructions, generator)
            .FindNextEnd(
                new(OpCodes.Call, typeof(SEnvironment).Method("GetEnvironmentCurrent")),
                new(OpCodes.Ldfld, typeof(CEnvironment).Field("m_mist")),
                new(OpCodes.Brfalse))
            .Advance(-1)
            .Replace(OpCodes.Pop)
            .FindNext(
                new(OpCodes.Call, typeof(SEnvironment).Method("GetEnvironmentCurrent")),
                new(OpCodes.Ldc_R4, 5f),
                new(OpCodes.Callvirt, typeof(CEnvironment).Method("GetBeginEndSmoothingValue")))
            .Replace(OpCodes.Ldc_R4, 1f)
            .Remove(2)
            .Finish();
    }
}
internal static class PermanentDarknessPatch {
    // [HarmonyTranspiler]
    // [HarmonyPatch(typeof(CBackground), nameof(CBackground.DrawBackgroundParralax))]
    // [HarmonyPatch(typeof(CBackground), nameof(CBackground.DrawBackgrounds))]
    // [HarmonyPatch(typeof(CParticleGroup), nameof(CParticleGroup.EmitNb))]
    // [HarmonyPatch(typeof(SWorldDll), "ProcessLightingSquare")]
    // private static IEnumerable<CodeInstruction> PermanentDarknessTranspiler(IEnumerable<CodeInstruction> instructions) {
    //     var codeMatcher = new CodeMatcher(instructions);
    // 
    //     codeMatcher.Start()
    //         .MatchForward(useEnd: false,
    //             new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(G), nameof(G.m_sunLight))))
    //         .ThrowIfInvalid("(1)")
    //         .Set(OpCodes.Ldc_R4, 0f);
    // 
    //     return codeMatcher.Instructions();
    // }
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SWorld), nameof(SWorld.UpdateLightSunValue))]
    private static void SWorld_UpdateLightSunValue() {
        G.m_sunLight = 0f;
    }
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(CModeSolo), nameof(CModeSolo.CreateInitialPlayerItems))]
    private static IEnumerable<CodeInstruction> CModeSolo_CreateInitialPlayerItems(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        return new CodeCursor(instructions, generator)
            .FindNext(
                new(OpCodes.Ldarg_1),
                new(OpCodes.Ldfld, typeof(CPlayer).Field("m_inventory")),
                new(OpCodes.Ldsfld, typeof(GItems).StaticField("gunRifle")))
            .Insert(
                new CodeInstruction(OpCodes.Ldarg_1),
                Transpilers.EmitDelegate(static (CPlayer player) => {
                    player.m_inventory.AddToInventory(GItems.lightSun);
                }))

            .MoveToStart()
            .FindNext(
                new(OpCodes.Ldsfld, typeof(GItems).StaticField("gunRifle")),
                new(OpCodes.Ldc_R4, 1f))
            .Insert(
                Transpilers.EmitDelegate(static () => {
                    SPickups.CreatePickup(GItems.lightSun, nb: 1f, pos: G.m_player.PosCenter + 6.5f * Vector2.right, withSpeed: false);
                }))
            .Finish();
    }
}
internal static class NoRainPatch {
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(SWorld), nameof(SWorld.OnUpdateSimu))]
    private static IEnumerable<CodeInstruction> SWorld_OnUpdateSimu(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        return new CodeCursor(instructions, generator)
            .FindNext(new CodeInstruction(OpCodes.Ldc_I4_1))
            .Replace(OpCodes.Ldc_I4_0)
            .Finish();
    }
}
internal static class InverseNightPatch {
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SGame), nameof(SGame.IsNight))]
    private static void SGame_IsNight(ref bool __result) {
        __result = !__result;
    }
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SGame), nameof(SGame.GetNightDurationLeft))]
    private static bool SGame_GetNightDurationLeft(ref float __result) {
        __result = 10f;
        return false;
    }
}
internal static class PermanentAcidWaterPatch {
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(CUnit), nameof(CUnit.Update))]
    private static IEnumerable<CodeInstruction> CUnit_Update(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        return new CodeCursor(instructions, generator)
            .FindNext(
                new(OpCodes.Call, typeof(SEnvironment).Method("GetEnvironmentCurrent")),
                new(OpCodes.Ldfld, typeof(CEnvironment).Field("m_acidWater")),
                new(OpCodes.Brfalse))
            .RemovePreservingLabels(3)
            .Finish();
    }
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(CEnvironment), nameof(CEnvironment.GetWaterAcidRatio))]
    private static IEnumerable<CodeInstruction> CEnvironment_GetWaterAcidRatio(IEnumerable<CodeInstruction> instructions) {
        return [
            new(OpCodes.Ldc_R4, 1f),
            new(OpCodes.Ret)
        ];
    }
}
internal static class NoRegenerationPatch {
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CUnitPlayer), nameof(CUnitPlayer.Update))]
    private static void CUnitPlayer_Update(CUnitPlayer __instance) {
        __instance.m_uDesc.m_regenSpeed = 0f;
    }
}
internal static class NoQuickSavesPatch {
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SGameStartEnd), nameof(SGameStartEnd.SaveGame))]
    private static bool SGameStartEnd_SaveGame(SDataSave.SaveType saveType) {
        if (saveType == SDataSave.SaveType.QuickSave) {
            return false;
        }
        return true;
    }
}
internal static class ContinuousEventsPatch {
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(SEnvironment), nameof(SEnvironment.OnUpdateSimu))]
    private static IEnumerable<CodeInstruction> SEnvironment_OnUpdateSimu(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        static void RemoveImpossibleEvents(List<CEnvironment> events) {
            bool hasPermanentMist = UltraHardcorePlugin.configPermanentMist.Value;
            bool hasPermanentAcidWater = UltraHardcorePlugin.configPermanentAcidWater.Value;
            events.RemoveAll(evnt => {
                if (hasPermanentMist && evnt.m_id == "mist") { return true; }
                if (hasPermanentAcidWater && evnt.m_id == "acidWater") { return true; }

                if (evnt.m_isDayEnv && SGame.IsNight()) {
                    return true;
                }
                if (evnt.m_isNightEnv && !SGame.IsNight()) {
                    return true;
                }
                return false;
            });
        }

        return new CodeCursor(instructions, generator)
            // list[global::UnityEngine.Random.Range(0, list.Count)];
            .FindNext(
                new(OpCodes.Ldloc_S),
                new(OpCodes.Ldloc_S),
                new(OpCodes.Ldc_I4_0),
                new(OpCodes.Ldloc_S),
                new(OpCodes.Callvirt, typeof(List<CEnvironment>).Method("get_Count")),
                new(OpCodes.Call),
                new(OpCodes.Callvirt),
                new(OpCodes.Stfld))
            .GetOperand(offset: 1, out LocalBuilder listVar)
            .Inject(
                new(OpCodes.Ldloc, listVar),
                Transpilers.EmitDelegate(RemoveImpossibleEvents))

            .FindNext(
                new(OpCodes.Ldloc_S),
                new(OpCodes.Ldfld),
                new(OpCodes.Ldfld, typeof(CEnvironment).Field("m_isNightEnv")),
                new(OpCodes.Brfalse))
            .Remove(51)
            .Insert(new(OpCodes.Call, typeof(GVars).Method("get_SimuTime")),
                new(OpCodes.Stsfld, typeof(GVars).StaticField("m_eventStartTime")))
            .Finish();
    }
}
internal static class InstantDrowning {
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(CUnit), nameof(CUnit.Update))]
    private static IEnumerable<CodeInstruction> CUnit_Update(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        // Patch the amount of damage when the player is drowning (m_air <= 0) to maximum possible damage (int.MaxValue)

        return new CodeCursor(instructions, generator)
            .FindNext(
                new(OpCodes.Ldarg_0),
                new(OpCodes.Call, typeof(GVars).Method("get_SimuTime")),
                new(OpCodes.Stfld, typeof(CUnit).Field("m_lastAirHit")),

                new(OpCodes.Ldarg_0),
                new(OpCodes.Isinst, typeof(CUnitPlayer)),
                new(OpCodes.Brfalse),
                new(OpCodes.Ldc_I4_5), // overwrite this to "ldc.i4 int.MaxValue"
                new(OpCodes.Br))
            .Advance(6)
            .Replace(OpCodes.Ldc_I4, int.MaxValue)
            .Finish();
    }
}
// public static class EnemyNoDetourPathing {
//     [HarmonyTranspiler]
//     [HarmonyPatch(typeof(SWorld_PF), nameof(SWorld_PF.AddMove))]
//     private static IEnumerable<CodeInstruction> SWorld_PF_AddMove(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
//         // Patch the enemy pathfinding algorithm, and make them ignore more optimal pathes and go directly through every cell
// 
//         var codeMatcher = new CodeMatcher(instructions, generator);
//         codeMatcher.Start()
//             .MatchForward(useEnd: false,
//                 new(OpCodes.Ldarg_0),
//                 new(OpCodes.Ldfld, typeof(SWorld_PF).GetField("m_grid")),
//                 new(OpCodes.Ldloc_0),
//                 new(OpCodes.Ldloc_1),
//                 new(OpCodes.Call, typeof(CCell[,]).GetMethod("Address")),
//                 new(OpCodes.Call, typeof(CCell).GetMethod("IsPassable")),
//                 new(OpCodes.Brfalse))
//             .ThrowIfInvalid("(1)")
//             .RemoveInstructions(7); // remove these instruction to make every cell behave like it is passble
//         return codeMatcher.Instructions();
//     }
// }
internal static class UnitInstantObservation {
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(CUnitMonster), nameof(CUnitMonster.UpdateTarget))]
    private static IEnumerable<CodeInstruction> CUnitMonster_UpdateTarget(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        return new CodeCursor(instructions, generator)
            .FindNext(
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(CUnitMonster).Field("m_target")),
                new(OpCodes.Brtrue))
            .Remove(2)
            .ReplaceOpcode(offset: 0, OpCodes.Br, out _)

            .FindNext(
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(CUnitMonster).Field("m_target")),
                new(OpCodes.Brtrue),
                // remove these lower instructions
                new(OpCodes.Call, typeof(SUnits).Method("IsThereABossAggressive")),
                new(OpCodes.Brtrue),
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(CUnitMonster).Field("m_isNightSpawn")),
                new(OpCodes.Brfalse))
            .Advance(3)
            .Remove(5)
            .Finish();
    }
}
internal static class HideClockPatch {
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SScreenHud), nameof(SScreenHud.OnUpdate))]
    private static void SScreenHud_OnUpdate(SScreenHud __instance) {
        __instance.m_bmpClock.SetVisible(false);
        __instance.m_bmpClockNeedle.SetVisible(false);
        __instance.m_bmpClockOver.SetVisible(false);
        __instance.m_txtWarning.SetVisible(false);
        __instance.m_txtDays.SetVisible(false);
    }
}
internal static class IngredientMultiplierPatch {
    private static readonly CItem[] excludedIngredients = [
        GItems.bossMadCrabMaterial, GItems.bossMadCrabSonar, GItems.masterGem,
        GItems.lootBalrog, GItems.lootDwellerLord
    ];

    private static void ApplyMult(CStack stack, int mult) {
        if (Array.IndexOf(excludedIngredients, stack.m_item) >= 0) {
            return;
        }
        stack.m_nb *= mult;
    }

    [HarmonyPatch(typeof(SDataLua), nameof(SDataLua.OnInit))]
    [HarmonyPostfix]
    private static void SDataLua_OnInit() {
        // SOutgame.Mode is "Solo"
        int mult = (int)UltraHardcorePlugin.configIngredientMultiplier.Value;
        foreach (CRecipesGroup recipeGroup in SDataLua.GetDescList<CRecipesGroup>("list_recipesgroups")) {
            foreach (CRecipe recipe in recipeGroup.m_recipes) {
                if (!recipe.m_isUpgrade || recipe.m_in1.m_nb > 1) {
                    ApplyMult(recipe.m_in1, mult);
                }
                ApplyMult(recipe.m_in2, mult);
                ApplyMult(recipe.m_in3, mult);
            }
        }
    }
}
internal static class OnEndOfNightPatch {
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CMode), nameof(CMode.OnEndOfNight))]
    private static void CMode_OnEndOfNight() {
        SOutgame.Params.m_monstersDamagesMult *= UltraHardcorePlugin.configMonsterDamageMultPerNight.Value;
        SOutgame.Params.m_monstersHpMult *= UltraHardcorePlugin.configMonsterHpMultPerNight.Value;
    }
}

[BepInPlugin("ultra-hardcore", ThisPluginInfo.Name, ThisPluginInfo.Version)]
[BepInDependency(DODModAPI.DODModAPIPlugin.GUID)]
public class UltraHardcorePlugin : BaseUnityPlugin {
    public static ConfigEntry<float> configPlayerHpMax = null!;
    public static ConfigEntry<bool> configPermanentMist = null!;
    public static ConfigEntry<bool> configPermanentAcidWater = null!;
    public static ConfigEntry<uint> configIngredientMultiplier = null!;
    public static ConfigEntry<float> configMonsterDamageMultPerNight = null!;
    public static ConfigEntry<float> configMonsterHpMultPerNight = null!;

    private void Start() {
        configPlayerHpMax = Config.Bind<float>(
            section: "UltraHardcore", key: "HpMax", defaultValue: 0f,
            configDescription: new ConfigDescription(
                "Maximum health for the player. '0' for default maximum health",
                new AcceptableValueRange<float>(0, float.MaxValue)
            )
        );
        configPermanentMist = Config.Bind<bool>(
            section: "UltraHardcore", key: "PermanentMist", defaultValue: false,
            description: "Makes the mist permanent, regardless of the current events"
        );
        var configPermanentDarkness = Config.Bind<bool>(
            section: "UltraHardcore", key: "PermanentDarkness", defaultValue: false,
            description: "Makes the night permanent, but without monsters. Adds Sun Light to the initial item list"
        );
        var configNoRain = Config.Bind<bool>(
            section: "UltraHardcore", key: "NoRain", defaultValue: false,
            description: "Removes all rains from the game"
        );
        var configInverseNight = Config.Bind<bool>(
            section: "UltraHardcore", key: "InverseNight", defaultValue: false,
            description: "Monsters attack during the day, but stop at night"
        );
        configPermanentAcidWater = Config.Bind<bool>(
            section: "UltraHardcore", key: "PermanentAcidWater", defaultValue: false,
            description: "Makes event 'ACIDIC WATERS' always active"
        );
        var configNoRegeneration = Config.Bind<bool>(
            section: "UltraHardcore", key: "NoRegeneration", defaultValue: false,
            description: "Removes regeneration from the player, so you can only gain health with potions"
        );
        var configNoQuickSaves = Config.Bind<bool>(
            section: "UltraHardcore", key: "NoQuickSaves", defaultValue: false,
            description: "Removes ability to quick save"
        );
        var configContinuousEvents = Config.Bind<bool>(
            section: "UltraHardcore", key: "ContinuousEvents", defaultValue: false,
            description: "Makes events always active. Note that day-only and night-only events will appear only at corresponding time of day"
        );
        var configInstantDrowning = Config.Bind<bool>(
            section: "UltraHardcore", key: "InstantDrowning", defaultValue: false,
            description: "Causes the player to instantly die by drowning (without slow loss of health)"
        );
        // var configEnemyNoDetourPathing = Config.Bind<bool>(
        //     section: "UltraHardcore", key: "EnemyNoDetourPathing", defaultValue: false,
        //     description: "Makes enemies to go ahead through cells ignoring it's durability"
        // );
        var configUnitInstantObservation = Config.Bind<bool>(
            section: "UltraHardcore", key: "UnitInstantObservation", defaultValue: false,
            description: "Makes every unit target closest player regardless of distance between them"
        );
        var configHideClock = Config.Bind<bool>(
            section: "UltraHardcore", key: "HideClock", defaultValue: false,
            description: "Hides the clock"
        );
        configIngredientMultiplier = Config.Bind<uint>(
            section: "UltraHardcore", key: "IngredientMultiplier", defaultValue: 1,
            description: "Multiplies all recipe's ingredients (ignoring unique) by provided number"
        );
        configMonsterDamageMultPerNight = Config.Bind<float>(
            section: "UltraHardcore", key: "MonsterDamageMultPerNight", defaultValue: 1f,
            description: "On the end of night multiplies monster damage by provided value"
        );
        configMonsterHpMultPerNight = Config.Bind<float>(
            section: "UltraHardcore", key: "MonsterHpMultPerNight", defaultValue: 1f,
            description: "On the end of night multiplies monster health by provided value"
        );

        var harmony = new Harmony(Info.Metadata.GUID);

        if (configPlayerHpMax.Value != 0f) {
            harmony.PatchAll(typeof(HpMaxPatch));
        }
        if (configPermanentMist.Value) {
            harmony.PatchAll(typeof(PermanentMistPatch));
        }
        if (configPermanentDarkness.Value) {
            harmony.PatchAll(typeof(PermanentDarknessPatch));
        }
        if (configNoRain.Value) {
            harmony.PatchAll(typeof(NoRainPatch));
        }
        if (configInverseNight.Value) {
            harmony.PatchAll(typeof(InverseNightPatch));
        }
        if (configPermanentAcidWater.Value) {
            harmony.PatchAll(typeof(PermanentAcidWaterPatch));
        }
        if (configNoRegeneration.Value) {
            harmony.PatchAll(typeof(NoRegenerationPatch));
        }
        if (configNoQuickSaves.Value) {
            harmony.PatchAll(typeof(NoQuickSavesPatch));
        }
        if (configContinuousEvents.Value) {
            harmony.PatchAll(typeof(ContinuousEventsPatch));
        }
        if (configInstantDrowning.Value) {
            harmony.PatchAll(typeof(InstantDrowning));
        }
        // if (configEnemyNoDetourPathing.Value) {
        //     harmony.PatchAll(typeof(EnemyNoDetourPathing));
        // }
        if (configUnitInstantObservation.Value) {
            harmony.PatchAll(typeof(UnitInstantObservation));
        }
        if (configHideClock.Value) {
            harmony.PatchAll(typeof(HideClockPatch));
        }
        if (configIngredientMultiplier.Value != 1) {
            harmony.PatchAll(typeof(IngredientMultiplierPatch));
        }
        if (configMonsterDamageMultPerNight.Value != 1f || configMonsterHpMultPerNight.Value != 1f) {
            harmony.PatchAll(typeof(OnEndOfNightPatch));
        }
    }
}

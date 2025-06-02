using HarmonyLib;
using ModUtils;
using ModUtils.Extensions;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

[HarmonyPatch]
internal static class Misc_WorldPatches {
    [HarmonyPatch(typeof(SDrawWorld), nameof(SDrawWorld.DrawElectricLightIFN))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> SDrawWorld_DrawElectricLightIFN(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var codeMatcher = new CodeMatcher(instructions, generator);

        codeMatcher.Start()
            .MatchForward(useEnd: false,
                new(OpCodes.Ldc_I4_0),
                new(OpCodes.Stloc_3),
                new(OpCodes.Br))
            .InjectAndAdvance(OpCodes.Ldloc_0)
            .Insert(
                new(OpCodes.Ldc_I4_S, (sbyte)5),
                new(OpCodes.Call, typeof(Math).Method<int, int>("Min")),
                new(OpCodes.Stloc_0));

        return codeMatcher.Instructions();
    }

    [HarmonyPatch(typeof(SWorld), nameof(SWorld.DoDamageToCell))]
    [HarmonyPrefix]
    private static bool SWorld_DoDamageToCell(ref int2 cellPos, ref bool __result) {
        var content = SWorld.Grid[cellPos.x, cellPos.y].GetContent();
        if ((content is ExtCItem_Explosive citem && citem.indestructible) || content is ExtCItem_IndestructibleMineral) {
            __result = false;
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(SWorldDll), nameof(SWorldDll.ProcessSimu))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> SWorldDll_ProcessSimu(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        static float ComputePlantGrowChange(CItem_Mineral mineral) {
            if (mineral is ExtCItem_FertileMineralDirt fertileDirt) {
                return fertileDirt.plantGrowChange;
            }
            return 0.15f;
        }
        var codeMatcher = new CodeMatcher(instructions, generator);
        codeMatcher.Start()
            .MatchForward(useEnd: false,
                new(OpCodes.Call, typeof(CCell).Method("GetContent")),
                new(OpCodes.Brtrue),
                new(OpCodes.Call, typeof(UnityEngine.Random).Method("get_value")),
                new(OpCodes.Ldc_R4, 0.15f),
                new(OpCodes.Bge_Un))
            .ThrowIfInvalid("(1)")
            .Advance(3)
            .RemoveInstruction()
            .Insert(
                new(OpCodes.Ldloc_S, (byte)8),
                Transpilers.EmitDelegate(ComputePlantGrowChange));
        return codeMatcher.Instructions();
    }
}

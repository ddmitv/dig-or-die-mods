using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace friendly_fire;

public static class CodeMatcherExtensions {
    public static CodeMatcher Substitute(this CodeMatcher self, OpCode opcode, object operand = null) {
        var prevInstruction = self.Instruction.Clone();
        self.SetAndAdvance(opcode, operand);
        self.Insert(prevInstruction);
        return self;
    }
}

[BepInPlugin("friendly-fire", "Friendly Fire", "1.0.0")]
public class Plugin : BaseUnityPlugin
{
    private void Awake()
    {
        Harmony.CreateAndPatchAll(typeof(Plugin));
        System.Console.WriteLine("Plugin \"Friendly Fire\" is loaded!");
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(CBullet), "CheckColWithUnits")]
    private static IEnumerable<CodeInstruction> CBullet_CheckColWithUnits(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var codeMatcher = new CodeMatcher(instructions, generator);

        codeMatcher.Start()
            .MatchForward(useEnd: false,
                new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(Vector2), nameof(Vector2.zero))),
                new CodeMatch(OpCodes.Stloc_S))
            .ThrowIfInvalid("friendly-fire transpiler: Failed to find `call Vector2.zero`, `stloc.s 7`")
            .CreateLabel(out var successLabel);

        codeMatcher.Start()
            .MatchForward(useEnd: true,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(CBullet), "m_unitsHit")),
                new CodeMatch(OpCodes.Ldloc_2),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(List<CUnit>), nameof(List<CUnit>.Contains))),
                new CodeMatch(OpCodes.Brtrue))
            .ThrowIfInvalid("friendly-fire transpiler: Failed to find `ldarg.0`, `ldfld CBullet.m_unitsHit`, `ldloc.2`, `callvirt List.Contains`")
            .Advance(1)
            .Substitute(OpCodes.Ldarg_0)
            .CreateLabel(out var failLabel)
            .Insert(
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CBullet), nameof(CBullet.m_attacker))),
                new CodeInstruction(OpCodes.Isinst, typeof(CUnitPlayer)),
                new CodeInstruction(OpCodes.Brfalse, failLabel), // `m_attacker` is not CUnitPlayer
                new CodeInstruction(OpCodes.Ldloc_2),
                new CodeInstruction(OpCodes.Isinst, typeof(CUnitPlayer)),
                new CodeInstruction(OpCodes.Brtrue, failLabel), // `cunit2` is CUnitPlayer
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CBullet), nameof(CBullet.m_attacker))),
                new CodeInstruction(OpCodes.Ldloc_2),
                new CodeInstruction(OpCodes.Bne_Un, successLabel)); // `m_attacker` != `cunit2`

        return codeMatcher.Instructions();
    }
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(SUnits), nameof(SUnits.DoDamageAOE))]
    private static IEnumerable<CodeInstruction> SUnits_DoDamageAOE(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var codeMatcher = new CodeMatcher(instructions, generator);

        codeMatcher.Start()
            .MatchForward(useEnd: false,
                new CodeMatch(OpCodes.Ldarg_S, (byte)8),
                new CodeMatch(OpCodes.Ldc_R4, -3.4028235E+38f),
                new CodeMatch(OpCodes.Beq))
            .ThrowIfInvalid("friendly-fire transpiler: Failed to find `ldarg.s angleMin`, `ldc.r4 -3.4028235E+38f`, `beq`")
            .CreateLabel(out var successLabel);

        codeMatcher.Start()
            .MatchForward(useEnd: true,
                new CodeMatch(OpCodes.Ldarg_S, (byte)6),
                new CodeMatch(OpCodes.Brfalse))
            .ThrowIfInvalid("friendly-fire transpiler: Failed to find `ldarg.s isFromPlayer`, `brfalse`")
            .Advance(1)
            .Substitute(OpCodes.Ldloc_2)
            .Insert(
                new CodeInstruction(OpCodes.Isinst, typeof(CUnitPlayer)),
                new CodeInstruction(OpCodes.Brtrue, successLabel));

        return codeMatcher.Instructions();
    }
}

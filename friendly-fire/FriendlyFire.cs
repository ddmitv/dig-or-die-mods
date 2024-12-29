using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace friendly_fire;

public static class CodeMatcherExtensions {
    public static CodeMatcher Inject(this CodeMatcher self, OpCode opcode, object operand = null) {
        var prevInstruction = self.Instruction.Clone();
        self.SetAndAdvance(opcode, operand);
        self.Insert(prevInstruction);
        return self;
    }
}

public static class Utils {
    public static T SSingleton_Inst<T>() where T : class, new() {
        var inst = typeof(SSingleton<T>).GetProperty("Inst", BindingFlags.NonPublic | BindingFlags.Static);
        return (T)inst.GetValue(null, []);
    }
}

[BepInPlugin("friendly-fire", "Friendly Fire", "1.0.0")]
public class FriendlyFire : BaseUnityPlugin
{
    private void Start()
    {
        ConfigEntry<bool> configDamageAOE = Config.Bind<bool>(
            section: "FriendlyFire", key: "DamageAOE", defaultValue: true,
            description: "Enables damage for players from explosions/lightning"
        );
        ConfigEntry<bool> configHideNames = Config.Bind<bool>(
            section: "FriendlyFire", key: "HideNames", defaultValue: false,
            description: "Hides other player names and chat messages above their heads"
        );

        Harmony.CreateAndPatchAll(typeof(FriendlyFire));
        if (configDamageAOE.Value) {
            Harmony.CreateAndPatchAll(typeof(SUnits_DoDamageAOE_Patch));
        }
        if (configHideNames.Value) {
            Harmony.CreateAndPatchAll(typeof(HidePlayerNames_Patch));
        }
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
            .ThrowIfInvalid("friendly-fire transpiler (1)")
            .Advance(1)
            .Inject(OpCodes.Ldarg_0)
            .CreateLabel(out var failLabel)
            .Insert(
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CBullet), nameof(CBullet.m_attacker))),
                new CodeInstruction(OpCodes.Isinst, typeof(CUnitPlayer)),
                new CodeInstruction(OpCodes.Brfalse, failLabel), // `m_attacker` is not CUnitPlayer
                new CodeInstruction(OpCodes.Ldloc_2),
                new CodeInstruction(OpCodes.Isinst, typeof(CUnitPlayer)),
                new CodeInstruction(OpCodes.Brfalse, failLabel), // `cunit2` is CUnitPlayer
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CBullet), nameof(CBullet.m_attacker))),
                new CodeInstruction(OpCodes.Ldloc_2),
                new CodeInstruction(OpCodes.Bne_Un, successLabel)); // `m_attacker` != `cunit2`

        return codeMatcher.Instructions();
    }

    // [HarmonyTranspiler]
    // [HarmonyPatch(typeof(CUnitPlayerLocal), "OnDeath")]
    // private static IEnumerable<CodeInstruction> CUnitPlayerLocal_OnDeath(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
    //     void OnDeathByPlayer(CUnitPlayerLocal self, CUnitPlayer attacker) {
    //         var SScreenHudChat_inst = Utils.SSingleton_Inst<SScreenHudChat>();
    //         var AddChatMessage_Local = AccessTools.Method(typeof(SScreenHudChat), "AddChatMessage_Local");
    // 
    //         string deathMessage = SLoc.GetText("CHAT_DEATH_KILLED", false, self.GetPlayer().m_name, attacker.GetPlayer().m_name);
    //         AddChatMessage_Local.Invoke(SScreenHudChat_inst, [null, deathMessage, false]);
    //     }
    // 
    //     var codeMatcher = new CodeMatcher(instructions, generator);
    // 
    //     codeMatcher.End()
    //         .MatchBack(useEnd: false,
    //             new CodeMatch(OpCodes.Ldarg_0),
    //             new CodeMatch(OpCodes.Ldarg_1),
    //             new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(string), nameof(string.Empty))),
    //             new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(CUnitPlayer), "OnDeath")))
    //         .ThrowIfInvalid("friendly-fire transpiler: Failed to find `ldarg.0`, `ldarg.1`, `ldsfld string.Empty`, `call CUnitPlayer.OnDeath`")
    //         .CreateLabel(out var successLabel);
    // 
    //     codeMatcher.Start()
    //         // if (attacker != null && attacker != this && attacker is CUnitMonster)
    //         .MatchForward(useEnd: false,
    //             new CodeMatch(OpCodes.Ldarg_1),
    //             new CodeMatch(OpCodes.Brfalse),
    // 
    //             new CodeMatch(OpCodes.Ldarg_1),
    //             new CodeMatch(OpCodes.Ldarg_0),
    //             new CodeMatch(OpCodes.Beq),
    // 
    //             new CodeMatch(OpCodes.Ldarg_1),
    //             new CodeMatch(OpCodes.Isinst, typeof(CUnitMonster)),
    //             new CodeMatch(OpCodes.Brfalse))
    //         .Inject(OpCodes.Ldarg_1)
    //         .CreateLabel(out var failLabel)
    //         .Insert(
    //             // attacker != null
    //             // OpCodes.Ldarg_1
    //             new CodeInstruction(OpCodes.Brfalse, failLabel),
    //             // attacker != this
    //             new CodeInstruction(OpCodes.Ldarg_1),
    //             new CodeInstruction(OpCodes.Ldarg_0),
    //             new CodeInstruction(OpCodes.Beq, failLabel),
    //             // attacker is CUnitPlayer
    //             new CodeInstruction(OpCodes.Ldarg_1),
    //             new CodeInstruction(OpCodes.Isinst, typeof(CUnitPlayer)),
    //             new CodeInstruction(OpCodes.Brfalse, failLabel),
    // 
    //             new CodeInstruction(OpCodes.Ldarg_0), // arg 0: this
    //             new CodeInstruction(OpCodes.Ldarg_1), // arg 1: attacker
    //             Transpilers.EmitDelegate(OnDeathByPlayer),
    //             new CodeInstruction(OpCodes.Br, successLabel));
    // 
    //     return codeMatcher.Instructions();
    // }
}

public static class SUnits_DoDamageAOE_Patch {
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(SUnits), nameof(SUnits.DoDamageAOE))]
    private static IEnumerable<CodeInstruction> SUnits_DoDamageAOE(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var codeMatcher = new CodeMatcher(instructions, generator);

        codeMatcher.Start()
            .MatchForward(useEnd: false,
                new CodeMatch(OpCodes.Ldarg_S, (byte)8),
                new CodeMatch(OpCodes.Ldc_R4, -3.4028235E+38f),
                new CodeMatch(OpCodes.Beq))
            .ThrowIfInvalid("friendly-fire transpiler (1)")
            .CreateLabel(out var successLabel);

        codeMatcher.Start()
            .MatchForward(useEnd: true,
                new CodeMatch(OpCodes.Ldarg_S, (byte)6),
                new CodeMatch(OpCodes.Brfalse))
            .ThrowIfInvalid("friendly-fire transpiler (2)")
            .Advance(1)
            .Inject(OpCodes.Ldloc_2)
            .Insert(
                new CodeInstruction(OpCodes.Isinst, typeof(CUnitPlayer)),
                new CodeInstruction(OpCodes.Brtrue, successLabel));

        return codeMatcher.Instructions();
    }
}

public static class HidePlayerNames_Patch {
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(SScreenHudWorld), "OnUpdate")]
    private static IEnumerable<CodeInstruction> SScreenHudWorld_OnUpdate(IEnumerable<CodeInstruction> instructions) {
        var codeMatcher = new CodeMatcher(instructions);
        var CMeshText_Get = AccessTools.Method(typeof(CMesh<CMeshText>), nameof(CMesh<CMeshText>.Get), [typeof(SScreen), typeof(bool)]);

        codeMatcher.Start()
            .MatchForward(useEnd: false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldc_I4_0),
                new CodeMatch(OpCodes.Call, CMeshText_Get))
            .ThrowIfInvalid("friendly-fire transpiler (1)")
            .SetAndAdvance(OpCodes.Nop, null)
            .RemoveInstructions(28);
        codeMatcher
            .MatchForward(useEnd: false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldc_I4_0),
                new CodeMatch(OpCodes.Call, CMeshText_Get),
                new CodeMatch(OpCodes.Ldloc_S),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(CPlayer), nameof(CPlayer.m_lastChat))),
                new CodeMatch(OpCodes.Ldloca_S))
            .ThrowIfInvalid("friendly-fire transpiler (2)")
            .SetAndAdvance(OpCodes.Nop, null)
            .RemoveInstructions(22);

        return codeMatcher.Instructions();
    }
}

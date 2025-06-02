using HarmonyLib;
using ModUtils;
using ModUtils.Extensions;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

[HarmonyPatch(typeof(SScreenCrafting))]
internal static class SScreenCraftingPatches {
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(SScreenCrafting.OnUpdate))]
    private static IEnumerable<CodeInstruction> SScreenCrafting_OnUpdate_ConsumableWeapon(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        // Allows to craft multiple times ExtCItem_ConsumableWeapon item (in not m_filterAdds mode) unlike CItem_Weapon

        return new CodeMatcher(instructions, generator)
            .MatchForward(useEnd: true,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(SScreenCrafting).Field("m_filterAdds")),
                new(OpCodes.Brtrue)) // -> successLabel
            .ThrowIfInvalid("(1)")
            .GetOperand(out Label successLabel)
            .MatchForward(useEnd: false,
                new CodeMatch(OpCodes.Ldloc_S).LocalIndex(17),
                new(OpCodes.Isinst, typeof(CItem_Weapon)),
                new(OpCodes.Brtrue))
            .ThrowIfInvalid("(2)")
            .Insert(
                new(OpCodes.Ldloc_S, (byte)17),
                new(OpCodes.Isinst, typeof(ExtCItem_ConsumableWeapon)),
                new(OpCodes.Brtrue, successLabel))
            .Instructions();

        //          if (m_filterAdds || ...)
        // ldarg.0
        // ldfld     SScreenCrafting::m_filterAdds
        // brtrue       --------------------------------------------------|
        //          if (... || item is ExtCItem_ConsumableWeapon || ...)  |
        // |> ldloc.s V_17                                                |
        // |> isinst ExtCItem_ConsumableWeapon                            |
        // |> brtrue    --------------------------------------------------|
        //          if (... || (!(item is CItem_Weapon) && ...) || ...)   |
        // ldloc.s V_17                                                   |
        // isinst CItem_Weapon                                            |
        // brtrue ...                                                     |
        // ...                                                            |
        //          if (m_filterAdds || item != GItems.autoBuilderMK1)    |
        // ldarg.0      <--------------------------------------------------
        // ldfld SScreenCrafting::m_filterAdds
        // brtrue ...
        // ldloc.s V_17
        // ldsfld GItems::autoBuilderMK1
        // bne.un ...
        // br ...
    }

    [HarmonyTranspiler]
    [HarmonyPatch(nameof(SScreenCrafting.OnUpdate))]
    private static IEnumerable<CodeInstruction> SScreenCrafting_OnUpdate(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        static bool IsAutobuilderPowered(CItem_MachineAutoBuilder autobuilderItem) {
            if (autobuilderItem is not ExtCItem_ConditionalMachineAutoBuilder conditionalAutobuilder) {
                return true;
            }
            var autobuilderPos = new int2(SItems.ActivableItemPos);
            bool isPowered = SWorld.Grid[autobuilderPos.x, autobuilderPos.y].IsPowered();

            if (conditionalAutobuilder.checkCondition is null) {
                return isPowered;
            }
            return isPowered && conditionalAutobuilder.checkCondition(autobuilderPos.x, autobuilderPos.y);
        }

        return new CodeMatcher(instructions, generator)
            .Start()
            .MatchForward(useEnd: true,
                new(OpCodes.Ldsfld, typeof(SInputs).StaticField("use")),
                new(OpCodes.Callvirt, typeof(SInputs.KeyBinding).Method("IsKeyDown")),
                new(OpCodes.Brfalse))
            .ThrowIfInvalid("(1)")
            .GetOperand(out Label skipOpenPanel)
            .Advance(1)
            .Insert(
                new(OpCodes.Ldloc_2),
                Transpilers.EmitDelegate(IsAutobuilderPowered),
                new(OpCodes.Brfalse, skipOpenPanel))
            .Instructions();
    }
}

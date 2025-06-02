using HarmonyLib;
using ModUtils;
using ModUtils.Extensions;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

[HarmonyPatch(typeof(SItems))]
internal static class SItemsPatches {
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(SItems.OnUpdateSimu))]
    private static IEnumerable<CodeInstruction> SItems_OnUpdateSimu_PortableTeleport(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var codeMatcher = new CodeMatcher(instructions, generator);
        codeMatcher.Start()
            .MatchForward(useEnd: true,
                new(OpCodes.Call, typeof(SSingleton<SWorld>).Method("get_Inst")),
                new(OpCodes.Ldloc_S),
                new(OpCodes.Callvirt, typeof(SWorld).Method<int2>("GetCellContent")),
                new(OpCodes.Isinst),
                new(OpCodes.Brtrue))
            .ThrowIfInvalid("(1)")
            .GetOperand(out Label skipLabel)
            .Advance(1)
            .Insert(
                Transpilers.EmitDelegate(static () => {
                    return G.m_player.PosCell == CItem_MachineTeleport.m_teleportersPos[0];
                }),
                new(OpCodes.Brtrue, skipLabel));
        return codeMatcher.Instructions();
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(SItems.OnInit))]
    private static void SItems_OnInit() {
        foreach (var itemField in typeof(CustomItems).GetFields(BindingFlags.Static | BindingFlags.Public)) {
            var modItem = (ModItem)itemField.GetValue(null);
            modItem.Item.m_tileTextureName = ModCTile.texturePath;
            ItemTools.RegisterItem(modItem.Item);
        }
        SLoc.ReprocessTexts();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(nameof(SItems.OnUpdateSimu))]
    private static IEnumerable<CodeInstruction> SItems_OnUpdateSimu_MiniaturizorMK6(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        static bool MiniaturizorMK6CustomLogic(CItem_Device cItem_Device, CItemCell content) {
            if (!ReferenceEquals(cItem_Device, CustomItems.miniaturizorMK6.Item)) { return false; }

            return content.m_hpMax > GItems.miniaturizorMK5.m_customValue;
        }

        var codeMatcher = new CodeMatcher(instructions, generator);
        codeMatcher.Start()
            // if (content != null && !(content is CItem_Wall) && (float)content.m_hpMax > customValue)
            .MatchForward(useEnd: true,
                new(OpCodes.Ldloc_S),
                new(OpCodes.Brfalse),

                new(OpCodes.Ldloc_S),
                new(OpCodes.Isinst, typeof(CItem_Wall)),
                new(OpCodes.Brtrue),
                // (1)
                new(OpCodes.Ldloc_S),
                new(OpCodes.Ldfld, typeof(CItemCell).Field("m_hpMax")),
                new(OpCodes.Conv_R4),
                new(OpCodes.Ldloc_S),
                new(OpCodes.Ble_Un))
            .ThrowIfInvalid("(1)")
            .Advance(1)
            .CreateLabel(out Label cannotDigLabel)
            .Advance(-5) // Put cursor at (1)
            .Insert(
                new(OpCodes.Ldloc_S, (byte)16),
                new(OpCodes.Ldloc_S, (byte)18),
                Transpilers.EmitDelegate(MiniaturizorMK6CustomLogic),
                new(OpCodes.Brtrue, cannotDigLabel));
        return codeMatcher.Instructions();

        //            if (content != null && ... && ...)
        // ldloc.s V_18
        // brfalse         --------------------------------------------------------|
        //            if (... && !(content is CItem_Wall) && ...)                  |
        // ldloc.s V_18                                                            |
        // isinst CItem_Wall                                                       |
        // brtrue          --------------------------------------------------------|
        //                                                                         |
        // |> ldloc.s V_16 (citem_Device: CItem_Device)                            |
        // |> ldloc.s V_18 (content: CItemCell)                                    |
        // |> call MiniaturizorMK6CustomLogic                                      |
        // |> brtrue       -----------------------------------------------------|  |
        //            if (... && ... && (float)content.m_hpMax > customValue)   |  |
        // ldloc.s V_18                                                         |  |
        // ldfld CItemCell::m_hpMax                                             |  |
        // conv.r4                                                              |  |
        // ldloc.s V_20                                                         |  |
        // ble.un          -----------------------------------------------------|--|
        //                 vvv Handle message cannot dig (basalt)               |  |
        // call SSingletonScreen<SScreenMessages>.get_Inst() <-------------------  |
        // ldloc.s V_18                                                            |
        // ldfld CItemCell::m_hpMax                                                |
        // ...                                                                     |
        //                 vvv Handle miniturizor mining                           |
        // ldloc.s V_18            <------------------------------------------------
        // brfalse ...
        // 
        // ldloc.s V_18
        // ldfld CItemCell::m_mainColor
        // br
        // ...
    }
}

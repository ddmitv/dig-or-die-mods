using BepInEx;
using HarmonyLib;
using ModUtils.Extensions;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

internal static class Patches {
    private static readonly HashSet<CItemCell> logicComponentItems = [
        GItems.elecSwitch, GItems.elecSwitchPush, GItems.elecSwitchRelay, GItems.elecCross,
        GItems.elecSignal, GItems.elecClock, GItems.elecToggle, GItems.elecDelay,
        GItems.elecWaterSensor, GItems.elecProximitySensor, GItems.elecDistanceSensor,
        GItems.elecAND, GItems.elecOR, GItems.elecXOR, GItems.elecNOT, GItems.elecLight,
        GItems.elecAlarm
    ];
    public static readonly Dictionary<CItemCell, ReplaceType> extraReplacableItems = [];

    private static ReplaceType GetReplaceType(CItemCell item) {
        if (item is CItem_Defense) {
            return ReplaceType.Defense;
        }
        if (item is CItem_Wall oldItemWall && oldItemWall.m_type == CItem_Wall.Type.Platform) {
            return ReplaceType.Platform;
        }
        if (item == GItems.light || item == GItems.lightSticky) {
           return ReplaceType.Light;
        }
        if (logicComponentItems.Contains(item)) {
            return ReplaceType.LogicComponent;
        }
        if (item is CItem_MineralDirt) {
            return ReplaceType.Dirt;
        }
        if (extraReplacableItems.TryGetValue(item, out ReplaceType type)) {
            return type;
        }
        return ReplaceType.None;
    }
    private static bool AreBothReplaceable(CItemCell oldItem, CItemCell newItem) {
        if (oldItem == null || newItem == null || oldItem == newItem) { return false; }

        var oldType = GetReplaceType(oldItem);
        if (oldType == ReplaceType.None) { return false; }

        var newType = GetReplaceType(newItem);
        if (newType == ReplaceType.None) { return false; }

        return oldType == newType;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(SScreenInventory), nameof(SScreenInventory.TryPlaceItemOnCell))]
    private static IEnumerable<CodeInstruction> SScreenInventory_TryPlaceItemOnCell(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        return new CodeMatcher(instructions, generator)
            .MatchForward(useEnd: false,
                new CodeMatch(OpCodes.Ldloc_S).LocalIndex(12),
                new(OpCodes.Ldloc_1),
                new(OpCodes.Beq),

                new CodeMatch(OpCodes.Ldloc_S).LocalIndex(12),
                new(OpCodes.Brfalse),

                new CodeMatch(OpCodes.Ldloc_S).LocalIndex(13),
                new(OpCodes.Brfalse),

                new CodeMatch(OpCodes.Ldloc_S).LocalIndex(5),
                new(OpCodes.Brtrue), // <-- offset 8

                new CodeMatch(OpCodes.Ldloc_S).LocalIndex(6),
                new(OpCodes.Brfalse))
            .ThrowIfInvalid("(1)")
            .GetOperandAtOffset(8, out Label successLabel) // brtrue <LABEL>
            .Advance(3)
            .InsertAndAdvance(
                new(OpCodes.Ldloc_S, (byte)12),
                new(OpCodes.Ldloc_0),
                Transpilers.EmitDelegate(AreBothReplaceable),
                new(OpCodes.Brtrue, successLabel))

            .MatchForward(useEnd: false,
                new(OpCodes.Call, typeof(SSingleton<SWorld>).Method("get_Inst")),
                new(OpCodes.Ldarg_2),
                new(OpCodes.Ldloc_1),
                new CodeMatch(OpCodes.Ldloc_S).LocalIndex(6),
                new(OpCodes.Callvirt, typeof(SWorld).Method<int2, CItemCell, bool>("SetContent")))
            .ThrowIfInvalid("(2)")
            .InjectAndAdvance(OpCodes.Ldarg_2)
            .InsertAndAdvance(
                new(OpCodes.Ldloc_S, (byte)12),
                new(OpCodes.Ldloc_0),
                Transpilers.EmitDelegate(static (int2 p, CItemCell oldItem, CItemCell newItem) => {
                    if (AreBothReplaceable(oldItem, newItem)) {
                        SPickups.CreatePickup(oldItem, nb: 1f, p + new Vector2(0.5f, 0.5f));
                    }
                }
            ))
            .Instructions();
    }
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(SMessageRequestBuild), nameof(SMessageRequestBuild.OnReceived))]
    private static IEnumerable<CodeInstruction> SMessageRequestBuild_OnReceived(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        return new CodeMatcher(instructions, generator)
            .MatchForward(useEnd: false,
                new(OpCodes.Call, typeof(SSingleton<SWorld>).Method("get_Inst")),
                new(OpCodes.Ldloc_1),
                new(OpCodes.Call, typeof(ushort2).Method<ushort2>("op_Implicit")),
                new(OpCodes.Ldloc_3),
                new CodeMatch(OpCodes.Ldloc_S).LocalIndex(7),
                new(OpCodes.Callvirt, typeof(SWorld).Method<int2, CItemCell, bool>("SetContent")))
            .ThrowIfInvalid("(1)")
            .InjectAndAdvance(OpCodes.Ldloc_1)
            .InsertAndAdvance(
                new(OpCodes.Ldloc_S, (byte)6),
                new(OpCodes.Ldloc_S, (byte)4),
                Transpilers.EmitDelegate(static (ushort2 p, CItemCell oldItem, CItemCell newItem) => {
                    if (AreBothReplaceable(oldItem, newItem)) {
                        SPickups.CreatePickup(oldItem, nb: 1f, (int2)p + new Vector2(0.5f, 0.5f));
                    }
                }
            ))
            .Instructions();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(CItemCell), nameof(CItemCell.CanItemBeAncheredAt))]
    private static IEnumerable<CodeInstruction> CItemCell_CanItemBeAncheredAt(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        return new CodeMatcher(instructions, generator)
            .MatchForward(useEnd: true,
                new(OpCodes.Call, typeof(CCell[,]).Method<int, int>("Get")),
                new(OpCodes.Stloc_0))
            .ThrowIfInvalid("(1)")
            .Advance(1)
            .Insert(
                new(OpCodes.Ldloca, (byte)0),
                new(OpCodes.Ldarg_2),
                new(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate(static (ref CCell cell, bool checkAtMousePlacement, CItemCell @this) => {
                    if (!checkAtMousePlacement) { return; }
                    if (AreBothReplaceable(oldItem: @this, newItem: cell.GetContent())) {
                        cell.m_contentId = 0;
                    }
                }))
            .Instructions();
    }
}

// Plugin API (and all enumerator values)
public enum ReplaceType {
    None,
    Defense,
    Platform,
    Light,
    Dirt,
    LogicComponent,
}

[BepInPlugin("replacementor", ThisPluginInfo.Name, ThisPluginInfo.Version)]
public class Replacementor : BaseUnityPlugin {
    // Plugin API (must be non-static)
    public void AddReplaceableItem(CItemCell item, ReplaceType type) {
        Patches.extraReplacableItems.Add(item, type);
    }

    void Awake() {
        var harmony = new Harmony(Info.Metadata.GUID);
        harmony.PatchAll(typeof(Patches));
    }
}

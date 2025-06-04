using HarmonyLib;
using ModUtils;
using ModUtils.Extensions;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

[HarmonyPatch]
internal static class CItemPatches {
    [HarmonyPatch(typeof(CItem), nameof(CItem.Init))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> CItem_Init(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var codeMatcher = new CodeMatcher(instructions, generator);

        codeMatcher.End()
            .MatchBack(useEnd: true,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(CItem).Field("m_tile")),
                new(OpCodes.Brfalse))
            .GetOperand(out Label failLabel)
            .Advance(1)
            .Insert(
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(CItem).Field("m_tileIcon")),
                new(OpCodes.Brtrue, failLabel));

        return codeMatcher.Instructions();
    }

    [HarmonyPatch(typeof(CItem_Device), nameof(CItem_Device.OnUpdate))]
    [HarmonyPrefix]
    private static bool CItem_Device_OnUpdate(CItem_Device __instance) {
        // Original CItem_Device.OnUpdate uses `this == GItems.defenseShield` to check for Shield item
        if (__instance.m_groupId == "Shield") {
            CItemVars myVars = __instance.GetMyVars();
            if (GVars.m_simuTimeD > (double)(myVars.ShieldLastHitTime + 0.5f)) {
                myVars.ShieldValue = Mathf.Min(myVars.ShieldValue + SMain.SimuDeltaTime * 0.5f * __instance.m_customValue * G.m_player.GetHpMax(), __instance.m_customValue * G.m_player.GetHpMax());
            }
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(CItem_Weapon), nameof(CItem_Weapon.Use_Local))]
    [HarmonyPostfix]
    private static void CItem_Weapon_Use_Local_Postfix(CItem_Weapon __instance, CPlayer player, Vector2 worldPos, bool isShift) {
        if (__instance == CustomItems.gunZF0Shotgun.Item
            && SWorld.GridRectCam.Contains(worldPos)
            && isShift) {
            CItemVars vars = GItems.gunZF0.GetVars(player);
            vars.ZF0TargetLastTimeHit = float.MinValue;
            vars.ZF0TargetId = ushort.MaxValue;
        }
    }

    [HarmonyPatch(typeof(CItem_Device), nameof(CItem_Device.Use_Local))]
    [HarmonyPostfix]
    private static void CItem_Device_Use_Local(CItem_Device __instance, CPlayer player, Vector2 mousePos, bool isShift) {
        if (__instance == CustomItems.portableTeleport.Item) {
            CItem_MachineTeleport.m_teleportersPos.Clear();
            CItem_MachineTeleport.m_teleportersPos.Add(player.m_unitPlayer.PosCell);
            for (int i = 0; i < SWorld.Gs.x; i++) {
                for (int j = 0; j < SWorld.Gs.y; j++) {
                    if (SWorld.Grid[i, j].GetContent() == GItems.teleport) {
                        CItem_MachineTeleport.m_teleportersPos.Add(new int2(i, j));
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(CItem_MachineTeleport), nameof(CItem_MachineTeleport.Teleport))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> CItem_MachineTeleport_Teleport_PortableTeleport(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var codeMatcher = new CodeMatcher(instructions, generator);

        codeMatcher.Start()
            .MatchForward(useEnd: false,
                new(OpCodes.Ldsfld, typeof(G).StaticField("m_player")),
                new(OpCodes.Ldloc_1),
                new(OpCodes.Call, typeof(int2).Method<int2>("op_Implicit")))
            .ThrowIfInvalid("(1)")
            .CreateLabel(out Label teleportLabel)
            .Start()
            .MatchForward(useEnd: false,
                new(OpCodes.Call, typeof(SWorld).Method("get_Grid")),
                new(OpCodes.Ldloca_S),
                new(OpCodes.Ldfld, typeof(int2).Field("x")),
                new(OpCodes.Ldloca_S),
                new(OpCodes.Ldfld, typeof(int2).Field("y")),
                new(OpCodes.Call, typeof(CCell[,]).Method<int, int>("Address")),

                new(OpCodes.Call, typeof(CCell).Method("GetContent")),
                new(OpCodes.Ldsfld, typeof(GItems).StaticField("teleport")),
                new(OpCodes.Bne_Un))
            .ThrowIfInvalid("(2)")
            .Insert(
                Transpilers.EmitDelegate(static () => {
                    return G.m_player.PosCell == CItem_MachineTeleport.m_teleportersPos[0];
                }),
                new(OpCodes.Brtrue, teleportLabel));

        return codeMatcher.Instructions();
    }
}

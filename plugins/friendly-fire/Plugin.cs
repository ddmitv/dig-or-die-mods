using BepInEx;
using HarmonyLib;
using DODModAPI.Extensions;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using DODModAPI;

internal static class PlayersDamagePlayersPatch {
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(CBullet), nameof(CBullet.CheckColWithUnits))]
    private static IEnumerable<CodeInstruction> CBullet_CheckColWithUnits(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        return new CodeCursor(instructions, generator)
            .FindNext(
                new(OpCodes.Call, typeof(Vector2).Method("get_zero")),
                new(OpCodes.Stloc_S))
            .CreateLabel(offset: 0, out var successLabel)

            //     if (cunit2 != null && cunit2.IsAlive() && (this.m_unitsHit == null || !this.m_unitsHit.Contains(cunit2)) && ((!flag && cunitMonster != null) || (flag && (cunitPlayer != null || cunitDefense != null))))
            //                                                                           ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            .MoveToStart()
            .FindNextEnd(
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(CBullet).Field("m_unitsHit")),
                new(OpCodes.Ldloc_2),
                new(OpCodes.Callvirt, typeof(List<CUnit>).Method("Contains")),
                new(OpCodes.Brtrue))
            .InjectWithLabel(failLabel => [
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(CBullet).Field("m_attacker")),
                new(OpCodes.Isinst, typeof(CUnitPlayer)),
                new(OpCodes.Brfalse, failLabel), // `m_attacker` is not CUnitPlayer
                new(OpCodes.Ldloc_2),
                new(OpCodes.Isinst, typeof(CUnitPlayer)),
                new(OpCodes.Brfalse, failLabel), // `cunit2` is CUnitPlayer
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(CBullet).Field("m_attacker")),
                new(OpCodes.Ldloc_2),
                new(OpCodes.Bne_Un, successLabel) // `m_attacker` != `cunit2`
            ])
            .Finish();
    }
}

internal static class DoDamageAOEToPlayers {
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(SUnits), nameof(SUnits.DoDamageAOE))]
    private static IEnumerable<CodeInstruction> SUnits_DoDamageAOE(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        return new CodeCursor(instructions, generator)
            .FindNext(
                new(OpCodes.Ldarg_S, (byte)8),
                new(OpCodes.Ldc_R4, -3.4028235E+38f),
                new(OpCodes.Beq))
            .CreateLabel(offset: 0, out var successLabel)

            .FindNextEnd(
                new(OpCodes.Ldarg_S, (byte)6),
                new(OpCodes.Brfalse))
            .Inject(
                new(OpCodes.Ldloc_2),
                new(OpCodes.Isinst, typeof(CUnitPlayer)),
                new(OpCodes.Brtrue, successLabel))
            .Finish();
    }
}

internal static class HidePlayerNamesPatch {
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(SScreenHudWorld), nameof(SScreenHudWorld.OnUpdate))]
    private static IEnumerable<CodeInstruction> SScreenHudWorld_OnUpdate(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        return new CodeCursor(instructions, generator)
            .FindNext(
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldc_I4_0),
                new(OpCodes.Call, typeof(CMesh<CMeshText>).Method<SScreen, bool>("Get")))
            .Replace(new(OpCodes.Nop), out _)
            .Remove(28)

            .MoveToStart()
            .FindNext(
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldc_I4_0),
                new(OpCodes.Call, typeof(CMesh<CMeshText>).Method<SScreen, bool>("Get")),
                new(OpCodes.Ldloc_S),
                new(OpCodes.Ldfld, typeof(CPlayer).Field("m_lastChat")),
                new(OpCodes.Ldloca_S))
            .Replace(new(OpCodes.Nop, null), out _)
            .Remove(22)

            .Finish();
    }
}

internal static class HideMinimapPlayers_Patch {
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(SMinimap), nameof(SMinimap.OnUpdate))]
    private static IEnumerable<CodeInstruction> SMinimap_OnUpdate(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        return new CodeCursor(instructions, generator)
            // hide minimap player icons
            .FindNext(
                new(OpCodes.Ldloc_S),
                new(OpCodes.Callvirt, typeof(CPlayer).Method("HasUnitPlayer")),
                new(OpCodes.Brtrue))

            .GetPos(out int pos)
            .FindNext(new CodeInstruction(OpCodes.Br))
            .GetOperand(offset: 0, out Label failLabel)
            .SetPos(pos)

            .GetOperand(offset: 0, out LocalBuilder playerVar)
            .Insert(
                new(OpCodes.Ldloc_S, playerVar),
                new(OpCodes.Call, typeof(CPlayer).Method("IsMe")),
                new(OpCodes.Brfalse, failLabel))

            // hide live view pixels
            .FindNext(
                new CodeInstruction(OpCodes.Callvirt, typeof(Texture2D).Method("SetPixels32", [typeof(int), typeof(int), typeof(int), typeof(int), typeof(Color32[])])))
            .Advance(1)
            .CreateLabel(offset: 0, out Label skipLabel)
            .Advance(-13)
            .Insert(
                new(OpCodes.Ldloc_1),
                new(OpCodes.Call, typeof(CPlayer).Method("IsMe")),
                new(OpCodes.Brfalse, skipLabel))
            .Finish();
    }
}

internal static class PlayerDamageToGroundPatch {
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(CBullet), nameof(CBullet.CheckColWithGround))]
    private static IEnumerable<CodeInstruction> CBullet_CheckColWithGround(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        return new CodeCursor(instructions, generator)
            .FindNext(
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(CBullet).Field("m_attacker")),
                new(OpCodes.Isinst, typeof(CUnitMonster)),
                new(OpCodes.Brfalse))
            .CreateLabel(offset: 4, out Label successLabel)
            .Inject(
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(CBullet).Field("m_attacker")),
                new(OpCodes.Isinst, typeof(CUnitPlayer)),
                new(OpCodes.Brtrue, successLabel))
            .Finish();
    }
}

internal static class DefenseDamagePlayersPatch {
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(CBullet), nameof(CBullet.CheckColWithUnits))]
    private static IEnumerable<CodeInstruction> CBullet_CheckColWithUnits(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        // (Start of collision check, end of unit type check)
        // call Vector2::get_zero()
        // stloc.s V_7
        return new CodeCursor(instructions, generator)
            .FindNext(
                new(OpCodes.Call, typeof(Vector2).Method("get_zero")),
                new(OpCodes.Stloc_S))
            .CreateLabel(offset: 0, out Label successLabel)

            .MoveToStart()
            .FindNext(
                new(OpCodes.Ldloc_S),
                new(OpCodes.Brtrue))
            // if (m_attacker is CUnitDefense && 
            //     cunit2 is CUnitPlayer && 
            //     m_attacker != cunit2) 
            //     -> jump to success (bypass original checks)
            .InjectWithLabel(failLabel => [
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(CBullet).Field("m_attacker")),
                new(OpCodes.Isinst, typeof(CUnitDefense)),
                new(OpCodes.Brfalse, failLabel),
                new(OpCodes.Ldloc_2),
                new(OpCodes.Isinst, typeof(CUnitPlayer)),
                new(OpCodes.Brfalse, failLabel),
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(CBullet).Field("m_attacker")),
                new(OpCodes.Ldloc_2),
                new(OpCodes.Bne_Un, successLabel)
            ])
            .Finish();

        //          [ if (... && (m_unitsHit == null || ...) && ...) ]
        // ldarg.0
        // ldfld CBullet::m_unitsHit
        // brfalse            --------------------|
        //          [ if (... && (... || !m_unitsHit.Contains(cunit2))) && ...) ]
        // ldarg.0                                |
        // ldfld CBullet::m_unitsHit              |
        // ldloc.2                                |
        // callvirt List<CUnit>::Contains(CUnit)  |
        // brtrue             --------------------|--------|
        //                                        |        |
        // |> ldfld CBullet::m_attacker  <---------X----|  |
        // |> isinst CUnitDefense                       |  |
        // |> brfalse         --------------------------|  |
        // |> ldloc.2                                   |  |
        // |> isinst CUnitPlayer                        |  |
        // |> brfalse         --------------------------|  |
        // |> ldarg.0                                   |  |
        // |> ldfld CBullet::m_attacker                 |  |
        // |> ldloc.2                                   |  |
        // |> bne.un          -----------------------|  |  |
        //                                           |  |  |
        // ldloc.s   V_6             <---------------|---  |
        // brtrue                                    |     |
        // ...                                       |     |
        //         [ Collision checking start ]      |     |
        // call Vector2::get_zero()  <----------------     |
        // stloc.s V_7                                     |
        // ...                                             |
        //         [ Loop end ]                            |
        // ldloc.1                   <----------------------
        // ldc.i4.1
        // add
        // stloc.1
        // ...
    }
}

internal static class DeathMessageKilledByPlayerPatch {
    private const string MagicChatMessageSystemArg = "__CHAT_DEATH_KILLED_BY_PLAYER";

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(SNetworkCommands), nameof(SNetworkCommands.ProcessCommand))]
    private static IEnumerable<CodeInstruction> SNetworkCommands_ProcessCommand(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        return new CodeCursor(instructions, generator)
            .FindNext(
                new(OpCodes.Ldloc_2),
                new(OpCodes.Ldc_I4_0),
                new(OpCodes.Ldelem_Ref),
                new(OpCodes.Ldc_I4_0),
                new(OpCodes.Ldloc_3),
                new(OpCodes.Ldc_I4_1),
                new(OpCodes.Ble),

                new(OpCodes.Ldloc_2),
                new(OpCodes.Ldc_I4_1),
                new(OpCodes.Ldelem_Ref),
                new(OpCodes.Br))
            .Inject(
                new(OpCodes.Ldloc_2),
                Transpilers.EmitDelegate(static (string[] args) => {
                    if (args[0] == MagicChatMessageSystemArg) {
                        args[0] = "CHAT_DEATH_KILLED";
                    }
                }))
            .Finish();
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(CUnitPlayer), nameof(CUnitPlayer.OnDeath))]
    private static void Base_CUnitPlayer_OnDeath(CUnitPlayer instance, CUnit attacker, string damageCause) {
        throw new NotImplementedException("This is a reverse patch method stub");
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CUnitPlayerLocal), nameof(CUnitPlayerLocal.OnDeath))]
    private static bool CUnitPlayerLocal_OnDeath(CUnitPlayerLocal __instance, CUnit attacker, string damageCause) {
        // run original if player was not killed by another player
        if (attacker is not CUnitPlayer attackerPlayer) { return true; }

        // replicate original CUnitPlayerLocal.OnDeath logic
        SNetwork.GetPlayer(__instance).CleanItemVars(true);
        if (SOutgame.Params.m_respawnDelay < 0) {
            SSingletonScreen<SScreenMessages>.Inst.AddMessage(SLoc.GetText("MESSAGE_GAME_OVER", false, null, null, null, null), SScreenMessages.MessageType.Normal, Vector2.up * 300f, 5f, 0.1f, 0.3f);
        }
        SScreenHudChat.AddChatMessage_Networked($"/system {MagicChatMessageSystemArg}|{__instance.GetPlayer().m_name}|{attackerPlayer.GetPlayer().m_name}");

        // since CUnitPlayerLocal.OnDeath overrides CUnitPlayer.OnDeath we need to call the base without triggering the override
        Base_CUnitPlayer_OnDeath(__instance, attacker, "");

        return false;
    }
}

[BepInPlugin("friendly-fire", ThisPluginInfo.Name, ThisPluginInfo.Version)]
[BepInDependency(DODModAPI.DODModAPIPlugin.GUID)]
public class FriendlyFire : BaseUnityPlugin {
    private void Start() {
        var configEnabled = Config.Bind<bool>(
            section: "General", key: "Enabled", defaultValue: true,
            description: "Enables the plugin"
        );
        var configDamageAOE = Config.Bind<bool>(
            section: "FriendlyFire", key: "DamageAOE", defaultValue: true,
            description: "Enables damage for players from explosions/lightning"
        );
        var configHideNames = Config.Bind<bool>(
            section: "FriendlyFire", key: "HideNames", defaultValue: false,
            description: "Hides other player names and chat messages above their heads"
        );
        var configHideMinimapPlayers = Config.Bind<bool>(
            section: "FriendlyFire", key: "HideMinimapPlayers", defaultValue: false,
            description: "Hides player icons from minimap"
        );
        var configPlayerDamageToGround = Config.Bind<bool>(
            section: "FriendlyFire", key: "PlayerDamageToGround", defaultValue: false,
            description: "Allows players to do damage to tiles"
        );
        var configDefenseDamagePlayers = Config.Bind<bool>(
            section: "FriendlyFire", key: "DefenseDamagePlayers", defaultValue: false,
            description: "Allows defense units (turrrets) to do damage to players"
        );
        if (!configEnabled.Value) { return; }

        var harmony = new Harmony(Info.Metadata.GUID);

        harmony.PatchAll(typeof(PlayersDamagePlayersPatch));
        harmony.PatchAll(typeof(DeathMessageKilledByPlayerPatch));

        if (configDamageAOE.Value) {
            harmony.PatchAll(typeof(DoDamageAOEToPlayers));
        }
        if (configHideNames.Value) {
            harmony.PatchAll(typeof(HidePlayerNamesPatch));
        }
        if (configHideMinimapPlayers.Value) {
            harmony.PatchAll(typeof(HideMinimapPlayers_Patch));
        }
        if (configPlayerDamageToGround.Value) {
            harmony.PatchAll(typeof(PlayerDamageToGroundPatch));
        }
        if (configDefenseDamagePlayers.Value) {
            harmony.PatchAll(typeof(DefenseDamagePlayersPatch));
        }
    }
}


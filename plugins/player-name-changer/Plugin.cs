using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using ModUtils;
using ModUtils.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;

[BepInPlugin("player-name-changer", "Player Name Changer", "0.0.0")]
public class PlayerNameChanger : BaseUnityPlugin {
    private static ConfigEntry<string> configPlayerName = null;

    private void Start() {
        configPlayerName = Config.Bind<string>("General", "PlayerName", defaultValue: null, description: "Override the player name in the game. Reset to set the original player name (from Steam)");
        var configEnable = Config.Bind<bool>("General", "Enable", defaultValue: true);

        if (!configEnable.Value) { return; }

        configPlayerName.SettingChanged += static (object sender, EventArgs e) => {
            SNetwork.GetMyPlayer().m_name = SNetwork.MySteamName;
            SOutgame.Params.m_hostName = SNetwork.MySteamName;
        };
        var harmony = new Harmony(Info.Metadata.GUID);
        
        harmony.Patch(typeof(SNetwork).Method("get_MySteamName"),
            prefix: new HarmonyMethod(typeof(PlayerNameChanger).Method("SNetwork_get_MySteamName")));

        harmony.Patch(Type.GetType("Steamworks.SteamFriends, Assembly-CSharp-firstpass, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").Method("GetFriendPersonaName"),
            prefix: new HarmonyMethod(typeof(PlayerNameChanger).Method("SteamFriends_GetFriendPersonaName")));

        harmony.Patch(typeof(CPlayer).Method("Load"),
            transpiler: new HarmonyMethod(typeof(PlayerNameChanger).Method("CPlayer_Load")));
    }

    private static bool SNetwork_get_MySteamName(ref string __result) {
        if (configPlayerName.Value == null) { return true; }
        __result = configPlayerName.Value;
        return false;
    }
    private static bool SteamFriends_GetFriendPersonaName(ref string __result) {
        if (configPlayerName.Value == null) { return true; }
        __result = configPlayerName.Value;
        return false;
    }
    private static IEnumerable<CodeInstruction> CPlayer_Load(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        return new CodeMatcher(instructions, generator)
            .MatchForward(useEnd: true,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldarg_1),
                new(OpCodes.Callvirt, typeof(BinaryReader).Method<>("ReadString")),
                new(OpCodes.Stfld, typeof(CPlayer).Field("m_name")))
            .ThrowIfInvalid("(1)")
            .Insert(
                Transpilers.EmitDelegate(static (string oldName) => {
                    return SNetwork.MySteamName;
                }))
            .Instructions();
    }
}

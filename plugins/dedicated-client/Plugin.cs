using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

using ModUtils;
using ModUtils.Extensions;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Emit;
using System.Text;

#pragma warning disable IDE0051

internal static class Patches {
    [HarmonyPatch(typeof(SScreenLobbies), nameof(SScreenLobbies.OnInit))]
    [HarmonyPostfix]
    private static void SScreenLobbies_OnInit(SScreenLobbies __instance) {
        DedicatedClient.btJoinDedicated = new CGuiButton(
            screen: __instance, parent: __instance.m_guiRoot,
            x: -580, y: -180
        ) {
            m_width = 380,
            m_height = 80,
        };
        DedicatedClient.btJoinDedicated.SetText("JOIN DEDICATED");
    }
    [HarmonyPatch(typeof(SScreenLobbies), nameof(SScreenLobbies.OnUpdate))]
    [HarmonyPostfix]
    private static void SScreenLobbies_OnUpdate() {
        if (DedicatedClient.btJoinDedicated.IsClicked()) {
            SScreenPopup.Inst.Show(callback: DedicatedClient.OnConnectCallback,
                messageId: "DEDICATED_CLIENT_ENTER_IP",
                yesId: "DEDICATED_CLIENT_CONNECT",
                noId: "COMMON_CANCEL",
                input: true
            );
        }
    }

    private const ulong HostSteamId = 4;

    [HarmonyPatch(typeof(SteamMatchmaking), nameof(SteamMatchmaking.GetNumLobbyMembers))]
    [HarmonyPrefix]
    private static bool SteamMatchmaking_GetNumLobbyMembers(out int __result) {
        __result = 0;
        return false;
    }
    [HarmonyPatch(typeof(SteamMatchmaking), nameof(SteamMatchmaking.JoinLobby))]
    [HarmonyPrefix]
    private static bool SteamMatchmaking_JoinLobby(CSteamID steamIDLobby) {
        throw new NotSupportedException("Called SteamMatchmaking.JoinLobby");
    }
    [HarmonyPatch(typeof(SteamMatchmaking), nameof(SteamMatchmaking.LeaveLobby))]
    [HarmonyPrefix]
    private static bool SteamMatchmaking_LeaveLobby(CSteamID steamIDLobby) {
        DedicatedClient.Disconnect();
        return false;
    }
    [HarmonyPatch(typeof(SteamMatchmaking), nameof(SteamMatchmaking.GetLobbyData))]
    [HarmonyPrefix]
    private static bool SteamMatchmaking_GetLobbyData(out string __result, CSteamID steamIDLobby, string pchKey) {
        __result = "";
        return false;
    }
    [HarmonyPatch(typeof(SteamMatchmaking), nameof(SteamMatchmaking.RequestLobbyData))]
    [HarmonyPrefix]
    private static bool SteamMatchmaking_RequestLobbyData(CSteamID steamIDLobby) {
        return false;
    }
    [HarmonyPatch(typeof(SteamMatchmaking), nameof(SteamMatchmaking.SetLobbyMemberData))]
    [HarmonyPrefix]
    private static bool SteamMatchmaking_SetLobbyMemberData(CSteamID steamIDLobby, string pchKey, string pchValue) {
        return false;
    }

    [HarmonyPatch(typeof(SteamNetworking), nameof(SteamNetworking.IsP2PPacketAvailable))]
    [HarmonyPrefix]
    private static bool SteamNetworking_IsP2PPacketAvailable(out bool __result, out uint pcubMsgSize, int nChannel) {
        pcubMsgSize = 0; // it gets overwritten later anyway
        __result = DedicatedClient.client != null && DedicatedClient.client.Client.Available != 0;
        return false;
    }
    private static uint _recvPacketLength = 0;
    private static uint _recvBufferOffset = 0;
    private static readonly byte[] _recvBuffer = new byte[1048576 + 4]; // 1 MiB + 4 bytes
    private const uint HEADER_SIZE = 4;

    public static void ResetReceiveBuffer() {
        _recvPacketLength = 0;
        _recvBufferOffset = 0;
    }

    [HarmonyPatch(typeof(SteamNetworking), nameof(SteamNetworking.ReadP2PPacket))]
    [HarmonyPrefix]
    private static bool SteamNetworking_ReadP2PPacket(out bool __result, byte[] pubDest, uint cubDest, out uint pcubMsgSize, out CSteamID psteamIDRemote, int nChannel) {
        psteamIDRemote = new CSteamID(HostSteamId);
        __result = false;
        pcubMsgSize = 0;
        if (DedicatedClient.client == null || DedicatedClient.client.Client.Available == 0) {
            return false;
        }
        Socket socket = DedicatedClient.client.Client;
        try {
            if (_recvBufferOffset < HEADER_SIZE) {
                int bytesRead = socket.Receive(_recvBuffer, (int)_recvBufferOffset, (int)(HEADER_SIZE - _recvBufferOffset), SocketFlags.None);
                _recvBufferOffset += (uint)bytesRead;
                if (_recvBufferOffset < HEADER_SIZE) {
                    return false;
                }
                _recvPacketLength = BitConverter.ToUInt32(_recvBuffer, 0);
            }
            if (_recvBuffer.Length < HEADER_SIZE + _recvPacketLength) {
                throw new InvalidOperationException($"Receiving packet is too large! Buffer size: {_recvBuffer.Length}, total packet size: {HEADER_SIZE + _recvPacketLength}");
            }
            if (_recvBufferOffset < HEADER_SIZE + _recvPacketLength) {
                uint bytesToRead = HEADER_SIZE + _recvPacketLength - _recvBufferOffset;
                int bytesRead = socket.Receive(_recvBuffer, (int)_recvBufferOffset, (int)bytesToRead, SocketFlags.None);
                if (bytesRead == 0) {
                    DedicatedClient.UnexpectedDisconnect();
                    return false;
                }
                _recvBufferOffset += (uint)bytesRead;
                if (_recvBufferOffset < HEADER_SIZE + _recvPacketLength) {
                    return false;
                }
            }
            Buffer.BlockCopy(_recvBuffer, (int)HEADER_SIZE, pubDest, 0, (int)_recvPacketLength);
            _recvBufferOffset = 0;

            pcubMsgSize = _recvPacketLength;
        } catch (SocketException) {
            DedicatedClient.UnexpectedDisconnect();
            return false;
        }
        if (DedicatedClient.configLogReceivedPackets.Value) {
            DedicatedClient.Log.LogInfo($"Received packet (len: {pcubMsgSize}), first 10 bytes: {string.Join(", ", pubDest.Take(Math.Min((int)pcubMsgSize, 10)).Select(x => x.ToString()).ToArray())}");
        }
        __result = true;
        return false;
    }
    [HarmonyPatch(typeof(SteamNetworking), nameof(SteamNetworking.SendP2PPacket))]
    [HarmonyPrefix]
    private static bool SteamNetworking_SendP2PPacket(out bool __result, CSteamID steamIDRemote, byte[] pubData, uint cubData, EP2PSend eP2PSendType, int nChannel) {
        if (DedicatedClient.client == null) {
            __result = false;
            return false;
        }
        if (DedicatedClient.configLogSendPackets.Value) {
            DedicatedClient.Log.LogInfo($"Sending packet (len: {cubData}), first 10 bytes: {string.Join(", ", pubData.Take(Math.Min((int)cubData, 10)).Select(x => x.ToString()).ToArray())}");
        }
        try {
            int bytesSend = DedicatedClient.client.Client.Send(pubData, offset: 0, size: (int)cubData, SocketFlags.None);
            if (bytesSend != cubData) {
                throw new InvalidOperationException($"Trying to send {cubData} bytes, but only successfully send {bytesSend}");
            }
        } catch (SocketException) {
            DedicatedClient.UnexpectedDisconnect();
            __result = false;
            return false;
        }
        __result = true;
        return false;
    }

    [HarmonyPatch(typeof(SMessageBase), nameof(SMessageBase.Send_Start))]
    [HarmonyPrefix]
    private static void SMessageBase_Send_Start(ref ulong steamIdRemote) {
        steamIdRemote = 2; // fake steam id. 0 or 1 values are ignored because they are reserved (see SNetworkMessages.SendAllMessages)
    }
    [HarmonyPatch(typeof(SMessageBase), nameof(SMessageBase.IsMessageOnlyInLobby))]
    [HarmonyPrefix]
    private static bool SMessageBase_IsMessageOnlyInLobby(out bool __result) {
        // since steamIdRemote is always 2, the game ignores all messages that are send to the player that is not registered (CPlayer == null and not in lobby)
        // check SMessageBase.Send_End for more info
        __result = false;
        return false;
    }
    [HarmonyPatch(typeof(SNetworkMessages), nameof(SNetworkMessages.ReceiveAllMessages))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> SNetworkMessages_ReceiveAllMessages(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        static void ReceiveClientId(byte messageId, CBuffer buffer, ref CSteamID steamId, ref int messageSize) {
            if (messageId == 7 || messageId == 254 || messageId == 4 || messageId == 11 || messageId == 12) {
                messageSize += 8;
                steamId.m_SteamID = buffer.ReadULong();
            } else if (messageId == 35) {
                steamId.m_SteamID = buffer.ReadULong();
            }
        }
        var codeMatcher = new CodeMatcher(instructions, generator);
        codeMatcher.Start()
            .MatchForward(useEnd: false,
                new(OpCodes.Ldloc_S),
                new(OpCodes.Callvirt, typeof(SMessageBase).Method("IsMessageOnlyInLobby")),
                new(OpCodes.Brfalse))
            .ThrowIfInvalid("(1)")
            .Insert(
                new(OpCodes.Ldloc_S, (byte)4), // message id
                new(OpCodes.Ldloc_S, (byte)1), // buffer
                new(OpCodes.Ldloca_S, (byte)2), // steam id
                new(OpCodes.Ldloca_S, (byte)6), // message size
                Transpilers.EmitDelegate(ReceiveClientId));
        if (DedicatedClient.configLogReceivedPackets.Value) {
            codeMatcher.Start()
                .MatchForward(useEnd: false,
                    new(OpCodes.Ldloc_S),
                    new(OpCodes.Callvirt, typeof(SMessageBase).Method("IsTraced")),
                    new(OpCodes.Brfalse))
                .ThrowIfInvalid("(2)")
                .RemoveInstructions(3);
        }
        return codeMatcher.Instructions();
    }
    // [HarmonyPatch(typeof(SMessagePlayerInfos), nameof(SMessagePlayerInfos.OnReceived))]
    // [HarmonyTranspiler]
    // private static IEnumerable<CodeInstruction> SMessagePlayerInfos_OnReceived(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
    //     static CPlayer GetOrCreatePlayer(ulong steamId) {
    //         var players = SNetwork.Inst.m_players;
    //         for (int i = 0; i < players.Count; i++) {
    //             if (players[i].m_steamId == steamId) {
    //                 return players[i];
    //             }
    //         }
    //         DedicatedClient.Log.LogInfo($"Adding new player from server with steamid={steamId}");
    //         var newPlayer = new CPlayer(steamId) { m_isInLobby = true };
    //         players.Add(newPlayer);
    //         return newPlayer;
    //     }
    //     return new CodeMatcher(instructions, generator)
    //         .MatchForward(useEnd: true,
    //             new(OpCodes.Ldsfld, typeof(SMessageBase).StaticField("m_buffer")),
    //             new(OpCodes.Callvirt, typeof(CBuffer).Method("ReadULong")),
    //             new(OpCodes.Call, typeof(SNetwork).Method<ulong>("GetPlayer")))
    //         .ThrowIfInvalid("(1)")
    //         .SetInstruction(Transpilers.EmitDelegate(GetOrCreatePlayer))
    //         .Instructions();
    // }
    [HarmonyPatch(typeof(SMessageStartInfos), nameof(SMessageStartInfos.LoadWorld))]
    [HarmonyPostfix]
    private static void SMessageStartInfos_LoadWorld(SMessageStartInfos __instance) {
        __instance.m_worldData = null; // make eligible for GC
    }
    // [HarmonyPatch(typeof(SMessageStartInfos), nameof(SMessageStartInfos.LoadWorld))]
    // [HarmonyFinalizer]
    // private static void SMessageStartInfos_LoadWorld_Finalizer() {
    //     DedicatedClient.Disconnect();
    //     SScreenLoading.Inst.Deactivate();
    // }
    [HarmonyPatch(typeof(SMessageBase), nameof(SMessageBase.Send_Start))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> SMessageBase_Send_Start(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        static void InsertPlaceholder(CBuffer buffer) {
            buffer.m_pos += 4;
        }
        return new CodeMatcher(instructions, generator)
            .MatchForward(useEnd: false,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldsfld, typeof(SMessageBase).StaticField("m_buffer")),
                new(OpCodes.Ldfld, typeof(CBuffer).Field("m_pos")),
                new(OpCodes.Stfld, typeof(SMessageBase).Field("m_bufferStartPos")))
            .ThrowIfInvalid("(1)")
            .Insert(
                new(OpCodes.Ldsfld, typeof(SMessageBase).StaticField("m_buffer")),
                Transpilers.EmitDelegate(InsertPlaceholder))
            .Instructions();
    }
    [HarmonyPatch(typeof(SMessageBase), nameof(SMessageBase.Send_End))]
    [HarmonyPostfix]
    private static void SMessageBase_Send_End(SMessageBase __instance) {
        if (SMessageBase.m_buffer.m_pos == __instance.m_bufferStartPos) {
            // message is empty, removing reserved bytes for total message length
            SMessageBase.m_buffer.m_pos -= 4;
            return;
        }
        var prevPos = SMessageBase.m_buffer.m_pos;
        SMessageBase.m_buffer.m_pos = __instance.m_bufferStartPos - 4;
        SMessageBase.m_buffer.WriteUInt((uint)(prevPos - __instance.m_bufferStartPos));
        SMessageBase.m_buffer.m_pos = prevPos;
    }
    private static readonly CPlayer fakeHostPlayer = new() { m_steamId = HostSteamId };

    [HarmonyPatch(typeof(SNetwork), nameof(SNetwork.GetHost))]
    [HarmonyPrefix]
    private static bool SNetwork_GetHost(out CPlayer __result) {
        __result = fakeHostPlayer;
        return false;
    }
    [HarmonyPatch(typeof(SNetworkMessages), nameof(SNetworkMessages.OnInit))]
    [HarmonyPostfix]
    private static void SNetworkMessages_OnInit(SNetworkMessages __instance) {
        void RegisterMessage(SMessageBase msg, byte id) {
            msg.m_messageId = id;
            __instance.m_listMessages[id] = msg;
        }
        RegisterMessage(CustomMessagePlayerSession.Inst, 46);
    }
}

internal sealed class CustomMessagePlayerSession : SMessageSingleton<CustomMessagePlayerSession> {
    public override int GetBodySize() => -1;

    public void Send() {
        Send_Start(0);
        m_buffer.WriteULong(SNetwork.MySteamID);
        m_buffer.WriteByteArray(Encoding.UTF8.GetBytes(SNetwork.MySteamName));
        Send_End();
    }
    public override void OnReceived(ulong steamIdRemote, uint bufferEndPos) {
        List<ulong> connectedPlayerSteamIds = new(capacity: SNetwork.Players.Count + 1);
        while (m_buffer.m_pos < bufferEndPos) {
            ulong steamId = m_buffer.ReadULong();
            string playerName = Encoding.UTF8.GetString(m_buffer.ReadByteArray());

            connectedPlayerSteamIds.Add(steamId);
            if (!SNetwork.Players.Any(player => player.m_steamId == steamId)) {
                SScreenHudChat.Inst.AddChatMessage_Local(null, $"\"{playerName}\" joined the game.");
                SNetwork.Players.Add(new CPlayer() {
                    m_steamId = steamId,
                    m_name = playerName,
                    m_isInLobby = true
                });
            }
        }
        SNetwork.Players.RemoveAll(player => {
            if (!connectedPlayerSteamIds.Contains(player.m_steamId)) {
                SScreenHudChat.Inst.AddChatMessage_Local(null, $"\"{player.m_name}\" disconnected the game.");
                return true;
            }
            return false;
        });
    }
}

[BepInPlugin("dedicated-client", ThisPluginInfo.Name, ThisPluginInfo.Version)]
public class DedicatedClient : BaseUnityPlugin {
    public static CGuiButton btJoinDedicated = null;

    public static TcpClient client = null;
    internal static ManualLogSource Log = null;

    public static ConfigEntry<bool> configLogSendPackets;
    public static ConfigEntry<bool> configLogReceivedPackets;

    public static void Disconnect() {
        if (client == null) { return; }
        Log.LogInfo($"Disconnecting from server {client?.Client?.RemoteEndPoint}");
        client?.Close();

        client = null;

        SGameStartEnd.QuitGame(closeLobby: true);
        SScreenHome.Inst.Activate();
        SScreenLobbies.Inst.Activate();

        G.m_player = null;
        SNetwork.Inst.ClearPlayers();
        SNetworkLobbies.Inst.m_lobbies.Clear();
        SNetworkLobbies.Inst.m_currentLobby = null;

        Patches.ResetReceiveBuffer();
    }
    public static void UnexpectedDisconnect() {
        Disconnect();

        SScreenPopup.Inst.Show(null, "DEDICATED_CLIENT_CONNECTION_DISCONNECTED", "COMMON_OK");
    }

    public static void OnConnectCallback() {
        string epStr = SScreenPopup.Inst.GetInput();
        if (!Utils.TryParseIPEndPoint(epStr, out IPEndPoint endPoint)) {
            SScreenPopup.Inst.Show(callback: null, messageId: "DEDICATED_CLIENT_INVALID_IP");
            return;
        }
        client?.Close();

        client = new TcpClient();
        try {
            client.Connect(endPoint);
            client.NoDelay = true;
            Log.LogInfo($"Connected to server {client?.Client?.RemoteEndPoint}");
            CustomMessagePlayerSession.Inst.Send();

            SNetworkLobbies.Inst.m_currentLobby = new CLobby() {
                m_modName = "Multi",
                m_lobbyId = 0,
                m_gameParams = new CParams() { m_hostId = 0 },
            };
        } catch (SocketException ex) {
            SScreenPopup.Inst.Show(callback: null, messageId: "DEDICATED_CLIENT_CONNECTION_FAILED", messageArg: ex.Message);
            Log.LogError($"Connection failed: {ex}");

            Disconnect();
        }
    }

    void Awake() {
        Log = Logger;
    }

    void Start() {
        var configEnabled = Config.Bind<bool>(section: "General", key: "Enabled", defaultValue: true);
        configLogSendPackets = Config.Bind<bool>(section: "Debug", key: "LogSendPackets", defaultValue: false);
        configLogReceivedPackets = Config.Bind<bool>(section: "Debug", key: "LogReceivedPackets", defaultValue: false);

        if (!configEnabled.Value) {
            return;
        }

        Utils.AddLocalizationText("DEDICATED_CLIENT_ENTER_IP", "Enter server IP:");
        Utils.AddLocalizationText("DEDICATED_CLIENT_CONNECT", "CONNECT");
        Utils.AddLocalizationText("DEDICATED_CLIENT_INVALID_IP", "Invalid IP");
        Utils.AddLocalizationText("DEDICATED_CLIENT_CONNECTION_FAILED", "Connection failed:\n{1}");
        Utils.AddLocalizationText("DEDICATED_CLIENT_CONNECTION_DISCONNECTED", "The connection to the server has been disconnected");

        var harmony = new Harmony(Info.Metadata.GUID);
        harmony.PatchAll(typeof(Patches));
    }

    void OnDistroy() {
        client?.Close();
        client = null;
    }
}

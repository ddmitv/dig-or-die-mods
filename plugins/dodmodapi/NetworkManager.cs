
using System;
using System.Collections.Generic;
using HarmonyLib;

namespace DODModAPI;

public static class NetworkManager {
    private static readonly List<SMessageBase> _autoIdMessages = new();
    private static uint _messagesCount = 0;

    private static bool _messagesLocked = false;

    public static void Register(SMessageBase message) {
        LateRegistrationException.ThrowIfLocked(_messagesLocked);

        if (message.m_messageId != 0) {
            var msgId = message.m_messageId;
            if ((msgId >= 1 && msgId <= 35) || (msgId >= 100 && msgId <= 102) || (msgId >= 253 && msgId <= 255)) {
                throw new ArgumentException($"Network message ID {msgId} ({msgId:X}) is reserved by the game", nameof(message));
            }
            ref SMessageBase messageSlot = ref SNetworkMessages.Inst.m_listMessages[message.m_messageId];
            if (messageSlot is not null) {
                throw new ArgumentException($"Network message ID {msgId} ({msgId:X}) has been already registered by '{messageSlot.GetType()}'", nameof(message));
            }
            messageSlot = message;
        } else {
            _autoIdMessages.Add(message);
        }
        _messagesCount += 1;
    }

    internal static class Patches {
        [HarmonyPatch(typeof(SNetworkMessages), nameof(SNetworkMessages.OnInit))]
        [HarmonyPostfix]
        private static void SNetworkMessages_OnInit(SNetworkMessages __instance) {
            // skipping 0 message ID (although it should work fine but i'm not too sure) and known reserved ones
            byte freeMessageId = 36;

            var listMessages = __instance.m_listMessages;
            if (listMessages.Length != 255) {
                throw new InvalidOperationException($"[NetworkManager] Failed assumption that SNetworkMessages.Inst.m_listMessages length is equals to 255");
            }

            foreach (var msg in _autoIdMessages) {
                while (freeMessageId < 255 && listMessages[freeMessageId] is not null) {
                    freeMessageId += 1;
                }
                if (freeMessageId >= 255) {
                    throw new InvalidOperationException($"[NetworkManager] Failed to register message '{msg.GetType()}': network message ID pool is exhausted (Max message ID 254 reached)");
                }

                msg.m_messageId = freeMessageId;
                listMessages[freeMessageId] = msg;
                freeMessageId += 1;
            }
            _messagesLocked = true;
            DODModAPIPlugin.Log.LogInfo($"Added {_messagesCount} network messages ({_autoIdMessages.Count} of which have auto message IDs)");
        }
    }
}

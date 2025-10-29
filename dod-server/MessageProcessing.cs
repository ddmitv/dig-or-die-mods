
using GameEngine;
using System;

public static class MessageProcessing {
    private delegate void ReceiveFn(CBufferSpan buffer, NetworkClient client);
    private readonly record struct MessageReceiveInfo(int MessageSize, ReceiveFn? ReceiveFn);

    private static readonly MessageReceiveInfo?[] _messagesArray = new MessageReceiveInfo?[256];

    private static readonly CBuffer _sendBuffer = new();

    static MessageProcessing() {
        RegisterMsg<MessageStartInfos>();
        RegisterMsg<MessageSpawnUnit>();
        RegisterReceivingMsg<MessagePlayerPos>();
        RegisterReceivingMsg<MessageChat>();
        RegisterReceivingMsg<MessagePlayerInfos>();
        RegisterReceivingMsg<MessagePing2>();
        RegisterMsg<MessagePong2>();
        RegisterReceivingMsg<MessageCamFormat>();
        RegisterMsg<MessageInventory>();
        RegisterReceivingMsg<MessageItemsBar>();
        RegisterMsg<MessagePickups>();
        RegisterReceivingMsg<MessageRequestThrow>();
        RegisterReceivingMsg<MessageRequestDig>();
        RegisterReceivingMsg<MessageRequestBuild>();
        RegisterReceivingMsg<MessageRequestCraft>();
        RegisterReceivingMsg<CustomMessagePlayerSession>();
        RegisterReceivingMsg<MessageDoDamage>();
        RegisterReceivingMsg<MessageItemActivate>();
        RegisterReceivingMsg<MessageItemUse>();
        RegisterReceivingMsg<MessageRequestBuildWire>();
        RegisterReceivingMsg<MessageMonsterChangeTarget>();
        RegisterReceivingMsg<MessageUnitsPos>();
        RegisterReceivingMsg<MessageMonstersAttack>();
    }
    private static void RegisterMsg<T>() where T : INetworkMessage {
        byte msgId = T.MessageID;
        if (_messagesArray[msgId] is not null) {
            throw new InvalidOperationException($"Message with same ID {msgId} already exists");
        }
        _messagesArray[msgId] = new MessageReceiveInfo(T.MessageSize, null);
    }
    private static void RegisterReceivingMsg<T>() where T : IReceivingNetworkMessage {
        byte msgId = T.MessageID;
        if (_messagesArray[msgId] is not null) {
            throw new InvalidOperationException($"Message with same ID {msgId} already exists");
        }
        _messagesArray[msgId] = new MessageReceiveInfo(T.MessageSize, T.Receive);
    }

    private static bool PrepareSendBuffer<T>(in T message) where T : INetworkMessage {
        _sendBuffer.pos = 0;
        int messageSize = T.MessageSize;

        _sendBuffer.pos += 4; // placeholder for total length
        _sendBuffer.WriteByte(T.MessageID);
        if (messageSize < 0) {
            _sendBuffer.pos += 4; // placeholder for length
        }
        uint bodyStart = _sendBuffer.pos;

        message.Create(_sendBuffer);

        if (messageSize < 0) {
            uint dynamicMessageSize = _sendBuffer.pos - bodyStart;
            if (dynamicMessageSize == 0) { return false; }
            _sendBuffer.WriteUIntAt(dynamicMessageSize, pos: bodyStart - 4);
        }

        _sendBuffer.WriteUIntAt(_sendBuffer.pos - 4, pos: 0);
        return true;
    }

    public static void Send<T>(NetworkClient client, in T message) where T : INetworkMessage {
        if (PrepareSendBuffer(message)) {
            NetworkClients.SendToClient(_sendBuffer, client);
        }
    }
    public static void SendToAll<T>(in T message) where T : INetworkMessage {
        if (PrepareSendBuffer(message)) {
            NetworkClients.SendToAllClients(_sendBuffer);
        }
    }
    public static void SendToAllExcept<T>(NetworkClient excludedClient, in T message) where T : INetworkMessage {
        if (PrepareSendBuffer(message)) {
            NetworkClients.SendToAllClientsExcept(_sendBuffer, excludedClient);
        }
    }
    public static void ReceiveAll(NetworkClient client) {
        while (true) {
            CBufferSpan? buffer = client.Receive();
            if (buffer is null) { return; }

            if (buffer.IsEmpty()) {
                Logging.Error($"Message has a length of zero (client={client})");
                break;
            }
            byte messageId = buffer.ReadByte();
            var msgInfo = _messagesArray[messageId];
            if (!msgInfo.HasValue) {
                Logging.Warning($"Received a non-existant message: {MessageIdToString(messageId)} from {client}");
                break;
            }
            int messageLength = msgInfo.Value.MessageSize >= 0 ? msgInfo.Value.MessageSize : buffer.ReadInt();
            if (messageLength < 0) {
                Logging.Error($"Received a negative message length (msg={MessageIdToString(messageId)}, size={messageLength})");
                break;
            }
            if (msgInfo.Value.ReceiveFn is null) {
                Logging.Warning($"Message doesn't implements receiving functionality (msg={MessageIdToString(messageId)}), received from {client}");
            } else {
                msgInfo.Value.ReceiveFn(buffer.Subrange((uint)messageLength), client);
            }
            buffer.Advance((uint)messageLength);
        }
    }

    private static string MessageIdToString(byte id) {
        return id switch {
            1 => "SMessageTest",
            2 => "SMessagePing",
            3 => "SMessagePong",
            4 => "SMessageCamFormat",
            5 => "SMessagePlayerInfos",
            6 => "SMessageItemsBar",
            7 => "SMessagePlayerPos",
            8 => "SMessageMonsterChangeTarget",
            9 => "SMessageUnitsPos",
            10 => "SMessageMonstersAttack",
            11 => "SMessageItemUse",
            12 => "SMessageItemActivate",
            13 => "SMessageItemUpdateFlags",
            14 => "SMessageDoDamage",
            15 => "SMessageRequestMasterDrone",
            16 => "SMessageRequestDig",
            17 => "SMessageRequestRemoveWire",
            18 => "SMessageRequestRemoveBackwall",
            19 => "SMessageRequestBuildWire",
            20 => "SMessageRequestBuild",
            21 => "SMessageRequestCraft",
            22 => "SMessageRequestThrow",
            23 => "SMessageRequestStormRain",
            24 => "SMessageJoinMe",
            25 => "SMessageStartInfos",
            26 => "SMessageInventory",
            27 => "SMessageServerWorld_OnChange",
            28 => "SMessageServerWorld_Water",
            29 => "SMessagePickups",
            30 => "SMessageSpawnUnit",
            31 => "SMessageRemoveUnit",
            32 => "SMessageRocketChangeStep",
            33 => "SMessageOneBlock",
            34 => "SMessageFireMeteor",
            35 => "SMessageChat",

            46 => "CustomMessagePlayerSession",

            100 => "SMessageRequestCreative_SetItem",
            101 => "SMessageRequestCreative_SetBg",
            102 => "SMessageRequestCreative_SpawnMonster",
            253 => "SMessagePing2",
            254 => "SMessagePong2",
            _ => $"[UNKNOWN ({id})]"
        };
    }
}

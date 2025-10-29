
using GameEngine;
using System;

public readonly struct MessagePing2 : IReceivingNetworkMessage {
    public static byte MessageID => 253;
    public static int MessageSize => 6;

    public void Create(CBuffer buffer) {
        throw new NotImplementedException();
    }
    public static void Receive(CBufferSpan buffer, NetworkClient client) {
        if (client.Player is null) { return; }

        ushort versionBuild = buffer.ReadUShort();
        float pingSentTime = buffer.ReadFloat();

        MessageProcessing.Send(client, new MessagePong2(pingSentTime, client.Player.m_steamId));
    }
}

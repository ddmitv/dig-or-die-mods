
using GameEngine;

public readonly struct MessagePong2(float pingSentTime, ulong steamId) : INetworkMessage {
    public static byte MessageID => 254;
    public static int MessageSize => 6;

    public void Create(CBuffer buffer) {
        buffer.WriteULong(steamId);

        buffer.WriteUShort((ushort)GameState.m_versionBuild);
        buffer.WriteFloat(pingSentTime);
    }
}

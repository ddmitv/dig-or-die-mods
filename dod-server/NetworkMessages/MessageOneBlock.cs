
using GameEngine;

public readonly struct MessageOneBlock(ushort2 pos) : INetworkMessage {
    public static byte MessageID => 33;
    public static int MessageSize => 12;

    public void Create(CBuffer buffer) {
        ref CCell cell = ref World.Grid[pos];
        Logging.Info($"Send MessageOneBlock pos={pos} (hp={cell.m_contentHP}, id={cell.m_contentId})");

        buffer.WriteUShort2(pos);
        buffer.WriteUInt(cell.m_flags);
        buffer.WriteUShort(cell.m_contentId);
        buffer.WriteUShort(cell.m_contentHP);
    }
}

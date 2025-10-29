
using GameEngine;

public readonly struct MessageRemoveUnit(CUnit unit) : INetworkMessage {
    public static byte MessageID => 31;
    public static int MessageSize => 2;

    public void Create(CBuffer buffer) {
        Logging.Info($"(MessageRemoveUnit) Removing unit {unit}");
        buffer.WriteUShort(unit.m_id);
    }
}

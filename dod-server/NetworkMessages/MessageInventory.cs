
using GameEngine;

public readonly struct MessageInventory(CPlayer player, CStack stack) : INetworkMessage {
    public static byte MessageID => 26;
    public static int MessageSize => 6;

    public void Create(CBuffer buffer) {
        Logging.Info($"Send MessageInventory message (player={player}, stack={stack})");
        buffer.WriteUShort(player.m_unitPlayerId);
        buffer.WriteUShort(stack.m_item.m_id);
        buffer.WriteUShort((ushort)stack.m_nb);
    }
}

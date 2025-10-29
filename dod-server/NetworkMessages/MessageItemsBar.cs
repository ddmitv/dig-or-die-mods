
using GameEngine;

public readonly struct MessageItemsBar(CPlayer player, int slot) : IReceivingNetworkMessage {
    public static byte MessageID => 6;
    public static int MessageSize => 5;

    public void Create(CBuffer buffer) {
        Logging.Info($"Send MessageItemsBar message (player={player}, slot={slot})");
        buffer.WriteUShort(player.m_unitPlayerId);
        buffer.WriteByte((byte)slot);
        buffer.WriteUShort(player.m_inventory.GetItemInSlot(slot)?.m_item.m_id ?? 0);
    }
    public static void Receive(CBufferSpan buffer, NetworkClient client) {
        ushort unitPlayerId = buffer.ReadUShort();
        CPlayer? player = PlayerManager.GetPlayerByInstanceId(unitPlayerId);
        if (player is null) {
            Logging.Warning($"(MessageItemsBar) Player doesn't exists for client {client}");
            return;
        }
        if (player != client.Player) {
            Logging.Error($"(MessageItemsBar) Authentication failed: each client can send only it's player info");
            return;
        }
        byte slot = buffer.ReadByte();
        if (slot > 20) {
            Logging.Warning($"(MessageItemsBar) Invalid bar item slot number, got {slot}");
            return;
        }
        ushort itemId = buffer.ReadUShort();
        if (!GItems.IsValidItem(itemId)) {
            Logging.Warning($"(MessageItemsBar) Invalid item received with id {itemId}");
            return;
        }
        Logging.Info($"Received MessageItemsBar message (player={player}, slot={slot}, item id={itemId})");
        player.m_inventory.m_barItems[slot] = player.m_inventory.GetStack(itemId);
    }
}

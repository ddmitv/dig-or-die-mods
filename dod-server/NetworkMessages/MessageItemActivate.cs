
using GameEngine;

public readonly struct MessageItemActivate(CPlayer player, CItem item, int2 posCell, bool isMode) : IReceivingNetworkMessage {
    public static byte MessageID => 12;
    public static int MessageSize => 7;

    public void Create(CBuffer buffer) {
        buffer.WriteULong(player.m_steamId);
        buffer.WriteUShort(item.m_id);
        buffer.WriteUShort2((ushort2)posCell);
        buffer.WriteBool(isMode);
    }
    public static void Receive(CBufferSpan buffer, NetworkClient client) {
        if (client.Player is not CPlayer player) {
            Logging.Warning($"(MessageItemActivate) Player doesn't exists for client {client}");
            return;
        }
        ushort itemId = buffer.ReadUShort();
        if (!GItems.TryGetItem(itemId, out CItemCell item)) {
            Logging.Warning($"(MessageItemActivate) Received invalid item id: {itemId}");
            return;
        }
        int2 pos = buffer.ReadUShort2();
        bool isMode = buffer.ReadBool();
        if (isMode) {
            item.ChangeMode_Local(pos);
        } else {
            item.Activate_Local(player, pos);
        }
        MessageProcessing.SendToAllExcept(client, new MessageItemActivate(player, item, pos, isMode));
    }
}

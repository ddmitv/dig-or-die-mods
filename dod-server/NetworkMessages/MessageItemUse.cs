
using GameEngine;

public readonly struct MessageItemUse(CPlayer player, CItem item, Vector2 mousePos, bool isShift) : IReceivingNetworkMessage {
    public static byte MessageID => 11;
    public static int MessageSize => 6;

    private const ushort IsShiftOffset = 32767; // note: this number is NOT power of 2, in fact it's one less to be a power of 2

    public void Create(CBuffer buffer) {
        buffer.WriteULong(player.m_steamId);
        buffer.WriteUShort((ushort)(item.m_id + (isShift ? IsShiftOffset : 0)));
        buffer.WriteVector2_asUShort2(mousePos);
    }
    public static void Receive(CBufferSpan buffer, NetworkClient client) {
        if (client.Player is not CPlayer player) {
            Logging.Warning($"(MessageItemUse) Player doesn't exists for client {client}");
            return;
        }
        ushort itemIdAndIsShift = buffer.ReadUShort();
        Vector2 mousePos = buffer.ReadVector2_FromUshort2();
        bool isShift = itemIdAndIsShift >= IsShiftOffset;

        ushort itemId = (ushort)(itemIdAndIsShift - (isShift ? IsShiftOffset : 0u));
        if (!GItems.TryGetItem(itemId, out CItem item)) {
            Logging.Warning($"(MessageItemUse) Received invalid item with id={itemId}");
            return;
        }
        item.Use_Local(player, mousePos, isShift);

        MessageProcessing.SendToAllExcept(client, new MessageItemUse(player, item, mousePos, isShift));
    }
}

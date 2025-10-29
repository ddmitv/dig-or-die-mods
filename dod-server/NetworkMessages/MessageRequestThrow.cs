
using GameEngine;
using System;

public readonly struct MessageRequestThrow : IReceivingNetworkMessage {
    public static byte MessageID => 22;
    public static int MessageSize => 9;

    public void Create(CBuffer buffer) {
        throw new NotImplementedException();
    }
    public static void Receive(CBufferSpan buffer, NetworkClient client) {
        Logging.Info($"Recieved MessageRequestThrow from {client}");
        if (client.Player is null) {
            Logging.Warning($"(MessageRequestThrow) Player doesn't exists for client {client}");
            return;
        }
        CPlayer player = client.Player;
        if (!GItems.TryGetItem(buffer.ReadUShort(), out CItem item)) {
            Logging.Warning($"(MessageRequestThrow) Invalid item received");
            return;
        }
        ushort2 pickupPos = buffer.ReadUShort2();
        bool random = buffer.ReadBool();
        ushort unitToSendToId = buffer.ReadUShort();

        if (player.m_inventory.RemoveFromInventory(item)) {
            CUnitPlayer? unitToSendTo = UnitManager.GetUnitById(unitToSendToId) as CUnitPlayer;

            PickupManager.CreatePickup(item, 1, new Vector2(pickupPos.x, pickupPos.y) + Vector2.one * 0.5f,
                threwByPlayer: player.m_unitPlayer, moveToPlayer: unitToSendTo, forceRandom: random);
        }
    }
}


using GameEngine;
using System;

public readonly struct MessageRequestBuildWire : IReceivingNetworkMessage {
    public static byte MessageID => 19;
    public static int MessageSize => 7;

    public void Create(CBuffer buffer) {
        throw new NotImplementedException();
    }
    public static void Receive(CBufferSpan buffer, NetworkClient client) {
        if (client.Player is not CPlayer player) {
            Logging.Warning($"(MessageRequestBuildWire) Player doesn't exists for client {client}");
            return;
        }
        ushort2 cellPos = buffer.ReadUShort2();
        ushort itemId = buffer.ReadUShort();
        if (!GItems.TryGetItem(itemId, out CItem_MachineWire itemWire)) {
            Logging.Warning($"(MessageRequestBuildWire) Invalid item id: {itemId}");
            return;
        }
        byte wireDir = buffer.ReadByte();
        if (wireDir != 0 && wireDir != 1) {
            Logging.Warning($"(MessageRequestBuildWire) Invalid wire direction: {wireDir}");
            return;
        }
        ref CCell cell = ref World.Grid[cellPos.x, cellPos.y];

        bool cellHasWire = cell.HasFlag(CCell.Flag_HasWireRight << wireDir);
        if (cellHasWire) {
            player.m_inventory.AddToInventory(itemWire, nb: 1);
        } else {
            if (!player.m_inventory.RemoveFromInventory(itemWire, nb: 1)) {
                Logging.Warning($"(MessageRequestBuildWire) Player doesn't have enough wires in their inventory");
                return;
            }
        }
        cell.SetFlag(CCell.Flag_HasWireRight << wireDir, !cellHasWire);
        World.OnSetContent(cellPos);
    }
}

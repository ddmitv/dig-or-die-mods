
using GameEngine;

public readonly struct MessageRequestDig(int2 pos, short damage) : IReceivingNetworkMessage {
    public static byte MessageID => 16;
    public static int MessageSize => 6;

    public void Create(CBuffer buffer) {
        buffer.WriteUShort2((ushort2)pos);
        buffer.WriteShort(damage);
    }
    public static void Receive(CBufferSpan buffer, NetworkClient client) {
        if (client.Player is null) {
            Logging.Warning($"(MessageRequestDig) Player doesn't exists for client {client}");
            return;
        }
        ushort2 cellPos = buffer.ReadUShort2();
        short damage = buffer.ReadShort();
        CUnitPlayer? cunitPlayer = UnitManager.GetUnitById(client.Player.m_unitPlayerId) as CUnitPlayer;

        int contentHP = World.Grid[cellPos.x, cellPos.y].m_contentHP;
        if (contentHP > 0) {
            if (damage > 0) {
                World.DoDamageToCell(cellPos, (ushort)damage, 2, false, cunitPlayer);
            } else {
                World.RepairCell(cellPos, (ushort)-damage);
            }
        }
        MessageProcessing.SendToAllExcept(client, new MessageRequestDig(cellPos, damage));
    }
}

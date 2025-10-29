
using GameEngine;

public readonly struct MessageMonstersAttack(CUnitMonster unit, Vector2 targetPos) : IReceivingNetworkMessage {
    public static byte MessageID => 10;
    public static int MessageSize => 6;

    public void Create(CBuffer buffer) {
        buffer.WriteUShort(unit.m_id);
        buffer.WriteVector2_asUShort2(targetPos);
    }
    public static void Receive(CBufferSpan buffer, NetworkClient client) {
        ushort unitId = buffer.ReadUShort();
        if (UnitManager.GetUnitById(unitId) is not CUnitMonster unit) {
            Logging.Warning($"(MessageMonstersAttack) Received invalid unit monster id: {unitId}");
            return;
        }
        Vector2 targetPos = buffer.ReadVector2_FromUshort2();
        // unit.Attack_Local();

        MessageProcessing.SendToAllExcept(client, new MessageMonstersAttack(unit, targetPos));
    }
}

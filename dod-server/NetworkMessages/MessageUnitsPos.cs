
using GameEngine;

public readonly struct MessageUnitsPos(bool sendEvenIfNetworkControled = false) : IReceivingNetworkMessage {
    public static byte MessageID => 9;
    public static int MessageSize => -1;

    public void Create(CBuffer buffer) {
        foreach (CUnit unit in UnitManager.units) {
            CUnitMonster? unitMonster = unit as CUnitMonster;
            if (unit is not CUnitPlayer
                && (unitMonster is null || !unitMonster.IsNetworkControlled() || sendEvenIfNetworkControled)
                && (unit.CanMove() || unitMonster is null || (unitMonster.Target is not null && unitMonster.IsAlive()))
                && (unitMonster is null || (unitMonster.CanMove() && (unitMonster.IsAlive() || unitMonster.m_speed.SqrMagnitude() > 0.1f)) || Game.FrameCount % 31 == (ulong)(unitMonster.m_rand * 30f)))
            // if (unit is not CUnitPlayer && (sendEvenIfNetworkControled || unitMonster is null || !unitMonster.IsNetworkControlled()))
            {
                buffer.WriteUShort(unit.m_id);
                buffer.WriteVector2_asUShort2(unit.m_pos, 1024f, ceilingY: true);
                buffer.WriteVector2_asShort2(unit.m_speed, 100f);
                buffer.WriteFloat_asShort(unit.m_lookingAngleRad, 3.5f);
                buffer.WriteFloat_asUShort(unit.m_hp, 20000f, ceilingY: true);
            }
        }
    }
    public static void Receive(CBufferSpan buffer, NetworkClient client) {
        while (!buffer.IsEmpty()) {
            ushort unitId = buffer.ReadUShort();
            Vector2 unitPos = buffer.ReadVector2_FromUshort2(1024f);
            Vector2 unitSpeed = buffer.ReadVector2_FromShort2(100f);
            float unitLookAngle = buffer.ReadFloat_fromShort(3.5f);
            float unitHp = buffer.ReadFloat_fromUShort(20000f);

            if (UnitManager.GetUnitById(unitId) is not CUnit unit) {
                Logging.Warning($"(MessageUnitsPos) Unit with id {unitId} not found (client={client})");
                return;
            }
            if (!(unit as CUnitMonster)?.IsNetworkControlled() ?? false) {
                Logging.Warning($"(MessageUnitsPos) Unit {unit} is not network controlled (client={client})");
                return;
            }
            unit.m_pos = unitPos;
            unit.m_speed = unitSpeed;
            unit.m_lookingAngleRad = unitLookAngle;
            unit.m_lookingDirection = Vector2.FromUnitPolar(unitLookAngle);
            unit.SetHp(unitHp);
        }
        // MessageProcessing.SendToAllExcept(client, new MessageUnitsPos(sendEvenIfNetworkControled: true));
    }
}

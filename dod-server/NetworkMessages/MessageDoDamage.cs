
using GameEngine;

public readonly struct MessageDoDamage(CUnit unit, float damage, CUnit? attacker, bool showDamage) : IReceivingNetworkMessage {
    public static byte MessageID => 14;
    public static int MessageSize => 9;

    public void Create(CBuffer buffer) {
        buffer.WriteUShort(unit.m_id);
        buffer.WriteFloat(damage);
        buffer.WriteUShort(attacker?.m_id ?? ushort.MaxValue);
        buffer.WriteBool(showDamage);
    }
    public static void Receive(CBufferSpan buffer, NetworkClient client) {
        ushort unitId = buffer.ReadUShort();
        if (UnitManager.GetUnitById(unitId) is not CUnit unit) {
            Logging.Warning($"(MessageDoDamage) Unit with id {unitId} doesn't exists");
            return;
        }
        float damage = buffer.ReadFloat();
        CUnit? attacker = UnitManager.GetUnitById(buffer.ReadUShort());
        bool showDamage = buffer.ReadBool();
        unit.DamageLocal(damage, attacker, showDamage);

        MessageProcessing.SendToAllExcept(client, new MessageDoDamage(unit, damage, attacker, showDamage));

        Logging.Info($"(MessageDoDamage) Unit {unit} received {damage} damage{(attacker is null ? "" : $" by attacker {attacker}")}");
    }
}

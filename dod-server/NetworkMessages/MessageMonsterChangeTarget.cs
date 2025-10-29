
using GameEngine;

public readonly struct MessageMonsterChangeTarget(CUnitMonster monster, CUnit? target) : IReceivingNetworkMessage {
    public static byte MessageID => 8;
    public static int MessageSize => 7;

    public void Create(CBuffer buffer) {
        buffer.WriteUShort(monster.m_id);
        buffer.WriteUShort(target?.m_id ?? ushort.MaxValue);
        buffer.WriteBool(monster.m_isNightSpawn);
        buffer.WriteFloat_asUShort(monster.m_hp, 20000f, true);
    }
    public static void Receive(CBufferSpan buffer, NetworkClient client) {
        ushort monsterId = buffer.ReadUShort();
        if (UnitManager.GetUnitById(monsterId) is not CUnitMonster monster) {
            Logging.Warning($"(MessageMonsterChangeTarget) Received invalid monster unit id: {monsterId}");
            return;
        }
        ushort targetId = buffer.ReadUShort();
        CUnit? target = UnitManager.GetUnitById(targetId);
        monster.SetTargetFromNetwork(target);

        monster.m_isNightSpawn = buffer.ReadBool();
        monster.SetHp(buffer.ReadFloat_fromUShort(20000f));

        MessageProcessing.SendToAllExcept(client, new MessageMonsterChangeTarget(monster, target));
    }
}

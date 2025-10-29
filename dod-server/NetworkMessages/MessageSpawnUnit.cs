
using GameEngine;

public readonly struct MessageSpawnUnit(CUnit.CDesc uDesc, Vector2 pos, ushort instanceId = 65535) : INetworkMessage {
    public static byte MessageID => 30;
    public static int MessageSize => -1;

    public const byte playerUnitId = 1;
    public const byte defenseUnitId = 3;

    public void Create(CBuffer buffer) {
        if (uDesc.m_id == 0) {
            Logging.Error($"(MessageSpawnUnit) Sending unit with id={uDesc.m_id}");
            return;
        }
        if (!World.IsInRectM2(pos)) {
            Logging.Error($"(MessageSpawnUnit) Sending unit ({uDesc}) that it's position outside world (pos={pos})");
            return;
        }
        buffer.WriteByte(uDesc.m_id);
        buffer.WriteVector2(pos);
        buffer.WriteUShort(instanceId);
        if (uDesc == GUnits.player) {
            if (PlayerManager.GetPlayerByInstanceId(instanceId) is CPlayer player) {
                buffer.WriteULong(player.m_steamId);
            } else {
                Logging.Error($"(MessageSpawnUnit) Player doesnt exists for unit instance id {instanceId}. Falling back to sending '0' steam id");
                buffer.WriteULong(0);
            }
        } else if (uDesc == GUnits.defense) {
            buffer.WriteUShort(World.Grid[(int)pos.x, (int)pos.y].m_contentId);
        }
    }
}

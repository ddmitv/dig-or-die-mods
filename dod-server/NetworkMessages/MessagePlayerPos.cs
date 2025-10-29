
using GameEngine;
using System;

public readonly struct MessagePlayerPos(CUnitPlayer playerUnit) : IReceivingNetworkMessage {
    public static byte MessageID => 7;
    public static int MessageSize => 12;

    public void Create(CBuffer buffer) {
        CPlayer? player = PlayerManager.GetPlayerByUnit(playerUnit);
        if (player is null) {
            Logging.Error($"(MessagePlayerPos) Player doesnt exists for unit id={playerUnit.m_id}");
            return;
        }

        buffer.WriteULong(player.m_steamId);
        buffer.WriteVector2_asUShort2(playerUnit.m_pos, maxValue: 1024f);
        buffer.WriteVector2_asShort2(playerUnit.m_speed, maxValue: 100f);
        buffer.WriteFloat_asShort(playerUnit.m_lookingAngleRad, maxValue: 3.5f); // original: Mathf.Atan2(G.m_player.GetLookingDirection().y, G.m_player.GetLookingDirection().x), 3.5f
        buffer.WriteFloat_asUShort(playerUnit.m_hp, maxValue: 1000f);
    }

    public static void Receive(CBufferSpan buffer, NetworkClient client) {
        if (client.Player is null || client.Player.m_unitPlayer is null) {
            Logging.Error($"(MessagePlayerPos) Player doesnt exists for client {client}");
            return;
        }
        CUnitPlayer playerUnit = client.Player.m_unitPlayer;
        // note: max coordinate for player is X: 1024 and Y: 1024
        Vector2 playerPos = buffer.ReadVector2_FromUshort2(1024f);
        // note: max player velocity can only be from X: [-100, 100] and Y: [-100, 100]
        Vector2 playerVel = buffer.ReadVector2_FromShort2(100f);
        float playerLookAngle = buffer.ReadFloat_fromShort(3.5f);
        // note: max player hp can only be 1000
        float playerHp = buffer.ReadFloat_fromUShort(1000f);

        playerUnit.m_pos = playerPos;
        playerUnit.m_speed = playerVel;
        playerUnit.m_lookingDirection = Vector2.FromUnitPolar(playerLookAngle);
        playerUnit.m_lookingAngleRad = playerLookAngle; // original: Mathf.Atan2(this.m_lookingDirection.y, this.m_lookingDirection.x);
        playerUnit.m_isFacingRight = (MathF.Abs(playerUnit.m_speed.x) <= 0.2f ? playerUnit.m_lookingDirection : playerUnit.m_speed).x >= 0f;
        playerUnit.SetHp(playerHp);
        client.Player.m_lastTimeMessagePosReceived = GVars.SimuTimeD;

        MessageProcessing.SendToAllExcept(client, new MessagePlayerPos(playerUnit));
    }
}

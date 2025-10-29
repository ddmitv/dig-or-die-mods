
using GameEngine;
using System.Linq;

public readonly struct MessagePickups(CPlayer player) : INetworkMessage {
    public static byte MessageID => 29;
    public static int MessageSize => -1;

    private const ushort RemovePickupOffset = 32767;

    public void Create(CBuffer buffer) {
        RectInt rectAroundScreen = player.GetRectAroundScreen(3);
        if (player.m_pickupsSendTime.Count < PickupManager.pickups.Count) {
            player.m_pickupsSendTime.AddRange(Enumerable.Repeat(0f, PickupManager.pickups.Count - player.m_pickupsSendTime.Count));
        }
        foreach (CPickup pickup in PickupManager.pickups) {
            if (!rectAroundScreen.Contains(pickup.m_pos) || pickup.m_lastUpdateTime <= player.m_pickupsSendTime[pickup.m_id]) {
                continue;
            }
            buffer.WriteUShort((ushort)(pickup.m_id + (pickup.m_active ? 0 : RemovePickupOffset)));
            if (pickup.m_active) {
                buffer.WriteUShort(pickup.m_item.m_id);
                buffer.WriteVector2_asUShort2(pickup.m_pos);
                buffer.WriteVector2_asShort2(pickup.m_speed, maxValue: 128f);
                buffer.WriteShort((pickup.m_threwByPlayer, pickup.m_moveToPlayer) switch {
                    (not null, _) => (short)pickup.m_threwByPlayer.m_id,
                    (null, not null) => (short)(pickup.m_moveToPlayer.m_id + 10000),
                    _ => -1
                });
            }
            player.m_pickupsSendTime[pickup.m_id] = GVars.SimuTime;
        }
    }
}

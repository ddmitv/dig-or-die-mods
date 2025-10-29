
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GameEngine;

public static class PickupManager {
    public static readonly CPickupList pickups = [];
    private static double lastTimeMessageSent = 0;

    public static void CreatePickup(CItem item, int nb, Vector2 pos, bool withSpeed = true, CUnitPlayer? threwByPlayer = null, CUnitPlayer? moveToPlayer = null, bool forceRandom = false) {
        for (int i = 0; i < nb; ++i) {
            pickups.Add(new CPickup(item, pos, withSpeed, threwByPlayer, moveToPlayer, forceRandom));
        }
    }
    public static void CleanAll() {
        pickups.Clear();
        lastTimeMessageSent = 0;
    }
    public static void Update() {
        foreach (CPickup pickup in pickups) {
            if (pickup.m_active) {
                pickup.Update();
            }
        }
        if (GVars.SimuTimeD > lastTimeMessageSent) {
            lastTimeMessageSent = GVars.SimuTimeD + 0.15;

            foreach (var player in PlayerManager.players) {
                if (player.networkClient is null || player.m_unitPlayer is null) { continue; }
                MessageProcessing.Send(player.networkClient, new MessagePickups(player));
            }
        }
    }
}

public sealed class CPickupList : IEnumerable<CPickup> {
    private readonly List<CPickup> list = new(capacity: 256);

    public int Count => list.Count;
    public int GetCountActives() => list.Count(x => x.m_active);
    public void Clear() => list.Clear();
    public CPickup this[int index] => list[index];

    public void Add(CPickup pickup) {
        for (int i = 0; i < list.Count; i++) {
            if (!list[i].m_active) {
                pickup.m_id = (ushort)i;
                list[i] = pickup;
                return;
            }
        }
        pickup.m_id = (ushort)list.Count;
        list.Add(pickup);
    }

    public IEnumerator<CPickup> GetEnumerator() => list.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();
}

public sealed class CPickup {
    public CItem m_item;
    public Vector2 m_pos;
    public float m_creationTime;
    public Vector2 m_speed;
    public float m_lastUpdateTime;
    public CUnitPlayer? m_threwByPlayer = null;
    public CUnitPlayer? m_moveToPlayer = null;

    public ushort m_id = 0;
    public bool m_active = true;

    public CPickup(CItem item, Vector2 pos, bool withSpeed = true, CUnitPlayer? threwByPlayer = null, CUnitPlayer? moveToPlayer = null, bool forceSpeedRandom = false) {
        m_item = item;
        m_pos = threwByPlayer?.PosCenter ?? pos;
        m_creationTime = GVars.SimuTime;
        m_threwByPlayer = threwByPlayer;
        m_moveToPlayer = moveToPlayer;
        m_speed = Vector2.zero;
        if (withSpeed) {
            if (threwByPlayer is null || forceSpeedRandom) {
                m_speed = new Vector2(-2f * 4f * Random.Float(), 0.5f + 1.5f * Random.Float());
            } else {
                m_speed = new Vector2((pos.x > threwByPlayer.PosCenter.x ? 20f : -20f) * (3f + Random.Float()), 5f + Random.Float());
            }
        }
    }

    private CUnitPlayer? GetBestPlayerToPick() {
        if (m_item.m_pickupPreventPick1sec && GVars.SimuTimeD < m_creationTime + 1f) { return null; }
        if (m_threwByPlayer is not null && GVars.SimuTimeD < m_creationTime + 0.5f && m_moveToPlayer is null) { return null; }
        if (m_threwByPlayer is not null && GVars.SimuTimeD < m_creationTime + 0.2f) { return null; }

        CUnitPlayer? currentPlayer = null;
        float currentScore = float.MaxValue;
        foreach (var player in PlayerManager.players) {
            CUnitPlayer? playerUnit = player.m_unitPlayer;
            if (playerUnit is null || !playerUnit.IsAlive() || player.IsAFK()) { continue; }
            if (m_threwByPlayer == playerUnit && GVars.SimuTimeD < m_creationTime + 1.5 && m_moveToPlayer is null) { continue; }

            float score = (playerUnit!.PosCenter - m_pos).SqrMagnitude();
            if (score <= Utils.Sqr(3.5f) || m_item.m_pickupAutoPicked || m_moveToPlayer is not null) {
                score -= m_moveToPlayer != playerUnit ? 0f : Utils.Sqr(2000f);
                if (score <= currentScore) {
                    currentPlayer = playerUnit;
                    currentScore = score;
                }
            }
        }
        return currentPlayer;
    }
    public void Update() {
        CUnitPlayer? bestPlayer = GetBestPlayerToPick();
        if (bestPlayer is not null) {
            float num = 15f + (m_moveToPlayer is null ? 0f : Math.Clamp(30f * (float)(GVars.SimuTimeD - m_creationTime), 0f, 15f));
            float num2 = Utils.MoveTowards(m_speed.magnitude, num, 20 * (float)Game.SimuDeltaTime);
            m_speed = (bestPlayer.PosFire - m_pos).normalized * num2;
            m_pos = Vector2.MoveTowards(m_pos, bestPlayer.PosFire, num2 * (float)Game.SimuDeltaTime);
            if (m_pos == bestPlayer.PosFire) {
                if (PlayerManager.GetPlayerByUnit(bestPlayer)?.m_inventory.AddToInventory(m_item, 1, false, true) is null) {
                    Logging.Warning($"Player doesnt exists for unit {bestPlayer}");
                }
                m_active = false;
            }
            m_lastUpdateTime = GVars.SimuTime;
        } else {
            m_speed.y -= 15f * Game.SimuDeltaTime;
            CCell cell = World.Grid[(int)m_pos.x, (int)m_pos.y];
            if (cell.HasFlag(CCell.Flag_StreamLFast)) {
                m_speed.x -= 6f * Game.SimuDeltaTime;
            }
            if (cell.HasFlag(CCell.Flag_StreamRFast)) {
                m_speed.x += 6f * Game.SimuDeltaTime;
            }
            m_speed.x = Math.Clamp(m_speed.x, -5f, 5f);
            Vector2 prevPos = m_pos;
            Vector2 nextPos = m_pos + m_speed * Game.SimuDeltaTime;
            nextPos = Vector2.Clamp(nextPos, Vector2.one, (Vector2)World.Gs - Vector2.one);
            if (m_speed.x > 0f && World.Grid[(int)(nextPos.x + 0.375f), (int)m_pos.y].IsPassable()) {
                m_pos.x = nextPos.x;
            } else if (m_speed.x < 0f && World.Grid[(int)(nextPos.x - 0.375f), (int)m_pos.y].IsPassable()) {
                m_pos.x = nextPos.x;
            }
            if (m_speed.y > 0f || !World.Grid[(int)nextPos.x, (int)(nextPos.y - 0.375f)].IsContentBlockOrPlat()) {
                m_pos.y = nextPos.y;
            } else {
                m_speed.y = 0f;
            }
            m_speed.x = Utils.MoveTowards(m_speed.x, 0f, 3f * Game.SimuDeltaTime);
            if (m_pos != prevPos) {
                m_lastUpdateTime = GVars.SimuTime;
            }
        }
        if (m_item.m_pickupDuration >= 0 && (GVars.SimuTimeD > (double)(m_creationTime + m_item.m_pickupDuration)) || (World.Grid[(int)m_pos.x, (int)m_pos.y].IsLava(0.7f))) {
            m_active = false;
            m_lastUpdateTime = GVars.SimuTime;
        }
    }
}

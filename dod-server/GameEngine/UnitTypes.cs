
using System;

namespace GameEngine;

#pragma warning disable IDE0044, IDE0051, CS0649, CS0169, CS0414

public class CUnit {
    public ushort m_id;
    public CDesc m_uDesc;
    public readonly float m_rand;
    protected int m_frameSpawn = -1;
    public Vector2 m_pos;
    protected Vector2 m_posLastFrame;
    public Vector2 m_posLastOnGroundOrWater;
    public Vector2 m_speed;
    public Vector2 m_speedLastFrame = Vector2.zero;
    protected Vector2 m_forces;
    protected Vector2 m_forcesPush;
    protected bool m_onGround;
    public Vector2 m_lookingDirection = Vector2.right;
    public float m_lookingAngleRad;
    protected bool m_isInWater;
    protected bool m_isUnderWater;
    protected bool m_isFootInWater;
    protected bool m_isWalkingOnWalls;
    protected int2 m_lastCollisionBlock = int2.negative;
    protected float m_lastTimeOnGroundOnWater = float.MinValue;
    public float m_hp;
    protected float m_lastFireTime = float.MinValue;
    protected float m_deathTime = float.MinValue;
    protected float m_hurtTime = float.MinValue;
    public float m_air = 1f;
    private float m_lastAirHit;
    protected float m_burnStartTime = float.MinValue;
    protected float m_burnLastTimeHit = float.MinValue;
    private float m_crushedDelay = 1f;
    private bool m_deadAndHitGround;

    public class CDesc {
        public required string m_codeName;
        public required byte m_id;
        public int m_tier;
        public float m_speedMax;
        public float m_hpMax;
        public int m_armor;
        public Vector2 m_size;
        public Color24 m_emitLight = default;
        public float m_regenSpeed;
        public bool m_canDive;
        public bool m_immuneToFire;
        public bool m_skipColsOnPlatforms;
        public bool m_isSpawnPriority;

        public CDesc(int tier, float speed, Vector2 size, int hpMax, int armor) {
            m_tier = tier;
            m_speedMax = speed;
            m_hpMax = hpMax;
            m_armor = armor;
            m_size = size;
        }

        public virtual float GetHpMax() => m_hpMax;
        public virtual float GetSpeedMax() => m_speedMax;

        public virtual CUnit CreateUnit(Vector2 pos) => throw new NotImplementedException($"CreateUnit not implemented for '{GetType()}'");

        public override string ToString() => $"{m_codeName}";
    }

    // used by reflection
    protected CUnit(CUnit.CDesc uDesc, Vector2 pos) {
        m_uDesc = uDesc;
        m_hp = uDesc.m_hpMax;
        m_pos = pos;
        m_posLastFrame = m_pos;
        m_rand = Random.Float();
        m_posLastOnGroundOrWater = m_pos;
        // this.m_frameSpawn = Time.frameCount; // only used in pathfinding
    }

    public override string ToString() => $"{{\"{m_uDesc}\" (id={m_id}, hp={m_hp})}}";

    public void SetHp(float value, bool skipDeathCheck = false) {
        if (m_hp > 0f && value <= 0f && m_deathTime < 0f && !skipDeathCheck) {
            Logging.Info($"Detect death of id={m_id} (not detected at damage). health: {m_hp} => {value} m_deathTime={m_deathTime}");
            OnDeath(null, string.Empty);
        }
        m_hp = value;
    }
    protected virtual void OnDeath(CUnit? attacker, string damageCause = "") {
        m_deathTime = GVars.SimuTime;
    }
    protected virtual void OnHit(CUnit? attacker) { }

    public void Damage(float damage, CUnit? attacker = null, bool showDamage = false, string damageCause = "") {
        MessageProcessing.SendToAll(new MessageDoDamage(this, damage, attacker, showDamage));
        DamageLocal(damage, attacker, showDamage, damageCause);
    }
    public virtual void DamageLocal(float damage, CUnit? attacker = null, bool showDamage = false, string damageCause = "") {
        m_hp = Math.Max(0f, m_hp - damage);
        OnHit(attacker);
        if (m_hp == 0f && m_deathTime < 0f) {
            OnDeath(attacker, damageCause);
        }
        if (damage > 0f) {
            m_hurtTime = GVars.SimuTime;
        }
    }
    public virtual bool Update() {
        if (!World.IsInRectM2(m_pos)) {
            Logging.Warning($"Unit {this} is out of the world (pos={m_pos}, CanMove={CanMove()})");
            return false;
        }
        float simuDeltaTime = Game.SimuDeltaTime;
        if (IsAlive()) {
            if (this is not CUnitFish || m_isInWater) {
                m_hp = Math.Min(GetHpMax(), m_hp + GetHpMax() * m_uDesc.m_regenSpeed * Game.SimuDeltaTime);
            }
        } else if (m_onGround || m_isInWater || m_isFootInWater) {
            m_deadAndHitGround = true;
        }
        if (CanMove()) {
            m_lookingAngleRad = MathF.Atan2(m_lookingDirection.y, m_lookingDirection.x);
            m_isInWater = World.IsInWater(PosCenter);
            m_isUnderWater = m_isInWater && World.IsInWater(PosCenter + 0.4f * m_uDesc.m_size.y * Vector2.up);
            m_isFootInWater = World.IsInWater(PosCenter - 0.3f * m_uDesc.m_size.y * Vector2.up);
        //     if ((World.IsInWater(PosCenter + Vector2.up * 0.2f * m_uDesc.m_size.y) && !m_isWalkingOnWalls) || (!IsAlive() && World.IsInWater(PosCenter - Vector2.up * 0.4f * m_uDesc.m_size.y))) {
        //         m_forces.y += 60f * simuDeltaTime;
        //     }
            if (m_isInWater || m_onGround) {
                m_posLastOnGroundOrWater = PosCenter;
                m_lastTimeOnGroundOnWater = GVars.SimuTime;
            }
            ref readonly CCell cell = ref World.Grid[PosCell];
        //     if (cell.HasFlag(CCell.Flag_StreamLFast)) {
        //         m_forces.x -= 30f * simuDeltaTime;
        //     }
        //     if (cell.HasFlag(CCell.Flag_StreamRFast)) {
        //         m_forces.x += 30f * simuDeltaTime;
        //     }
        //     if (m_onGround) {
        //         m_forces.x -= 8f * m_speed.x * simuDeltaTime;
        //     }
        //     m_forces -= 1.7f * m_speed * simuDeltaTime;
        //     if (!m_isWalkingOnWalls) {
        //         m_forces.y -= 50f * simuDeltaTime * ((!(this is CUnitPlayer)) ? 1f : SOutgame.Params.m_gravityPlayers) * SEnvironment.GetEnvironmentCurrent().m_unitsGravityMult;
        //     } else {
        //         m_forces -= 5f * m_speed * simuDeltaTime;
        //     }
        //     if (m_isInWater) {
        //         m_forces -= 3f * m_speed * simuDeltaTime;
        //     }
        //     m_speed += m_forces + m_forcesPush;
        //     m_speed.x = Math.Clamp(m_speed.x, -30f, 30f);
        //     m_speed.y = Math.Clamp(m_speed.y, -30f, 30f);
        //     if (m_onGround && Math.Abs(m_speed.x) < 0.5f && Math.Abs(m_forces.x) < 0.5f) {
        //         m_speed.x = 0f;
        //     }
        //     Vector2 newPos = World.ClampRectM2(m_pos + m_speed * simuDeltaTime);
        //     Vector2 pos = SMiscCols.CheckCol_UnitGround(this, newPos, ref m_lastCollisionBlock);
        //     m_onGround = pos.y > newPos.y && !m_isInWater;
        //     int num = 0;
        //     string damageCause = string.Empty;
        //     Vector2 vector = ((!(m_speed.sqrMagnitude < m_speedLastFrame.sqrMagnitude)) ? m_speedLastFrame : m_speed);
        //     if (pos.y != newPos.y) {
        //         num = (int)Mathf.Max(0f, 13f * Mathf.Max(0f, Mathf.Abs(vector.y) - 22f) * ((!(this is CUnitPlayer)) ? 0.4f : 1f) * Mathf.Clamp01(GVars.SimuTime - m_lastTimeOnGroundOnWater));
        //         m_speed.y = 0f;
        //         damageCause = ((!(pos.y > newPos.y)) ? "hit_up" : "hit_down");
        //     }
        //     if (pos.x != newPos.x) {
        //         num = (int)Mathf.Max(num, 13f * Mathf.Max(0f, Mathf.Abs(vector.x) - 22f) * ((!(this is CUnitPlayer)) ? 0.4f : 1f) * Mathf.Clamp01(GVars.SimuTime - m_lastTimeOnGroundOnWater));
        //         m_speed.x = 0f;
        //         damageCause = "hit_side";
        //     }
        //     if (num >= 1) {
        //         num = Math.Max(1, (int)((float)num * GetArmorMult()) - (int)GetArmor());
        //     }
        //     if (num > 0 && IsAlive()) {
        //         Damage(num, this, showDamage: true, damageCause);
        //     }
        //     m_posLastFrame = m_pos;
        //     if (!IsNetworkControlled()) {
        //         m_pos = pos;
        //     }
        //     m_pos.x = Mathf.Clamp(m_pos.x, 13f + 0.5f * m_uDesc.m_size.x, (float)SWorld.Gs.x - 13f - 0.5f * m_uDesc.m_size.x);
        //     m_pos.y = Mathf.Clamp(m_pos.y, 13f, (float)SWorld.Gs.y - 13f);
        //     m_forces = Vector2.zero;
        //     m_forcesPush = Vector2.zero;
            if (IsAlive()) {
                if (m_isUnderWater && this is not CUnitFish && this is not CUnitBoss) {
                    m_air = Utils.MoveTowards(m_air, 0f, 0.07f * Game.SimuDeltaTime * 0.7f);
                } else if (this is CUnitFish && !m_isInWater) {
                    m_air = Utils.MoveTowards(m_air, 0f, Game.SimuDeltaTime);
                } else {
                    m_air = Utils.MoveTowards(m_air, 1f, 0.5f * Game.SimuDeltaTime);
                }
                if (m_air <= 0f && Game.SimuTime > m_lastAirHit) {
                    m_lastAirHit = Game.SimuTime + 0.5f;
                    Damage(this is CUnitPlayer ? 5 : (int)(2f + 0.05f * GetHpMax()), attacker: this, showDamage: true, "drown");
                }
                // if (this is CUnitPlayer && (m_isInWater || cell.m_water > 0.002f) && SEnvironment.GetEnvironmentCurrent().m_acidWater && GVars.m_simuTimeD > (double)(m_lastAirHit + 0.5f)) {
                //     m_lastAirHit = GVars.SimuTime;
                //     Damage(m_isInWater ? 5 : 1, this, showDamage: true, string.Empty);
                // }
                m_crushedDelay = cell.IsContentBlock() ? (m_crushedDelay - Game.SimuDeltaTime) : 1f;
                if (m_crushedDelay <= 0f) {
                    m_crushedDelay = 0.5f;
                    Damage(5f, attacker: this, showDamage: true, "crushed");
                }
                if (!m_uDesc.m_immuneToFire) {
                    bool isInCellLava = cell.IsPassable() && cell.IsLava() && cell.m_water > 0.001f;
                    if ((isInCellLava && cell.m_water > 0.2f) || cell.HasFlag(CCell.Flag_IsBurning)) {
                        m_burnStartTime = GVars.SimuTime + 5f;
                    }
                    if (cell.IsPassable() && !cell.IsLava() && cell.m_water > 0.01f) {
                        m_burnStartTime = float.MinValue;
                    }
                    if ((isInCellLava || GVars.m_simuTimeD < m_burnStartTime) && GVars.m_simuTimeD > m_burnLastTimeHit) {
                        m_burnLastTimeHit = GVars.SimuTime + 0.7f;
                        float burnDamage = (this is CUnitPlayer)
                            ? ((cell.m_water > 0.2f) ? 25f : (isInCellLava ? 10f : 2f))
                            : ((cell.m_water > 0.2f) ? 50f : (isInCellLava ? 30f : 10f));
                        Damage(burnDamage, attacker: this, showDamage: true, "burnt");
                    }
                }
            }
        }
        // else {
        //     m_forces = Vector2.zero;
        //     m_forcesPush = Vector2.zero;
        // }
        m_pos.x = Math.Clamp(m_pos.x, 13f + 0.5f * m_uDesc.m_size.x, World.Gs.x - 13f - 0.5f * m_uDesc.m_size.x);
        m_pos.y = Math.Clamp(m_pos.y, 13f, World.Gs.y - 13f);
        m_speedLastFrame = m_speed;
        return true;
    }

    public bool IsAlive() => m_hp > 0f;
    public virtual Vector2 PosFire => new(m_pos.x + (m_lookingDirection.x <= 0f ? -1f : 1f) * 0.8f * m_uDesc.m_size.x, m_pos.y + 0.5f * m_uDesc.m_size.y);

    public CDesc UDesc => m_uDesc;

    public virtual Rect RectCol => new(m_pos.x - 0.5f * m_uDesc.m_size.x, m_pos.y, m_uDesc.m_size.x, m_uDesc.m_size.y);

    public virtual float GetHpMax() => m_uDesc.GetHpMax();

    public virtual bool CanMove() => true;

    public Vector2 PosCenter => new(m_pos.x, m_pos.y + 0.5f * m_uDesc.m_size.y);
    public int2 PosCell => (int2)m_pos;

    public virtual float GetArmor() => m_uDesc.m_armor;
}

public class CUnitPlayer : CUnit {
    public bool m_isFacingRight = true;
    private bool m_onGroundLastFrame;
    private bool m_isInWaterLastFrame;
    private CItem m_lastItemHold = null!;
    private float m_lastItemHoldTime;
    private float m_lastTimeMadeSound = float.MinValue;
    private float m_lastTimeMadeBigSound = float.MinValue;
    private float m_lastRespawnTime = float.MinValue;

    public new class CDesc : CUnit.CDesc {
        public CDesc(float speed, Vector2 size, int hpMax) : base(tier: -1, speed, size, hpMax, armor: 0) {
            m_regenSpeed = 0.005f;
            m_isSpawnPriority = true;
        }

        public override float GetHpMax() {
            return m_hpMax * GameState.m_playerHpMult[(int)MathF.Max(0, GParams.m_difficulty)];
        }
        public override CUnit CreateUnit(Vector2 pos) => new CUnitPlayer(this, pos);
    }

    // used by reflection
    protected CUnitPlayer(CUnitPlayer.CDesc uDesc, Vector2 pos) : base(uDesc, pos) { }

    public override Vector2 PosFire => m_pos + Vector2.up * 0.5f * m_uDesc.m_size.y + Vector2.FromPolar(m_lookingAngleRad, 0.45f);

    public new CDesc UDesc => (CDesc)m_uDesc;

    public CPlayer? GetPlayer() {
        return PlayerManager.GetPlayerByUnit(this);
    }
    private CPlayer GetPlayerOrThrow() {
        return GetPlayer() ?? throw new InvalidOperationException($"For unit {this}, player doesn't exists");
    }

    public override float GetArmor() {
        CPlayer player = GetPlayerOrThrow();
        CItem_Device? bestActiveOfGroup = player.m_inventory.GetBestActiveOfGroup(CItem_Device.Group.Armor);
        return m_uDesc.m_armor + (bestActiveOfGroup?.m_customValue ?? 0f);
    }

    public bool IsInCharacterScreen() {
        return GetPlayerOrThrow().m_skinHairStyle <= -1;
    }

    public bool IsRespawnInvincible() {
        return Game.SimuTime < m_lastRespawnTime + 3f;
    }

    public bool IsInvisible() {
        CPlayer player = GetPlayerOrThrow();
        return GItems.potionInvisibility.IsDurationItemActive(player) || GItems.rocketTop.GetVars(player).IsInRocketTop;
    }

    public bool IsStealth() {
        CPlayer player = GetPlayerOrThrow();
        CItem_Device? bestActiveOfGroup = player.m_inventory.GetBestActiveOfGroup(CItem_Device.Group.Invisibility, true);
        return bestActiveOfGroup?.IsDurationItemActive(player) ?? false;
    }

    public float GetLastTimeMadeSound(bool v) {
        return float.MaxValue;
    }
}

public sealed class CUnitPlayerLocal : CUnitPlayer {
    public new class CDesc : CUnitPlayer.CDesc {
        public CDesc(float speed, Vector2 size, int hpMax)
            : base(speed, size, hpMax) {}
        public override CUnit CreateUnit(Vector2 pos) => new CUnitPlayerLocal(this, pos);
    }
    private CUnitPlayerLocal(CUnitPlayerLocal.CDesc uDesc, Vector2 pos)
        : base(uDesc, pos) { }
}

public class CUnitMonster : CUnit {
    public bool m_isNightSpawn;
    public CUnit? m_target;
    public CUnit? m_targetLastFrame;
    public float m_targetChangeTime = float.MinValue;
    protected float m_lastTimeAttacked = float.MinValue;
    protected int m_attacksNbLeft;
    protected float m_lastJumpTime;
    protected float m_lastTimeNearPlayers = -1f;
    protected bool m_needAir;
    public bool m_isCreativeSpawn;

    public new class CDesc : CUnit.CDesc {
        public CAttackDesc m_attackDesc;
        public CStack[] m_loot;
        public float m_pfPeriod = 0.3f;
        public float m_deathDuration = 20f;
        public bool m_goAwayIfNoPf;

        public CDesc(int tier, float speed, Vector2 size, int hpMax, int armor, CAttackDesc attackDesc, CStack[] loot)
            : base(tier, speed, size, hpMax, armor) {
            m_attackDesc = attackDesc;
            m_loot = loot;
            m_regenSpeed = 0.02f;
        }

        public override float GetHpMax() {
            return GetHpMax((int)NetworkClients.ConnectedClientsCount());
        }

        public float GetHpMax(int nbPlayers) {
            return m_hpMax
                * GameState.m_monstersHpMult[Math.Max(0, GParams.m_difficulty)]
                * GParams.m_monstersHpMult
                * (1f + (nbPlayers - 1) * GParams.m_monstersHpAddPerPlayer);
        }

        public override float GetSpeedMax() {
            // return m_speedMax * SEnvironment.GetEnvironmentCurrent().m_monsterSpeedMult;
            return m_speedMax;
        }
        public override CUnit CreateUnit(Vector2 pos) => new CUnitMonster(this, pos);
    }
    // used by reflection
    protected CUnitMonster(CUnitMonster.CDesc uDesc, Vector2 pos) : base(uDesc, pos) {
        m_lastTimeNearPlayers = GVars.SimuTime;
    }
    public new CDesc UDesc => (CDesc)m_uDesc;

    public virtual CUnit? Target {
        get => m_target;
        set => m_target = value;
    }

    public bool IsLastPathfindSuccessful() {
        return false;
    }
    public bool IsPathfindingProgressing() {
        return false;
    }
    public bool IsNetworkControlled() {
        return m_targetLastFrame is CUnitPlayer && m_targetLastFrame.IsAlive();
    }
    protected override void OnDeath(CUnit? attacker, string damageCause = "") {
         if (attacker is CUnitPlayer || attacker is CUnitDefense || attacker is CUnitDrone) {
            UnitManager.OnMonsterDeath(this);
            // if (attacker is CUnitDrone) {
            //     SSingleton<SShipAI>.Inst.OnMonsterKilledByDrone();
            // }
            float difficultyFactor = GameState.m_nightDropFrequencyByDifficulty[Math.Max(0, GParams.m_difficulty)];
            int bloodTier = Random.Float() < 0.6f ? (UDesc.m_tier - 1) : Random.IntBetween(0, UDesc.m_tier - 1);
            float dropChanceMult = GParams.m_dropChanceMult/* * SEnvironment.GetEnvironmentCurrent().m_monsterDropMult*/;
            if (m_isNightSpawn) {
                if (Utils.GetRandomCorrected(difficultyFactor * dropChanceMult, m_uDesc.m_id, attacker)) {
                    PickupManager.CreatePickup(GameState.m_nightDropItems[bloodTier], 1, PosCenter);
                }
            } else {
                foreach (CStack stack in UDesc.m_loot) {
                    if (Utils.GetRandomCorrected(stack.m_probability * dropChanceMult, m_uDesc.m_id, attacker)) {
                        PickupManager.CreatePickup(stack.m_item, stack.m_nb, PosCenter);
                    }
                }
                if (GParams.m_difficulty == -1 && Utils.GetRandomCorrected(difficultyFactor * dropChanceMult, m_uDesc.m_id, attacker)) {
                    PickupManager.CreatePickup(GameState.m_nightDropItems[bloodTier], (int)Random.FloatBetween(2, 4), PosCenter);
                }
            }
        }
        base.OnDeath(attacker);
    }
    protected override void OnHit(CUnit? attacker) {
        if (attacker is CUnitPlayer || attacker is CUnitDefense) {
            m_lastTimeAttacked = GVars.SimuTime;
            if (attacker != m_targetLastFrame && (m_targetLastFrame is not CUnitPlayer || (GVars.SimuTime > m_targetChangeTime + 10f && Random.Float() < 0.5f) || (GVars.SimuTime > m_targetChangeTime + 5f && Random.Float() < 0.3f) || Random.Float() < 0.05f)) {
                SetTargetAndPropagate(attacker);
            }
        }
        if (attacker is CUnitDefense && Random.Float() < 0.1f && !IsNetworkControlled()) {
            m_target = attacker;
        }
        // SOutgame.Mode.OnMonsterHit(this, attacker);
        base.OnHit(attacker);
    }

    private void SetTargetAndPropagate(CUnit? target) {
        bool isNewTarget = m_target is null && target is not null;
        m_target = target;
        if (!isNewTarget) {
            return;
        }
        foreach (CUnit unit in UnitManager.units) {
            if (unit is not CUnitMonster unitMonster
                || unitMonster == this
                || !unitMonster.IsAlive()
                || (unitMonster.PosCenter - PosCenter).SqrMagnitude() >= 16f) {
                continue;
            }
            unitMonster.SetTargetAndPropagate(target);
        }
    }

    public void SetTargetFromNetwork(CUnit? target) {
        if (target == m_targetLastFrame) {
            return;
        }
        m_targetLastFrame = target;
        m_targetChangeTime = GVars.SimuTime;
        m_target = target;
    }

    public override bool Update() {
        if (m_hp == 0f && (GVars.m_simuTimeD > (double)(m_deathTime + UDesc.m_deathDuration))) {
            return false;
        }
        bool keepAlive = this is CUnitFish && !m_isInWater && IsAlive();
        if (!keepAlive) {
            foreach (var player in PlayerManager.players) {
                if (player.IsInRectAroundScreen(GameState.m_distMonstersUnspawn, m_pos)) {
                    keepAlive = true;
                    break;
                }
            }
        }
        int2 @int = (RectCol.x == 0f) ? PosCell : (int2)RectCol.center;
        // m_isWalkingOnWalls = this is CUnitWall && IsAlive() && (World.Grid[@int.x, @int.y].HasBgSurfaceOrBackwall() || SWorld.Grid[@int.x - 1, @int.y].IsContentBlock() || SWorld.Grid[@int.x + 1, @int.y].IsContentBlock() || SWorld.Grid[@int.x, @int.y + 1].IsContentBlock() || SWorld.Grid[@int.x - 1, @int.y - 1].IsContentBlock() || SWorld.Grid[@int.x + 1, @int.y + 1].IsContentBlock() || SWorld.Grid[@int.x + 1, @int.y - 1].IsContentBlock() || SWorld.Grid[@int.x - 1, @int.y + 1].IsContentBlock());
        // if (!World.Grid[@int.x, @int.y].HasBgSurfaceOrBackwall() && World.Grid[@int.x, @int.y - 1].IsContentBlock() && Mathf.Abs(this.m_pfDir.x) > Mathf.Abs(this.m_pfDir.y) * 1.5f) {
        //     m_isWalkingOnWalls = false;
        // }
        if (!IsNetworkControlled()) {
            if (keepAlive) {
                m_lastTimeNearPlayers = GVars.SimuTime;
            } else if (GVars.SimuTime > m_lastTimeNearPlayers + 20f && this is not CUnitBoss) {
                return false;
            }
            if (IsAlive()) {
                // if (CanMove()) {
                //     UpdatePathfinding();
                //     if (GVars.m_simuTimeD < (double)(this.m_lastFireTime + 0.15f) || (this.m_target != null && (this.m_pos - this.m_target.PosCenter).magnitude < 0.3f)) {
                //         this.m_pfDir = Vector2.zero;
                //     }
                //     if (this.m_pfDir != Vector2.zero) {
                //         this.m_lookingDirection = Vector2.MoveTowards(this.m_lookingDirection, this.m_pfDir.normalized, 5f * SMain.SimuDeltaTime);
                //     }
                //     this.UpdateMoveForces();
                //     if (SEnvironment.GetEnvironmentCurrent().m_monsterSpeedMult > 1f) {
                //         this.m_forces.x = this.m_forces.x * SEnvironment.GetEnvironmentCurrent().m_monsterSpeedMult;
                //     }
                //     float num = Mathf.Min(this.RectCol.width, this.RectCol.height);
                //     for (int j = 0; j < SUnits.Units.Count; j++) {
                //         CUnit cunit = SUnits.Units[j];
                //         if (cunit != null && cunit is CUnitMonster && cunit.IsAlive() && cunit != this && (cunit.PosCenter - base.PosCenter).sqrMagnitude < num * num) {
                //             cunit.Push((cunit.PosCenter - base.PosCenter).normalized * (1f - (cunit.PosCenter - base.PosCenter).magnitude / num));
                //         }
                //     }
                // }
                UpdateTarget();
            }
        }
        // if (IsAlive() && CanMove()) {
        //     this.AttackIFP(keepAlive);
        // }
        if (m_target != m_targetLastFrame) {
            if (!IsNetworkControlled()) {
                MessageProcessing.SendToAll(new MessageMonsterChangeTarget(this, m_target));
                m_targetLastFrame = m_target;
                m_targetChangeTime = GVars.SimuTime;
            } else {
                m_target = m_targetLastFrame;
            }
        }
        if (IsNetworkControlled()) {
            CPlayer? player = m_targetLastFrame is CUnitPlayer unitPlayer ? PlayerManager.GetPlayerByUnit(unitPlayer) : null;
            if (player is null || !player.HasUnitPlayerAlive()) {
                Logging.Warning($"(CUnitMonster) Monster {this} targetting a disconnected player {player?.ToString() ?? "[NULL]"}, untargeting them");
                SetTargetFromNetwork(null);
            } else if (player.HasUnitPlayer() && player.IsAFK()) {
                Logging.Warning($"(CUnitMonster) Monster {this} targetting an AFK player {player}, untargeting them");
                SetTargetFromNetwork(null);
            }
        }
        return base.Update();
    }

    protected virtual void UpdateTarget() {
        // float monsterAggroDistMult = SEnvironment.GetEnvironmentCurrent().m_monsterAggroDistMult;
        const float monsterAggroDistMult = 1f;
        if (m_target == null && GParams.m_difficulty >= 0) {
            foreach (CPlayer player in PlayerManager.players) {
                if (player.m_unitPlayer is null) {
                    continue;
                }
                bool isPotionPheromonesActive = GItems.potionPheromones.IsDurationItemActive(player);
                CUnitPlayer unitPlayer = player.m_unitPlayer;
                if (!unitPlayer.IsAlive()
                    || unitPlayer.GetArmor() >= UDesc.m_attackDesc.GetDamage(this) * Math.Max(1, UDesc.m_attackDesc.m_nbAttacks) && !isPotionPheromonesActive
                    || unitPlayer.IsInCharacterScreen()
                    || unitPlayer.IsRespawnInvincible()
                    || unitPlayer.IsInvisible()) {
                    continue;
                }
                float stealthFactor = unitPlayer.IsStealth() ? 0.5f : 1f;
                float sqrMagnitude = ((Vector2)unitPlayer.PosCell - PosCenter).SqrMagnitude();
                if (sqrMagnitude < Utils.Sqr(2f * monsterAggroDistMult * stealthFactor)
                    || (sqrMagnitude < Utils.Sqr(10f * monsterAggroDistMult * stealthFactor) && Game.SimuTime - unitPlayer.GetLastTimeMadeSound(false) < 0.5)
                    || (sqrMagnitude < Utils.Sqr(8f * monsterAggroDistMult * stealthFactor) && m_lookingDirection.x * (unitPlayer.PosCenter.x - PosCenter.x) > 0f && Utils.CheckCol_SegmentGround(PosCenter, unitPlayer.PosCenter, true, false) == Vector2.zero)
                    || isPotionPheromonesActive) {
                    SetTargetAndPropagate(unitPlayer);
                }
            }
        }
        if (m_target is null && (UnitManager.IsThereABossAggressive() || m_isNightSpawn)) {
            m_target = UnitManager.GetClosestPlayer(m_pos);
        }
        if (m_target is not null && (
            !m_target.IsAlive()
            || (!m_isNightSpawn
                && GVars.m_simuTimeD > (double)(m_lastTimeAttacked + 5f)
                && (m_target.m_pos - m_pos).magnitude > 18f * monsterAggroDistMult * ((!(m_target as CUnitPlayer)?.IsStealth() ?? true) ? 1f : 0.5f)))) {
            m_target = null;
        }
    }
}

public sealed class CUnitDrone : CUnit {
    // private int m_attacksNbLeft;

    public new class CDesc : CUnit.CDesc {
        public CItem_Device m_itemDrone;
        public CAttackDesc? m_attackDesc;

        public CDesc(CItem_Device itemDrone, float speed, Vector2 size, int hpMax, int armor, CAttackDesc? attackDesc)
            : base(-1, speed, size, hpMax, armor) {
            m_itemDrone = itemDrone;
            m_attackDesc = attackDesc;
            m_skipColsOnPlatforms = true;
        }
        public override CUnit CreateUnit(Vector2 pos) => new CUnitDrone(this, pos);
    }

    private CUnitDrone(CUnit.CDesc uDesc, Vector2 pos)
        : base(uDesc, pos) {
        foreach (CUnit unit in UnitManager.units) {
            if (unit is not CUnitDrone cunitDrone || !cunitDrone.IsAlive()) {
                continue;
            }
            if (cunitDrone.UDesc.m_itemDrone.m_customValue > UDesc.m_itemDrone.m_customValue) {
                Damage(m_hp * 2f, this);
                GVars.m_droneLastTimeDontEnter = GVars.SimuTime;
                // this.ComputeNewObjectiveCell();
                // cunitDrone.TeleportToObjective();
            } else {
                cunitDrone.Damage(cunitDrone.m_hp * 2f, this, false, string.Empty);
                GVars.m_droneLastTimeEnters = GVars.SimuTime;
            }
        }
        if (GVars.m_postGame) {
            Damage(m_hp);
        }
    }
    public new CDesc UDesc => (CDesc)m_uDesc;
}

public sealed class CUnitDefense : CUnit {
    public CItem_Defense? m_item;

    private int m_attacksNbLeft;

    private float m_angleDeg;

    private float m_timeRepaired;

    private Vector2 m_defenseLastTargetPos = Vector2.zero;

    private float m_defenseLastTimeSearchTarget = float.MinValue;

    public new class CDesc : CUnit.CDesc {
        public CDesc(int tier, float speed, Vector2 size, int hpMax, int armor)
            : base(tier, speed, size, hpMax, armor) {}

        public override CUnit CreateUnit(Vector2 pos) => new CUnitDefense(this, pos);
    }
    private CUnitDefense(CUnit.CDesc uDesc, Vector2 pos)
        : base(uDesc, pos) {
        m_hp = (float)World.Grid[(int)pos.x, (int)pos.y].m_contentHP;
    }

    public float GetAngleRad() => m_angleDeg * 0.017453292f;

    public override Vector2 PosFire => m_pos + Vector2.up * 0.5f + Vector2.FromPolar(GetAngleRad(), 0.4f);

    public override float GetHpMax() {
        return m_item is null ? m_hp : m_item.m_hpMax;
    }
    public override bool CanMove() => false;

    // public float GetAngleMin() {
    //     return (!World.Grid[PosCell].HasFlag(CCell.Flag_IsXReversed)) ? m_item.m_angleMinDeg : Math.Min(180f - m_item.m_angleMinDeg, 180f - m_item.m_angleMaxDeg);
    // }
    // 
    // public float GetAngleMax() {
    //     return (!World.Grid[PosCell].HasFlag(CCell.Flag_IsXReversed)) ? m_item.m_angleMaxDeg : Math.Max(180f - m_item.m_angleMinDeg, 180f - m_item.m_angleMaxDeg);
    // }

    protected override void OnDeath(CUnit? attacker, string damageCause = "") {
        World.DestroyCell(PosCell, 0, false, null);
        PickupManager.CreatePickup(GItems.metalScrap, 2, PosCenter);
        base.OnDeath(attacker, string.Empty);
    }
}

public class CUnitGround : CUnitMonster {
    public new class CDesc : CUnitMonster.CDesc {
        public CDesc(int tier, float speed, Vector2 size, int hpMax, int armor, CAttackDesc attackDesc, CStack[] loot)
            : base(tier, speed, size, hpMax, armor, attackDesc, loot) {
            m_canDive = true;
        }
        public override CUnit CreateUnit(Vector2 pos) => new CUnitGround(this, pos);
    }

    protected CUnitGround(CDesc uDesc, Vector2 pos)
        : base(uDesc, pos) {}
}

public class CUnitBird : CUnitMonster {
    private float m_flyDuration = (float)Random.IntBetween(0, 10);

    private bool m_justExploded;

    public new class CDesc : CUnitMonster.CDesc {
        public CDesc(int tier, float speed, Vector2 size, int hpMax, int armor, CAttackDesc attackDesc, CStack[] loot)
            : base(tier, speed, size, hpMax, armor, attackDesc, loot) {}

        public override CUnit CreateUnit(Vector2 pos) => new CUnitBird(this, pos);
    }

    protected CUnitBird(CUnitBird.CDesc uDesc, Vector2 pos)
        : base(uDesc, pos) {}
}

public class CUnitFish : CUnitMonster {
    public new class CDesc : CUnitMonster.CDesc {
        public CDesc(int tier, float speed, Vector2 size, int hpMax, int armor, CAttackDesc attackDesc, CStack[] loot)
            : base(tier, speed, size, hpMax, armor, attackDesc, loot) {
            m_goAwayIfNoPf = true;
            m_canDive = true;
        }
        public override CUnit CreateUnit(Vector2 pos) => new CUnitFish(this, pos);
    }

    protected CUnitFish(CUnitFish.CDesc uDesc, Vector2 pos)
        : base(uDesc, pos) {}

    public override bool Update() {
        return base.Update();
    }
}

public class CUnitBoss : CUnitMonster {
    protected int2 m_lastPFBlockingCell = int2.negative;

    protected CUnitDefense? m_lastDefenseAttacker;

    protected float m_lastTimeHit = float.MinValue;

    protected float m_isGoingBackHomeTime = -1f;

    protected float m_lastTimeMoved = float.MinValue;

    protected float m_durationOutOfZone;

    protected CUnitPlayer? m_targetSeen;

    public new class CDesc : CUnitMonster.CDesc {
        public Rect m_zoneHome;

        public Rect m_zoneDetect;

        public Rect m_zonePursuit;

        private Rect m_zoneHomeBackup;

        private Rect m_zoneDetectBackup;

        private Rect m_zonePursuitBackup;

        private int2 m_zoneCenterManual;

        public CDesc(int tier, float speed, Vector2 size, int hpMax, int armor, CAttackDesc attackDesc, CStack[] loot, Rect zoneHome, Rect zoneDetect, Rect zonePursuit)
            : base(tier, speed, size, hpMax, armor, attackDesc, loot) {
            this.m_zoneHomeBackup = zoneHome;
            this.m_zoneHome = zoneHome;
            this.m_zoneDetectBackup = zoneDetect;
            this.m_zoneDetect = zoneDetect;
            this.m_zonePursuitBackup = zonePursuit;
            this.m_zonePursuit = zonePursuit;
            this.m_deathDuration = 120f;
            this.m_regenSpeed = 0.003f;
            this.m_skipColsOnPlatforms = true;
            this.m_isSpawnPriority = true;
        }

        public override float GetSpeedMax() {
            return this.m_speedMax;
        }

        public void ResetZones() {
            this.m_zoneHome = this.m_zoneHomeBackup;
            this.m_zoneDetect = this.m_zoneDetectBackup;
            this.m_zonePursuit = this.m_zonePursuitBackup;
        }

        public override CUnit CreateUnit(Vector2 pos) => new CUnitBoss(this, pos);
    }
    protected CUnitBoss(CUnitBoss.CDesc uDesc, Vector2 pos)
        : base(uDesc, pos) {}
}

public sealed class CUnitBossCrab : CUnitBoss {
    public const int m_screamNbMonsters = 15;

    public const float m_screamCooldown = 40f;

    public float m_screamLastTime = float.MinValue;

    public float m_lastHitWallTime = float.MinValue;

    public new class CDesc : CUnitBoss.CDesc {
        public CDesc(int tier, float speed, Vector2 size, int hpMax, int armor, CAttackDesc attackDesc, CStack[] loot, Rect zoneHome, Rect zoneDetect, Rect zonePursuit)
            : base(tier, speed, size, hpMax, armor, attackDesc, loot, zoneHome, zoneDetect, zonePursuit) {
        }
        public override CUnit CreateUnit(Vector2 pos) => new CUnitBossCrab(this, pos);
    }

    private CUnitBossCrab(CUnitBossCrab.CDesc uDesc, Vector2 pos)
        : base(uDesc, pos) {}
}

public sealed class CUnitWall : CUnitMonster {
    public new class CDesc : CUnitMonster.CDesc {
        public CDesc(int tier, float speed, Vector2 size, int hpMax, int armor, CAttackDesc attackDesc, CStack[] loot)
            : base(tier, speed, size, hpMax, armor, attackDesc, loot) {
            m_pfPeriod = 0.5f;
            m_canDive = true;
        }
        public override CUnit CreateUnit(Vector2 pos) => new CUnitWall(this, pos);
    }

    private CUnitWall(CUnitWall.CDesc uDesc, Vector2 pos)
        : base(uDesc, pos) {}
}

public sealed class CUnitBossBird : CUnitBoss {
    public const int m_bigFireNb = 20;

    public const float m_bigFireCooldown = 10f;

    public float m_lastBigFireTime = float.MinValue;

    public int m_bigFireNbLeft;

    public new class CDesc : CUnitBoss.CDesc {
        public CDesc(int tier, float speed, Vector2 size, int hpMax, int armor, CAttackDesc attackDesc, CStack[] loot, Rect zoneHome, Rect zoneDetect, Rect zonePursuit)
            : base(tier, speed, size, hpMax, armor, attackDesc, loot, zoneHome, zoneDetect, zonePursuit) {}

        public override CUnit CreateUnit(Vector2 pos) => new CUnitBossBird(this, pos);
    }

    private CUnitBossBird(CUnitBossBird.CDesc uDesc, Vector2 pos)
        : base(uDesc, pos) {}
}

public sealed class CUnitBossDweller : CUnitBoss {
    private int nbBurst;

    private float m_lastBurstTime;

    public new class CDesc : CUnitBoss.CDesc {
        public CDesc(int tier, float speed, Vector2 size, int hpMax, int armor, CAttackDesc attackDesc, CStack[] loot, Rect zoneHome, Rect zoneDetect, Rect zonePursuit)
            : base(tier, speed, size, hpMax, armor, attackDesc, loot, zoneHome, zoneDetect, zonePursuit) {
        }
        public override CUnit CreateUnit(Vector2 pos) => new CUnitBossDweller(this, pos);
    }

    private CUnitBossDweller(CUnitBossDweller.CDesc uDesc, Vector2 pos)
        : base(uDesc, pos) {}
}

public sealed class CUnitBossBalrog : CUnitBoss {
    private float m_lastScreamTime;

    private float m_lavaAttackTime;

    private readonly CAttackDesc m_lavaAttack = new(80f, 50, 1, 10f, 0f, 20f, GBullets.grenadeLava);

    public new class CDesc : CUnitBoss.CDesc {
        public CDesc(int tier, float speed, Vector2 size, int hpMax, int armor, CAttackDesc attackDesc, CStack[] loot, Rect zoneHome, Rect zoneDetect, Rect zonePursuit)
            : base(tier, speed, size, hpMax, armor, attackDesc, loot, zoneHome, zoneDetect, zonePursuit) {}

        public override CUnit CreateUnit(Vector2 pos) => new CUnitBossBalrog(this, pos);
    }

    private CUnitBossBalrog(CUnitBossBalrog.CDesc uDesc, Vector2 pos)
        : base(uDesc, pos) {}
}

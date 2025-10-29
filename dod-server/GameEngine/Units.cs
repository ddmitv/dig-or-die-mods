
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameEngine;

public static class GUnits {
    public static readonly CUnitPlayer.CDesc player = new(speed: 5f, size: new(0.6f, 0.8f), hpMax: 50) {
        m_codeName = "player",
        m_id = 1
    };

    public static readonly CUnitPlayerLocal.CDesc playerLocal = new(5f, new Vector2(0.6f, 0.8f), 50) {
        m_codeName = "playerLocal",
        m_id = 2
    };

    public static readonly CUnitDefense.CDesc defense = new(-1, 5f, Vector2.zero, 10, 0) {
        m_codeName = "defense",
        m_id = 3
    };

    public static readonly CUnitDrone.CDesc drone = new(GItems.drone, 8f, new Vector2(0.25f, 0.25f), 200, 2, null) {
        m_codeName = "drone",
        m_id = 4
    };

    public static readonly CUnitDrone.CDesc droneCombat = new(GItems.droneCombat, 8f, new Vector2(0.4f, 0.4f), 300, 4, new CAttackDesc(10f, 30, 1, 1f, 0f, 0f, GBullets.laserDrone)) {
        m_codeName = "droneCombat",
        m_id = 5
    };

    public static readonly CUnitDrone.CDesc droneWar = new(GItems.droneWar, 8f, new Vector2(0.55f, 0.55f), 400, 6, new CAttackDesc(10f, 40, 1, 0.5f, 0f, 0f, GBullets.laserDrone)) {
        m_codeName = "droneWar",
        m_id = 6
    };

    public static readonly CUnitGround.CDesc hound = new(1, 4f, new Vector2(0.8f, 0.5f), 10, 0, new CAttackDesc(0f, 4, 1, 1f, 0f, 10f, null), [new CStack(GItems.dogHorn, probability: 0.5f)]) {
        m_codeName = "hound",
        m_id = 7
    };

    public static readonly CUnitBird.CDesc firefly = new(1, 2f, new Vector2(0.5f, 0.8f), 10, 0, new CAttackDesc(6f, 6, 1, 1f, 1f, 5f, GBullets.firefly), [new CStack(GItems.lightGem, probability: 0.3f)]) {
        m_emitLight = Color24.FromNumber(4185599U),
        m_codeName = "firefly",
        m_id = 8
    };

    public static readonly CUnitBird.CDesc fireflyRed = new(2, 2f, new Vector2(0.5f, 0.8f), 50, 0, new CAttackDesc(7f, 6, 3, 1f, 1f, 5f, GBullets.firefly), [new CStack(GItems.energyGem, probability: 0.3f)]) {
        m_emitLight = Color24.FromNumber(16438734U),
        m_codeName = "fireflyRed",
        m_id = 9
    };

    public static readonly CUnitGround.CDesc dweller = new(2, 2f, new Vector2(0.8f, 0.5f), 35, 2, new CAttackDesc(6f, 6, 1, 1f, 0f, 10f, GBullets.dweller), [new CStack(GItems.moleShell, probability: 0.3f)]) {
        m_codeName = "dweller",
        m_id = 10
    };

    public static readonly CUnitFish.CDesc fish = new(2, 1.5f, new Vector2(0.8f, 0.7f), 35, 2, new CAttackDesc(0f, 8, 1, 1f, 2f, 10f, null), [new CStack(GItems.fish2Regen, probability: 0.3f)]) {
        m_regenSpeed = 0.3f,
        m_codeName = "fish",
        m_id = 11
    };

    public static readonly CUnitBird.CDesc bat = new(2, 7f, new Vector2(0.5f, 0.8f), 50, 0, new CAttackDesc(0f, 10, 1, 0.7f, 2f, 5f, null), [new CStack(GItems.bat2Sonar, probability: 0.3f)]) {
        m_canDive = true,
        m_codeName = "bat",
        m_id = 12
    };

    public static readonly CUnitGround.CDesc houndBlack = new(3, 4f, new Vector2(0.8f, 0.5f), 85, 0, new CAttackDesc(0f, 10, 1, 1f, 0f, 10f, null), [new CStack(GItems.dogHorn3, probability: 0.5f)]) {
        m_codeName = "houndBlack",
        m_id = 13
    };

    public static readonly CUnitBird.CDesc fireflyBlack = new(3, 2f, new Vector2(0.6f, 0.9f), 100, 0, new CAttackDesc(8f, 6, 5, 1f, 1f, 5f, GBullets.firefly), [new CStack(GItems.darkGem, probability: 0.3f)]) {
        m_emitLight = Color24.FromNumber(10658466U),
        m_codeName = "fireflyBlack",
        m_id = 14
    };

    public static readonly CUnitGround.CDesc dwellerBlack = new(3, 2.5f, new Vector2(0.8f, 0.5f), 70, 3, new CAttackDesc(7f, 6, 3, 1.2f, 0f, 10f, GBullets.dweller), [new CStack(GItems.moleShellBlack, probability: 0.3f)]) {
        m_codeName = "dwellerBlack",
        m_id = 15
    };

    public static readonly CUnitFish.CDesc fishBlack = new(3, 1.5f, new Vector2(0.8f, 0.7f), 60, 3, new CAttackDesc(0f, 15, 1, 1f, 2f, 10f, null), [new CStack(GItems.fish3Regen, probability: 0.3f)]) {
        m_regenSpeed = 0.3f,
        m_codeName = "fishBlack",
        m_id = 16
    };

    public static readonly CUnitBird.CDesc batBlack = new(3, 7f, new Vector2(0.5f, 0.8f), 100, 0, new CAttackDesc(0f, 10, 1, 0.7f, 2f, 5f, null), [new CStack(GItems.bat3Sonar, probability: 0.3f)]) {
        m_canDive = true,
        m_codeName = "batBlack",
        m_id = 17
    };

    public static readonly CUnitBossCrab.CDesc bossMadCrab = new(3, 2f, new Vector2(3.8f, 4.8f), 3000, 6, new CAttackDesc(2f, 30, 1, 1f, 0f, 20f, null),
        [new CStack(GItems.bossMadCrabSonar, probability: 1), new CStack(GItems.bossMadCrabMaterial, probability: 1)],
        new Rect(21f, 415f, 22f, 15f), new Rect(5f, 410f, 71f, 25f), new Rect(0f, 400f, 116f, 45f)) {
        m_codeName = "bossMadCrab",
        m_id = 18
    };

    public static readonly CUnitFish.CDesc shark = new(4, 4.5f, new Vector2(0.8f, 0.5f), 120, 2, new CAttackDesc(0f, 15, 1, 0.6f, 2f, 5f, null), [new CStack(GItems.sharkSkin, probability: 0.3f)]) {
        m_codeName = "shark",
        m_id = 19
    };

    public static readonly CUnitBird.CDesc fireflyExplosive = new(4, 4f, new Vector2(0.7f, 0.8f), 120, 0, new CAttackDesc(1f, 50, 0, 99f, 0f, 0f, null), [new CStack(GItems.unstableGemResidue, probability: 0.3f)]) {
        m_emitLight = Color24.FromNumber(4185599U),
        m_codeName = "fireflyExplosive",
        m_id = 20
    };

    public static readonly CUnitWall.CDesc antClose = new(4, 4f, new Vector2(0.7f, 0.7f), 200, 4, new CAttackDesc(0f, 20, 1, 0.8f, 2f, 10f, null), [new CStack(GItems.antShell, probability: 0.3f)]) {
        m_codeName = "antClose",
        m_id = 21
    };

    public static readonly CUnitWall.CDesc antDist = new(4, 3.5f, new Vector2(0.7f, 0.7f), 200, 2, new CAttackDesc(3f, 20, 1, 0.5f, 2f, 5f, GBullets.dweller), []) {
        m_codeName = "antDist",
        m_id = 22
    };

    public static readonly CUnitBossBird.CDesc bossFirefly = new(4, 2f, new Vector2(3.8f, 3.8f), 5000, 0, new CAttackDesc(27f, 12, 3, 1.5f, 0f, 5f, GBullets.firefly),
        [new CStack(GItems.masterGem, probability: 1)],
        new Rect(350f, 865f, 30f, 20f), new Rect(260f, 820f, 200f, 75f), new Rect(230f, 800f, 240f, 100f)) {
        m_emitLight = Color24.FromNumber(4185599U),
        m_codeName = "bossFirefly",
        m_id = 23
    };

    public static readonly CUnitBossDweller.CDesc bossDweller = new(4, 2f, new Vector2(5f, 3f), 4000, 10, new CAttackDesc(30f, 20, 1, 1f, 0f, 20f, GBullets.dwellerBig),
        [new CStack(GItems.lootDwellerLord, probability: 1)],
        new Rect(831f, 681f, 22f, 12f), new Rect(822f, 674f, 59f, 29f), new Rect(814f, 666f, 71f, 51f)) {
        m_immuneToFire = true,
        m_codeName = "bossDweller",
        m_id = 24
    };

    public static readonly CUnitWall.CDesc lavaAnt = new(5, 4f, new Vector2(0.7f, 0.7f), 300, 0, new CAttackDesc(5f, 15, 1, 1f, 2f, 10f, GBullets.fireballSmall), [new CStack(GItems.lootLavaSpider, probability: 0.3f)]) {
        m_canDive = true,
        m_immuneToFire = true,
        m_codeName = "lavaAnt",
        m_id = 25
    };

    public static readonly CUnitBird.CDesc lavaBat = new(5, 5f, new Vector2(0.5f, 0.8f), 400, 0, new CAttackDesc(5f, 15, 1, 0.7f, 2f, 10f, GBullets.fireballBig), [new CStack(GItems.lootLavaBat, probability: 0.3f)]) {
        m_canDive = true,
        m_immuneToFire = true,
        m_codeName = "lavaBat",
        m_id = 26
    };

    public static readonly CUnitGround.CDesc particleGround = new(5, 2.5f, new Vector2(0.8f, 0.5f), 400, 5, new CAttackDesc(5f, 10, 1, 0.5f, 0f, 10f, GBullets.particleSmall), [new CStack(GItems.lootParticleGround, probability: 0.3f)]) {
        m_codeName = "particleGround",
        m_id = 27
    };

    public static readonly CUnitBird.CDesc particleBird = new(5, 2f, new Vector2(0.6f, 0.9f), 300, 5, new CAttackDesc(8f, 20, 1, 1.5f, 1f, 10f, GBullets.particleMedium), [new CStack(GItems.lootParticleBirds, probability: 0.3f)]) {
        m_codeName = "particleBird",
        m_id = 28
    };

    public static readonly CUnitBird.CDesc particleBird2 = new(5, 2f, new Vector2(0.6f, 0.9f), 400, 7, new CAttackDesc(8f, 30, 1, 1.5f, 1f, 10f, GBullets.particleMedium), [new CStack(GItems.lootLargeParticleBirds, probability: 0.3f)]) {
        m_codeName = "particleBird2",
        m_id = 29
    };

    public static readonly CUnitGround.CDesc balrogMini = new(5, 4f, new Vector2(0.8f, 0.5f), 500, 5, new CAttackDesc(0f, 20, 1, 1f, 0f, 10f, null), [new CStack(GItems.lootMiniBalrog, probability: 0.3f)]) {
        m_emitLight = Color24.FromNumber(11141120U),
        m_codeName = "balrogMini",
        m_id = 30
    };

    public static readonly CUnitBossBalrog.CDesc bossBalrog = new(5, 8f, new Vector2(2.8f, 5.8f), 10000, 10, new CAttackDesc(40f, 15, 3, 2f, 0f, 20f, GBullets.fireballBig),
        [new CStack(GItems.lootBalrog, 1), new CStack(GItems.sapphire, 7)],
        new Rect(1000f, 16f, 23f, 21f), new Rect(972f, 13f, 52f, 40f), new Rect(960f, 11f, 61f, 68f)) {
        m_immuneToFire = true,
        m_codeName = "bossBalrog",
        m_id = 31
    };

    public static readonly CUnitBossBalrog.CDesc bossBalrog2 = new(5, 8f, new Vector2(2.8f, 5.8f), 13000, 15, new CAttackDesc(40f, 15, 5, 1.5f, 0f, 20f, GBullets.fireballBig),
        [new CStack(GItems.lootBalrog, 1), new CStack(GItems.sapphire, 14)],
        new Rect(1000f, 16f, 23f, 21f), new Rect(972f, 13f, 52f, 40f), new Rect(960f, 11f, 61f, 68f)) {
        m_immuneToFire = true,
        m_codeName = "bossBalrog2",
        m_id = 32
    };

    public static readonly CUnit.CDesc?[] udescs = [
        null,
        player, playerLocal, defense, drone, droneCombat, droneWar, hound, firefly, fireflyRed,
        dweller, fish, bat, houndBlack, fireflyBlack, dwellerBlack, fishBlack, batBlack,
        bossMadCrab, shark, fireflyExplosive, antClose, antDist, bossFirefly, bossDweller,
        lavaAnt, lavaBat, particleGround, particleBird, particleBird2, balrogMini, bossBalrog, bossBalrog2
    ];
}

public static class UnitManager {
    private const ushort minUnitInstanceId = 1000;
    private const ushort maxUnitInstanceId = 65000;

    private static float m_nightMonstersToSpawnNb = 0f;
    private static float m_lastTimeDaySpawn = float.MinValue;

    private static ushort lastUnitInstanceId = minUnitInstanceId;
    private static readonly CUnit?[] unitsById = new CUnit?[65536];

    private static readonly List<CUnit> unitsToRemove = new(capacity: 4);
    private static float lastTimeSentMessageUnitsPos = -1f;
    private const float SentMessageUnitsPosPeriod = 0.2f;

    public static readonly List<CUnit> units = [];

    public static readonly List<CSpeciesKillsInfo> speciesKilled = [];

    public class CSpeciesKillsInfo {
        public required CUnit.CDesc m_uDesc;
        public int m_nb;
        public float m_lastKillTime;
    }

    public static void Update() {
        uint totalAliveMonsters = 0;
        foreach (CUnit unit in units) {
            if (!unit.Update()) {
                unitsToRemove.Add(unit);
            }
            if (unit is CUnitMonster unitMonster && unitMonster.CanMove() && unitMonster.IsAlive()) {
                totalAliveMonsters += 1;
            }
        }
        // if (Game.FrameCount % 20 == 0) {
        //     SUnits_SpawnPool.ResetPfFails();
        // }
        bool isNight = Game.IsNight();
        bool isRocketPrep = Game.IsRocketPrep();
        bool sunEclipse = /*SEnvironment.GetEnvironmentCurrent().m_sunEclipse*/ false;
        if ((isNight || isRocketPrep || sunEclipse) && GParams.m_difficulty >= 0) {
            m_nightMonstersToSpawnNb += ((Game.GetNightDurationLeft() >= 10f || isRocketPrep || sunEclipse)
                ? (GameState.m_nightSpawnFrequency[GParams.m_difficulty] * Game.SimuDeltaTime * GParams.m_monstersNightSpawnRateMult * (1f + (float)(PlayerManager.GetNbPlayersConnected() - 1) * GParams.m_monstersNightSpawnRateAddPerPlayer))
                : 0f)
                /** SEnvironment.GetEnvironmentCurrent().m_nightSpawnMultiplier*/ * SOutgame.Mode.NightSpawnMultiplier;
            for (int i = 0; i < 10; i++) {
                if (m_nightMonstersToSpawnNb <= 1f) {
                    break;
                }
                if (SpawnMonster(nightSpawn: true)) {
                    m_nightMonstersToSpawnNb -= 1f;
                }
            }
        } else {
            m_nightMonstersToSpawnNb = 0f;
            if (GVars.m_simuTimeD > (double)(m_lastTimeDaySpawn + 2f)) {
                CUnitPlayer? playerAlone = null;
                if ((totalAliveMonsters < GParams.m_monstersDayNb + (PlayerManager.GetNbPlayersConnected() - 1) * GParams.m_monstersDayNbAddPerPlayer
                        || GetPlayerWithFewerMonsters(out playerAlone) < GParams.m_monstersDayNb - 1)
                    && SpawnMonster(nightSpawn: false, forceTarget: playerAlone)) {
                    m_lastTimeDaySpawn = GVars.SimuTime;
                }
            }
        }
        if (unitsToRemove.Count > 0) {
            // O(n*m) solution, it is possible to improve it
            unitsToRemove.ForEach(RemoveUnit);
            unitsToRemove.Clear();
        }
        if (Game.SimuTime > lastTimeSentMessageUnitsPos) {
            lastTimeSentMessageUnitsPos = Game.SimuTime + SentMessageUnitsPosPeriod;
            MessageProcessing.SendToAll(new MessageUnitsPos());
        }
    }

    public static void CleanAll() {
        unitsById.AsSpan().Clear();
        units.Clear();
        speciesKilled.Clear();
        m_lastTimeDaySpawn = float.MinValue;
    }

    public static void OnMonsterDeath(CUnitMonster monster) {
        if (monster is CUnitBoss && monster.m_isCreativeSpawn) {
            return;
        }
        CSpeciesKillsInfo? specieKillInfo = speciesKilled.Find(x => x.m_uDesc == monster.m_uDesc);
        if (specieKillInfo is null) {
            Logging.Info($"Adding the monster {monster.UDesc} in the species killed list");
            specieKillInfo = new CSpeciesKillsInfo() {
                m_uDesc = monster.UDesc
            };
            speciesKilled.Add(specieKillInfo);
        }
        specieKillInfo.m_nb += 1;
        specieKillInfo.m_lastKillTime = Game.SimuTime;
    }

    private static ushort SearchFreeUnitInstanceId() {
        ushort currentId = lastUnitInstanceId;
        uint numberOfTries = 0;
        while (unitsById[currentId] != null && numberOfTries < 65000) {
            if (currentId > maxUnitInstanceId) {
                currentId = minUnitInstanceId;
            } else {
                currentId += 1;
            }
            numberOfTries += 1;
        }
        lastUnitInstanceId = currentId;
        return currentId;
    }

    public static CUnit? GetUnitById(ushort id) {
        return unitsById[id];
    }

    public static CUnit? SpawnUnit(CUnit.CDesc uDesc, Vector2 pos, ushort instanceId = 65535) {
        if (instanceId == ushort.MaxValue) {
            instanceId = SearchFreeUnitInstanceId();
        }
        CUnit unit = uDesc.CreateUnit(pos);

        if (unitsById[instanceId] is CUnit excessUnit) {
            Logging.Warning($"Unit with ID {instanceId} ({excessUnit}) already spawned, removing it");
            units.Remove(excessUnit);
        }
        unit.m_id = instanceId;
        unitsById[instanceId] = unit;
        units.Add(unit);

        Logging.Info($"Spawning unit '{uDesc.m_codeName}' (pos={pos}, id={instanceId})");
        MessageProcessing.SendToAll(new MessageSpawnUnit(uDesc, pos, instanceId));

        return unit;
    }

    private static bool SpawnMonster(bool nightSpawn, CUnit? forceTarget = null, CUnitMonster.CDesc? forceUnit = null) {
        CPlayer? player = null;
        if (forceTarget is CUnitPlayer unitPlayer_) {
            player = unitPlayer_.GetPlayer();
        } else if (PlayerManager.players.Count > 0) {
            for (int i = 0; i < 10000; i++) {
                player = PlayerManager.players[Random.IntBetween(0, PlayerManager.players.Count)];
                if (player.HasUnitPlayerAlive() || (i > 5000 && player.HasUnitPlayer())) {
                    break;
                }
            }
        }
        if (player is null || player.m_unitPlayer is null) {
            return false;
        }
        CUnitPlayer unitPlayer = player.m_unitPlayer;
        CUnit.CDesc? unitDesc = forceUnit;
        if (forceUnit is null) {
            SUnits_SpawnPool.Reset();
            if (nightSpawn && speciesKilled.Count != 0) {
                for (int i = speciesKilled.Count - 1; i >= 0 && SUnits_SpawnPool.GetCount() < 4; --i) {
                    if (speciesKilled[i].m_uDesc is not CUnitBoss.CDesc) {
                        SUnits_SpawnPool.Add(speciesKilled[i].m_uDesc);
                    }
                }
            } else if (!nightSpawn) {
                if (/*SOutgame.Params.m_dynamicSpawn*/ false) {
                    // SUnits_SpawnPool.AddDynamic();
                } else {
                    CUnitMonster.CDesc[] monstersList = SOutgame.Mode.GetMonstersList(unitPlayer.m_pos);
                    for (int j = 0; j < monstersList.Length; j++) {
                        SUnits_SpawnPool.Add(monstersList[j]);
                    }
                }
            }
            unitDesc = SUnits_SpawnPool.GetUnitToSpawn(nightSpawn);
        }
        if (unitDesc is not null) {
            bool isUnitBird = unitDesc is CUnitBird.CDesc;
            bool isUnitFish = unitDesc is CUnitFish.CDesc;
            bool isUnitWall = unitDesc is CUnitWall.CDesc;
            int num2 = 0;
            for (int k = 0; k < 3000; k++) {
                Vector2 randomValidSpawnPos = GetRandomValidSpawnPos(player);
                if (randomValidSpawnPos == Vector2.zero) {
                    continue;
                }
                int2 cellPos = (int2)randomValidSpawnPos;
                ref readonly CCell cell = ref World.Grid[cellPos];
                if (
                    // Horizontal distance check
                    (!(Math.Abs(randomValidSpawnPos.x - unitPlayer.m_pos.x) > Math.Abs(randomValidSpawnPos.y - unitPlayer.m_pos.y)) && k <= 500)
                    // Cell passability and spawn conditions
                    || (!cell.IsPassable() && (k <= 1500 || !nightSpawn || cell.GetContent() is not CItem_Mineral))
                    // Water and surface conditions
                    || ((!isUnitBird || cell.m_water >= 0.5f) &&
                        (!isUnitFish || cell.m_water <= 0.5f) &&
                        (!isUnitWall || (!cell.HasBgSurfaceOrBackwall() && k <= 600)) &&
                        (isUnitBird || isUnitFish || (!World.Grid[cellPos.x, cellPos.y - 1].IsContentBlock() && k <= 600)))
                    // Water depth check
                    || (!isUnitFish && cell.m_water >= 0.5f && k <= 2000)
                    // Neighbor passability count
                    || (((World.Grid[cellPos.x - 1, cellPos.y].IsPassable() ? 1 : 0) +
                        (World.Grid[cellPos.x + 1, cellPos.y].IsPassable() ? 1 : 0) +
                        (World.Grid[cellPos.x, cellPos.y - 1].IsPassable() ? 1 : 0) +
                        (World.Grid[cellPos.x, cellPos.y + 1].IsPassable() ? 1 : 0)) < 2 && k <= 600)
                    // Player speed and direction check
                    || ((Math.Abs(unitPlayer.m_speed.x) <= 3f || unitPlayer.m_speed.x * (cellPos.x - unitPlayer.PosCell.x) <= 0f) && k <= 300)
                    // Backwall presence
                    || (cell.GetBackwall() is not null)
                ) {
                    continue;
                }
                Vector2 vector = randomValidSpawnPos + Vector2.right * 0.5f + Vector2.up * unitDesc.m_size.y / 2f;
                int2 int4 = int2.negative;
                if (nightSpawn && num2 < 3 && unitDesc is not CUnitFish.CDesc) {
                    CUnitPlayer? closestPlayer = GetClosestPlayer(vector, 10000f);
                    if (closestPlayer is not null) {
                        int4 = (int2)closestPlayer.m_posLastOnGroundOrWater;
                        // SSingleton<SWorld_PF>.Inst.GetPath(unitDesc, vector, int4, m_spawnPfPath);
                        num2++;
                        // if (m_spawnPfPath.Count == 0) {
                        //     continue;
                        // }
                    }
                }
                CUnitMonster? unitMonster = SpawnUnit(unitDesc, randomValidSpawnPos + Vector2.right * 0.5f) as CUnitMonster
                    ?? throw new InvalidOperationException($"Failed to spawn unit {unitDesc}");
                unitMonster.m_target = forceTarget;
                unitMonster.m_isNightSpawn = nightSpawn;
                // if (num2 > 0 && m_spawnPfPath.Count != 0) {
                //     unitMonster.SetPfPath(m_spawnPfPath, vector, int4);
                // }
                if (!cell.IsPassable() && cell.GetContent() is CItem_Mineral) {
                    World.DestroyCell(cellPos, loot: 0);
                    // Debug.Log("caution, spawned in mineral");
                }
                return true;
            }
        }
        // Debug.LogWarning(Time.frameCount + " Player=" + player.m_unitPlayerId + " Monster=" + ((unitDesc == null) ? "-" : unitDesc.m_codeName) + " : Could not spawn.");
        SUnits_SpawnPool.SetUnitCannotSpawn(unitDesc);
        return false;
    }

    public static CUnitPlayer? GetClosestPlayer(Vector2 pos, float distanceMax = 10000f) {
        CUnitPlayer? result = null;
        float sqrDistanceMax = distanceMax * distanceMax;
        foreach (CPlayer player in PlayerManager.players) {
            CUnitPlayer? unitPlayer = player.m_unitPlayer;
            if (unitPlayer is null || !unitPlayer.IsAlive() || player.IsAFK()) {
                continue;
            }
            float sqrMagnitude = (unitPlayer.PosCenter - pos).SqrMagnitude();
            if (sqrMagnitude <= sqrDistanceMax) {
                result = unitPlayer;
                sqrDistanceMax = sqrMagnitude;
            }
        }
        return result;
    }

    private static Vector2 GetRandomValidSpawnPos(CPlayer player) {
        RectInt rectAroundScreen = player.GetRectAroundScreen(GameState.m_distMonstersSpawn);
        Vector2 spawnPos = new(
            rectAroundScreen.x + Random.Float() * rectAroundScreen.width,
            rectAroundScreen.y + Random.Float() * rectAroundScreen.height
        );
        if (!World.IsInRectM2(spawnPos)) {
            return Vector2.zero;
        }
        if (!World.IsInRectCam(spawnPos)) {
            return Vector2.zero;
        }
        foreach (CPlayer itPlayer in PlayerManager.players) {
            if (itPlayer.HasUnitPlayerAlive() && itPlayer.GetRectAroundScreen(1).Contains(spawnPos.x, spawnPos.y)) {
                return Vector2.zero;
            }
        }
        return spawnPos;
    }

    private static int GetPlayerWithFewerMonsters(out CUnitPlayer? playerAlone) {
        playerAlone = null;
        int resultMonstersCount = int.MaxValue;
        foreach (CPlayer player in PlayerManager.players) {
            if (!player.HasUnitPlayerAlive()) {
                continue;
            }
            int monstersCount = 0;
            RectInt rectAroundScreen = player.GetRectAroundScreen(GameState.m_distMonstersSpawn);
            for (int j = 0; j < units.Count; j++) {
                if (units[j] is CUnitMonster unitMonster && unitMonster.IsAlive() && rectAroundScreen.Contains(unitMonster.PosCenter)) {
                    monstersCount++;
                }
            }
            if (monstersCount < resultMonstersCount) {
                playerAlone = player.m_unitPlayer;
                resultMonstersCount = monstersCount;
            }
        }
        return resultMonstersCount;
    }

    public static void RemoveUnit(CUnit unit) {
        unitsById[unit.m_id] = null;
        units.Remove(unit);

        MessageProcessing.SendToAll(new MessageRemoveUnit(unit));
    }
    public static bool IsUnitInCell(int2 cell, bool skipDrones = false) {
        foreach (CUnit unit in units) {
            if (unit.IsAlive() && (!skipDrones || unit is not CUnitDrone)) {
                Rect rectCol = unit.RectCol;
                if (rectCol.x < (cell.x + 1) && rectCol.xMax > cell.x && rectCol.y < (cell.y + 1) && rectCol.yMax > cell.y) {
                    return true;
                }
            }
        }
        return false;
    }
    public static Vector2 GetBestSpawnPoint(bool onlySkip = false) {
        return (Vector2)GParams.m_spawnPos;
    }

    public static bool IsThereABossAggressive() {
        return false;
    }
}

public class SUnits_SpawnPool {
    private class CUDescParams {
        public bool m_spawn = true;
        public int m_failedSpawn;
        public float m_proba;
        public int m_nbBlocked;
        public int m_nbTotal;
    }

    private static readonly Dictionary<CUnit.CDesc, CUDescParams> m_dicoParams = [];

    // private static int[]? m_dynamicSurfacesNb = null;

    public static void SetUnitCannotSpawn(CUnit.CDesc? udesc) {
        if (udesc is not null && m_dicoParams.TryGetValue(udesc, out CUDescParams? value)) {
            value.m_failedSpawn++;
        }
    }

    public static int GetCount() {
        return m_dicoParams.Values.Count(param => param.m_spawn);
    }

    public static void ResetPfFails() {
        foreach (var param in m_dicoParams.Values) {
            param.m_failedSpawn = 0;
        }
    }

    public static void Reset() {
        foreach (var param in m_dicoParams.Values) {
            param.m_spawn = false;
        }
    }

    public static void Add(CUnit.CDesc? u1, CUnit.CDesc? u2, CUnit.CDesc? u3 = null, CUnit.CDesc? u4 = null) {
        Add(u1);
        Add(u2);
        Add(u3);
        Add(u4);
    }

    public static void Add(CUnit.CDesc? udesc) {
        if (udesc is null) {
            return;
        }
        if (!m_dicoParams.TryGetValue(udesc, out CUDescParams? value)) {
            value = new CUDescParams();
            m_dicoParams[udesc] = value;
        }
        value.m_spawn = true;
    }

    public static CUnit.CDesc? GetUnitToSpawn(bool isNight) {
        foreach (var param in m_dicoParams.Values) {
            param.m_proba = (param.m_nbTotal = (param.m_nbBlocked = 0));
            param.m_proba += param.m_failedSpawn * 150;
        }
        for (int i = 0; i < UnitManager.units.Count; i++) {
            if (UnitManager.units[i] is not CUnitMonster unitMonster || !unitMonster.IsAlive() || !m_dicoParams.ContainsKey(unitMonster.UDesc)) {
                continue;
            }
            m_dicoParams[unitMonster.UDesc].m_proba += (unitMonster.IsLastPathfindSuccessful() && unitMonster.IsPathfindingProgressing()) ? 1 : 15;
            m_dicoParams[unitMonster.UDesc].m_nbTotal++;
            m_dicoParams[unitMonster.UDesc].m_nbBlocked += unitMonster.IsLastPathfindSuccessful() ? 0 : 1;
        }
        float num = 0f;
        foreach (var param in m_dicoParams.Values) {
            if (param.m_spawn) {
                if (isNight) {
                    param.m_proba = (param.m_proba != 0f) ? (1f / param.m_proba) : 2f;
                } else {
                    param.m_proba = 1f;
                }
                num += param.m_proba;
            }
        }
        CUnit.CDesc? result = null;
        float num2 = Random.Float() * num;
        float num3 = 0f;
        foreach (var elem in m_dicoParams) {
            if (elem.Value.m_spawn) {
                num3 += elem.Value.m_proba;
                if (num3 >= num2) {
                    result = elem.Key;
                    break;
                }
            }
        }
        return result;
    }
}

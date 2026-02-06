
using System;
using System.Collections.Generic;

namespace SaveTool.Data;

[Serializable]
public struct int2 {
    public int x;
    public int y;

    public int2(int _x, int _y) {
        this.x = _x;
        this.y = _y;
    }
}
public struct Vector2 {
    public float x;
    public float y;

    public Vector2(float x, float y) {
        this.x = x;
        this.y = y;
    }
}

public struct ItemVar() {
    public float timeLastUse = float.MinValue;
    public float timeActivation = float.MinValue;
    public KeyValuePair<string, float>[] dico = [];
}
public struct Pickup() {
    public ushort id;
    public float x;
    public float y;
    public float creationTime;
}
public struct Unit() {
    public string codeName = "";
    public float x;
    public float y;
    public ushort instanceId;
    public float hp;
    public float air;

    public bool isNightSpawn = false;
    public ushort targetId = ushort.MaxValue;
    public bool isCreativeSpawn = false;
}
public struct SpeciesKillsInfo() {
    public string codeName = "";
    public int nb = 0;
    public float lastKillTime = 0f;
}
public struct Inventory() {
    public struct Item {
        public ushort id;
        public int nb;
    }
    public Item[] items = [];
    public ushort[] barItems = [];
    public ushort itemSelected = 0;
}
public struct Player() {
    public ulong steamId = 0;
    public string name = "";
    public float x;
    public float y;
    public ushort unitPlayerId = ushort.MaxValue;

    public bool skinIsFemale;
    public float skinColorSkin;
    public int skinHairStyle;
    public byte skinColorHairR;
    public byte skinColorHairG;
    public byte skinColorHairB;
    public byte skinColorEyesR;
    public byte skinColorEyesG;
    public byte skinColorEyesB;

    public Inventory inventory;
    public ItemVar?[] itemVars = [];
}
public struct Params() {
    public int m_difficulty;
    public bool m_startCheated;
    public bool m_eventsActive;
    public int2 m_spawnPos;
    public int2 m_shipPos;
    public int2 m_gridSize = new int2(1024, 1024);
    public int m_seed;
    public string m_gameName = "";
    public int m_visibility;
    public string m_passwordMD5 = "";
    public ulong m_hostId;
    public string m_hostName = "";
    public bool m_gameOverIfAllDead = true;
    public int m_nbPlayersMax = 4;
    public bool m_clientGetHostItems = true;
    public bool m_banGiveLootToHost = true;
    public bool m_devMode;
    public bool m_checkMinerals = false; // default in game: true
    public bool m_dynamicSpawn;
    public float m_cloudCycleDistance = 1624f;
    public float m_cloudCycleDuration = 200f;
    public float m_cloudRadius = 100f;
    public float m_rainQuantity = 0.012f;
    public float m_generationOreDiv = 1f;
    public float m_weightMult = 1f;
    public float m_dropChanceMult = 1f;
    public float m_lavaPressureBottomCycle = 2f;
    public float m_lavaPressureTopCycle = 40f;
    public float m_eruptionDurationTotal = 800f;
    public float m_eruptionDurationAcc = 180f;
    public float m_eruptionDurationUp = 500f;
    public float m_eruptionPressure = 125f;
    public float m_eruptionCheckMinY = 780f;
    public float m_dayDurationTotal = 720f;
    public float m_nightDuration = 108f;
    public float m_gravityPlayers = 1f;
    public float m_eventsDelayMin = 700f;
    public float m_eventsDelayMax = 1000f;
    public int m_rocketPreparationDuration = 300;
    public float m_speedSimu = 1f;
    public float m_speedSimuWorld = 1f;
    public bool m_speedSimuWorldLocked;
    public int m_rainY = 880;
    public int m_fastEvaporationYMax = 280;
    public int m_sunLightYMin = 600;
    public int m_sunLightYMax = 665;
    public int m_respawnDelay = -1;
    public float m_dropAtDeathPercent_Peaceful;
    public float m_dropAtDeathPercent_Easy;
    public float m_dropAtDeathPercent_Normal = 0.05f;
    public float m_dropAtDeathPercent_Hard = 0.1f;
    public float m_dropAtDeathPercent_Brutal = 0.15f;
    public int m_dropAtDeathMax = 20;
    public int m_monstersDayNb = 7;
    public int m_monstersDayNbAddPerPlayer = 2;
    public float m_bossRespawnDelay = -1f;
    public float m_monstersNightSpawnRateMult = 1f;
    public float m_monstersNightSpawnRateAddPerPlayer = 0.35f;
    public float m_monstersHpMult = 1f;
    public float m_monstersHpAddPerPlayer = 0.2f;
    public float m_monstersDamagesMult = 1f;
    public float m_monstersDamagesAddPerPlayer = 0.1f;
}
public struct GlobalVars() {
    // flattened from CDesc
    public string m_mod = "Solo";
    public string m_id = "params";
    public int m_idNum = 0;

    public string m_lastSaveDate = "";
    public double m_simuTimeD = 0.0;
    public double m_worldTimeD = 0.0;
    public float m_clock = 0f;
    public float m_cloudPosRatio = 0f;
    public ushort m_droneTargetId = 0;
    public bool m_achievementsLocked = false;
    public int m_eventIdNum = -1;
    public float m_eventStartTime = 0f;
    public bool m_lavaCycleSkipped = false;
    public string m_bossAreas = "";
    public bool m_monsterT2AlreadyHit = false;
    public float m_eruptionTime = 0f;
    public float m_eruptionStartPressure = 0f;
    public bool m_brokenHeart = false;
    public int2 m_heartPos = new int2(-1, -1);
    public float m_cinematicIntroTime = float.MinValue;
    public Vector2 m_cinematicRocketPos = new Vector2(0f, 0f);
    public RocketStep m_cinematicRocketStep = RocketStep._Inactive;
    public float m_cinematicRocketStepStartTime = float.MinValue;
    public bool m_postGame = false;
    public float m_autoBuilderLastTimeFound = 0f;
    public bool m_achievNoElectricity = false;
    public bool m_achievNoShoot = false;
    public bool m_achievNoCraft = false;
    public bool m_achievNoMK2 = false;
    public bool m_achievWentToSea = false;
    public bool m_achievEarlyDive = false;
    public List<string> m_aiSentencesTold = [];
    public int m_autoBuilderLevelBuilt = 0;
    public int m_autoBuilderLevelUsed = -1;
    public int m_nbNightsSurvived = 0;
    public bool m_bossKilled_Madcrab = false;
    public bool m_bossKilled_FireflyQueen = false;
    public bool m_bossKilled_DwellerLord = false;
    public bool m_bossKilled_Balrog = false;
    public float m_droneLastTimeEnters = float.MinValue;
    public float m_droneLastTimeDontEnter = float.MinValue;
    public int m_droneComboNb = 1;

    public enum RocketStep {
        _Inactive,
        Count0_50,
        Count50_Wait,
        Count50to100,
        Liftoff
    }
}

public class GameState {
    public float version = 1.1f;
    public int versionBuild = 1000;
    public string modeName = "";
    public Params @params = new();
    public string[] itemsInSave = [];
    public Pickup[] pickups = [];
    public Player[] players = [];
    public string[] lastEvents = [];
    public byte[] worldData = [];
    public Unit[] units = [];
    public SpeciesKillsInfo[] speciesKilled = [];
    public GlobalVars vars = new();
}

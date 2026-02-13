
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Buffers.Binary;
using SaveTool.Data;

namespace SaveTool;

public class SaveLoadingException(string message) : Exception(message) { }

public static class V0_25_Converter {
    private static bool IsUnitMonster(string unitCodeName) {
        return unitCodeName is not ("player" or "defense" or "drone" or "droneCombat" or "droneWar");
    }

    public static Data.GameState Deserialize(BinaryReader rawReader) {
        Data.GameState gameState = new();
        if (new string(rawReader.ReadChars(8)) != "VERSION=") {
            throw new SaveLoadingException("Invalid header (v0.0x)");
        }
        float mixedVersion = rawReader.ReadSingle();
        int build = (int)(mixedVersion / 10f);
        if (mixedVersion % 10f < 0.2f) {
            throw new SaveLoadingException("Invalid header (v0.1x)");
        }
        // unused
        int _compressedDataLength = rawReader.ReadInt32();
        if (rawReader.ReadString() != "Uncompressed Data") { // magic string
            throw new SaveLoadingException("Uncompressed Data");
        }
        byte[] compressedData = rawReader.ReadBytes((int)(rawReader.BaseStream.Length - rawReader.BaseStream.Position));

        byte[]? rawData = Utils.CLZF2.Decompress(compressedData);
        if (rawData is null) {
            throw new SaveLoadingException("Corrupted compressed data");
        }
        var reader = new BinaryReader(new MemoryStream(rawData));
        if (reader.ReadString() != "Compressed Data Start") { // magic string
            throw new SaveLoadingException("Compressed Data Start");
        }

        gameState.itemsInSave = new string[reader.ReadInt32() - 1];
        for (int i = 0; i < gameState.itemsInSave.Length; ++i) {
            gameState.itemsInSave[i] = reader.ReadString();
        }
        var mainPlayer = new Data.Player();
        mainPlayer.inventory.items = new Data.Inventory.Item[reader.ReadInt32()];
        for (int i = 0; i < mainPlayer.inventory.items.Length; ++i) {
            mainPlayer.inventory.items[i].id = reader.ReadUInt16();
            mainPlayer.inventory.items[i].nb = reader.ReadInt32();
        }
        mainPlayer.itemVars = new Data.ItemVar?[mainPlayer.inventory.items.Length];
        for (int i = 0; i < mainPlayer.itemVars.Length; ++i) {
            var itemVar = new Data.ItemVar() {
                timeLastUse = reader.ReadSingle(),
                timeActivation = reader.ReadSingle(),
                dico = new KeyValuePair<string, float>[reader.ReadInt32()],
            };
            for (int j = 0; j < itemVar.dico.Length; ++j) {
                itemVar.dico[j] = new(reader.ReadString(), reader.ReadSingle());
            }
            // we need to skip first item var to make the game not "touch" null item (0th one) on save loading
            if (i == 0) { continue; }

            mainPlayer.itemVars[i] = itemVar;
        }
        // length should always be 20 or 17 depending on version
        mainPlayer.inventory.barItems = new ushort[reader.ReadInt32()];
        for (int i = 0; i < mainPlayer.inventory.barItems.Length; ++i) {
            mainPlayer.inventory.barItems[i] = reader.ReadUInt16();
        }
        // v0.25: stores bar index of selected item slot
        // v1.11: stores item instanceId of selected item slot
        // also need convert save item instanceId -> game item instanceId
        // player.inventory.itemSelected = player.inventory.barItems[reader.ReadInt32()];
        _ = reader.ReadInt32();

        gameState.pickups = new Data.Pickup[reader.ReadInt32()];
        for (int i = 0; i < gameState.pickups.Length; ++i) {
            // game stores pickup item instanceId as UInt16, but reads it as Int16 (in v0.25 and latest)
            gameState.pickups[i].id = reader.ReadUInt16();
            gameState.pickups[i].x = reader.ReadSingle();
            gameState.pickups[i].y = reader.ReadSingle();
            gameState.pickups[i].creationTime = reader.ReadSingle();
        }
        if (reader.ReadString() != "Items Data") { // magic string
            throw new SaveLoadingException("Items Data");
        }
        // explanation: game uses SWorldDll.GetSaveOffset method to calculate offset between world rows while saving/loading
        // since world size in v0.25 always* 1024x1024, we can calculate sum of returned values
        // from SWorldDll.GetSaveOffset from x=0 to x=1023 (inclusive) and it would be 4727
        // second magic value: each world cell is stored with 19 bytes each, multiply it with 1024^2 and we get 19922944
        // basically, since the world data world is same between v0.25 and latest we can just forward it
        gameState.worldData = reader.ReadBytes(4727 + 19922944);
        if (reader.ReadString() != "World Data") { // magic string
            throw new SaveLoadingException("World Data");
        }
        gameState.units = new Data.Unit[reader.ReadInt32()];
        for (int i = 0; i < gameState.units.Length; ++i) {
            var unit = new Data.Unit() {
                codeName = reader.ReadString(),
                x = reader.ReadSingle(),
                y = reader.ReadSingle(),
                instanceId = (ushort)reader.ReadInt32(),
                hp = reader.ReadSingle(),
                air = reader.ReadSingle()
            };
            if (IsUnitMonster(unit.codeName)) {
                unit.isNightSpawn = reader.ReadBoolean();
            }
            // dummy data
            _ = reader.ReadBytes(16);
            if (unit.codeName == "player") {
                mainPlayer.unitPlayerId = unit.instanceId;
            }
            gameState.units[i] = unit;
        }
        gameState.players = new Data.Player[1] { mainPlayer };

        gameState.speciesKilled = new Data.SpeciesKillsInfo[reader.ReadInt32()];
        for (int i = 0; i < gameState.speciesKilled.Length; ++i) {
            gameState.speciesKilled[i].codeName = reader.ReadString();
            gameState.speciesKilled[i].nb = 1;
        }
        // unit instance id max, not used by latest game version in any way, so it's ignored
        _ = reader.ReadInt32();

        if (reader.ReadString() != "Units Data") { // magic string
            throw new SaveLoadingException("Units Data");
        }
        static void CheckVarType(string actual, string expected) {
            if (actual != expected) {
                throw new SaveLoadingException($"Mismatched field type: expected '{expected}', got '{actual}'");
            }
        }

        ref var @params = ref gameState.@params;
        ref var vars = ref gameState.vars;

        float oldRocketActivationTime = float.MinValue;
        float oldRocketEnterTime = float.MinValue;
        float oldRocketLaunchTime = float.MinValue;

        int varsCount = reader.ReadInt32();
        for (int i = 0; i < varsCount; ++i) {
            string varName = reader.ReadString();
            string varType = reader.ReadString();

            switch (varName) {
            case "m_difficulty":
                CheckVarType(varType, "Int32");
                @params.m_difficulty = BinFmtCodec.ReadInt(reader);
                break;
            case "m_seed":
                CheckVarType(varType, "Int32");
                @params.m_seed = BinFmtCodec.ReadInt(reader);
                break;
            case "m_shipPos":
                CheckVarType(varType, "int2");
                @params.m_shipPos = BinFmtCodec.ReadInt2(reader);
                break;
            case "m_lastSaveDate":
                CheckVarType(varType, "String");
                vars.m_lastSaveDate = BinFmtCodec.ReadString(reader);
                break;
            case "m_simuTimeD":
                CheckVarType(varType, "Double");
                vars.m_simuTimeD = BinFmtCodec.ReadDouble(reader);
                break;
            case "m_worldTimeD":
                CheckVarType(varType, "Double");
                vars.m_worldTimeD = BinFmtCodec.ReadDouble(reader);
                break;
            case "m_clock":
                CheckVarType(varType, "Single");
                vars.m_clock = BinFmtCodec.ReadFloat(reader);
                break;
            case "monsterT2AlreadyHit":
                CheckVarType(varType, "Boolean");
                vars.m_monsterT2AlreadyHit = BinFmtCodec.ReadBool(reader);
                break;
            case "m_eruptionTime":
                CheckVarType(varType, "Single");
                vars.m_eruptionTime = BinFmtCodec.ReadFloat(reader);
                break;
            case "m_eruptionStartPressure":
                CheckVarType(varType, "Single");
                vars.m_eruptionStartPressure = BinFmtCodec.ReadFloat(reader);
                break;
            case "m_brokenHeart":
                CheckVarType(varType, "Boolean");
                vars.m_brokenHeart = BinFmtCodec.ReadBool(reader);
                break;
            case "m_cinematicIntroTime":
                CheckVarType(varType, "Single");
                vars.m_cinematicIntroTime = BinFmtCodec.ReadFloat(reader);
                break;
            case "m_cinematicRocketActivationTime":
                CheckVarType(varType, "Single");
                oldRocketActivationTime = BinFmtCodec.ReadFloat(reader);
                break;
            case "m_cinematicRocketEnterTime":
                CheckVarType(varType, "Single");
                oldRocketEnterTime = BinFmtCodec.ReadFloat(reader);
                break;
            case "m_cinematicRocketLaunchTime":
                CheckVarType(varType, "Single");
                oldRocketLaunchTime = BinFmtCodec.ReadFloat(reader);
                break;
            case "m_cinematicRocketPos":
                CheckVarType(varType, "Vector2");
                vars.m_cinematicRocketPos = BinFmtCodec.ReadVector2(reader);
                break;
            case "m_postGame":
                CheckVarType(varType, "Boolean");
                vars.m_postGame = BinFmtCodec.ReadBool(reader);
                break;
            case "m_autoBuilderLastTimeFound":
                CheckVarType(varType, "Single");
                vars.m_autoBuilderLastTimeFound = BinFmtCodec.ReadFloat(reader);
                break;
            case "m_achievNoElectricity":
                CheckVarType(varType, "Boolean");
                vars.m_achievNoElectricity = BinFmtCodec.ReadBool(reader);
                break;
            case "m_achievNoShoot":
                CheckVarType(varType, "Boolean");
                vars.m_achievNoShoot = BinFmtCodec.ReadBool(reader);
                break;
            case "m_achievNoCraft":
                CheckVarType(varType, "Boolean");
                vars.m_achievNoCraft = BinFmtCodec.ReadBool(reader);
                break;
            case "m_achievNoMK2":
                CheckVarType(varType, "Boolean");
                vars.m_achievNoMK2 = BinFmtCodec.ReadBool(reader);
                break;
            case "m_achievWentToSea":
                CheckVarType(varType, "Boolean");
                vars.m_achievWentToSea = BinFmtCodec.ReadBool(reader);
                break;
            case "m_achievEarlyDive":
                CheckVarType(varType, "Boolean");
                vars.m_achievEarlyDive = BinFmtCodec.ReadBool(reader);
                break;
            case "m_aiSentencesTold":
                CheckVarType(varType, "List`1");
                vars.m_aiSentencesTold = BinFmtCodec.ReadStringList_v2000(reader);
                break;
            case "m_autoBuilderLevelBuilt":
                CheckVarType(varType, "Int32");
                vars.m_autoBuilderLevelBuilt = BinFmtCodec.ReadInt(reader);
                break;
            case "m_nbNightsSurvived":
                CheckVarType(varType, "Int32");
                vars.m_nbNightsSurvived = BinFmtCodec.ReadInt(reader);
                break;
            case "m_bossKilled_Madcrab":
                CheckVarType(varType, "Boolean");
                vars.m_bossKilled_Madcrab = BinFmtCodec.ReadBool(reader);
                break;
            case "m_bossKilled_FireflyQueen":
                CheckVarType(varType, "Boolean");
                vars.m_bossKilled_FireflyQueen = BinFmtCodec.ReadBool(reader);
                break;
            case "m_bossKilled_DwellerLord":
                CheckVarType(varType, "Boolean");
                vars.m_bossKilled_DwellerLord = BinFmtCodec.ReadBool(reader);
                break;
            case "m_bossKilled_Balrog":
                CheckVarType(varType, "Boolean");
                vars.m_bossKilled_Balrog = BinFmtCodec.ReadBool(reader);
                break;
            case "m_droneLastTimeEnters":
                CheckVarType(varType, "Single");
                vars.m_droneLastTimeEnters = BinFmtCodec.ReadFloat(reader);
                break;
            case "m_droneLastTimeDontEnter":
                CheckVarType(varType, "Single");
                vars.m_droneLastTimeDontEnter = BinFmtCodec.ReadFloat(reader);
                break;
            case "m_droneComboNb":
                CheckVarType(varType, "Int32");
                vars.m_droneComboNb = BinFmtCodec.ReadInt(reader);
                break;
            default:
                throw new SaveLoadingException($"Unusual field name: '{varName}'");
            }
        }
        // note: no mapping for RocketStep.Count50to100 since v0.25 didn't have a distinct 50->100% counting phase
        if (oldRocketLaunchTime > 0f) {
            vars.m_cinematicRocketStep = Vars.RocketStep.Liftoff;
            vars.m_cinematicRocketStepStartTime = oldRocketLaunchTime;
        } else if (oldRocketEnterTime > 0f) {
            vars.m_cinematicRocketStep = Vars.RocketStep.Count50_Wait;
            vars.m_cinematicRocketStepStartTime = oldRocketEnterTime;
        } else if (oldRocketActivationTime > 0f) {
            vars.m_cinematicRocketStep = Vars.RocketStep.Count0_50;
            vars.m_cinematicRocketStepStartTime = oldRocketActivationTime;
        } else {
            vars.m_cinematicRocketStep = Vars.RocketStep._Inactive;
            vars.m_cinematicRocketStepStartTime = float.MinValue;
        }

        if (reader.ReadString() != "Vars Data") { // magic string
            throw new SaveLoadingException("Vars Data");
        }
        if (reader.BaseStream.Position < reader.BaseStream.Length) {
            throw new SaveLoadingException("Non-zero bytes left to read");
        }
        return gameState;
    }
}

public static class V0_13_Converter {
    private static readonly Dictionary<short, string> mapUnitIdToCodeName = new() {
        { 0, "player" },
        { 1, "defense" },
        { 100, "hound" },
        { 101, "firefly" },
        { 110, "fireflyRed" },
        { 111, "dweller" },
        { 112, "fish" },
        { 113, "bat" },
        { 121, "houndBlack" },
        { 122, "fireflyBlack" },
        { 123, "dwellerBlack" },
        { 124, "fishBlack" },
        { 125, "batBlack" },
        { 126, "bossMadCrab" },
        { 140, "shark" },
        { 141, "fireflyExplosive" },
        { 142, "antClose" },
        { 143, "antDist" },
        { 144, "bossFirefly" }
    };
    private static readonly Dictionary<short, ushort> mapItemId = new() {
        { -1, 0 }, // null item
        { 0, 0 }, // null item
        { 1110, 1 }, // dirt
        { 1120, 2 }, // dirtRed
        { 1130, 3 }, // silt
        { 1140, 4 }, // dirtBlack
        { 1150, 5 }, // dirtSky
        { 1200, 6 }, // rock
        { 1210, 7 }, // iron
        { 1220, 8 }, // coal
        { 1230, 9 }, // copper
        { 1240, 10 }, // gold
        { 1300, 11 }, // aluminium
        { 1310, 12 }, // rockFlying
        { 1320, 13 }, // rockGaz
        { 1400, 14 }, // granit
        { 1410, 15 }, // uranium
        { 1420, 16 }, // crystal
        { 1430, 17 }, // crystalLight
        { 1440, 18 }, // crystalBlack
        { 1500, 19 }, // lava
        { 1600, 20 }, // lavaOld
        { 2010, 21 }, // wood
        { 2020, 22 }, // woodwater
        { 2030, 23 }, // woodSky
        { 2040, 24 }, // deadPlant
        { 2100, 25 }, // tree
        { 2110, 26 }, // treePine
        { 2120, 27 }, // treeWater
        { 2130, 28 }, // treeSky
        { 2200, 29 }, // bush
        { 2210, 30 }, // flowerBlue
        { 2220, 31 }, // flowerWhite
        { 2230, 32 }, // fernRed
        { 2240, 33 }, // waterBush
        { 2250, 34 }, // waterLight
        { 2260, 35 }, // waterCoral
        { 2270, 36 }, // blackGrass
        { 2280, 37 }, // blackMushroom
        { 2290, 38 }, // skyBush
        { 2300, 39 }, // metalScrap
        { 2310, 40 }, // lightGem
        { 2312, 41 }, // energyGem
        { 2314, 42 }, // darkGem
        { 2320, 43 }, // dogHorn
        { 2322, 44 }, // dogHorn3
        { 2330, 45 }, // moleShell
        { 2332, 46 }, // moleShellBlack
        { 2340, 47 }, // fish2Regen
        { 2342, 48 }, // fish3Regen
        { 2350, 49 }, // bat2Sonar
        { 2352, 50 }, // bat3Sonar
        { 2360, 51 }, // antShell
        { 2370, 52 }, // sharkSkin
        { 2700, 53 }, // bloodyFlesh1
        { 2710, 53 }, // special: mapping from even older saves
        { 2720, 54 }, // bloodyFlesh2
        { 2730, 54 }, // special: mapping from even older saves
        { 2900, 55 }, // bossMadCrabSonar
        { 2910, 56 }, // bossMadCrabMaterial
        { 2920, 57 }, // masterGem
        { 3000, 58 }, // miniaturizorMK1
        { 3001, 59 }, // miniaturizorMK2
        { 3002, 60 }, // miniaturizorMK3
        { 3003, 61 }, // miniaturizorMK4
        { 3100, 62 }, // potionHp
        { 3230, 63 }, // potionHpRegen
        { 3210, 64 }, // potionHpBig
        { 3810, 65 }, // potionCritics
        { 3820, 66 }, // potionArmor
        { 3220, 67 }, // armorMk1
        { 3300, 68 }, // armorMk2
        { 3420, 69 }, // armorMk3
        { 3200, 70 }, // flashLight
        { 3310, 71 }, // electricWire
        { 3320, 72 }, // metalDetector
        { 3330, 73 }, // effeilGlasses
        { 3400, 74 }, // waterBreather
        { 3410, 75 }, // jetpack
        { 4100, 76 }, // gunRifle
        { 4110, 77 }, // gunShotgun
        { 4200, 78 }, // gunMachineGun
        { 4300, 79 }, // gunSnipe
        { 4310, 80 }, // gunLaser
        { 4320, 81 }, // gunRocket
        { 4410, 82 }, // gunZF0
        { 4420, 83 }, // gunMegaSnipe
        { 4430, 84 }, // gunLaserGatling
        { 4400, 85 }, // gunStorm
        { 5100, 86 }, // wallWood
        { 5110, 87 }, // platform
        { 5200, 88 }, // wallConcrete
        { 5210, 89 }, // wallIronSupport
        { 5220, 90 }, // backwall
        { 5300, 91 }, // wallReinforced
        { 5310, 92 }, // wallDoor
        { 5320, 93 }, // platformSteel
        { 5330, 94 }, // generatorWater
        { 5340, 95 }, // waterPump
        { 5400, 96 }, // wallComposite
        { 5410, 97 }, // wallCompositeSupport
        { 5420, 98 }, // wallCompositeLight
        { 6100, 99 }, // turret360
        { 6200, 100 }, // turretGatling
        { 6300, 101 }, // turretHeavy
        { 6310, 102 }, // turretReparator
        { 6320, 103 }, // turretMine
        { 6400, 104 }, // turretCeiling
        { 6410, 105 }, // turretLaser
        { 7000, 106 }, // autoBuilderMK1
        { 7010, 107 }, // autoBuilderMK2
        { 7020, 108 }, // autoBuilderMK3
        { 7030, 109 }, // autoBuilderMK4
        { 7200, 110 }, // light
        { 7300, 111 }, // lightSticky
        { 7310, 112 }, // generatorSun
        { 7320, 113 }, // lightSun
        { 7330, 114 }, // teleport
        { 7900, 115 }, // rocketTop
        { 7910, 116 }, // rocketTank
        { 7920, 117 } // rocketEngine
    };
    private static readonly string[] itemsInSaveArray = [
        "dirt", "dirtRed", "silt", "dirtBlack", "dirtSky", "rock", "iron", "coal", "copper",
        "gold", "aluminium", "rockFlying", "rockGaz", "granit", "uranium", "crystal",
        "crystalLight", "crystalBlack", "lava", "lavaOld", "wood", "woodwater", "woodSky",
        "deadPlant", "tree", "treePine", "treeWater", "treeSky", "bush", "flowerBlue",
        "flowerWhite", "fernRed", "waterBush", "waterLight", "waterCoral", "blackGrass",
        "blackMushroom", "skyBush", "metalScrap", "lightGem", "energyGem", "darkGem",
        "dogHorn", "dogHorn3", "moleShell", "moleShellBlack", "fish2Regen", "fish3Regen",
        "bat2Sonar", "bat3Sonar", "antShell", "sharkSkin", "bloodyFlesh1", "bloodyFlesh2",
        "bossMadCrabSonar", "bossMadCrabMaterial", "masterGem", "miniaturizorMK1", "miniaturizorMK2",
        "miniaturizorMK3", "miniaturizorMK4", "potionHp", "potionHpRegen", "potionHpBig",
        "potionCritics", "potionArmor", "armorMk1", "armorMk2", "armorMk3", "flashLight",
        "electricWire", "metalDetector", "effeilGlasses", "waterBreather", "jetpack",
        "gunRifle", "gunShotgun", "gunMachineGun", "gunSnipe", "gunLaser", "gunRocket",
        "gunZF0", "gunMegaSnipe", "gunLaserGatling", "gunStorm", "wallWood", "platform",
        "wallConcrete", "wallIronSupport", "backwall", "wallReinforced", "wallDoor",
        "platformSteel", "generatorWater", "waterPump", "wallComposite", "wallCompositeSupport",
        "wallCompositeLight", "turret360", "turretGatling", "turretHeavy", "turretReparator",
        "turretMine", "turretCeiling", "turretLaser", "autoBuilderMK1", "autoBuilderMK2",
        "autoBuilderMK3", "autoBuilderMK4", "light", "lightSticky", "generatorSun",
        "lightSun", "teleport", "rocketTop", "rocketTank", "rocketEngine"
    ];

    private static uint ConvertCellFlags(uint x) {
        // generated with https://programming.sirrida.de/calcperm.php
        // index vector: * * * * * * * * * * 8 * 9 10 11 * 12 * 13 * 14 * * * 0 * * 4 18 * 15 * *
        // settings: LSB; origin 0; base 10; target bits
        return 0b00000000_00000100_11111111_00010001 & (
                ((x & 0x01000000) >> 24)
              | ((x & 0x08000000) >> 23)
              | ((x & 0x40000000) >> 15)
              | ((x & 0x10000000) >> 10)
              | ((x & 0x00100000) >> 6)
              | ((x & 0x00040000) >> 5)
              | ((x & 0x00010000) >> 4)
              | ((x & 0x00007000) >> 3)
              | ((x & 0x00000400) >> 2));
    }

    private static ushort ConvertItemId(short id) {
        return mapItemId.TryGetValue(id, out ushort value) ? value : (ushort)0;
    }

    public static Data.GameState Deserialize(BinaryReader rawReader) {
        Data.GameState gameState = new();
        if (new string(rawReader.ReadChars(8)) != "VERSION=") {
            throw new SaveLoadingException("Invalid header (v0.0x) (error code: -1)");
        }
        float version = rawReader.ReadSingle();
        _ = rawReader.ReadInt32(); // compressed data length (ignore, unused)

        if (rawReader.ReadString() != "OK") {
            throw new SaveLoadingException("Invalid magic string (error code: 1)");
        }
        byte[] compressedData = rawReader.ReadBytes((int)(rawReader.BaseStream.Length - rawReader.BaseStream.Position));

        byte[]? rawData = Utils.CLZF2.Decompress(compressedData);
        if (rawData is null) {
            throw new SaveLoadingException("Corrupted compressed data (error code: 2)");
        }
        using var reader = new BinaryReader(new MemoryStream(rawData));

        _ = reader.ReadString(); // save creation date (`DateTime.Now.ToString()`)
        gameState.@params.m_difficulty = reader.ReadInt32();
        gameState.@params.m_seed = reader.ReadInt32();
        gameState.@params.m_shipPos = new Data.int2(reader.ReadInt32(), reader.ReadInt32());
        gameState.vars.m_simuTimeD = reader.ReadDouble();
        gameState.vars.m_clock = reader.ReadSingle();
        if (reader.ReadString() != "OK") {
            throw new SaveLoadingException("Invalid magic string (error code: 3)");
        }
        // 19922944 = 1024*1024*19 (19 = cell size in bytes)
        // 4727 = sum of SWorldDll.GetSaveOffset(x), where x in [0, 1023]
        gameState.worldData = new byte[4727 + 19922944];

        // old cell layout: uint32 flags, uint16 contentId, uint16 contentHP, float water, int16 forceX, int16 forceY, byte light (17 bytes)
        // new cell layout: uint32 flags, uint16 contentId, uint16 contentHP, float water, int16 forceX, int16 forceY, byte lightR, byte lightG, byte lightB (19 bytes)
        int worldDataIdx = 0;
        for (int i = 0; i < 1024; ++i) {
            worldDataIdx += V1_11_Converter.GetSaveOffset(i);
            for (int j = 0; j < 1024; ++j) {
                // convert bit flags in old to new format that are doing same function
                uint newFlags = ConvertCellFlags(reader.ReadUInt32());
                BinaryPrimitives.WriteUInt32LittleEndian(gameState.worldData.AsSpan(worldDataIdx), newFlags);

                // convert old item id to new ids
                ushort contentId = ConvertItemId((short)reader.ReadUInt16());
                BinaryPrimitives.WriteUInt32LittleEndian(gameState.worldData.AsSpan(worldDataIdx + 4), contentId);

                // copy hp, water, forceX, forceY, lightR
                _ = reader.Read(gameState.worldData, worldDataIdx + 6, 11);

                byte light = gameState.worldData[worldDataIdx + 16];
                gameState.worldData[worldDataIdx + 17] = light; // green
                gameState.worldData[worldDataIdx + 18] = light; // blue
                worldDataIdx += 19;
            }
        }
        if (worldDataIdx != gameState.worldData.Length) {
            throw new SaveLoadingException("Incomplete world data convertion");
        }
        if (reader.ReadString() != "OK") {
            throw new SaveLoadingException("Invalid magic string (error code: 4)");
        }
        var mainPlayer = new Data.Player();

        gameState.units = new Data.Unit[reader.ReadInt32()];
        for (int i = 0; i < gameState.units.Length; ++i) {
            short unitId = reader.ReadInt16();
            var unit = new Data.Unit() {
                codeName = mapUnitIdToCodeName[unitId],
                x = reader.ReadSingle(),
                y = reader.ReadSingle(),
                instanceId = (ushort)i,
                hp = reader.ReadSingle(),
                air = 1f
            };
            if (unitId >= 100) { // if unit is monster
                unit.isNightSpawn = reader.ReadBoolean();
            }
            if (unitId == 0) { // if unit is player
                mainPlayer.unitPlayerId = unit.instanceId;
            }
            gameState.units[i] = unit;
        }
        gameState.speciesKilled = new Data.SpeciesKillsInfo[reader.ReadInt32()];
        for (int i = 0; i < gameState.speciesKilled.Length; ++i) {
            gameState.speciesKilled[i].codeName = mapUnitIdToCodeName[reader.ReadInt16()];
            gameState.speciesKilled[i].nb = 1;
        }
        if (reader.ReadString() != "OK") {
            throw new SaveLoadingException("Invalid magic string (error code: 5)");
        }

        gameState.itemsInSave = itemsInSaveArray;

        mainPlayer.inventory.items = new Data.Inventory.Item[reader.ReadInt32()];
        for (int i = 0; i < mainPlayer.inventory.items.Length; ++i) {
            mainPlayer.inventory.items[i].id = ConvertItemId(reader.ReadInt16());
            mainPlayer.inventory.items[i].nb = reader.ReadInt32();
        }
        mainPlayer.inventory.barItems = new ushort[reader.ReadInt32()];
        for (int i = 0; i < mainPlayer.inventory.barItems.Length; ++i) {
            mainPlayer.inventory.barItems[i] = ConvertItemId(reader.ReadInt16());
        }
        // old format: stores bar index of selected bar item slot
        // new format: stores item id of selected item slot
        int barItemSelected = reader.ReadInt32();
        mainPlayer.inventory.itemSelected = barItemSelected < 0 ? (ushort)0 : mainPlayer.inventory.barItems[barItemSelected];

        gameState.pickups = new Data.Pickup[reader.ReadInt32()];
        for (int i = 0; i < gameState.pickups.Length; ++i) {
            gameState.pickups[i] = new Data.Pickup() {
                id = ConvertItemId(reader.ReadInt16()),
                x = reader.ReadSingle(),
                y = reader.ReadSingle(),
                creationTime = reader.ReadSingle()
            };
        }
        if (reader.ReadString() != "OK") {
            throw new SaveLoadingException("Invalid magic string (error code: 6)");
        }
        int gameVarsCount = reader.ReadInt32();
        for (int i = 0; i < gameVarsCount; ++i) {
            string gameVarName = reader.ReadString();
            float gameVarValue = reader.ReadSingle();
            if (gameVarName == "NbNightsSurvived") {
                gameState.vars.m_nbNightsSurvived = (int)gameVarValue;
            } else if (gameVarName == "AutoBuilderLevelBuilt") {
                gameState.vars.m_autoBuilderLevelBuilt = (int)gameVarValue;
            } else if (gameVarName == "ShipAiStepId") {
                // ignored
            } else if (gameVarName == "FirstMonsterHitT2") {
                gameState.vars.m_monsterT2AlreadyHit = gameVarValue == 1f;
            } else if (gameVarName == "PlayerAir") {
                gameState.units[mainPlayer.unitPlayerId].air = gameVarValue - 1f;
            }
        }
        gameState.players = new Data.Player[1] { mainPlayer };

        if (reader.ReadString() != "OK") {
            // error code 7 skipped?
            throw new SaveLoadingException("Invalid magic string (error code: 8)");
        }
        if (reader.BaseStream.Position < reader.BaseStream.Length) {
            throw new SaveLoadingException("Non-zero bytes left to read");
        }

        return gameState;
    }
}

public static class V0_06_Converter {
    private static readonly Dictionary<short, string> mapUnitIdToCodeName = new() {
        { 0, "player" },
        { 1, "defense" },
        { 100, "hound" },
        { 101, "firefly" },
        { 110, "fireflyRed" },
        { 111, "dweller" },
        { 112, "fish" },
        { 121, "houndBlack" },
        { 122, "fireflyBlack" },
        { 123, "dwellerBlack" },
        { 124, "fishBlack" },
    };
    private static readonly Dictionary<short, ushort> mapItemId = new() {
        { -1, 0 },   // null (empty bar item)
        { 0, 0 },    // null
        { 1000, 1 },   // "Grass" (no equivalent)
        { 1010, 1 },   // dirt
        { 1100, 2 },   // rock
        { 1110, 3 },   // iron
        { 1120, 4 },   // coal
        { 1200, 5 },   // aluminium
        { 1210, 6 },   // rockFlying
        { 1220, 7 },   // rockGaz
        { 1300, 8 },   // granit
        { 2000, 9 },   // metalScrap
        { 2010, 10 },  // wood
        { 2020, 11 },  // bush
        { 2030, 12 },  // flowerBlue
        { 2040, 13 },  // flowerWhite
        { 2400, 14 },  // lightGem
        { 2410, 15 },  // energyGem
        { 2420, 16 },  // dogHorn
        { 2430, 17 },  // dogHorn3
        { 2440, 18 },  // moleShell
        { 2450, 19 },  // moleShellBlack
        { 2460, 0 },   // "Black Tiger Fish Skin" (no equivalent)
        { 2700, 0 },   // "Empty Bottle" (no equivalent)
        { 3000, 20 },  // miniaturizorMK1
        { 3001, 21 },  // miniaturizorMK2
        { 3002, 22 },  // miniaturizorMK3
        { 3020, 23 },  // flashLight
        { 3030, 24 },  // potionHp
        { 3040, 25 },  // potionHpBig
        { 3050, 26 },  // armorMk1
        { 3060, 27 },  // armorMk2
        { 4000, 28 },  // gunRifle
        { 4010, 29 },  // gunShotgun
        { 4020, 30 },  // gunMachineGun
        { 4030, 31 },  // gunSnipe
        { 4040, 32 },  // gunLaser
        { 5100, 33 },  // platform
        { 5200, 34 },  // wallWood
        { 5210, 35 },  // wallConcrete
        { 5220, 36 },  // wallReinforced
        { 5600, 37 },  // wallIronSupport
        { 5800, 38 },  // backwall
        { 6000, 39 },  // turret360
        { 6010, 40 },  // turretGatling
        { 6020, 40 },  // turretGatling (duplicate old ID)
        { 6030, 41 },  // turretHeavy
        { 6040, 42 },  // turretReparator
        { 7000, 43 },  // autoBuilderMK1
        { 7001, 44 },  // autoBuilderMK2
        { 7002, 45 },  // autoBuilderMK3
        { 7020, 0 },   // "Auto-Builder Rocket" (no equivalent)
        { 7100, 46 },  // light
        { 7200, 47 },  // teleport
        { 7900, 48 },  // rocketTop
        { 7901, 49 },  // rocketTank
        { 7902, 50 }   // rocketEngine
    };
    private static readonly string[] itemsInSaveArray = [
        "dirt", "rock", "iron", "coal", "aluminium", "rockFlying", "rockGaz", "granit",
        "metalScrap", "wood", "bush", "flowerBlue", "flowerWhite", "lightGem", "energyGem",
        "dogHorn", "dogHorn3", "moleShell", "moleShellBlack", "miniaturizorMK1",
        "miniaturizorMK2", "miniaturizorMK3", "flashLight", "potionHp", "potionHpBig",
        "armorMk1", "armorMk2", "gunRifle", "gunShotgun", "gunMachineGun", "gunSnipe",
        "gunLaser", "platform", "wallWood", "wallConcrete", "wallReinforced",
        "wallIronSupport", "backwall", "turret360", "turretGatling", "turretHeavy",
        "turretReparator", "autoBuilderMK1", "autoBuilderMK2", "autoBuilderMK3", "light",
        "teleport", "rocketTop", "rocketTank", "rocketEngine",
        "tree" // extra: used by world cell data convertion (index: 51)
    ];

    private readonly static uint Flag_HasBgSolidDirt = 4096;
    private readonly static uint Flag_HasBgSolidRock = 8192;
    private readonly static uint Flag_HasBgSolidGranit = 16384;
    private readonly static uint Flag_HasBgSolidCrystal = 32768;
    private readonly static uint Flag_WaterFall = 65536;
    private readonly static uint Flag_StreamLFast = 262144;
    private readonly static uint Flag_StreamRFast = 1048576;

    private static uint ConvertCellFlags(uint x) {
        return
              ((x & (Flag_HasBgSolidDirt | Flag_HasBgSolidRock)) >> 3)
            | (((x & Flag_HasBgSolidGranit) >> 5) * 3)
            | ((x & (Flag_HasBgSolidCrystal | Flag_WaterFall)) >> 4)
            | ((x & Flag_StreamLFast) >> 5)
            | ((x & Flag_StreamRFast) >> 6);
    }

    public static Data.GameState Deserialize(BinaryReader reader) {
        Data.GameState gameState = new();

        _ = reader.ReadString(); // save creation date (`DateTime.Now.ToString()`)
        gameState.@params.m_difficulty = reader.ReadInt32();
        gameState.@params.m_seed = reader.ReadInt32();
        gameState.@params.m_shipPos = new Data.int2(reader.ReadInt32(), reader.ReadInt32());
        gameState.vars.m_simuTimeD = (double)reader.ReadSingle();
        gameState.vars.m_clock = reader.ReadSingle();

        if (reader.ReadString() != "OK") {
            throw new SaveLoadingException("Invalid magic string (error code: 1)");
        }
        // 19922944 = 1024*1024*19 (19 = cell size in bytes)
        // 4727 = sum of SWorldDll.GetSaveOffset(x), where x in [0, 1023]
        gameState.worldData = new byte[4727 + 19922944];

        // old cell layout: uint32 flags, uint16 backwallId, uint16 contentId, float contentHP, float water, float light, float forceX, float forceY (28 bytes)
        // new cell layout: uint32 flags, uint16 contentId, uint16 contentHP, float water, int16 forceX, int16 forceY, byte lightR, byte lightG, byte lightB (19 bytes)
        using var worldDataWriter = new BinaryWriter(new MemoryStream(gameState.worldData));
        for (int i = 0; i < 1024; ++i) {
            worldDataWriter.Seek(V1_11_Converter.GetSaveOffset(i), SeekOrigin.Current);
            for (int j = 0; j < 1024; ++j) {
                uint flags = ConvertCellFlags(reader.ReadUInt32());
                short backWallId = reader.ReadInt16();
                short contentId = reader.ReadInt16();
                float contentHP = reader.ReadSingle();
                float water = reader.ReadSingle() * 1.6f; // not exact formula for water convertion, just an approximation
                byte light = (byte)(reader.ReadSingle() * 255f);
                float forceX = reader.ReadSingle();
                float forceY = reader.ReadSingle();

                if (contentId == 5210 && contentHP == 100f) { contentHP = 150f; }
                if (contentId == 5600 && contentHP == 100f) { contentHP = 150f; }
                if (contentId == 5220 && contentHP == 250f) { contentHP = 300f; }

                if (contentId == 1000) { // if grass
                    flags |= V1_11_Converter.Flag_CustomData0;
                }
                ushort newContentId = contentId != 2010 /*wood*/ ? (ushort)mapItemId[contentId] : (ushort)51 /*tree*/;

                worldDataWriter.Write(flags); // flags
                worldDataWriter.Write(newContentId);
                worldDataWriter.Write((ushort)contentHP);
                worldDataWriter.Write(water);
                worldDataWriter.Write((short)forceX);
                worldDataWriter.Write((short)forceY);
                worldDataWriter.Write(light);
                worldDataWriter.Write(light);
                worldDataWriter.Write(light);
            }
        }
        if (worldDataWriter.BaseStream.Position != gameState.worldData.Length) {
            throw new SaveLoadingException("Incomplete world data convertion");
        }
        if (reader.ReadString() != "OK") {
            throw new SaveLoadingException("Invalid magic string (error code: 2)");
        }

        var mainPlayer = new Data.Player();

        gameState.units = new Data.Unit[reader.ReadInt32()];
        for (int i = 0; i < gameState.units.Length; ++i) {
            short unitId = reader.ReadInt16();
            var unit = new Data.Unit() {
                codeName = mapUnitIdToCodeName[unitId],
                x = reader.ReadSingle(),
                y = reader.ReadSingle(),
                hp = reader.ReadSingle(),
                instanceId = (ushort)i,
                air = 1f
            };
            if (unitId == 0) { // if player
                mainPlayer.unitPlayerId = unit.instanceId;
            }
            gameState.units[i] = unit;
        }
        gameState.speciesKilled = new Data.SpeciesKillsInfo[reader.ReadInt32()];
        for (int i = 0; i < gameState.speciesKilled.Length; ++i) {
            gameState.speciesKilled[i].codeName = mapUnitIdToCodeName[reader.ReadInt16()];
            gameState.speciesKilled[i].nb = 1;
        }
        if (reader.ReadString() != "OK") {
            throw new SaveLoadingException("Invalid magic string (error code: 3)");
        }

        mainPlayer.inventory.items = new Data.Inventory.Item[reader.ReadInt32()];
        for (int i = 0; i < mainPlayer.inventory.items.Length; ++i) {
            mainPlayer.inventory.items[i] = new() {
                id = mapItemId[reader.ReadInt16()],
                nb = reader.ReadInt32(),
            };
        }
        mainPlayer.inventory.barItems = new ushort[reader.ReadInt32()];
        for (int i = 0; i < mainPlayer.inventory.barItems.Length; ++i) {
            mainPlayer.inventory.barItems[i] = mapItemId[reader.ReadInt16()];
        }
        int itemBarSelected = reader.ReadInt32();
        mainPlayer.inventory.itemSelected = itemBarSelected < 0 ? (ushort)0 : mainPlayer.inventory.barItems[itemBarSelected];

        gameState.pickups = new Data.Pickup[reader.ReadInt32()];
        for (int i = 0; i < gameState.pickups.Length; ++i) {
            gameState.pickups[i] = new() {
                id = mapItemId[reader.ReadInt16()],
                x = reader.ReadSingle(),
                y = reader.ReadSingle(),
                creationTime = reader.ReadSingle(),
            };
        }
        if (reader.ReadString() != "OK") {
            throw new SaveLoadingException("Invalid magic string (error code: 4)");
        }

        _ = reader.ReadInt32(); // shipAiStepId
        gameState.vars.m_nbNightsSurvived = reader.ReadInt32();
        gameState.vars.m_autoBuilderLevelBuilt = reader.ReadInt32();
        if (reader.ReadString() != "OK") {
            throw new SaveLoadingException("Invalid magic string (error code: 5)");
        }

        gameState.itemsInSave = itemsInSaveArray;
        gameState.players = new Data.Player[1] { mainPlayer };

        return gameState;
    }
}

public static class V1_11_Converter {
    public static readonly uint Flag_CustomData0 = 1U;

    public static int GetSaveOffset(int x) {
        uint hash = (uint)x * 0x9e3779b1U;
        hash = ((hash >> 15) ^ hash) * 0x85ebca77U;
        hash = ((hash >> 13) ^ hash) * 0xc2b2ae3dU;
        hash = (hash >> 16) ^ hash;

        ulong product = hash * 10UL;
        return (int)(product / 0xFFFFFFFFUL);
    }

    private static bool IsUnitMonster(string unitCodeName) {
        return unitCodeName is not ("player" or "playerLocal" or "defense" or "drone" or "droneCombat" or "droneWar");
    }

    public static byte[] Serialize(Data.GameState gameState) {
        using MemoryStream memoryStream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(memoryStream);

        writer.Write("SAVE FILE"); // magic string header
        writer.Write(gameState.version);
        writer.Write(gameState.versionBuild);
        writer.Write(gameState.modeName);

        writer.Write("Header"); // magic string

        ref readonly var @params = ref gameState.@params;
        writer.Write("m_difficulty"); BinFmtCodec.WriteInt(writer, @params.m_difficulty);
        writer.Write("m_startCheated"); BinFmtCodec.WriteBool(writer, @params.m_startCheated);
        writer.Write("m_eventsActive"); BinFmtCodec.WriteBool(writer, @params.m_eventsActive);
        writer.Write("m_spawnPos"); BinFmtCodec.WriteInt2(writer, @params.m_spawnPos);
        writer.Write("m_shipPos"); BinFmtCodec.WriteInt2(writer, @params.m_shipPos);
        writer.Write("m_gridSize"); BinFmtCodec.WriteInt2(writer, @params.m_gridSize);
        writer.Write("m_seed"); BinFmtCodec.WriteInt(writer, @params.m_seed);
        writer.Write("m_gameName"); BinFmtCodec.WriteString(writer, @params.m_gameName);
        writer.Write("m_visibility"); BinFmtCodec.WriteInt(writer, @params.m_visibility);
        writer.Write("m_passwordMD5"); BinFmtCodec.WriteString(writer, @params.m_passwordMD5);
        writer.Write("m_hostId"); BinFmtCodec.WriteULong(writer, @params.m_hostId);
        writer.Write("m_hostName"); BinFmtCodec.WriteString(writer, @params.m_hostName);
        writer.Write("m_gameOverIfAllDead"); BinFmtCodec.WriteBool(writer, @params.m_gameOverIfAllDead);
        writer.Write("m_nbPlayersMax"); BinFmtCodec.WriteInt(writer, @params.m_nbPlayersMax);
        writer.Write("m_clientGetHostItems"); BinFmtCodec.WriteBool(writer, @params.m_clientGetHostItems);
        writer.Write("m_banGiveLootToHost"); BinFmtCodec.WriteBool(writer, @params.m_banGiveLootToHost);
        writer.Write("m_devMode"); BinFmtCodec.WriteBool(writer, @params.m_devMode);
        writer.Write("m_checkMinerals"); BinFmtCodec.WriteBool(writer, @params.m_checkMinerals);
        writer.Write("m_dynamicSpawn"); BinFmtCodec.WriteBool(writer, @params.m_dynamicSpawn);
        writer.Write("m_cloudCycleDistance"); BinFmtCodec.WriteFloat(writer, @params.m_cloudCycleDistance);
        writer.Write("m_cloudCycleDuration"); BinFmtCodec.WriteFloat(writer, @params.m_cloudCycleDuration);
        writer.Write("m_cloudRadius"); BinFmtCodec.WriteFloat(writer, @params.m_cloudRadius);
        writer.Write("m_rainQuantity"); BinFmtCodec.WriteFloat(writer, @params.m_rainQuantity);
        writer.Write("m_generationOreDiv"); BinFmtCodec.WriteFloat(writer, @params.m_generationOreDiv);
        writer.Write("m_weightMult"); BinFmtCodec.WriteFloat(writer, @params.m_weightMult);
        writer.Write("m_dropChanceMult"); BinFmtCodec.WriteFloat(writer, @params.m_dropChanceMult);
        writer.Write("m_lavaPressureBottomCycle"); BinFmtCodec.WriteFloat(writer, @params.m_lavaPressureBottomCycle);
        writer.Write("m_lavaPressureTopCycle"); BinFmtCodec.WriteFloat(writer, @params.m_lavaPressureTopCycle);
        writer.Write("m_eruptionDurationTotal"); BinFmtCodec.WriteFloat(writer, @params.m_eruptionDurationTotal);
        writer.Write("m_eruptionDurationAcc"); BinFmtCodec.WriteFloat(writer, @params.m_eruptionDurationAcc);
        writer.Write("m_eruptionDurationUp"); BinFmtCodec.WriteFloat(writer, @params.m_eruptionDurationUp);
        writer.Write("m_eruptionPressure"); BinFmtCodec.WriteFloat(writer, @params.m_eruptionPressure);
        writer.Write("m_eruptionCheckMinY"); BinFmtCodec.WriteFloat(writer, @params.m_eruptionCheckMinY);
        writer.Write("m_dayDurationTotal"); BinFmtCodec.WriteFloat(writer, @params.m_dayDurationTotal);
        writer.Write("m_nightDuration"); BinFmtCodec.WriteFloat(writer, @params.m_nightDuration);
        writer.Write("m_gravityPlayers"); BinFmtCodec.WriteFloat(writer, @params.m_gravityPlayers);
        writer.Write("m_eventsDelayMin"); BinFmtCodec.WriteFloat(writer, @params.m_eventsDelayMin);
        writer.Write("m_eventsDelayMax"); BinFmtCodec.WriteFloat(writer, @params.m_eventsDelayMax);
        writer.Write("m_rocketPreparationDuration"); BinFmtCodec.WriteInt(writer, @params.m_rocketPreparationDuration);
        writer.Write("m_speedSimu"); BinFmtCodec.WriteFloat(writer, @params.m_speedSimu);
        writer.Write("m_speedSimuWorld"); BinFmtCodec.WriteFloat(writer, @params.m_speedSimuWorld);
        writer.Write("m_speedSimuWorldLocked"); BinFmtCodec.WriteBool(writer, @params.m_speedSimuWorldLocked);
        writer.Write("m_rainY"); BinFmtCodec.WriteInt(writer, @params.m_rainY);
        writer.Write("m_fastEvaporationYMax"); BinFmtCodec.WriteInt(writer, @params.m_fastEvaporationYMax);
        writer.Write("m_sunLightYMin"); BinFmtCodec.WriteInt(writer, @params.m_sunLightYMin);
        writer.Write("m_sunLightYMax"); BinFmtCodec.WriteInt(writer, @params.m_sunLightYMax);
        writer.Write("m_respawnDelay"); BinFmtCodec.WriteInt(writer, @params.m_respawnDelay);
        writer.Write("m_dropAtDeathPercent_Peaceful"); BinFmtCodec.WriteFloat(writer, @params.m_dropAtDeathPercent_Peaceful);
        writer.Write("m_dropAtDeathPercent_Easy"); BinFmtCodec.WriteFloat(writer, @params.m_dropAtDeathPercent_Easy);
        writer.Write("m_dropAtDeathPercent_Normal"); BinFmtCodec.WriteFloat(writer, @params.m_dropAtDeathPercent_Normal);
        writer.Write("m_dropAtDeathPercent_Hard"); BinFmtCodec.WriteFloat(writer, @params.m_dropAtDeathPercent_Hard);
        writer.Write("m_dropAtDeathPercent_Brutal"); BinFmtCodec.WriteFloat(writer, @params.m_dropAtDeathPercent_Brutal);
        writer.Write("m_dropAtDeathMax"); BinFmtCodec.WriteInt(writer, @params.m_dropAtDeathMax);
        writer.Write("m_monstersDayNb"); BinFmtCodec.WriteInt(writer, @params.m_monstersDayNb);
        writer.Write("m_monstersDayNbAddPerPlayer"); BinFmtCodec.WriteInt(writer, @params.m_monstersDayNbAddPerPlayer);
        writer.Write("m_bossRespawnDelay"); BinFmtCodec.WriteFloat(writer, @params.m_bossRespawnDelay);
        writer.Write("m_monstersNightSpawnRateMult"); BinFmtCodec.WriteFloat(writer, @params.m_monstersNightSpawnRateMult);
        writer.Write("m_monstersNightSpawnRateAddPerPlayer"); BinFmtCodec.WriteFloat(writer, @params.m_monstersNightSpawnRateAddPerPlayer);
        writer.Write("m_monstersHpMult"); BinFmtCodec.WriteFloat(writer, @params.m_monstersHpMult);
        writer.Write("m_monstersHpAddPerPlayer"); BinFmtCodec.WriteFloat(writer, @params.m_monstersHpAddPerPlayer);
        writer.Write("m_monstersDamagesMult"); BinFmtCodec.WriteFloat(writer, @params.m_monstersDamagesMult);
        writer.Write("m_monstersDamagesAddPerPlayer"); BinFmtCodec.WriteFloat(writer, @params.m_monstersDamagesAddPerPlayer);

        writer.Write(""); // denotes ending of serialized params
        writer.Write("Game Params Data"); // magic string

        writer.Write(gameState.itemsInSave.Length + 1);
        for (int i = 0; i < gameState.itemsInSave.Length; ++i) {
            writer.Write(gameState.itemsInSave[i]);
        }
        writer.Write(gameState.pickups.Length);
        foreach (Data.Pickup pickup in gameState.pickups) {
            writer.Write(pickup.id);
            writer.Write(pickup.x);
            writer.Write(pickup.y);
            writer.Write(pickup.creationTime);
        }
        writer.Write("Items Data"); // magic string

        writer.Write(gameState.players.Length);
        foreach (Data.Player player in gameState.players) {
            writer.Write(player.steamId);
            writer.Write(player.name);
            writer.Write(player.x);
            writer.Write(player.y);
            writer.Write(player.unitPlayerId);

            writer.Write(player.skinIsFemale);
            writer.Write(player.skinColorSkin);
            writer.Write(player.skinHairStyle);
            writer.Write(player.skinColorHairR);
            writer.Write(player.skinColorHairG);
            writer.Write(player.skinColorHairB);
            writer.Write(player.skinColorEyesR);
            writer.Write(player.skinColorEyesG);
            writer.Write(player.skinColorEyesB);

            writer.Write(player.inventory.items.Length);
            foreach (Data.Inventory.Item item in player.inventory.items) {
                writer.Write(item.id);
                writer.Write(item.nb);
            }
            writer.Write(player.inventory.barItems.Length);
            foreach (ushort barItemId in player.inventory.barItems) {
                writer.Write(barItemId);
            }
            writer.Write(player.inventory.itemSelected);

            writer.Write(player.itemVars.Length);
            foreach (Data.ItemVar? itemVar in player.itemVars) {
                writer.Write(itemVar.HasValue);
                if (itemVar.HasValue) {
                    writer.Write(itemVar.Value.timeLastUse);
                    writer.Write(itemVar.Value.timeActivation);
                    writer.Write(itemVar.Value.dico.Length);
                    foreach (KeyValuePair<string, float> pair in itemVar.Value.dico) {
                        writer.Write(pair.Key);
                        writer.Write(pair.Value);
                    }
                }
            }
        }
        writer.Write("Players"); // magic string

        writer.Write(gameState.lastEvents.Length);
        foreach (string lastEvent in gameState.lastEvents) {
            writer.Write(lastEvent);
        }
        writer.Write("Environments"); // magic string

        writer.Write(gameState.worldData);
        writer.Write("World Data"); // magic string

        writer.Write(gameState.units.Length);
        foreach (Data.Unit unit in gameState.units) {
            writer.Write(unit.codeName);
            writer.Write(unit.x);
            writer.Write(unit.y);
            writer.Write(unit.instanceId);
            writer.Write(unit.hp);
            writer.Write(unit.air);
            if (IsUnitMonster(unit.codeName)) {
                writer.Write(unit.isNightSpawn);
                writer.Write(unit.targetId);
                writer.Write(unit.isCreativeSpawn);
            }
        }
        writer.Write(gameState.speciesKilled.Length);
        foreach (Data.SpeciesKillsInfo speciesKillsInfo in gameState.speciesKilled) {
            writer.Write(speciesKillsInfo.codeName);
            writer.Write(speciesKillsInfo.nb);
            writer.Write(speciesKillsInfo.lastKillTime);
        }
        writer.Write("Units Data"); // magic string

        ref readonly var vars = ref gameState.vars;
        writer.Write("m_lastSaveDate"); BinFmtCodec.WriteString(writer, vars.m_lastSaveDate);
        writer.Write("m_simuTimeD"); BinFmtCodec.WriteDouble(writer, vars.m_simuTimeD);
        writer.Write("m_worldTimeD"); BinFmtCodec.WriteDouble(writer, vars.m_worldTimeD);
        writer.Write("m_clock"); BinFmtCodec.WriteFloat(writer, vars.m_clock);
        writer.Write("m_cloudPosRatio"); BinFmtCodec.WriteFloat(writer, vars.m_cloudPosRatio);
        writer.Write("m_droneTargetId"); BinFmtCodec.WriteUShort(writer, vars.m_droneTargetId);
        writer.Write("m_achievementsLocked"); BinFmtCodec.WriteBool(writer, vars.m_achievementsLocked);
        writer.Write("m_eventIdNum"); BinFmtCodec.WriteInt(writer, vars.m_eventIdNum);
        writer.Write("m_eventStartTime"); BinFmtCodec.WriteFloat(writer, vars.m_eventStartTime);
        writer.Write("m_lavaCycleSkipped"); BinFmtCodec.WriteBool(writer, vars.m_lavaCycleSkipped);
        writer.Write("m_bossAreas"); BinFmtCodec.WriteString(writer, vars.m_bossAreas);
        writer.Write("m_monsterT2AlreadyHit"); BinFmtCodec.WriteBool(writer, vars.m_monsterT2AlreadyHit);
        writer.Write("m_eruptionTime"); BinFmtCodec.WriteFloat(writer, vars.m_eruptionTime);
        writer.Write("m_eruptionStartPressure"); BinFmtCodec.WriteFloat(writer, vars.m_eruptionStartPressure);
        writer.Write("m_brokenHeart"); BinFmtCodec.WriteBool(writer, vars.m_brokenHeart);
        writer.Write("m_heartPos"); BinFmtCodec.WriteInt2(writer, vars.m_heartPos);
        writer.Write("m_cinematicIntroTime"); BinFmtCodec.WriteFloat(writer, vars.m_cinematicIntroTime);
        writer.Write("m_cinematicRocketPos"); BinFmtCodec.WriteVector2(writer, vars.m_cinematicRocketPos);
        writer.Write("m_cinematicRocketStep"); BinFmtCodec.WriteRocketStepEnum(writer, vars.m_cinematicRocketStep);
        writer.Write("m_cinematicRocketStepStartTime"); BinFmtCodec.WriteFloat(writer, vars.m_cinematicRocketStepStartTime);
        writer.Write("m_postGame"); BinFmtCodec.WriteBool(writer, vars.m_postGame);
        writer.Write("m_autoBuilderLastTimeFound"); BinFmtCodec.WriteFloat(writer, vars.m_autoBuilderLastTimeFound);
        writer.Write("m_achievNoElectricity"); BinFmtCodec.WriteBool(writer, vars.m_achievNoElectricity);
        writer.Write("m_achievNoShoot"); BinFmtCodec.WriteBool(writer, vars.m_achievNoShoot);
        writer.Write("m_achievNoCraft"); BinFmtCodec.WriteBool(writer, vars.m_achievNoCraft);
        writer.Write("m_achievNoMK2"); BinFmtCodec.WriteBool(writer, vars.m_achievNoMK2);
        writer.Write("m_achievWentToSea"); BinFmtCodec.WriteBool(writer, vars.m_achievWentToSea);
        writer.Write("m_achievEarlyDive"); BinFmtCodec.WriteBool(writer, vars.m_achievEarlyDive);
        writer.Write("m_aiSentencesTold"); BinFmtCodec.WriteStringList_v2050(writer, vars.m_aiSentencesTold);
        writer.Write("m_autoBuilderLevelBuilt"); BinFmtCodec.WriteInt(writer, vars.m_autoBuilderLevelBuilt);
        writer.Write("m_autoBuilderLevelUsed"); BinFmtCodec.WriteInt(writer, vars.m_autoBuilderLevelUsed);
        writer.Write("m_nbNightsSurvived"); BinFmtCodec.WriteInt(writer, vars.m_nbNightsSurvived);
        writer.Write("m_bossKilled_Madcrab"); BinFmtCodec.WriteBool(writer, vars.m_bossKilled_Madcrab);
        writer.Write("m_bossKilled_FireflyQueen"); BinFmtCodec.WriteBool(writer, vars.m_bossKilled_FireflyQueen);
        writer.Write("m_bossKilled_DwellerLord"); BinFmtCodec.WriteBool(writer, vars.m_bossKilled_DwellerLord);
        writer.Write("m_bossKilled_Balrog"); BinFmtCodec.WriteBool(writer, vars.m_bossKilled_Balrog);
        writer.Write("m_droneLastTimeEnters"); BinFmtCodec.WriteFloat(writer, vars.m_droneLastTimeEnters);
        writer.Write("m_droneLastTimeDontEnter"); BinFmtCodec.WriteFloat(writer, vars.m_droneLastTimeDontEnter);
        writer.Write("m_droneComboNb"); BinFmtCodec.WriteInt(writer, vars.m_droneComboNb);

        writer.Write(""); // denotes ending of vars section
        writer.Write("Vars Data"); // magic string

        return Utils.CLZF2.Compress(memoryStream.ToArray());
    }
}


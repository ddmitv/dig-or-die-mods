
using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Reflection;

namespace SaveTool;

// Version = 0.25 (build 301)

public class SaveLoadingException(string message) : Exception(message) { }

public class UnsafeDeserializationException(string assemblyName, string typeName)
    : Exception($"Unsafe deserialization from type '{typeName}', assembly: '{assemblyName}'") { }

public static class BinaryFormatterHelpers {
    private sealed class Vector2_SerializationSurrogate : ISerializationSurrogate {
        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context) {
            info.AddValue("x", ((Data.Vector2)obj).x);
            info.AddValue("y", ((Data.Vector2)obj).y);
        }
        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector) {
            return new Data.Vector2(info.GetSingle("x"), info.GetSingle("y"));
        }
    }
    private sealed class FormatterBinder : SerializationBinder {
        private static readonly HashSet<string> _allowedTypes = [
            "System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
            "System.Double, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
            "System.Single, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
            "System.Boolean, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
            "System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
            "System.Collections.Generic.List`1[[System.String, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
            "int2, Assembly-CSharp",
            "UnityEngine.Vector2, UnityEngine",
        ];

        public override Type? BindToType(string assemblyName, string typeName) {
            if (!_allowedTypes.Contains($"{typeName}, {assemblyName}")) {
                throw new UnsafeDeserializationException(assemblyName, typeName);
            }
            if (typeName == "int2" && assemblyName == "Assembly-CSharp") {
                return typeof(Data.int2);
            }
            if (typeName == "UnityEngine.Vector2" && assemblyName == "UnityEngine") {
                return typeof(Data.Vector2);
            }
            return null;
        }

        public override void BindToName(Type serializedType, out string? assemblyName, out string? typeName) {
            if (serializedType == typeof(Data.int2)) {
                assemblyName = "Assembly-CSharp";
                typeName = "int2";
            } else if (serializedType == typeof(Data.Vector2)) {
                assemblyName = "UnityEngine";
                typeName = "UnityEngine.Vector2";
            } else if (serializedType == typeof(Data.GlobalVars.RocketStep)) {
                assemblyName = "Assembly-CSharp";
                typeName = "GVars/RocketStep";
            } else {
                assemblyName = null;
                typeName = null;
            }
        }
    }
    public static BinaryFormatter GetBinaryFormatter() {
        var surrogateSelector = new SurrogateSelector();
        surrogateSelector.AddSurrogate(typeof(Data.Vector2),
            new StreamingContext(StreamingContextStates.All),
            new Vector2_SerializationSurrogate());

        return new BinaryFormatter() {
            SurrogateSelector = surrogateSelector,
            Binder = new FormatterBinder()
        };
    }
}

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
        BinaryFormatter formatter = BinaryFormatterHelpers.GetBinaryFormatter();
        int varsCount = reader.ReadInt32();
        for (int i = 0; i < varsCount; ++i) {
            string varName = reader.ReadString();
            string varType = reader.ReadString();
            object varValue = formatter.Deserialize(reader.BaseStream);

            // ignore these field because there's no equivalents for them (maybe m_cinematicRocketStep or m_cinematicRocketStepStartTime?)
            if (varName is "m_cinematicRocketActivationTime" or "m_cinematicRocketEnterTime" or "m_cinematicRocketLaunchTime") {
                continue;
            }
            if (varName is "m_difficulty" or "m_seed" or "m_shipPos") {
                FieldInfo field = typeof(Data.Params).GetField(varName);
                if (varType != field.FieldType.Name) {
                    throw new SaveLoadingException("Variables corrupted");
                }
                if (field is null) { continue; }
                field.SetValue(gameState.gameParams, varValue);
            } else {
                string fixedVarName = (varName == "monsterT2AlreadyHit" ? "m_monsterT2AlreadyHit" : varName);
                FieldInfo field = typeof(Data.GlobalVars).GetField(fixedVarName);
                if (field is null) { continue; }

                if (varType != field.FieldType.Name) {
                    throw new SaveLoadingException("Variables corrupted");
                }
                field.SetValue(gameState.globalVars, varValue);
            }
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

    private static int GetSaveOffset(int x) {
        uint hash = (uint)x * 0x9e3779b1U;
        hash = ((hash >> 15) ^ hash) * 0x85ebca77U;
        hash = ((hash >> 13) ^ hash) * 0xc2b2ae3dU;
        hash = (hash >> 16) ^ hash;

        ulong product = hash * 10UL;
        return (int)(product / 0xFFFFFFFFUL);
    }

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
        gameState.gameParams.m_difficulty = reader.ReadInt32();
        gameState.gameParams.m_seed = reader.ReadInt32();
        gameState.gameParams.m_shipPos = new Data.int2(reader.ReadInt32(), reader.ReadInt32());
        gameState.globalVars.m_simuTimeD = reader.ReadDouble();
        gameState.globalVars.m_clock = reader.ReadSingle();
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
            worldDataIdx += GetSaveOffset(i);
            for (int j = 0; j < 1024; ++j) {
                // convert bit flags in old to new format that are doing same function
                uint newFlags = ConvertCellFlags(reader.ReadUInt32());
                Utils.ByteHelpers.WriteAt(gameState.worldData, worldDataIdx, newFlags);

                // convert old item id to new ids
                ushort contentId = ConvertItemId((short)reader.ReadUInt16());
                Utils.ByteHelpers.WriteAt(gameState.worldData, worldDataIdx + 4, contentId);

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
        mainPlayer.inventory.itemSelected = mainPlayer.inventory.barItems[reader.ReadInt32()];

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
                gameState.globalVars.m_nbNightsSurvived = (int)gameVarValue;
            } else if (gameVarName == "AutoBuilderLevelBuilt") {
                gameState.globalVars.m_autoBuilderLevelBuilt = (int)gameVarValue;
            } else if (gameVarName == "ShipAiStepId") {
                // ignored
            } else if (gameVarName == "FirstMonsterHitT2") {
                gameState.globalVars.m_monsterT2AlreadyHit = gameVarValue == 1f;
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

public static class V1_11_Converter {
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

        BinaryFormatter formatter = BinaryFormatterHelpers.GetBinaryFormatter();
        foreach (FieldInfo field in typeof(Data.Params).GetFields()) {
            object value = field.GetValue(gameState.gameParams);

            writer.Write(field.Name);
            formatter.Serialize(writer.BaseStream, value);
        }
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

        foreach (FieldInfo field in typeof(Data.GlobalVars).GetFields()) {
            object value = field.GetValue(gameState.globalVars);

            writer.Write(field.Name);
            formatter.Serialize(writer.BaseStream, value);
        }
        writer.Write(""); // denotes ending of serialized global vars
        writer.Write("Vars Data"); // magic string

        return Utils.CLZF2.Compress(memoryStream.ToArray());
    }
}



using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace GameEngine;

#pragma warning disable SYSLIB0050, SYSLIB0011 // Type or member is obsolete

public static class SaveManager {
    public sealed class SaveLoadingException(string message) : Exception(message) { }
    public sealed class SaveSavingException(string message) : Exception(message) { }

    private sealed class Vector2_SerializationSurrogate : ISerializationSurrogate {
        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context) {
            info.AddValue("x", ((Vector2)obj).x);
            info.AddValue("y", ((Vector2)obj).y);
        }
        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector? selector) {
            return new Vector2(info.GetSingle("x"), info.GetSingle("y"));
        }
    }
    private sealed class FormatterBinder : SerializationBinder {
        public override Type? BindToType(string assemblyName, string typeName) {
            if (assemblyName == "Assembly-CSharp") {
                if (typeName == "int2") {
                    return typeof(int2);
                }
                if (typeName == "GVars+RocketStep") {
                    return typeof(GVars.RocketStep);
                }
                throw new NotSupportedException($"Unexpected type: \"{typeName}\" in assembly \"{assemblyName}\"");
            }
            if (assemblyName == "UnityEngine") {
                if (typeName == "UnityEngine.Vector2") {
                    return typeof(Vector2);
                }
                throw new NotSupportedException($"Unexpected type: \"{typeName}\" in assembly \"{assemblyName}\"");
            }
            return null;
        }
        public override void BindToName(Type serializedType, out string? assemblyName, out string? typeName) {
            if (serializedType == typeof(int2)) {
                assemblyName = "Assembly-CSharp";
                typeName = "int2";
            } else if (serializedType == typeof(Vector2)) {
                assemblyName = "UnityEngine";
                typeName = "UnityEngine.Vector2";
            } else if (serializedType == typeof(GVars.RocketStep)) {
                assemblyName = "Assembly-CSharp";
                typeName = "GVars+RocketStep";
            } else {
                assemblyName = null;
                typeName = null;
            }
        }
    }
    private static BinaryFormatter GetBinaryFormatter() {
        var surrogateSelector = new SurrogateSelector();
        surrogateSelector.AddSurrogate(typeof(Vector2),
            new StreamingContext(StreamingContextStates.All),
            new Vector2_SerializationSurrogate());

        return new BinaryFormatter() {
            SurrogateSelector = surrogateSelector,
            Binder = new FormatterBinder()
        };
    }
    private static int GetSaveOffset(int x) {
        uint hash = (uint)x * 0x9e3779b1U;
        hash = ((hash >> 15) ^ hash) * 0x85ebca77U;
        hash = ((hash >> 13) ^ hash) * 0xc2b2ae3dU;
        hash = (hash >> 16) ^ hash;

        ulong product = hash * 10UL;
        return (int)(product / 0xFFFFFFFFUL);
    }

    public static void Load(byte[] compressedData) {
        byte[]? rawData = CLZF2.Decompress(compressedData);
        compressedData = null!; // make it eligible for garbage collection
        if (rawData is null) {
            throw new SaveLoadingException("Failed to LZF decompress save file");
        }
        MemoryStream readerMemory = new(rawData);
        BinaryReader reader = new(readerMemory);

        if (reader.ReadString() != "SAVE FILE") {
            throw new SaveLoadingException("Invalid magic string (\"SAVE FILE\")");
        }
        float version = reader.ReadSingle();
        int build = reader.ReadInt32();
        if (build < 481) {
            throw new SaveLoadingException($"Save file version (v{version} build {build}) is not compatible with current version");
        }
        string modeName = reader.ReadString(); // unused
        if (reader.ReadString() != "Header") {
            throw new SaveLoadingException("Invalid magic string (\"Header\")");
        }

        BinaryFormatter formatter = GetBinaryFormatter();

        for (int i = 0; i < 10_000; ++i) {
            string fieldName = reader.ReadString();
            if (fieldName.Length == 0) { break; }

            object fieldValue = formatter.Deserialize(readerMemory);
            FieldInfo? fieldInfo = typeof(GParams).GetField(fieldName, BindingFlags.Static | BindingFlags.Public);
            fieldInfo?.SetValue(null, fieldValue);
        }
        if (reader.ReadString() != "Game Params Data") {
            throw new SaveLoadingException("Invalid magic string (\"Game Params Data\")");
        }
        ushort[] itemsIdInSave = new ushort[reader.ReadInt32()];
        for (int i = 1; i < itemsIdInSave.Length; ++i) {
            itemsIdInSave[i] = mapItemCodeNameToId[reader.ReadString()];
        }
        int pickupsCount = reader.ReadInt32();
        for (int i = 0; i < pickupsCount; ++i) {
            short itemId = reader.ReadInt16();
            PickupManager.pickups.Add(new CPickup(
                item: GItems.items[itemsIdInSave[itemId]]!,
                pos: new(reader.ReadSingle(), reader.ReadSingle())
            ) {
                m_creationTime = reader.ReadSingle()
            });
        }
        if (reader.ReadString() != "Items Data") {
            throw new SaveLoadingException("Invalid magic string (\"Items Data\")");
        }
        int playersCount = reader.ReadInt32();
        for (int i = 0; i < playersCount; ++i) {
            CPlayer player = new();
            player.m_steamId = reader.ReadUInt64();
            player.m_name = reader.ReadString();
            player.m_posSaved.x = reader.ReadSingle();
            player.m_posSaved.y = reader.ReadSingle();
            player.m_unitPlayerId = reader.ReadUInt16();
            if (build >= 459) {
                player.m_skinIsFemale = reader.ReadBoolean();
                player.m_skinColorSkin = reader.ReadSingle();
                player.m_skinHairStyle = reader.ReadInt32();
                player.m_skinColorHair = new(reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
                player.m_skinColorEyes = new(reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
            }
            int inventoryItemsCount = reader.ReadInt32();
            for (int j = 0; j < inventoryItemsCount; ++j) {
                ushort itemId = itemsIdInSave[reader.ReadUInt16()];
                int itemCount = reader.ReadInt32();
                player.m_inventory.m_items.Add(new CStack(GItems.items[itemId]!, itemCount));
            }
            player.m_inventory.SortInventory();
            int barItemsCount = reader.ReadInt32();
            for (int j = 0; j < barItemsCount; ++j) {
                ushort itemId = itemsIdInSave[reader.ReadUInt16()];
                player.m_inventory.m_barItems[j] = player.m_inventory.GetStack(itemId);
            }
            player.m_inventory.m_itemSelected = player.m_inventory.GetStack(reader.ReadUInt16());

            int itemVarsCount = reader.ReadInt32();
            for (int j = 0; j < itemVarsCount; ++j) {
                if (reader.ReadBoolean()) {
                    ushort itemId = itemsIdInSave[j];
                    CItemVars itemVars = player.GetItemVars(itemId);

                    itemVars.TimeLastUse = reader.ReadSingle();
                    itemVars.TimeActivation = reader.ReadSingle();
                    int dicoCount = reader.ReadInt32();
                    for (int k = 0; k < dicoCount; ++k) {
                        string dicoKey = reader.ReadString();
                        float dicoValue = reader.ReadSingle();
                        itemVars.Dico.Add(dicoKey, dicoValue);
                    }
                }
            }
            PlayerManager.players.Add(player);
        }
        if (reader.ReadString() != "Players") {
            throw new SaveLoadingException("Invalid magic string (\"Players\")");
        }
        if (build >= 815) {
            int eventsCount = reader.ReadInt32();
            for (int i = 0; i < eventsCount; ++i) {
                string eventNameId = reader.ReadString();
            }
            if (reader.ReadString() != "Environments") {
                throw new SaveLoadingException("Invalid magic string (\"Environments\")");
            }
        }
        (int gridSizeX, int gridSizeY) = World.Gs;
        for (int i = 0; i < gridSizeX; ++i) {
            readerMemory.Seek(GetSaveOffset(i), SeekOrigin.Current);
            for (int j = 0; j < gridSizeY; ++j) {
                ref CCell cell = ref World.Grid[i, j];
                cell.m_flags = reader.ReadUInt32();
                ushort itemId = reader.ReadUInt16();
                if (itemId >= itemsIdInSave.Length) {
                    throw new SaveLoadingException($"Invalid cell item id '{itemId}' at ({i}, {j}) (max item id={itemsIdInSave.Length})");
                }
                cell.m_contentId = itemsIdInSave[itemId];
                cell.m_contentHP = reader.ReadUInt16();
                cell.m_water = reader.ReadSingle();
                cell.m_forceX = reader.ReadInt16();
                cell.m_forceY = reader.ReadInt16();
                cell.m_light = new(reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
            }
        }
        if (reader.ReadString() != "World Data") {
            throw new SaveLoadingException("Invalid magic string (\"World Data\")");
        }
        Dictionary<string, CUnit.CDesc> mapCodeNameToUDesc = [];
        for (int i = 1; i < GUnits.udescs.Length; ++i) {
            mapCodeNameToUDesc.Add(GUnits.udescs[i]!.m_codeName, GUnits.udescs[i]!);
        }
        int unitsCount = reader.ReadInt32();
        for (int i = 0; i < unitsCount; ++i) {
            var udesc = mapCodeNameToUDesc[reader.ReadString()];
            Vector2 unitPos = new(reader.ReadSingle(), reader.ReadSingle());
            ushort unitInstanceId = reader.ReadUInt16();
            float unitHp = reader.ReadSingle();
            float unitAir = reader.ReadSingle();
            if (udesc is CUnitPlayer.CDesc) { continue; }

            CUnit unit = UnitManager.SpawnUnit(udesc, unitPos, unitInstanceId)
                ?? throw new SaveLoadingException("Invalid unit");
            unit.SetHp(Math.Max(unitHp, unit is CUnitPlayer ? 1 : -1));
            unit.m_air = unitAir;
            if (unit is CUnitMonster unitMonster) {
                unitMonster.m_isNightSpawn = reader.ReadBoolean();
                if (build >= 626) {
                    unitMonster.SetTargetFromNetwork(UnitManager.GetUnitById(reader.ReadUInt16()));
                }
                if (build >= 830) {
                    unitMonster.m_isCreativeSpawn = reader.ReadBoolean();
                }
            }
        }
        UnitManager.speciesKilled.Clear();
        int speciesKilledCount = reader.ReadInt32();
        for (int i = 0; i < speciesKilledCount; ++i) {
            UnitManager.speciesKilled.Add(new() {
                m_uDesc = mapCodeNameToUDesc[reader.ReadString()],
                m_nb = reader.ReadInt32(),
                m_lastKillTime = reader.ReadSingle()
            });
        }
        if (reader.ReadString() != "Units Data") {
            throw new SaveLoadingException("Invalid magic string (\"Units Data\")");
        }
        for (int i = 0; i < 10_000; ++i) {
            string fieldName = reader.ReadString();
            if (fieldName.Length == 0) { break; }

            object fieldValue = formatter.Deserialize(readerMemory);
            FieldInfo? fieldInfo = typeof(GVars).GetField(fieldName, BindingFlags.Static | BindingFlags.Public);
            fieldInfo?.SetValue(null, fieldValue);
        }
        if (reader.ReadString() != "Vars Data") {
            throw new SaveLoadingException("Invalid magic string (\"Vars Data\")");
        }
        if (readerMemory.Position < readerMemory.Length) {
            throw new SaveLoadingException("Non-zero bytes left to read");
        }
    }
    public static byte[] Save() {
        using MemoryStream memoryStream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(memoryStream);

        GVars.m_lastSaveDate = DateTime.Now.ToString();

        writer.Write("SAVE FILE"); // magic string header
        writer.Write(GameState.m_version);
        writer.Write(GameState.m_versionBuild);
        writer.Write(GameState.modeName);

        writer.Write("Header"); // magic string

        writer.Write(GParams.Serialize());
        writer.Write("Game Params Data"); // magic string

        writer.Write(GItems.items.Length);
        for (int i = 1; i < GItems.items.Length; ++i) {
            writer.Write(GItems.items[i]!.m_codeName);
        }
        writer.Write(PickupManager.pickups.GetCountActives());
        foreach (CPickup pickup in PickupManager.pickups) {
            if (!pickup.m_active) { continue; }
            writer.Write(pickup.m_item.m_id);
            writer.Write(pickup.m_pos.x);
            writer.Write(pickup.m_pos.y);
            writer.Write(pickup.m_creationTime);
        }
        writer.Write("Items Data"); // magic string

        foreach (CUnit unit in UnitManager.units) {
            if (unit is not CUnitPlayer unitPlayer) { continue; }
            if (PlayerManager.GetPlayerByUnit(unitPlayer) is CPlayer player) {
                player.m_posSaved = unitPlayer.m_pos;
            }
        }

        writer.Write(PlayerManager.players.Count);
        foreach (CPlayer player in PlayerManager.players) {
            writer.Write(player.m_steamId);
            writer.Write(player.m_name);
            writer.Write(player.m_posSaved.x);
            writer.Write(player.m_posSaved.y);
            writer.Write(player.m_unitPlayerId);

            writer.Write(player.m_skinIsFemale);
            writer.Write(player.m_skinColorSkin);
            writer.Write(player.m_skinHairStyle);
            writer.Write(player.m_skinColorHair.r);
            writer.Write(player.m_skinColorHair.g);
            writer.Write(player.m_skinColorHair.b);
            writer.Write(player.m_skinColorEyes.r);
            writer.Write(player.m_skinColorEyes.g);
            writer.Write(player.m_skinColorEyes.b);

            writer.Write(player.m_inventory.m_items.Count);
            foreach (CStack item in player.m_inventory.m_items) {
                writer.Write(item.m_item.m_id);
                writer.Write(item.m_nb);
            }
            writer.Write(player.m_inventory.m_barItems.Length);
            foreach (CStack? barItemId in player.m_inventory.m_barItems) {
                writer.Write(barItemId?.m_item.m_id ?? 0);
            }
            writer.Write(player.m_inventory.m_itemSelected?.m_item.m_id ?? 0);

            writer.Write(player.m_itemVars.Count);
            foreach (CItemVars? itemVar in player.m_itemVars) {
                writer.Write(itemVar is not null);
                if (itemVar is not null) {
                    writer.Write(itemVar.TimeLastUse);
                    writer.Write(itemVar.TimeActivation);
                    writer.Write(itemVar.Dico.Count);
                    foreach (KeyValuePair<string, float> pair in itemVar.Dico) {
                        writer.Write(pair.Key);
                        writer.Write(pair.Value);
                    }
                }
            }
        }
        writer.Write("Players"); // magic string

        // writer.Write(gameState.lastEvents.Length);
        // foreach (string lastEvent in gameState.lastEvents) {
        //     writer.Write(lastEvent);
        // }
        writer.Write(0);
        writer.Write("Environments"); // magic string

        for (int i = 0; i < World.Gs.x; ++i) {
            writer.Seek(GetSaveOffset(i), SeekOrigin.Current);
            for (int j = 0; j < World.Gs.y; ++j) {
                ref readonly CCell cell = ref World.Grid[i, j];
                writer.Write(cell.m_flags);
                writer.Write(cell.m_contentId);
                writer.Write(cell.m_contentHP);
                writer.Write(cell.m_water);
                writer.Write(cell.m_forceX);
                writer.Write(cell.m_forceY);
                writer.Write(cell.m_light.r);
                writer.Write(cell.m_light.g);
                writer.Write(cell.m_light.b);
            }
        }
        writer.Write("World Data"); // magic string

        writer.Write(UnitManager.units.Count);
        foreach (CUnit unit in UnitManager.units) {
            writer.Write(unit.UDesc.m_codeName);
            writer.Write(unit.m_pos.x);
            writer.Write(unit.m_pos.y);
            writer.Write(unit.m_id);
            writer.Write(unit.m_hp);
            writer.Write(unit.m_air);
            if (unit is CUnitMonster unitMonster) {
                writer.Write(unitMonster.m_isNightSpawn);
                writer.Write(unitMonster.Target?.m_id ?? ushort.MaxValue);
                writer.Write(unitMonster.m_isCreativeSpawn);
            }
        }
        writer.Write(UnitManager.speciesKilled.Count);
        foreach (var speciesKillsInfo in UnitManager.speciesKilled) {
            writer.Write(speciesKillsInfo.m_uDesc.m_codeName);
            writer.Write(speciesKillsInfo.m_nb);
            writer.Write(speciesKillsInfo.m_lastKillTime);
        }
        writer.Write("Units Data"); // magic string

        BinaryFormatter formatter = GetBinaryFormatter();
        foreach (FieldInfo field in typeof(GVars).GetFields(BindingFlags.Static | BindingFlags.Public)) {
            object value = field.GetValue(null) ?? throw new SaveSavingException($"Field in GVars is null");

            writer.Write(field.Name);
            formatter.Serialize(writer.BaseStream, value);
        }
        writer.Write(""); // denotes ending of serialized global vars
        writer.Write("Vars Data"); // magic string

        return CLZF2.Compress(memoryStream.ToArray());
    }

    private static readonly Dictionary<string, ushort> mapItemCodeNameToId = new() {
        { "miniaturizorMK1", 1 },
        { "miniaturizorMK2", 2 },
        { "miniaturizorMK3", 3 },
        { "miniaturizorMK4", 4 },
        { "miniaturizorMK5", 5 },
        { "miniaturizorUltimate", 6 },
        { "potionHp", 7 },
        { "potionHpRegen", 8 },
        { "potionHpBig", 9 },
        { "potionHpMega", 10 },
        { "potionArmor", 11 },
        { "potionPheromones", 12 },
        { "potionCritics", 13 },
        { "potionInvisibility", 14 },
        { "potionSpeed", 15 },
        { "armorMk1", 16 },
        { "armorMk2", 17 },
        { "armorMk3", 18 },
        { "armorUltimate", 19 },
        { "defenseShield", 20 },
        { "drone", 21 },
        { "droneCombat", 22 },
        { "droneWar", 23 },
        { "flashLight", 24 },
        { "minimapper", 25 },
        { "effeilGlasses", 26 },
        { "metalDetector", 27 },
        { "waterDetector", 28 },
        { "flashLightMK2", 29 },
        { "waterBreather", 30 },
        { "jetpack", 31 },
        { "invisibilityDevice", 32 },
        { "ultimateJetpack", 33 },
        { "ultimateBrush", 34 },
        { "ultimateRebreather", 35 },
        { "gunRifle", 36 },
        { "gunShotgun", 37 },
        { "gunMachineGun", 38 },
        { "gunSnipe", 39 },
        { "gunLaser", 40 },
        { "gunRocket", 41 },
        { "gunZF0", 42 },
        { "gunMegaSnipe", 43 },
        { "gunLaserGatling", 44 },
        { "gunStorm", 45 },
        { "gunGrenadeLaunch", 46 },
        { "gunParticlesShotgun", 47 },
        { "gunParticlesSniper", 48 },
        { "gunFlamethrower", 49 },
        { "gunLightSword", 50 },
        { "gunUltimateParticlesGatling", 51 },
        { "gunUltimateGrenadeLauncher", 52 },
        { "ultimateWaterPistol", 53 },
        { "ultimateLavaPistol", 54 },
        { "ultimateSpongePistol", 55 },
        { "ultimateTotoroGun", 56 },
        { "ultimateMonstersGun", 57 },
        { "turret360", 58 },
        { "turretGatling", 59 },
        { "turretReparator", 60 },
        { "turretHeavy", 61 },
        { "turretMine", 62 },
        { "turretSpikes", 63 },
        { "turretReparatorMK2", 64 },
        { "turretCeiling", 65 },
        { "turretLaser", 66 },
        { "turretTesla", 67 },
        { "turretFlame", 68 },
        { "turretParticles", 69 },
        { "explosive", 70 },
        { "wallWood", 71 },
        { "platform", 72 },
        { "wallConcrete", 73 },
        { "wallIronSupport", 74 },
        { "backwall", 75 },
        { "wallReinforced", 76 },
        { "wallDoor", 77 },
        { "platformSteel", 78 },
        { "generatorWater", 79 },
        { "waterPump", 80 },
        { "wallComposite", 81 },
        { "wallCompositeSupport", 82 },
        { "wallCompositeLight", 83 },
        { "wallCompositeDoor", 84 },
        { "wallUltimate", 85 },
        { "autoBuilderMK1", 86 },
        { "autoBuilderMK2", 87 },
        { "light", 88 },
        { "autoBuilderMK3", 89 },
        { "lightSticky", 90 },
        { "electricWire", 91 },
        { "generatorSun", 92 },
        { "lightSun", 93 },
        { "teleport", 94 },
        { "elecSwitch", 95 },
        { "autoBuilderMK4", 96 },
        { "autoBuilderMK5", 97 },
        { "elecSwitchPush", 98 },
        { "elecSwitchRelay", 99 },
        { "elecCross", 100 },
        { "elecSignal", 101 },
        { "elecClock", 102 },
        { "elecToggle", 103 },
        { "elecDelay", 104 },
        { "elecWaterSensor", 105 },
        { "elecProximitySensor", 106 },
        { "elecDistanceSensor", 107 },
        { "elecAND", 108 },
        { "elecOR", 109 },
        { "elecXOR", 110 },
        { "elecNOT", 111 },
        { "elecLight", 112 },
        { "elecAlarm", 113 },
        { "reactor", 114 },
        { "rocketTop", 115 },
        { "rocketTank", 116 },
        { "rocketEngine", 117 },
        { "autoBuilderUltimate", 118 },
        { "dirt", 119 },
        { "dirtRed", 120 },
        { "silt", 121 },
        { "dirtBlack", 122 },
        { "dirtSky", 123 },
        { "rock", 124 },
        { "iron", 125 },
        { "coal", 126 },
        { "copper", 127 },
        { "gold", 128 },
        { "aluminium", 129 },
        { "rockFlying", 130 },
        { "rockGaz", 131 },
        { "crystal", 132 },
        { "crystalLight", 133 },
        { "crystalBlack", 134 },
        { "granit", 135 },
        { "uranium", 136 },
        { "titanium", 137 },
        { "lightonium", 138 },
        { "thorium", 139 },
        { "sulfur", 140 },
        { "sapphire", 141 },
        { "organicRock", 142 },
        { "organicRockHeart", 143 },
        { "lava", 144 },
        { "lavaOld", 145 },
        { "diamonds", 146 },
        { "organicRockDefenseLead", 147 },
        { "organicRockDefense", 148 },
        { "wood", 149 },
        { "woodwater", 150 },
        { "woodSky", 151 },
        { "woodGranit", 152 },
        { "deadPlant", 153 },
        { "tree", 154 },
        { "treePine", 155 },
        { "treeWater", 156 },
        { "treeSky", 157 },
        { "treeGranit", 158 },
        { "bush", 159 },
        { "flowerBlue", 160 },
        { "flowerWhite", 161 },
        { "fernRed", 162 },
        { "waterBush", 163 },
        { "waterLight", 164 },
        { "waterCoral", 165 },
        { "blackGrass", 166 },
        { "blackMushroom", 167 },
        { "skyBush", 168 },
        { "lavaFlower", 169 },
        { "lavaPlant", 170 },
        { "bushGranit", 171 },
        { "organicHair", 172 },
        { "metalScrap", 173 },
        { "lightGem", 174 },
        { "energyGem", 175 },
        { "darkGem", 176 },
        { "dogHorn", 177 },
        { "dogHorn3", 178 },
        { "moleShell", 179 },
        { "moleShellBlack", 180 },
        { "fish2Regen", 181 },
        { "fish3Regen", 182 },
        { "bat2Sonar", 183 },
        { "bat3Sonar", 184 },
        { "antShell", 185 },
        { "sharkSkin", 186 },
        { "unstableGemResidue", 187 },
        { "lootDwellerLord", 188 },
        { "lootParticleGround", 189 },
        { "lootParticleBirds", 190 },
        { "lootLargeParticleBirds", 191 },
        { "lootLavaSpider", 192 },
        { "lootLavaBat", 193 },
        { "lootMiniBalrog", 194 },
        { "bloodyFlesh1", 195 },
        { "bloodyFlesh2", 196 },
        { "bloodyFlesh3", 197 },
        { "bossMadCrabSonar", 198 },
        { "bossMadCrabMaterial", 199 },
        { "masterGem", 200 },
        { "lootBalrog", 201 },
    };

    private static readonly HashSet<string> nonMonsterUnitCodeNames = new() {
        "player", "playerLocal", "defense", "drone", "droneCombat", "droneWar"
    };
}

#pragma warning restore SYSLIB0050, SYSLIB0011 // Type or member is obsolete

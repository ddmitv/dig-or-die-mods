
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Reflection;
using System.Runtime.Remoting.Messaging;

namespace SaveTool;

// Version = 0.25 (build 301)

public class SaveLoadingException(string message) : Exception(message) { }

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
        public override Type? BindToType(string assemblyName, string typeName) {
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

        gameState.itemsInSave = new string[reader.ReadInt32()];
        for (int i = 1; i < gameState.itemsInSave.Length; ++i) {
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
        // latest: stores item instanceId of selected item slot
        // FIX (also need convert save item instanceId -> game item instanceId)
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

        writer.Write(gameState.itemsInSave.Length);
        for (int i = 1; i < gameState.itemsInSave.Length; ++i) {
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


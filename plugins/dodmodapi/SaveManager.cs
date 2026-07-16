
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using DODModAPI.Extensions;
using HarmonyLib;

namespace DODModAPI;

public interface IModSaveHandler {
    public enum SaveResult {
        Continue,
        Skip,
    }

    string ModId { get; }
    uint CurrentVersion { get; }

    SaveResult Save(BinaryWriter writer);
    void Load(BinaryReader reader, uint savedVersion);
}

public static class SaveManager {
    private static readonly Dictionary<string, IModSaveHandler> _handlers = new();

    private const ulong DODModAPIMagic = 0x444F444D6F644150; // "DODModAP"

    public static void Register(IModSaveHandler handler) {
        if (handler is null) { throw new ArgumentNullException(nameof(handler)); }

        string modSaveId = handler.ModId;

        if (string.IsNullOrEmpty(modSaveId)) { throw new ArgumentException("ModId required", nameof(handler)); }
        if (_handlers.ContainsKey(modSaveId)) { throw new ArgumentException($"Duplicate mod save ID: {modSaveId}"); }

        _handlers.Add(modSaveId, handler);
        // DODModAPIPlugin.Log.LogInfo()
    }

    private static void WriteModSaveData(BinaryWriter writer) {
        if (writer.BaseStream is not MemoryStream ms) {
            DODModAPIPlugin.Log.LogError($"Base stream of BinaryWriter is not MemoryStream (it is {writer.BaseStream.GetType()}), unable to save mods custom data in the save file");
            return;
        }

        writer.Write(DODModAPIMagic);

        long modDataCountPos = ms.Position;
        uint modDataCount = 0;
        writer.Write(default(uint)); // placeholder for mod data count field

        foreach (var kvp in _handlers) {
            long startPos = ms.Position;

            string modId = kvp.Key;
            writer.Write(modId);
            writer.Write(kvp.Value.CurrentVersion);

            long blobLengthPos = ms.Position;
            writer.Write(default(uint)); // placeholder for mod data blob length

            try {
                // actually serialize custom mod data
                var result = kvp.Value.Save(writer);

                if (result == IModSaveHandler.SaveResult.Skip) {
                    DODModAPIPlugin.Log.LogInfo($"Saved data for mod ID \"{modId}\" (skipped)");
                    // rollback
                    ms.Position = startPos;
                    ms.SetLength(startPos);
                    continue;
                }

                var currentPos = ms.Position;
                if (currentPos < blobLengthPos + sizeof(uint)) {
                    goto abortSaving;
                }

                uint blobLength = (uint)(currentPos - (blobLengthPos + sizeof(uint)));
                DODModAPIPlugin.Log.LogInfo($"Saved data for mod ID \"{modId}\" ({blobLength} bytes)");

                ms.Position = blobLengthPos;
                writer.Write(blobLength); // write to placeholder
                ms.Position = currentPos;

                modDataCount += 1;
            } catch (Exception e) {
                DODModAPIPlugin.Log.LogError($"Saving error for mod with ID \"{modId}\": {e.Message}");

                if (!ms.CanWrite) {
                    throw new InvalidOperationException($"Mod with ID \"{modId}\" closed the save stream. Save file is likely corrupted.");
                }
                // rollback
                ms.Position = startPos;
                ms.SetLength(startPos);
            }
            continue;
abortSaving:
            throw new InvalidOperationException($"Save saving handle for mod ID \"{modId}\" is incorrectly implemented: seeked backwards during save, corrupting data structure. Save file is likely corrupted");
        }
        ms.Position = modDataCountPos;
        writer.Write(modDataCount); // write to placeholder

        // other mods may add some data after, so we should put pos to the end
        writer.Seek(0, SeekOrigin.End);

        DODModAPIPlugin.Log.LogInfo($"Wrote mod save data ({modDataCount} custom mod data handlers)");
    }

    private static bool ReadModSaveData(BinaryReader reader) {
        if (reader.BaseStream is not MemoryStream ms) {
            LogErrorAndSetErr($"Base stream of BinaryReader is not MemoryStream (it is {reader.BaseStream.GetType()}), unable to read mods custom data in the save file");
            return false;
        }

        long remaining = reader.BaseStream.Length - reader.BaseStream.Position;
        if (remaining < sizeof(ulong) + sizeof(uint)) {
            // the save probably doesn't contains custom mod data, skipping mod data reading
            return true;
        }
        ulong magic = reader.ReadUInt64();
        if (magic != DODModAPIMagic) {
            LogErrorAndSetErr($"Bad magic: corrupted or no mod data in save file (expected: {DODModAPIMagic:X16}, got: {magic:X16})");
            return false;
        }
        uint modDataCount = reader.ReadUInt32();
        try {
            for (uint i = 0; i < modDataCount; ++i) {
                // read header
                string modId = reader.ReadString();
                uint savedModVersion = reader.ReadUInt32();
                uint len = reader.ReadUInt32();
                long endPos = ms.Position + len;

                if (endPos > ms.Length) {
                    LogErrorAndSetErr($"DODModAPI internal save structure is corrupted: length of mod data blob ({len}) is out of bounds (around mod ID \"{modId}\")");
                    return false;
                }

                if (!_handlers.TryGetValue(modId, out IModSaveHandler handler)) {
                    DODModAPIPlugin.Log.LogWarning($"Unknown mod data skipped: \"{modId}\" ({len} bytes)");
                    ms.Position = endPos;
                    continue;
                }
                try {
                    // actually deserialize custom mod data
                    handler.Load(reader, savedModVersion);

                    if (ms.Position != endPos) {
                        DODModAPIPlugin.Log.LogError($"Save load handler for mod ID \"{modId}\" is incorrectly implemented: leaving unread data or reading past the end of the block (expected end pos: {endPos}, got: {ms.Position})");
                        ms.Position = endPos;
                    }

                    DODModAPIPlugin.Log.LogInfo($"Loaded data for mod ID \"{modId}\", version {savedModVersion} ({len} bytes)");
                } catch (Exception e) {
                    DODModAPIPlugin.Log.LogError($"Loading error for mod with ID \"{modId}\": {e.Message}");
                    // skip
                    ms.Position = endPos;
                }
            }
        } catch (EndOfStreamException e) {
            DODModAPIPlugin.Log.LogError($"Corrupted save file: Unexpected end of stream while reading mod data headers: {e.Message}");
        } catch (FormatException e) {
            DODModAPIPlugin.Log.LogError($"Corrupted save file: Malformed data structure detected in mod data headers: {e.Message}");
        } catch (IOException e) {
            DODModAPIPlugin.Log.LogError($"Corrupted save file: IO error while reading mod data headers: {e.Message}");
        } catch (OutOfMemoryException e) {
            DODModAPIPlugin.Log.LogError($"Corrupted save file: Malicious or corrupted string length detected in mod data headers: {e.Message}");
        }

        DODModAPIPlugin.Log.LogInfo($"Read mod save data ({modDataCount} custom mod data handlers)");
        return true;

        static void LogErrorAndSetErr(string msg) {
            DODModAPIPlugin.Log.LogError(msg);
            SDataSave.Inst.LoadingError = msg;
        }
    }


    internal static class Patches {
        [HarmonyPatch(typeof(SDataSave), nameof(SDataSave.Save))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> SDataSave_Save(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
            return new CodeCursor(instructions, generator)
                .MoveToEnd()
                .FindPreviousEnd(
                    new(OpCodes.Ldloc_3),
                    new(OpCodes.Ldstr, "Vars Data"),
                    new(OpCodes.Callvirt, typeof(System.IO.BinaryWriter).Method<string>("Write"))
                    // insert here
                )
                .Insert(
                    new(OpCodes.Ldloc_3),
                    Transpilers.EmitDelegate(WriteModSaveData))
                .Finish();
        }
        [HarmonyPatch(typeof(SDataSave), nameof(SDataSave.LoadData), MethodType.Enumerator)]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> SDataSave_LoadData(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
            return new CodeCursor(instructions, generator)
                .MoveToEnd()
                .FindPrevious(
                    new(OpCodes.Ldc_I4_0),
                    new(OpCodes.Ret))
                .CreateLabel(offset: 0, out var yieldBreakLabel)
                .FindPreviousEnd(
                    new(OpCodes.Ldstr, "Vars Data"),
                    new(OpCodes.Call, typeof(SDataSave).Method<BinaryReader, string>("Check")),
                    new(OpCodes.Brtrue),
                    new(OpCodes.Br)
                    // inject here
                )
                .Inject(
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Ldfld, typeof(SDataSave).CoroutineField("<LoadData>c__Iterator0", "<r>__4")), // load BinaryReader
                    Transpilers.EmitDelegate(ReadModSaveData),
                    new(OpCodes.Brfalse, yieldBreakLabel))
                .Finish();
        }
    }
}

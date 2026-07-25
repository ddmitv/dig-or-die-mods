
using DODModAPI.Extensions;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using UnityEngine;

namespace DODModAPI;

public class CommandException : Exception {
    public int? ArgIndex { get; } = null;

    public CommandException(string message, int argIndex) : base(message) {
        ArgIndex = argIndex;
    }
    public CommandException(string message) : base(message) { }
}

public static class CommandManager {
    private static readonly Dictionary<string, CommandEntry> _commands = new();
    private static readonly List<ChatPreprocessorEntry> _chatPreprocessors = new();

    private static Regex? _splitCmdArgsRegex = null;

    public delegate void CommandAction(CommandArgs args);
    public delegate List<string>? CommandCompletions(int argIndex);
    public delegate bool ChatPreprocessor(ref string text);

    private struct CommandEntry {
        public CommandAction action;
        public bool isLocal;
        public CommandCompletions? tabCompleter;
        public bool disableAchievements;
    }
    private struct ChatPreprocessorEntry {
        public ChatPreprocessor fn;
        public int priority;
    }

    public struct CommandOptions {
        public bool Local { get; set; } = false;
        public CommandCompletions? TabCompleter { get; set; } = null;
        public bool Overwrite { get; set; } = false;
        public bool DisableAchievements { get; set; } = true;
        // public string HelpMessage { get; set; } = null;

        public CommandOptions() {}
    }

    public static void Register(string name, CommandOptions cmdOpts, CommandAction fn) {
        if (name is null) {
            throw new ArgumentNullException(nameof(name));
        }
        if (!name.StartsWith("/")) {
            throw new ArgumentException($"Command name \"{name}\" must start with '/'", nameof(name));
        }
        if (_commands.ContainsKey(name) && !cmdOpts.Overwrite) {
            throw new ArgumentException($"Command \"{name}\" is already registered", nameof(name));
        }
        _commands[name] = new CommandEntry {
            action = fn,
            isLocal = cmdOpts.Local,
            tabCompleter = cmdOpts.TabCompleter,
            disableAchievements = cmdOpts.DisableAchievements,
        };
    }

    public static void RegisterChatPreprocessor(int priority, ChatPreprocessor preprocessor) {
        if (preprocessor is null) {
            throw new ArgumentNullException(nameof(preprocessor));
        }
        var entry = new ChatPreprocessorEntry {
            fn = preprocessor, priority = priority
        };
        Misc.InsertSortedList(_chatPreprocessors, entry, (a, b) => b.priority.CompareTo(a.priority) /* reverse order */);
    }

    private static string[] SplitCommandArgs(string text) {
        _splitCmdArgsRegex ??= new Regex(@"[\""].+?[\""]|[^ ]+");
        return _splitCmdArgsRegex.Matches(text)
            .Cast<Match>()
            .Select(m => m.Value.Trim('"'))
            .ToArray();
    }

    private static string EscapeArg(string arg) {
        if (arg.Length == 0 || arg.Any(char.IsWhiteSpace)) {
            return $"\"{arg}\"";
        }
        return arg;
    }

    private static string TabOnList(string[] commandAndArgs, int argIndex, List<string> argList) {
        static string CompleteArg(string arg, List<string> argList) {
            int currIndex = argList.IndexOf(arg);
            if (currIndex >= 0) {
                int step = SInputs.GetKeyShift() ? -1 : 1;
                currIndex = Misc.PosMod(currIndex + step, argList.Count);
                return argList[currIndex];
            }
            int foundIndex = argList.FindIndex(x => x.StartsWith(arg, StringComparison.OrdinalIgnoreCase));
            if (foundIndex < 0) {
                foundIndex = argList.FindIndex(x => x.IndexOf(arg, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            if (foundIndex < 0) {
                return arg;
            }
            return argList[foundIndex];
        }
        if (argIndex >= commandAndArgs.Length) {
            Misc.ArrayAppend(ref commandAndArgs, argList[0]);
        } else {
            commandAndArgs[argIndex] = CompleteArg(commandAndArgs[argIndex], argList);
        }
        return Misc.StringJoin(commandAndArgs, (x, i) => i == 0 ? x : EscapeArg(x), " ");
    }

    [HarmonyPatch]
    internal static class Patches {
        [HarmonyPatch(typeof(SNetworkCommands), nameof(SNetworkCommands.ProcessCommand))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> SNetworkCommands_ProcessCommand(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
            return new CodeCursor(instructions, generator)
                .MoveToEnd()
                //     this.DrawHelp_IfSenderIsMe(string.Empty, playerSender, true);
                //     ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                //     return; (implicit)
                //     ^^^^^^
                .FindPrevious(
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Ldsfld, typeof(string).StaticField("Empty")),
                    new(OpCodes.Ldarg_2),
                    new(OpCodes.Ldc_I4_1),
                    new(OpCodes.Call, typeof(SNetworkCommands).Method(nameof(SNetworkCommands.DrawHelp_IfSenderIsMe))),
                    new(OpCodes.Ret))
                .CreateLabel(offset: 5, out Label onSuccessLabel)
                //     if (!ExecModCommand(input, playerSender)) {
                //     +++++++++++++++++++++++++++++++++++++++++
                //         this.DrawHelp_IfSenderIsMe(string.Empty, playerSender, true);
                //     }
                //     return; (implicit)
                .Inject(
                    new(OpCodes.Ldarg_1),
                    new(OpCodes.Ldarg_2),
                    Transpilers.EmitDelegate(ExecModCommand),
                    new(OpCodes.Brtrue, onSuccessLabel))
                .Finish();

            static bool ExecModCommand(string text, CPlayer playerSender) {
                string[] commandAndArgs = SplitCommandArgs(text);
                if (commandAndArgs.Length == 0) {
                    return false;
                }
                string command = commandAndArgs[0];
                if (!_commands.TryGetValue(command, out CommandEntry cmdEntry)) {
                    return false;
                }
                string[] args = commandAndArgs.Skip(1).ToArray();

                try {
                    cmdEntry.action(new CommandArgs(args, playerSender));
                    if (cmdEntry.disableAchievements && !GVars.m_achievementsLocked) {
                        GVars.m_achievementsLocked = true;
                        Misc.SendChatMessageLocal("Achievements have been deactivated in this game");
                    }
                } catch (CommandException ex) {
                    if (!playerSender.IsMe()) { return true; }

                    string errorMessage = ex.ArgIndex switch {
                        null => $"{command}: {ex.Message}",
                        int argIndex => $"{command}: {ex.Message} (argument #{argIndex})",
                    };
                    SSingletonScreen<SScreenHudChat>.Inst.AddChatMessage_Local(null, errorMessage, false);
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(SNetworkCommands), nameof(SNetworkCommands.TabCommand))]
        [HarmonyPrefix]
        private static bool SNetworkCommands_TabCommand(string input, ref string __result) {
            string[] commandAndArgs = SplitCommandArgs(input);
            if (commandAndArgs.Length == 0) {
                return true;
            }

            string command = commandAndArgs[0];
            if (!_commands.TryGetValue(command, out CommandEntry cmdInfo) || cmdInfo.tabCompleter is null) {
                return true;
            }
            int argIndex = Math.Max(0, commandAndArgs.Length + (char.IsWhiteSpace(input[input.Length - 1]) ? 1 : 0) - 2);

            List<string>? completions = cmdInfo.tabCompleter(argIndex);
            if (completions is null || completions.Count == 0) {
                __result = input;
                return false;
            }
            __result = TabOnList(commandAndArgs, argIndex + 1, completions);
            return false;
        }

        [HarmonyPatch(typeof(SScreenHudChat), nameof(SScreenHudChat.AddChatMessage_Networked))]
        [HarmonyPrefix]
        private static void SScreenHudChat_AddChatMessage_Networked(string str, ref ulong steamIdRemote) {
            string[] commandAndArgs = SplitCommandArgs(str);
            if (commandAndArgs.Length == 0) { return; }
            if (!_commands.TryGetValue(commandAndArgs[0], out CommandEntry cmdInfo)) {
                return;
            }
            if (cmdInfo.isLocal) {
                // if steamIdRemote != SNetwork.MySteamID it sends SMessageChat
                steamIdRemote = SNetwork.MySteamID;
            }
        }

        [HarmonyPatch(typeof(SScreenHudChat), nameof(SScreenHudChat.OnUpdate))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> SScreenHudChat_OnUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
            return new CodeCursor(instructions, generator)
                //     if (this.m_inputChat.m_text.StartsWith("/") && SNetwork.GetNbPlayersConnected() <= 1)
                //     ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                .FindNext(
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Ldfld, typeof(SScreenHudChat).Field(nameof(SScreenHudChat.m_inputChat))),
                    new(OpCodes.Ldfld, typeof(CGuiInput).Field(nameof(CGuiInput.m_text))),
                    new(OpCodes.Ldstr, "/"),
                    new(OpCodes.Callvirt, typeof(string).Method<string>(nameof(string.StartsWith))),
                    new(OpCodes.Brfalse))
                .AssertInstruction(offset: -1, new(OpCodes.Brfalse))
                .GetOperand(offset: -1, out Label skipMessageLabel)
                //     if (PreprocessChatMessage() && this.m_inputChat.m_text.StartsWith("/") && SNetwork.GetNbPlayersConnected() <= 1)
                //         ++++++++++++++++++++++++++
                .Insert(
                    Transpilers.EmitDelegate(PreprocessChatMessage),
                    new(OpCodes.Brfalse, skipMessageLabel))
                .Finish();

            static bool PreprocessChatMessage() {
                ref string text = ref SScreenHudChat.Inst.m_inputChat.m_text;

                foreach (var preprocessor in _chatPreprocessors) {
                    if (!preprocessor.fn(ref text)) {
                        return false;
                    }
                }
                return true;
            }
        }
    }
}

public struct CommandArgs {
    private readonly string[] _args;
    private readonly CPlayer _sender;

    public readonly string[] ArgArray => _args;
    public readonly CPlayer PlayerSender => _sender;
    public int Index { readonly get; set; } = 0;

    public readonly bool HasNext => Index < _args.Length;
    public readonly int Remaining => _args.Length - Index;

    public CommandArgs(string[] args, CPlayer sender) {
        _args = args;
        _sender = sender;
    }

    public readonly void ArgNone() {
        if (HasNext) {
            throw new CommandException(Index == 0 ? "None arguments are expected" : "No more arguments are expected", Index + 1);
        }
    }

    public string ArgString(string argName = "string") {
        if (!HasNext) {
            throw new CommandException($"Expected {argName}", Index + 1);
        }
        return _args[Index++];
    }

    public int ArgInt(string argName = "integer") {
        if (!HasNext) {
            throw new CommandException($"Expected {argName}", Index + 1);
        }
        if (!int.TryParse(_args[Index], NumberStyles.Integer, CultureInfo.InvariantCulture, out int result)) {
            throw new CommandException($"Invalid {argName}", Index + 1);
        }
        Index += 1;
        return result;
    }
    public float ArgFloat(string argName = "float") {
        if (!HasNext) {
            throw new CommandException($"Expected {argName}", Index + 1);
        }
        if (!float.TryParse(_args[Index], NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float result)) {
            throw new CommandException($"Invalid {argName}", Index + 1);
        }
        Index += 1;
        return result;
    }

    // currently only supports "~" player relative coords. it would be nice to also have "^" - player's cursor relative coords
    // but it's untrivial to implement: you would need to send player's mouse coords to all connected clients
    // in order for them to calculate target position and execute the command
    // (since the command sync is implemented by all the clients executing the exactly same command)

    public int2 ArgCellPos(string argName = "cell position") {
        var basePos = PlayerSender?.m_unitPlayer?.PosCell ?? new int2(0, 0);

        int x = ArgRelativeCellCoord(basePos.x, $"{argName} (X coordinate)");
        if (x < 0 || x >= SWorld.Gs.x) {
            throw new CommandException($"{argName} X coordinate is out of the world: {x}", Index);
        }
        int y = ArgRelativeCellCoord(basePos.y, $"{argName} (Y coordinate)");
        if (y < 0 || y >= SWorld.Gs.y) {
            throw new CommandException($"{argName} Y coordinate is out of the world: {y}", Index);
        }
        return new int2(x, y);
    }
    public Vector2 ArgWorldPos(string argName = "world position") {
        var basePos = PlayerSender?.m_unitPlayer?.m_pos ?? new Vector2(0, 0);

        float x = ArgRelativeWorldCoord(basePos.x, $"{argName} (X coordinate)");
        if (x < SWorld.GridRectM2.x || x >= SWorld.GridRectM2.xMax) {
            throw new CommandException($"{argName} X coordinate is out of the world: {x}", Index);
        }
        float y = ArgRelativeWorldCoord(basePos.y, $"{argName} (Y coordinate)");
        if (y < SWorld.GridRectM2.y || y >= SWorld.GridRectM2.yMax) {
            throw new CommandException($"{argName} Y coordinate is out of the world: {y}", Index);
        }
        return new Vector2(x, y);
    }

    public CPlayer ArgPlayer(string argName = "player") {
        string playerName = ArgString(argName);
        CPlayer player = SNetwork.Players.FirstOrDefault(player => player.m_name == playerName);
        if (player is null) {
            var closestPlayerName = Misc.ClosestStringMatch(playerName, SNetwork.Players.Select(x => x.m_name));
            throw new CommandException($"Unknown {argName} name; did you mean \"{closestPlayerName}\"?", Index);
        }
        return player;
    }

    public CItem ArgItem(string argName = "item") {
        string codeNameOrId = ArgString(argName);
        if (codeNameOrId.StartsWith("#")) {
            if (!ushort.TryParse(codeNameOrId.Substring(1), out ushort itemId)) {
                throw new CommandException($"Invalid {argName} ID", Index);
            }
            if (itemId >= GItems.Items.Count) {
                throw new CommandException($"{argName} ID is out of range", Index);
            }
            return GItems.Items[itemId];
        }
        var item = GItems.Items.Skip(1).FirstOrDefault(x => x.m_codeName == codeNameOrId);
        if (item is null) {
            var closestCodeName = Misc.ClosestStringMatch(codeNameOrId,
                GItems.Items.Skip(1).Select(x => x.m_codeName)
            );
            throw new CommandException($"Unknown {argName} code name; did you mean \"{closestCodeName}\"?", Index);
        }
        return item;
    }

    public CItemCell ArgCellItem(string argName = "cell item") {
        string codeNameOrId = ArgString(argName);
        if (codeNameOrId.StartsWith("#")) {
            if (!ushort.TryParse(codeNameOrId.Substring(1), out ushort itemId)) {
                throw new CommandException($"Invalid {argName} ID", Index);
            }
            if (itemId >= GItems.Items.Count) {
                throw new CommandException($"{argName} ID is out of range", Index);
            }
            var item = GItems.Items[itemId];
            if (item is not CItemCell itemCell) {
                throw new CommandException($"Expected {argName}, not regular item", Index);
            }
            return itemCell;
        } else {
            var item = GItems.Items.Skip(1).FirstOrDefault(x => x.m_codeName == codeNameOrId);
            var allItemCellCodeNames = GItems.Items.Skip(1).OfType<CItemCell>().Select(x => x.m_codeName);

            if (item is null) {
                var closestCodeName = Misc.ClosestStringMatch(codeNameOrId, allItemCellCodeNames);
                throw new CommandException($"Unknown {argName} code name; did you mean \"{closestCodeName}\"?", Index);
            }
            if (item is not CItemCell itemCell) {
                var closestCodeName = Misc.ClosestStringMatch(codeNameOrId, allItemCellCodeNames);
                throw new CommandException($"Expected {argName}, not regular item; did you mean \"{closestCodeName}\"?", Index);
            }
            return itemCell;
        }
    }

    public CUnit.CDesc ArgUnitDesc(string argName = "unit descriptor") {
        string codeNameOrId = ArgString(argName);
        if (codeNameOrId.StartsWith("#")) {
            if (!byte.TryParse(codeNameOrId.Substring(1), out byte unitId)) {
                throw new CommandException($"Invalid {argName} ID", Index);
            }
            if (unitId >= GUnits.UDescs.Count) {
                throw new CommandException($"{argName} ID is out of range", Index);
            }
            return GUnits.UDescs[unitId];
        }

        var unit = GUnits.UDescs.Skip(1).FirstOrDefault(x => x.m_codeName == codeNameOrId);
        if (unit is null) {
            var closestCodeName = Misc.ClosestStringMatch(codeNameOrId,
                GUnits.UDescs.Skip(1).Select(x => x.m_codeName)
            );
            throw new CommandException($"Unknown {argName} code name; did you mean \"{closestCodeName}\"?", Index);
        }
        return unit;
    }

    public bool ArgBool(string argName = "boolean") {
        if (!HasNext) {
            throw new CommandException($"Expected {argName}", Index + 1);
        }
        if (!bool.TryParse(_args[Index], out bool result)) {
            throw new CommandException($"Invalid boolean", Index + 1);
        }
        Index += 1;
        return result;
    }

    public T ArgEnum<T>(string argName = "value") where T : struct, Enum {
        if (!HasNext) {
            throw new CommandException($"Expected {argName}", Index + 1);
        }
        string strValue = _args[Index];
        if (!Enum.IsDefined(typeof(T), strValue)) {
            var closestEnumName = Misc.ClosestStringMatch(strValue, Enum.GetNames(typeof(T)));
            throw new CommandException($"Unknown {argName}; did you mean \"{closestEnumName}\"?", Index);
        }
        Index += 1;
        return (T)Enum.Parse(typeof(T), strValue);
    }

    private float ArgRelativeWorldCoord(float relativeBase, string argName) {
        if (!HasNext) {
            throw new CommandException($"Expected {argName}", Index + 1);
        }
        var coord = _args[Index];
        Index += 1;

        if (coord.StartsWith("~")) {
            string offsetStr = coord.Substring(1);
            if (string.IsNullOrEmpty(offsetStr)) {
                return relativeBase;
            }
            if (!float.TryParse(offsetStr, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float offset)) {
                throw new CommandException($"Invalid relative offset for {argName}", Index);
            }
            return relativeBase + offset;
        } else {
            if (!float.TryParse(coord, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float result)) {
                throw new CommandException($"Invalid {argName}", Index);
            }
            return result;
        }
    }
    private int ArgRelativeCellCoord(int relativeBase, string argName) {
        if (!HasNext) {
            throw new CommandException($"Expected {argName}", Index + 1);
        }
        var coord = _args[Index];
        Index += 1;

        if (coord.StartsWith("~")) {
            string offsetStr = coord.Substring(1);
            if (string.IsNullOrEmpty(offsetStr)) {
                return relativeBase;
            }
            if (!int.TryParse(offsetStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int offset)) {
                throw new CommandException($"Invalid relative offset for {argName}", Index);
            }
            return relativeBase + offset;
        } else {
            if (!int.TryParse(coord, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result)) {
                throw new CommandException($"Invalid {argName}", Index);
            }
            return result;
        }
    }
}

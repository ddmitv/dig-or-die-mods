using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using ModUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

[Serializable]
public class InvalidCommandArgument : Exception {
    public int? argIndex = null;

    public InvalidCommandArgument(string message) : base(message) {}

    public InvalidCommandArgument(string message, int argIndex) : base(message) {
        this.argIndex = argIndex;
    }
}

public static class CustomCommandsPatch {
    public delegate void ExecCommandFn(string[] args, CPlayer playerSender);
    public delegate List<string> TabCommandFn(int argIndex);

    public static Dictionary<string, ExecCommandFn> customCommands = new();

    public static Dictionary<string, TabCommandFn> customTabCommands = new();

    public static Dictionary<string, string> customCommandHelpString = new();

    private static string[] ParseArgs(string text) {
        return text.Split([' ', '\t', '\r', '\n', '\v', '\f'], StringSplitOptions.RemoveEmptyEntries);
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(SNetworkCommands), nameof(SNetworkCommands.ProcessCommand))]
    private static IEnumerable<CodeInstruction> SNetworkCommands_ProcessCommand(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        static bool ExecCustomCommand(string text, CPlayer playerSender) {
            string[] commandAndArgs = ParseArgs(text);
            string command = commandAndArgs[0];
            if (!customCommands.TryGetValue(command, out ExecCommandFn fn)) {
                return false;
            }
            string[] args = commandAndArgs.Skip(1).ToArray();

            try {
                fn(args, playerSender);
            } catch (InvalidCommandArgument exception) {
                if (!playerSender.IsMe()) { return true; }

                string errorMessage = exception.argIndex switch {
                    null => $"{command}: {exception.Message}",
                    int idx => $"{command}: {exception.Message} (argument {idx})"
                }; 
                SSingletonScreen<SScreenHudChat>.Inst.AddChatMessage_Local(null, errorMessage, false);
            }
            return true;
        }

        var codeMatcher = new CodeMatcher(instructions, generator);

        codeMatcher.Start()
            .MatchForward(useEnd: false,
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldsfld, Utils.StaticField(typeof(System.String), "Empty")),
                new(OpCodes.Ldarg_2),
                new(OpCodes.Ldc_I4_1),
                new(OpCodes.Call, AccessTools.Method(typeof(SNetworkCommands), "DrawHelp_IfSenderIsMe")),
                new(OpCodes.Ret))
            .CreateLabelAtOffset(5, out Label exitLabel)
            .InjectAndAdvance(OpCodes.Ldarg_1)
            .Insert(
                new(OpCodes.Ldarg_2),
                Transpilers.EmitDelegate(ExecCustomCommand),
                new(OpCodes.Brtrue, exitLabel));

        return codeMatcher.Instructions();
    }
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SNetworkCommands), nameof(SNetworkCommands.TabCommand))]
    private static void SNetworkCommands_TabCommand(SNetworkCommands __instance, string input, ref string __result) {
        string[] commandAndArgs = ParseArgs(input);
        string command = commandAndArgs[0];

        if (!customTabCommands.TryGetValue(command, out TabCommandFn tabCommand)) {
            return;
        }
        string arg = commandAndArgs.Length <= 1 ? "" : commandAndArgs[1];
        List<string> argList = tabCommand(commandAndArgs.Length);
        __result = __instance.TabOnList(__result, command, arg, argList);
    }
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SNetworkCommands), nameof(SNetworkCommands.DrawHelp_IfSenderIsMe))]
    private static void SNetworkCommands_DrawHelp_IfSenderIsMe(SNetworkCommands __instance, string command, CPlayer playerSender, bool all) {
        if (!playerSender.IsMe()) { return; }

        if (all) {
            foreach (var commandHelpString in customCommandHelpString.Values) {
                Utils.AddChatMessageLocalNL(commandHelpString);
            }
            return;
        }
        if (!customCommandHelpString.TryGetValue(command, out string helpString)) {
            return;
        }
        Utils.AddChatMessageLocalNL(helpString);
    }
}

public class NothingCItemCell : CItemCell {
    public NothingCItemCell() : base(tile: null, tileIcon: null, 0, 0) {
        m_id = 0;
        m_name = "Nothing";
        m_codeName = "nothing";
    }
}

[BepInPlugin("extra-commands", "Extra Commands", "1.0.0")]
public class ExtraCommands : BaseUnityPlugin
{
    private static readonly CItemCell nothingCell = new NothingCItemCell();

    private static void AddCommand(
        string name,
        CustomCommandsPatch.ExecCommandFn fn,
        CustomCommandsPatch.TabCommandFn tabCommandFn = null,
        string helpString = null
    ) {
        if (!name.StartsWith("/")) {
            throw new ArgumentException("Command name must start with '/'", nameof(name));
        }
        CustomCommandsPatch.customCommands.Add(name, fn);
        if (tabCommandFn is not null) {
            CustomCommandsPatch.customTabCommands.Add(name, tabCommandFn);
        }
        if (helpString is not null) {
            CustomCommandsPatch.customCommandHelpString.Add(name, helpString);
        }
    }
    private static CPlayer GetPlayerByName(string name) {
        return SNetwork.Players.FirstOrDefault(player => player.m_name == name);
    }
    private static List<string> GetListOfPlayersNames() {
        List<string> result = SNetwork.Players.Select(player => player.m_name).ToList();
        result.Sort();
        return result;
    }
    private static CItem ParseItem(string codeName) {
        if (codeName.StartsWith("#")) {
            if (!uint.TryParse(codeName.Substring(1), out uint itemId)) {
                throw new FormatException("Invalid item id");
            }
            if (itemId >= GItems.Items.Count) {
                throw new FormatException("Item id is out of range");
            }
            return GItems.Items[(int)itemId];
        }
        if (codeName == nothingCell.m_codeName) {
            return null;
        }
        var item = GItems.Items.Skip(1).FirstOrDefault(x => x.m_codeName == codeName);
        if (item is null) {
            throw new FormatException("Unknown item code name");
        }
        return item;
    }
    private static List<string> GetListOfCCellItemNames() {
        return GItems.Items.Skip(1).Where(x => x is CItemCell).Select(x => x.m_codeName).ToList();
    }
    private static CUnit.CDesc ParseUnitCDesc(string codeName) {
        if (codeName.StartsWith("#")) {
            if (!uint.TryParse(codeName.Substring(1), out uint unitId)) {
                throw new FormatException("Invalid unit id");
            }
            if (unitId >= GUnits.UDescs.Count) {
                throw new FormatException("Unit id is out of range");
            }
            return GUnits.UDescs[(int)unitId];
        }

        var unit = GUnits.UDescs.Skip(1).FirstOrDefault(x => x.m_codeName == codeName);
        if (unit is null) {
            throw new FormatException("Unknown unit code name");
        }
        return unit;
    }
    private static bool ParseRelativeCoordinate(string input, int playerPos, int playerCursorPos, out int result) {
        int num = 0;
        result = 0;
        if (input.StartsWith("~")) {
            if (input.Length == 1 || int.TryParse(input.Substring(1), out num)) {
                result = num + playerPos;
                return true;
            } else {
                return false;
            }
        }
        if (input.StartsWith("^")) {
            if (input.Length == 1 || int.TryParse(input.Substring(1), out num)) {
                result = num + playerCursorPos;
                return true;
            } else {
                return false;
            }
        }
        if (int.TryParse(input, out num)) {
            result = num;
            return true;
        } else {
            return false;
        }
    }
    private static bool ParseRelativeCoordinate(string input, float playerPos, float playerCursorPos, out float result) {
        float num = 0;
        result = 0;
        if (input.StartsWith("~")) {
            if (input.Length == 1 || float.TryParse(input.Substring(1), out num)) {
                result = num + playerPos;
                return true;
            } else {
                return false;
            }
        }
        if (input.StartsWith("^")) {
            if (input.Length == 1 || float.TryParse(input.Substring(1), out num)) {
                result = num + playerCursorPos;
                return true;
            } else {
                return false;
            }
        }
        if (float.TryParse(input, out num)) {
            result = num;
            return true;
        } else {
            return false;
        }
    }

    private void Start() {
        var harmony = new Harmony("extra-commands");
        harmony.PatchAll(typeof(CustomCommandsPatch));

        AddCommand("/tp", (string[] args, CPlayer player) => {
            if (args.Length == 0) {
                throw new InvalidCommandArgument("Expected number or player name", 1);
            }

            if (args.Length == 1) {
                CPlayer targetPlayer = GetPlayerByName(args[0]);
                if (targetPlayer == null) {
                    throw new InvalidCommandArgument("Unknown player name", 1);
                }
                player.m_unitPlayer.Pos = targetPlayer.m_unitPlayer.Pos;
            } else {
                var playerPos = player.m_unitPlayer.Pos;
                var mousePos = SGame.MouseWorldPos;
                if (!ParseRelativeCoordinate(args[0], playerPos.x, mousePos.x, out float pos_x)) {
                    throw new InvalidCommandArgument("Expected number", 1);
                }
                if (args.Length <= 1 || !ParseRelativeCoordinate(args[1], playerPos.y, mousePos.y, out float pos_y)) {
                    throw new InvalidCommandArgument("Expected number", 2);
                }

                Vector2 new_pos = new Vector2(pos_x, pos_y);
                if (!SWorld.GridRectM2.Contains(new_pos)) {
                    throw new InvalidCommandArgument("The position is out of the world");
                }
                player.m_unitPlayer.Pos = new_pos;
                Utils.AddChatMessageLocal($"Teleported to ({pos_x}, {pos_y})");
            }
        }, tabCommandFn: (int argIndex) => {
            return GetListOfPlayersNames();
        }, helpString: 
            "<color='#afe8f5'>/tp pos-x pos-y</color>: Teleport to the location.\n" +
            "<color='#afe8f5'>/tp player-name</color>: Teleport to the 'player-name's location."
        );
        AddCommand("/give", (string[] args, CPlayer player) => {
            if (args.Length == 0) {
                throw new InvalidCommandArgument("Expected item name", 1);
            }
            CItem selectedItem;
            try {
                selectedItem = ParseItem(args[0]);
            } catch (FormatException formatException) {
                throw new InvalidCommandArgument(formatException.Message, 1);
            }
            if (selectedItem is null) {
                throw new InvalidCommandArgument("Cannot give null item", 1);
            }

            int itemCount = 1;
            if (args.Length >= 2) {
                if (!int.TryParse(args[1], out itemCount)) {
                    throw new InvalidCommandArgument("Expected number of items", 2);
                }
            }
            Utils.AddChatMessageLocal($"Given {itemCount} {selectedItem.Name}");
            player.m_inventory.AddToInventory(selectedItem, itemCount);
        }, tabCommandFn: (int argIndex) => {
            return GItems.Items.Skip(1).Select(x => x.m_codeName).ToList();
        }, helpString:
            "<color='#afe8f5'>/give item-codename</color>: Give 1 'item-codename' to the player.\n" +
            "<color='#afe8f5'>/give item-codename amount</color>: Give 'amount' of 'item-codename' to the player"
        );
        AddCommand("/place", (string[] args, CPlayer player) => {
            if (args.Length == 0) {
                throw new InvalidCommandArgument("Expected item cell code name", 1);
            }
            CItem selectedItem;
            try {
                selectedItem = ParseItem(args[0]) ?? nothingCell;
            } catch (FormatException formatException) {
                throw new InvalidCommandArgument(formatException.Message, 1);
            }
            if (selectedItem is not CItemCell selectedCell) {
                throw new InvalidCommandArgument("Expected item cell, not regular item", 1);
            }
            var playerPos = player.m_unitPlayer.PosCell;
            var mousePos = SGame.MouseWorldPosInt;
            if (args.Length < 2 || !ParseRelativeCoordinate(args[1], playerPos.x, mousePos.x, out int posI)) {
                throw new InvalidCommandArgument("Expected integer", 2);
            }
            if (args.Length < 3 || !ParseRelativeCoordinate(args[2], playerPos.y, mousePos.y, out int posJ)) {
                throw new InvalidCommandArgument("Expected integer", 3);
            }

            if (!Utils.IsInWorld(posI, posJ)) {
                throw new InvalidCommandArgument("The cell position is out of the world");
            }
            Utils.AddChatMessageLocal($"Replaced cell at ({posI}, {posJ}) with {selectedCell.Name}");
            Utils.RawSetContent(posI, posJ, selectedCell);
        }, tabCommandFn: (int argIndex) => {
            return GetListOfCCellItemNames();
        }, helpString:
            "<color='#afe8f5'>/place tile-codename pos-x pos-y</color>: Place 'tile-codename' to the 'pos-x', 'pos-y' location. 'pos-x' and 'pos-y' must be integers"
        );
        AddCommand("/fill", (string[] args, CPlayer player) => {
            if (args.Length == 0) {
                throw new InvalidCommandArgument("Expected item cell code name", 1);
            }
            CItem selectedItem;
            try {
                selectedItem = ParseItem(args[0]) ?? nothingCell;
            } catch (FormatException formatException) {
                throw new InvalidCommandArgument(formatException.Message, 1);
            }
            if (selectedItem is not CItemCell selectedCell) {
                throw new InvalidCommandArgument("Expected item cell, not regular item", 1);
            }

            var playerPos = player.m_unitPlayer.PosCell;
            var mousePos = SGame.MouseWorldPosInt;
            if (args.Length < 2 || !ParseRelativeCoordinate(args[1], playerPos.x, mousePos.x, out int fromX)) {
                throw new InvalidCommandArgument("Expected integer", 2);
            }
            if (args.Length < 3 || !ParseRelativeCoordinate(args[2], playerPos.y, mousePos.y, out int fromY)) {
                throw new InvalidCommandArgument("Expected integer", 3);
            }
            if (args.Length < 4 || !ParseRelativeCoordinate(args[3], playerPos.x, mousePos.x, out int toX)) {
                throw new InvalidCommandArgument("Expected integer", 4);
            }
            if (args.Length < 5 || !ParseRelativeCoordinate(args[4], playerPos.y, mousePos.y, out int toY)) {
                throw new InvalidCommandArgument("Expected integer", 5);
            }

            if (!Utils.IsInWorld(fromX - 1, fromY - 1)) {
                throw new InvalidCommandArgument($"The cell 'from' position is out of the world ({fromX}, {fromY})");
            }
            if (!Utils.IsInWorld(toX, toY)) {
                throw new InvalidCommandArgument($"The cell 'to' position is out of the world ({toX}, {toY})");
            }
            int replacedCellsNum = Math.Max(0, toX - fromX + 1) * Math.Max(0, toY - fromY + 1);

            Utils.AddChatMessageLocal(
                $"Filled cells from ({fromX}, {fromY}) to ({toX}, {toY}) with {selectedCell.Name}. " +
                $"Total replaced cells: {replacedCellsNum}"
            );
            for (int x = fromX; x <= toX; ++x) {
                for (int y = fromY; y <= toY; ++y) {
                    Utils.RawSetContent(x, y, selectedCell);
                }
            }
        }, tabCommandFn: (int argIndex) => {
            return GetListOfCCellItemNames();
        }, helpString:
            "<color='#afe8f5'>/fill tile-codename from-x from-y to-x to-y</color>: Fill region with a specific cell"
        );
        AddCommand("/killinfo", (string[] args, CPlayer player) => {
            if (args.Length > 0) {
                throw new InvalidCommandArgument("None arguments are expected");
            }
            foreach (var specieKilled in SSingleton<SUnits>.Inst.SpeciesKilled) {
                Utils.AddChatMessageLocal($"{specieKilled.m_uDesc.GetName()}: {specieKilled.m_nb} ({GVars.SimuTime - specieKilled.m_lastKillTime:0.00})");
            }
        }, helpString:
            "<color='#afe8f5'>/killinfo</color>: Displays world's species kill information in the format 'species name': 'kill number' ('time since last kill')"
        );
        AddCommand("/spawn", (string[] args, CPlayer player) => {
            if (args.Length == 0) {
                throw new InvalidCommandArgument("Expected item cell code name", 1);
            }
            CUnit.CDesc selectedUnit;
            try {
                selectedUnit = ParseUnitCDesc(args[0]);
            } catch (FormatException formatException) {
                throw new InvalidCommandArgument(formatException.Message, 1);
            }
            if (selectedUnit is null) {
                throw new InvalidCommandArgument("Cannot spawn null unit", 1);
            }

            var playerPos = player.m_unitPlayer.Pos;
            var mousePos = SGame.MouseWorldPos;
            if (args.Length < 2 || !ParseRelativeCoordinate(args[1], playerPos.x, mousePos.x, out float spawnX)) {
                throw new InvalidCommandArgument("Expected x coordinate (number)", 2);
            }
            if (args.Length < 3 || !ParseRelativeCoordinate(args[2], playerPos.y, mousePos.y, out float spawnY)) {
                throw new InvalidCommandArgument("Expected y coordinate (number)", 3);
            }

            Vector2 spawnPos = new Vector2(spawnX, spawnY);
            if (!SWorld.GridRectM2.Contains(spawnPos)) {
                throw new InvalidCommandArgument($"The spawn position is out of the world ({spawnX}, {spawnY})");
            }
            Utils.AddChatMessageLocal($"Spawned unit {selectedUnit.GetName()} at ({spawnX}, {spawnY})");
            SUnits.SpawnUnit(selectedUnit, spawnPos);
        }, tabCommandFn: (int argIndex) => {
            return GUnits.UDescs.Skip(1).Select(x => x.m_codeName).ToList();
        }, helpString:
            "<color='#afe8f5'>/spawn unit-codename spawn-x spawn-y</color>: Creates new unit 'unit-codename' at 'spawn-x', 'spawn-y' position"
        );
        AddCommand("/clearinventory", (string[] args, CPlayer player) => {
            if (args.Length > 0) {
                throw new InvalidCommandArgument("None arguments are expected");
            }
            Utils.AddChatMessageLocal($"Cleared {player.m_inventory.Items.Count} items from inventory");
            player.m_inventory.CleanAll();
        }, helpString:
            "<color='#afe8f5'>/clearinventory</color> Clears current player inventory."
        );
        AddCommand("/clearpickups", (string[] args, CPlayer player) => {
            if (args.Length > 0) {
                throw new InvalidCommandArgument("None arguments are expected");
            }
            Utils.AddChatMessageLocal($"Cleared {SPickups.Pickups.Count} pickups");
            SSingleton<SPickups>.Inst.CleanAll();
        }, helpString:
            "<color='#afe8f5'>/clearpickups</color> Clears all pickups in the worldclearpickups."
        );
    }
}


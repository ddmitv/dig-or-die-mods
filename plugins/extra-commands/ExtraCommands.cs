using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using ModUtils;
using Mono.Cecil;
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
            return nothingCell;
        }
        var item = GItems.Items.Skip(1).FirstOrDefault(x => x.m_codeName == codeName);
        if (item is null) {
            throw new FormatException("Unknown item code name");
        }
        return item;
    }
    public class SetCellArgs {
        public uint flags = 0;
        public bool replaceBackground = false;
        public ushort hp = ushort.MaxValue;
        public short forceX = 0;
        public short forceY = 0;
        public float water = 0f;
        public Color24 light = default;
        public byte elecProd = 0;
        public byte elecCons = 0;
        public Color24 temp = default;
    }
    public static void SetCell(int i, int j, CItemCell cell, SetCellArgs args = null) {
        args ??= new();

        ref CCell selectedCell = ref SWorld.Grid[i, j];
        CItemCell prevContent = selectedCell.GetContent();
        selectedCell.m_contentId = cell.m_id;
        selectedCell.m_contentHP = args.hp == ushort.MaxValue ? cell.m_hpMax : args.hp;

        if (args.replaceBackground) {
            selectedCell.m_flags &= (CCell.Flag_BackWall_0 | CCell.Flag_BgSurface_0 | CCell.Flag_BgSurface_1 | CCell.Flag_BgSurface_2);
            selectedCell.m_flags |= args.flags;
        } else {
            selectedCell.m_flags = args.flags;
        }
        selectedCell.m_forceX = args.forceX;
        selectedCell.m_forceY = args.forceY;
        selectedCell.m_water = args.water;
        selectedCell.m_light = args.light;
        selectedCell.m_elecProd = args.elecProd;
        selectedCell.m_elecCons = args.elecCons;
        selectedCell.m_temp = args.temp;

        SWorldNetwork.OnSetContent(i, j, true, prevContent);
    }

    private class ParseCellResult {
        public CItemCell item;
        public SetCellArgs parameters = new();
    }

    private static ParseCellResult ParseCellParameters(string str) {
        int codeNameEnd = str.IndexOf('{');
        string codeName = str.Substring(0, codeNameEnd == -1 ? str.Length : codeNameEnd);

        CItem item = ParseItem(codeName);
        if (item is null) {
            throw new FormatException("Unknown item code name");
        }
        if (item is not CItemCell itemCell) {
            throw new FormatException("Expected item cell, not regular item");
        }
        var result = new ParseCellResult() { item = itemCell };
        result.parameters.hp = itemCell.m_hpMax;

        if (codeNameEnd == -1) {
            return result;
        }
        if (str[str.Length - 1] != '}') {
            throw new FormatException("Unmatched '}'");
        }
        var cellParamsStr = str.Remove(str.Length - 1).Substring(codeNameEnd + 1).Split(',').Select(x => x.Trim());
        var parameters = result.parameters;

        void SetFlag(uint flag, string val) {
            Utils.SetFlag(ref parameters.flags, flag, Utils.ParseBool(val));
        }
        foreach (var cellParamStr in cellParamsStr) {
            string[] paramNameAndValue = cellParamStr.Split('=');
            if (paramNameAndValue.Length != 2) {
                throw new FormatException("There must be only one '='");
            }
            string paramName = paramNameAndValue[0];
            string paramValue = paramNameAndValue[1];
            
            switch (paramName.ToLower()) {
            case "hp": parameters.hp = ushort.Parse(paramValue); break;
            case "forcex": parameters.forceX = short.Parse(paramValue); break;
            case "forcey": parameters.forceY = short.Parse(paramValue); break;
            case "water": parameters.water = float.Parse(paramValue); break;
            case "elecprod": parameters.elecProd = byte.Parse(paramValue); break;
            case "eleccons": parameters.elecCons = byte.Parse(paramValue); break;
            case "data0": SetFlag(CCell.Flag_CustomData0, paramValue); break;
            case "data1": SetFlag(CCell.Flag_CustomData1, paramValue); break;
            case "data2": SetFlag(CCell.Flag_CustomData2, paramValue); break;
            case "burning": SetFlag(CCell.Flag_IsBurning, paramValue); break;
            case "mapped": SetFlag(CCell.Flag_IsMapped, paramValue); break;
            case "backwall": SetFlag(CCell.Flag_BackWall_0, paramValue); break;
            case "bg0": SetFlag(CCell.Flag_BgSurface_0, paramValue); parameters.replaceBackground = true; break;
            case "bg1": SetFlag(CCell.Flag_BgSurface_1, paramValue); parameters.replaceBackground = true; break;
            case "bg2": SetFlag(CCell.Flag_BgSurface_2, paramValue); parameters.replaceBackground = true; break;
            case "waterfall": SetFlag(CCell.Flag_WaterFall, paramValue); break;
            case "streamlfast": SetFlag(CCell.Flag_StreamLFast, paramValue); break;
            case "streamrfast": SetFlag(CCell.Flag_StreamRFast, paramValue); break;
            case "lava": SetFlag(CCell.Flag_IsLava, paramValue); break;
            case "haswireright": SetFlag(CCell.Flag_HasWireRight, paramValue); break;
            case "haswiretop": SetFlag(CCell.Flag_HasWireTop, paramValue); break;
            case "electricalgostate": SetFlag(CCell.Flag_ElectricAlgoState, paramValue); break;
            case "powered": SetFlag(CCell.Flag_IsPowered, paramValue); break;
            case "light": parameters.light = Utils.ParseColor24(paramValue); break;
            case "temp": parameters.temp = Utils.ParseColor24(paramValue); break;
            default: throw new FormatException($"Unknown cell parameter '{paramName}'");
            }
        }
        return result;
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
            ParseCellResult selectedCell;
            try {
                selectedCell = ParseCellParameters(args[0]);
            } catch (Exception ex) when (ex is FormatException || ex is OverflowException) {
                throw new InvalidCommandArgument(ex.Message, 1);
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
            Utils.AddChatMessageLocal($"Replaced cell at ({posI}, {posJ}) with {selectedCell.item.Name}");
            SetCell(posI, posJ, selectedCell.item, selectedCell.parameters);
        }, tabCommandFn: (int argIndex) => {
            return GetListOfCCellItemNames();
        }, helpString:
            "<color='#afe8f5'>/place tile-codename pos-x pos-y</color>: Place 'tile-codename' to the 'pos-x', 'pos-y' location. 'pos-x' and 'pos-y' must be integers"
        );
        AddCommand("/fill", (string[] args, CPlayer player) => {
            if (args.Length == 0) {
                throw new InvalidCommandArgument("Expected item cell code name", 1);
            }
            ParseCellResult selectedCell;
            try {
                selectedCell = ParseCellParameters(args[0]);
            } catch (FormatException formatException) {
                throw new InvalidCommandArgument(formatException.Message, 1);
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
                $"Filled cells from ({fromX}, {fromY}) to ({toX}, {toY}) with {selectedCell.item.Name}. " +
                $"Total replaced cells: {replacedCellsNum}"
            );
            for (int x = fromX; x <= toX; ++x) {
                for (int y = fromY; y <= toY; ++y) {
                    SetCell(x, y, selectedCell.item, selectedCell.parameters);
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


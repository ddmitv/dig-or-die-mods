using DODModAPI;
using System;
using System.Linq;
using UnityEngine;

public sealed class NoneItemPlaceholder : CItemCell {
    public NoneItemPlaceholder() : base(tile: null, tileIcon: null, 0, 0) {
        m_id = 0;
        m_name = "None";
        m_codeName = "none";
    }
    public static readonly CItemCell Inst = new NoneItemPlaceholder();
}

public static class CustomCommands {
    private static CItem ParseItem(string codeName) {
        if (codeName.StartsWith("#")) {
            if (!uint.TryParse(codeName.Substring(1), out uint itemId)) {
                throw new FormatException("Invalid item id");
            }
            if (itemId == 0) { return NoneItemPlaceholder.Inst; }

            if (itemId >= GItems.Items.Count) {
                throw new FormatException("Item id is out of range");
            }
            return GItems.Items[(int)itemId];
        }
        if (codeName == NoneItemPlaceholder.Inst.m_codeName) {
            return NoneItemPlaceholder.Inst;
        }
        var item = GItems.Items.Skip(1).FirstOrDefault(x => x.m_codeName == codeName);
        if (item is null) {
            var closestCodeName = Misc.ClosestStringMatch(codeName,
                GItems.Items.Skip(1).Select(x => x.m_codeName)
            );
            throw new FormatException($"Unknown item code name; did you mean '{closestCodeName}'?");
        }
        return item;
    }
    private static void SetCell(int i, int j, in CCell cell, bool replaceBackground = true) {
        ref CCell currCell = ref SWorld.Grid[i, j];
        CItemCell prevContent = currCell.GetContent();

        uint oldFlags = currCell.m_flags;
        currCell = cell;

        if (!replaceBackground) {
            currCell.m_flags = (currCell.m_flags & ~CellFlags.BgSurfaceMask) | (oldFlags & CellFlags.BgSurfaceMask);
        }

        SWorldNetwork.OnSetContent(i, j, true, prevContent);
    }
    private static Color24 ParseColor24(string str) {
        string[] valuesStr = str.Split(':');
        if (valuesStr.Length == 1) {
            return new Color24(uint.Parse(str, System.Globalization.NumberStyles.HexNumber));
        }
        if (valuesStr.Length != 3) { throw new FormatException("Expected exact 3 values for Color24"); }
        return new Color24(byte.Parse(valuesStr[0]), byte.Parse(valuesStr[1]), byte.Parse(valuesStr[2]));
    }

    private struct ParseCellResult {
        public CItemCell item;
        public CCell cell;
        public bool replaceBackground;
    }

    private delegate bool TryParseFn<T>(string value, out T result);

    private static ParseCellResult ParseCellParameters(string str) {
        int braceIndex = str.IndexOf('{');
        string codeName = braceIndex == -1 ? str : str.Substring(0, braceIndex);

        CItem item = ParseItem(codeName);
        if (item is not CItemCell itemCell) {
            var closestCodeName = Misc.ClosestStringMatch(codeName,
                GItems.Items.Skip(1).OfType<CItemCell>().Select(x => x.m_codeName)
            );
            throw new FormatException($"Expected item cell, not regular item; did you mean \"{closestCodeName}\"?");
        }
        var result = new ParseCellResult() {
            item = itemCell,
            cell = new CCell() {
                m_contentId = itemCell.m_id,
                m_contentHP = itemCell.m_hpMax,
            },
            replaceBackground = false,
        };
        if (braceIndex == -1) {
            return result;
        }
        if (!str.EndsWith("}")) {
            throw new FormatException("Unmatched '}'");
        }
        var cellParams = str.Substring(braceIndex + 1, str.Length - braceIndex - 2).Split(',');

        foreach (var cellParam in cellParams) {
            int eqSymbolIndex = cellParam.IndexOf('=');
            if (eqSymbolIndex <= 0 || eqSymbolIndex == cellParam.Length - 1) {
                throw new FormatException($"Invalid cell parameter: '{cellParam.Trim()}'");
            }
            string name = cellParam.Substring(0, eqSymbolIndex).Trim();
            string value = cellParam.Substring(eqSymbolIndex + 1).Trim();

            switch (name.ToLowerInvariant()) {
            case "hp": HandleNumericParam(value, name, ushort.TryParse, out result.cell.m_contentHP); break;
            case "forcex": HandleNumericParam(value, name, short.TryParse, out result.cell.m_forceX); break;
            case "forcey": HandleNumericParam(value, name, short.TryParse, out result.cell.m_forceY); break;
            case "water": HandleNumericParam(value, name, float.TryParse, out result.cell.m_water); break;
            case "elecprod": HandleNumericParam(value, name, byte.TryParse, out result.cell.m_elecProd); break;
            case "eleccons": HandleNumericParam(value, name, byte.TryParse, out result.cell.m_elecCons); break;
            case "data": HandleDataParam(value, name, ref result.cell.m_flags); break;
            case "burning": HandleBitFlagParam(value, name, CellFlags.IsBurning, ref result.cell.m_flags); break;
            case "mapped": HandleBitFlagParam(value, name, CellFlags.IsMapped, ref result.cell.m_flags); break;
            case "backwall": HandleBitFlagParam(value, name, CellFlags.BackWall_0, ref result.cell.m_flags); break;
            case "bg": HandleBgParam(value, name, ref result.cell.m_flags); result.replaceBackground = true; break;
            case "waterfall": HandleBitFlagParam(value, name, CellFlags.WaterFall, ref result.cell.m_flags); break;
            case "streamlfast": HandleBitFlagParam(value, name, CellFlags.StreamLFast, ref result.cell.m_flags); break;
            case "streamrfast": HandleBitFlagParam(value, name, CellFlags.StreamRFast, ref result.cell.m_flags); break;
            case "lava": HandleBitFlagParam(value, name, CellFlags.IsLava, ref result.cell.m_flags); break;
            case "wire": HandleWireParam(value, name, ref result.cell.m_flags); break;
            case "electricalgostate": HandleBitFlagParam(value, name, CellFlags.ElectricAlgoState, ref result.cell.m_flags); break;
            case "powered": HandleBitFlagParam(value, name, CellFlags.IsPowered, ref result.cell.m_flags); break;
            case "light": result.cell.m_light = ParseColor24(value); break;
            case "temp": result.cell.m_temp = ParseColor24(value); break;
            default: throw new FormatException($"Unknown cell parameter \"{name}\"");
            }
        }
        return result;

        static void HandleNumericParam<T>(string str, string name, TryParseFn<T> tryParseFn, out T value) {
            if (!tryParseFn(str, out value)) {
                throw new FormatException($"'{name}' cell parameter is invalid: {str}");
            }
        }
        static void HandleBitFlagParam(string str, string name, uint mask, ref uint flags) {
            if (!Misc.TryParseBool(str, out bool value)) {
                throw new FormatException($"'{name}' cell parameter is invalid: {str}");
            }
            Misc.SetFlag(ref flags, mask, value);
        }
        static void HandleDataParam(string str, string name, ref uint flags) {
            flags &= ~CellFlags.CustomDataMask;
            byte data = Misc.ParseByteSmart(str);

            const uint MaxData = CellFlags.CustomDataMask >> CellFlags.CustomDataBitShift;
            if (data > MaxData) {
                throw new FormatException($"'{name}' cell parameter is out of range: {str} (max={MaxData})");
            }
            flags |= (uint)data << CellFlags.CustomDataBitShift;
        }
        static void HandleBgParam(string str, string name, ref uint flags) {
            flags &= ~CellFlags.BgSurfaceMask;

            if (Misc.TryParseByteSmart(str, out byte bgId)) {
                const uint MaxBgId = CellFlags.BgSurfaceMask >> CellFlags.BgSurfaceBitShift;
                if (bgId > MaxBgId) {
                    throw new FormatException($"'{name}' cell parameter is out of range: {str} (max={MaxBgId})");
                }
                flags |= (uint)bgId << CellFlags.BgSurfaceBitShift;
            } else {
                flags |= str.ToLowerInvariant() switch {
                    "none" => 0u,
                    "dirt" => 1u,
                    "rock" => 2u,
                    "granit" => 3u,
                    "crystal" => 4u,
                    "lava" => 5u,
                    "organic" => 6u,
                    _ => throw new FormatException($"'{name}' cell parameter is invalid"),
                } << CellFlags.BgSurfaceBitShift;
            }
        }
        static void HandleWireParam(string str, string name, ref uint flags) {
            flags &= ~CellFlags.HasWireMask;
            flags |= str.ToLowerInvariant() switch {
                "right" or "1" or "r" => CellFlags.HasWireRight,
                "top" or "2" or "t" => CellFlags.HasWireTop,
                "topright" or "righttop" or "rt" or "tr" or "3" => CellFlags.HasWireRight | CellFlags.HasWireTop,
                "0" => 0,
                _ => throw new FormatException($"'{name}' cell parameter is invalid: {str}"),
            };
        }
    }

    private static bool TryParseClockTime(string str, out float result) {
        string[] timeParts = str.Split(':');
        if (timeParts.Length == 2) {
            result = default;
            if (!int.TryParse(timeParts[0], out int hoursPart)) {
                return false;
            }
            if (!float.TryParse(timeParts[1],
                System.Globalization.NumberStyles.Float & ~System.Globalization.NumberStyles.AllowLeadingSign,
                System.Globalization.CultureInfo.InvariantCulture,
                out float minutesPart)
            ) {
                return false;
            }
            if (minutesPart < 0 || minutesPart > 60) {
                return false;
            }
            result = (hoursPart + Math.Sign(hoursPart) * minutesPart / 60f) / 24f;
            return true;
        } else {
            return float.TryParse(str, out result);
        }
    }

    static public void AddCustomCommands() {
        var opts = new CommandManager.CommandOptions {
            DisableAchievements = BetterChat.configDisableAchievementsOnCommand.Value
        };
        
        CommandManager.Register("/tp", opts with {
            TabCompleter = argIdx => argIdx == 0 ? SNetwork.Players.Select(p => p.m_name).OrderBy(x => x).ToList() : null,
        }, (args, playerSender) => {
            if (playerSender.m_unitPlayer is null) {
                throw new CommandException("Cannot teleport: player has no active unit");
            }
            
            if (args.Remaining == 1) {
                CPlayer target = args.ArgPlayer();
                if (target.m_unitPlayer is null) {
                    throw new CommandException($"Cannot teleport: player \"{target.m_name}\" has no active unit");
                }
                playerSender.m_unitPlayer.Pos = target.m_unitPlayer.Pos;
            } else if (args.Remaining == 2) {
                Vector2 pos = args.ArgWorldPos();
                playerSender.m_unitPlayer.Pos = pos;
                Misc.SendChatMessageLocal($"Teleported to {pos}");
            } else {
                throw new CommandException("Expected either a player name or X and Y coordinates");
            }
        });
        CommandManager.Register("/give", opts with {
            TabCompleter = argIdx => argIdx == 0 ? GItems.Items.Skip(1).Select(x => x.m_codeName).ToList() : null,
        }, (args, playerSender) => {
            CItem selectedItem = args.ArgItem();
            if (selectedItem is null) {
                throw new CommandException("Cannot give null item", args.Index);
            }
            int itemCount = args.HasNext ? args.ArgInt("number of items") : 1;
            args.ArgNone();

            Misc.SendChatMessageLocal($"Given {itemCount} {selectedItem.Name}");
            CInventory inventory = playerSender.m_inventory;
            CStack itemStack = inventory.GetStack(selectedItem);
            if (itemStack != null) {
                itemStack.m_nb += itemCount;
            } else {
                itemStack = new CStack(selectedItem, itemCount);
                inventory.m_items.Add(itemStack);
                inventory.m_items.Sort(inventory.InventorySorting);
                inventory.AddItemToBarIFP(itemStack, select: false, skipMaterialsAndMinerals: true);
            }
        });
        CommandManager.Register("/place", opts with {
            TabCompleter = argIdx => argIdx == 0 ? GItems.Items.Skip(1).OfType<CItemCell>().Select(x => x.m_codeName).ToList() : null,
        }, (args, playerSender) => {
            ParseCellResult selectedCell;
            try {
                selectedCell = ParseCellParameters(args.ArgString("cell"));
            } catch (FormatException ex) {
                throw new CommandException(ex.Message, args.Index);
            }
            int2 pos = args.ArgCellPos();
            args.ArgNone();

            Misc.SendChatMessageLocal($"Replaced cell at {pos} with {selectedCell.item.Name}");
            SetCell(pos.x, pos.y, selectedCell.cell, selectedCell.replaceBackground);
        });
        CommandManager.Register("/fill", opts with {
            TabCompleter = argIdx => argIdx == 0 ? GItems.Items.Skip(1).OfType<CItemCell>().Select(x => x.m_codeName).ToList() : null,
        }, (args, playerSender) => {
            ParseCellResult selectedCell;
            try {
                selectedCell = ParseCellParameters(args.ArgString("cell"));
            } catch (FormatException ex) {
                throw new CommandException(ex.Message, args.Index);
            }
            int2 from = args.ArgCellPos("from position");
            int2 to = args.ArgCellPos("to position");
            args.ArgNone();

            Misc.NormalizeBounds(ref from, ref to);

            int replacedCellsNum = Math.Max(0, to.x - from.x + 1) * Math.Max(0, to.y - from.y + 1);

            Misc.SendChatMessageLocal(
                $"Filled cells from {from} to {to} with {selectedCell.item.Name}. " +
                $"Total replaced cells: {replacedCellsNum}"
            );
            for (int x = from.x; x <= to.x; ++x) {
                for (int y = from.y; y <= to.y; ++y) {
                    SetCell(x, y, selectedCell.cell, selectedCell.replaceBackground);
                }
            }
        });
        CommandManager.Register("/killinfo", opts with {
            Local = true,
        }, (args, playerSender) => {
            args.ArgNone();
            foreach (var specieKilled in SSingleton<SUnits>.Inst.SpeciesKilled) {
                Misc.SendChatMessageLocal($"{specieKilled.m_uDesc.GetName()}: {specieKilled.m_nb} ({GVars.SimuTime - specieKilled.m_lastKillTime:0.00})");
            }
        });
        CommandManager.Register("/spawn", opts with {
            TabCompleter = argIdx => argIdx == 0 ? GUnits.UDescs.Skip(1).Select(x => x.m_codeName).ToList() : null,
        }, (args, playerSender) => {
            CUnit.CDesc selectedUnit = args.ArgUnitDesc();
            if (selectedUnit is null) {
                throw new CommandException("Cannot spawn null unit", args.Index);
            }
            Vector2 spawnPos = args.ArgWorldPos("unit spawn position");
            args.ArgNone();

            Misc.SendChatMessageLocal($"Spawned unit {selectedUnit.GetName()} at {spawnPos}");
            SUnits.SpawnUnit(selectedUnit, spawnPos);
        });
        CommandManager.Register("/clearinventory", opts, (args, playerSender) => {
            args.ArgNone();
            Misc.SendChatMessageLocal($"Cleared {playerSender.m_inventory.Items.Count} items from inventory");
            playerSender.m_inventory.CleanAll();
        });
        CommandManager.Register("/clearpickups", opts, (args, player) => {
            args.ArgNone();
            Misc.SendChatMessageLocal($"Cleared {SPickups.Pickups.Count} pickups");
            SSingleton<SPickups>.Inst.CleanAll();
        });
        CommandManager.Register("/clone", opts, (args, player) => {
            int2 srcFrom = args.ArgCellPos("start source position");
            int2 srcTo = args.ArgCellPos("end source position");
            int2 dest = args.ArgCellPos("start destination source position");
            args.ArgNone();

            Misc.NormalizeBounds(ref srcFrom, ref srcTo);

            if (!Misc.IsInWorld(dest + (srcTo - srcFrom))) {
                throw new CommandException($"end destination position is out of the world {dest + (srcTo - srcFrom)}");
            }

            int clonedCellsNum = Math.Max(0, srcTo.x - srcFrom.x + 1) * Math.Max(0, srcTo.y - srcFrom.y + 1);
            Misc.SendChatMessageLocal(
                $"Cloned cells from source region {srcFrom}-{srcTo} to destination starting at {dest}. " +
                $"Total cloned cells: {clonedCellsNum}"
            );

            bool isOverlapping = (dest.y >= srcFrom.y);
            int iStep = !isOverlapping ? 1 : -1;
            int iStart = !isOverlapping ? 0 : srcTo.x - srcFrom.x - 1;
            int iEnd = !isOverlapping ? srcTo.x - srcFrom.x : -1;

            int copyLength = srcTo.y - srcFrom.y;
            for (int i = iStart; i != iEnd; i += iStep) {
                int srcIdx = srcFrom.y + (i + srcFrom.x) * SWorld.Gs.y;
                int destIdx = dest.y + (i + dest.x) * SWorld.Gs.y;
                Array.Copy(SWorld.Grid, srcIdx, SWorld.Grid, destIdx, copyLength);
            }
        });

        CommandManager.Register("/replace", opts with {
            TabCompleter = argIdx => argIdx <= 1 ? GItems.Items.Skip(1).OfType<CItemCell>().Select(x => x.m_codeName).ToList() : null,
        }, (args, player) => {
            CItemCell srcCell = args.ArgCellItem("source cell item");

            ParseCellResult destCell;
            try {
                destCell = ParseCellParameters(args.ArgString("destination cell"));
            } catch (FormatException ex) {
                throw new CommandException(ex.Message, args.Index);
            }
            int2 from = args.ArgCellPos();
            int2 to = args.ArgCellPos();
            args.ArgNone();

            Misc.NormalizeBounds(ref from, ref to);

            int replacedCellsNum = 0;
            for (int x = from.x; x <= to.x; ++x) {
                for (int y = from.y; y <= to.y; ++y) {
                    if (SWorld.Grid[x, y].m_contentId != srcCell.m_id) {
                        continue;
                    }
                    replacedCellsNum += 1;
                    SetCell(x, y, destCell.cell, destCell.replaceBackground);
                }
            }
            int checkedCellsNum = Math.Max(0, to.x - from.x + 1) * Math.Max(0, to.y - from.y + 1);
            Misc.SendChatMessageLocal(
                $"Replaced cells from {from} to {to} of type {srcCell.Name} to {destCell.item.Name}. " +
                $"Total replaced cells: {replacedCellsNum}, total checked cells: {checkedCellsNum}"
            );
        });
        CommandManager.Register("/freecam", opts with {
            Local = true,
        }, (args, player) => {
            if (args.Remaining >= 1) {
                string[] parts = args.ArgString("freecam parameter").Split('=');
                args.ArgNone();

                bool isGetter = parts.Length == 1;
                if (parts[0] == "speed") {
                    if (isGetter) {
                        Misc.SendChatMessageLocal(FreecamModePatch.cameraSpeed.ToString());
                    } else if (float.TryParse(parts[1], out float val)) {
                        FreecamModePatch.cameraSpeed = val;
                    } else {
                        throw new CommandException("Invalid speed", args.Index);
                    }
                } else if (parts[0] == "zoom") {
                    if (isGetter) {
                        Misc.SendChatMessageLocal(G.m_zoomIndex.ToString());
                    } else if (int.TryParse(parts[1], out int newZoomIndex)) {
                        G.m_zoomIndex = newZoomIndex;
                    } else {
                        throw new CommandException("Invalid zoom", args.Index);
                    }
                } else {
                    throw new CommandException("Unknown freecam parameter; expected either 'speed' or 'zoom'", args.Index);
                }
            } else {
                FreecamModePatch.isInFreecamMode ^= true;
                if (FreecamModePatch.isInFreecamMode) {
                    FreecamModePatch.cameraPos = G.m_player.Pos;
                }
            }
        });
        CommandManager.Register("/exportpng", opts with {
            Local = true,
        }, (args, player) => {
            string exportPath = Misc.AppendExtension(Misc.GetFullPathFromBase(
                args.Remaining == 0 ? "SavedScreen.png" : args.ArgString("file path"),
                System.IO.Path.Combine(Application.dataPath, "..")
            ), extension: ".png");
            args.ArgNone();

            Texture2D texture2D = new Texture2D(SWorld.Gs.x, SWorld.Gs.y, TextureFormat.RGB24, mipmap: false);
            var minimapInst = SSingleton<SMinimap>.Inst;

            for (int i = 0; i < SWorld.Gs.x; ++i) {
                for (int j = 0; j < SWorld.Gs.y; ++j) {
                    texture2D.SetPixel(i, j, minimapInst.GetColor(i, j, checkForFlagIsMapped: false));
                }
            }
            texture2D.Apply();
            byte[] bytes = texture2D.EncodeToPNG();
            System.IO.File.WriteAllBytes(exportPath, bytes);

            Misc.SendChatMessageLocal($"Exported world image to: \"{exportPath}\" ({bytes.Length} bytes)");
        });
        CommandManager.Register("/clock", opts with {
            TabCompleter = argIdx => argIdx == 0 ? ["pause", "resume", "morning", "night", "evening", "midday", "midnight", "lavastart", "lavaend"] : null,
        }, (args, player) => {
            string arg = args.ArgString("value").ToLowerInvariant();
            args.ArgNone();

            if (arg == "pause") {
                ClockCommandPatch.isPaused = true;
            } else if (arg == "resume") {
                ClockCommandPatch.isPaused = false;
            } else if (arg == "morning") {
                GVars.m_clock = SGame.GetNightClockHalfDuration();
            } else if (arg == "night") {
                GVars.m_clock = 1f - SGame.GetNightClockHalfDuration();
            } else if (arg == "evening") {
                GVars.m_clock = 1f - (SOutgame.Params.m_nightDuration + 120f) / (SOutgame.Params.m_dayDurationTotal * 2f);
            } else if (arg == "midday") {
                GVars.m_clock = 0.5f;
            } else if (arg == "midnight") {
                GVars.m_clock = 0f;
            } else if (arg == "lavastart") {
                GVars.m_clock = 0.45f;
            } else if (arg == "lavaend") {
                GVars.m_clock = 0.9f;
            } else if (arg.StartsWith("+") || arg.StartsWith("-")) {
                if (!TryParseClockTime(arg, out float clockDelta)) {
                    throw new CommandException("Expected delta clock time", args.Index);
                }
                GVars.m_clock = Misc.PosMod(GVars.m_clock + clockDelta, 1f);
            } else {
                if (!TryParseClockTime(arg, out float newClockTime)) {
                    throw new CommandException("Expected new clock time", args.Index);
                }
                if (newClockTime < 0f || newClockTime > 1f) {
                    throw new CommandException("Clock time must be between [0, 1]", args.Index);
                }
                GVars.m_clock = newClockTime;
            }
        });
    }
}

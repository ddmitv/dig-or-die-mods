
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace DODModAPI;

public static class Misc {
    public static void AddLocalizationText(string id, string text) {
        var dico = SSingleton<SLoc>.Inst.m_dico;
        if (dico.TryGetValue(id, out SLoc.CSentence value)) {
            throw new ArgumentException($"Localization ID collision: \"{id}\" already exists. Original text: \"{value.m_textStatic}\". New text: \"{text}\". ", nameof(id));
        }
        dico.Add(id, new SLoc.CSentence(id, text));
    }
    public static int DamerauLevenshteinDistance(string str1, string str2,
        int insertionCost = 1, int deletionCost = 1, int substitutionCost = 1, int transpositionCost = 1
    ) {
        var d = new int[str1.Length + 1, str2.Length + 1];
        for (int i = 0; i <= str1.Length; ++i) {
            d[i, 0] = i;
        }
        for (int j = 0; j <= str2.Length; ++j) {
            d[0, j] = j;
        }
        for (int i = 1; i <= str1.Length; ++i) {
            for (int j = 1; j <= str2.Length; ++j) {
                int cost = str1[i - 1] == str2[j - 1] ? 0 : substitutionCost;

                d[i, j] = Math.Min(Math.Min(
                    d[i - 1, j] + deletionCost,
                    d[i, j - 1] + insertionCost),
                    d[i - 1, j - 1] + cost
                );
                if (i > 1 && j > 1 && str1[i - 1] == str2[j - 2] && str1[i - 2] == str2[j - 1]) {
                    d[i, j] = Math.Min(
                        d[i, j],
                        d[i - 2, j - 2] + transpositionCost
                    );
                }
            }
        }
        return d[str1.Length, str2.Length];
    }
    public static string? ClosestStringMatch(string target, IEnumerable<string> sources) {
        if (sources == null) { throw new ArgumentNullException(nameof(sources)); }

        string targetLower = target.ToLowerInvariant();

        string? result = null;
        int resultDist = int.MaxValue;
        bool hasAny = false;
        foreach (string src in sources) {
            hasAny = true;
            int dist = DamerauLevenshteinDistance(src.ToLowerInvariant(), targetLower,
                insertionCost: 1, deletionCost: 2, substitutionCost: 3, transpositionCost: 3
            );
            if (dist < resultDist) {
                resultDist = dist;
                result = src;
            }
        }
        if (!hasAny) {
            throw new InvalidOperationException("source string sequence is empty");
        }
        return result;
    }

    public static int PosMod(int x, int y) {
        int remainder = x % y;
        return remainder < 0 ? remainder + y : remainder;
    }
    public static float PosMod(float x, float y) {
        float remainder = x % y;
        return remainder < 0 ? remainder + y : remainder;
    }

    public static void ArrayAppend<T>(ref T[] array, T value) {
        Array.Resize(ref array, array.Length + 1);
        array[array.Length - 1] = value;
    }

    public static string StringJoin<T>(IList<T> list, Func<T, int, string>? converter = null, string delimiter = ", ") {
        if (list is null) { return ""; };

        converter ??= (T x, int i) => x?.ToString() ?? "";

        StringBuilder sb = new(capacity: list.Count * delimiter.Length);
        for (int i = 0; i < list.Count; ++i) {
            if (i > 0) {
                sb.Append(delimiter);
            }
            sb.Append(converter(list[i], i));
        }
        return sb.ToString();
    }

    public static byte ParseByteSmart(string input) {
        if (input.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) {
            return byte.Parse(input.Substring(2), NumberStyles.HexNumber);
        }
        if (input.StartsWith("0b", StringComparison.OrdinalIgnoreCase)) {
            return Convert.ToByte(input.Substring(2), 2);
        }
        return byte.Parse(input);
    }
    public static bool TryParseByteSmart(string input, out byte result) {
        result = 0;
        if (string.IsNullOrEmpty(input)) { return false; }
        if (input.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) {
            return byte.TryParse(input.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
        }
        if (input.StartsWith("0b", StringComparison.OrdinalIgnoreCase)) {
            try {
                result = Convert.ToByte(input.Substring(2), 2);
                return true;
            } catch (OverflowException) {
                return false;
            } catch (FormatException) {
                return false;
            }
        }
        return byte.TryParse(input, out result);
    }

    public static bool TryParseBool(string str, out bool result) {
        result = false;
        if (string.IsNullOrEmpty(str)) { return false; }

        if (str == "true" || str == "True" || str == "1") {
            result = true;
            return true;
        } else if (str == "false" || str == "False" || str == "0") {
            result = false;
            return true;
        } else {
            return false;
        }
    }
    public static void SetFlag(ref uint flags, uint flag, bool value) {
        flags = value ? (flags | flag) : (flags & ~flag);
    }

    public static bool IsInWorld(int i, int j) {
        return i >= 0 && j >= 0 && i < SWorld.Gs.x && j < SWorld.Gs.y;
    }
    public static bool IsInWorld(int2 pos) {
        return pos.x >= 0 && pos.y >= 0 && pos.x < SWorld.Gs.x && pos.y < SWorld.Gs.y;
    }

    public static void Swap<T>(ref T left, ref T right) {
        T temp = left;
        left = right;
        right = temp;
    }

    public static void NormalizeBounds(ref int2 p1, ref int2 p2) {
        if (p1.x > p2.x) { Swap(ref p2.x, ref p1.x); }
        if (p1.y > p2.y) { Swap(ref p2.y, ref p1.y); }
    }

    public static int BinarySearch<T>(List<T> list, T item, Func<T, T, int> comparer) {
        int min = 0;
        int max = list.Count - 1;
        while (min <= max) {
            int mid = min + ((max - min) >> 1);
            int cmp = comparer(item, list[mid]);
            if (cmp == 0) {
                return mid;
            }
            if (cmp < 0) {
                max = mid - 1;
            } else {
                min = mid + 1;
            }
        }
        return ~min;
    }

    public static void InsertSortedList<T>(List<T> list, T item, Func<T, T, int> comparer) {
        int index = BinarySearch(list, item, comparer);
        if (index < 0) { index = ~index; }
        list.Insert(index, item);
    }

    public static void SendChatMessageLocal(string msg) {
        SSingletonScreen<SScreenHudChat>.Inst.AddChatMessage_Local(null, msg);
    }
    public static void SendChatMessageLocalNL(string rawMsg) {
        foreach (var msg in rawMsg.Split('\n')) {
            SSingletonScreen<SScreenHudChat>.Inst.AddChatMessage_Local(null, msg);
        }
    }

    public static RectInt ClampRect(RectInt rect, int minX, int minY, int maxX, int maxY) {
        int x = Math.Max(rect.x, minX);
        int y = Math.Max(rect.y, minY);

        int xMax = Math.Min(rect.xMax, maxX);
        int yMax = Math.Min(rect.yMax, maxY);

        int width = Math.Max(0, xMax - x);
        int height = Math.Max(0, yMax - y);

        return new RectInt(x, y, width, height);
    }

    public static string GetFullPathFromBase(string path, string basePath) {
        if (Path.IsPathRooted(path)) {
            return Path.GetFullPath(path);
        }
        return Path.GetFullPath(Path.Combine(basePath, path));
    }
    public static string AppendExtension(string path, string extension) {
        if (Path.HasExtension(path)) {
            return path;
        }
        return path + extension;
    }

    public static double Hypot(double x, double y) => Math.Sqrt(x * x + y * y);
    public static float Hypot(float x, float y) => Mathf.Sqrt(x * x + y * y);
    public static double Hypot(int2 vec) => Math.Sqrt(vec.x * vec.x + vec.y * vec.y);

    public static float Sqr(float x) => x * x;
    public static byte Sqr(byte x) => (byte)(x * x);
    public static sbyte Sqr(sbyte x) => (sbyte)(x * x);
    public static short Sqr(short x) => (short)(x * x);
    public static ushort Sqr(ushort x) => (ushort)(x * x);
    public static int Sqr(int x) => x * x;
    public static uint Sqr(uint x) => x * x;
    public static long Sqr(long x) => x * x;
    public static ulong Sqr(ulong x) => x * x;

    public static float Cub(float x) => x * x * x;
    public static double Cub(double x) => x * x * x;
    public static byte Cub(byte x) => (byte)(x * x * x);
    public static sbyte Cub(sbyte x) => (sbyte)(x * x * x);
    public static short Cub(short x) => (short)(x * x * x);
    public static ushort Cub(ushort x) => (ushort)(x * x * x);
    public static int Cub(int x) => x * x * x;
    public static uint Cub(uint x) => x * x * x;
    public static long Cub(long x) => x * x * x;
    public static ulong Cub(ulong x) => x * x * x;

    public static bool TryParseBinary(string str, out long result) {
        result = 0;
        if (string.IsNullOrEmpty(str)) { return false; }

        ulong tmp = 0;
        foreach (char ch in str) {
            if (ch != '0' && ch != '1') { return false; }
            tmp = (tmp << 1) | (uint)(ch - '0');
            if (tmp > long.MaxValue) {
                return false;
            }
        }
        result = (long)tmp;
        return true;
    }

    public static void AddLava(ref CCell cell, float lavaQuantity) {
        if (!cell.IsPassable()) { return; }

        if (!cell.IsLava()) {
            cell.m_water = 0;
        }
        cell.m_water += lavaQuantity;
        cell.SetFlag(CCell.Flag_IsLava, true);
    }

    public static sbyte CeilDiv(sbyte dividend, sbyte divisor) => (sbyte)((dividend + divisor - 1) / divisor);
    public static byte CeilDiv(byte dividend, byte divisor) => (byte)((dividend + divisor - 1) / divisor);
    public static short CeilDiv(short dividend, short divisor) => (short)((dividend + divisor - 1) / divisor);
    public static ushort CeilDiv(ushort dividend, ushort divisor) => (ushort)((dividend + divisor - 1) / divisor);
    public static int CeilDiv(int dividend, int divisor) => (int)((long)dividend + divisor - 1) / divisor;
    public static long CeilDiv(long dividend, long divisor) => (dividend / divisor) + (dividend % divisor == 0 ? 0 : 1);
    public static uint CeilDiv(uint dividend, uint divisor) => (uint)((ulong)dividend + divisor - 1) / divisor;
    public static ulong CeilDiv(ulong dividend, ulong divisor) => (dividend / divisor) + (dividend % divisor == 0 ? 0UL : 1UL);

    public static Fn CreateNonVirtualDelegate<Fn, T>(T self, MethodInfo methodInfo)
        where Fn : Delegate
        where T : class
    {
        if (methodInfo is null) { throw new ArgumentNullException(nameof(methodInfo)); }
        RuntimeHelpers.PrepareMethod(methodInfo.MethodHandle);
        var funcPtr = methodInfo.MethodHandle.GetFunctionPointer();
        try {
            return (Fn)Activator.CreateInstance(typeof(Fn), self, funcPtr);
        } catch (ArgumentException ex) {
            throw new InvalidOperationException($"Delegate signature mismatch. Delegate: {typeof(Fn).Name}, Method: {methodInfo.Name}", ex);
        }
    }

    public static T ShallowClone<T>(T source) where T : class {
        var memberwiseCloneMethod = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return (T)memberwiseCloneMethod.Invoke(source, null);
    }

    public static RectInt MakeCenterRectInt(int2 center, int range) => new(center.x - range, center.y - range, range << 1, range << 1);
    public static RectInt MakeCenterRectInt(int x, int y, int range) => new(x - range, y - range, range << 1, range << 1);
    public static RectInt MakeMinMaxRectInt(int xMin, int yMin, int xMax, int yMax) => new(xMin, yMin, xMax - xMin, yMax - yMin);

    public static class WorldIntRects {
        public static RectInt GridRectCamInt => new(13, 13, SWorld.Gs.x - 26, SWorld.Gs.y - 26);
        public static RectInt GridRectM2Int => new(2, 2, SWorld.Gs.x - 4, SWorld.Gs.y - 4);
        public static RectInt GridRectInt => new(0, 0, SWorld.Gs.x, SWorld.Gs.y);
    }

    public static RectInt RectIntIntersection(RectInt a, RectInt b) {
        int x = Math.Max(a.x, b.x);
        int y = Math.Max(a.y, b.y);

        int xMax = Math.Min(a.xMax, b.xMax);
        int yMax = Math.Min(a.yMax, b.yMax);

        int width = Math.Max(0, xMax - x);
        int height = Math.Max(0, yMax - y);

        return new RectInt(x, y, width, height);
    }

    public static Vector2 RotateRight(Vector2 vec) {
        return new Vector2(vec.y, -vec.x);
    }
    public static Vector2 RotateLeft(Vector2 vec) {
        return new Vector2(-vec.y, vec.x);
    }

    public static float EaseOutQuad(float x) => 1f - Sqr(1f - x);
    public static float EaseOutCubic(float x) => 1f - Cub(1f - x);
}

public static class CollectionHelpers {
    public static void Partition<T>(List<T> source, Func<T, bool> pred, out List<T> matching, out List<T> nonMatching) {
        if (source is null) { throw new ArgumentNullException(nameof(source)); }
        if (pred is null) { throw new ArgumentNullException(nameof(pred)); }

        matching = [];
        nonMatching = [];

        for (int i = 0; i < source.Count; ++i) {
            if (pred(source[i])) {
                matching.Add(source[i]);
            } else {
                nonMatching.Add(source[i]);
            }
        }
    }
}

public static class CellFlags {
    public const uint CustomData0 = 1U;
    public const uint CustomData1 = 2U;
    public const uint CustomData2 = 4U;
    public const uint IsXReversed = 16U;
    public const uint IsBurning = 32U;
    public const uint IsMapped = 64U;
    public const uint BackWall_0 = 256U;
    public const uint BgSurface_0 = 512U;
    public const uint BgSurface_1 = 1024U;
    public const uint BgSurface_2 = 2048U;
    public const uint WaterFall = 4096U;
    public const uint StreamLFast = 8192U;
    public const uint StreamRFast = 16384U;
    public const uint IsLava = 32768U;
    public const uint HasWireRight = 65536U;
    public const uint HasWireTop = 131072U;
    public const uint ElectricAlgoState = 262144U;
    public const uint IsPowered = 524288U;

    public const uint CustomDataMask = CustomData0 | CustomData1 | CustomData2;
    public const uint BgSurfaceMask = BgSurface_0 | BgSurface_1 | BgSurface_2;
    public const uint BgSurfaceAndBackwallMask = BackWall_0 | BgSurface_0 | BgSurface_1 | BgSurface_2;
    public const uint HasWireMask = HasWireRight | HasWireTop;
    public const uint StreamMask = StreamLFast | StreamRFast;

    public const int CustomDataBitShift = 0;
    public const int BgSurfaceBitShift = 9;
    public const int BackWallBitShift = 8;
    public const int WireMaskBitShift = 16;

    public const uint BgNoneFlag = 0;
    public const uint BgDirtFlag = BgSurface_0;
    public const uint BgRockFlag = BgSurface_1;
    public const uint BgGranitFlag = BgSurface_0 | BgSurface_1;
    public const uint BgCrystalFlag = BgSurface_2;
    public const uint BgLavaFlag = BgSurface_0 | BgSurface_2;
    public const uint BgOrganicFlag = BgSurface_1 | BgSurface_2;

    public const uint AllKnownFlags = CustomData0 | CustomData1 | CustomData2 | IsXReversed | IsBurning | IsMapped | BackWall_0 | BgSurface_0 | BgSurface_1 | BgSurface_2 | WaterFall | StreamLFast | StreamRFast | IsLava | HasWireRight | HasWireTop | ElectricAlgoState | IsPowered;
    public const uint UnknownFlag1 = 8U;
    public const uint UnknownFlag2 = 128U;
}

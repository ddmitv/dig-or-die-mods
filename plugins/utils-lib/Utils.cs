using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace ModUtils;

public static class Utils {
    public static void ForEachInCircleClamped(int range, int2 pos, Action<int, int> fn) {
        int sqrRange = range * range;
        int minX = Math.Max(pos.x - range, 0), maxX = Math.Min(pos.x + range, SWorld.Gs.x - 1);
        int minY = Math.Max(pos.y - range, 0), maxY = Math.Min(pos.y + range, SWorld.Gs.y - 1);
        for (int i = minX; i <= maxX; ++i) {
            for (int j = minY; j <= maxY; ++j) {
                int2 relative = new int2(i, j) - pos;
                if (relative.sqrMagnitude <= sqrRange) {
                    fn(i, j);
                }
            }
        }
    }
    public static int2 FindInCircleClamped(int range, int2 pos, Func<int, int, bool> fn) {
        int sqrRange = range * range;
        int minX = Math.Max(pos.x - range, 0), maxX = Math.Min(pos.x + range, SWorld.Gs.x - 1);
        int minY = Math.Max(pos.y - range, 0), maxY = Math.Min(pos.y + range, SWorld.Gs.y - 1);
        for (int i = minX; i <= maxX; ++i) {
            for (int j = minY; j <= maxY; ++j) {
                int2 relative = new int2(i, j) - pos;
                if (relative.sqrMagnitude <= sqrRange) {
                    if (fn(i, j)) {
                        return new int2(i, j);
                    }
                }
            }
        }
        return int2.negative;
    }
    public static bool IsValidCell(int x, int y) {
        return x >= 0 && y >= 0 && x < SWorld.Gs.x && y < SWorld.Gs.y;
    }
    public static void AddLava(ref CCell cell, float lavaQuantity) {
        if (!cell.IsPassable()) { return; }

        if (!cell.IsLava()) {
            cell.m_water = 0;
        }
        cell.m_water += lavaQuantity;
        cell.SetFlag(CCell.Flag_IsLava, true);
    }
    public static void SetUnitBurningAround(Vector2 center, float radius) {
        foreach (CUnit unit in SUnits.Units) {
            if (unit is null || !unit.IsAlive() || unit.m_uDesc.m_immuneToFire) {
                continue;
            }

            if ((unit.PosCenter - center).sqrMagnitude <= radius * radius
                && SMiscCols.CheckCol_SegmentGround(center, unit.PosCenter) == Vector2.zero) {
                unit.m_burnStartTime = GVars.SimuTime;
            }
        }
    }
    public static T MakeMemberwiseClone<T>(T obj) where T : class {
        return (T)AccessTools.Method(typeof(T), "MemberwiseClone").Invoke(obj, []);
    }

    public static byte[] ReadAllBytes(Stream stream) {
        if (stream.CanSeek && stream.Position <= stream.Length) {
            byte[] buffer = new byte[stream.Length - stream.Position];
            _ = stream.Read(buffer, offset: 0, count: buffer.Length);
            return buffer;
        } else {
            byte[] buffer = new byte[4 * 4096];
            using MemoryStream ms = new MemoryStream();

            int readNum;
            while ((readNum = stream.Read(buffer, offset: 0, count: buffer.Length)) > 0) {
                ms.Write(buffer, offset: 0, count: readNum);
            }

            return ms.ToArray();
        }
    }
    public static void RunStaticConstructor(Type type) {
        System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(type.TypeHandle);
    }
    public static void EvaporateWaterAround(int range, int2 pos, float evaporationRate) {
        Utils.ForEachInCircleClamped(range, pos, (int x, int y) => {
            ref var cell = ref SWorld.Grid[x, y];
            if (!cell.IsLava() && cell.m_water > 0) {
                cell.m_water = Mathf.Max(0f, cell.m_water - SMain.SimuDeltaTime * evaporationRate);
            }
        });
    }
    public static int HashCombine(int first, int second) {
        unchecked {
            uint x = (uint)first + 0x9e3779b9 + (uint)second;
            x ^= x >> 16;
            x *= 0x21F0AAAD;
            x ^= x >> 15;
            x *= 0x735A2D97;
            x ^= x >> 15;
            return (int)x;
        }
    }
    public static int GetPluginHash(BaseUnityPlugin plugin) {
        var metadata = MetadataHelper.GetMetadata(plugin);
        return HashCombine(metadata.GUID.GetHashCode(), metadata.Version.GetHashCode());
    }
    public static void UniqualizeVersionBuild(ref int versionBuild, BaseUnityPlugin plugin) {
        versionBuild = Utils.HashCombine(versionBuild, Utils.GetPluginHash(plugin));
        if (versionBuild <= 1000) {
            versionBuild += 124629556;
        }
    }
    public static void ArrayAppend<T>(ref T[] array, T value) {
        Array.Resize(ref array, array.Length + 1);
        array[array.Length - 1] = value;
    }
    public static int CeilDiv(int x, int y) {
        return (x + y - 1) / y;
    }
    public static void DoShockWave(Vector2 center, float radius, float damage, float knockbackTarget) {
        float radiusSqr = radius * radius;
        foreach (CUnit unit in SUnits.Units) {
            if (unit == null || !unit.IsAlive()) { continue; }
            if ((unit.PosCenter - center).sqrMagnitude > radiusSqr) { continue; }

            var distanceFactor = Mathf.Clamp01(1f - Utils.Sqr((unit.PosCenter - center).magnitude / radius));

            var appliedDamage = Mathf.Max(1f, damage * distanceFactor * unit.GetArmorMult() - unit.GetArmor());
            unit.Damage(appliedDamage, attacker: null, showDamage: true, damageCause: "");
            unit.Push((unit.PosCenter - center).normalized * knockbackTarget * distanceFactor);
        }
    }
    public static bool IsInWorld(int i, int j) {
        return i >= 0 && j >= 0 && i < SWorld.Gs.x && j < SWorld.Gs.y;
    }
    public static bool IsInWorld(int2 pos) {
        return IsInWorld(pos.x, pos.y);
    }
    public static void AddChatMessageLocal(string msg) {
        SSingletonScreen<SScreenHudChat>.Inst.AddChatMessage_Local(null, msg);
    }
    public static void AddChatMessageLocalNL(string rawMsg) {
        foreach (var msg in rawMsg.Split('\n')) {
            SSingletonScreen<SScreenHudChat>.Inst.AddChatMessage_Local(null, msg);
        }
    }
    public static int Clamp(int val, int min, int max) {
        return (val < min) ? min : (val > max) ? max : val;
    }

    public static bool ParseBool(string str) {
        if (str is null) { throw new ArgumentNullException(nameof(str)); }
        return str switch {
            "true" => true,
            "True" => true,
            "1" => true,
            "false" => false,
            "False" => false,
            "0" => false,
            _ => throw new FormatException("Failed to parse bool")
        };
    }
    public static void SetFlag(ref uint flags, uint flag, bool value) {
        flags = (!value) ? (flags & ~flag) : (flags | flag);
    }
    public static Color24 ParseColor24(string str) {
        string[] valuesStr = str.Split(':');
        if (valuesStr.Length == 1) {
            return new Color24(uint.Parse(str, System.Globalization.NumberStyles.HexNumber));
        }
        if (valuesStr.Length != 3) { throw new FormatException("Expected exact 3 values for Color24"); }
        return new Color24(byte.Parse(valuesStr[0]), byte.Parse(valuesStr[1]), byte.Parse(valuesStr[2]));
    }
    public static void Swap<T>(ref T left, ref T right) {
        T temp = left;
        left = right;
        right = temp;
    }
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
    public static int LevenshteinDistance(string source, string target) {
        int n = source.Length;
        int m = target.Length;
        int[,] d = new int[n + 1, m + 1];

        if (n == 0) {
            return m;
        }

        if (m == 0) {
            return n;
        }

        for (int i = 0; i <= n; d[i, 0] = i++) {}
        for (int j = 0; j <= m; d[0, j] = j++) {}

        for (int i = 1; i <= n; i++) {
            for (int j = 1; j <= m; j++) {
                int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }
        return d[n, m];
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
    public static string ClosestStringMatch(string target, IEnumerable<string> sources) {
        if (!sources.Any()) { throw new InvalidOperationException("source string sequence is empty"); }

        string result = null;
        int resultDist = int.MaxValue;
        foreach (string src in sources) {
            int dist = DamerauLevenshteinDistance(src.ToLowerInvariant(), target.ToLowerInvariant(),
                insertionCost: 1, deletionCost: 2, substitutionCost: 3, transpositionCost: 3
            );
            if (dist < resultDist) {
                resultDist = dist;
                result = src;
            }
        }
        return result;
    }
    public static double Hypot(double x, double y) {
        return Math.Sqrt(x * x + y * y);
    }
    public static float Hypot(float x, float y) {
        return Mathf.Sqrt(x * x + y * y);
    }
    public static double Hypot(int2 vec) {
        return Math.Sqrt(vec.x * vec.x + vec.y * vec.y);
    }
    public static string DumpAllFieldsAndProps(object obj) {
        const BindingFlags allFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.GetField | BindingFlags.SetField | BindingFlags.GetProperty | BindingFlags.SetProperty;
        StringBuilder sb = new();
        var type = obj.GetType();
        foreach (var field in type.GetFields(allFlags)) {
            sb.AppendLine($"{type.Name}.{field.Name} = {field.GetValue(obj)}");
        }
        foreach (var prop in type.GetProperties(allFlags)) {
            try { sb.AppendLine($"{type.Name}.{prop.Name} = {prop.GetValue(obj, null)}"); }
            catch { }
        }
        sb.Length -= 1; // remove newline character from last iteration
        return sb.ToString();
    }
    public static Fn GetBaseMethod<Fn, T>(T self, string methodName) {
        return (Fn)Activator.CreateInstance(typeof(Fn), self, typeof(T).GetMethod(methodName).MethodHandle.GetFunctionPointer());
    }
    public static void AddLocalizationText(string id, string text) {
        SSingleton<SLoc>.Inst.m_dico.Add(id, new SLoc.CSentence(id, text));
    }
    public static int PosMod(int x, int y) {
        int remainder = x % y;
        return remainder < 0 ? remainder + y : remainder;
    }
    public static float PosMod(float x, float y) {
        float remainder = x % y;
        return remainder < 0 ? remainder + y : remainder;
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
    public static T Exchange<T>(ref T obj, T newValue) {
        T oldValue = obj;
        obj = newValue;
        return oldValue;
    }
    public static RectInt ClampRect(RectInt rect, int minX, int minY, int maxX, int maxY) {
        return new RectInt(
            Math.Max(rect.x, minX), Math.Max(rect.y, minY),
            Math.Min(rect.width, maxX), Math.Min(rect.height, maxY)
        );
    }
    public static RectInt CreateCenterRectInt(int2 center, int range) {
        return new RectInt(center.x - range, center.y - range, range << 1, range << 1);
    }
    public static RectInt CreateCenterRectInt(int x, int y, int range) {
        return new RectInt(x - range, y - range, range << 1, range << 1);
    }
    public static RectInt CreateMinMaxRectInt(int xMin, int yMin, int xMax, int yMax) {
        return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
    }
    public static RectInt GridRectCamInt => CreateMinMaxRectInt(13, 13, SWorld.Gs.x - 26, SWorld.Gs.y - 26);
    public static RectInt GridRectM2Int => CreateMinMaxRectInt(2, 2, SWorld.Gs.x - 4, SWorld.Gs.y - 4);
    public static RectInt GridRectInt => CreateMinMaxRectInt(0, 0, SWorld.Gs.x, SWorld.Gs.y);

    public static float Sqr(float x) { return x * x; }
    public static double Sqr(double x) { return x * x; }
    public static byte Sqr(byte x) { return (byte)(x * x); }
    public static sbyte Sqr(sbyte x) { return (sbyte)(x * x); }
    public static short Sqr(short x) { return (short)(x * x); }
    public static ushort Sqr(ushort x) { return (ushort)(x * x); }
    public static int Sqr(int x) { return x * x; }
    public static uint Sqr(uint x) { return x * x; }
    public static long Sqr(long x) { return x * x; }
    public static ulong Sqr(ulong x) { return x * x; }

    public static float Cub(float x) { return x * x * x; }
    public static double Cub(double x) { return x * x * x; }
    public static byte Cub(byte x) { return (byte)(x * x * x); }
    public static sbyte Cub(sbyte x) { return (sbyte)(x * x * x); }
    public static short Cub(short x) { return (short)(x * x * x); }
    public static ushort Cub(ushort x) { return (ushort)(x * x * x); }
    public static int Cub(int x) { return x * x * x; }
    public static uint Cub(uint x) { return x * x * x; }
    public static long Cub(long x) { return x * x * x; }
    public static ulong Cub(ulong x) { return x * x * x; }

    public static float EaseOutQuad(float x) {
        return 1f - Sqr(1f - x);
    }
    public static float EaseOutCubic(float x) {
        return 1f - Cub(1f - x);
    }

    public static bool IsStringNullOrWhiteSpace(string value) {
        if (value == null) {
            return true;
        }

        for (int i = 0; i < value.Length; i++) {
            if (!char.IsWhiteSpace(value[i])) {
                return false;
            }
        }
        return true;
    }

    public static bool TryParseIPEndPoint(string s, out IPEndPoint result) {
        // https://github.com/dotnet/runtime/blob/9d5a6a9aa463d6d10b0b0ba6d5982cc82f363dc3/src/libraries/System.Net.Primitives/src/System/Net/IPEndPoint.cs#L97C13-L127C26
        const int MaxPort = 0x0000FFFF;

        int addressLength = s.Length;
        int lastColonPos = s.LastIndexOf(':');

        if (lastColonPos > 0) {
            if (s[lastColonPos - 1] == ']') {
                addressLength = lastColonPos;
            }
            else if (s.Substring(0, lastColonPos).LastIndexOf(':') == -1) {
                addressLength = lastColonPos;
            }
        }
        if (IPAddress.TryParse(s.Substring(0, addressLength), out IPAddress address)) {
            uint port = 0;
            if (addressLength == s.Length ||
                (uint.TryParse(s.Substring(addressLength + 1), NumberStyles.None, CultureInfo.InvariantCulture, out port) && port <= MaxPort)) {
                result = new IPEndPoint(address, (int)port);
                return true;
            }
        }
        result = null;
        return false;
    }
}

public sealed class WeakDictionary<TKey, TValue> where TKey : class {
    private readonly Dictionary<int, List<KeyValuePair<WeakReference, TValue>>> _buckets = [];
    private int _cleanupCounter = 0;
    private const int CleanupInterval = 100;

    public TValue Get(TKey key) {
        if (key is null) { throw new ArgumentNullException(nameof(key)); }

        if (!TryGet(key, out TValue value)) {
            return default;
        }
        return value;
    }
    public bool TryGet(TKey key, out TValue value) {
        if (key is null) { throw new ArgumentNullException(nameof(key)); }

        Cull();
        int hashCode = key.GetHashCode();
        if (!_buckets.TryGetValue(hashCode, out var bucket)) {
            value = default;
            return false;
        }

        for (int i = bucket.Count - 1; i >= 0; --i) {
            var weakRef = bucket[i].Key;
            if (ReferenceEquals(weakRef.Target, key)) {
                value = bucket[i].Value;
                return true;
            }
        }
        value = default;
        return false;
    }

    public void Set(TKey key, TValue value) {
        if (key is null) { throw new ArgumentNullException(nameof(key)); }

        Cull();
        int hashCode = key.GetHashCode();
        if (!_buckets.TryGetValue(hashCode, out var bucket)) {
            bucket = [new(key: new WeakReference(key), value: value)];
            _buckets[hashCode] = bucket;
            return;
        }

        for (int i = bucket.Count - 1; i >= 0; --i) {
            var weakRef = bucket[i].Key;
            if (ReferenceEquals(weakRef.Target, key)) {
                bucket[i] = new(key: weakRef, value: value);
                return;
            }
        }
        bucket.Add(new(key: new WeakReference(key), value: value));
    }
    public TValue TrySet(TKey key, TValue defaultValue) {
        if (key is null) { throw new ArgumentNullException(nameof(key)); }

        Cull();
        int hashCode = key.GetHashCode();
        if (!_buckets.TryGetValue(hashCode, out var bucket)) {
            bucket = [new(key: new WeakReference(key), value: defaultValue)];
            _buckets[hashCode] = bucket;
            return defaultValue;
        }
        for (int i = bucket.Count - 1; i >= 0; --i) {
            var weakRef = bucket[i].Key;
            if (ReferenceEquals(weakRef.Target, key)) {
                return bucket[i].Value;
            }
        }
        bucket.Add(new(key: new WeakReference(key), value: defaultValue));
        return defaultValue;
    }

    private void Cull() {
        _cleanupCounter += 1;
        if (_cleanupCounter < CleanupInterval) { return; }
        _cleanupCounter = 0;

        List<int> emptyBuckets = [];
        foreach (var kvp in _buckets) {
            var bucket = kvp.Value;
            for (int i = bucket.Count - 1; i >= 0; --i) {
                if (!bucket[i].Key.IsAlive) {
                    bucket.RemoveAt(i);
                }
            }
            if (bucket.Count == 0) {
                emptyBuckets.Add(kvp.Key);
            }
        }
        foreach (int bucketKey in emptyBuckets) {
            _buckets.Remove(bucketKey);
        }
    }
}

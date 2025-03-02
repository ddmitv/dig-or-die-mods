using BepInEx;
using HarmonyLib;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ModUtils;

public static class Utils {
    public static void ApplyInCircle(int range, int2 pos, Action<int, int> fn) {
        int sqrRange = range * range;
        for (int i = pos.x - range; i <= pos.x + range; ++i) {
            for (int j = pos.y - range; j <= pos.y + range; ++j) {
                int2 relative = new int2(i, j) - pos;
                if (relative.sqrMagnitude <= sqrRange) {
                    fn(i, j);
                }
            }
        }
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
    public static T MakeMemberwiseClone<T>(T obj) where T : class {
        return (T)AccessTools.Method(typeof(T), "MemberwiseClone").Invoke(obj, []);
    }
    public static byte[] ReadAllBytes(Stream stream) {
        byte[] buffer = new byte[16 * 1024];
        using MemoryStream ms = new MemoryStream();

        int readNum;
        while ((readNum = stream.Read(buffer, offset: 0, count: buffer.Length)) > 0) {
            ms.Write(buffer, offset: 0, count: readNum);
        }
        return ms.ToArray();
    }
    public static void RunStaticConstructor(Type type) {
        System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(type.TypeHandle);
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
    public static FieldInfo StaticField(Type type, string fieldName) {
        return type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
    }
    public static void DoShockWave(Vector2 center, float radius, float damage, float knockbackTarget) {
        float radiusSqr = radius * radius;
        foreach (CUnit unit in SUnits.Units) {
            if (unit == null || !unit.IsAlive()) { continue; }
            if ((unit.PosCenter - center).sqrMagnitude > radiusSqr) { continue; }

            var distanceFactor = Mathf.Clamp01(1f - Mathf.Pow((unit.PosCenter - center).magnitude / radius, 2f));

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
            return new Color24(uint.Parse(str));
        }
        if (valuesStr.Length != 3) { throw new FormatException("Expected exact 3 values for Color24"); }
        return new Color24(byte.Parse(valuesStr[0]), byte.Parse(valuesStr[1]), byte.Parse(valuesStr[2]));
    }
    public static void Swap<T>(ref T left, ref T right) {
        T temp = left;
        left = right;
        right = temp;
    }
}

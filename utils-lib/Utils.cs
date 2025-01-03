using BepInEx;
using HarmonyLib;
using System;
using System.IO;

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
}

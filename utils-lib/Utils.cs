using HarmonyLib;
using System;
using System.IO;
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
}

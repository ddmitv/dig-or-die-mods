
using GameEngine;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

public static class Utils {
    // public static void SetBit(byte[] byteArray, uint bitIndex, bool value) {
    //     byte bitMask = (byte)(1 << (int)(bitIndex & 7u));
    //     ref byte segment = ref byteArray[bitIndex >> 3];
    //     if (value) {
    //         segment |= bitMask;
    //     } else {
    //         segment &= (byte)~bitMask;
    //     }
    // }
    public static void SetBit(byte[] byteArray, uint bitIndex, bool value) {
        uint idx = bitIndex / 8;
        byte b = (byte)(1 << (int)(bitIndex % 8));
        byteArray[idx] = (byte)(value ? (byteArray[idx] | b) : (byteArray[idx] & ~b));
    }
    public static float Clamp(float value, float min, float max) {
        if (value < min) {
            value = min;
        } else if (value > max) {
            value = max;
        }
        return value;
    }
    public static float Sqr(float x) => x * x;
    public static double Sqr(double x) => x * x;

    public static float MoveTowards(float current, float target, float maxDelta) {
        if (Math.Abs(target - current) <= maxDelta) {
            return target;
        }
        return current + Math.Sign(target - current) * maxDelta;
    }

    public static void Fill<T>(List<T> list, T value) {
        CollectionsMarshal.AsSpan(list).Fill(value);
    }
    public static void FillWithDefault<T>(List<T> list) {
        CollectionsMarshal.AsSpan(list).Clear();
    }

    public static float Clamp01(float value) {
        if (value < 0f) { return 0f; }
        if (value > 1f) { return 1f; }
        return value;
    }

    private static readonly Dictionary<ushort, float> m_randomRates = [];

    public static bool GetRandomCorrected(float chance, short id, CUnit? attacker) {
        if (chance >= 1f) {
            return true;
        }
        chance = Clamp01(chance);
        int attackerId = (attacker is not CUnitPlayer) ? -1 : attacker.m_id;
        ushort hash = (ushort)((101149 * id + 150991 * attackerId) % 65535);
        if (!m_randomRates.TryGetValue(hash, out float correctedChance)) {
            correctedChance = chance;
        }
        bool isSuccessful = GameEngine.Random.Float() < correctedChance;
        m_randomRates[hash] = correctedChance + 0.5f * (isSuccessful ? -(1f - chance) : chance);
        return isSuccessful;
    }

    public static int PosMod(int x, int y) {
        int remainder = x % y;
        return remainder < 0 ? remainder + y : remainder;
    }
    public static float PosMod(float x, float y) {
        float remainder = x % y;
        return remainder < 0 ? remainder + y : remainder;
    }

    // public static Vector2 CheckCol_UnitGround(CUnit unit, Vector2 newPos, ref int2 blockCol) {
    //     if (newPos == unit.m_pos) {
    //         return unit.m_pos;
    //     }
    //     Vector2 pos = unit.m_pos;
    //     Vector2 vector = newPos - unit.m_pos;
    //     Vector2 size = unit.UDesc.m_size;
    //     blockCol = int2.negative;
    //     bool flag = vector.x > 0f;
    //     float num = unit.m_pos.x + (float)((!flag) ? (-1) : 1) * 0.5f * size.x;
    //     int num2 = ((!flag) ? ((int)num - 1) : (int)MathF.Ceiling(num));
    //     int num3 = ((!flag) ? ((int)(num + vector.x) - 1) : (int)MathF.Ceiling(num + vector.x));
    //     bool flag2 = unit is CUnitBoss && SMiscCols.CheckCol_CellsGround_LineByLine((int)(newPos.x - 0.5f * size.x), Mathf.CeilToInt(newPos.x + 0.5f * size.x), (int)(pos.y + size.y + 1f), Mathf.CeilToInt(pos.y + size.y + 2.1f), false, true) == int2.negative;
    //     int num4 = (int)(pos.y + ((!flag2) ? 0f : 1.1f));
    //     int num5 = (int)MathF.Ceiling(pos.y + size.y);
    //     int2 @int = SMiscCols.CheckCol_CellsGround_ColByCol(num2, num3, num4, num5, false);
    //     if (@int != int2.negative) {
    //         newPos.x = (float)@int.x + ((!flag) ? (1f + 0.501f * size.x) : (-0.501f * size.x));
    //         blockCol = @int;
    //     }
    //     bool flag3 = vector.y > 0f;
    //     float num6 = unit.m_pos.y + ((!flag3) ? 0f : size.y);
    //     float num7 = num6 + ((!flag2) ? 0f : 1.1f);
    //     num2 = (int)(newPos.x - 0.5f * size.x);
    //     num3 = (int)MathF.Ceiling(newPos.x + 0.5f * size.x);
    //     num4 = ((!flag3) ? ((int)num7 - 1) : (int)MathF.Ceiling(num7));
    //     num5 = ((!flag3) ? ((int)(num6 + vector.y) - 1) : (int)MathF.Ceiling(num6 + vector.y));
    //     CUnitMonster? cunitMonster = unit as CUnitMonster;
    //     bool flag4 = !flag3 && !unit.UDesc.m_skipColsOnPlatforms && (cunitMonster == null || !cunitMonster.IsLastPathfindSuccessful() || cunitMonster.m_lookingDirection.y >= 0f) && (cunitMonster == null || cunitMonster.m_lookingDirection.y >= -Math.Abs(cunitMonster.m_lookingDirection.x));
    //     @int = SMiscCols.CheckCol_CellsGround_LineByLine(num2, num3, num4, num5, flag4, false);
    //     if (@int != int2.negative) {
    //         newPos.y = (float)@int.y + ((!flag3) ? 1f : (-size.y));
    //         if (blockCol == int2.negative) {
    //             blockCol = @int;
    //         }
    //     }
    //     return new Vector2(newPos.x, newPos.y);
    // }

    public static Vector2 CheckCol_SegmentGround(Vector2 A, Vector2 B, bool checkBCell = true, bool returnCellCenter = false) {
        if (!World.IsInRectM2(A)) {
            return A;
        }
        int2 @int = (int2)A;
        int2 int2 = (int2)B;
        if (@int == int2) {
            return (!World.Grid[@int.x, @int.y].IsPassable() && checkBCell) ? A : Vector2.zero;
        }
        Vector2 vector = B - A;
        float num = 1f;
        float num2 = 1f;
        float num3 = 1f;
        float num4 = 1f;
        int num5 = 0;
        int i = 0;
        while (i < 1000) {
            num5++;
            if (!World.IsInRectM2(@int) || !World.Grid[@int.x, @int.y].IsPassable()) {
                if (returnCellCenter) {
                    return new Vector2(@int.x + 0.5f, @int.y + 0.5f);
                }
                Vector2 vector2 = Vector2.zero;
                float num6 = vector.x / vector.y;
                if (num <= 0f) {
                    vector2.y = num4;
                    vector2.x = (vector2.y - A.y) * num6 + A.x;
                }
                if (num2 <= 0f) {
                    vector2.x = num3;
                    vector2.y = (vector2.x - A.x) / num6 + A.y;
                }
                if (num2 > 0f && num > 0f) {
                    vector2 = A;
                }
                return vector2;
            } else {
                num4 = @int.y + ((vector.y <= 0f) ? 0 : 1);
                float num7 = num4 - A.y;
                num = (vector.x * num7 - vector.y * (@int.x - A.x)) * (vector.x * num7 - vector.y * ((@int.x + 1) - A.x));
                if (vector.y * (B.y - num4) <= 0f) {
                    num = 1f;
                }
                num3 = (@int.x + ((vector.x <= 0f) ? 0 : 1));
                float num8 = num3 - A.x;
                num2 = (vector.x * (@int.y - A.y) - vector.y * num8) * (vector.x * ((@int.y + 1) - A.y) - vector.y * num8);
                if (vector.x * (B.x - num3) <= 0f) {
                    num2 = 1f;
                }
                if (num <= 0f) {
                    @int.y += (vector.y <= 0f) ? -1 : 1;
                }
                if (num2 <= 0f) {
                    @int.x += (vector.x <= 0f) ? -1 : 1;
                }
                if (num > 0f && num2 > 0f) {
                    break;
                }
                if ((Vector2)@int == B && !checkBCell) {
                    break;
                }
                i++;
            }
        }
        return Vector2.zero;
    }

    public static string EnumToNumber<TEnum>(TEnum value) where TEnum : struct, Enum {
        Type underlyingType = typeof(TEnum).GetEnumUnderlyingType();
        if (underlyingType == typeof(sbyte)) { return Unsafe.As<TEnum, sbyte>(ref value).ToString(); }
        if (underlyingType == typeof(byte)) { return Unsafe.As<TEnum, byte>(ref value).ToString(); }
        if (underlyingType == typeof(short)) { return Unsafe.As<TEnum, short>(ref value).ToString(); }
        if (underlyingType == typeof(ushort)) { return Unsafe.As<TEnum, ushort>(ref value).ToString(); }
        if (underlyingType == typeof(int)) { return Unsafe.As<TEnum, int>(ref value).ToString(); }
        if (underlyingType == typeof(uint)) { return Unsafe.As<TEnum, uint>(ref value).ToString(); }
        if (underlyingType == typeof(long)) { return Unsafe.As<TEnum, long>(ref value).ToString(); }
        if (underlyingType == typeof(ulong)) { return Unsafe.As<TEnum, ulong>(ref value).ToString(); }
        throw new InvalidOperationException($"Unknown enum underlying type");
    }

    public static string EnumToString<TEnum>(TEnum value) where TEnum : struct, Enum {
        return typeof(TEnum).Name + ($".{Enum.GetName<TEnum>(value)} [{EnumToNumber(value)}]" ?? $"({EnumToNumber(value)})");
    }

    public static ReadOnlySpan<byte> Get7BitEncodedSizeString(string str) {
        byte[] rented = ArrayPool<byte>.Shared.Rent(str.Length * 3);
        int actualByteCount = Encoding.UTF8.GetBytes(str, rented);
        uint actualByteCountTmp = (uint)actualByteCount;
        int pos = 0;
        byte[] buffer = new byte[actualByteCount + 5];

        while (actualByteCountTmp > 0x7Fu) {
            buffer[pos++] = (byte)(actualByteCountTmp | ~0x7Fu);
            actualByteCountTmp >>= 7;
        }
        buffer[pos++] = (byte)actualByteCountTmp;
        rented[..actualByteCount].CopyTo(buffer.AsSpan()[pos..]);

        ArrayPool<byte>.Shared.Return(rented);
        return buffer.AsSpan()[..(pos + actualByteCount)];
    }
}

public static class Extensions {
    public static IEnumerable<(int index, T item)> WithIndex<T>(this IEnumerable<T> source) {
        return source.Select((item, index) => (index, item));
    }
    public static uint PostAdd(ref this uint obj, uint delta) {
        uint prev = obj;
        obj += delta;
        return prev;
    }
    public static int PostAdd(ref this int obj, int delta) {
        int prev = obj;
        obj += delta;
        return prev;
    }
}

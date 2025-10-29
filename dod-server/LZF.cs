// ---------------------------------------------------
// Decompiled CLZF2 class from Dig Or Die with changes
// ---------------------------------------------------

using System;

public static class CLZF2 {
    public static byte[] Compress(byte[] inputBytes) {
        byte[] array = new byte[inputBytes.Length / 4];
        int num;
        for (num = lzf_compress(inputBytes, ref array); num == 0; num = lzf_compress(inputBytes, ref array)) {
            array = new byte[array.Length * 2];
        }
        byte[] array2 = new byte[num];
        Buffer.BlockCopy(array, 0, array2, 0, num);
        return array2;
    }

    public static byte[]? Decompress(byte[] inputBytes) {
        byte[] array = new byte[inputBytes.Length * 12];
        int i;
        for (i = lzf_decompress(inputBytes, ref array); i <= 0; i = lzf_decompress(inputBytes, ref array)) {
            if (i < 0) {
                return null;
            }
            array = new byte[array.Length * 2];
        }
        byte[] array2 = new byte[i];
        Buffer.BlockCopy(array, 0, array2, 0, i);
        return array2;
    }

    public static int lzf_compress(byte[] input, ref byte[] output) {
        int num = input.Length;
        int num2 = output.Length;
        Array.Clear(HashTable, 0, (int)HSIZE);
        uint num3 = 0U;
        uint num4 = 0U;
        uint num5 = (uint)((input[(int)(UIntPtr)num3] << 8) | input[(int)(UIntPtr)(num3 + 1U)]);
        int num6 = 0;
        for (; ; )
        {
            if (num3 < (ulong)(long)(num - 2)) {
                num5 = (num5 << 8) | input[(int)(UIntPtr)(num3 + 2U)];
                long num7 = (long)(ulong)(((num5 ^ (num5 << 5)) >> (int)(24U - HLOG - num5 * 5U)) & (HSIZE - 1U));
                long num8 = HashTable[(int)checked((IntPtr)num7)];
                HashTable[(int)checked((IntPtr)num7)] = (long)(ulong)num3;
                long num9;
                if ((num9 = (long)(num3 - (ulong)num8 - 1UL)) < (long)(ulong)MAX_OFF && num3 + 4U < (ulong)(long)num && num8 > 0L && input[(int)checked((IntPtr)num8)] == input[(int)(UIntPtr)num3] && input[(int)checked((IntPtr)unchecked(num8 + 1L))] == input[(int)(UIntPtr)(num3 + 1U)] && input[(int)checked((IntPtr)unchecked(num8 + 2L))] == input[(int)(UIntPtr)(num3 + 2U)]) {
                    uint num10 = 2U;
                    uint num11 = (uint)(num - (int)num3 - (int)num10);
                    num11 = (num11 <= MAX_REF) ? num11 : MAX_REF;
                    if (num4 + (ulong)(long)num6 + 1UL + 3UL >= (ulong)(long)num2) {
                        break;
                    }
                    do {
                        num10 += 1U;
                    }
                    while (num10 < num11 && input[(int)checked((IntPtr)unchecked(num8 + (long)(ulong)num10))] == input[(int)(UIntPtr)(num3 + num10)]);
                    if (num6 != 0) {
                        output[(int)(UIntPtr)num4++] = (byte)(num6 - 1);
                        num6 = -num6;
                        do {
                            output[(int)(UIntPtr)num4++] = input[(int)checked((IntPtr)unchecked(num3 + (ulong)(long)num6))];
                        }
                        while (++num6 != 0);
                    }
                    num10 -= 2U;
                    num3 += 1U;
                    if (num10 < 7U) {
                        output[(int)(UIntPtr)num4++] = (byte)((num9 >> 8) + (long)((ulong)num10 << 5));
                    } else {
                        output[(int)(UIntPtr)num4++] = (byte)((num9 >> 8) + 224L);
                        output[(int)(UIntPtr)num4++] = (byte)(num10 - 7U);
                    }
                    output[(int)(UIntPtr)num4++] = (byte)num9;
                    num3 += num10 - 1U;
                    num5 = (uint)((input[(int)(UIntPtr)num3] << 8) | input[(int)(UIntPtr)(num3 + 1U)]);
                    num5 = (num5 << 8) | input[(int)(UIntPtr)(num3 + 2U)];
                    HashTable[(int)(UIntPtr)(((num5 ^ (num5 << 5)) >> (int)(24U - HLOG - num5 * 5U)) & (HSIZE - 1U))] = (long)(ulong)num3;
                    num3 += 1U;
                    num5 = (num5 << 8) | input[(int)(UIntPtr)(num3 + 2U)];
                    HashTable[(int)(UIntPtr)(((num5 ^ (num5 << 5)) >> (int)(24U - HLOG - num5 * 5U)) & (HSIZE - 1U))] = (long)(ulong)num3;
                    num3 += 1U;
                    continue;
                }
            } else if (num3 == (ulong)(long)num) {
                goto Block_13;
            }
            num6++;
            num3 += 1U;
            if (num6 == (long)(ulong)MAX_LIT) {
                if (num4 + 1U + MAX_LIT >= (ulong)(long)num2) {
                    return 0;
                }
                output[(int)(UIntPtr)num4++] = (byte)(MAX_LIT - 1U);
                num6 = -num6;
                do {
                    output[(int)(UIntPtr)num4++] = input[(int)checked((IntPtr)unchecked(num3 + (ulong)(long)num6))];
                }
                while (++num6 != 0);
            }
        }
        return 0;
Block_13:
        if (num6 != 0) {
            if (num4 + (ulong)(long)num6 + 1UL >= (ulong)(long)num2) {
                return 0;
            }
            output[(int)(UIntPtr)num4++] = (byte)(num6 - 1);
            num6 = -num6;
            do {
                output[(int)(UIntPtr)num4++] = input[(int)checked((IntPtr)unchecked(num3 + (ulong)(long)num6))];
            }
            while (++num6 != 0);
        }
        return (int)num4;
    }

    public static int lzf_decompress(byte[] input, ref byte[] output) {
        int num = input.Length;
        int num2 = output.Length;
        uint num3 = 0U;
        uint num4 = 0U;
        for (; ; )
        {
            uint num5 = input[(int)(UIntPtr)num3++];
            if (num5 < 32U) {
                num5 += 1U;
                if (num4 + num5 > (ulong)(long)num2) {
                    break;
                }
                do {
                    output[(int)(UIntPtr)num4++] = input[(int)(UIntPtr)num3++];
                }
                while ((num5 -= 1U) != 0U);
            } else {
                uint num6 = num5 >> 5;
                int num7 = (int)(num4 - ((num5 & 31U) << 8) - 1U);
                if (num6 == 7U) {
                    num6 += input[(int)(UIntPtr)num3++];
                }
                num7 -= input[(int)(UIntPtr)num3++];
                if (num4 + num6 + 2U > (ulong)(long)num2) {
                    return 0;
                }
                if (num7 < 0) {
                    goto Block_6;
                }
                output[(int)(UIntPtr)num4++] = output[num7++];
                output[(int)(UIntPtr)num4++] = output[num7++];
                do {
                    output[(int)(UIntPtr)num4++] = output[num7++];
                }
                while ((num6 -= 1U) != 0U);
            }
            if (num3 >= (ulong)(long)num) {
                return (int)num4;
            }
        }
        return 0;
Block_6:
        return -1;
    }

    public static int lzf_compress(byte[] input, int inputLength, ref byte[] output) {
        int num = output.Length;
        Array.Clear(HashTable, 0, (int)HSIZE);
        uint num2 = 0U;
        uint num3 = 0U;
        uint num4 = (uint)((input[(int)(UIntPtr)num2] << 8) | input[(int)(UIntPtr)(num2 + 1U)]);
        int num5 = 0;
        for (; ; )
        {
            if (num2 < (ulong)(long)(inputLength - 2)) {
                num4 = (num4 << 8) | input[(int)(UIntPtr)(num2 + 2U)];
                long num6 = (long)(ulong)(((num4 ^ (num4 << 5)) >> (int)(24U - HLOG - num4 * 5U)) & (HSIZE - 1U));
                long num7 = HashTable[(int)checked((IntPtr)num6)];
                HashTable[(int)checked((IntPtr)num6)] = (long)(ulong)num2;
                long num8;
                if ((num8 = (long)(num2 - (ulong)num7 - 1UL)) < (long)(ulong)MAX_OFF && num2 + 4U < (ulong)(long)inputLength && num7 > 0L && input[(int)checked((IntPtr)num7)] == input[(int)(UIntPtr)num2] && input[(int)checked((IntPtr)unchecked(num7 + 1L))] == input[(int)(UIntPtr)(num2 + 1U)] && input[(int)checked((IntPtr)unchecked(num7 + 2L))] == input[(int)(UIntPtr)(num2 + 2U)]) {
                    uint num9 = 2U;
                    uint num10 = (uint)(inputLength - (int)num2 - (int)num9);
                    num10 = (num10 <= MAX_REF) ? num10 : MAX_REF;
                    if (num3 + (ulong)(long)num5 + 1UL + 3UL >= (ulong)(long)num) {
                        break;
                    }
                    do {
                        num9 += 1U;
                    }
                    while (num9 < num10 && input[(int)checked((IntPtr)unchecked(num7 + (long)(ulong)num9))] == input[(int)(UIntPtr)(num2 + num9)]);
                    if (num5 != 0) {
                        output[(int)(UIntPtr)num3++] = (byte)(num5 - 1);
                        num5 = -num5;
                        do {
                            output[(int)(UIntPtr)num3++] = input[(int)checked((IntPtr)unchecked(num2 + (ulong)(long)num5))];
                        }
                        while (++num5 != 0);
                    }
                    num9 -= 2U;
                    num2 += 1U;
                    if (num9 < 7U) {
                        output[(int)(UIntPtr)num3++] = (byte)((num8 >> 8) + (long)((ulong)num9 << 5));
                    } else {
                        output[(int)(UIntPtr)num3++] = (byte)((num8 >> 8) + 224L);
                        output[(int)(UIntPtr)num3++] = (byte)(num9 - 7U);
                    }
                    output[(int)(UIntPtr)num3++] = (byte)num8;
                    num2 += num9 - 1U;
                    num4 = (uint)((input[(int)(UIntPtr)num2] << 8) | input[(int)(UIntPtr)(num2 + 1U)]);
                    num4 = (num4 << 8) | input[(int)(UIntPtr)(num2 + 2U)];
                    HashTable[(int)(UIntPtr)(((num4 ^ (num4 << 5)) >> (int)(24U - HLOG - num4 * 5U)) & (HSIZE - 1U))] = (long)(ulong)num2;
                    num2 += 1U;
                    num4 = (num4 << 8) | input[(int)(UIntPtr)(num2 + 2U)];
                    HashTable[(int)(UIntPtr)(((num4 ^ (num4 << 5)) >> (int)(24U - HLOG - num4 * 5U)) & (HSIZE - 1U))] = (long)(ulong)num2;
                    num2 += 1U;
                    continue;
                }
            } else if (num2 == (ulong)(long)inputLength) {
                goto Block_13;
            }
            num5++;
            num2 += 1U;
            if (num5 == (long)(ulong)MAX_LIT) {
                if (num3 + 1U + MAX_LIT >= (ulong)(long)num) {
                    return 0;
                }
                output[(int)(UIntPtr)num3++] = (byte)(MAX_LIT - 1U);
                num5 = -num5;
                do {
                    output[(int)(UIntPtr)num3++] = input[(int)checked((IntPtr)unchecked(num2 + (ulong)(long)num5))];
                }
                while (++num5 != 0);
            }
        }
        return 0;
Block_13:
        if (num5 != 0) {
            if (num3 + (ulong)(long)num5 + 1UL >= (ulong)(long)num) {
                return 0;
            }
            output[(int)(UIntPtr)num3++] = (byte)(num5 - 1);
            num5 = -num5;
            do {
                output[(int)(UIntPtr)num3++] = input[(int)checked((IntPtr)unchecked(num2 + (ulong)(long)num5))];
            }
            while (++num5 != 0);
        }
        return (int)num3;
    }

    public static int lzf_decompress(byte[] input, int inputLength, ref byte[] output) {
        int num = output.Length;
        uint num2 = 0U;
        uint num3 = 0U;
        for (; ; )
        {
            uint num4 = input[(int)(UIntPtr)num2++];
            if (num4 < 32U) {
                num4 += 1U;
                if (num3 + num4 > (ulong)(long)num) {
                    break;
                }
                do {
                    output[(int)(UIntPtr)num3++] = input[(int)(UIntPtr)num2++];
                }
                while ((num4 -= 1U) != 0U);
            } else {
                uint num5 = num4 >> 5;
                int num6 = (int)(num3 - ((num4 & 31U) << 8) - 1U);
                if (num5 == 7U) {
                    num5 += input[(int)(UIntPtr)num2++];
                }
                num6 -= input[(int)(UIntPtr)num2++];
                if (num3 + num5 + 2U > (ulong)(long)num) {
                    return 0;
                }
                if (num6 < 0) {
                    goto Block_6;
                }
                output[(int)(UIntPtr)num3++] = output[num6++];
                output[(int)(UIntPtr)num3++] = output[num6++];
                do {
                    output[(int)(UIntPtr)num3++] = output[num6++];
                }
                while ((num5 -= 1U) != 0U);
            }
            if (num2 >= (ulong)(long)inputLength) {
                return (int)num3;
            }
        }
        return 0;
Block_6:
        return -1;
    }

    private static readonly uint HLOG = 14U;

    private static readonly uint HSIZE = 16384U;

    private static readonly uint MAX_LIT = 32U;

    private static readonly uint MAX_OFF = 8192U;

    private static readonly uint MAX_REF = 264U;

    private static readonly long[] HashTable = new long[HSIZE];
}

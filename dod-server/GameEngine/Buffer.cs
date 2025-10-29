
using System;
using System.Runtime.CompilerServices;

namespace GameEngine;

public class CBuffer {
    public readonly byte[] data = GC.AllocateUninitializedArray<byte>(1024 * 1024);
    public uint pos = 0;

    public void WriteBool(bool value) {
        Unsafe.WriteUnaligned(ref data[pos], value);
        pos += 1;
    }

    public void WriteByte(byte value) {
        data[pos] = value;
        pos += 1;
    }

    public void WriteSByte(sbyte value) {
        Unsafe.WriteUnaligned(ref data[pos], value);
        pos += 1;
    }

    public void WriteByteArray(ReadOnlySpan<byte> source) {
        WriteInt(source.Length);
        source.CopyTo(data.AsSpan()[(int)pos..]);
        pos += (uint)source.Length;
    }

    public void WriteShort(short value) {
        Unsafe.WriteUnaligned(ref data[pos], value);
        pos += 2;
    }

    public void WriteUShort(ushort value) {
        Unsafe.WriteUnaligned(ref data[pos], value);
        pos += 2;
    }

    public void WriteUShort2(ushort2 value) {
        Unsafe.WriteUnaligned(ref data[pos], Unsafe.BitCast<ushort2, uint>(value));
        pos += 4;
    }

    public void WriteInt(int value) {
        Unsafe.WriteUnaligned(ref data[pos], value);
        pos += 4;
    }

    public void WriteIntAt(int value, uint pos) {
        Unsafe.WriteUnaligned(ref data[pos], value);
    }

    public void WriteUInt(uint value) {
        Unsafe.WriteUnaligned(ref data[pos], value);
        pos += 4;
    }

    public void WriteUIntAt(uint value, uint pos) {
        Unsafe.WriteUnaligned(ref data[pos], value);
    }

    public void WriteFloat(float value) {
        Unsafe.WriteUnaligned(ref data[pos], Unsafe.BitCast<float, int>(value));
        pos += 4;
    }

    public void WriteFloat_asShort(float value, float maxValue) {
        WriteShort((short)Math.Round(value * 32767f / maxValue));
    }

    public void WriteFloat_asUShort(float value, float maxValue, bool ceilingY = false) {
        WriteUShort((ushort)(ceilingY ? Math.Ceiling(value * 65535f / maxValue) : Math.Round(value * 65535f / maxValue)));
    }

    public void WriteFloat_asSByte(float value, float maxValue) {
        WriteSByte((sbyte)Math.Round(value * 127f / maxValue));
    }

    public void WriteVector2(Vector2 value) {
        Unsafe.WriteUnaligned(ref data[pos], Unsafe.BitCast<Vector2, ulong>(value));
        pos += 8;
    }

    public void WriteVector2_asShort2(Vector2 value, float maxValue) {
        float scaleFactor = 32767f / maxValue;
        WriteShort((short)Math.Round(value.x * scaleFactor));
        WriteShort((short)Math.Round(value.y * scaleFactor));
    }

    public void WriteVector2_asUShort2(Vector2 value, float maxValue = 1024f, bool ceilingY = false) {
        WriteFloat_asUShort(value.x, maxValue);
        WriteFloat_asUShort(value.y, maxValue, ceilingY);
    }

    public void WriteULong(ulong value) {
        Unsafe.WriteUnaligned(ref data[pos], value);
        pos += 8;
    }
}
public class CBufferSpan {
    private readonly byte[] data;
    private uint pos;
    private readonly uint dataLength;

    public uint Pos => pos;
    public uint Length => dataLength - pos;

    public CBufferSpan(byte[] data, uint start, uint length) {
        if (start + length > data.Length) {
            throw new ArgumentException($"Provided start and length positions are out of range");
        }
        this.data = data;
        this.pos = start;
        this.dataLength = start + length;
    }
    public CBufferSpan(CBuffer buffer, uint length) {
        if (buffer.pos + length > buffer.data.Length) {
            throw new ArgumentException($"Provided start and length positions are out of range");
        }
        this.data = buffer.data;
        this.pos = (uint)buffer.pos;
        this.dataLength = (uint)buffer.pos + length;
    }

    private void CheckLength(uint readSize) {
        if (pos + readSize > dataLength) {
            throw new IndexOutOfRangeException();
        }
    }

    public CBufferSpan Subrange(uint length) {
        return new CBufferSpan(data, pos, length);
    }
    public void Advance(uint dist) {
        CheckLength(dist);
        pos += dist;
    }
    public bool IsEmpty() {
        return pos >= dataLength;
    }

    public bool ReadBool() {
        CheckLength(1);
        return Unsafe.ReadUnaligned<bool>(ref data[pos.PostAdd(1)]);
    }
    public byte ReadByte() {
        CheckLength(1);
        return data[pos++];
    }
    public sbyte ReadSByte() {
        CheckLength(1);
        return Unsafe.ReadUnaligned<sbyte>(ref data[pos.PostAdd(1)]);
    }
    public ReadOnlySpan<byte> ReadByteSpan() {
        int spanSize = ReadInt();
        CheckLength((uint)spanSize);
        uint start = pos;
        pos += (uint)spanSize;
        return new ReadOnlySpan<byte>(data, (int)start, spanSize);
    }
    public short ReadShort() {
        CheckLength(2);
        return Unsafe.ReadUnaligned<short>(ref data[pos.PostAdd(2)]);
    }
    public ushort ReadUShort() {
        CheckLength(2);
        return Unsafe.ReadUnaligned<ushort>(ref data[pos.PostAdd(2)]);
    }
    public ushort2 ReadUShort2() {
        CheckLength(4);
        return Unsafe.BitCast<int, ushort2>(
            Unsafe.ReadUnaligned<int>(ref data[pos.PostAdd(4)])
        );
    }
    public int ReadInt() {
        CheckLength(4);
        return Unsafe.ReadUnaligned<int>(ref data[pos.PostAdd(4)]);
    }
    public uint ReadUInt() {
        CheckLength(4);
        return Unsafe.ReadUnaligned<uint>(ref data[pos.PostAdd(4)]);
    }
    public float ReadFloat() {
        CheckLength(4);
        return Unsafe.BitCast<int, float>(
            Unsafe.ReadUnaligned<int>(ref data[pos.PostAdd(4)])
        );
    }
    public float ReadFloat_fromShort(float maxValue) {
        CheckLength(2);
        return ReadShort() * maxValue / 32767f;
    }
    public float ReadFloat_fromUShort(float maxValue) {
        CheckLength(2);
        float num = ReadUShort() * maxValue / 65535f;
        if (num % 1f > 0.99f || num % 1f < 0.01f) {
            num = (float)Math.Round(num);
        }
        return num;
    }
    public float ReadFloat_fromSByte(float maxValue) {
        CheckLength(1);
        return ReadSByte() * maxValue / 127f;
    }
    public Vector2 ReadVector2() {
        CheckLength(8);
        return Unsafe.BitCast<ulong, Vector2>(
            Unsafe.ReadUnaligned<ulong>(ref data[pos.PostAdd(8)])
        );
    }
    public Vector2 ReadVector2_FromShort2(float maxValue) {
        CheckLength(4);
        float scaleFactor = maxValue / 32767f;
        return new Vector2(ReadShort() * scaleFactor, ReadShort() * scaleFactor);
    }
    public Vector2 ReadVector2_FromUshort2(float maxValue = 1024f) {
        CheckLength(4);
        return new Vector2(ReadFloat_fromUShort(maxValue), ReadFloat_fromUShort(maxValue));
    }
    public ulong ReadULong() {
        CheckLength(8);
        return Unsafe.ReadUnaligned<ulong>(ref data[pos.PostAdd(8)]);
    }
    public bool CanReadBytes(uint num) {
        return pos + num <= dataLength;
    }
}

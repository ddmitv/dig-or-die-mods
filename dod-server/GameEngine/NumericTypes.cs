
using System;

namespace GameEngine;

#pragma warning disable IDE1006

public struct Vector2 {
    public float x;
    public float y;

    public Vector2(float x, float y) {
        this.x = x;
        this.y = y;
    }
    public static Vector2 FromPolar(float angleRad, float dist) {
        (float sinX, float cosX) = MathF.SinCos(angleRad);
        return new Vector2(cosX, sinX) * dist;
    }
    public static Vector2 FromUnitPolar(float angleRad) {
        (float sinX, float cosX) = MathF.SinCos(angleRad);
        return new Vector2(cosX, sinX);
    }

    public static float SqrMagnitude(Vector2 a) => a.SqrMagnitude();

    public readonly float SqrMagnitude() {
        return MathF.FusedMultiplyAdd(x, x, y * y);
    }
    public void Normalize() {
        float magnitude = this.magnitude;
        if (magnitude > 1E-05f) {
            this /= magnitude;
        } else {
            this = Vector2.zero;
        }
    }

    public override readonly string ToString() {
        return $"({x:F1}, {y:F1})";
    }

    public static Vector2 zero => new(0f, 0f);
    public static Vector2 one => new(1f, 1f);
    public static Vector2 up => new(0f, 1f);
    public static Vector2 down => new(0f, -1f);
    public static Vector2 left => new(-1f, 0f);
    public static Vector2 right => new(1f, 0f);

    public readonly float magnitude => MathF.Sqrt(MathF.FusedMultiplyAdd(x, x, y * y));

    public readonly Vector2 normalized {
        get {
            Vector2 vector = new(x, y);
            vector.Normalize();
            return vector;
        }
    }

    public static Vector2 operator +(Vector2 a, Vector2 b) {
        return new Vector2(a.x + b.x, a.y + b.y);
    }
    public static Vector2 operator -(Vector2 a, Vector2 b) {
        return new Vector2(a.x - b.x, a.y - b.y);
    }
    public static Vector2 operator -(Vector2 a) {
        return new Vector2(-a.x, -a.y);
    }
    public static Vector2 operator *(Vector2 a, float d) {
        return new Vector2(a.x * d, a.y * d);
    }
    public static Vector2 operator *(float d, Vector2 a) {
        return new Vector2(a.x * d, a.y * d);
    }
    public static Vector2 operator /(Vector2 a, float d) {
        return new Vector2(a.x / d, a.y / d);
    }
    public static bool operator ==(Vector2 lhs, Vector2 rhs) {
        return SqrMagnitude(lhs - rhs) < 9.9999994E-11f;
    }
    public static bool operator !=(Vector2 lhs, Vector2 rhs) {
        return SqrMagnitude(lhs - rhs) >= 9.9999994E-11f;
    }
    public readonly override bool Equals(object? other) {
        if (other is not Vector2) {
            return false;
        }
        Vector2 vector = (Vector2)other;
        return x.Equals(vector.x) && y.Equals(vector.y);
    }
    public readonly override int GetHashCode() {
        return x.GetHashCode() ^ (y.GetHashCode() << 2);
    }

    public static Vector2 MoveTowards(Vector2 current, Vector2 target, float maxDistanceDelta) {
        Vector2 vector = target - current;
        float magnitude = vector.magnitude;
        if (magnitude <= maxDistanceDelta || magnitude == 0f) {
            return target;
        }
        return current + vector / magnitude * maxDistanceDelta;
    }
    public static Vector2 Clamp(Vector2 v, Vector2 min, Vector2 max) {
        return new(Math.Clamp(v.x, min.x, max.x), Math.Clamp(v.y, min.y, max.y));
    }
}

[Serializable]
public struct int2 {
    public int x;
    public int y;

    public static readonly int2 zero = new(0, 0);
    public static readonly int2 up = new(0, 1);
    public static readonly int2 right = new(1, 0);
    public static readonly int2 one = new(1, 1);
    public static readonly int2 negative = new(-1, -1);

    public int2(int x, int y) {
        this.x = x;
        this.y = y;
    }

    public static int2 operator +(int2 a, int2 b) {
        return new int2(a.x + b.x, a.y + b.y);
    }
    public static int2 operator -(int2 a, int2 b) {
        return new int2(a.x - b.x, a.y - b.y);
    }
    public static int2 operator -(int2 a) {
        return new int2(-a.x, -a.y);
    }
    public static int2 operator *(int2 a, int b) {
        return new int2(a.x * b, a.y * b);
    }
    public static int2 operator *(int b, int2 a) {
        return new int2(a.x * b, a.y * b);
    }
    public static int2 operator *(int2 b, int2 a) {
        return new int2(a.x * b.x, a.y * b.y);
    }
    public static Vector2 operator /(int2 a, float b) {
        return new Vector2((float)a.x / b, (float)a.y / b);
    }
    public static explicit operator Vector2(int2 v) {
        return new Vector2(v.x, v.y);
    }
    public static explicit operator int2(Vector2 v) {
        return new int2((int)MathF.Floor(v.x), (int)MathF.Floor(v.y));
    }

    public static bool operator ==(int2 a, int2 b) {
        return a.x == b.x && a.y == b.y;
    }
    public static bool operator !=(int2 a, int2 b) {
        return a.x != b.x || a.y != b.y;
    }

    public readonly void Deconstruct(out int x, out int y) {
        x = this.x;
        y = this.y;
    }
    public override readonly string ToString() => $"({x}, {y})";

    public override readonly bool Equals(object? obj) => obj is int2 other && other == this;

    public override readonly int GetHashCode() => throw new NotImplementedException();

    public static int2 Min(int2 left, int2 right) {
        return new int2(Math.Min(left.x, right.x), Math.Min(left.y, right.y));
    }
    public static int2 Max(int2 left, int2 right) {
        return new int2(Math.Max(left.x, right.x), Math.Max(left.y, right.y));
    }
}

public struct ushort2 {
    public ushort x;
    public ushort y;

    public ushort2(ushort x, ushort y) {
        this.x = x;
        this.y = y;
    }
    public static explicit operator ushort2(int2 v) {
        return new ushort2((ushort)v.x, (ushort)v.y);
    }
    public static implicit operator int2(ushort2 v) {
        return new int2(v.x, v.y);
    }
    public override readonly string ToString() => $"({x}, {y})";
}

[Serializable]
public struct Color24 {
    public byte r;
    public byte g;
    public byte b;

    public Color24(byte r, byte g, byte b) {
        this.r = r;
        this.g = g;
        this.b = b;
    }
    public static Color24 FromNumber(uint number) {
        return new((byte)(number >> 16), (byte)(number >> 8), (byte)number);
    }
}

public struct Color32 {
    public byte r;
    public byte g;
    public byte b;
    public byte a;

    public Color32(byte r, byte g, byte b, byte a) {
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
    }
}

public struct RectInt {
    public int x;
    public int y;
    public int width;
    public int height;

    public RectInt(int x, int y, int width, int height) {
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
    }
    public RectInt(int2 center, int width, int height) {
        this.x = center.x - width / 2;
        this.y = center.y - height / 2;
        this.width = width;
        this.height = height;
    }
    public readonly int xMax => x + width;
    public readonly int yMax => y + height;
    public readonly Vector2 center => new(x + width / 2f, y + height / 2f);
    public readonly int2 size => new(width, height);
    public readonly int GetArea() => width * height;
    public readonly bool Contains(int i, int j) => i >= x && j >= y && i <= x + width && j <= y + height;
    public readonly bool Contains(float i, float j) => i >= x && j >= y && i <= x + width && j <= y + height;
    public readonly bool Contains(Vector2 pos) => pos.x >= x && pos.y >= y && pos.x <= x + width && pos.y <= y + height;
    public readonly bool Overlaps(RectInt rect) => rect.x < xMax && rect.xMax > x && rect.y < yMax && rect.yMax > y;

    public static readonly RectInt Unbounded = new(int.MinValue / 2, int.MinValue / 2, int.MaxValue, int.MaxValue);
}

public struct Rect {
    public float x;
    public float y;
    public float width;
    public float height;

    public Rect(float x, float y, float width, float height) {
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
    }
    public float xMin {
        readonly get => x;
        set {
            float xMax = this.xMax;
            x = value;
            width = xMax - x;
        }
    }
    public float yMin {
        readonly get => y;
        set {
            float yMax = this.yMax;
            y = value;
            height = yMax - y;
        }
    }
    public float xMax {
        readonly get => width + x;
        set => width = value - x;
    }
    public float yMax {
        readonly get => height + y;
        set => height = value - y;
    }
    public Vector2 center {
        readonly get => new(x + width / 2f, y + height / 2f);
        set {
            x = value.x - width / 2f;
            y = value.y - height / 2f;
        }
    }
}

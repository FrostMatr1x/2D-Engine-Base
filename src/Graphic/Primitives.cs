using System;

namespace Engine.Graphic;

public struct Rectangle : IEquatable<Rectangle>
{
    public int X;
    public int Y;
    public int Width;
    public int Height;

    public Rectangle(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public static Rectangle Empty => new Rectangle(0, 0, 0, 0);

    public int Left => X;
    public int Top => Y;
    public int Right => X + Width;
    public int Bottom => Y + Height;
    public bool IsEmpty => Width == 0 && Height == 0;

    public bool Contains(int x, int y) => x >= X && x < X + Width && y >= Y && y < Y + Height;

    public bool Contains(Rectangle value) =>
        value.X >= X && value.Right <= Right && value.Y >= Y && value.Bottom <= Bottom;

    public bool Intersects(Rectangle value) =>
        value.Left < Right && Left < value.Right && value.Top < Bottom && Top < value.Bottom;

    public bool Equals(Rectangle other) => X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;

    public override bool Equals(object? obj) => obj is Rectangle other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);

    public override string ToString() => $"{{X:{X} Y:{Y} Width:{Width} Height:{Height}}}";

    public static bool operator ==(Rectangle a, Rectangle b) => a.Equals(b);
    public static bool operator !=(Rectangle a, Rectangle b) => !a.Equals(b);
}

public struct Color : IEquatable<Color>
{
    public byte R;
    public byte G;
    public byte B;
    public byte A;

    public Color(byte r, byte g, byte b, byte a)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public Color(int r, int g, int b, int a)
    {
        R = (byte)r;
        G = (byte)g;
        B = (byte)b;
        A = (byte)a;
    }

    public Color(float r, float g, float b, float a)
    {
        R = ToByte(r);
        G = ToByte(g);
        B = ToByte(b);
        A = ToByte(a);
    }

    public static Color White => new Color(255, 255, 255, 255);
    public static Color Black => new Color(0, 0, 0, 255);
    public static Color Transparent => new Color(0, 0, 0, 0);
    public static Color CornflowerBlue => new Color(100, 149, 237, 255);

    public uint PackedValue => (uint)(R | (G << 8) | (B << 16) | (A << 24));

    public static Color Lerp(Color a, Color b, float amount)
    {
        amount = Math.Clamp(amount, 0f, 1f);
        return new Color(
            (byte)(a.R + (b.R - a.R) * amount),
            (byte)(a.G + (b.G - a.G) * amount),
            (byte)(a.B + (b.B - a.B) * amount),
            (byte)(a.A + (b.A - a.A) * amount));
    }

    private static byte ToByte(float value) => (byte)Math.Clamp((int)(value * 255f + 0.5f), 0, 255);

    public static Color operator *(Color value, float scale) =>
        new Color((byte)(value.R * scale), (byte)(value.G * scale), (byte)(value.B * scale), (byte)(value.A * scale));

    public static Color operator *(float scale, Color value) => value * scale;

    public bool Equals(Color other) => R == other.R && G == other.G && B == other.B && A == other.A;

    public override bool Equals(object? obj) => obj is Color other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(R, G, B, A);

    public static bool operator ==(Color a, Color b) => a.Equals(b);
    public static bool operator !=(Color a, Color b) => !a.Equals(b);
}

public struct Matrix : IEquatable<Matrix>
{
    public float M11, M12, M13, M14;
    public float M21, M22, M23, M24;
    public float M31, M32, M33, M34;
    public float M41, M42, M43, M44;

    public static Matrix Identity => new Matrix
    {
        M11 = 1f,
        M22 = 1f,
        M33 = 1f,
        M44 = 1f
    };

    public static Matrix CreateScale(float scale) => CreateScale(scale, scale, scale);

    public static Matrix CreateScale(float x, float y, float z)
    {
        Matrix result = Identity;
        result.M11 = x;
        result.M22 = y;
        result.M33 = z;
        return result;
    }

    public static Matrix CreateOrthographicOffCenter(float left, float right, float bottom, float top, float near, float far)
    {
        float width = right - left;
        float height = top - bottom;
        float depth = far - near;

        Matrix result = Identity;
        result.M11 = 2f / width;
        result.M22 = 2f / height;
        result.M33 = 1f / depth;
        result.M41 = -(right + left) / width;
        result.M42 = -(top + bottom) / height;
        result.M43 = -near / depth;
        return result;
    }

    public static Matrix operator *(Matrix a, Matrix b)
    {
        return new Matrix
        {
            M11 = a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31 + a.M14 * b.M41,
            M12 = a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32 + a.M14 * b.M42,
            M13 = a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33 + a.M14 * b.M43,
            M14 = a.M11 * b.M14 + a.M12 * b.M24 + a.M13 * b.M34 + a.M14 * b.M44,

            M21 = a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31 + a.M24 * b.M41,
            M22 = a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32 + a.M24 * b.M42,
            M23 = a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33 + a.M24 * b.M43,
            M24 = a.M21 * b.M14 + a.M22 * b.M24 + a.M23 * b.M34 + a.M24 * b.M44,

            M31 = a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31 + a.M34 * b.M41,
            M32 = a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32 + a.M34 * b.M42,
            M33 = a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33 + a.M34 * b.M43,
            M34 = a.M31 * b.M14 + a.M32 * b.M24 + a.M33 * b.M34 + a.M34 * b.M44,

            M41 = a.M41 * b.M11 + a.M42 * b.M21 + a.M43 * b.M31 + a.M44 * b.M41,
            M42 = a.M41 * b.M12 + a.M42 * b.M22 + a.M43 * b.M32 + a.M44 * b.M42,
            M43 = a.M41 * b.M13 + a.M42 * b.M23 + a.M43 * b.M33 + a.M44 * b.M43,
            M44 = a.M41 * b.M14 + a.M42 * b.M24 + a.M43 * b.M34 + a.M44 * b.M44
        };
    }

    public Mat4 ToMat4() => new Mat4
    {
        M00 = M11,
        M01 = M12,
        M02 = M13,
        M03 = M14,
        M10 = M21,
        M11 = M22,
        M12 = M23,
        M13 = M24,
        M20 = M31,
        M21 = M32,
        M22 = M33,
        M23 = M34,
        M30 = M41,
        M31 = M42,
        M32 = M43,
        M33 = M44
    };

    public bool Equals(Matrix other) =>
        M11 == other.M11 && M12 == other.M12 && M13 == other.M13 && M14 == other.M14 &&
        M21 == other.M21 && M22 == other.M22 && M23 == other.M23 && M24 == other.M24 &&
        M31 == other.M31 && M32 == other.M32 && M33 == other.M33 && M34 == other.M34 &&
        M41 == other.M41 && M42 == other.M42 && M43 == other.M43 && M44 == other.M44;

    public override bool Equals(object? obj) => obj is Matrix other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(M11, M22, M33, M44);
}

[Flags]
public enum SpriteEffects
{
    None = 0,
    FlipHorizontally = 1,
    FlipVertically = 2
}

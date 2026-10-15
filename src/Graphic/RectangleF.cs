using System;

namespace Engine.Graphic;

public struct RectangleF : IEquatable<RectangleF>
{
    public float X;
    public float Y;
    public float Width;
    public float Height;

    public RectangleF(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public static RectangleF Empty => new RectangleF(0f, 0f, 0f, 0f);

    public float Left => X;
    public float Top => Y;
    public float Right => X + Width;
    public float Bottom => Y + Height;
    public bool IsEmpty => Width == 0f && Height == 0f;

    public bool Contains(float x, float y) => x >= X && x < X + Width && y >= Y && y < Y + Height;

    public bool Contains(RectangleF value) =>
        value.X >= X && value.Right <= Right && value.Y >= Y && value.Bottom <= Bottom;

    public bool IntersectsWith(RectangleF value) =>
        value.Left < Right && Left < value.Right && value.Top < Bottom && Top < value.Bottom;

    public static RectangleF Intersect(RectangleF a, RectangleF b)
    {
        float left = MathF.Max(a.Left, b.Left);
        float top = MathF.Max(a.Top, b.Top);
        float right = MathF.Min(a.Right, b.Right);
        float bottom = MathF.Min(a.Bottom, b.Bottom);

        if (right <= left || bottom <= top)
        {
            return Empty;
        }

        return new RectangleF(left, top, right - left, bottom - top);
    }

    public static RectangleF Union(RectangleF a, RectangleF b)
    {
        float left = MathF.Min(a.Left, b.Left);
        float top = MathF.Min(a.Top, b.Top);
        float right = MathF.Max(a.Right, b.Right);
        float bottom = MathF.Max(a.Bottom, b.Bottom);
        return new RectangleF(left, top, right - left, bottom - top);
    }

    public bool Equals(RectangleF other) => X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;

    public override bool Equals(object obj) => obj is RectangleF other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);

    public override string ToString() => $"{{X:{X} Y:{Y} Width:{Width} Height:{Height}}}";

    public static bool operator ==(RectangleF a, RectangleF b) => a.Equals(b);

    public static bool operator !=(RectangleF a, RectangleF b) => !a.Equals(b);
}

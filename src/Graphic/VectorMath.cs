using System;
using System.Numerics;

namespace Engine.Graphic;

public static class VectorMath
{
    public static Vector2 Floor(Vector2 value) => new Vector2(MathF.Floor(value.X), MathF.Floor(value.Y));

    public static Vector2 Ceiling(Vector2 value) => new Vector2(MathF.Ceiling(value.X), MathF.Ceiling(value.Y));

    public static Vector2 Round(Vector2 value) => new Vector2(MathF.Round(value.X), MathF.Round(value.Y));
}

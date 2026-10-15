using System.Numerics;
using System.Runtime.InteropServices;

namespace Engine.Graphic;

[StructLayout(LayoutKind.Sequential)]
public struct Vertex2D
{
    public Vector2 Position;

    public Vertex2D(float x, float y)
    {
        Position = new Vector2(x, y);
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct Instance2D
{
    public Vector2 Position;
    public Vector2 Scale;
    public float Rotation;
    public uint Color;

    public Instance2D(Vector2 position, Vector2 scale, float rotation, uint color)
    {
        Position = position;
        Scale = scale;
        Rotation = rotation;
        Color = color;
    }

    public static uint PackColor(byte r, byte g, byte b, byte a)
    {
        return (uint)(r | (g << 8) | (b << 16) | (a << 24));
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct PushConstants
{
    public Mat4 Projection;
}

using System;
using System.Numerics;

namespace Engine.Graphic;

public sealed class Camera2D
{
    public Vector2 Position;
    public float Zoom = 1.0f;
    public float MinZoom = 0.02f;
    public float MaxZoom = 50.0f;

    public Mat4 GetViewProjection(float width, float height)
    {
        return Mat4.OrthographicOffCenter(0f, width, 0f, height, 0f, 1f)
            * Mat4.Translation(width * 0.5f, height * 0.5f, 0f)
            * Mat4.Scale(Zoom, Zoom, 1f)
            * Mat4.Translation(-Position.X, -Position.Y, 0f);
    }

    public Vector2 ScreenToWorld(Vector2 screen, float width, float height)
    {
        Vector2 centered = screen - new Vector2(width * 0.5f, height * 0.5f);
        return Position + centered / Zoom;
    }

    public void ZoomAt(Vector2 screen, float scrollDelta, float width, float height)
    {
        Vector2 before = ScreenToWorld(screen, width, height);
        Zoom = Math.Clamp(Zoom * (1.0f + scrollDelta), MinZoom, MaxZoom);
        Vector2 after = ScreenToWorld(screen, width, height);
        Position += before - after;
    }
}

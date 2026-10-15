using System.Numerics;
using Engine.Graphic;

namespace Engine; 
public static class Camera
{
    public static float Scale { get; set; } = 1f;
    public static Matrix TransformMatrix => Matrix.CreateScale(Scale);

    public static Vector2 CameraPosition = Vector2.Zero;
    public static Vector2 FirstVisiblePosition;
    public static Vector2 LastVisiblePosition;
    public static Vector2 ScreenCenter => new Vector2(
     GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width / 2,
     GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height / 2);

    private const int POSITION_OFFSET = 120;

    public static Vector2 WorldToScreen(Vector2 vector)
    {
        vector = (new Vector2(vector.X, -vector.Y) - CameraPosition) * 16;
        return vector + ScreenCenter / Scale;
    }

    public static Vector2 ScreenToWorld(Vector2 vector)
    {
        vector -= ScreenCenter;
        vector /= 16f * Scale;
        vector += CameraPosition;
        return new Vector2(vector.X, -vector.Y);
    }

    public static bool IsPositionVisible(Vector2 position)
    {
        return position.X >= FirstVisiblePosition.X &&
                position.X <= LastVisiblePosition.X &&
                position.Y <= FirstVisiblePosition.Y &&
                position.Y >= LastVisiblePosition.Y;
    }
    
    public static void Update()
    {
        FirstVisiblePosition = ScreenToWorld(new(-POSITION_OFFSET, -POSITION_OFFSET));
        LastVisiblePosition = ScreenToWorld(new(GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width + POSITION_OFFSET, GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height + POSITION_OFFSET));
    }
}
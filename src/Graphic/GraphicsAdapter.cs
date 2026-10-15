using Silk.NET.Core.Contexts;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Engine.Graphic;

public sealed class DisplayMode
{
    public DisplayMode(int width, int height, int refreshRate = 60)
    {
        Width = width;
        Height = height;
        RefreshRate = refreshRate;
    }

    public int Width { get; }

    public int Height { get; }

    public int RefreshRate { get; }

    public override string ToString() => $"{Width}x{Height}@{RefreshRate}";
}

public sealed class GraphicsAdapter
{
    private static GraphicsAdapter _default;

    private GraphicsAdapter(DisplayMode mode)
    {
        CurrentDisplayMode = mode;
    }

    public static GraphicsAdapter DefaultAdapter => _default ??= new GraphicsAdapter(QueryDisplayMode());

    public DisplayMode CurrentDisplayMode { get; }

    private static DisplayMode QueryDisplayMode()
    {
        try
        {
            WindowOptions options = WindowOptions.Default with
            {
                API = GraphicsAPI.None,
                IsVisible = false,
                Size = new Vector2D<int>(1, 1)
            };

            using IWindow probe = Window.Create(options);
            Vector2D<int>? resolution = probe.Monitor?.VideoMode.Resolution;
            if (resolution.HasValue && resolution.Value.X > 0 && resolution.Value.Y > 0)
            {
                return new DisplayMode(resolution.Value.X, resolution.Value.Y);
            }
        }
        catch
        {
        }

        return new DisplayMode(1920, 1080);
    }
}

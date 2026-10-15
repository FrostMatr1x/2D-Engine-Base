namespace Engine;

public sealed class GraphicsDeviceManager
{
    public GraphicsDeviceManager(object game = null)
    {
        Game = game;
    }

    public object Game { get; }

    public int PreferredBackBufferWidth { get; set; }

    public int PreferredBackBufferHeight { get; set; }

    public bool IsFullScreen { get; set; }

    public void ApplyChanges()
    {
    }
}

namespace Engine;

public sealed class GraphicsDeviceManager
{
    public GraphicsDeviceManager(Game game = null)
    {
        Game = game;
        if (game != null)
        {
            game.DeviceManager = this;
        }
    }

    public Game Game { get; }

    public int PreferredBackBufferWidth { get; set; }

    public int PreferredBackBufferHeight { get; set; }

    public bool IsFullScreen { get; set; }

    public bool SynchronizeWithVerticalRetrace { get; set; }

    public void ApplyChanges()
    {
    }
}

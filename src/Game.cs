using Engine.Content;
using Engine.Graphic;

namespace Engine;

public abstract class Game
{
    private bool _exitRequested;

    protected Game()
    {
        Content = new ContentManager("Content");
    }

    public GraphicsDevice GraphicsDevice { get; internal set; }

    public ContentManager Content { get; }

    internal GraphicsDeviceManager DeviceManager { get; set; }

    public GameWindow Window { get; internal set; }

    public bool IsMouseVisible { get; set; } = true;

    public bool IsActive { get; internal set; } = true;

    protected virtual void Initialize()
    {
    }

    protected virtual void LoadContent()
    {
    }

    protected virtual void UnloadContent()
    {
    }

    protected virtual void Update(GameTime gameTime)
    {
    }

    protected virtual void Draw(GameTime gameTime)
    {
    }

    public void Exit()
    {
        _exitRequested = true;
    }

    internal bool ExitRequested => _exitRequested;

    internal void DoInitialize() => Initialize();

    internal void DoLoadContent() => LoadContent();

    internal void DoUpdate(GameTime gameTime) => Update(gameTime);

    internal void DoDraw(GameTime gameTime) => Draw(gameTime);
}

public sealed class GameWindow
{
    private readonly EngineWindow _window;

    internal GameWindow(EngineWindow window)
    {
        _window = window;
    }

    public string Title
    {
        get => _window.Title;
        set => _window.Title = value;
    }

    public Rectangle ClientBounds => new Rectangle(0, 0, _window.FramebufferWidth, _window.FramebufferHeight);

    public bool AllowUserResizing { get; set; }
}

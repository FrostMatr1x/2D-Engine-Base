using System;
using Engine.Graphic;
using Engine.UI;

namespace Engine;

internal sealed class GameHostSelfTestGame : Game
{
    private SpriteBatch _spriteBatch;
    private Texture2D _pixel;
    private Texture2D _alpha;
    private Texture2D _uiTexture;
    private SpriteFont _font;
    private UIElement _panel;
    private bool _loaded;
    private readonly bool _uiTest;

    public GameHostSelfTestGame()
    {
        _uiTest = Environment.GetEnvironmentVariable("ENGINE_UI_SELFTEST") == "1";
    }

    protected override void Initialize()
    {
        Console.WriteLine($"[gamehost selftest] Initialize ui={_uiTest}");
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _pixel = new Texture2D(GraphicsDevice, 8, 8);
        Color[] data = new Color[64];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = Color.White;
        }
        _pixel.SetData(data);

        _alpha = new Texture2D(GraphicsDevice, 4, 4);
        Color[] alphaData = new Color[16];
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                alphaData[y * 4 + x] = x < 2 ? new Color(255, 0, 0, 255) : new Color(0, 0, 0, 0);
            }
        }
        _alpha.SetData(alphaData);

        if (_uiTest)
        {
            _font = Content.Load<SpriteFont>("Font");
            _uiTexture = Content.Load<Texture2D>("UICollider");
            UIText.Init(_font);
            UIElement.Load(Content);

            _panel = new UIElement("panel", new System.Numerics.Vector2(640f, 360f), new System.Numerics.Vector2(240f, 120f));
            UIImage image = _panel.AddComponent<UIImage>();
            image.Init(_uiTexture);

            UIText text = _panel.AddComponent<UIText>();
            text.Init("AVATAR Гойда", 2f, new System.Numerics.Vector2(0f, 0f), _font);
        }

        _loaded = true;
        Console.WriteLine("[gamehost selftest] LoadContent");
    }

    protected override void Update(GameTime gameTime)
    {
        if (_uiTest)
        {
            UIElement.Update(gameTime);
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        if (!_loaded)
        {
            return;
        }

        GraphicsDevice.Clear(new Color(20, 24, 40, 255));

        Viewport viewport = GraphicsDevice.Viewport;
        _spriteBatch.Begin(SpriteSortMode.Deferred, SamplerState.PointClamp);
        _spriteBatch.Draw(
            _pixel,
            new Rectangle(viewport.Width / 2 - 260, viewport.Height / 2 - 40, 120, 80),
            Color.White);

        _spriteBatch.Draw(
            _alpha,
            new Rectangle(viewport.Width / 2 - 260, viewport.Height / 2 - 200, 240, 120),
            Color.White);
        _spriteBatch.End();

        if (_uiTest)
        {
            UIElement.Draw(_spriteBatch);
        }
    }
}

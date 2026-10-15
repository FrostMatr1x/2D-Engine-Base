using System;
using System.Globalization;
using System.Numerics;
using Engine.Content;
using Engine.Graphic;
using Engine.Graphic.Vulkan;
using Engine.Input;
using EngineInputHelper = Engine.Input.InputHelper;
using Engine.Sim;

namespace Engine;

public sealed class Application
{
    private const int InstanceCount = 100000;
    private const int FramesInFlight = 2;

    private readonly EngineWindow _window;
    private readonly Time _time = new();
    private readonly Camera2D _camera = new();
    private readonly FixedStepLoop _fixedStep = new();
    private readonly BoidsSim _simulation = new();

    private VulkanContext _context;
    private Renderer2D _renderer;
    private GraphicsDevice _graphicsDevice;
    private ContentManager _content;
    private InputController _input;
    private SpriteBatch _spriteBatch;
    private SpriteFont _font;
    private Texture2D _pixelTexture;
    private Texture2D _testTexture;
    private Instance2D[] _renderState = Array.Empty<Instance2D>();
    private GameTime _gameTime = new();
    private double _hudTimer;
    private double _elapsed;
    private long _clampFrames;
    private readonly double _exitAfter;
    private float _fps;
    private long _frameCount;
    private bool _disposed;

    public Application(EngineWindow window)
    {
        _window = window;
        _exitAfter = double.TryParse(
            Environment.GetEnvironmentVariable("ENGINE_EXIT_AFTER"),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out double exitAfterSeconds)
            ? exitAfterSeconds
            : 0.0;
    }

    public static void Main(string[] args)
    {
        if (Environment.GetEnvironmentVariable("ENGINE_GAMEHOST_SELFTEST") == "1")
        {
            GameHost.Run(new GameHostSelfTestGame());
            return;
        }

        var window = new EngineWindow("2D Engine — Vulkan");
        var application = new Application(window);
        application.Run();
    }

    public void Run()
    {
        _window.Load += OnLoad;
        _window.Update += OnUpdate;
        _window.Render += OnRender;
        _window.Closing += OnClosing;
        _window.FramebufferResize += OnFramebufferResize;
        _window.Run();
    }

    private void OnLoad()
    {
        _context = new VulkanContext();
        _context.Init(_window);

        _renderer = new Renderer2D(_context, _window, InstanceCount, FramesInFlight);
        _graphicsDevice = new GraphicsDevice(_context, _renderer);
        _input = new InputController(_window, _camera);

        _content = new ContentManager(_graphicsDevice);
        _font = _content.Load<SpriteFont>("Font");
        _testTexture = _content.Load<Texture2D>("UICollider");

        _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });

        _spriteBatch = new SpriteBatch(_context, FramesInFlight);

        _simulation.Bounds = new Vector2(2000f, 1500f);
        _simulation.Init(InstanceCount, 12345);
        _renderState = new Instance2D[InstanceCount];

        _camera.Position = _simulation.Bounds * 0.5f;
        _camera.Zoom = 1.0f;

        if (Environment.GetEnvironmentVariable("ENGINE_INPUT_SELFTEST") == "1")
        {
            RunInputSelfTest();
        }

        if (Environment.GetEnvironmentVariable("ENGINE_CONTENT_SELFTEST") == "1")
        {
            RunContentSelfTest();
        }

        if (Environment.GetEnvironmentVariable("ENGINE_TEXTURE_SELFTEST") == "1")
        {
            RunTextureSelfTest();
        }

        EngineInputHelper.InputKeyboard.AddKeyEvent(Keys.Escape, _ => _window.Close(), "exit", false);
        EngineInputHelper.InputKeyboard.AddKeyEvent(Keys.Space, _ => _clampFrames++, "clamp-space", true, ClampKey: true);

        Console.WriteLine($"GPU: {_context.SelectedDevice.DeviceName}; validation: {(_context.ValidationEnabled ? "on" : "off")}");
        Console.WriteLine($"[content] font '{_font.Atlas.Width}x{_font.Atlas.Height}', glyphs={_font.Glyphs.Count}, texture {_testTexture.Width}x{_testTexture.Height}");
    }

    private void OnFramebufferResize(Silk.NET.Maths.Vector2D<int> size)
    {
        _renderer?.NotifyFramebufferResized();
    }

    private void OnUpdate(double delta)
    {
        if (_disposed)
        {
            return;
        }

        _time.Update();
        _gameTime.Update(_time);
        Update(_gameTime);
    }

    private void Update(GameTime gameTime)
    {
        _input.Update();
        _fixedStep.Update((float)gameTime.ElapsedGameTime.TotalSeconds, _simulation);
    }

    private void OnRender(double delta)
    {
        if (_disposed)
        {
            return;
        }

        _frameCount++;
        Render(_gameTime);

        _elapsed += delta;
        if (_exitAfter > 0.0 && _elapsed >= _exitAfter)
        {
            _window.Close();
            return;
        }

        UpdateStats(delta);
    }

    private void Render(GameTime gameTime)
    {
        _graphicsDevice.Clear(new Color(13, 15, 23, 255));

        _simulation.GetRenderState(_renderState, _fixedStep.Alpha);
        DrawSprites(gameTime);
        _renderer.DrawFrame(_camera, _renderState, _spriteBatch);
    }

    private void UpdateStats(double delta)
    {
        if (delta > 0.0)
        {
            float instant = (float)(1.0 / delta);
            _fps = _fps <= 0f ? instant : _fps + (instant - _fps) * 0.1f;
        }

        _hudTimer += delta;
        if (_hudTimer >= 0.25)
        {
            _hudTimer = 0.0;
        }
    }

    private void DrawSprites(GameTime gameTime)
    {
        Vector2 center = _camera.Position;
        Vector2 origin = new Vector2(_testTexture.Width / 2f, _testTexture.Height / 2f);

        _spriteBatch.Begin(SpriteSortMode.Deferred, SamplerState.PointClamp);
        _spriteBatch.Draw(_testTexture, center + new Vector2(-200f, 0f), null, Color.White, 0f, origin, new Vector2(4f, 4f), SpriteEffects.None, 0f);
        _spriteBatch.Draw(_testTexture, center, null, Color.White, 0.7853982f, origin, new Vector2(4f, 4f), SpriteEffects.None, 0f);
        _spriteBatch.Draw(_testTexture, center + new Vector2(200f, 0f), null, Color.White, 0f, origin, new Vector2(4f, 4f), SpriteEffects.FlipHorizontally, 0f);
        _spriteBatch.Draw(_pixelTexture, new Rectangle((int)center.X - 60, (int)center.Y - 160, 120, 120), new Color(220, 40, 40, 128));
        _spriteBatch.DrawString(_font, "Гойда", center + new Vector2(-240f, 150f), Color.White, 0.35f, Vector2.Zero, new Vector2(0.6f, 0.6f), SpriteEffects.None, 0f);
        DrawHud(gameTime);
        _spriteBatch.End();
    }

    private void DrawHud(GameTime gameTime)
    {
        Viewport viewport = _graphicsDevice.Viewport;
        float halfWidth = viewport.Width * 0.5f / _camera.Zoom;
        float halfHeight = viewport.Height * 0.5f / _camera.Zoom;

        float lineHeight = _font.LineHeight;

        Vector2 position = new Vector2(
            _camera.Position.X - halfWidth + 12f,
            _camera.Position.Y - halfHeight + 12f + lineHeight);

        _spriteBatch.DrawString(_font, "2D Engine — Vulkan", position, Color.White);
        position.Y += lineHeight;
        _spriteBatch.DrawString(
            _font,
            $"{_fps:0} fps  N={InstanceCount}  steps={_fixedStep.LastStepCount}  clamp={_clampFrames}  t={gameTime.TotalGameTime.TotalSeconds:0.0}s",
            position,
            new Color(200, 220, 255, 255));
        position.Y += lineHeight;
        _spriteBatch.DrawString(_font, "AVATAR Гойда 0123", position, new Color(255, 210, 120, 255));
    }

    private void RunContentSelfTest()
    {
        bool fontCached = ReferenceEquals(_font, _content.Load<SpriteFont>("Font"));
        bool textureCached = ReferenceEquals(_testTexture, _content.Load<Texture2D>("UICollider"));

        string missingType = "-";
        string missingMessage = "-";
        try
        {
            _content.Load<Texture2D>("__definitely_missing__");
        }
        catch (Exception ex)
        {
            missingType = ex.GetType().Name;
            missingMessage = ex.Message;
        }

        Vector2 measured = _font.MeasureString("AVATAR Гойда");
        double kernAV = _font.GetKerning('A', 'V');
        double kernGo = _font.GetKerning('Г', 'о');

        bool pass = fontCached && textureCached
            && missingType == "FileNotFoundException"
            && measured.X > 0f && measured.Y > 0f;
        Console.WriteLine(
            $"[content selftest] fontCached={fontCached} textureCached={textureCached} " +
            $"missing={missingType} measure={measured.X:0.0}x{measured.Y:0.0} kern(A,V)={kernAV:0.00} kern(Г,о)={kernGo:0.00} " +
            $"-> {(pass ? "PASS" : "FAIL")}");
        Console.WriteLine($"[content selftest] missing message: {missingMessage}");
    }

    private void RunTextureSelfTest()
    {
        var source = new Color[16];
        for (int i = 0; i < source.Length; i++)
        {
            source[i] = new Color((byte)(i * 16), (byte)(255 - i * 16), (byte)(i * 7), i == 0 ? (byte)0 : (byte)255);
        }

        var texture = new Texture2D(_graphicsDevice, 4, 4);
        texture.SetData(source);
        var readBack = new Color[16];
        texture.GetData(readBack);
        texture.Dispose();

        bool roundTrip = true;
        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] != readBack[i])
            {
                roundTrip = false;
                break;
            }
        }

        var tilePixels = new Color[_testTexture.Width * _testTexture.Height];
        _testTexture.GetData(tilePixels);
        int gpuNonEmpty = 0;
        foreach (Color color in tilePixels)
        {
            if (color.A != 0)
            {
                gpuNonEmpty++;
            }
        }

        int expectedNonEmpty = CountCpuNonEmpty("UICollider");

        bool pass = roundTrip && gpuNonEmpty == expectedNonEmpty;
        Console.WriteLine(
            $"[texture selftest] roundTrip={roundTrip} gpuNonEmpty={gpuNonEmpty} cpuNonEmpty={expectedNonEmpty} -> {(pass ? "PASS" : "FAIL")}");
    }

    private int CountCpuNonEmpty(string asset)
    {
        string path = _content.ResolveExisting(asset, ".png", ".bmp", ".jpg", ".jpeg", ".tga", ".gif");
        using SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> image =
            SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(path);
        int count = 0;
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                Span<SixLabors.ImageSharp.PixelFormats.Rgba32> row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    if (row[x].A != 0)
                    {
                        count++;
                    }
                }
            }
        });
        return count;
    }

    private static void RunInputSelfTest()
    {
        EngineInputHelper.Clear();

        int single = 0;
        int repeat = 0;
        int held = 0;

        EngineInputHelper.InputKeyboard.AddKeyEvent(Keys.A, _ => single++, "single", false);
        EngineInputHelper.InputKeyboard.AddKeyEvent(Keys.C, _ => repeat++, "repeat", true);
        EngineInputHelper.InputKeyboard.AddKeyEvent(Keys.B, _ => held++, "clamp", true, ClampKey: true);

        bool aDown = false;
        EngineInputHelper.KeyStateProvider = key => key == Keys.A && aDown;
        EngineInputHelper.Update();
        aDown = true;
        EngineInputHelper.Update();
        EngineInputHelper.Update();

        bool cDown = false;
        EngineInputHelper.KeyStateProvider = key => key == Keys.C && cDown;
        cDown = true;
        EngineInputHelper.Update();
        EngineInputHelper.Update();
        cDown = false;
        EngineInputHelper.Update();
        cDown = true;
        EngineInputHelper.Update();

        bool bDown = false;
        EngineInputHelper.KeyStateProvider = key => key == Keys.B && bDown;
        bDown = true;
        EngineInputHelper.Update();
        EngineInputHelper.Update();

        bool pass = single == 1 && repeat == 2 && held == 2 && EngineInputHelper.KeyEventCount == 2;
        Console.WriteLine($"[input selftest] single={single} repeat={repeat} held={held} events={EngineInputHelper.KeyEventCount} -> {(pass ? "PASS" : "FAIL")}");

        EngineInputHelper.Clear();
    }

    private void OnClosing()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        Console.WriteLine($"rendered {_frameCount} frames, {_fps:0} fps, {InstanceCount} instances");

        _context?.Vk.DeviceWaitIdle(_context.Device);
        _pixelTexture?.Dispose();
        _content?.Dispose();
        _spriteBatch?.Dispose();
        _input?.Dispose();
        _renderer?.Dispose();
        _context?.Dispose();
    }
}

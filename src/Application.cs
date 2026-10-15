using System;
using System.Numerics;
using Engine.Graphic;
using Engine.Graphic.Vulkan;
using Engine.Input;
using Engine.Sim;
using SixLabors.ImageSharp.PixelFormats;

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
    private InputController _input;
    private SpriteBatch _spriteBatch;
    private Texture2D _pixelTexture;
    private Texture2D _testTexture;
    private Instance2D[] _renderState = Array.Empty<Instance2D>();
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
        _exitAfter = double.TryParse(Environment.GetEnvironmentVariable("ENGINE_EXIT_AFTER"), out double exitAfterSeconds)
            ? exitAfterSeconds
            : 0.0;
    }

    public static void Main(string[] args)
    {
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
        _input = new InputController(_window, _camera);

        Texture2D.Init(_context, _renderer.Descriptors);
        _pixelTexture = new Texture2D();
        _testTexture = new Texture2D(CreateTestImage());
        _spriteBatch = new SpriteBatch(_context, FramesInFlight);

        Texture2D transient = new Texture2D(CreateTestImage());
        transient.Dispose();

        _simulation.Bounds = new Vector2(2000f, 1500f);
        _simulation.Init(InstanceCount, 12345);
        _renderState = new Instance2D[InstanceCount];

        _camera.Position = _simulation.Bounds * 0.5f;
        _camera.Zoom = 1.0f;

        if (Environment.GetEnvironmentVariable("ENGINE_INPUT_SELFTEST") == "1")
        {
            RunInputSelfTest();
        }

        InputHelper.InputKeyboard.AddKeyEvent(Keys.Escape, _ => _window.Close(), "exit", false);
        InputHelper.InputKeyboard.AddKeyEvent(Keys.Space, _ => _clampFrames++, "clamp-space", true, ClampKey: true);

        Console.WriteLine($"GPU: {_context.SelectedDevice.DeviceName}; validation: {(_context.ValidationEnabled ? "on" : "off")}");
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
        _input.Update();
        _fixedStep.Update(_time.DeltaTime, _simulation);
    }

    private void OnRender(double delta)
    {
        if (_disposed)
        {
            return;
        }

        _frameCount++;
        _simulation.GetRenderState(_renderState, _fixedStep.Alpha);
        DrawSprites();
        _renderer.DrawFrame(_camera, _renderState, _spriteBatch);

        _elapsed += delta;
        if (_exitAfter > 0.0 && _elapsed >= _exitAfter)
        {
            _window.Close();
            return;
        }

        UpdateHud(delta);
    }

    private void UpdateHud(double delta)
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
            _window.Title = $"2D Engine | {_fps:0} fps | N={InstanceCount} | steps={_fixedStep.LastStepCount} | clamp={_clampFrames}";
        }
    }

    private void DrawSprites()
    {
        Vector2 center = _camera.Position;

        _spriteBatch.Begin(SpriteSortMode.Deferred, SamplerState.PointClamp);
        _spriteBatch.Draw(_testTexture, center + new Vector2(-200f, 0f), null, Color.White, 0f, new Vector2(32f, 32f), new Vector2(4f, 4f), SpriteEffects.None, 0f);
        _spriteBatch.Draw(_testTexture, center, null, Color.White, 0.7853982f, new Vector2(32f, 32f), new Vector2(4f, 4f), SpriteEffects.None, 0f);
        _spriteBatch.Draw(_testTexture, center + new Vector2(200f, 0f), null, Color.White, 0f, new Vector2(32f, 32f), new Vector2(4f, 4f), SpriteEffects.FlipHorizontally, 0f);
        _spriteBatch.Draw(_pixelTexture, new Rectangle((int)center.X - 60, (int)center.Y - 160, 120, 120), new Color(220, 40, 40, 128));
        _spriteBatch.End();
    }

    private static void RunInputSelfTest()
    {
        InputHelper.Clear();

        int single = 0;
        int repeat = 0;
        int held = 0;

        InputHelper.InputKeyboard.AddKeyEvent(Keys.A, _ => single++, "single", false);
        InputHelper.InputKeyboard.AddKeyEvent(Keys.C, _ => repeat++, "repeat", true);
        InputHelper.InputKeyboard.AddKeyEvent(Keys.B, _ => held++, "clamp", true, ClampKey: true);

        bool aDown = false;
        InputHelper.KeyStateProvider = key => key == Keys.A && aDown;
        InputHelper.Update();
        aDown = true;
        InputHelper.Update();
        InputHelper.Update();

        bool cDown = false;
        InputHelper.KeyStateProvider = key => key == Keys.C && cDown;
        cDown = true;
        InputHelper.Update();
        InputHelper.Update();
        cDown = false;
        InputHelper.Update();
        cDown = true;
        InputHelper.Update();

        bool bDown = false;
        InputHelper.KeyStateProvider = key => key == Keys.B && bDown;
        bDown = true;
        InputHelper.Update();
        InputHelper.Update();

        bool pass = single == 1 && repeat == 2 && held == 2 && InputHelper.KeyEventCount == 2;
        Console.WriteLine($"[input selftest] single={single} repeat={repeat} held={held} events={InputHelper.KeyEventCount} -> {(pass ? "PASS" : "FAIL")}");

        InputHelper.Clear();
    }

    private static SixLabors.ImageSharp.Image<Rgba32> CreateTestImage()
    {
        const int size = 64;
        var image = new SixLabors.ImageSharp.Image<Rgba32>(size, size);

        var red = new Rgba32(220, 40, 40, 255);
        var green = new Rgba32(40, 200, 60, 255);
        var blue = new Rgba32(40, 80, 220, 255);
        var yellow = new Rgba32(230, 210, 40, 255);
        var black = new Rgba32(0, 0, 0, 255);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                image[x, y] = y < size / 2
                    ? (x < size / 2 ? red : green)
                    : (x < size / 2 ? blue : yellow);
            }
        }

        for (int y = 2; y < 12; y++)
        {
            for (int x = 2; x < 12; x++)
            {
                image[x, y] = black;
            }
        }

        return image;
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
        _testTexture?.Dispose();
        _pixelTexture?.Dispose();
        _spriteBatch?.Dispose();
        _input?.Dispose();
        _renderer?.Dispose();
        _context?.Dispose();
    }
}

using System;
using System.Numerics;
using Engine.Graphic;
using Engine.Graphic.Vulkan;
using Engine.Input;
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
    private InputController _input;
    private Instance2D[] _renderState = Array.Empty<Instance2D>();
    private double _hudTimer;
    private float _fps;
    private long _frameCount;
    private bool _disposed;

    public Application(EngineWindow window)
    {
        _window = window;
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

        _simulation.Bounds = new Vector2(2000f, 1500f);
        _simulation.Init(InstanceCount, 12345);
        _renderState = new Instance2D[InstanceCount];

        _camera.Position = _simulation.Bounds * 0.5f;
        _camera.Zoom = 1.0f;

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
        _renderer.DrawFrame(_camera, _renderState);

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
            _window.Title = $"2D Engine | {_fps:0} fps | N={InstanceCount} | steps={_fixedStep.LastStepCount}";
        }
    }

    private void OnClosing()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        Console.WriteLine($"rendered {_frameCount} frames, {_fps:0} fps, {InstanceCount} instances");

        _input?.Dispose();
        _renderer?.Dispose();
        _context?.Dispose();
    }
}

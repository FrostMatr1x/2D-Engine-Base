using System;
using System.Numerics;
using Engine.Graphic;
using Silk.NET.Input;
using SilkMouseButton = Silk.NET.Input.MouseButton;

namespace Engine.Input;

public sealed class InputController : IDisposable
{
    private readonly EngineWindow _window;
    private readonly Camera2D _camera;
    private readonly IInputContext _input;
    private readonly IMouse _mouse;
    private readonly IKeyboard _keyboard;
    private Vector2 _lastMouse;
    private bool _panning;

    public InputController(EngineWindow window, Camera2D camera)
    {
        _window = window;
        _camera = camera;
        _input = window.Window.CreateInput();
        _mouse = _input.Mice.Count > 0 ? _input.Mice[0] : null;
        _keyboard = _input.Keyboards.Count > 0 ? _input.Keyboards[0] : null;
        InputHelper.Bind(_keyboard, _mouse);
    }

    public void Update()
    {
        InputHelper.Update();

        if (_mouse == null)
        {
            return;
        }

        Vector2 position = _mouse.Position;
        bool dragging = _mouse.IsButtonPressed(SilkMouseButton.Right) || _mouse.IsButtonPressed(SilkMouseButton.Middle);

        if (dragging)
        {
            if (_panning)
            {
                Vector2 delta = position - _lastMouse;
                _camera.Position -= delta / _camera.Zoom;
            }
            _panning = true;
        }
        else
        {
            _panning = false;
        }

        _lastMouse = position;

        float scroll = _mouse.ScrollWheels.Count > 0 ? _mouse.ScrollWheels[0].Y : 0f;
        if (scroll != 0f)
        {
            _camera.ZoomAt(position, scroll * 0.1f, _window.FramebufferWidth, _window.FramebufferHeight);
        }
    }

    public void Dispose()
    {
        _input?.Dispose();
    }
}

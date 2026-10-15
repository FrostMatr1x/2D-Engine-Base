using System.Numerics;
using Silk.NET.Input;
using SilkMouseButton = Silk.NET.Input.MouseButton;

namespace Engine.Input;

public struct Point
{
    public int X;
    public int Y;

    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }

    public static Point Zero => new Point(0, 0);

    public Vector2 ToVector2() => new Vector2(X, Y);

    public override string ToString() => $"{{X:{X} Y:{Y}}}";
}

public struct MouseState
{
    public int X;
    public int Y;
    public ButtonState LeftButton;
    public ButtonState MiddleButton;
    public ButtonState RightButton;

    public Point Position => new Point(X, Y);

    public bool IsButtonDown(MouseButton button) => button switch
    {
        MouseButton.LeftButton => LeftButton == ButtonState.Pressed,
        MouseButton.RightButton => RightButton == ButtonState.Pressed,
        MouseButton.MiddleButton => MiddleButton == ButtonState.Pressed,
        _ => false
    };
}

public static class Mouse
{
    private static IMouse _mouse;

    internal static void Bind(IMouse mouse)
    {
        _mouse = mouse;
    }

    public static MouseState GetState()
    {
        if (_mouse == null)
        {
            return default;
        }

        Vector2 position = _mouse.Position;
        return new MouseState
        {
            X = (int)position.X,
            Y = (int)position.Y,
            LeftButton = ToButtonState(SilkMouseButton.Left),
            MiddleButton = ToButtonState(SilkMouseButton.Middle),
            RightButton = ToButtonState(SilkMouseButton.Right)
        };
    }

    public static void SetCursorVisible(bool visible)
    {
        if (_mouse == null || _mouse.Cursor == null)
        {
            return;
        }

        _mouse.Cursor.CursorMode = visible ? CursorMode.Normal : CursorMode.Hidden;
    }

    private static ButtonState ToButtonState(SilkMouseButton button)
    {
        return _mouse != null && _mouse.IsButtonPressed(button) ? ButtonState.Pressed : ButtonState.Released;
    }
}

public static class Keyboard
{
    private static IKeyboard _keyboard;

    internal static void Bind(IKeyboard keyboard)
    {
        _keyboard = keyboard;
    }

    public static KeyboardState GetState() => new KeyboardState(_keyboard);
}

public struct KeyboardState
{
    private readonly IKeyboard _keyboard;

    internal KeyboardState(IKeyboard keyboard)
    {
        _keyboard = keyboard;
    }

    public bool IsKeyDown(Keys key) => _keyboard != null && _keyboard.IsKeyPressed(KeyMap.ToSilk(key));

    public bool IsKeyUp(Keys key) => !IsKeyDown(key);
}

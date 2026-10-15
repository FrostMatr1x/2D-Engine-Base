using System;
using System.Collections.Generic;
using System.Linq;
using Silk.NET.Input;

namespace Engine.Input;

public static class InputHelper
{
    private static readonly List<KeyEvent> ButtonEvents = new List<KeyEvent>();
    private static readonly List<KeyEvent> KeysEvents = new List<KeyEvent>();
    private static IKeyboard _keyboard;
    private static IMouse _mouse;

    public static Func<Keys, bool> KeyStateProvider { get; set; }

    public static void Bind(IKeyboard keyboard, IMouse mouse)
    {
        _keyboard = keyboard;
        _mouse = mouse;
    }

    public static void Clear()
    {
        KeysEvents.Clear();
        ButtonEvents.Clear();
        KeyStateProvider = null;
    }

    public static int KeyEventCount => KeysEvents.Count;

    public static int ButtonEventCount => ButtonEvents.Count;

    public class KeyEvent
    {
        public Keys[] key;
        public MouseButton[] button;
        public Action<object[]> actions;
        public string name;
        public bool manyRepeat;
        public bool ClampKey;
        public bool[] IsDown;
        public bool WasKlick = false;

        public KeyEvent(Keys[] key, Action<object[]> actions, string name, bool manyRepeat, bool ClampKey = false)
        {
            button = new[] { MouseButton.Null };
            this.key = key;
            this.actions = actions;
            this.name = name;
            this.manyRepeat = manyRepeat;
            this.ClampKey = ClampKey;
            IsDown = new bool[key.Length];
            for (int i = 0; i < IsDown.Length; i++)
            {
                IsDown[i] = false;
            }
        }

        public KeyEvent(MouseButton[] button, Action<object[]> actions, string name, bool manyRepeat, bool ClampKey = false)
        {
            key = new[] { Keys.None };
            this.button = button;
            this.actions = actions;
            this.name = name;
            this.manyRepeat = manyRepeat;
            this.ClampKey = ClampKey;
            IsDown = new bool[button.Length];
            for (int i = 0; i < IsDown.Length; i++)
            {
                IsDown[i] = false;
            }
        }
    }

    public static void Update()
    {
        for (int i = 0; i < KeysEvents.Count; i++)
        {
            CheckButtons(i, true, out bool wasRemove);
            if (wasRemove)
            {
                i--;
            }
        }

        for (int i = 0; i < ButtonEvents.Count; i++)
        {
            CheckButtons(i, false, out bool wasRemove);
            if (wasRemove)
            {
                i--;
            }
        }
    }

    private static void CheckButtons(int id, bool isKeyboard, out bool wasRemove)
    {
        wasRemove = false;
        KeyEvent keyEvent = isKeyboard ? KeysEvents[id] : ButtonEvents[id];

        for (int i = 0; i < keyEvent.IsDown.Length; i++)
        {
            if (isKeyboard)
            {
                keyEvent.IsDown[i] = IsKeyDown(keyEvent.key[i]);
            }
            else
            {
                keyEvent.IsDown[i] = InputMouse.GetMouseState(keyEvent.button[i]) == ButtonState.Pressed;
            }

            if (keyEvent.IsDown.All(x => x == true))
            {
                if (keyEvent.ClampKey)
                {
                    keyEvent.actions?.Invoke(null);
                }
                else if (!keyEvent.WasKlick)
                {
                    keyEvent.WasKlick = true;
                    keyEvent.actions?.Invoke(null);
                }

                if (!keyEvent.manyRepeat)
                {
                    if (isKeyboard)
                    {
                        KeysEvents.RemoveAt(id);
                    }
                    else
                    {
                        ButtonEvents.RemoveAt(id);
                    }
                    wasRemove = true;
                    return;
                }
            }

            if (!keyEvent.ClampKey)
            {
                if (keyEvent.IsDown.All(x => x == false))
                {
                    keyEvent.WasKlick = false;
                }
            }
        }
    }

    private static bool IsKeyDown(Keys key)
    {
        if (KeyStateProvider != null)
        {
            return KeyStateProvider(key);
        }
        return _keyboard != null && _keyboard.IsKeyPressed(KeyMap.ToSilk(key));
    }

    public static class InputKeyboard
    {
        public static void RemoveKeyEvent(Keys key, string name)
        {
            for (int i = KeysEvents.Count - 1; i >= 0; i--)
            {
                if (KeysEvents[i].key[0] == key && KeysEvents[i].name == name)
                {
                    KeysEvents.RemoveAt(i);
                    return;
                }
            }
            Console.WriteLine("Error: Failed to delete action");
        }

        public static void RemoveKeyEvent(Keys[] keys, string name)
        {
            for (int i = KeysEvents.Count - 1; i >= 0; i--)
            {
                if (KeysEvents[i].key.Length != keys.Length)
                {
                    continue;
                }

                bool match = true;
                for (int j = 0; j < KeysEvents[i].key.Length; j++)
                {
                    if (KeysEvents[i].key[j] != keys[j])
                    {
                        match = false;
                        break;
                    }
                }

                if (match && KeysEvents[i].name == name)
                {
                    KeysEvents.RemoveAt(i);
                    return;
                }
            }
            Console.WriteLine("Error: Failed to delete action");
        }

        public static void AddKeyEvent(Keys key, Action<object[]> actions, string name, bool manyRepeat, bool ClampKey = false)
        {
            KeysEvents.Add(new KeyEvent(new[] { key }, actions, name, manyRepeat, ClampKey));
        }

        public static void AddKeyEvent(Keys[] key, Action<object[]> actions, string name, bool manyRepeat, bool ClampKey = false)
        {
            KeysEvents.Add(new KeyEvent(key, actions, name, manyRepeat, ClampKey));
        }
    }

    public static class InputMouse
    {
        public static void RemoveButtonEvent(MouseButton button, string name)
        {
            for (int i = ButtonEvents.Count - 1; i >= 0; i--)
            {
                if (ButtonEvents[i].button[0] == button && ButtonEvents[i].name == name)
                {
                    ButtonEvents.RemoveAt(i);
                    return;
                }
            }
            Console.WriteLine("Error: Failed to delete action");
        }

        public static void RemoveButtonEvent(MouseButton[] buttons, string name)
        {
            for (int i = ButtonEvents.Count - 1; i >= 0; i--)
            {
                if (ButtonEvents[i].button.Length != buttons.Length)
                {
                    continue;
                }

                bool match = true;
                for (int j = 0; j < ButtonEvents[i].button.Length; j++)
                {
                    if (ButtonEvents[i].button[j] != buttons[j])
                    {
                        match = false;
                        break;
                    }
                }

                if (match && ButtonEvents[i].name == name)
                {
                    ButtonEvents.RemoveAt(i);
                    return;
                }
            }
            Console.WriteLine("Error: Failed to delete action");
        }

        public static void AddButtonEvent(MouseButton button, Action<object[]> actions, string name, bool manyRepeat, bool ClampKey = false)
        {
            ButtonEvents.Add(new KeyEvent(new[] { button }, actions, name, manyRepeat, ClampKey));
        }

        public static void AddButtonEvent(MouseButton[] buttons, Action<object[]> actions, string name, bool manyRepeat, bool ClampKey = false)
        {
            ButtonEvents.Add(new KeyEvent(buttons, actions, name, manyRepeat, ClampKey));
        }

        public static ButtonState GetMouseState(MouseButton button)
        {
            if (_mouse == null || button == MouseButton.Null)
            {
                return ButtonState.Released;
            }

            Silk.NET.Input.MouseButton silkButton = button switch
            {
                MouseButton.LeftButton => Silk.NET.Input.MouseButton.Left,
                MouseButton.RightButton => Silk.NET.Input.MouseButton.Right,
                MouseButton.MiddleButton => Silk.NET.Input.MouseButton.Middle,
                _ => Silk.NET.Input.MouseButton.Unknown
            };

            return _mouse.IsButtonPressed(silkButton) ? ButtonState.Pressed : ButtonState.Released;
        }
    }
}

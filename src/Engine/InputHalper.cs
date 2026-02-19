using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Input;

namespace Engine;

public enum MouseButton { LeftButton, RightButton, MiddleButton, Null }
/// <summary>
/// Статический класс для обработки событий ввода с клавиатуры и мыши. Управляет регистрацией, удалением и проверкой состояния нажатия клавиш и кнопок мыши.
/// </summary>
public static class InputHelper
{
    /// <summary>
    /// Список событий, связанных с нажатием клавиш.
    /// </summary>
    private static List<KeyEvent> ButtonsEvents = new List<KeyEvent>();
    
    /// <summary>
    /// Список событий, связанных с нажатием клавиш.
    /// </summary>
    private static List<KeyEvent> KeysEvents = new List<KeyEvent>();

    /// <summary>
    /// Класс, представляющий событие ввода с клавиатуры или мыши.
    /// </summary>
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

        /// <summary>
        /// Конструктор события для клавиатуры.
        /// </summary>
        /// <param name="key">Массив клавиш.</param>
        /// <param name="actions">Действие, выполняемое при активации.</param>
        /// <param name="name">Имя события.</param>
        /// <param name="manyRepeat">Флаг повторения.</param>
        /// <param name="ClampKey">Флаг блокировки повторного выполнения.</param>
        public KeyEvent(Keys[] key, Action<object[]> actions, string name, bool manyRepeat, bool ClampKey = false)
        {
            button = new MouseButton[] { MouseButton.Null };
            this.key = key;
            this.actions = actions;
            this.name = name;
            this.manyRepeat = manyRepeat;
            this.ClampKey = ClampKey;
            IsDown = new bool[key.Length];
            for (int i = 0; i < IsDown.Length; i++)
                IsDown[i] = false;
        }

        /// <summary>
        /// Конструктор события для мыши.
        /// </summary>
        /// <param name="button">Массив кнопок мыши.</param>
        /// <param name="actions">Действие, выполняемое при активации.</param>
        /// <param name="name">Имя события.</param>
        /// <param name="manyRepeat">Флаг повторения.</param>
        /// <param name="ClampKey">Флаг блокировки повторного выполнения.</param>
        public KeyEvent(MouseButton[] button, Action<object[]> actions, string name, bool manyRepeat, bool ClampKey = false)
        {
            key = new Keys[] { Keys.None };
            this.button = button;
            this.actions = actions;
            this.name = name;
            this.manyRepeat = manyRepeat;
            this.ClampKey = ClampKey;
            IsDown = new bool[button.Length];
            for (int i = 0; i < IsDown.Length; i++)
                IsDown[i] = false;
        }
    }

    /// <summary>
    /// Обновляет состояние всех зарегистрированных событий ввода.
    /// </summary>
    public static void Update()
    {
        for (int i = 0; i < KeysEvents.Count; i++)
        {
            CheckButtons(i, true, out bool wasRemove);
            if (wasRemove) i--;
        }
        for (int i = 0; i < ButtonsEvents.Count; i++)
        {
            CheckButtons(i, false, out bool wasRemove);
            if (wasRemove) i--;
        }
    }

    /// <summary>
    /// Проверяет состояние клавиш/кнопок мыши для указанного события и выполняет действие при необходимости.
    /// </summary>
    /// <param name="id">Индекс события в списке.</param>
    /// <param name="isKeyboard">Флаг, указывающий, является ли событие клавиатурным.</param>
    /// <param name="wasRemove">Флаг, указывающий, было ли событие удалено.</param>
    private static void CheckButtons(int id, bool isKeyboard, out bool wasRemove)
    {
        wasRemove = false;
        KeyEvent keyEvent = isKeyboard ? KeysEvents[id] : ButtonsEvents[id];
        for (int i = 0; i < keyEvent.IsDown.Length; i++)
        {
            if (isKeyboard)
            {
                keyEvent.IsDown[i] = Keyboard.GetState().IsKeyDown(keyEvent.key[i]);
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
                    if (isKeyboard) KeysEvents.RemoveAt(id);
                    else ButtonsEvents.RemoveAt(id);
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

    /// <summary>
    /// Вложенный класс для работы с событиями клавиатуры.
    /// </summary>
    public static class InputKeyboard
    {
        /// <summary>
        /// Удаляет событие клавиатуры по одиночной клавише и имени.
        /// </summary>
        /// <param name="key">Клавиша.</param>
        /// <param name="name">Имя события.</param>
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

        /// <summary>
        /// Удаляет событие клавиатуры по массиву клавиш и имени.
        /// </summary>
        /// <param name="keys">Массив клавиш.</param>
        /// <param name="name">Имя события.</param>
        public static void RemoveKeyEvent(Keys[] keys, string name)
        {
            bool match = false;
            for (int i = KeysEvents.Count - 1; i >= 0; i--)
            {
                if (keys.Length == KeysEvents[i].key.Length)
                {
                    for (int j = 0; j < KeysEvents[i].key.Length; j++)
                    {
                        if (KeysEvents[i].key[j] == keys[j])
                        {
                            match = true;
                        }
                        else
                        {
                            match = false;
                            break;
                        }
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

        /// <summary>
        /// Добавляет событие клавиатуры для одиночной клавиши.
        /// </summary>
        /// <param name="key">Клавиша.</param>
        /// <param name="actions">Действие, выполняемое при активации.</param>
        /// <param name="name">Имя события.</param>
        /// <param name="manyRepeat">Флаг повторения.</param>
        /// <param name="ClampKey">Флаг блокировки повторного выполнения.</param>
        public static void AddKeyEvent(Keys key, Action<object[]> actions, string name, bool manyRepeat, bool ClampKey = false)
        {
            KeysEvents.Add(new KeyEvent(new Keys[] { key }, actions, name, manyRepeat, ClampKey));
        }

        /// <summary>
        /// Добавляет событие клавиатуры для массива клавиш.
        /// </summary>
        /// <param name="key">Массив клавиш.</param>
        /// <param name="actions">Действие, выполняемое при активации.</param>
        /// <param name="name">Имя события.</param>
        /// <param name="manyRepeat">Флаг повторения.</param>
        /// <param name="ClampKey">Флаг блокировки повторного выполнения.</param>
        public static void AddKeyEvent(Keys[] key, Action<object[]> actions, string name, bool manyRepeat, bool ClampKey = false)
        {
            KeysEvents.Add(new KeyEvent(key, actions, name, manyRepeat, ClampKey));
        }
    }

    /// <summary>
    /// Вложенный класс для работы с событиями мыши.
    /// </summary>
    public static class InputMouse
    {
        /// <summary>
        /// Удаляет событие мыши по одиночной кнопке и имени.
        /// </summary>
        /// <param name="button">Кнопка мыши.</param>
        /// <param name="name">Имя события.</param>
        public static void RemoveButtonEvent(MouseButton button, string name)
        {
            for (int i = ButtonsEvents.Count - 1; i >= 0; i--)
            {
                if ((int)ButtonsEvents[i].button[0] == (int)button && ButtonsEvents[i].name == name)
                {
                    ButtonsEvents.RemoveAt(i);
                    return;
                }
            }
            Console.WriteLine("Error: Failed to delete action");
        }

        /// <summary>
        /// Удаляет событие мыши по массиву кнопок и имени.
        /// </summary>
        /// <param name="buttons">Массив кнопок мыши.</param>
        /// <param name="name">Имя события.</param>
        public static void RemoveButtonEvent(MouseButton[] buttons, string name)
        {
            bool match = false;
            for (int i = ButtonsEvents.Count - 1; i >= 0; i--)
            {
                if (buttons.Length == ButtonsEvents[i].button.Length)
                {
                    for (int j = 0; j < ButtonsEvents[i].button.Length; j++)
                    {
                        if (ButtonsEvents[i].button[j] == buttons[j])
                        {
                            match = true;
                        }
                        else
                        {
                            return;
                        }
                    }

                }
                if (match && ButtonsEvents[i].name == name)
                {
                    ButtonsEvents.RemoveAt(i);
                    return;
                }
            }
            Console.WriteLine("Error: Failed to delete action");
        }

        /// <summary>
        /// Добавляет событие мыши для одиночной кнопки.
        /// </summary>
        /// <param name="button">Кнопка мыши.</param>
        /// <param name="actions">Действие, выполняемое при активации.</param>
        /// <param name="name">Имя события.</param>
        /// <param name="manyRepeat">Флаг повторения.</param>
        /// <param name="ClampKey">Флаг блокировки повторного выполнения.</param>
        public static void AddButtonEvent(MouseButton button, Action<object[]> actions, string name, bool manyRepeat, bool ClampKey = false)
        {
            ButtonsEvents.Add(new KeyEvent(new MouseButton[] { button }, actions, name, manyRepeat, ClampKey));
        }

        /// <summary>
        /// Добавляет событие мыши для массива кнопок.
        /// </summary>
        /// <param name="buttons">Массив кнопок мыши.</param>
        /// <param name="actions">Действие, выполняемое при активации.</param>
        /// <param name="name">Имя события.</param>
        /// <param name="manyRepeat">Флаг повторения.</param>
        /// <param name="ClampKey">Флаг блокировки повторного выполнения.</param>
        public static void AddButtonEvent(MouseButton[] buttons, Action<object[]> actions, string name, bool manyRepeat, bool ClampKey = false)
        {
            ButtonsEvents.Add(new KeyEvent(buttons, actions, name, manyRepeat, ClampKey));
        }

        /// <summary>
        /// Получает текущее состояние указанной кнопки мыши.
        /// </summary>
        /// <param name="button">Кнопка мыши.</param>
        /// <returns>Состояние кнопки (Pressed/Released).</returns>
        public static ButtonState GetMouseState(MouseButton button)
        {
            if (button == MouseButton.LeftButton)
            {
                return Mouse.GetState().LeftButton;
            }
            if (button == MouseButton.MiddleButton)
            {
                return Mouse.GetState().MiddleButton;
            }
            if (button == MouseButton.RightButton)
            {
                return Mouse.GetState().RightButton;
            }
            return ButtonState.Released;
        }
    }
}

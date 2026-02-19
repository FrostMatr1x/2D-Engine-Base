using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Engine;

/// <summary>
/// Представляет событие, запланированное на выполнение через определённый промежуток времени.
/// Событие содержит имя, действие и время окончания ожидания.
/// </summary>
public class TimeEvent
{
    /// <summary>
    /// Имя события. Используется для идентификации и удаления события из системы.
    /// </summary>
    public string Name;

    /// <summary>
    /// Действие, которое будет выполнено по истечении заданного времени.
    /// Может быть null, в этом случае событие просто удаляется без вызова.
    /// </summary>
    public Action<GameTime> Action;

    /// <summary>
    /// Время (в секундах с начала игры), когда событие должно быть выполнено.
    /// Определяется как текущее время + задержка при добавлении события.
    /// </summary>
    public float EndTime;
}

/// <summary>
/// Управляет системой отложенных событий, которые выполняются после указанной задержки.
/// Позволяет планировать, отслеживать и отменять события во времени.
/// </summary>
public class TimeEvents
{
    /// <summary>
    /// Список активных отложенных событий, ожидающих выполнения.
    /// События проверяются и удаляются при наступлении их времени выполнения.
    /// </summary>
    private static List<TimeEvent> events = new List<TimeEvent>();

    /// <summary>
    /// Последнее значение GameTime, полученное при обновлении системы.
    /// Используется для корректного расчёта времени выполнения новых событий.
    /// </summary>
    private static GameTime _gameTime;

    /// <summary>
    /// Обновляет систему отложенных событий. Проверяет все события и выполняет те,
    /// чьё время наступило. Вызывается каждый кадр игрового цикла.
    /// </summary>
    /// <param name="gameTime">Текущее игровое время, предоставляемое движком XNA/Monogame.</param>
    public static void Update(GameTime gameTime)
    {
        _gameTime = gameTime;
        for (int i = events.Count - 1; i >= 0; i--)
        {
            var nextEvent = events[i];
            if (gameTime.TotalGameTime.TotalSeconds >= nextEvent.EndTime)
            {
                nextEvent.Action?.Invoke(gameTime);
                events.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Добавляет новое отложенное событие в систему.
    /// Событие будет выполнено через указанное количество секунд.
    /// </summary>
    /// <param name="howLongWait">Задержка в секундах перед выполнением действия.</param>
    /// <param name="action">Действие, которое будет выполнено по истечении задержки.</param>
    /// <param name="name">Имя события, используемое для последующего удаления.</param>
    public static void AddEvent(float howLongWait, Action<GameTime> action, string name)
    {
        if (_gameTime != null)
        {
            events.Add(new TimeEvent { Action = action, Name = name, EndTime = (float)_gameTime.TotalGameTime.TotalSeconds + howLongWait });
        }
        else
        {
            events.Add(new TimeEvent { Action = action, Name = name, EndTime = howLongWait });
        }
    }

    /// <summary>
    /// Удаляет событие из системы по его имени.
    /// Если событие уже было выполнено или не существует, метод ничего не делает.
    /// </summary>
    /// <param name="name">Имя события, которое необходимо удалить.</param>
    public static void RemoveEvent(string name)
    {
        events.Remove(events.Find(e => e.Name == name));
    }
}
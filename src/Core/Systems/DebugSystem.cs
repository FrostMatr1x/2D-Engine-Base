using System;
using System.Numerics;

namespace Engine;

/// <summary>
/// Предоставляет набор статических методов для записи сообщений в консоль с различными уровнями важности и цветовым кодированием.
/// </summary>
public static class Debug
{
    public static event Action<ConsoleMessage> OnLogAdded;
    public static event Action<int> OnLogRemoved;

    /// <summary>
    /// Хранит последнее записанное сообщение для обнаружения дубликатов.
    /// </summary>
    private static string LastLog;

    /// <summary>
    /// Считает количество последовательных повторений последнего записанного сообщения.
    /// </summary>
    private static int LogCount = 0;
    
    /// <summary>
    /// Записывает обычное сообщение в консоль с указанным текстом и цветом.
    /// Если сообщение повторяется, перезаписывает предыдущую строку с увеличенным счётчиком.
    /// </summary>
    /// <param name="text">Текст сообщения для записи.</param>
    /// <param name="color">Цвет текста сообщения. По умолчанию — белый.</param>
    public static void Log(string text, Vector4? color = null)
    {
        color ??= new Vector4(0.9f, 0.9f, 0.9f, 1);
        OnLogAdded?.Invoke(new ConsoleMessage(GetCorrectText(text), color.Value));
    }

    /// <summary>
    /// Записывает сообщение об ошибке в консоль красным цветом.
    /// Если сообщение повторяется, перезаписывает предыдущую строку с увеличенным счётчиком.
    /// </summary>
    /// <param name="text">Текст сообщения об ошибке для записи.</param>
    public static void Error(string text)
    {
        OnLogAdded?.Invoke(new ConsoleMessage(GetCorrectText(text), new(0.7f, 0, 0, 1)));
    }

    /// <summary>
    /// Записывает предупреждающее сообщение в консоль жёлтым цветом.
    /// Если сообщение повторяется, перезаписывает предыдущую строку с увеличенным счётчиком.
    /// </summary>
    /// <param name="text">Текст предупреждающего сообщения для записи.</param>
    public static void Warning(string text)
    {
        OnLogAdded?.Invoke(new ConsoleMessage(GetCorrectText(text), new(1, 1, 0, 1)));
    }

    /// <summary>
    /// Записывает сообщение об успешном выполнении в консоль зелёным цветом.
    /// Если сообщение повторяется, перезаписывает предыдущую строку с увеличенным счётчиком.
    /// </summary>
    /// <param name="text">Текст сообщения об успехе для записи.</param>
    public static void Success(string text)
    {
        OnLogAdded?.Invoke(new ConsoleMessage(GetCorrectText(text), new(0, 1, 0, 1)));
    }

    /// <summary>
    /// Обрабатывает входной текст, определяя, является ли он дубликатом последнего записанного сообщения.
    /// Если это дубликат, возвращает форматированную строку с количеством повторений.
    /// В противном случае вставляет новую строку и сбрасывает счётчик.
    /// </summary>
    /// <param name="text">Текст сообщения для обработки.</param>
    /// <returns>
    /// Форматированная строка с количеством повторений, если сообщение дублируется;
    /// в противном случае — исходный текст в новой строке.
    /// </returns>
    private static string GetCorrectText(string text)
    {
        if (LastLog == text)
        {
            LogCount++;
            OnLogRemoved?.Invoke(0);
            return $"({LogCount}) {text}";
        }
        else
        {
            Console.WriteLine(" ");
            LastLog = text;
            LogCount = 1;
            return text;
        }
    }

    public record ConsoleMessage (
        string Message,
        Vector4 Color
    );
}
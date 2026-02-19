using System;

namespace Engine;

/// <summary>
/// Предоставляет набор статических методов для записи сообщений в консоль с различными уровнями важности и цветовым кодированием.
/// </summary>
public static class Debug
{
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
    public static void Log(string text, ConsoleColor color = ConsoleColor.White)
    {
        Console.ForegroundColor = color;
        Console.Write(GetCorrectText(text));
        Console.ResetColor();
    }

    /// <summary>
    /// Записывает сообщение об ошибке в консоль красным цветом.
    /// Если сообщение повторяется, перезаписывает предыдущую строку с увеличенным счётчиком.
    /// </summary>
    /// <param name="text">Текст сообщения об ошибке для записи.</param>
    public static void Error(string text)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write(GetCorrectText(text));
        Console.ResetColor();
    }

    /// <summary>
    /// Записывает предупреждающее сообщение в консоль жёлтым цветом.
    /// Если сообщение повторяется, перезаписывает предыдущую строку с увеличенным счётчиком.
    /// </summary>
    /// <param name="text">Текст предупреждающего сообщения для записи.</param>
    public static void Warning(string text)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write(GetCorrectText(text));
        Console.ResetColor();
    }

    /// <summary>
    /// Записывает сообщение об успешном выполнении в консоль зелёным цветом.
    /// Если сообщение повторяется, перезаписывает предыдущую строку с увеличенным счётчиком.
    /// </summary>
    /// <param name="text">Текст сообщения об успехе для записи.</param>
    public static void Success(string text)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write(GetCorrectText(text));
        Console.ResetColor();
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
            return $"\r({LogCount}) {text}";
        }
        else
        {
            Console.WriteLine(" ");
            LastLog = text;
            LogCount = 0;
            return text;
        }
    }
}
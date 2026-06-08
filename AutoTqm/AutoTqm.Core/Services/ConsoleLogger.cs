using AutoTqm.Core.Interfaces;

namespace AutoTqm.Core.Services;

/// <summary>
/// 控制台日志服务 — 支持分级输出与调试开关
/// </summary>
public class ConsoleLogger : ILogger
{
    public bool IsDebugEnabled { get; set; }

    public void Debug(string message)
    {
        if (IsDebugEnabled)
            WriteLine("[DEBUG]", message, ConsoleColor.Gray);
    }

    public void Info(string message) => WriteLine("[INFO]", message, ConsoleColor.Cyan);
    public void Warning(string message) => WriteLine("[WARN]", message, ConsoleColor.Yellow);
    public void Error(string message, Exception? ex = null)
    {
        WriteLine("[ERROR]", message, ConsoleColor.Red);
        if (ex != null)
            Console.WriteLine($"  → {ex.GetType().Name}: {ex.Message}");
    }

    private static void WriteLine(string level, string message, ConsoleColor color)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine($"{level} {message}");
        Console.ForegroundColor = prev;
    }
}

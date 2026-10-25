using System;

namespace RobloxHotbarScroll.Models;

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}

public class LogEntry
{
    public DateTime Timestamp { get; } = DateTime.Now;
    public LogLevel Level { get; }
    public string Message { get; }
    public string? Details { get; }

    public LogEntry(LogLevel level, string message, string? details = null)
    {
        Level = level;
        Message = message;
        Details = details;
    }

    public string FormattedTime => Timestamp.ToString("HH:mm:ss.fff");

    public override string ToString()
    {
        return $"[{FormattedTime}] [{Level.ToString().ToUpperInvariant()}] {Message}" +
               (string.IsNullOrWhiteSpace(Details) ? "" : $" - {Details}");
    }
}

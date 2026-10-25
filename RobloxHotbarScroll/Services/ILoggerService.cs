using System;
using System.Collections.ObjectModel;
using RobloxHotbarScroll.Models;

namespace RobloxHotbarScroll.Services;

public interface ILoggerService
{
    ObservableCollection<LogEntry> Entries { get; }
    string LogFilePath { get; }
    void LogInfo(string message, string? details = null);
    void LogDebug(string message, string? details = null);
    void LogWarning(string message, string? details = null);
    void LogError(string message, Exception? ex = null);
    void LogException(string context, Exception ex);
    void Clear();
}

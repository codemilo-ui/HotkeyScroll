using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using RobloxHotbarScroll.Models;

namespace RobloxHotbarScroll.Services;

public class LoggerService : ILoggerService
{
    private const int MaxMemoryLogEntries = 300;
    private readonly object _lock = new();
    private readonly string _primaryLogPath;
    private readonly string? _fallbackLogPath;

    public ObservableCollection<LogEntry> Entries { get; } = new();
    public string LogFilePath => _primaryLogPath;

    public LoggerService()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string appDataDir = Path.Combine(appData, "RobloxHotbarScroll", "Logs");
        Directory.CreateDirectory(appDataDir);
        _primaryLogPath = Path.Combine(appDataDir, "application.log");

        try
        {
            // Also attempt to write to ./logs/application.log next to the executable if directory is writable
            string localDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Directory.CreateDirectory(localDir);
            _fallbackLogPath = Path.Combine(localDir, "application.log");
        }
        catch
        {
            _fallbackLogPath = null;
        }

        string header = $"\n================ Session Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ================\n";
        AppendRaw(header);
    }

    public void LogInfo(string message, string? details = null) =>
        AddEntry(new LogEntry(LogLevel.Info, message, details));

    public void LogDebug(string message, string? details = null) =>
        AddEntry(new LogEntry(LogLevel.Debug, message, details));

    public void LogWarning(string message, string? details = null) =>
        AddEntry(new LogEntry(LogLevel.Warning, message, details));

    public void LogError(string message, Exception? ex = null)
    {
        string? details = ex != null ? FormatExceptionDetails(ex) : null;
        AddEntry(new LogEntry(LogLevel.Error, message, details));
    }

    public void LogException(string context, Exception ex)
    {
        string details = FormatExceptionDetails(ex);
        AddEntry(new LogEntry(LogLevel.Error, $"{context}: {ex.Message}", details));
    }

    public static string FormatExceptionDetails(Exception ex)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[Exception Type]: {ex.GetType().FullName}");
        sb.AppendLine($"[Message]: {ex.Message}");
        sb.AppendLine($"[Source]: {ex.Source}");
        sb.AppendLine($"[StackTrace]:\n{ex.StackTrace}");

        Exception? inner = ex.InnerException;
        int depth = 1;
        while (inner != null)
        {
            sb.AppendLine($"--- Inner Exception ({depth++}): {inner.GetType().FullName} ---");
            sb.AppendLine($"[Message]: {inner.Message}");
            sb.AppendLine($"[Source]: {inner.Source}");
            sb.AppendLine($"[StackTrace]:\n{inner.StackTrace}");
            inner = inner.InnerException;
        }

        return sb.ToString();
    }

    public void Clear()
    {
        lock (_lock)
        {
            DispatchToUI(() => Entries.Clear());
        }
    }

    private void AddEntry(LogEntry entry)
    {
        lock (_lock)
        {
            DispatchToUI(() =>
            {
                if (Entries.Count >= MaxMemoryLogEntries)
                {
                    Entries.RemoveAt(0);
                }
                Entries.Add(entry);
            });

            string line = entry.ToString() + Environment.NewLine;
            AppendRaw(line);
        }
    }

    private void AppendRaw(string text)
    {
        try
        {
            File.AppendAllText(_primaryLogPath, text);
        }
        catch { }

        if (!string.IsNullOrEmpty(_fallbackLogPath))
        {
            try
            {
                File.AppendAllText(_fallbackLogPath, text);
            }
            catch { }
        }
    }

    private static void DispatchToUI(Action action)
    {
        var app = System.Windows.Application.Current;
        if (app?.Dispatcher != null && !app.Dispatcher.CheckAccess())
        {
            app.Dispatcher.BeginInvoke(action);
        }
        else
        {
            action();
        }
    }
}

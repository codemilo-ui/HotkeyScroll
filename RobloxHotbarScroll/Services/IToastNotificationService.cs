using System;
using System.Collections.ObjectModel;

namespace RobloxHotbarScroll.Services;

public enum ToastType
{
    Info,
    Success,
    Warning,
    Error,
    Update
}

public class ToastNotification
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Title { get; }
    public string Message { get; }
    public ToastType Type { get; }
    public DateTime CreatedAt { get; } = DateTime.Now;

    public string Icon => Type switch
    {
        ToastType.Success => "✓",
        ToastType.Warning => "⚠",
        ToastType.Error => "✕",
        ToastType.Update => "✦",
        _ => "ℹ"
    };

    public string AccentColor => Type switch
    {
        ToastType.Success => "#10B981",
        ToastType.Warning => "#F59E0B",
        ToastType.Error => "#EF4444",
        ToastType.Update => "#A855F7",
        _ => "#8B5CF6"
    };

    public string IconBgColor => Type switch
    {
        ToastType.Success => "#1F3D2F",
        ToastType.Warning => "#3D2D14",
        ToastType.Error => "#3D1818",
        ToastType.Update => "#3B1B54",
        _ => "#251D4A"
    };

    public ToastNotification(string title, string message, ToastType type = ToastType.Info)
    {
        Title = title;
        Message = message;
        Type = type;
    }
}

public interface IToastNotificationService
{
    ObservableCollection<ToastNotification> ActiveToasts { get; }
    void Show(string title, string message, ToastType type = ToastType.Info);
    void Dismiss(ToastNotification toast);
    event EventHandler<ToastNotification>? ToastShown;
}

using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;

namespace RobloxHotbarScroll.Services;

public class ToastNotificationService : IToastNotificationService
{
    private readonly ISettingsService _settingsService;
    public ObservableCollection<ToastNotification> ActiveToasts { get; } = new();
    public event EventHandler<ToastNotification>? ToastShown;

    public ToastNotificationService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public void Show(string title, string message, ToastType type = ToastType.Info)
    {
        if (!_settingsService.CurrentSettings.ShowNotifications) return;

        var toast = new ToastNotification(title, message, type);

        DispatchToUI(() =>
        {
            // Limit to max 3 concurrent toasts
            while (ActiveToasts.Count >= 3)
            {
                ActiveToasts.RemoveAt(0);
            }
            ActiveToasts.Add(toast);
        });

        ToastShown?.Invoke(this, toast);

        // Auto dismiss after 3.5 seconds
        var timer = new System.Threading.Timer(_ =>
        {
            Dismiss(toast);
        }, null, 3500, Timeout.Infinite);
    }

    public void Dismiss(ToastNotification toast)
    {
        DispatchToUI(() =>
        {
            ActiveToasts.Remove(toast);
        });
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

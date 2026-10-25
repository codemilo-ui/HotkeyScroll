using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using RobloxHotbarScroll.Models;

namespace RobloxHotbarScroll.Services;

public class TrayService : ITrayService
{
    private readonly ILoggerService _logger;
    private readonly ISettingsService _settingsService;
    private readonly IHotbarController _hotbarController;

    private NotifyIcon? _notifyIcon;
    private ContextMenuStrip? _contextMenu;
    private ToolStripMenuItem? _statusItem;
    private ToolStripMenuItem? _toggleItem;

    private bool _isDisposed;
    private bool _lastActive;
    private int _lastSlot = 1;

    public event EventHandler? OpenDashboardRequested;
    public event EventHandler? OpenSettingsRequested;
    public event EventHandler? OpenLogsRequested;
    public event EventHandler? ExitRequested;

    public TrayService(
        ILoggerService logger,
        ISettingsService settingsService,
        IHotbarController hotbarController)
    {
        _logger = logger;
        _settingsService = settingsService;
        _hotbarController = hotbarController;
    }

    public void Initialize()
    {
        if (_notifyIcon != null) return;

        _contextMenu = new ContextMenuStrip();
        _contextMenu.RenderMode = ToolStripRenderMode.System;

        var headerItem = new ToolStripMenuItem("HotkeyScroll")
        {
            Enabled = false,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold)
        };

        _statusItem = new ToolStripMenuItem("● Active")
        {
            Enabled = false
        };

        string hotkey = _settingsService.CurrentSettings.ToggleHotkey;
        _toggleItem = new ToolStripMenuItem($"Pause / Resume ({hotkey})", null, (s, e) =>
        {
            _hotbarController.ToggleEnabled();
        });

        var openItem = new ToolStripMenuItem("Open", null, (s, e) =>
        {
            OpenDashboardRequested?.Invoke(this, EventArgs.Empty);
        })
        {
            Font = new Font("Segoe UI", 9f, FontStyle.Bold)
        };

        var settingsItem = new ToolStripMenuItem("Settings", null, (s, e) =>
        {
            OpenSettingsRequested?.Invoke(this, EventArgs.Empty);
        });

        var logsItem = new ToolStripMenuItem("View Logs", null, (s, e) =>
        {
            OpenLogsRequested?.Invoke(this, EventArgs.Empty);
        });

        var exitItem = new ToolStripMenuItem("Exit", null, (s, e) =>
        {
            ExitRequested?.Invoke(this, EventArgs.Empty);
        });

        _contextMenu.Items.Add(headerItem);
        _contextMenu.Items.Add(_statusItem);
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add(_toggleItem);
        _contextMenu.Items.Add(openItem);
        _contextMenu.Items.Add(settingsItem);
        _contextMenu.Items.Add(logsItem);
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add(exitItem);

        _notifyIcon = new NotifyIcon
        {
            Text = "HotkeyScroll - Roblox Hotbar Remapper",
            ContextMenuStrip = _contextMenu,
            Visible = true,
            Icon = CreateTrayIcon(true, 1)
        };

        _notifyIcon.DoubleClick += (s, e) =>
        {
            OpenDashboardRequested?.Invoke(this, EventArgs.Empty);
        };

        _logger.LogInfo("System tray service initialized.");
    }

    public void UpdateState(bool isEnabled, bool isActive, int currentSlot, string robloxStatus)
    {
        _lastActive = isActive;
        _lastSlot = currentSlot;

        if (_notifyIcon == null) return;

        try
        {
            string hotkey = _settingsService.CurrentSettings.ToggleHotkey;

            if (_statusItem != null)
            {
                if (!isEnabled)
                {
                    _statusItem.Text = "● Paused";
                }
                else if (isActive)
                {
                    _statusItem.Text = $"● Active (Slot {currentSlot})";
                }
                else
                {
                    _statusItem.Text = "○ Waiting for Roblox";
                }
            }

            if (_toggleItem != null)
            {
                _toggleItem.Text = isEnabled ? $"Pause ({hotkey})" : $"Resume ({hotkey})";
            }

            string tooltip = $"HotkeyScroll (v1.1.0)\nStatus: {(isEnabled ? "Active" : "Paused")}\nSlot: {currentSlot}\nHotkey: {hotkey}";
            if (!string.IsNullOrEmpty(robloxStatus))
            {
                tooltip += $"\n{robloxStatus}";
            }

            if (tooltip.Length >= 64)
            {
                tooltip = tooltip.Substring(0, 60) + "...";
            }
            _notifyIcon.Text = tooltip;

            var oldIcon = _notifyIcon.Icon;
            _notifyIcon.Icon = CreateTrayIcon(isEnabled && isActive, currentSlot);
            oldIcon?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError("Error updating tray state.", ex);
        }
    }

    public void ShowNotification(string title, string message)
    {
        if (!_settingsService.CurrentSettings.ShowNotifications || _notifyIcon == null) return;

        try
        {
            _notifyIcon.ShowBalloonTip(2500, title, message, ToolTipIcon.Info);
        }
        catch { }
    }

    private static Icon CreateTrayIcon(bool active, int slot)
    {
        using var bitmap = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            // Dark violet background
            using (var brush = new SolidBrush(Color.FromArgb(18, 14, 34)))
            {
                g.FillEllipse(brush, 1, 1, 30, 30);
            }

            // Purple border
            Color borderColor = active ? Color.FromArgb(139, 92, 246) : Color.FromArgb(90, 80, 120);
            using (var pen = new Pen(borderColor, 2f))
            {
                g.DrawEllipse(pen, 1, 1, 30, 30);
            }

            // Draw Slot Number
            string label = slot == 10 ? "0" : slot.ToString();
            using (var font = new Font("Segoe UI", 12f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var textBrush = new SolidBrush(Color.White))
            {
                var sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(label, font, textBrush, new RectangleF(1, 1, 30, 30), sf);
            }

            // Status Indicator dot in corner
            Color dotColor = active ? Color.FromArgb(16, 185, 129) : Color.FromArgb(245, 158, 11);
            using (var dotBrush = new SolidBrush(dotColor))
            {
                g.FillEllipse(dotBrush, 21, 21, 8, 8);
            }
        }

        IntPtr hIcon = bitmap.GetHicon();
        Icon icon = (Icon)Icon.FromHandle(hIcon).Clone();
        DestroyIcon(hIcon);
        return icon;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Icon?.Dispose();
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        _contextMenu?.Dispose();
        _contextMenu = null;
    }
}

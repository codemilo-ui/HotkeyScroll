using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using RobloxHotbarScroll.Services;
using RobloxHotbarScroll.ViewModels;
using RobloxHotbarScroll.Views;

namespace RobloxHotbarScroll;

public partial class App : System.Windows.Application
{
    private const string AppMutexName = @"Global\CodeMilo_HotkeyScroll_SingleInstance_Mutex";
    private const string AppShowEventName = @"Global\CodeMilo_HotkeyScroll_ShowInstance_Event";

    private Mutex? _singleInstanceMutex;
    private EventWaitHandle? _showInstanceEvent;
    private volatile bool _isRunning = true;

    private ILoggerService? _logger;
    private ISettingsService? _settingsService;
    private IRobloxDetector? _robloxDetector;
    private IInputSimulator? _inputSimulator;
    private IToastNotificationService? _toastService;
    private IHotbarController? _hotbarController;
    private IInputManager? _inputManager;
    private ITrayService? _trayService;
    private IUpdateService? _updateService;

    private MainViewModel? _mainViewModel;
    private MainWindow? _mainWindow;

    public App()
    {
        // 1. Register global unhandled exception hooks immediately
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 2. Single-instance check with IPC signaling
        try
        {
            _singleInstanceMutex = new Mutex(true, AppMutexName, out bool isNewInstance);
            if (!isNewInstance)
            {
                // Signal the primary instance to show and activate its MainWindow
                try
                {
                    using var existingEvent = EventWaitHandle.OpenExisting(AppShowEventName);
                    existingEvent.Set();
                }
                catch { }

                Shutdown(0);
                return;
            }

            // Create signaling event handle for other instances to notify us
            _showInstanceEvent = new EventWaitHandle(false, EventResetMode.AutoReset, AppShowEventName);
            StartInstanceSignalListener();
        }
        catch
        {
            // If mutex cannot be created (e.g. permission restriction), proceed with startup
        }

        try
        {
            // 3. Create logging service and output startup diagnostics
            _logger = new LoggerService();
            _logger.LogInfo("===== HotkeyScroll Starting =====");
            _logger.LogInfo($"Version: {UpdateConfig.CurrentVersionString} (CodeMilo)");
            _logger.LogInfo($"OS: {RuntimeInformation.OSDescription} ({RuntimeInformation.ProcessArchitecture})");

            // 4. Load configuration safely
            _settingsService = new SettingsService(_logger);
            _logger.LogInfo("Configuration loaded");

            // 5. Create application core services
            _toastService = new ToastNotificationService(_settingsService);
            _updateService = new UpdateService(_logger, _settingsService, _toastService);
            _robloxDetector = new RobloxDetector(_logger, _settingsService);
            _inputSimulator = new WindowsInputSimulator(_logger);
            _hotbarController = new HotbarController(_logger, _settingsService, _robloxDetector, _inputSimulator, _toastService);
            _inputManager = new WindowsInputManager(_logger, _settingsService, _hotbarController);
            _trayService = new TrayService(_logger, _settingsService, _hotbarController);

            // 6. Create ViewModel & MainWindow
            _mainViewModel = new MainViewModel(_logger, _settingsService, _robloxDetector, _hotbarController, _trayService, _inputManager, _toastService, _updateService);
            _mainWindow = new MainWindow(_mainViewModel, _settingsService);
            _logger.LogInfo("Main window created");

            // Wire Tray Actions
            _trayService.OpenDashboardRequested += (s, args) =>
            {
                Dispatcher.BeginInvoke(() => _mainWindow?.ShowMainWindow());
            };

            _trayService.OpenSettingsRequested += (s, args) =>
            {
                Dispatcher.BeginInvoke(() =>
                {
                    _mainWindow?.ShowMainWindow();
                    _mainWindow?.SwitchToTab("Settings");
                });
            };

            _trayService.OpenLogsRequested += (s, args) =>
            {
                Dispatcher.BeginInvoke(() =>
                {
                    _mainWindow?.ShowMainWindow();
                    _mainWindow?.SwitchToTab("Logs");
                });
            };

            _trayService.ExitRequested += (s, args) =>
            {
                Dispatcher.BeginInvoke(ExitApplication);
            };

            // 7. Show MainWindow
            // Only start minimized if explicitly requested via command line switch --minimized / --tray
            bool startMinimizedArg = e.Args.Any(a => a.Equals("--minimized", StringComparison.OrdinalIgnoreCase) ||
                                                    a.Equals("--tray", StringComparison.OrdinalIgnoreCase));

            if (startMinimizedArg)
            {
                _logger.LogInfo("Started minimized to system tray via command line flag.");
            }
            else
            {
                _mainWindow.ShowMainWindow();
                _logger.LogInfo("Main window displayed");
            }

            // 8. Safely initialize background services (Fault-Tolerant)
            InitializeBackgroundServices();

            _logger.LogInfo("Application startup completed");
        }
        catch (Exception ex)
        {
            HandleFatalStartupException(ex);
        }
    }

    private void InitializeBackgroundServices()
    {
        // A failure in a non-critical background service must NOT cause the entire UI to crash.
        try
        {
            _trayService?.Initialize();
            _logger?.LogInfo("Initializing tray");
        }
        catch (Exception ex)
        {
            _logger?.LogException("Failed to initialize system tray", ex);
        }

        try
        {
            _robloxDetector?.Start();
            _logger?.LogInfo("Initializing Roblox detector");
        }
        catch (Exception ex)
        {
            _logger?.LogException("Failed to start Roblox detector", ex);
        }

        try
        {
            _inputManager?.Start();
            _logger?.LogInfo("Initializing input manager");
        }
        catch (Exception ex)
        {
            _logger?.LogException("Failed to start input manager", ex);
        }
    }

    private void StartInstanceSignalListener()
    {
        var thread = new Thread(() =>
        {
            while (_isRunning && _showInstanceEvent != null)
            {
                try
                {
                    if (_showInstanceEvent.WaitOne(1000))
                    {
                        Dispatcher.BeginInvoke(() =>
                        {
                            _mainWindow?.ShowMainWindow();
                        });
                    }
                }
                catch { }
            }
        })
        {
            Name = "RobloxHotbarScroll.InstanceListener",
            IsBackground = true
        };
        thread.Start();
    }

    private void HandleFatalStartupException(Exception ex)
    {
        _logger?.LogException("Fatal application startup exception", ex);

        string logPath = _logger?.LogFilePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RobloxHotbarScroll", "Logs", "application.log");

        string message = $"Roblox Hotbar Scroll encountered an error while starting.\n\nThe error has been saved to:\n\n{logPath}\n\nDetails: {ex.Message}";

        try
        {
            System.Windows.MessageBox.Show(message, "Roblox Hotbar Scroll - Startup Error",
                                           MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch { }

        Shutdown(1);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logger?.LogException("DispatcherUnhandledException caught", e.Exception);
        e.Handled = true; // Prevent app termination if possible
    }

    private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            _logger?.LogException("AppDomain.CurrentDomain.UnhandledException caught", ex);
        }
        else
        {
            _logger?.LogError($"AppDomain unhandled non-exception object: {e.ExceptionObject}");
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _logger?.LogException("TaskScheduler.UnobservedTaskException caught", e.Exception);
        e.SetObserved();
    }

    private void ExitApplication()
    {
        _isRunning = false;
        _logger?.LogInfo("Application exit requested.");

        try
        {
            _inputManager?.Stop();
            _inputManager?.Dispose();

            (_inputSimulator as IDisposable)?.Dispose();

            _robloxDetector?.Stop();
            _robloxDetector?.Dispose();

            _trayService?.Dispose();

            _mainWindow?.ExplicitExit();
        }
        catch (Exception ex)
        {
            _logger?.LogException("Exception during application shutdown cleanup", ex);
        }
        finally
        {
            try
            {
                _showInstanceEvent?.Dispose();
                _showInstanceEvent = null;
            }
            catch { }

            try
            {
                _singleInstanceMutex?.ReleaseMutex();
                _singleInstanceMutex?.Dispose();
                _singleInstanceMutex = null;
            }
            catch { }

            Shutdown(0);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _isRunning = false;
        base.OnExit(e);
    }
}

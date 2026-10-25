using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media;
using RobloxHotbarScroll.Services;
using RobloxHotbarScroll.ViewModels;

namespace RobloxHotbarScroll.Views;

public partial class MainWindow : Window
{
    private readonly ISettingsService _settingsService;
    private bool _isExplicitExit;

    public MainWindow(MainViewModel viewModel, ISettingsService settingsService)
    {
        InitializeComponent();
        DataContext = viewModel;
        _settingsService = settingsService;

        Loaded += OnWindowLoaded;

        // Wire navigation commands from ViewModel
        viewModel.OpenSettingsCommand = new RelayCommand(() => SwitchToTab("Settings"));
        viewModel.OpenAboutCommand = new RelayCommand(() => SwitchToTab("About"));
        viewModel.AboutVM.ViewLogsCommand = new RelayCommand(() => SwitchToTab("Logs"));

        // Subscribe to ViewModel property changes for smooth animations
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (_settingsService.CurrentSettings.ReducedMotion)
        {
            RootBorder.Opacity = 1.0;
            RootScale.ScaleX = 1.0;
            RootScale.ScaleY = 1.0;
            RootTranslate.Y = 0;
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.CurrentSlot))
        {
            AnimateSlotNumberChange();
        }
        else if (e.PropertyName == nameof(MainViewModel.IsUtilityActive) ||
                 e.PropertyName == nameof(MainViewModel.StatusTitle))
        {
            AnimateStatusChange();
        }
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            try
            {
                DragMove();
            }
            catch { }
        }
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MinimizeToTray_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        if (_settingsService.CurrentSettings.MinimizeToTray && !_isExplicitExit)
        {
            Hide();
        }
        else
        {
            ExplicitExit();
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_settingsService.CurrentSettings.MinimizeToTray && !_isExplicitExit)
        {
            e.Cancel = true;
            Hide();
        }
        else if (!_isExplicitExit && !_settingsService.CurrentSettings.ReducedMotion)
        {
            e.Cancel = true;
            ExplicitExit();
        }
        else
        {
            base.OnClosing(e);
        }
    }

    public void ExplicitExit()
    {
        if (_isExplicitExit) return;
        _isExplicitExit = true;

        if (_settingsService.CurrentSettings.ReducedMotion || RootBorder == null)
        {
            Close();
            System.Windows.Application.Current.Shutdown(0);
            return;
        }

        var fadeOut = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(120))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        fadeOut.Completed += (s, e) =>
        {
            Close();
            System.Windows.Application.Current.Shutdown(0);
        };
        RootBorder.BeginAnimation(UIElement.OpacityProperty, fadeOut);
    }

    public void ShowMainWindow()
    {
        if (!IsVisible)
        {
            Show();
        }

        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();

        try
        {
            Topmost = true;
            Topmost = false;
        }
        catch { }

        Focus();
    }

    public void SwitchToTab(string tabName)
    {
        switch (tabName)
        {
            case "Dashboard":
                TabDashboard.IsChecked = true;
                break;
            case "Settings":
                TabSettings.IsChecked = true;
                break;
            case "Logs":
                TabLogs.IsChecked = true;
                break;
            case "About":
                TabAbout.IsChecked = true;
                break;
        }
    }

    private void NavTab_Checked(object sender, RoutedEventArgs e)
    {
        if (ViewDashboard == null || ViewSettings == null || ViewLogs == null || ViewAbout == null)
            return;

        FrameworkElement? target = null;
        if (TabDashboard.IsChecked == true) target = ViewDashboard;
        else if (TabSettings.IsChecked == true) target = ViewSettings;
        else if (TabLogs.IsChecked == true) target = ViewLogs;
        else if (TabAbout.IsChecked == true) target = ViewAbout;

        ViewDashboard.Visibility = (target == ViewDashboard) ? Visibility.Visible : Visibility.Collapsed;
        ViewSettings.Visibility = (target == ViewSettings) ? Visibility.Visible : Visibility.Collapsed;
        ViewLogs.Visibility = (target == ViewLogs) ? Visibility.Visible : Visibility.Collapsed;
        ViewAbout.Visibility = (target == ViewAbout) ? Visibility.Visible : Visibility.Collapsed;

        if (target != null)
        {
            AnimatePageTransition(target);
        }
    }

    private void AnimatePageTransition(FrameworkElement view)
    {
        if (view == null) return;

        if (_settingsService.CurrentSettings.ReducedMotion)
        {
            view.Opacity = 1.0;
            if (view.RenderTransform is TranslateTransform tt)
            {
                tt.Y = 0;
            }
            return;
        }

        view.Opacity = 0.0;
        if (view.RenderTransform is TranslateTransform trans)
        {
            trans.Y = 12.0;

            var slideAnim = new DoubleAnimation(12.0, 0.0, TimeSpan.FromMilliseconds(200))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            trans.BeginAnimation(TranslateTransform.YProperty, slideAnim);
        }

        var fadeAnim = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        view.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
    }

    private void AnimateSlotNumberChange()
    {
        if (_settingsService.CurrentSettings.ReducedMotion || SlotTranslate == null)
            return;

        var bounceAnim = new DoubleAnimation(-8.0, 0.0, TimeSpan.FromMilliseconds(150))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        SlotTranslate.BeginAnimation(TranslateTransform.YProperty, bounceAnim);
    }

    private void AnimateStatusChange()
    {
        if (_settingsService.CurrentSettings.ReducedMotion || StatusScale == null)
            return;

        var scaleXAnim = new DoubleAnimationUsingKeyFrames();
        scaleXAnim.KeyFrames.Add(new SplineDoubleKeyFrame(1.08, TimeSpan.FromMilliseconds(80)));
        scaleXAnim.KeyFrames.Add(new SplineDoubleKeyFrame(1.0, TimeSpan.FromMilliseconds(200)));

        var scaleYAnim = new DoubleAnimationUsingKeyFrames();
        scaleYAnim.KeyFrames.Add(new SplineDoubleKeyFrame(1.08, TimeSpan.FromMilliseconds(80)));
        scaleYAnim.KeyFrames.Add(new SplineDoubleKeyFrame(1.0, TimeSpan.FromMilliseconds(200)));

        StatusScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnim);
        StatusScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnim);
    }
}

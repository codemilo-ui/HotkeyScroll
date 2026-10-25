using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using RobloxHotbarScroll.ViewModels;

namespace RobloxHotbarScroll.Views.Components;

public partial class HotbarDisplay : System.Windows.Controls.UserControl
{
    private const double SlotPitch = 52.0; // 46px width + 6px margins
    private const double BaseOffset = 3.0;
    private const double CanvasBaseOffset = 11.0; // Aligned with slot center on 540 canvas

    public HotbarDisplay()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateIndicatorPosition(false);
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is INotifyPropertyChanged oldVm)
        {
            oldVm.PropertyChanged -= OnViewModelPropertyChanged;
        }

        if (e.NewValue is INotifyPropertyChanged newVm)
        {
            newVm.PropertyChanged += OnViewModelPropertyChanged;
            UpdateIndicatorPosition(false);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.CurrentSlot))
        {
            UpdateIndicatorPosition(true);
        }
    }

    private void UpdateIndicatorPosition(bool animate)
    {
        if (DataContext is not MainViewModel vm) return;

        int slot = vm.CurrentSlot;
        if (slot < 1 || slot > 10) slot = 1;

        double targetX = (slot - 1) * SlotPitch + BaseOffset;
        double pointerTargetX = (slot - 1) * SlotPitch + CanvasBaseOffset;

        if (animate && vm.AnimationsEnabled)
        {
            double durationMs = Math.Max(50, 180 * vm.AnimationSpeed);
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

            var anim = new DoubleAnimation
            {
                To = targetX,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                EasingFunction = ease
            };
            IndicatorTransform.BeginAnimation(TranslateTransform.XProperty, anim);

            var pointerAnim = new DoubleAnimation
            {
                To = pointerTargetX,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                EasingFunction = ease
            };
            PointerTransform.BeginAnimation(TranslateTransform.XProperty, pointerAnim);
        }
        else
        {
            IndicatorTransform.BeginAnimation(TranslateTransform.XProperty, null);
            IndicatorTransform.X = targetX;

            PointerTransform.BeginAnimation(TranslateTransform.XProperty, null);
            PointerTransform.X = pointerTargetX;
        }
    }
}

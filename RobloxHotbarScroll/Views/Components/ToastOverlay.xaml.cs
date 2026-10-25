using System.Windows;
using System.Windows.Controls;
using RobloxHotbarScroll.Services;
using RobloxHotbarScroll.ViewModels;

namespace RobloxHotbarScroll.Views.Components;

public partial class ToastOverlay : System.Windows.Controls.UserControl
{
    public ToastOverlay()
    {
        InitializeComponent();
    }

    private void Dismiss_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.DataContext is ToastNotification toast)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.DismissToast(toast);
            }
        }
    }
}

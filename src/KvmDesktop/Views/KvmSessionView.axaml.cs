using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using KvmDesktop.ViewModels;

namespace KvmDesktop.Views;

public partial class KvmSessionView : UserControl
{
    public KvmSessionView()
    {
        InitializeComponent();
        
        this.AttachedToVisualTree += (s, e) => {
            if (DataContext is KvmSessionViewModel vm)
            {
                vm._inputCapturer.Attach(this);
                this.Focus();
            }
        };

        this.DetachedFromVisualTree += (s, e) => {
            if (DataContext is KvmSessionViewModel vm)
            {
                vm._inputCapturer.Detach();
            }
        };
        
        // Add global hotkey for toggling capture
        this.KeyDown += (s, e) => {
            if (e.Key == Key.F11)
            {
                if (DataContext is KvmSessionViewModel vm)
                {
                    vm.ToggleCapture();
                }
            }
            else if (e.Key == Key.P && e.KeyModifiers.HasFlag(KeyModifiers.Alt))
            {
                // Alt + P release capture
                if (DataContext is KvmSessionViewModel vm)
                {
                    vm._inputCapturer.IsEnabled = false;
                    vm.Overlay.IsMouseCaptured = false;
                }
            }
        };
    }
}

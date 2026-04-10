using CommunityToolkit.Mvvm.ComponentModel;

namespace KvmDesktop.ViewModels;

public partial class KvmOverlayViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isMouseCaptured;

    [ObservableProperty]
    private string _videoResolution = "Unknown";

    [ObservableProperty]
    private bool _isVideoConnected;

    [ObservableProperty]
    private bool _isHidConnected;

    [ObservableProperty]
    private string _statusMessage = "Connecting...";
}

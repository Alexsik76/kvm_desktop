using System;
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

    [ObservableProperty]
    private int _fps;

    [ObservableProperty]
    private double _frameIntervalMs;

    [ObservableProperty]
    private bool _isDebugEnabled;

    public KvmOverlayViewModel()
    {
        IsDebugEnabled = Environment.GetEnvironmentVariable("KVM_DEBUG") == "1"
            || Array.Exists(Environment.GetCommandLineArgs(), a => a == "--debug");
    }
}

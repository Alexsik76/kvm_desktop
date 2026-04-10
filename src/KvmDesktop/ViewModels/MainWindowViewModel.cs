using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using KvmDesktop.Services;

namespace KvmDesktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private ViewModelBase _currentContent;

    public MainWindowViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        
        // Start with Login screen
        _currentContent = CreateLoginViewModel();
    }

    private LoginViewModel CreateLoginViewModel()
    {
        var vm = _serviceProvider.GetRequiredService<LoginViewModel>();
        vm.LoginSuccess += () => CurrentContent = CreateDashboardViewModel();
        return vm;
    }

    private DashboardViewModel CreateDashboardViewModel()
    {
        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        vm.LoggedOut += () => CurrentContent = CreateLoginViewModel();
        vm.NodeLaunched += (node) => CurrentContent = CreateKvmSessionViewModel(node);
        
        // Initial load of nodes
        _ = vm.LoadNodesCommand.ExecuteAsync(null);
        
        return vm;
    }

    private KvmSessionViewModel CreateKvmSessionViewModel(Models.KvmNode node)
    {
        var userSession = _serviceProvider.GetRequiredService<IUserSession>();
        var hidClient = _serviceProvider.GetRequiredService<IHidClient>();
        var inputCapturer = _serviceProvider.GetRequiredService<IInputCapturer>();

        // Determine the node domain for direct WebSocket connection
        string nodeDomain = "";
        if (!string.IsNullOrEmpty(node.TunnelUrl))
        {
            if (Uri.TryCreate(node.TunnelUrl, UriKind.Absolute, out var tunnelUri))
            {
                nodeDomain = tunnelUri.Host;
            }
            else
            {
                nodeDomain = node.TunnelUrl.Replace("https://", "").Replace("http://", "").TrimEnd('/');
            }
        }
        else if (!string.IsNullOrEmpty(node.InternalIp))
        {
            nodeDomain = node.InternalIp;
        }

        if (string.IsNullOrEmpty(nodeDomain))
        {
            // Fallback if neither TunnelUrl nor InternalIp is available
            Console.WriteLine("[MainWindowVM] Warning: Both TunnelUrl and InternalIp are empty. HID connection might fail.");
        }

        // HID URL according to README_API.md: wss://<node_domain>/ws/control
        string hidUrl = $"wss://{nodeDomain}/ws/control";

        // Stream URL for WebRTC (API backend)
        string streamUrl = string.IsNullOrEmpty(node.StreamUrl) 
            ? $"https://kvm-api.lab.vn.ua/api/v1/nodes/{node.Id}/signal/offer"
            : node.StreamUrl;

        var vm = new KvmSessionViewModel(
            hidClient,
            inputCapturer,
            streamUrl, // Renamed from signalingUrl
            hidUrl,
            userSession.CurrentUser?.AccessToken ?? "");

        _ = vm.StartSessionCommand.ExecuteAsync(null);
        return vm;
    }
}

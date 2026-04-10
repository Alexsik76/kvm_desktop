using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KvmDesktop.Models;
using KvmDesktop.Services;

namespace KvmDesktop.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly INodeService _nodeService;
    private readonly IKvmLauncherService _launcherService;
    private readonly IUserSession _userSession;
    private readonly IAuthService _authService;
    private readonly IPipeServerService _pipeServerService;

    [ObservableProperty]
    private ObservableCollection<KvmNode> _nodes = new();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private KvmNode? _selectedNode;

    [ObservableProperty]
    private string? _clientStatus;

    public DashboardViewModel(
        INodeService nodeService, 
        IKvmLauncherService launcherService, 
        IUserSession userSession,
        IAuthService authService,
        IPipeServerService pipeServerService)
    {
        _nodeService = nodeService;
        _launcherService = launcherService;
        _userSession = userSession;
        _authService = authService;
        _pipeServerService = pipeServerService;

        _pipeServerService.MessageReceived += OnPipeMessageReceived;
    }

    private void OnPipeMessageReceived(object? sender, PipeMessage e)
    {
        if (e.Type == PipeMessageTypes.StatusUpdate)
        {
            ClientStatus = e.Payload?.ToString();
        }
        else if (e.Type == PipeMessageTypes.Error)
        {
            ClientStatus = $"Error: {e.Payload}";
        }
    }

    public string WelcomeMessage => $"Welcome, {_userSession.CurrentUser?.Username ?? "User"}!";

    [RelayCommand]
    private async Task LoadNodesAsync()
    {
        IsBusy = true;
        try
        {
            var nodes = await _nodeService.GetNodesAsync();
            Nodes.Clear();
            foreach (var node in nodes)
            {
                Nodes.Add(node);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task LaunchNodeAsync(KvmNode? node)
    {
        var targetNode = node ?? SelectedNode;
        if (targetNode == null) return;

        if (_userSession.CurrentUser == null) return;

        OnNodeLaunched(targetNode);
        await Task.CompletedTask;
    }

    [RelayCommand]
    private void Logout()
    {
        _authService.Logout();
        OnLoggedOut();
    }

    public event Action? LoggedOut;
    private void OnLoggedOut() => LoggedOut?.Invoke();

    public event Action<KvmNode>? NodeLaunched;
    private void OnNodeLaunched(KvmNode node) => NodeLaunched?.Invoke(node);
}

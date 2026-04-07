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

    [ObservableProperty]
    private ObservableCollection<KvmNode> _nodes = new();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private KvmNode? _selectedNode;

    public DashboardViewModel(
        INodeService nodeService, 
        IKvmLauncherService launcherService, 
        IUserSession userSession,
        IAuthService authService)
    {
        _nodeService = nodeService;
        _launcherService = launcherService;
        _userSession = userSession;
        _authService = authService;
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
        if (targetNode == null || _userSession.CurrentUser == null) return;

        try
        {
            await _launcherService.LaunchNodeAsync(targetNode, _userSession.CurrentUser.AccessToken);
        }
        catch (Exception)
        {
            // Error handling for launcher failure
        }
    }

    [RelayCommand]
    private void Logout()
    {
        _authService.Logout();
        OnLoggedOut();
    }

    public event Action? LoggedOut;
    private void OnLoggedOut() => LoggedOut?.Invoke();
}

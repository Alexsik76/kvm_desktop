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
        Console.WriteLine("[DashboardVM] LaunchNodeAsync called.");
        var targetNode = node ?? SelectedNode;
        if (targetNode == null)
        {
            Console.WriteLine("[DashboardVM] No node selected.");
            return;
        }

        if (_userSession.CurrentUser == null)
        {
            Console.WriteLine("[DashboardVM] No user session found.");
            return;
        }

        try
        {
            string pipeName = $"kvm_pipe_{Guid.NewGuid():N}";
            Console.WriteLine($"[DashboardVM] Preparing launch for node: {targetNode.Name} with pipe: {pipeName}");
            
            // Start the pipe server
            var startTask = _pipeServerService.StartAsync(pipeName);

            // Launch the external process
            Console.WriteLine("[DashboardVM] Launching native client...");
            await _launcherService.LaunchNodeAsync(targetNode, pipeName);

            ClientStatus = "Waiting for client connection...";
            Console.WriteLine("[DashboardVM] Waiting for client to connect to pipe...");

            // Wait for the client to connect (StartAsync returns when client connects)
            await startTask;
            Console.WriteLine("[DashboardVM] Client connected to pipe server.");

            if (_pipeServerService.IsConnected)
            {
                ClientStatus = "Connected. Sending handshake...";
                // ... (решта коду)
                Console.WriteLine("[DashboardVM] Building and sending handshake...");

                string nodeDomain = "";
                if (!string.IsNullOrEmpty(targetNode.TunnelUrl))
                {
                    if (Uri.TryCreate(targetNode.TunnelUrl, UriKind.Absolute, out Uri tunnelUri))
                    {
                        nodeDomain = tunnelUri.Host;
                    }
                    else
                    {
                        nodeDomain = targetNode.TunnelUrl.Replace("https://", "").Replace("http://", "");
                    }
                }
                else if (!string.IsNullOrEmpty(targetNode.InternalIp))
                {
                    nodeDomain = targetNode.InternalIp;
                }

                string hidUrl = $"wss://{nodeDomain}/ws/control";

                string streamUrl = string.IsNullOrEmpty(targetNode.StreamUrl) 
                    ? $"https://kvm-api.lab.vn.ua/api/v1/nodes/{targetNode.Id}/signal/offer" 
                    : targetNode.StreamUrl;

                // Send the sensitive data via the secure pipe
                var handshake = new HandshakeData
                {
                    AccessToken = _userSession.CurrentUser.AccessToken,
                    StreamUrl = streamUrl,
                    HidUrl = hidUrl
                };

                await _pipeServerService.SendAsync(new PipeMessage
                {
                    Type = PipeMessageTypes.Handshake,
                    Payload = handshake
                });

                ClientStatus = "Handshake sent.";
            }
        }
        catch (Exception ex)
        {
            ClientStatus = $"Launch failed: {ex.Message}";
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

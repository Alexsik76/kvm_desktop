using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

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
        
        // Initial load of nodes
        _ = vm.LoadNodesCommand.ExecuteAsync(null);
        
        return vm;
    }
}

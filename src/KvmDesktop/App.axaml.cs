using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using System;
using System.Linq;
using Avalonia.Markup.Xaml;
using KvmDesktop.ViewModels;
using KvmDesktop.Views;
using KvmDesktop.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KvmDesktop;

public partial class App : Application
{
    public IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Session and Preferences State
        services.AddSingleton<IUserSession, UserSession>();
        services.AddSingleton<IPreferencesService, PreferencesService>();

        // HTTP Client
        services.AddHttpClient<IAuthService, AuthService>(client =>
        {
            client.BaseAddress = new Uri("https://kvm-api.lab.vn.ua/api/v1/");
        });

        services.AddHttpClient<INodeService, NodeService>(client =>
        {
            client.BaseAddress = new Uri("https://kvm-api.lab.vn.ua/api/v1/");
        });

        // Services
        services.AddSingleton<IKvmLauncherService, KvmLauncherService>();
        services.AddSingleton<IPipeServerService, PipeServerService>();

        // ViewModels
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<DashboardViewModel>();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
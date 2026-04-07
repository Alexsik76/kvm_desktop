using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KvmDesktop.Services;

namespace KvmDesktop.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private readonly IPreferencesService _preferencesService;
    
    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _rememberMe;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    public LoginViewModel(IAuthService authService, IPreferencesService preferencesService)
    {
        _authService = authService;
        _preferencesService = preferencesService;

        LoadPreferences();
    }

    private void LoadPreferences()
    {
        var prefs = _preferencesService.GetPreferences();
        if (prefs.RememberMe)
        {
            Username = prefs.Username;
            Password = prefs.EncryptedPassword;
            RememberMe = true;
        }
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Username and password are required.";
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            bool success = await _authService.LoginAsync(Username, Password);
            if (success)
            {
                SaveOrClearPreferences();
                OnLoginSuccess();
            }
            else
            {
                ErrorMessage = "Invalid username or password.";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SaveOrClearPreferences()
    {
        if (RememberMe)
        {
            _preferencesService.SavePreferences(new Models.UserPreferences
            {
                Username = Username,
                EncryptedPassword = Password,
                RememberMe = true
            });
        }
        else
        {
            _preferencesService.ClearPreferences();
        }
    }

    // Event or callback for successful login
    public event System.Action? LoginSuccess;

    private void OnLoginSuccess() => LoginSuccess?.Invoke();
}

using KvmDesktop.Models;

namespace KvmDesktop.Services;

public interface IPreferencesService
{
    UserPreferences GetPreferences();
    void SavePreferences(UserPreferences preferences);
    void ClearPreferences();
}

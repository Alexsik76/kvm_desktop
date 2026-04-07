using System;
using System.IO;
using System.Text;
using System.Text.Json;
using KvmDesktop.Models;

namespace KvmDesktop.Services;

public class PreferencesService : IPreferencesService
{
    private readonly string _filePath;

    public PreferencesService()
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string folder = Path.Combine(appDataPath, "KvmDesktop");
        Directory.CreateDirectory(folder);
        _filePath = Path.Combine(folder, "preferences.json");
    }

    public UserPreferences GetPreferences()
    {
        if (!File.Exists(_filePath))
            return new UserPreferences();

        try
        {
            string json = File.ReadAllText(_filePath);
            var prefs = JsonSerializer.Deserialize<UserPreferences>(json);
            if (prefs != null && !string.IsNullOrEmpty(prefs.EncryptedPassword))
            {
                // De-obfuscate from Base64
                prefs.EncryptedPassword = Encoding.UTF8.GetString(Convert.FromBase64String(prefs.EncryptedPassword));
            }
            return prefs ?? new UserPreferences();
        }
        catch (Exception)
        {
            return new UserPreferences();
        }
    }

    public void SavePreferences(UserPreferences preferences)
    {
        try
        {
            // Create a copy to avoid modifying the original model used in UI
            var copy = new UserPreferences
            {
                Username = preferences.Username,
                RememberMe = preferences.RememberMe,
                EncryptedPassword = Convert.ToBase64String(Encoding.UTF8.GetBytes(preferences.EncryptedPassword))
            };

            string json = JsonSerializer.Serialize(copy);
            File.WriteAllText(_filePath, json);
        }
        catch (Exception)
        {
        }
    }

    public void ClearPreferences()
    {
        if (File.Exists(_filePath))
        {
            try { File.Delete(_filePath); } catch { }
        }
    }
}

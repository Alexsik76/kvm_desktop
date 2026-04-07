namespace KvmDesktop.Models;

public class UserPreferences
{
    public string Username { get; set; } = string.Empty;
    public string EncryptedPassword { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
}

namespace KvmDesktop.Models;

/// <summary>
/// Represents a user in the system.
/// </summary>
public class User
{
    public string Username { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

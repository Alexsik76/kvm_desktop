namespace KvmDesktop.Models;

/// <summary>
/// Data sent from Launcher to Client during the initial connection.
/// Using PascalCase for IPC to match C++ client expectations.
/// </summary>
public class HandshakeData
{
    public string AccessToken { get; set; } = string.Empty;
    public string StreamUrl { get; set; } = string.Empty;
    public string HidUrl { get; set; } = string.Empty;
}

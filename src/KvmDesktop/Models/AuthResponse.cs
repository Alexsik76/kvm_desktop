using System.Text.Json.Serialization;

namespace KvmDesktop.Models;

/// <summary>
/// Represents the response from the authentication API.
/// </summary>
public class AuthResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;
}

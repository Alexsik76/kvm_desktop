using System.Text.Json.Serialization;

namespace KvmDesktop.Models;

/// <summary>
/// Represents a KVM node (Raspberry Pi device).
/// </summary>
public class KvmNode
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("tunnel_url")]
    public string TunnelUrl { get; set; } = string.Empty;

    [JsonPropertyName("internal_ip")]
    public string InternalIp { get; set; } = string.Empty;

    [JsonPropertyName("stream_url")]
    public string StreamUrl { get; set; } = string.Empty;

    [JsonPropertyName("hid_url")]
    public string HidUrl { get; set; } = string.Empty;
}

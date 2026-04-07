namespace KvmDesktop.Models;

/// <summary>
/// Generic message wrapper for Named Pipe communication.
/// Using PascalCase for IPC to match C++ client expectations.
/// </summary>
public class PipeMessage
{
    public string Type { get; set; } = string.Empty;
    public object? Payload { get; set; }
}

public static class PipeMessageTypes
{
    public const string Handshake = "Handshake";
    public const string StatusUpdate = "StatusUpdate";
    public const string Error = "Error";
}

using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace KvmDesktop.Services;

public class WebSocketHidClient : IHidClient
{
    private readonly Channel<string> _messageChannel;
    private readonly CancellationTokenSource _cts = new();
    private ClientWebSocket? _webSocket;
    private Task? _sendTask;
    private bool _isDisposed;

    public bool IsConnected => _webSocket?.State == WebSocketState.Open;

    public WebSocketHidClient()
    {
        _messageChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    public async Task ConnectAsync(Uri uri, string token, CancellationToken ct = default)
    {
        if (IsConnected) return;

        // According to Go HID server implementation, token MUST be in query parameter.
        var uriBuilder = new UriBuilder(uri);
        var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
        query["token"] = token;
        uriBuilder.Query = query.ToString();
        Uri finalUri = uriBuilder.Uri;

        _webSocket = new ClientWebSocket();
        
        // No Authorization header needed for WebSocket handshake in this architecture.
        await _webSocket.ConnectAsync(finalUri, ct);
        
        _sendTask = Task.Run(() => ProcessQueueAsync(_cts.Token), _cts.Token);
    }

    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        if (_webSocket != null)
        {
            if (_webSocket.State == WebSocketState.Open)
            {
                try { await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", ct); } catch { }
            }
            _webSocket.Dispose();
            _webSocket = null;
        }

        if (_sendTask != null)
        {
            _cts.Cancel();
            try { await _sendTask; } catch (OperationCanceledException) { }
            _sendTask = null;
        }
    }

    public void EnqueueKeyboardEvent(byte modifiers, byte[] keys)
    {
        var message = new KeyboardMessage
        {
            Data = new KeyboardData
            {
                Modifiers = modifiers,
                Keys = keys
            }
        };
        
        string json = JsonSerializer.Serialize(message, JsonOptions);
        _messageChannel.Writer.TryWrite(json);
    }

    public void EnqueueMouseEvent(byte buttons, sbyte x, sbyte y, sbyte wheel)
    {
        var message = new MouseMessage
        {
            Data = new MouseData
            {
                Buttons = buttons,
                X = x,
                Y = y,
                Wheel = wheel
            }
        };

        string json = JsonSerializer.Serialize(message, JsonOptions);
        _messageChannel.Writer.TryWrite(json);
    }

    private async Task ProcessQueueAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var message in _messageChannel.Reader.ReadAllAsync(ct))
            {
                if (_webSocket?.State == WebSocketState.Open)
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(message);
                    await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, ct);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Console.WriteLine($"HID WebSocket error: {ex.Message}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        await DisconnectAsync();
        _cts.Dispose();
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private class KeyboardMessage
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "keyboard";
        [JsonPropertyName("data")]
        public KeyboardData Data { get; set; } = new();
    }

    private class KeyboardData
    {
        [JsonPropertyName("modifiers")]
        public byte Modifiers { get; set; }
        [JsonPropertyName("keys")]
        public byte[] Keys { get; set; } = Array.Empty<byte>();
    }

    private class MouseMessage
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "mouse";
        [JsonPropertyName("data")]
        public MouseData Data { get; set; } = new();
    }

    private class MouseData
    {
        [JsonPropertyName("buttons")]
        public byte Buttons { get; set; }
        [JsonPropertyName("x")]
        public sbyte X { get; set; } // Changed from short to sbyte
        [JsonPropertyName("y")]
        public sbyte Y { get; set; } // Changed from short to sbyte
        [JsonPropertyName("wheel")]
        public sbyte Wheel { get; set; }
    }
}

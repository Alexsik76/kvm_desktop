using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KvmDesktop.Models;

namespace KvmDesktop.Services;

public class PipeServerService : IPipeServerService
{
    private NamedPipeServerStream? _pipeServer;
    private CancellationTokenSource? _cts;
    private Task? _readTask;

    public bool IsConnected => _pipeServer?.IsConnected ?? false;

    public event EventHandler<PipeMessage>? MessageReceived;

    public async Task StartAsync(string pipeName)
    {
        Console.WriteLine($"[PipeServer] Starting server with name: {pipeName}");
        Stop();

        _cts = new CancellationTokenSource();
        _pipeServer = new NamedPipeServerStream(
            pipeName,
            PipeDirection.InOut,
            1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous);

        try
        {
            Console.WriteLine("[PipeServer] Waiting for client connection...");
            await _pipeServer.WaitForConnectionAsync(_cts.Token);
            Console.WriteLine("[PipeServer] Client connected!");
            
            // Start background listening task
            _readTask = Task.Run(() => ListenAsync(_cts.Token), _cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[PipeServer] Connection wait cancelled.");
            Stop();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PipeServer] Error during connection: {ex.Message}");
            Stop();
        }
    }

    public void Stop()
    {
        if (_pipeServer != null)
        {
            Console.WriteLine("[PipeServer] Stopping server and closing pipe.");
        }
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        _pipeServer?.Dispose();
        _pipeServer = null;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task SendAsync(PipeMessage message)
    {
        if (_pipeServer == null || !IsConnected)
        {
            Console.WriteLine("[PipeServer] Cannot send message: Not connected.");
            return;
        }

        try
        {
            string json = JsonSerializer.Serialize(message);
            Console.WriteLine($"[PipeServer] Sending message: {json}");
            byte[] buffer = Encoding.UTF8.GetBytes(json + "\n");
            await _pipeServer.WriteAsync(buffer, 0, buffer.Length);
            await _pipeServer.FlushAsync();
            Console.WriteLine("[PipeServer] Message sent successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PipeServer] Error sending message: {ex.Message}");
        }
    }

    private async Task ListenAsync(CancellationToken token)
    {
        if (_pipeServer == null) return;

        using var reader = new StreamReader(_pipeServer, Encoding.UTF8, leaveOpen: true);

        while (!token.IsCancellationRequested && IsConnected)
        {
            try
            {
                string? line = await reader.ReadLineAsync(token);
                if (line == null)
                {
                    Console.WriteLine("[PipeServer] End of stream reached (Client disconnected).");
                    break;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                Console.WriteLine($"[PipeServer] Message received from client: {line}");
                var message = JsonSerializer.Deserialize<PipeMessage>(line, JsonOptions);
                if (message != null)
                {
                    MessageReceived?.Invoke(this, message);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[PipeServer] Listening task cancelled.");
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PipeServer] Error reading message: {ex.Message}");
            }
        }
    }
}

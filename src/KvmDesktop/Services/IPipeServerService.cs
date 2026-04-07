using System;
using System.Threading.Tasks;
using KvmDesktop.Models;

namespace KvmDesktop.Services;

/// <summary>
/// Interface for managing the Named Pipe Server for IPC.
/// </summary>
public interface IPipeServerService
{
    bool IsConnected { get; }
    
    /// <summary>
    /// Starts the server and waits for a client connection asynchronously.
    /// </summary>
    Task StartAsync(string pipeName);
    
    /// <summary>
    /// Stops the server and closes the connection.
    /// </summary>
    void Stop();
    
    /// <summary>
    /// Sends a generic message asynchronously.
    /// </summary>
    Task SendAsync(PipeMessage message);
    
    /// <summary>
    /// Event triggered when a message is received from the client.
    /// </summary>
    event EventHandler<PipeMessage>? MessageReceived;
}

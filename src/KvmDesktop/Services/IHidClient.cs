using System;
using System.Threading;
using System.Threading.Tasks;

namespace KvmDesktop.Services;

public interface IHidClient : IAsyncDisposable
{
    bool IsConnected { get; }
    Task ConnectAsync(Uri uri, string token, CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);
    
    void EnqueueKeyboardEvent(byte modifiers, byte[] keys);
    void EnqueueMouseEvent(byte buttons, sbyte x, sbyte y, sbyte wheel);
}

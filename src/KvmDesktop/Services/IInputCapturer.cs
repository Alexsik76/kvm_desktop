using System;
using Avalonia.Controls;

namespace KvmDesktop.Services;

public interface IInputCapturer
{
    bool IsEnabled { get; set; }
    void Attach(Control control);
    void Detach();
    event EventHandler? CaptureReleased;
}

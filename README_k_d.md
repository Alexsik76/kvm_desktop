# KVM Desktop Client (Thick Client)

## Project Overview
KVM Desktop is a high-performance, cross-platform KVM (Keyboard, Video, Mouse) client application. It provides centralized control for remote Raspberry Pi-based KVM nodes, featuring real-time video streaming, low-latency HID (Human Interface Device) redirection, and secure node management.

## Key Features
- **Integrated Experience:** A unified "thick client" architecture where UI, control logic, and video rendering coexist in a single process.
- **Low-Latency HID Redirection:** Native mouse and keyboard capture with direct WebSocket transmission to the remote host.
- **High-Performance Video:** Hardware-accelerated video decoding via a specialized native C++ library (`KVMVideoCodec.dll`).
- **Secure Authentication:** JWT-based OAuth2 authentication against a FastAPI backend.
- **Modern UI:** Built with Avalonia UI, offering a responsive, high-DPI aware interface with an integrated status overlay.

## System Architecture (New Architecture)
The application has migrated from a launcher-based model to an integrated in-process architecture:

### 1. C# Core (.NET 10)
- **UI Framework:** Avalonia UI (MVVM Pattern).
- **HID Management:** `InputCapturer` handles low-level Windows/Linux input events, converting them to USB HID Usage codes via `AvaloniaEventMapper`.
- **Networking:** `WebSocketHidClient` manages a persistent, high-speed connection for input transmission using `System.Net.WebSockets`.
- **Orchestration:** `KvmSessionViewModel` coordinates the lifecycle of both HID and Video components.

### 2. Native Video Engine (C++ DLL)
- **Library:** `KVMVideoCodec.dll` (P/Invoke integration).
- **Functionality:** Handles WebRTC/WHEP stream ingestion and H.264 decoding.
- **Interoperability:** Decoded frames are passed back to C# via a high-speed memory callback (`FrameCallback`) and rendered using `WriteableBitmap`.

### 3. Removed Components
- **Named Pipes (IPC):** No longer required as all communication happens within a single process memory space.
- **External Launcher Logic:** The app no longer spawns separate `KVMControlApp.exe` processes.

## Technical Stack
- **Language:** C# 12 / .NET 10.0.
- **UI:** Avalonia UI 11.x.
- **MVVM:** CommunityToolkit.Mvvm.
- **Native Interop:** P/Invoke (LibraryImport) with Unsafe Blocks for high-speed memory copying.
- **DI:** Microsoft.Extensions.DependencyInjection.

## System Requirements
- **Operating System:** Windows 10/11 (x64), Linux, or macOS.
- **Runtime:** .NET 10.0 Runtime.
- **Dependencies:** `KVMVideoCodec.dll` (and its dependencies like FFmpeg/WebRTC) must be present in the application directory.

## Compilation and Deployment
1. **Prepare Native Assets:** Ensure `KVMVideoCodec.dll` is compiled and available.
2. **Build C# Project:**
   ```powershell
   cd src/KvmDesktop
   dotnet build -c Release
   ```
3. **Execution:** Run `KvmDesktop.exe` (or `dotnet KvmDesktop.dll`).

## Debug HUD (Diagnostics Overlay)

A minimal performance overlay can be activated for debugging and profiling. It displays two real-time metrics updated once per second:

| Metric | Description |
|--------|-------------|
| **FPS** | Frames decoded in the last second |
| **Frame** | Average inter-frame interval (ms), a proxy for decode latency |

FPS is color-coded: **green** ≥ 50, **yellow** 20–49, **red** < 20.

The HUD is rendered as a semi-transparent monospace panel in the top-right corner of the video overlay (`KvmOverlayView`). It is completely invisible in production unless explicitly enabled.

### Activation

```powershell
# Via environment variable (recommended)
$env:KVM_DEBUG=1; .\KvmDesktop.exe

# Via CLI argument
.\KvmDesktop.exe --debug
```

### Implementation notes
- Metrics are accumulated on the C++ decoder callback thread (`KvmSessionViewModel.OnFrameReceived`) — no render-thread blocking.
- UI properties (`Fps`, `FrameIntervalMs`) are pushed to `KvmOverlayViewModel` via `Dispatcher.UIThread.Post`, consistent with the existing double-buffer render pattern.
- `IsDebugEnabled` is read once in the `KvmOverlayViewModel` constructor; changing the env var at runtime has no effect.
- Color coding uses `FpsToColorConverter` (`Converters/FpsToColorConverter.cs`), registered as a local `UserControl.Resources` in `KvmOverlayView.axaml`.

## HID Control Details
- **Capture Toggle:** Press **F11** to enter/exit "Relative Mouse" mode.
- **Emergency Release:** Press **Ctrl + Alt** to immediately release mouse and keyboard focus.
- **Mapping:** Standard US-HID keyboard layout mapping is provided by default.

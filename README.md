# KVM Desktop Launcher

## Project Overview
KVM Desktop is a specialized cross-platform management application designed to act as a centralized control plane for remote KVM (Keyboard, Video, Mouse) nodes based on Raspberry Pi hardware. The application facilitates secure user authentication, real-time node monitoring, and the orchestrated invocation of high-performance native streaming clients.

## Functional Capabilities
- **Identity Management:** Secure authentication against a FastAPI backend using OAuth2 Password Flow (JWT).
- **Session Persistence:** Optional encrypted local storage of user credentials for automated session initialization.
- **Node Discovery:** Real-time retrieval and visualization of available KVM hardware nodes, including network status and resource endpoints.
- **Process Orchestration:** Automated lifecycle management of native client processes with secure parameter passing.
- **Inter-Process Communication (IPC):** Bi-directional communication with native clients via Named Pipes for secure credential transfer and status reporting.

## System Architecture
The application is built using a strict Model-View-ViewModel (MVVM) architectural pattern to ensure separation of concerns and maintainability.
- **UI Framework:** Avalonia UI (XAML-based cross-platform framework).
- **Core Runtime:** .NET 10.0.
- **Dependency Management:** Microsoft Extensions Dependency Injection for inversion of control.
- **State Management:** Singleton-based session handling for synchronized token propagation across services.

## System Requirements
- **Operating System:** Windows 10/11 (x64), Linux (with X11 or Wayland), or macOS.
- **Runtime:** .NET 10.0 SDK or Runtime environment.
- **Network:** Access to the KVM Control Plane API (https://kvm-api.lab.vn.ua).
- **Native Client:** Compatible `KVMControlApp` binary for the respective platform.

## Compilation and Deployment
To compile the application from the source repository, ensure the .NET 10 SDK is installed and execute the following commands in the project root:

1. Restore dependencies:
   `dotnet restore`

2. Build the project in release configuration:
   `dotnet build -c Release`

3. The compiled binaries will be located in:
   `src/KvmDesktop/bin/Release/net10.0/`

## Native Client Integration Requirements
The launcher acts as a supervisor for a native C++ client. For successful integration, the client must adhere to the following specifications:

### Executable Placement
The launcher expects the native client binary to be located in the same directory as the primary application executable.
- **Windows:** `KVMControlApp.exe`
- **Linux:** `KVMControlApp`

### Interaction Interface (IPC)
Communication is established via Named Pipes to prevent sensitive data exposure in the system process tree.
- **Execution Argument:** The client is invoked with a single argument: `--pipe <UNIQUE_PIPE_NAME>`.
- **Pipe Configuration:** The client must connect to the pipe path `\\.\pipe\<UNIQUE_PIPE_NAME>` (on Windows) with `InOut` directionality.
- **Data Protocol:** Messages are exchanged as UTF-8 encoded, single-line JSON strings terminated by a newline character (`\n`).

### Connection Sequence
1. The Launcher starts the Pipe Server and executes the Client.
2. The Client connects to the designated Pipe.
3. The Launcher transmits a `Handshake` message containing:
   - `AccessToken`: JWT for API and WebSocket authorization.
   - `StreamUrl`: Endpoint for WebRTC/WHEP video ingestion.
   - `HidUrl`: Endpoint for WebSocket HID control proxy.
4. The Client may subsequently send `StatusUpdate` or `Error` messages back to the Launcher for UI feedback.

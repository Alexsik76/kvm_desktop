# C++ Client IPC Integration Guide (Named Pipes)

This document describes how the native C++ client (`control_app`) must communicate with the C# Launcher using Named Pipes.

## 1. Execution Arguments
The Launcher will execute the client with the following command-line argument:
`control_app --pipe <UNIQUE_PIPE_NAME>`

**Example:**
`control_app --pipe kvm_pipe_550e8400e29b41d4a716446655440000`

## 2. Connecting to the Pipe
On Windows, the client must connect to the following pipe path:
`\\.\pipe\<UNIQUE_PIPE_NAME>`

### Windows C++ API Example:
```cpp
HANDLE hPipe = CreateFile(
    TEXT("\\\\.\\pipe\\kvm_pipe_..."), 
    GENERIC_READ | GENERIC_WRITE, 
    0, NULL, OPEN_EXISTING, 0, NULL);
```

## 3. Communication Protocol
The protocol uses **JSON messages** sent as **single-line strings** terminated by a **newline character (`\n`)**.

### 3.1. Messages from Launcher (Receive only)
The first message sent by the Launcher after connection is the **Handshake**.

**Type:** `Handshake`
**Payload structure:**
```json
{
  "Type": "Handshake",
  "Payload": {
    "AccessToken": "eyJhbG...",
    "StreamUrl": "https://api.lab.vn.ua/v1/nodes/offer",
    "HidUrl": "wss://api.lab.vn.ua/v1/nodes/ws"
  }
}
```
*Note: The `Payload` object in the Handshake message contains all sensitive configuration.*

### 3.2. Messages from Client (Send to Launcher)
The client can send status updates or error messages to the Launcher to be displayed in the UI.

**Type:** `StatusUpdate`
```json
{
  "Type": "StatusUpdate",
  "Payload": "WebRTC Connection established"
}
```

**Type:** `Error`
```json
{
  "Type": "Error",
  "Payload": "Failed to access video stream (403 Forbidden)"
}
```

## 4. Message Format Requirements
1. **JSON Encoding:** Use UTF-8.
2. **Delimiter:** Every JSON message MUST end with `\n`.
3. **Bi-directional:** The pipe is opened in `InOut` mode by the Launcher.

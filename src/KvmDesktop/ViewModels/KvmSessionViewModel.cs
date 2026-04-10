using System;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KvmDesktop.Services;

namespace KvmDesktop.ViewModels;

public partial class KvmSessionViewModel : ViewModelBase
{
    private readonly IHidClient _hidClient;
    internal readonly IInputCapturer _inputCapturer;
    
    [ObservableProperty]
    private KvmOverlayViewModel _overlay;

    [ObservableProperty]
    private WriteableBitmap? _videoBitmap;

    private WriteableBitmap? _bitmap1;
    private WriteableBitmap? _bitmap2;
    private bool _useBitmap1 = true;

    private readonly string _streamUrl; // Changed from _signalingUrl
    private readonly string _hidUrl;
    private readonly string _token;
    private readonly VideoDecoderNative.FrameCallback _frameCallback;

    public KvmSessionViewModel(
        IHidClient hidClient, 
        IInputCapturer inputCapturer,
        string streamUrl, // Changed from signalingUrl
        string hidUrl,
        string token)
    {
        _hidClient = hidClient;
        _inputCapturer = inputCapturer;
        _streamUrl = streamUrl;
        _hidUrl = hidUrl;
        _token = token;
        Overlay = new KvmOverlayViewModel();
        
        // Pin the delegate to prevent GC
        _frameCallback = OnFrameReceived;
    }

    [RelayCommand]
    public async Task StartSessionAsync()
    {
        Console.WriteLine($"[KVM Session] Starting session. Stream: {_streamUrl}, HID: {_hidUrl}");
        Overlay.StatusMessage = "Connecting to HID...";
        try
        {
            // Use exact node address for HID
            Uri hidUri = new Uri(_hidUrl);
            Console.WriteLine($"[KVM Session] Connecting HID WebSocket...");

            await _hidClient.ConnectAsync(hidUri, _token);
            Overlay.IsHidConnected = true;
            Console.WriteLine("[KVM Session] HID Connected successfully.");

            Overlay.StatusMessage = "Connecting to Video...";
            Console.WriteLine($"[KVM Session] Initializing Video DLL with URL: {_streamUrl}");

            int result = VideoDecoderNative.KvmInitialize(_streamUrl, _token, _frameCallback);
            if (result == 0)
            {
                Overlay.IsVideoConnected = true;
                Overlay.StatusMessage = "Connected";
                Console.WriteLine("[KVM Session] Video Initialization success.");
            }
            else
            {
                Overlay.StatusMessage = $"Video Error (Code: {result})";
                Console.WriteLine($"[KVM Session] Video Initialization FAILED with code: {result}");
            }
        }
        catch (Exception ex)
        {
            Overlay.StatusMessage = $"Error: {ex.Message}";
            Console.WriteLine($"[KVM Session] CRITICAL ERROR: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task StopSessionAsync()
    {
        VideoDecoderNative.KvmStop();
        await _hidClient.DisconnectAsync();
        _inputCapturer.IsEnabled = false;
        Overlay.IsVideoConnected = false;
        Overlay.IsHidConnected = false;
    }

    [RelayCommand]
    public void ToggleCapture()
    {
        _inputCapturer.IsEnabled = !_inputCapturer.IsEnabled;
        Overlay.IsMouseCaptured = _inputCapturer.IsEnabled;
    }

    private void OnFrameReceived(IntPtr data, int width, int height, int stride)
    {
        // Копіюємо кадр локально, щоб негайно звільнити фоновий потік декодера (C++)
        int bufferSize = height * stride;
        byte[] frameBuffer = System.Buffers.ArrayPool<byte>.Shared.Rent(bufferSize);
        Marshal.Copy(data, frameBuffer, 0, bufferSize);

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            try
            {
                // Перевіряємо, чи потрібно створити або перестворити буфери (якщо змінився розмір)
                if (_bitmap1 == null || _bitmap1.PixelSize.Width != width || _bitmap1.PixelSize.Height != height)
                {
                    _bitmap1 = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Opaque);
                    _bitmap2 = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Opaque);
                    Overlay.VideoResolution = $"{width}x{height}";
                }

                // Вибираємо буфер для запису (той, який зараз НЕ відображається)
                var targetBitmap = _useBitmap1 ? _bitmap1 : _bitmap2;

                using (var lockedBitmap = targetBitmap.Lock())
                {
                    Marshal.Copy(frameBuffer, 0, lockedBitmap.Address, bufferSize);
                }

                // Avalonia ігнорує OnPropertyChanged, якщо посилання на об'єкт не змінилося.
                // Змінюючи посилання на інший об'єкт (подвійна буферизація), ми ГАРАНТОВАНО змушуємо UI перемалювати кадр.
                VideoBitmap = targetBitmap;
                _useBitmap1 = !_useBitmap1;
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(frameBuffer);
            }
        });
    }
}
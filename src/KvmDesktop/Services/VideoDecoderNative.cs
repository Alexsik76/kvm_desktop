using System;
using System.Runtime.InteropServices;

namespace KvmDesktop.Services;

public static partial class VideoDecoderNative
{
    private const string DllName = "KVMVideoCodec";

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void FrameCallback(IntPtr data, int width, int height, int stride);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int KvmInitialize(string url, string token, FrameCallback callback);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void KvmStop();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr KvmGetVersion();
}

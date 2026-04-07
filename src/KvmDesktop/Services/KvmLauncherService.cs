using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using KvmDesktop.Models;

namespace KvmDesktop.Services;

public class KvmLauncherService : IKvmLauncherService
{
    public Task LaunchNodeAsync(KvmNode node, string token)
    {
        string executableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
            ? "control_app.exe" 
            : "control_app";

        // Construct arguments: control_app --stream <video_url> --hid <hid_url> --token <jwt_token>
        // Note: Using the URLs from the node model. 
        // Based on README_API, stream_url and hid_url are provided by the API.
        string arguments = $"--stream \"{node.StreamUrl}\" --hid \"{node.HidUrl}\" --token \"{token}\"";

        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = executableName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = false
            };

            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            // In a real app, we would notify the user that the executable was not found
            Debug.WriteLine($"Failed to launch KVM client: {ex.Message}");
            throw new InvalidOperationException($"Could not launch {executableName}. Ensure it is in the application directory or PATH.", ex);
        }

        return Task.CompletedTask;
    }
}

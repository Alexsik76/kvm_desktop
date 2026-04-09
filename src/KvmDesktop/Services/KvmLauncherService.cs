using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using KvmDesktop.Models;

namespace KvmDesktop.Services;

public class KvmLauncherService : IKvmLauncherService
{
    public Task LaunchNodeAsync(KvmNode node, string pipeName)
    {
        string executableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
            ? "KVMControlApp.exe" 
            : "KVMControlApp";

        // Construct arguments: KVMControlApp --pipe <pipe_name>
        string arguments = $"--pipe \"{pipeName}\"";

        try
        {
            // Navigate from bin/Debug/net10.0 up to the root 'app' folder, then into control_app/build/Debug
            string baseDir = AppContext.BaseDirectory;
            string workingDirectory = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, "..", "..", "..", "..", "..", "..", "control_app", "build", "Debug"));
            string executablePath = System.IO.Path.Combine(workingDirectory, executableName);

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Process process = new Process { StartInfo = startInfo };
            
            process.OutputDataReceived += (s, e) => 
            {
                if (e.Data != null)
                {
                    Debug.WriteLine($"[KVMControlApp] {e.Data}");
                }
            };
            
            process.ErrorDataReceived += (s, e) => 
            {
                if (e.Data != null)
                {
                    Debug.WriteLine($"[KVMControlApp Error] {e.Data}");
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
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

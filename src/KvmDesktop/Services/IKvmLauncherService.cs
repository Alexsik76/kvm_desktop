using System.Threading.Tasks;
using KvmDesktop.Models;

namespace KvmDesktop.Services;

/// <summary>
/// Interface for launching the native KVM client application.
/// </summary>
public interface IKvmLauncherService
{
    Task LaunchNodeAsync(KvmNode node, string token);
}

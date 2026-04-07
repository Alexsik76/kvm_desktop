using System.Threading.Tasks;

namespace KvmDesktop.Services;

/// <summary>
/// Interface for authentication operations.
/// </summary>
public interface IAuthService
{
    Task<bool> LoginAsync(string username, string password);
    void Logout();
}

using KvmDesktop.Models;

namespace KvmDesktop.Services;

/// <summary>
/// Manages the current user session state.
/// </summary>
public interface IUserSession
{
    User? CurrentUser { get; set; }
    bool IsAuthenticated => CurrentUser != null;
}

public class UserSession : IUserSession
{
    public User? CurrentUser { get; set; }
}

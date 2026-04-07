using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using KvmDesktop.Models;

namespace KvmDesktop.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IUserSession _userSession;

    public AuthService(HttpClient httpClient, IUserSession userSession)
    {
        _httpClient = httpClient;
        _userSession = userSession;
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("username", username),
            new KeyValuePair<string, string>("password", password)
        });

        try
        {
            var response = await _httpClient.PostAsync("auth/login", content);
            if (response.IsSuccessStatusCode)
            {
                var authData = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (authData != null)
                {
                    _userSession.CurrentUser = new User
                    {
                        Username = username,
                        AccessToken = authData.AccessToken,
                        RefreshToken = authData.RefreshToken
                    };
                    return true;
                }
            }
        }
        catch (Exception)
        {
            // Log error in a real app
        }

        return false;
    }

    public void Logout()
    {
        _userSession.CurrentUser = null;
    }
}

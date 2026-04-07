using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using KvmDesktop.Models;

namespace KvmDesktop.Services;

public class NodeService : INodeService
{
    private readonly HttpClient _httpClient;
    private readonly IUserSession _userSession;

    public NodeService(HttpClient httpClient, IUserSession userSession)
    {
        _httpClient = httpClient;
        _userSession = userSession;
    }

    public async Task<IEnumerable<KvmNode>> GetNodesAsync()
    {
        if (_userSession.CurrentUser == null)
            return Array.Empty<KvmNode>();

        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _userSession.CurrentUser.AccessToken);

        try
        {
            var nodes = await _httpClient.GetFromJsonAsync<IEnumerable<KvmNode>>("nodes");
            return nodes ?? Array.Empty<KvmNode>();
        }
        catch (Exception)
        {
            return Array.Empty<KvmNode>();
        }
    }
}

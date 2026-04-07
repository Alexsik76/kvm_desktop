using System.Collections.Generic;
using System.Threading.Tasks;
using KvmDesktop.Models;

namespace KvmDesktop.Services;

/// <summary>
/// Interface for managing KVM nodes.
/// </summary>
public interface INodeService
{
    Task<IEnumerable<KvmNode>> GetNodesAsync();
}

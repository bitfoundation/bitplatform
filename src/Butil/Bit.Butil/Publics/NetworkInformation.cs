using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/NetworkInformation">Network Information API</see>
/// (<c>navigator.connection</c>) plus the always-available <c>navigator.onLine</c>.
/// </summary>
public class NetworkInformation(IJSRuntime js)
{
    /// <summary>One-shot snapshot of the network state.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(NetworkConnectionStatus))]
    public ValueTask<NetworkConnectionStatus> GetStatus()
        => js.Invoke<NetworkConnectionStatus>("BitButil.networkInformation.getStatus");
}

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Bit.Butil.Publics.UserAgent;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// This service is used to detect the user agent information such as the Operating System, browser or web-view, versions and properties.
/// </summary>
public class UserAgent(IJSRuntime js)
{
    /// <summary>
    /// Extracts the user agent properties from the browser or web-view.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UserAgentProperties))]
    public async ValueTask<UserAgentProperties> Extract()
    {
        return await js.FastInvokeAsync<UserAgentProperties>("BitButil.userAgent.extract");
    }
}

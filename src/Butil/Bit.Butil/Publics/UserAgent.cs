using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
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
    public async ValueTask<UserAgentProperties> Extract(string? userAgentString = null)
    {
        return await js.Invoke<UserAgentProperties>("BitButil.userAgent.extract", userAgentString);
    }

    /// <summary>
    /// True when the runtime exposes <c>navigator.userAgentData</c> (modern UA Client Hints).
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsClientHintsSupported()
        => js.Invoke<bool>("BitButil.userAgent.isClientHintsSupported");

    /// <summary>
    /// Low-entropy brands list from <c>navigator.userAgentData.brands</c>. Returns an empty array
    /// on browsers that don't expose UA-CH.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UserAgentBrand))]
    public ValueTask<UserAgentBrand[]> GetBrands()
        => js.Invoke<UserAgentBrand[]>("BitButil.userAgent.getBrands");

    /// <summary>True when the user-agent identifies itself as a mobile device.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsMobile()
        => js.Invoke<bool>("BitButil.userAgent.isMobile");

    /// <summary>The OS family the UA-CH layer reports - empty string when unsupported.</summary>
    public ValueTask<string> GetPlatform()
        => js.Invoke<string>("BitButil.userAgent.getPlatform");

    /// <summary>
    /// Requests high-entropy UA values. Callers must explicitly opt in to each hint
    /// they need (e.g. <c>"architecture", "platformVersion", "model"</c>) - see
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/NavigatorUAData/getHighEntropyValues">getHighEntropyValues()</see>.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(HighEntropyUserAgent))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UserAgentBrand))]
    public ValueTask<HighEntropyUserAgent> GetHighEntropyValues(params string[] hints)
        => js.Invoke<HighEntropyUserAgent>("BitButil.userAgent.getHighEntropyValues", (object)hints);
}

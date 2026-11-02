using Microsoft.AspNetCore.Components;

namespace Bit.BlazeDb.Samples.Core.Shared;

/// <summary>
/// A UTC time as the person looking at the page should read it. WebAssembly and the MAUI WebView run on their own
/// device, so its local time zone is theirs. A server renders - prerendered, or on an Interactive Server circuit -
/// for people anywhere, and its local time zone is nobody's in particular, so there the time is shown in UTC and
/// says so.
/// </summary>
public static class ViewerTime
{
    public static string Format(DateTime utc, RendererInfo renderer, string format) =>
        renderer.Name is "WebAssembly" or "WebView"
            ? utc.ToLocalTime().ToString(format)
            : $"{utc.ToString(format)} UTC";
}

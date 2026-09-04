namespace Bit.Butil;

/// <summary>
/// A user-agent string parsed into the pieces people actually want from it. This is best-effort
/// pattern matching over a string browsers deliberately freeze and lie in, so treat every member as
/// a hint: for anything a decision depends on, feature-detect, or ask
/// <see cref="UserAgent.GetHighEntropyValues"/> for the UA Client Hints values instead.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator/userAgent">Navigator.userAgent</see>
/// </summary>
public class UserAgentProperties
{
    /// <summary>The browser or web-view name - e.g. <c>"Chrome"</c>, <c>"Safari"</c>.</summary>
    public string? Name { get; set; }

    /// <summary>The browser version, as far as the string reveals it.</summary>
    public string? Version { get; set; }

    /// <summary>The release channel when the build advertises one - e.g. <c>"beta"</c>, <c>"nightly"</c>.</summary>
    public string? Prerelease { get; set; }

    /// <summary>The rendering engine - e.g. <c>"Blink"</c>, <c>"WebKit"</c>, <c>"Gecko"</c>.</summary>
    public string? Layout { get; set; }

    /// <summary>The device manufacturer, when the string names one - e.g. <c>"Apple"</c>.</summary>
    public string? Manufacturer { get; set; }

    /// <summary>The device model, when the string names one - e.g. <c>"iPhone"</c>.</summary>
    public string? Product { get; set; }

    /// <summary>The operating system name - e.g. <c>"Windows"</c>, <c>"Android"</c>, <c>"iOS"</c>.</summary>
    public string? OsName { get; set; }

    /// <summary>The operating system version, as far as the string reveals it.</summary>
    public string? OsVersion { get; set; }

    /// <summary>The OS architecture in bits - 32 or 64 - or <c>null</c> when the string does not say.</summary>
    public int? OsArchitecture { get; set; }

    /// <summary>The parsed pieces joined into one human-readable line.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// The raw string all of the above was parsed out of - either the one passed to
    /// <see cref="UserAgent.Extract"/> or the browser's own.
    /// </summary>
    public string? UserAgentValue { get; set; }
}

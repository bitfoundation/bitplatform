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
    /// <summary>
    /// The browser, web-view or crawler name - e.g. <c>"Chrome"</c>, <c>"Safari"</c>,
    /// <c>"Android WebView"</c>, <c>"Googlebot"</c>.
    /// </summary>
    /// <remarks>
    /// An in-app browser is named as itself (<c>"Facebook"</c>, <c>"WeChat"</c>) rather than as the
    /// engine it embeds, because that is the part a page's behaviour actually differs by.
    /// </remarks>
    public string? Name { get; set; }

    /// <summary>
    /// The browser version, as far as the string reveals it - <c>null</c> where it reveals none,
    /// which is what an iOS in-app web view and some TV browsers send.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>The release channel when the build advertises one - e.g. <c>"beta"</c>, <c>"nightly"</c>.</summary>
    public string? Prerelease { get; set; }

    /// <summary>The rendering engine - e.g. <c>"Blink"</c>, <c>"WebKit"</c>, <c>"Gecko"</c>.</summary>
    public string? Layout { get; set; }

    /// <summary>The device manufacturer, when the string names one - e.g. <c>"Apple"</c>.</summary>
    public string? Manufacturer { get; set; }

    /// <summary>The device model, when the string names one - e.g. <c>"iPhone"</c>.</summary>
    /// <remarks>
    /// Chrome 110 and later freeze Android's model field to the literal <c>"K"</c>, so most current
    /// Android strings name no device at all and this is <c>null</c>. The real model is a
    /// high-entropy Client Hint: <c>GetHighEntropyValues("model")</c>.
    /// </remarks>
    public string? Product { get; set; }

    /// <summary>
    /// The operating system name - e.g. <c>"Windows"</c>, <c>"Android"</c>, <c>"iOS"</c>,
    /// <c>"macOS"</c>.
    /// </summary>
    public string? OsName { get; set; }

    /// <summary>The operating system version, as far as the string reveals it.</summary>
    /// <remarks>
    /// Two of these are frozen at the source and no parser can unfreeze them: Windows 11 still
    /// says <c>NT 10.0</c>, so it reads as <c>"10"</c>, and every macOS since Big Sur still says
    /// <c>10_15_7</c>. <c>GetHighEntropyValues("platformVersion")</c> gives the real number where
    /// the browser supports Client Hints.
    /// </remarks>
    public string? OsVersion { get; set; }

    /// <summary>The OS architecture in bits - 32 or 64 - or <c>null</c> when it cannot be known.</summary>
    /// <remarks>
    /// Most mobile strings say nothing about bitness, and this reports <c>null</c> for them rather
    /// than a default. The exceptions are iOS and macOS, which are 64-bit by the fact that Apple
    /// removed 32-bit support from both years ago.
    /// </remarks>
    public int? OsArchitecture { get; set; }

    /// <summary>The parsed pieces joined into one human-readable line.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// The raw string all of the above was parsed out of - either the one passed to
    /// <see cref="UserAgent.Extract"/> or the browser's own.
    /// </summary>
    public string? UserAgentValue { get; set; }
}

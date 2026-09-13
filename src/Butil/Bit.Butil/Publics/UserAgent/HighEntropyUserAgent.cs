namespace Bit.Butil;

/// <summary>
/// Result of <see cref="UserAgent.GetHighEntropyValues"/>. All fields are nullable because callers
/// can request a subset and the runtime may decline to provide some values.
/// </summary>
public class HighEntropyUserAgent
{
    /// <summary>The CPU architecture, e.g. <c>"x86"</c> or <c>"arm"</c>.</summary>
    public string? Architecture { get; set; }

    /// <summary>The architecture's word size, e.g. <c>"64"</c>.</summary>
    public string? Bitness { get; set; }

    /// <summary>The brand list, major versions only - the same values the low-entropy header carries.</summary>
    public UserAgentBrand[]? Brands { get; set; }

    /// <summary>The brand list with full version strings.</summary>
    public UserAgentBrand[]? FullVersionList { get; set; }

    /// <summary>True on a mobile device.</summary>
    public bool? Mobile { get; set; }

    /// <summary>The device model. Only Android reports one; elsewhere it is empty.</summary>
    public string? Model { get; set; }

    /// <summary>The platform name, e.g. <c>"Windows"</c>, <c>"macOS"</c>, <c>"Android"</c>.</summary>
    public string? Platform { get; set; }

    /// <summary>The platform version. On Windows this is a Chromium-specific number, not the marketing one.</summary>
    public string? PlatformVersion { get; set; }

    /// <summary>The browser's full version string.</summary>
    public string? UaFullVersion { get; set; }
    
    /// <summary>True for a 32-bit browser running under WOW64 on 64-bit Windows.</summary>
    public bool? Wow64 { get; set; }
}

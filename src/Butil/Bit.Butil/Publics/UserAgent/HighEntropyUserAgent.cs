namespace Bit.Butil;

/// <summary>
/// Result of <see cref="UserAgent.GetHighEntropyValues"/>. All fields are nullable because callers
/// can request a subset and the runtime may decline to provide some values.
/// </summary>
public class HighEntropyUserAgent
{
    public string? Architecture { get; set; }
    public string? Bitness { get; set; }
    public UserAgentBrand[]? Brands { get; set; }
    public UserAgentBrand[]? FullVersionList { get; set; }
    public bool? Mobile { get; set; }
    public string? Model { get; set; }
    public string? Platform { get; set; }
    public string? PlatformVersion { get; set; }
    public string? UaFullVersion { get; set; }
    public bool? Wow64 { get; set; }
}

namespace Bit.Butil;

/// <summary>One entry inside a <see cref="WebLockSnapshot"/>.</summary>
public class WebLockInfo
{
    /// <summary>The lock's name - the string it was requested under.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary><c>"exclusive"</c> or <c>"shared"</c>.</summary>
    public string Mode { get; set; } = "exclusive";
    
    /// <summary>An opaque id for the tab or worker holding or awaiting the lock.</summary>
    public string ClientId { get; set; } = string.Empty;
}

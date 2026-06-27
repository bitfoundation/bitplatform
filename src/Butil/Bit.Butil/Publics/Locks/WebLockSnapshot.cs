namespace Bit.Butil;

/// <summary>
/// Snapshot of <see href="https://developer.mozilla.org/en-US/docs/Web/API/LockManager/query">LockManager.query()</see>.
/// </summary>
public class WebLockSnapshot
{
    public WebLockInfo[] Held { get; set; } = [];
    public WebLockInfo[] Pending { get; set; } = [];
}

/// <summary>One entry inside a <see cref="WebLockSnapshot"/>.</summary>
public class WebLockInfo
{
    public string Name { get; set; } = string.Empty;
    public string Mode { get; set; } = "exclusive";
    public string ClientId { get; set; } = string.Empty;
}

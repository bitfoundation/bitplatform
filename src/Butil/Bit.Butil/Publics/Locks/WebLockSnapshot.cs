namespace Bit.Butil;

/// <summary>
/// Snapshot of <see href="https://developer.mozilla.org/en-US/docs/Web/API/LockManager/query">LockManager.query()</see>.
/// </summary>
public class WebLockSnapshot
{
    /// <summary>The locks currently granted, across every tab of this origin.</summary>
    public WebLockInfo[] Held { get; set; } = [];

    /// <summary>The requests still waiting for a lock to be released.</summary>
    public WebLockInfo[] Pending { get; set; } = [];
}

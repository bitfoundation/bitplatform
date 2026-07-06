namespace Bit.Butil;

/// <summary>
/// See <see href="https://developer.mozilla.org/en-US/docs/Web/API/LockManager/request#mode">LockManager.request().mode</see>.
/// </summary>
public enum WebLockMode
{
    /// <summary>Default. Only one exclusive holder at a time.</summary>
    Exclusive,

    /// <summary>Multiple shared holders allowed; mutually exclusive with Exclusive.</summary>
    Shared
}

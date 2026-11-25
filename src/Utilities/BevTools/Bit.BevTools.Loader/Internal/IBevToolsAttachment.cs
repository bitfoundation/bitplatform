namespace Bit.BevTools;

/// <summary>A BevTools session as the loader sees it. Disposing it ends a session the attach created, never one DI owns.</summary>
internal interface IBevToolsAttachment : IDisposable
{
    bool IsEnabled { get; }

    /// <summary>Why the session is or is not enabled.</summary>
    string Reason { get; }

    /// <summary>
    /// Opens the panel, on <paramref name="panelId"/> with <paramref name="query"/> in its filter when one is given.
    /// Returns why it did not, a panel this session does not have, or null once it is open.
    /// </summary>
    string? Open(string? panelId, string? query);
}

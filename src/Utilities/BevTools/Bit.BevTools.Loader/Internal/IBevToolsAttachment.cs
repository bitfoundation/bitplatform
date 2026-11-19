namespace Bit.BevTools;

/// <summary>A BevTools session as the loader sees it. Disposing it ends a session the attach created, never one DI owns.</summary>
internal interface IBevToolsAttachment : IDisposable
{
    bool IsEnabled { get; }

    /// <summary>Why the session is or is not enabled.</summary>
    string Reason { get; }

    void Open();
}

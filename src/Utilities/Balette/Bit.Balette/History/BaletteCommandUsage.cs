namespace Bit.Balette;

/// <summary>Usage statistics for one command, used for recents, pinning and frecency ranking.</summary>
public sealed record BaletteCommandUsage(int Count, DateTimeOffset LastUsed, bool Pinned)
{
    /// <summary>
    /// When the command was pinned, which orders the pinned commands: the first pinned is listed first. Null when it is not
    /// pinned - or was pinned before this was recorded, which lists it ahead of the dated ones, by id.
    /// </summary>
    public DateTimeOffset? PinnedAt { get; init; }
}

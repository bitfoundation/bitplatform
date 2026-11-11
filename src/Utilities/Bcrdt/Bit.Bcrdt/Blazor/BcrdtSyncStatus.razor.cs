namespace Bit.Bcrdt.Blazor;

/// <summary>
/// Says where a document's connection stands - synced, syncing, connecting, reconnecting, offline, or local only when no
/// transport is configured - with when it last synced while it is not, and whether the server granted read access only;
/// optionally with a button that takes it offline and back (<see cref="ShowToggle"/>). Shows the cascaded document, or
/// <see cref="Document"/>, and follows its connection as it changes. Supply <see cref="ChildContent"/> to render the
/// <see cref="BcrdtSyncStatusSnapshot"/> yourself.
/// </summary>
/// <remarks>
/// Named for the sync status rather than <c>BcrdtConnectionStatus</c>, which is the enum it reports: a component of that
/// name would make every <c>BcrdtConnectionStatus.Synced</c> ambiguous in a file that imports both namespaces.
/// </remarks>
public partial class BcrdtSyncStatus
{
    /// <summary>
    /// What the default rendering says, in words: the state, when the document last synced while it is not synced, and
    /// read-only access.
    /// </summary>
    /// <remarks>
    /// Words rather than only a colour, so the state reaches a screen reader and a reader who cannot tell the colours
    /// apart. The time is the local time, in <paramref name="culture"/>'s long time format.
    /// </remarks>
    internal static string Describe(BcrdtSyncStatusSnapshot status, IFormatProvider? culture)
    {
        if (status.IsLocalOnly) return "Local only";

        string text = status.Status switch
        {
            BcrdtConnectionStatus.Synced => "Synced",
            BcrdtConnectionStatus.Handshaking => "Syncing",
            BcrdtConnectionStatus.Connecting => "Connecting",
            BcrdtConnectionStatus.Reconnecting => "Reconnecting",
            BcrdtConnectionStatus.Faulted => "Connection failed",
            _ => "Offline",
        };

        // How fresh this copy is matters exactly while it may not be: once synced, it is as fresh as it gets.
        if (!status.IsSynced)
        {
            text += status.LastSyncedAt is { } at
                ? " - last synced " + at.ToLocalTime().ToString("T", culture)
                : " - never synced";
        }

        if (status.IsReadOnly) text += " - read-only";
        return text;
    }

    /// <summary>The state as a word for the <c>data-status</c> attribute, for a stylesheet to select on.</summary>
    internal static string StatusName(BcrdtSyncStatusSnapshot status) =>
        status.IsLocalOnly ? "local" : status.Status.ToString().ToLowerInvariant();

    // Logical sizes, so the dot sits the same way in a right-to-left page; its colour only repeats what the words say.
    private static string DotStyle(BcrdtSyncStatusSnapshot status)
    {
        string color = status switch
        {
            { IsSynced: true } => "#2e7d32",
            { IsBusy: true } => "#b26a00",
            { HasTransport: true, Status: BcrdtConnectionStatus.Faulted } => "#c62828",
            _ => "currentColor;opacity:.45",
        };
        return $"flex:none;inline-size:.6em;block-size:.6em;border-radius:50%;background:{color}";
    }
}

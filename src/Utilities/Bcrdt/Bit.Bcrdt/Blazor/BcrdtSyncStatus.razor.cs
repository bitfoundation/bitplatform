using Microsoft.AspNetCore.Components;

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
    private BcrdtCollaborativeDocument? _document;
    private BcrdtSyncProvider? _trackedSync;
    private IDisposable? _syncSubscription;
    private BcrdtSyncStatusSnapshot? _snapshot;
    private bool _toggling;

    /// <summary>
    /// The document whose connection to show. Without it, the document an enclosing
    /// <see cref="BcrdtCollaborationScope"/> cascades; without either, nothing is rendered.
    /// </summary>
    [Parameter]
    public BcrdtCollaborativeDocument? Document { get; set; }

    /// <summary>
    /// Whether to offer a button that takes the document offline on purpose and brings it back. Off by default: going
    /// offline is a choice an application offers deliberately, not one every status line should.
    /// </summary>
    /// <remarks>
    /// The button reads the connection rather than remembering its last click: one the server refused, or that gave up,
    /// offers to reconnect, and one still retrying offers to stop. It is not rendered while the page is prerendered,
    /// when nothing would answer a click, nor for a document without a transport.
    /// </remarks>
    [Parameter]
    public bool ShowToggle { get; set; }

    /// <summary>CSS classes for the toggle's button - your design system's button classes, say.</summary>
    [Parameter]
    public string? ToggleClass { get; set; }

    /// <summary>
    /// Invoked once the toggle has connected the document (<see langword="true"/>) or disconnected it
    /// (<see langword="false"/>).
    /// </summary>
    [Parameter]
    public EventCallback<bool> OnToggled { get; set; }

    /// <summary>CSS classes for the default rendering.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>
    /// Replaces the default rendering entirely, toggle included.
    /// </summary>
    /// <remarks>
    /// The built-in markup is deliberately plain - a dot and a sentence, with no stylesheet - so that the connection
    /// is visible in five minutes. Supply this to render the snapshot however the application wants; it is taken again
    /// on every change of the connection, coalesced like every render of <see cref="BcrdtComponentBase"/>. A template
    /// that offers to go offline calls the document's <c>ConnectAsync</c> and <c>DisconnectAsync</c> itself.
    /// </remarks>
    [Parameter]
    public RenderFragment<BcrdtSyncStatusSnapshot>? ChildContent { get; set; }

    /// <summary>The document opened by an enclosing <see cref="BcrdtCollaborationScope"/>, if any.</summary>
    [CascadingParameter]
    private BcrdtCollaborativeDocument? CascadedDocument { get; set; }

    protected override void OnParametersSet() => Refresh();

    protected override bool ShouldRender()
    {
        Refresh();
        return true;
    }

    private void Refresh()
    {
        _document = Document ?? CascadedDocument;

        // Follow the document's provider rather than the one first seen: a scope switching documents hands over a
        // different document, and the old provider's events must stop rendering this one - and stop keeping it alive.
        if (!ReferenceEquals(_trackedSync, _document?.Sync))
        {
            _syncSubscription?.Dispose();
            _syncSubscription = null;
            _trackedSync = _document?.Sync;
            if (_trackedSync is not null) _syncSubscription = Track(_trackedSync);
        }

        _snapshot = _document is null ? null : BcrdtSyncStatusSnapshot.Of(_document);
    }

    private async Task ToggleAsync()
    {
        // A second press while the first is in flight would undo it before it took effect.
        if (_toggling || _document is not { Sync: not null } document) return;

        bool connect = BcrdtSyncStatusSnapshot.Of(document).IsOffline;
        _toggling = true;
        try
        {
            if (connect) await document.ConnectAsync();
            else await document.DisconnectAsync();
        }
        catch (ObjectDisposedException)
        {
            // Closed while the button was on screen: there is no connection left to change.
            return;
        }
        finally
        {
            _toggling = false;
        }

        if (OnToggled.HasDelegate) await OnToggled.InvokeAsync(connect);
    }

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

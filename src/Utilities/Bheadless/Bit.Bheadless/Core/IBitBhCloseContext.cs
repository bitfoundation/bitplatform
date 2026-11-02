namespace Bit.Bheadless;

/// <summary>
/// A shared interface implemented by cascading contexts that support a "close"
/// action — dialogs, popovers, disclosures, collapsibles, menus … This enables
/// <see cref="BitBhCloseButton"/> to close the nearest enclosing closeable ancestor
/// without needing to know which specific component it's nested inside.
/// </summary>
public interface IBitBhCloseContext
{
    /// <summary>Closes the enclosing component.</summary>
    Task CloseAsync();
}

/// <summary>
/// A cancellable dismissal request raised before a dialog / popover closes in
/// response to Escape, an outside click or focus leaving it. Set
/// <see cref="Cancel"/> to keep it open (e.g. to confirm discarding changes).
/// </summary>
public sealed class BitBhDismissRequest
{
    /// <summary>Creates a request.</summary>
    public BitBhDismissRequest(BitBhDismissReason reason) => Reason = reason;

    /// <summary>Why the component is being dismissed.</summary>
    public BitBhDismissReason Reason { get; }

    /// <summary>Set to <c>true</c> to prevent the dismissal.</summary>
    public bool Cancel { get; set; }
}

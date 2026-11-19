namespace Bit.Bheadless;

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

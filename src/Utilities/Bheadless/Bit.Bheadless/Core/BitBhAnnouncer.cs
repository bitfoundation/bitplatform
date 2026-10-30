namespace Bit.Bheadless;

/// <summary>
/// Announces messages to screen-reader users through a visually hidden ARIA live
/// region — for async results ("12 results"), drag-and-drop progress, toasts, form
/// submission outcomes and any other change that isn't conveyed by focus.
///
/// <para>
/// Inject <see cref="IBitBhAnnouncer"/> anywhere. In tests, replace it with a fake
/// that records messages (see <c>Bit.Bheadless.Testing</c>).
/// </para>
/// <code>
/// @inject IBitBhAnnouncer Announcer
/// await Announcer.AnnounceAsync($"{results.Count} results available");
/// </code>
/// </summary>
public interface IBitBhAnnouncer
{
    /// <summary>Announces <paramref name="message"/> with the given politeness.</summary>
    ValueTask AnnounceAsync(string message, BitBhPoliteness politeness = BitBhPoliteness.Polite);
}

/// <summary>Default <see cref="IBitBhAnnouncer"/> backed by a live region the JS runtime appends to &lt;body&gt;.</summary>
public sealed class BitBhAnnouncer : IBitBhAnnouncer
{
    private readonly BitBhInterop _interop;

    /// <summary>Creates the announcer.</summary>
    public BitBhAnnouncer(BitBhInterop interop)
    {
        _interop = interop;
    }

    /// <summary>How long (ms) a message stays in the region before it is cleared. Default: 7000.</summary>
    public int ClearAfterMilliseconds { get; set; } = 7000;

    /// <inheritdoc />
    public ValueTask AnnounceAsync(string message, BitBhPoliteness politeness = BitBhPoliteness.Polite)
        => string.IsNullOrWhiteSpace(message)
            ? ValueTask.CompletedTask
            : _interop.AnnounceAsync(message, politeness, ClearAfterMilliseconds);
}

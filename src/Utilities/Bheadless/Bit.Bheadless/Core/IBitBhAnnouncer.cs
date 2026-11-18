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

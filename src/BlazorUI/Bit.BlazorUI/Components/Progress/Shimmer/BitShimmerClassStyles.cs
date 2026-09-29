namespace Bit.BlazorUI;

/// <summary>
/// Defines per-part CSS class/style values for <see cref="BitShimmer"/>.
/// </summary>
public class BitShimmerClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitShimmer.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the content of the BitShimmer.
    /// </summary>
    /// <remarks>
    /// The same box holds the content an <see cref="BitShimmer.Overlay"/> covers, so a shimmer that covers its
    /// content rather than standing in for it is reached by these as well.
    /// </remarks>
    public string? Content { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the live region of the BitShimmer that carries its Label and LoadedLabel.
    /// </summary>
    /// <remarks>
    /// The region is rendered right after the root rather than inside it, so a selector that reaches it through
    /// the root does not apply, and one that counts or pairs the root's siblings sees it as well.
    /// <br />
    /// It follows the root's <c>Visibility</c>, <c>hidden</c>, <c>inert</c> and <c>aria-hidden</c>, but not a
    /// stylesheet: a shimmer hidden with a <c>display:none</c> in its style or behind a class hides the region
    /// through these as well, or the region still announces its text.
    /// </remarks>
    public string? Label { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the shimmer wrapper of the BitShimmer.
    /// </summary>
    /// <remarks>
    /// A multi-line shimmer draws one wrapper per line, so these are applied to each of them.
    /// </remarks>
    public string? ShimmerWrapper { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the shimmer of the BitShimmer.
    /// </summary>
    /// <remarks>
    /// This is the animated part inside each wrapper, which a placeholder set to
    /// <see cref="BitShimmerAnimation.None"/> does not draw.
    /// </remarks>
    public string? Shimmer { get; set; }
}

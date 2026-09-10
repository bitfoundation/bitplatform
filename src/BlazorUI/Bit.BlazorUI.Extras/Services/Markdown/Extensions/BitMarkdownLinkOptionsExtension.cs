namespace Bit.BlazorUI;

/// <summary>
/// Decides what <c>target</c> and <c>rel</c> a rendered link carries, replacing the defaults
/// (external links open in a new tab with <c>noopener noreferrer</c>).
/// </summary>
/// <remarks>
/// The two things this is usually for: keeping links in the same tab, because a viewer embedded in
/// an app is not a web page and a new tab is jarring there; and marking user-written links
/// <c>nofollow ugc</c>, which is what search engines expect of content a site did not write and
/// what stops a comment box from becoming a link farm.
/// </remarks>
public sealed class BitMarkdownLinkOptionsExtension : IBitMarkdownExtension
{
    /// <summary>Creates the extension with the policy to apply.</summary>
    /// <param name="externalTarget">Where a link to another origin opens. Defaults to a new tab.</param>
    /// <param name="externalRel">
    /// The <c>rel</c> for a link to another origin, or <c>null</c> for none. Defaults to
    /// <c>noopener noreferrer</c>; add <c>nofollow ugc</c> to it for user-written content.
    /// </param>
    /// <param name="internalTarget">Where a link within the document or the site opens. Defaults to the same tab.</param>
    /// <param name="internalRel">The <c>rel</c> for a link within the document or the site, or <c>null</c> for none.</param>
    public BitMarkdownLinkOptionsExtension(
        BitMarkdownLinkTarget externalTarget = BitMarkdownLinkTarget.Blank,
        string? externalRel = "noopener noreferrer",
        BitMarkdownLinkTarget internalTarget = BitMarkdownLinkTarget.Self,
        string? internalRel = null)
    {
        ExternalTarget = externalTarget;
        ExternalRel = externalRel;
        InternalTarget = internalTarget;
        InternalRel = internalRel;
    }

    /// <summary>Where a link to another origin opens.</summary>
    public BitMarkdownLinkTarget ExternalTarget { get; }

    /// <summary>The <c>rel</c> given to a link to another origin.</summary>
    public string? ExternalRel { get; }

    /// <summary>Where a link within the document or the site opens.</summary>
    public BitMarkdownLinkTarget InternalTarget { get; }

    /// <summary>The <c>rel</c> given to a link within the document or the site.</summary>
    public string? InternalRel { get; }

    public void Setup(BitMarkdownPipelineBuilder builder)
        => builder.Renderers.Add(new BitMarkdownLinkOptionsRenderer(
            ExternalTarget, ExternalRel, InternalTarget, InternalRel));
}

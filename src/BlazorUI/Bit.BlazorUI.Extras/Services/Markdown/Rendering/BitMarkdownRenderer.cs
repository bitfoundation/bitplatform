using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI;

/// <summary>
/// Walks an AST and dispatches each node to a matching <see cref="BitMarkdownNodeRenderer"/>.
/// Renderers are probed in reverse registration order, so the last renderer registered for a
/// node type wins, allowing pipeline extensions to override the core renderers.
/// </summary>
public sealed class BitMarkdownRenderer
{
    private readonly IReadOnlyList<BitMarkdownNodeRenderer> _renderers;

    /// <summary>Creates a renderer that writes its own words in English.</summary>
    public BitMarkdownRenderer(IReadOnlyList<BitMarkdownNodeRenderer> renderers)
        : this(renderers, BitMarkdownTexts.Default)
    {
    }

    /// <summary>Creates a renderer that writes its own words from <paramref name="texts"/>.</summary>
    public BitMarkdownRenderer(IReadOnlyList<BitMarkdownNodeRenderer> renderers, BitMarkdownTexts texts)
    {
        _renderers = renderers;
        Texts = texts ?? BitMarkdownTexts.Default;
    }

    /// <summary>
    /// The words the renderers write themselves - alert titles, footnote back-links, the accessible
    /// names of the regions and controls the markup adds. A renderer reads them from here rather
    /// than hard-coding English.
    /// </summary>
    public BitMarkdownTexts Texts { get; }

    /// <summary>
    /// The address of the page the output is placed in, without a fragment. When set, an in-page
    /// destination (<c>#id</c>) is written as this address followed by the fragment.
    /// </summary>
    /// <remarks>
    /// A bare <c>#id</c> is resolved against the document's <c>&lt;base href&gt;</c>, not against the
    /// page, so in an app whose base is <c>/</c> every footnote, permalink and in-page link on any
    /// other page would lead to the home page instead. Leave it <c>null</c> where the page is the
    /// base address itself, or where no <c>&lt;base&gt;</c> is in play.
    /// </remarks>
    public string? DocumentUrl { get; init; }

    /// <summary>
    /// When <c>true</c>, every paragraph, heading and list is written with <c>dir="auto"</c>, so each
    /// takes the direction of its own text - what a document mixing right-to-left and left-to-right
    /// languages needs, since one direction for the whole document lays out every block written in
    /// the other backwards. Defaults to <c>false</c>.
    /// </summary>
    public bool AutoDirection { get; init; }

    /// <summary>
    /// Returns the destination to write for <paramref name="url"/>: an in-page one (<c>#id</c>)
    /// prefixed with <see cref="DocumentUrl"/>, anything else unchanged.
    /// </summary>
    public string ResolveInPageUrl(string url)
    {
        if (string.IsNullOrEmpty(DocumentUrl) || string.IsNullOrEmpty(url) || url[0] != '#') return url;

        return DocumentUrl + url;
    }

    /// <summary>Renders a sequence of nodes.</summary>
    public void WriteNodes(RenderTreeBuilder builder, IEnumerable<BitMarkdownNode> nodes)
    {
        foreach (var node in nodes)
            WriteNode(builder, node);
    }

    /// <summary>Renders a single node using the matching renderer (last registered wins).</summary>
    public void WriteNode(RenderTreeBuilder builder, BitMarkdownNode node)
    {
        for (int i = _renderers.Count - 1; i >= 0; i--)
        {
            if (_renderers[i].Accept(node))
            {
                _renderers[i].Write(this, builder, node);
                return;
            }
        }

        throw new InvalidOperationException(
            $"No renderer registered for node type '{node.GetType().Name}'. " +
            "Register a BitMarkdownNodeRenderer for it via the pipeline builder.");
    }
}

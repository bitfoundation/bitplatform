namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.MarkdownViewer;

public partial class BitMarkdownViewerDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
           Name = "CodeBlockTemplate",
           Type = "RenderFragment<BitMarkdownCodeBlockNode>?",
           DefaultValue = "null",
           Description = "Renders every fenced or indented code block instead of the default pre/code - for a syntax highlighter, a copy button or a diagram renderer. The language is Info, the source Content. The output is outside the viewer's stylesheet.",
        },
        new()
        {
           Name = "HeadingLevelOffset",
           Type = "int",
           DefaultValue = "0",
           Description = "Shifts every heading down by this many levels (with 1, # renders as h2), clamped to h1-h6, so the document's outline nests under the page it is placed in.",
        },
        new()
        {
           Name = "HeadingIdPrefix",
           Type = "string?",
           DefaultValue = "null",
           Description = "Prepended to every heading id and to the in-page links that point at one, so documents sharing a page (chat messages, comments) never repeat an id, and untrusted {#id} headings cannot take one the page uses.",
        },
        new()
        {
           Name = "ImageRendering",
           Type = "BitMarkdownViewerImageRendering",
           DefaultValue = "BitMarkdownViewerImageRendering.SameOrigin",
           Description = "Which images may load. A remote image is fetched the moment it renders, leaking whatever its URL encodes; SameOrigin blocks cross-origin images, None blocks all, All is for trusted sources. A blocked image keeps its alt text.",
           LinkType = LinkType.Link,
           Href = "#markdown-viewer-image-rendering-enum",
        },
        new()
        {
           Name = "ImageTemplate",
           Type = "RenderFragment<BitMarkdownImageNode>?",
           DefaultValue = "null",
           Description = "Renders every image instead of the default img - for a lightbox or a placeholder. The ImageRendering policy has already run, so a blocked image arrives with an empty Url.",
        },
        new()
        {
           Name = "Inline",
           Type = "bool",
           DefaultValue = "false",
           Description = "Renders the root as a span and drops the paragraph wrappers, so a short piece of Markdown can sit inside a sentence, a cell or a label. Other blocks still render as themselves.",
        },
        new()
        {
           Name = "LinkTemplate",
           Type = "RenderFragment<BitMarkdownLinkNode>?",
           DefaultValue = "null",
           Description = "Renders every link instead of the default anchor - to route through the router or decorate external links. The URL is already sanitized; read the text with BitMarkdownInlineHelpers.PlainText(context.Children).",
        },
        new()
        {
           Name = "Markdown",
           Type = "string?",
           DefaultValue = "null",
           Description = "The Markdown source to render.",
        },
        new()
        {
           Name = "MaxLength",
           Type = "int",
           DefaultValue = "0",
           Description = "When greater than zero, the source is truncated to this many characters before parsing (never inside a surrogate pair). 0 means no limit.",
        },
        new()
        {
           Name = "MaxNestingDepth",
           Type = "int",
           DefaultValue = "100",
           Description = "The maximum block/inline nesting depth; deeper content renders as plain text. An always-on guard against stack exhaustion by hostile input; values <= 0 fall back to 100.",
        },
        new()
        {
           Name = "OnParsed",
           Type = "EventCallback<BitMarkdownDocumentNode>",
           DefaultValue = "",
           Description = "Raised after each parse with the document about to be rendered - to read a table of contents or front matter out of it, or rewrite it.",
           LinkType = LinkType.Link,
           Href = "#markdown-viewer-document-node",
        },
        new()
        {
           Name = "OnTaskChanged",
           Type = "EventCallback<BitMarkdownViewerTaskChangedEventArgs>",
           DefaultValue = "",
           Description = "Raised when a task-list checkbox is ticked, with the source rewritten to match. Setting it is what enables the checkboxes (unless the viewer is disabled); the viewer never changes Markdown itself.",
           LinkType = LinkType.Link,
           Href = "#markdown-viewer-task-changed-args",
        },
        new()
        {
           Name = "Pipeline",
           Type = "BitMarkdownPipeline?",
           DefaultValue = "null",
           Description = "The flavors the source is parsed with. Defaults to the CommonMark core; use BitMarkdownPipelines (Basic, GitHub, Advanced) or build one with BitMarkdownPipelineBuilder.",
           LinkType = LinkType.Link,
           Href = "#markdown-viewer-pipeline",
        },
        new()
        {
           Name = "StripBidiControlCharacters",
           Type = "bool",
           DefaultValue = "false",
           Description = "Strips the Unicode bidirectional control characters before parsing, neutralizing 'Trojan Source' (CVE-2021-42574) spoofing. Recommended for untrusted or AI-generated Markdown.",
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
                new() { Name = "Footnotes", Type = "string", DefaultValue = "Footnotes", Description = "The heading of the footnotes section, read by a screen reader only; each footnote reference is described by it." },
                new() { Name = "FootnoteBackReference", Type = "string", DefaultValue = "Back to reference {0}", Description = "The accessible name of a footnote's back-link, given the footnote's number." },
                new() { Name = "FootnoteBackReferenceOccurrence", Type = "string", DefaultValue = "Back to reference {0}-{1}", Description = "The accessible name of one of several back-links on the same footnote, given the footnote's number and the citation's." },
                new() { Name = "NewTab", Type = "string", DefaultValue = "(opens in a new tab)", Description = "Read out after the text of a link that opens in a new tab; not shown. An empty string leaves it out." },
                new() { Name = "CodeBlock", Type = "string", DefaultValue = "Code block", Description = "The accessible name of a code block, which scrolls and so is a tab stop." },
                new() { Name = "Table", Type = "string", DefaultValue = "Table", Description = "The accessible name of the scrollable region a table sits in." },
                new() { Name = "PermalinkTo", Type = "string", DefaultValue = "Permalink to {0}", Description = "The accessible name of a heading's permalink, given the heading's text." },
                new() { Name = "PermalinkToSection", Type = "string", DefaultValue = "Permalink to this section", Description = "The accessible name of a permalink whose heading has no text of its own." },
                new() { Name = "Task", Type = "string", DefaultValue = "Task {0}", Description = "The accessible name of an interactive task-list checkbox whose item has no text, given its number (a box is otherwise named after its item)." },
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
                    Name = "Texts",
                    Type = "BitMarkdownTexts",
                    DefaultValue = "",
                    Description = "The words the renderers write themselves.",
                    LinkType = LinkType.Link,
                    Href = "#markdown-viewer-texts",
                },
                new()
                {
                    Name = "DocumentUrl",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The page the output is placed in (init-only). When set, an in-page destination (#id) is written as this address plus the fragment, so a <base href> cannot send it to another page. The viewer sets it for you.",
                },
                new()
                {
                    Name = "ResolveInPageUrl",
                    Type = "string ResolveInPageUrl(string url)",
                    DefaultValue = "",
                    Description = "Returns an in-page destination prefixed with DocumentUrl, anything else unchanged. A custom node renderer writing an href should go through it.",
                },
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

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-MarkdownViewer-color", DefaultValue = "var(--bit-clr-fg-pri)", Description = "Text color of the document." },
        new() { Name = "--bit-MarkdownViewer-font-family", DefaultValue = "inherit", Description = "Font of the document." },
        new() { Name = "--bit-MarkdownViewer-font-size", DefaultValue = "inherit", Description = "Font size of the document; its headings and code spans scale with it." },
        new() { Name = "--bit-MarkdownViewer-line-height", DefaultValue = "1.6", Description = "Line height of the document." },
        new() { Name = "--bit-MarkdownViewer-block-spacing", DefaultValue = "1em", Description = "Room under each block (paragraph, list, table, code block, ...)." },
        new() { Name = "--bit-MarkdownViewer-scroll-margin", DefaultValue = "0", Description = "Room kept above a heading or a footnote an in-page link scrolls to - the height of a sticky app bar." },
        new() { Name = "--bit-MarkdownViewer-heading-color", DefaultValue = "inherit", Description = "Text color of the headings (h6 falls back to var(--bit-clr-fg-sec))." },
        new() { Name = "--bit-MarkdownViewer-heading-font-family", DefaultValue = "inherit", Description = "Font of the headings." },
        new() { Name = "--bit-MarkdownViewer-heading-font-weight", DefaultValue = "var(--bit-tpg-fw-semibold)", Description = "Weight of the headings." },
        new() { Name = "--bit-MarkdownViewer-heading-border-color", DefaultValue = "var(--bit-clr-brd-sec)", Description = "Rule under the h1 and h2 headings." },
        new() { Name = "--bit-MarkdownViewer-link-color", DefaultValue = "var(--bit-clr-pri-fg)", Description = "Text color of the links." },
        new() { Name = "--bit-MarkdownViewer-link-hover-color", DefaultValue = "var(--bit-MarkdownViewer-link-color)", Description = "Text color of a link under the pointer." },
        new() { Name = "--bit-MarkdownViewer-link-decoration", DefaultValue = "underline", Description = "text-decoration-line of the links at rest. Keep a non-color cue if you remove it (WCAG 1.4.1)." },
        new() { Name = "--bit-MarkdownViewer-code-font-family", DefaultValue = "var(--bit-tpg-font-family-mono)", Description = "Font of the code spans, code blocks and math." },
        new() { Name = "--bit-MarkdownViewer-code-color", DefaultValue = "inherit", Description = "Text color of the code spans." },
        new() { Name = "--bit-MarkdownViewer-code-background", DefaultValue = "color-mix(in srgb, var(--bit-clr-fg-pri) 8%, transparent)", Description = "Background of the code spans." },
        new() { Name = "--bit-MarkdownViewer-code-radius", DefaultValue = "var(--bit-shp-radius-control)", Description = "Corner radius of the code spans." },
        new() { Name = "--bit-MarkdownViewer-code-block-color", DefaultValue = "inherit", Description = "Text color of the code blocks." },
        new() { Name = "--bit-MarkdownViewer-code-block-background", DefaultValue = "color-mix(in srgb, var(--bit-clr-fg-pri) 5%, transparent)", Description = "Background of the code blocks." },
        new() { Name = "--bit-MarkdownViewer-code-block-padding", DefaultValue = "spacing(2)", Description = "Padding of the code blocks." },
        new() { Name = "--bit-MarkdownViewer-code-block-radius", DefaultValue = "var(--bit-shp-radius-surface)", Description = "Corner radius of the code blocks." },
        new() { Name = "--bit-MarkdownViewer-blockquote-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "Text color of the block quotes." },
        new() { Name = "--bit-MarkdownViewer-blockquote-border-color", DefaultValue = "var(--bit-clr-brd-pri)", Description = "Bar down the start edge of the block quotes." },
        new() { Name = "--bit-MarkdownViewer-table-border-color", DefaultValue = "var(--bit-clr-brd-pri)", Description = "Borders of the table cells." },
        new() { Name = "--bit-MarkdownViewer-table-header-background", DefaultValue = "transparent", Description = "Background of the header row." },
        new() { Name = "--bit-MarkdownViewer-table-stripe-background", DefaultValue = "color-mix(in srgb, var(--bit-clr-fg-pri) 4%, transparent)", Description = "Background of every second body row." },
        new() { Name = "--bit-MarkdownViewer-table-cell-padding", DefaultValue = "spacing(0.75) spacing(1.625)", Description = "Padding of the table cells." },
        new() { Name = "--bit-MarkdownViewer-rule-color", DefaultValue = "var(--bit-clr-brd-pri)", Description = "Color of the thematic break (hr) and of the rule above the footnotes." },
        new() { Name = "--bit-MarkdownViewer-rule-thickness", DefaultValue = "0.25em", Description = "Thickness of the thematic break (hr)." },
        new() { Name = "--bit-MarkdownViewer-mark-color", DefaultValue = "inherit", Description = "Text color of the highlighted (==mark==) text." },
        new() { Name = "--bit-MarkdownViewer-mark-background", DefaultValue = "color-mix(in srgb, var(--bit-clr-wrn) 35%, transparent)", Description = "Background of the highlighted (==mark==) text." },
        new() { Name = "--bit-MarkdownViewer-container-background", DefaultValue = "color-mix(in srgb, var(--bit-clr-fg-pri) 4%, transparent)", Description = "Background of the ::: containers." },
        new() { Name = "--bit-MarkdownViewer-note-color", DefaultValue = "var(--bit-clr-inf-fg)", Description = "Bar and title of the NOTE alerts and of the note/info containers." },
        new() { Name = "--bit-MarkdownViewer-tip-color", DefaultValue = "var(--bit-clr-suc-fg)", Description = "Bar and title of the TIP alerts and of the tip/success containers." },
        new() { Name = "--bit-MarkdownViewer-important-color", DefaultValue = "var(--bit-clr-pri-fg)", Description = "Bar and title of the IMPORTANT alerts." },
        new() { Name = "--bit-MarkdownViewer-warning-color", DefaultValue = "var(--bit-clr-wrn-fg)", Description = "Bar and title of the WARNING alerts and of the warning containers." },
        new() { Name = "--bit-MarkdownViewer-caution-color", DefaultValue = "var(--bit-clr-err-fg)", Description = "Bar and title of the CAUTION alerts and of the caution/danger containers." },
    ];



    private readonly string basicMarkdown = @"# Native Markdown in Blazor

Rendered entirely in **C#** with *no* JavaScript - see the [bit platform][bit] site.

- Real DOM output
- Safe by default
    1. URLs sanitized
    2. Raw HTML shown as text

> Character references decode: &copy; 2026 &mdash; but `&copy;` stays literal in code.

```csharp
var html = ""no innerHTML"";
```

[bit]: https://bitplatform.dev ""bit platform""
";



    private readonly string gitHubMarkdown = @"Supports ~~strikethrough~~ and bare links like https://bitplatform.dev,
plus footnotes[^1].

- [x] Parse Markdown in pure C#
- [ ] Use any JavaScript

| Feature       | Basic | GitHub |
|:--------------|:-----:|:------:|
| Headings      |   ✔   |   ✔    |
| Tables        |       |   ✔    |

> [!NOTE]
> Useful information that users should know, even when skimming.

> [!TIP]
> Helpful advice for doing things better.

> [!IMPORTANT]
> Key information users need to achieve their goal.

> [!WARNING]
> Urgent info that needs immediate attention.

> [!CAUTION]
> Advises about the risks of an action.

[^1]: Numbered in citation order and gathered at the end, each with a back-link.
";



    private readonly BitMarkdownPipeline customPipeline = new BitMarkdownPipelineBuilder()
        .UsePipeTables()
        .UseStrikethrough()
        .UseTaskLists()
        .UseEmojis()
        .Build();

    private readonly string customMarkdown = @"# Custom pipeline :sparkles:

Only pipe tables, strikethrough, task lists and emoji were picked.
Autolinks were left out, so https://bitplatform.dev stays plain text.

- [x] ~~Old~~ approach replaced
- [ ] Anything left to do?
";



    private readonly BitMarkdownPipeline typographyPipeline = new BitMarkdownPipelineBuilder()
        .UseEmphasisExtras()
        .UseSmartyPants()
        .Build();

    private readonly string typographyMarkdown = @"Water is H~2~O, the area of a circle is πr^2^, and this release ++adds streaming++
and ~~drops the old overload~~ - so ==read the migration notes== first.

""Typography matters,"" she said -- and it's hard to disagree... The 2024--2026 range
uses an en dash; an aside uses an em dash --- like this one.

Code keeps every character: `--- ""not curled"" ...` and 1 + 2 = 3 is plain prose.
";



    private readonly BitMarkdownPipeline softBreakPipeline = new BitMarkdownPipelineBuilder()
        .UseSoftLineAsHardLine()
        .Build();

    private readonly string lineBreaksMarkdown = @"Roses are red
Violets are blue
Markdown reflows
Unless you tell it not to";



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
---

# Release notes

The metadata above describes the file; it is not part of the document.
";



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
This one is a real `<details>`, so it opens and closes.
:::

:::glossary
A name the stylesheet has no opinion about is a plain block you style yourself.
:::
";



    private readonly BitMarkdownPipeline documentationPipeline = new BitMarkdownPipelineBuilder()
        .UseDefinitionLists()
        .UseAbbreviations()
        .UseFigures()
        .Build();

    private readonly string documentationMarkdown = @"*[AST]: Abstract Syntax Tree
*[HTML]: HyperText Markup Language

Pipeline
: The immutable set of flavors a document is parsed with.
: Build it once and share it.

AST
: The tree the parser produces and the renderer writes HTML from.

![The bit platform logo](/_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo-blue.png ""A titled image becomes a captioned figure"")
";



    private readonly BitMarkdownPipeline mathPipeline = new BitMarkdownPipelineBuilder()
        .UseMathematics()
        .Build();

    private readonly string mathMarkdown = @"Euler's identity, $e^{i\pi} + 1 = 0$, in one line.

$$
\int_0^1 x^2 \, dx = \frac{1}{3}
$$

Prices are left alone: this costs $5 and that one $10.
";



    private static readonly BitMarkdownViewerImageRendering[] imageRenderingModes =
    [
        BitMarkdownViewerImageRendering.SameOrigin,
        BitMarkdownViewerImageRendering.None,
        BitMarkdownViewerImageRendering.All
    ];

    private BitMarkdownViewerImageRendering imageRendering = BitMarkdownViewerImageRendering.SameOrigin;

    private readonly string untrustedMarkdown = @"A same-origin image loads unless the policy is `None`:

![the bit logo](/_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo-blue.png)

A cross-origin one only loads under `All`:

![a remote badge](https://img.shields.io/nuget/v/Bit.BlazorUI.Extras)

Unsafe URLs never survive the sanitizer, whatever the policy:
[a javascript link](javascript:alert(1)). Raw <b>HTML</b> and <script>alert(1)</script> are rendered as text.
";



    private record TocEntry(int Level, string Id, string Text);

    private List<TocEntry> tocEntries = [];

    private string tocExcerpt = string.Empty;

    private readonly BitMarkdownPipeline tocPipeline = new BitMarkdownPipelineBuilder()
        .UseAutoIdentifiers(anchorLinks: true)
        .Build();

    private void HandleTocParsed(BitMarkdownDocumentNode document)
    {
        tocEntries = BitMarkdownAstHelper.Descendants(document)
                                         .OfType<BitMarkdownHeadingNode>()
                                         .Where(h => string.IsNullOrEmpty(h.Id) is false)
                                         .Select(h => new TocEntry(h.Level, h.Id!, BitMarkdownInlineHelpers.PlainText(h.Inlines)))
                                         .ToList();

        var text = BitMarkdownAstHelper.ToPlainText(document).ReplaceLineEndings(" ");
        tocExcerpt = text.Length > 120 ? text[..120] + "..." : text;
    }

    private readonly string tocMarkdown = @"## Installation {#install}

Hover a heading for its permalink. This one names its own id, so links to it survive a rewording.

### .NET CLI

Run `dotnet add package Bit.BlazorUI.Extras`.

## Release notes

### Added

Footnotes, alerts and reference links - see [Installation](#install) first.
";



    private readonly BitMarkdownPipeline linkPolicyPipeline = new BitMarkdownPipelineBuilder()
        .UseLinkOptions(externalTarget: BitMarkdownLinkTarget.Self,
                        externalRel: "noopener noreferrer nofollow ugc")
        .Build();

    private readonly string linkPolicyMarkdown = @"A link a reader wrote to [somewhere else](https://example.com)
opens in the same tab and is marked `nofollow ugc`.

A link to [another page here](/components/markdowneditor) is untouched.
";

    private readonly BitMarkdownPipeline baseUrlPipeline = new BitMarkdownPipelineBuilder()
        .UseBaseUrl("/_content/Bit.BlazorUI.Demo.Client.Core/images/")
        .Build();

    private readonly string baseUrlMarkdown = @"![the bit logo](bit-logo-blue.png)

The image is written with a relative path, the way a README writes one.
";



    private readonly string templatesMarkdown = @"Every code block below is drawn by the template, not by the viewer:

```csharp
var pipeline = new BitMarkdownPipelineBuilder().UseGitHubFlavored().Build();
```

```bash
dotnet add package Bit.BlazorUI.Extras
```

And every link, like [the bit platform](https://bitplatform.dev), is a BitLink.
";

    [Inject] private IJSRuntime js { get; set; } = default!;

    private BitMarkdownCodeBlockNode? copiedBlock;

    private async Task CopyCodeAsync(BitMarkdownCodeBlockNode block)
    {
        try
        {
            await js.InvokeVoidAsync("navigator.clipboard.writeText", block.Content);
            copiedBlock = block;
        }
        catch (JSException)
        {
            // The clipboard can be denied (no permission, an insecure origin); the button just stays as it was.
        }
    }



    private bool tasksEnabled = true;

    private string taskListMarkdown = @"## Release checklist

- [x] Write the parser
- [ ] Write the docs
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



    private readonly BitMarkdownPipeline localizedPipeline = new BitMarkdownPipelineBuilder()
        .UseGitHubFlavored()
        .UseTexts(new BitMarkdownTexts
        {
            AlertNote = "Hinweis",
            AlertTip = "Tipp",
            AlertImportant = "Wichtig",
            AlertWarning = "Warnung",
            AlertCaution = "Vorsicht",
            Footnotes = "Fußnoten",
            FootnoteBackReference = "Zurück zur Referenz {0}",
            FootnoteBackReferenceOccurrence = "Zurück zur Referenz {0}-{1}",
            NewTab = "(öffnet in neuem Tab)",
            Table = "Tabelle",
            Task = "Aufgabe {0}",
        })
        .Build();

    private readonly string localizedMarkdown = @"> [!WARNING]
> Der Titel dieses Kastens kommt aus den Texten der Pipeline, nicht aus dem Dokument.

Auch die Tabelle, die Fußnote[^1] und der [externe Link](https://bitplatform.dev) tragen deutsche Namen.

| Spalte | Wert |
|:-------|-----:|
| eins   |    1 |

[^1]: Ihr Rücklink wird als „Zurück zur Referenz 1“ angesagt.
";



    private readonly string commentMarkdown = @"# Looks good to me

Tested on **Firefox** and **Safari**. One nit, under [Naming](#naming):

## Naming

`MaxLength` reads well, but the [docs](https://bitplatform.dev) should say it counts characters.

```csharp
var viewer = new BitMarkdownViewer { MaxLength = 100_000 };
```
";



    private enum MarkdownFlavor { Basic, GitHub, Advanced }

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
        MarkdownFlavor.Basic => "Basic: the CommonMark core only - tables, strikethrough, task lists, footnotes, alerts, emoji and bare URLs render as plain text.",
        MarkdownFlavor.GitHub => "GitHub: pipe tables, ~~strikethrough~~, task lists, autolink literals, footnotes and alerts.",
        _ => "Advanced: the GitHub flavors plus front matter, the emphasis extras, :::containers, definition lists, abbreviations, figures, :sparkles: emoji and heading ids."
    };

    private const string SampleMarkdown = """
        # BitMarkdownViewer

        A **native Blazor** Markdown viewer written in _pure C#_ - no JavaScript,
        no `innerHTML`, and ~~no external dependencies~~ zero external dependencies.

        ## Feature highlights

        - **Bold**, *italic*, ***bold italic***, and ~~strikethrough~~
        - H~2~O, x^2^, ++inserted++ and ==highlighted== (the emphasis extras)
        - [Links](https://learn.microsoft.com/aspnet/core/blazor) and images
        - Nested lists:
            1. First item
            2. Second item
                - nested bullet
        - Task lists:
            - [x] Parse blocks
            - [ ] Conquer the world

        ## Code

        ```csharp
        public static BitMarkdownDocumentNode Parse(string? markdown)
        {
            var document = new BitMarkdownDocumentNode();
            return document;
        }
        ```

        > [!TIP]
        > Switch the Flavor above to Basic and watch this become an ordinary block quote.

        | Feature        | Supported | Notes                  |
        | :------------- | :-------: | ---------------------: |
        | Tables         |    Yes    | With column alignment  |
        | Raw HTML       |    No     | Escaped for safety     |

        Reference links keep the prose clean[^why]: see the [bit platform][bit] site.

        [bit]: https://bitplatform.dev "bit platform"
        [^why]: The destination is declared once, at the bottom.

        Emoji :rocket: :tada:, bare URLs https://learn.microsoft.com and &copy; 2026.

        ---

        Made with C# and the Blazor render tree.
        """;



    private readonly BitMarkdownViewerParams[] markdownViewerParams =
    [
        new()
        {
            Pipeline = BitMarkdownPipelines.GitHub,
            HeadingLevelOffset = 2,
            ImageRendering = BitMarkdownViewerImageRendering.None,
        }
    ];

    private readonly string cascadingMarkdown = @"# Release 9.4

- [x] ~~Old~~ parser replaced
- [ ] Docs

| Flavor | Cascaded |
|--------|:--------:|
| GitHub |    ✔     |
";



    private readonly string cssVariablesStyle = @"--bit-MarkdownViewer-font-family: Georgia, 'Times New Roman', serif;
--bit-MarkdownViewer-line-height: 1.8;
--bit-MarkdownViewer-heading-color: #a855f7;
--bit-MarkdownViewer-heading-border-color: #a855f7;
--bit-MarkdownViewer-code-background: rgba(168, 85, 247, 0.15);
--bit-MarkdownViewer-blockquote-border-color: #a855f7;
--bit-MarkdownViewer-table-header-background: rgba(168, 85, 247, 0.15);
--bit-MarkdownViewer-table-stripe-background: transparent;";

    private readonly string cssVariablesMarkdown = @"## Restyled with variables

A serif body, purple headings and `tinted code`.

> A quote with a purple bar.

| Column | Value |
|--------|------:|
| one    |     1 |
| two    |     2 |
";



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

    private readonly string mixedDirectionMarkdown = @"## A comment thread

Each paragraph takes the direction of its own text.

این پاراگراف فارسی است و از راست به چپ چیده می‌شود.

- English item
";
}

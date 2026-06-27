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
           Type = "BitMarkdownViewerPipeline?",
           DefaultValue = "null",
           Description = @"The processing pipeline (flavor set). Defaults to the basic CommonMark core with no extensions.
                           Use one of the ready-made pipelines on BitMarkdownViewerPipelines (Basic, GitHub, Advanced)
                           or build a custom one with BitMarkdownViewerPipelineBuilder.",
           LinkType = LinkType.Link,
           Href = "#markdown-viewer-pipeline",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "markdown-viewer-pipeline",
            Title = "BitMarkdownViewerPipeline",
            Description = "An immutable, reusable Markdown processing configuration produced by a BitMarkdownViewerPipelineBuilder. Pipelines are thread-safe and should be cached and shared. Ready-made pipelines are available on BitMarkdownViewerPipelines (Basic, GitHub, Advanced).",
            Parameters =
            [
                new()
                {
                    Name = "Basic",
                    Type = "static BitMarkdownViewerPipeline",
                    DefaultValue = "",
                    Description = "A pipeline with only the basic CommonMark core (no flavors).",
                },
                new()
                {
                    Name = "Parse",
                    Type = "BitMarkdownViewerDocumentNode Parse(string? markdown)",
                    DefaultValue = "",
                    Description = "Parses Markdown source into an AST, applying all AST processors.",
                    LinkType = LinkType.Link,
                    Href = "#markdown-viewer-document-node",
                },
                new()
                {
                    Name = "CreateRenderer",
                    Type = "BitMarkdownViewerMarkdownRenderer CreateRenderer()",
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
            Title = "BitMarkdownViewerDocumentNode",
            Description = "The root of a parsed Markdown document. Inherits from BitMarkdownViewerMarkdownNode.",
            Parameters =
            [
                new()
                {
                    Name = "Children",
                    Type = "List<BitMarkdownViewerMarkdownNode>",
                    DefaultValue = "[]",
                    Description = "The top-level child nodes of the document.",
                    LinkType = LinkType.Link,
                    Href = "#markdown-viewer-node",
                },
                new()
                {
                    Name = "ChildNodes",
                    Type = "IList<BitMarkdownViewerMarkdownNode>",
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
            Title = "BitMarkdownViewerMarkdownNode",
            Description = "The abstract base type for every node produced by the parser. Nodes expose their mutable child collections so that AST processors (plugins) can traverse and rewrite the tree generically, even for node types they did not define.",
            Parameters =
            [
                new()
                {
                    Name = "ChildNodes",
                    Type = "virtual IList<BitMarkdownViewerMarkdownNode>?",
                    DefaultValue = "null",
                    Description = "The node's single child collection, if it has exactly one. Container nodes override this; leaf nodes return null.",
                },
                new()
                {
                    Name = "ChildLists",
                    Type = "virtual IEnumerable<IList<BitMarkdownViewerMarkdownNode>>",
                    DefaultValue = "",
                    Description = "All mutable child collections owned by this node. Defaults to the single ChildNodes collection; nodes with several (e.g. a table's cells) override this to expose each one.",
                },
            ]
        },
        new()
        {
            Id = "markdown-viewer-renderer",
            Title = "BitMarkdownViewerMarkdownRenderer",
            Description = "Walks an AST and dispatches each node to a matching node renderer. Renderers are probed in reverse registration order, so the last renderer registered for a node type wins, allowing pipeline extensions to override the core renderers.",
            Parameters =
            [
                new()
                {
                    Name = "WriteNodes",
                    Type = "void WriteNodes(RenderTreeBuilder builder, IEnumerable<BitMarkdownViewerMarkdownNode> nodes)",
                    DefaultValue = "",
                    Description = "Renders a sequence of nodes.",
                },
                new()
                {
                    Name = "WriteNode",
                    Type = "void WriteNode(RenderTreeBuilder builder, BitMarkdownViewerMarkdownNode node)",
                    DefaultValue = "",
                    Description = "Renders a single node using the matching renderer (last registered wins).",
                },
            ]
        },
    ];



    // -- Advanced (live editor) example --------------------------------------

    private MarkdownFlavor playgroundFlavor = MarkdownFlavor.Advanced;
    private BitMarkdownViewerPipeline playgroundPipeline = BitMarkdownViewerPipelines.Advanced;
    private string playgroundMarkdown = SampleMarkdown;

    private void SetPlaygroundFlavor(MarkdownFlavor flavor)
    {
        playgroundFlavor = flavor;
        playgroundPipeline = flavor switch
        {
            MarkdownFlavor.Basic => BitMarkdownViewerPipelines.Basic,
            MarkdownFlavor.GitHub => BitMarkdownViewerPipelines.GitHub,
            _ => BitMarkdownViewerPipelines.Advanced
        };
    }

    private void ResetPlaygroundSample() => playgroundMarkdown = SampleMarkdown;

    private string playgroundHint => playgroundFlavor switch
    {
        MarkdownFlavor.Basic => "Basic CommonMark only. Tables, strikethrough, task lists, emoji and bare URLs render as plain text.",
        MarkdownFlavor.GitHub => "GitHub Flavored Markdown: pipe tables, ~~strikethrough~~, task lists and autolink literals.",
        _ => "Advanced: GitHub Flavored Markdown plus :sparkles: emoji and automatic heading ids."
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
        public static BitMarkdownViewerDocumentNode Parse(string? markdown)
        {
            var document = new BitMarkdownViewerDocumentNode();
            if (string.IsNullOrEmpty(markdown))
                return document;
            return document;
        }
        ```

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

        ## Safety

        Link and image URLs are sanitized, so `javascript:` URIs are stripped and raw
        HTML in the source is rendered as text rather than executed.

        ## Plugins (try the Flavor switch above)

        With the **Advanced** flavor you also get emoji and autolinks:

        - Emoji shortcodes: :rocket: :sparkles: :tada: :fire: :+1:
        - Bare URLs become links: https://learn.microsoft.com
        - Email autolinks: support@example.com

        Switch to **Basic** to see the same source rendered as plain CommonMark.

        ---

        Made with C# and the Blazor render tree.
        """;



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



    // -- Custom pipeline example ---------------------------------------------

    private readonly BitMarkdownViewerPipeline customPipeline = new BitMarkdownViewerPipelineBuilder()
        .UsePipeTables()
        .UseStrikethrough()
        .UseTaskLists()
        .UseEmojis()
        .UseAutoIdentifiers()
        .Build();

    private readonly string customMarkdown = @"# Custom pipeline :sparkles:

This viewer uses a pipeline composed with only the extensions we picked:
pipe tables, strikethrough, task lists, emoji and auto identifiers.

- [x] ~~Old~~ approach replaced
- [ ] Anything left to do?
";



    private readonly string example1RazorCode = @"
<BitMarkdownViewer Markdown=""@(""# Native Markdown in Blazor\n\nRendered entirely in **C#** with no JavaScript and no third-party packages.\n\n- Real DOM output\n- Safe by default\n- Zero interop"")"" />";

    private readonly string example2RazorCode = @"
<BitMarkdownViewer Markdown=""@gitHubMarkdown"" Pipeline=""BitMarkdownViewerPipelines.GitHub"" />";
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
            <BitMarkdownViewer Markdown=""@playgroundMarkdown"" Pipeline=""@playgroundPipeline"" />
        </div>
    </div>
</div>";
    private readonly string example3CsharpCode = @"
private enum MarkdownFlavor { Basic, GitHub, Advanced }

private MarkdownFlavor playgroundFlavor = MarkdownFlavor.Advanced;
private BitMarkdownViewerPipeline playgroundPipeline = BitMarkdownViewerPipelines.Advanced;
private string playgroundMarkdown = SampleMarkdown; // a feature-rich sample document

private void SetPlaygroundFlavor(MarkdownFlavor flavor)
{
    playgroundFlavor = flavor;
    playgroundPipeline = flavor switch
    {
        MarkdownFlavor.Basic => BitMarkdownViewerPipelines.Basic,
        MarkdownFlavor.GitHub => BitMarkdownViewerPipelines.GitHub,
        _ => BitMarkdownViewerPipelines.Advanced
    };
}

private void ResetPlaygroundSample() => playgroundMarkdown = SampleMarkdown;

private string playgroundHint => playgroundFlavor switch
{
    MarkdownFlavor.Basic => ""Basic CommonMark only. Tables, strikethrough, task lists, emoji and bare URLs render as plain text."",
    MarkdownFlavor.GitHub => ""GitHub Flavored Markdown: pipe tables, ~~strikethrough~~, task lists and autolink literals."",
    _ => ""Advanced: GitHub Flavored Markdown plus :sparkles: emoji and automatic heading ids.""
};

private const string SampleMarkdown = ""# BitMarkdownViewer\n\nA **native Blazor** Markdown viewer written in _pure C#_ - no JavaScript.\n\n- Headings, **bold**, *italic*, ~~strikethrough~~\n- `inline code` and fenced code blocks\n- [Links](https://bitplatform.dev) and task lists:\n    - [x] Parse blocks\n    - [ ] Conquer the world\n\n> Switch the Flavor above to compare rendering."";";

    private readonly string example4RazorCode = @"
<BitMarkdownViewer Markdown=""@customMarkdown"" Pipeline=""customPipeline"" />";
    private readonly string example4CsharpCode = @"
private readonly BitMarkdownViewerPipeline customPipeline = new BitMarkdownViewerPipelineBuilder()
    .UsePipeTables()
    .UseStrikethrough()
    .UseTaskLists()
    .UseEmojis()
    .UseAutoIdentifiers()
    .Build();

private readonly string customMarkdown = @""# Custom pipeline :sparkles:

This viewer uses a pipeline composed with only the extensions we picked:
pipe tables, strikethrough, task lists, emoji and auto identifiers.

- [x] ~~Old~~ approach replaced
- [ ] Anything left to do?
"";";
}

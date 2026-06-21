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
        },
    ];



    // -- Advanced (live editor) example --------------------------------------

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
        public static DocumentNode Parse(string? markdown)
        {
            var document = new DocumentNode();
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

- [x] ~~Old~~ approach replaced
- [ ] Anything left to do?
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
<div class=""mdv-toolbar"">
    <span class=""mdv-label"">Flavor:</span>
    <BitButton Size=""BitSize.Small""
               Variant=""@(playgroundFlavor == MarkdownFlavor.Basic ? BitVariant.Fill : BitVariant.Outline)""
               OnClick=""@(() => SetPlaygroundFlavor(MarkdownFlavor.Basic))"">Basic</BitButton>
    <BitButton Size=""BitSize.Small""
               Variant=""@(playgroundFlavor == MarkdownFlavor.GitHub ? BitVariant.Fill : BitVariant.Outline)""
               OnClick=""@(() => SetPlaygroundFlavor(MarkdownFlavor.GitHub))"">GitHub</BitButton>
    <BitButton Size=""BitSize.Small""
               Variant=""@(playgroundFlavor == MarkdownFlavor.Advanced ? BitVariant.Fill : BitVariant.Outline)""
               OnClick=""@(() => SetPlaygroundFlavor(MarkdownFlavor.Advanced))"">Advanced</BitButton>
</div>
<div class=""mdv-split"">
    <textarea class=""mdv-editor"" @bind=""playgroundMarkdown"" @bind:event=""oninput""></textarea>
    <div class=""mdv-preview"">
        <BitMarkdownViewer Markdown=""@playgroundMarkdown"" Pipeline=""@playgroundPipeline"" />
    </div>
</div>";
    private readonly string example3CsharpCode = @"
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
}";

    private readonly string example4RazorCode = @"
<BitMarkdownViewer Markdown=""@customMarkdown"" Pipeline=""customPipeline"" />";
    private readonly string example4CsharpCode = @"
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

- [x] ~~Old~~ approach replaced
- [ ] Anything left to do?
"";";
}

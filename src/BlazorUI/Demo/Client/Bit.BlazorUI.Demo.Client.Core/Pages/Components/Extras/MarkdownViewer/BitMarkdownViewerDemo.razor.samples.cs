namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.MarkdownViewer;

public partial class BitMarkdownViewerDemo
{
    private readonly string example1RazorCode = @"
<BitMarkdownViewer Markdown=""@basicMarkdown"" />";

    private readonly string example1CsharpCode = @"
private readonly string basicMarkdown = @""# Native Markdown in Blazor

Rendered entirely in **C#** with *no* JavaScript - see the [bit platform][bit] site.

- Real DOM output
- Safe by default
    1. URLs sanitized
    2. Raw HTML shown as text

> Character references decode: &copy; 2026 &mdash; but `&copy;` stays literal in code.

```csharp
var html = """"no innerHTML"""";
```

[bit]: https://bitplatform.dev """"bit platform""""
"";";

    private readonly string example2RazorCode = @"
<BitMarkdownViewer Markdown=""@gitHubMarkdown"" Pipeline=""BitMarkdownPipelines.GitHub"" />";

    private readonly string example2CsharpCode = @"
private readonly string gitHubMarkdown = @""Supports ~~strikethrough~~ and bare links like https://bitplatform.dev,
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
"";";

    private readonly string example3RazorCode = @"
<BitMarkdownViewer Markdown=""@customMarkdown"" Pipeline=""customPipeline"" />";

    private readonly string example3CsharpCode = @"
private readonly BitMarkdownPipeline customPipeline = new BitMarkdownPipelineBuilder()
    .UsePipeTables()
    .UseStrikethrough()
    .UseTaskLists()
    .UseEmojis()
    .Build();

private readonly string customMarkdown = @""# Custom pipeline :sparkles:

Only pipe tables, strikethrough, task lists and emoji were picked.
Autolinks were left out, so https://bitplatform.dev stays plain text.

- [x] ~~Old~~ approach replaced
- [ ] Anything left to do?
"";";

    private readonly string example4RazorCode = @"
<BitMarkdownViewer Markdown=""@typographyMarkdown"" Pipeline=""@typographyPipeline"" />";

    private readonly string example4CsharpCode = @"
private readonly BitMarkdownPipeline typographyPipeline = new BitMarkdownPipelineBuilder()
    .UseEmphasisExtras()
    .UseSmartyPants()
    .Build();

private readonly string typographyMarkdown = @""Water is H~2~O, the area of a circle is πr^2^, and this release ++adds streaming++
and ~~drops the old overload~~ - so ==read the migration notes== first.

""""Typography matters,"""" she said -- and it's hard to disagree... The 2024--2026 range
uses an en dash; an aside uses an em dash --- like this one.

Code keeps every character: `--- """"not curled"""" ...` and 1 + 2 = 3 is plain prose.
"";";

    private readonly string example5RazorCode = @"
<BitMarkdownViewer Markdown=""@lineBreaksMarkdown"" />

<BitMarkdownViewer Markdown=""@lineBreaksMarkdown"" Pipeline=""@softBreakPipeline"" />";

    private readonly string example5CsharpCode = @"
private readonly BitMarkdownPipeline softBreakPipeline = new BitMarkdownPipelineBuilder()
    .UseSoftLineAsHardLine()
    .Build();

private readonly string lineBreaksMarkdown = @""Roses are red
Violets are blue
Markdown reflows
Unless you tell it not to"";";

    private readonly string example6RazorCode = @"
<BitMarkdownViewer Markdown=""@frontMatterMarkdown"" />

<BitMarkdownViewer Markdown=""@frontMatterMarkdown"" Pipeline=""@frontMatterPipeline"" OnParsed=""HandleFrontMatterParsed"" />
<div>Metadata read from the AST: @frontMatterText</div>";

    private readonly string example6CsharpCode = @"
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
---

# Release notes

The metadata above describes the file; it is not part of the document.
"";";

    private readonly string example7RazorCode = @"
<BitMarkdownViewer Markdown=""@containersMarkdown"" Pipeline=""@containersPipeline"" />";

    private readonly string example7CsharpCode = @"
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
This one is a real `<details>`, so it opens and closes.
:::

:::glossary
A name the stylesheet has no opinion about is a plain block you style yourself.
:::
"";";

    private readonly string example8RazorCode = @"
<BitMarkdownViewer Markdown=""@documentationMarkdown"" Pipeline=""@documentationPipeline"" />";

    private readonly string example8CsharpCode = @"
private readonly BitMarkdownPipeline documentationPipeline = new BitMarkdownPipelineBuilder()
    .UseDefinitionLists()
    .UseAbbreviations()
    .UseFigures()
    .Build();

private readonly string documentationMarkdown = @""*[AST]: Abstract Syntax Tree
*[HTML]: HyperText Markup Language

Pipeline
: The immutable set of flavors a document is parsed with.
: Build it once and share it.

AST
: The tree the parser produces and the renderer writes HTML from.

![The bit platform logo](/_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo-blue.png """"A titled image becomes a captioned figure"""")
"";";

    private readonly string example9RazorCode = @"
<BitMarkdownViewer Markdown=""@mathMarkdown"" Pipeline=""@mathPipeline"" />

<KatexTypesetter>
    <BitMarkdownViewer Markdown=""@mathMarkdown"" Pipeline=""@mathPipeline"" />
</KatexTypesetter>";

    private readonly string example9CsharpCode = @"
private readonly BitMarkdownPipeline mathPipeline = new BitMarkdownPipelineBuilder()
    .UseMathematics()
    .Build();

private readonly string mathMarkdown = @""Euler's identity, $e^{i\pi} + 1 = 0$, in one line.

$$
\int_0^1 x^2 \, dx = \frac{1}{3}
$$

Prices are left alone: this costs $5 and that one $10.
"";";

    private readonly string example9TypesetterCode = @"
@* Typesets the TeX the viewer leaves in place, once it has rendered it. *@
@inject IJSRuntime JSRuntime

<div @ref=""host"">@ChildContent</div>

@code {
    private ElementReference host;

    [Parameter] public RenderFragment? ChildContent { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // The script skips what it has already typeset, so only math drawn since is touched.
        await JSRuntime.InvokeVoidAsync(""typesetMath"", host);
    }
}";

    private readonly string example9ScriptCode = @"
// Loaded by a <script> tag after Blazor's own. KaTeX is fetched the first time math is typeset.
const katexBaseUrl = 'https://cdnjs.cloudflare.com/ajax/libs/KaTeX/0.18.6/';
let katexLoading;

function loadKatex() {
    katexLoading ??= new Promise((resolve, reject) => {
        const link = document.createElement('link');
        link.rel = 'stylesheet';
        link.href = katexBaseUrl + 'katex.min.css';
        document.head.appendChild(link);

        const script = document.createElement('script');
        script.src = katexBaseUrl + 'katex.min.js';
        script.onload = () => resolve();
        script.onerror = () => {
            katexLoading = undefined;
            reject(new Error('KaTeX could not be loaded.'));
        };
        document.head.appendChild(script);
    });

    return katexLoading;
}

// The class is read rather than the delimiters: KaTeX's auto-render does not take a single $ as
// one by default, and the viewer has already told math from prices.
async function typesetMath(element) {
    if (element == null) return;

    try {
        await loadKatex();
    } catch {
        return; // with no typesetter the TeX still reads as itself
    }

    element.querySelectorAll('.math:not([data-typeset])').forEach(math => {
        const display = math.classList.contains('math-display');
        const delimiter = display ? 2 : 1;
        const tex = (math.textContent ?? '').slice(delimiter, -delimiter);

        katex.render(tex, math, { displayMode: display, throwOnError: false });
        math.dataset.typeset = '';
    });
}";

    private readonly DemoCodeFile[] example9CodeFiles;

    private readonly string example10RazorCode = @"
@foreach (var mode in imageRenderingModes)
{
    <BitButton Size=""BitSize.Small""
               aria-pressed=""@(imageRendering == mode ? ""true"" : ""false"")""
               Variant=""@(imageRendering == mode ? BitVariant.Fill : BitVariant.Outline)""
               OnClick=""@(() => imageRendering = mode)"">@mode</BitButton>
}

<BitMarkdownViewer Markdown=""@untrustedMarkdown""
                   Pipeline=""BitMarkdownPipelines.GitHub""
                   ImageRendering=""@imageRendering""
                   StripBidiControlCharacters=""true""
                   MaxLength=""100000"" />";

    private readonly string example10CsharpCode = @"
private static readonly BitMarkdownViewerImageRendering[] imageRenderingModes =
[
    BitMarkdownViewerImageRendering.SameOrigin,
    BitMarkdownViewerImageRendering.None,
    BitMarkdownViewerImageRendering.All
];

private BitMarkdownViewerImageRendering imageRendering = BitMarkdownViewerImageRendering.SameOrigin;

private readonly string untrustedMarkdown = @""A same-origin image loads unless the policy is `None`:

![the bit logo](/_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo-blue.png)

A cross-origin one only loads under `All`:

![a remote badge](https://img.shields.io/nuget/v/Bit.BlazorUI.Extras)

Unsafe URLs never survive the sanitizer, whatever the policy:
[a javascript link](javascript:alert(1)). Raw <b>HTML</b> and <script>alert(1)</script> are rendered as text.
"";";

    private readonly string example11RazorCode = @"
<style>
    .toc-layout {
        display: flex;
        gap: 1.5rem;
    }

    .toc {
        display: flex;
        flex-direction: column;
        flex: 0 0 12rem;
    }
</style>

<div class=""toc-layout"">
    <nav class=""toc"" aria-label=""On this page"">
        @foreach (var entry in tocEntries)
        {
            @* A BitLink scrolls to an in-page target; a bare <a href=""#id""> would follow <base href> off the page. *@
            <BitLink Href=""@($""#{entry.Id}"")"" Style=""@($""padding-inline-start:{(entry.Level - 2) * 0.75}rem"")"">@entry.Text</BitLink>
        }
    </nav>
    <BitMarkdownViewer Markdown=""@tocMarkdown"" Pipeline=""@tocPipeline"" OnParsed=""HandleTocParsed"" />
</div>
<div>Excerpt: @tocExcerpt</div>";

    private readonly string example11CsharpCode = @"
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

    var text = BitMarkdownAstHelper.ToPlainText(document).ReplaceLineEndings("" "");
    tocExcerpt = text.Length > 120 ? text[..120] + ""..."" : text;
}

private readonly string tocMarkdown = @""## Installation {#install}

Hover a heading for its permalink. This one names its own id, so links to it survive a rewording.

### .NET CLI

Run `dotnet add package Bit.BlazorUI.Extras`.

## Release notes

### Added

Footnotes, alerts and reference links - see [Installation](#install) first.
"";";

    private readonly string example12RazorCode = @"
<BitMarkdownViewer Markdown=""@linkPolicyMarkdown"" Pipeline=""@linkPolicyPipeline"" />

<BitMarkdownViewer Markdown=""@baseUrlMarkdown"" Pipeline=""@baseUrlPipeline"" />";

    private readonly string example12CsharpCode = @"
private readonly BitMarkdownPipeline linkPolicyPipeline = new BitMarkdownPipelineBuilder()
    .UseLinkOptions(externalTarget: BitMarkdownLinkTarget.Self,
                    externalRel: ""noopener noreferrer nofollow ugc"")
    .Build();

private readonly string linkPolicyMarkdown = @""A link a reader wrote to [somewhere else](https://example.com)
opens in the same tab and is marked `nofollow ugc`.

A link to [another page here](/components/markdowneditor) is untouched.
"";

private readonly BitMarkdownPipeline baseUrlPipeline = new BitMarkdownPipelineBuilder()
    .UseBaseUrl(""/_content/Bit.BlazorUI.Demo.Client.Core/images/"")
    .Build();

private readonly string baseUrlMarkdown = @""![the bit logo](bit-logo-blue.png)

The image is written with a relative path, the way a README writes one.
"";";

    private readonly string example13RazorCode = @"
<div>
    Formatting a value in place:
    <BitMarkdownViewer Inline Markdown=""@(""the **fastest** path is `Span<T>` - [read why](https://learn.microsoft.com/dotnet/api/system.span-1)"")"" />
</div>";

    private readonly string example14RazorCode = @"
<BitMarkdownViewer Markdown=""@templatesMarkdown"" Pipeline=""BitMarkdownPipelines.GitHub"">
    <CodeBlockTemplate>
        <div class=""mdv-code-card"">
            <div class=""mdv-code-card-head"">
                <span>@(context.Info ?? ""text"")</span>
                <BitButton Size=""BitSize.Small"" Variant=""BitVariant.Text"" IconName=""@BitIconName.Copy"" Title=""Copy"" AriaLabel=""Copy"" />
            </div>
            <pre tabindex=""0""><code>@context.Content</code></pre>
        </div>
    </CodeBlockTemplate>
    <LinkTemplate>
        <BitLink Href=""@context.Url"" Target=""_blank"">
            @BitMarkdownInlineHelpers.PlainText(context.Children)
            <BitIcon IconName=""@BitIconName.NavigateExternalInline"" AriaLabel=""opens in a new tab"" />
        </BitLink>
    </LinkTemplate>
</BitMarkdownViewer>";

    private readonly string example14CsharpCode = @"
private readonly string templatesMarkdown = @""Every code block below is drawn by the template, not by the viewer:

```csharp
var pipeline = new BitMarkdownPipelineBuilder().UseGitHubFlavored().Build();
```

```bash
dotnet add package Bit.BlazorUI.Extras
```

And every link, like [the bit platform](https://bitplatform.dev), is a BitLink.
"";";

    private readonly string example14ScssCode = @"
// The template's output is outside the viewer's stylesheet, so its chrome - and its code block - is styled here.
::deep .mdv-code-card {
    margin-bottom: 1rem;
    overflow: hidden;
    border-radius: 0.5rem;
    border: 1px solid $bit-color-border-secondary;
}

::deep .mdv-code-card-head {
    display: flex;
    gap: 0.5rem;
    align-items: center;
    justify-content: space-between;
    padding: 0.25rem 0.25rem 0.25rem 0.75rem;
    font-family: var(--bit-tpg-font-family-mono);
    font-size: 0.75rem;
    color: $bit-color-foreground-secondary;
    background: $bit-color-background-secondary;
}

::deep .mdv-code-card pre {
    margin: 0;
    padding: 1rem;
    overflow: auto;
    font-size: 0.85em;
    font-family: var(--bit-tpg-font-family-mono);
}";

    private readonly DemoCodeFile[] example14CodeFiles;

    private readonly string example15RazorCode = @"
<BitCheckbox Label=""IsEnabled"" @bind-Value=""tasksEnabled"" />

<BitMarkdownViewer Markdown=""@taskListMarkdown""
                   Pipeline=""BitMarkdownPipelines.GitHub""
                   IsEnabled=""tasksEnabled""
                   OnTaskChanged=""HandleTaskChanged"" />
<div>@taskListStatus</div>";

    private readonly string example15CsharpCode = @"
private bool tasksEnabled = true;

private string taskListMarkdown = @""## Release checklist

- [x] Write the parser
- [ ] Write the docs
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

    private readonly string example16RazorCode = @"
<BitMarkdownViewer Markdown=""@localizedMarkdown"" Pipeline=""@localizedPipeline"" />";

    private readonly string example16CsharpCode = @"
private readonly BitMarkdownPipeline localizedPipeline = new BitMarkdownPipelineBuilder()
    .UseGitHubFlavored()
    .UseTexts(new BitMarkdownTexts
    {
        AlertNote = ""Hinweis"",
        AlertTip = ""Tipp"",
        AlertImportant = ""Wichtig"",
        AlertWarning = ""Warnung"",
        AlertCaution = ""Vorsicht"",
        Footnotes = ""Fußnoten"",
        FootnoteBackReference = ""Zurück zur Referenz {0}"",
        FootnoteBackReferenceOccurrence = ""Zurück zur Referenz {0}-{1}"",
        NewTab = ""(öffnet in neuem Tab)"",
        Table = ""Tabelle"",
        Task = ""Aufgabe {0}"",
    })
    .Build();

private readonly string localizedMarkdown = @""> [!WARNING]
> Der Titel dieses Kastens kommt aus den Texten der Pipeline, nicht aus dem Dokument.

Auch die Tabelle, die Fußnote[^1] und der [externe Link](https://bitplatform.dev) tragen deutsche Namen.

| Spalte | Wert |
|:-------|-----:|
| eins   |    1 |

[^1]: Ihr Rücklink wird als „Zurück zur Referenz 1“ angesagt.
"";";

    private readonly string example17RazorCode = @"
<div class=""comment"">
    <h3>Comment by Sam</h3>
    <BitMarkdownViewer Markdown=""@commentMarkdown""
                       Pipeline=""BitMarkdownPipelines.Advanced""
                       HeadingLevelOffset=""3""
                       HeadingIdPrefix=""comment-42-""
                       AriaLabel=""Comment by Sam"" />
</div>";

    private readonly string example17CsharpCode = @"
private readonly string commentMarkdown = @""# Looks good to me

Tested on **Firefox** and **Safari**. One nit, under [Naming](#naming):

## Naming

`MaxLength` reads well, but the [docs](https://bitplatform.dev) should say it counts characters.

```csharp
var viewer = new BitMarkdownViewer { MaxLength = 100_000 };
```
"";";

    private readonly string example18RazorCode = @"
@foreach (var flavor in Enum.GetValues<MarkdownFlavor>())
{
    <BitButton Size=""BitSize.Small""
               aria-pressed=""@(playgroundFlavor == flavor ? ""true"" : ""false"")""
               Variant=""@(playgroundFlavor == flavor ? BitVariant.Fill : BitVariant.Outline)""
               OnClick=""@(() => SetPlaygroundFlavor(flavor))"">@flavor</BitButton>
}
<BitButton Size=""BitSize.Small"" Variant=""BitVariant.Text"" OnClick=""ResetPlaygroundSample"">Reset sample</BitButton>
<BitButton Size=""BitSize.Small"" Variant=""BitVariant.Text"" OnClick=""@(() => playgroundMarkdown = string.Empty)"">Clear</BitButton>

<div>@playgroundHint</div>

<div style=""display:flex;gap:1rem"">
    <textarea spellcheck=""false"" aria-label=""Markdown source"" style=""flex:1;height:500px"" @bind=""playgroundMarkdown"" @bind:event=""oninput""></textarea>
    <div style=""flex:1;height:500px;overflow:auto"">
        <BitMarkdownViewer Markdown=""@playgroundMarkdown""
                           Pipeline=""@playgroundPipeline""
                           AriaLabel=""Preview""
                           StripBidiControlCharacters=""true""
                           MaxLength=""100000"" />
    </div>
</div>";

    private readonly string example18CsharpCode = @"
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
    MarkdownFlavor.Basic => ""Basic: the CommonMark core only - tables, strikethrough, task lists, footnotes, alerts, emoji and bare URLs render as plain text."",
    MarkdownFlavor.GitHub => ""GitHub: pipe tables, ~~strikethrough~~, task lists, autolink literals, footnotes and alerts."",
    _ => ""Advanced: the GitHub flavors plus front matter, the emphasis extras, :::containers, definition lists, abbreviations, figures, :sparkles: emoji and heading ids.""
};

private const string SampleMarkdown = """"""
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

    [bit]: https://bitplatform.dev ""bit platform""
    [^why]: The destination is declared once, at the bottom.

    Emoji :rocket: :tada:, bare URLs https://learn.microsoft.com and &copy; 2026.

    ---

    Made with C# and the Blazor render tree.
    """""";";

    private readonly string example19RazorCode = @"
<BitParams Parameters=""markdownViewerParams"">
    <BitMarkdownViewer Markdown=""@cascadingMarkdown"" />

    <BitMarkdownViewer Markdown=""@cascadingMarkdown"" Pipeline=""BitMarkdownPipelines.Basic"" />
</BitParams>";

    private readonly string example19CsharpCode = @"
private readonly BitMarkdownViewerParams[] markdownViewerParams =
[
    new()
    {
        Pipeline = BitMarkdownPipelines.GitHub,
        HeadingLevelOffset = 2,
        ImageRendering = BitMarkdownViewerImageRendering.None,
    }
];

private readonly string cascadingMarkdown = @""# Release 9.4

- [x] ~~Old~~ parser replaced
- [ ] Docs

| Flavor | Cascaded |
|--------|:--------:|
| GitHub |    ✔     |
"";";

    private readonly string example20RazorCode = @"
<style>
    .custom-mdv {
        padding: 1rem;
        border-radius: 0.5rem;
        background: var(--bit-clr-bg-sec);
    }

    .custom-mdv h3 {
        margin-top: 0;
        color: var(--bit-clr-pri);
    }

    .custom-mdv code {
        color: var(--bit-clr-pri-dark);
        background: var(--bit-clr-pri-light);
    }
</style>

<BitMarkdownViewer Style=""border-inline-start:0.25rem solid var(--bit-clr-pri);padding-inline-start:1rem""
                   Markdown=""@(""A **styled** viewer, set apart with an inline `Style`."")"" />

<BitMarkdownViewer Class=""custom-mdv""
                   Markdown=""@(""### A classy viewer\n\nEvery `code` span and heading inside it is restyled from the page's own stylesheet."")"" />


<BitMarkdownViewer Style=""@cssVariablesStyle"" Markdown=""@cssVariablesMarkdown"" Pipeline=""BitMarkdownPipelines.GitHub"" />";

    private readonly string example20CsharpCode = @"
private readonly string cssVariablesStyle = @""--bit-MarkdownViewer-font-family: Georgia, 'Times New Roman', serif;
--bit-MarkdownViewer-line-height: 1.8;
--bit-MarkdownViewer-heading-color: #a855f7;
--bit-MarkdownViewer-heading-border-color: #a855f7;
--bit-MarkdownViewer-code-background: rgba(168, 85, 247, 0.15);
--bit-MarkdownViewer-blockquote-border-color: #a855f7;
--bit-MarkdownViewer-table-header-background: rgba(168, 85, 247, 0.15);
--bit-MarkdownViewer-table-stripe-background: transparent;"";

private readonly string cssVariablesMarkdown = @""## Restyled with variables

A serif body, purple headings and `tinted code`.

> A quote with a purple bar.

| Column | Value |
|--------|------:|
| one    |     1 |
| two    |     2 |
"";";

    private readonly string example21RazorCode = @"
<BitMarkdownViewer Dir=""BitDir.Rtl"" Markdown=""@rtlMarkdown"" Pipeline=""BitMarkdownPipelines.Advanced"" />

<BitMarkdownViewer Dir=""BitDir.Auto"" Markdown=""@mixedDirectionMarkdown"" />";

    private readonly string example21CsharpCode = @"
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
"";

private readonly string mixedDirectionMarkdown = @""## A comment thread

Each paragraph takes the direction of its own text.

این پاراگراف فارسی است و از راست به چپ چیده می‌شود.

- English item
"";";

    public BitMarkdownViewerDemo()
    {
        example9CodeFiles =
        [
            new("KatexTypesetter.razor", example9TypesetterCode),
            new("typeset-math.js", example9ScriptCode),
        ];

        example14CodeFiles =
        [
            new("BitMarkdownViewerDemo.razor.scss", example14ScssCode),
        ];
    }
}

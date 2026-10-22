using System.Text;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.MarkdownView;

public partial class BitAiMarkdownViewDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Citations",
            Type = "IReadOnlyList<BitAiCitation>?",
            DefaultValue = "null",
            Description = "The citations that the inline citation markers of the document resolve against. A marker whose number matches the Number of a citation is drawn as a BitAiCitationMarker (unless BitAiUiOptions.InlineCitations is off); one that matches none stays the literal text it was.",
            LinkType = LinkType.Link,
            Href = "/components/ai/citationmarker",
        },
        new()
        {
            Name = "Classes",
            Type = "BitAiMarkdownViewClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the Markdown view.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Document",
            Type = "BitAiMarkdownDocument?",
            DefaultValue = "null",
            Description = "The parsed document to render (BitAiMarkdownParser.Parse, or the incremental parser a BitAiStreamingMarkdown holds). Nothing is rendered while it is null.",
            LinkType = LinkType.Link,
            Href = "#markdown-parser",
        },
        new()
        {
            Name = "OnCitationClick",
            Type = "EventCallback<BitAiCitation>",
            DefaultValue = "",
            Description = "The callback for when the user activates an inline citation marker, with the citation it stands for.",
        },
        new()
        {
            Name = "ShowCaret",
            Type = "bool",
            DefaultValue = "false",
            Description = "Draws a caret after the last paragraph while it is still open (a document parsed with isComplete false), the sign that more text is on its way. BitAiUiOptions.ShowStreamingCaret turns the caret off for every view at once.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAiMarkdownViewClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the Markdown view.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitAiMarkdownViewClassStyles",
            Parameters =
            [
                new() { Name = "Root", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the root element of the BitAiMarkdownView." },
                new() { Name = "Paragraph", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for every paragraph of the BitAiMarkdownView." },
                new() { Name = "Heading", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for every heading of the BitAiMarkdownView." },
                new() { Name = "List", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for every list (bulleted, numbered or task list) of the BitAiMarkdownView." },
                new() { Name = "ListItem", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for every list item of the BitAiMarkdownView." },
                new() { Name = "Quote", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for every block quote of the BitAiMarkdownView." },
                new() { Name = "TableContainer", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the scroll container of every table of the BitAiMarkdownView, which also draws its frame." },
                new() { Name = "Table", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for every table of the BitAiMarkdownView." },
                new() { Name = "Separator", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for every thematic break (horizontal rule) of the BitAiMarkdownView." },
                new() { Name = "InlineCode", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for every inline code span of the BitAiMarkdownView." },
                new() { Name = "Link", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for every link of the BitAiMarkdownView." },
                new() { Name = "Image", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for every image of the BitAiMarkdownView." },
                new()
                {
                    Name = "ImagePlaceholder",
                    Type = "BitActionButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the button (a BitActionButton) the BitAiMarkdownView shows in place of an image the image policy loads only on request.",
                    LinkType = LinkType.Link,
                    Href = "/components/actionbutton/#class-styles",
                },
                new() { Name = "BlockedImage", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the alternative text the BitAiMarkdownView shows in place of an image the image policy blocks." },
                new() { Name = "Math", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the built-in rendering of every mathematical expression of the BitAiMarkdownView (not used while a MathRenderer is registered)." },
                new() { Name = "FootnoteReference", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for every footnote reference of the BitAiMarkdownView." },
                new() { Name = "Footnotes", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the footnotes section of the BitAiMarkdownView." },
                new() { Name = "Caret", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the caret the BitAiMarkdownView draws after streaming text." },
            ]
        },
        new()
        {
            Id = "markdown-parser",
            Title = "BitAiMarkdownParser",
            Description = "Parses Markdown into the BitAiMarkdownDocument the view renders: a CommonMark subset with the GitHub-flavoured extensions models write (tables, task lists, strikethrough, autolinks, footnotes, mathematics), built to parse a half-written answer many times a second rather than for conformance. Every block records whether the source ended inside it, which is what keeps streaming settled. For text that grows, BitAiIncrementalMarkdownParser (new(options), Parse(source, isComplete), Reset()) reuses the work done for the text already seen; BitAiStreamingMarkdown holds one.",
            Parameters =
            [
                new()
                {
                    Name = "Parse(string? markdown, BitAiMarkdownOptions? options = null, bool isComplete = true)",
                    Type = "BitAiMarkdownDocument",
                    DefaultValue = "",
                    Description = "Parses the Markdown with the given extensions (BitAiMarkdownOptions.Default when null). isComplete false marks the trailing block open: more text is expected. A null or empty source is an empty document.",
                },
            ]
        },
        new()
        {
            Id = "markdown-document",
            Title = "BitAiMarkdownDocument",
            Description = "A parsed document: what BitAiMarkdownParser.Parse returns and the view renders. Its blocks are a tree of BitAiMarkdownBlock nodes (paragraphs, headings, lists, quotes, tables, fenced code, mathematics, ...), each of which records whether the source ended inside it (IsOpen).",
            Parameters =
            [
                new() { Name = "Blocks", Type = "List<BitAiMarkdownBlock>", DefaultValue = "[]", Description = "The blocks of the document, in order." },
                new() { Name = "Footnotes", Type = "List<BitAiFootnoteBlock>", DefaultValue = "[]", Description = "The footnotes the text cites, in the order they are first cited and numbered from one; a definition nobody cites is dropped." },
                new() { Name = "Empty", Type = "BitAiMarkdownDocument", DefaultValue = "", Description = "Static: an empty document." },
            ]
        },
        new()
        {
            Id = "markdown-options",
            Title = "BitAiMarkdownOptions",
            Description = "Which Markdown extensions to enable and how to treat untrusted markup - a record handed to BitAiMarkdownParser.Parse, and the Markdown of BitAiUiOptions, which a BitAiStreamingMarkdown parses with and every view reads AllowRawHtml from. The defaults are what an AI chat wants: the GitHub-flavoured extensions on, raw HTML off. BitAiMarkdownOptions.Default holds them.",
            Parameters =
            [
                new() { Name = "Tables", Type = "bool", DefaultValue = "true", Description = "Parse pipe tables." },
                new() { Name = "Strikethrough", Type = "bool", DefaultValue = "true", Description = "Parse strikethrough written with double tildes." },
                new() { Name = "TaskLists", Type = "bool", DefaultValue = "true", Description = "Parse task-list checkboxes in list items. The view draws them disabled: they show what the model wrote, nothing of the page can change them." },
                new() { Name = "AutoLinks", Type = "bool", DefaultValue = "true", Description = "Turn bare URLs into links." },
                new() { Name = "Math", Type = "bool", DefaultValue = "true", Description = "Parse dollar-delimited mathematics ($inline$ and $$display$$)." },
                new() { Name = "CitationMarkers", Type = "bool", DefaultValue = "true", Description = "Turn bracketed digit runs such as [3] into citation markers, which the Citations of the view resolve." },
                new() { Name = "AllowRawHtml", Type = "bool", DefaultValue = "false", Description = "Keep raw HTML as markup instead of escaping it. The view renders it only when the options in force (BitAiUiOptions.Markdown) allow it too. Leave it off unless the Markdown comes from a source you trust as much as your own code: a model can be talked into emitting a script tag." },
                new() { Name = "HeadingIds", Type = "bool", DefaultValue = "true", Description = "Give headings anchor ids derived from their text (bit-ai-h-<slug>, numbered -2, -3, ... when a text repeats), which the view renders after its own id." },
                new() { Name = "HideDanglingDelimiters", Type = "bool", DefaultValue = "true", Description = "Hide a trailing unmatched emphasis or code delimiter while the text is still streaming, so a bold run does not flash a literal asterisk before its closing one arrives." },
                new() { Name = "MaxNestingDepth", Type = "int", DefaultValue = "32", Description = "How deeply blocks and inline spans may nest before further markers are read as literal text - a bound on the parser's recursion and on the tree the view walks, so a line of ten thousand > cannot take the process down." },
                new() { Name = "SoftBreaksAsHardBreaks", Type = "bool", DefaultValue = "false", Description = "Treat a single newline inside a paragraph as a line break, as chat clients often do (CommonMark renders it as a space)." },
                new() { Name = "Footnotes", Type = "bool", DefaultValue = "true", Description = "Parse footnotes: [^1] in the text with a matching [^1]: note definition." },
            ]
        },
        new()
        {
            Id = "image-policy",
            Title = "BitAiImagePolicy",
            Description = "Which images of the Markdown are fetched - the Images of BitAiUiOptions. A model can be talked into emitting an image URL, and a browser fetching it leaks the reader's address and the fact that they read the message to whoever controls the host. The default allows images; a host handling sensitive conversations restricts them to its own CDN, asks before loading, or blocks them. Script-capable URL schemes are neutralised whatever the policy.",
            Parameters =
            [
                new() { Name = "Allow", Type = "BitAiImagePolicy", DefaultValue = "", Description = "Static: shows every image whose URL scheme is safe. The default policy." },
                new() { Name = "Block", Type = "BitAiImagePolicy", DefaultValue = "", Description = "Static: shows no remote image (nor a data image); each one is replaced by its alternative text." },
                new() { Name = "ClickToLoad", Type = "BitAiImagePolicy", DefaultValue = "", Description = "Static: loads each remote image only when the reader activates its placeholder." },
                new() { Name = "AllowListedHosts(params string[] hosts)", Type = "BitAiImagePolicy", DefaultValue = "", Description = "Static: shows the remote images of the given hosts (compared without regard to case; a leading *. matches subdomains) and a click-to-load placeholder for the rest. A relative URL has no host, so it gets the placeholder too." },
                new() { Name = "AllowDataImages", Type = "bool", DefaultValue = "true", Description = "Whether inline data: images are shown. They involve no network request, so they are shown under every policy but Block while this is on." },
                new() { Name = "Hosts", Type = "IReadOnlyCollection<string>", DefaultValue = "[]", Description = "The hosts allowed under AllowListedHosts." },
                new() { Name = "Mode", Type = "BitAiImageMode", DefaultValue = "BitAiImageMode.Allow", Description = "How images are treated.", LinkType = LinkType.Link, Href = "#image-mode-enum" },
                new() { Name = "Decide(string url)", Type = "BitAiImageDecision", DefaultValue = "", Description = "Decides what to do with one image source (already checked for a safe scheme): show it, show a placeholder that loads it on request, or hide it.", LinkType = LinkType.Link, Href = "#image-decision-enum" },
            ]
        },
        new()
        {
            Id = "math-context",
            Title = "BitAiMathContext",
            Description = "What a mathematics renderer is handed. The renderer is a BitAiMathRenderer - delegate RenderFragment BitAiMathRenderer(BitAiMathContext context) - set as BitAiRendererRegistry.MathRenderer, which replaces the built-in monospace fallback of every view.",
            Parameters =
            [
                new() { Name = "Expression", Type = "string", DefaultValue = "", Description = "The expression source, without its dollar delimiters." },
                new() { Name = "IsBlock", Type = "bool", DefaultValue = "", Description = "True for display mathematics ($$...$$), false for an inline expression." },
            ]
        },
        new()
        {
            Id = "code-block-context",
            Title = "BitAiCodeBlockContext",
            Description = "What a code block renderer is handed. The renderer is a BitAiCodeBlockRenderer - delegate RenderFragment BitAiCodeBlockRenderer(BitAiCodeBlockContext context) - registered for one language with BitAiRendererRegistry.RegisterCodeLanguage, which renders the fenced blocks of that language in place of the BitAiCodeBlockView.",
            Parameters =
            [
                new() { Name = "Code", Type = "string", DefaultValue = "", Description = "The source inside the fence." },
                new() { Name = "Language", Type = "string?", DefaultValue = "", Description = "The language of the fence info string, as written." },
                new() { Name = "Meta", Type = "string?", DefaultValue = "", Description = "The rest of the info string after the language, when there is one (```bars title=\"Load\")." },
                new() { Name = "IsStreaming", Type = "bool", DefaultValue = "", Description = "Whether the block is still receiving content - its closing fence has not arrived. A renderer of a diagram language waits for it to turn false: a half-written diagram is a syntax error, and one flashing on every token is worse than the source." },
            ]
        },
        new()
        {
            Id = "renderer-registry",
            Title = "BitAiRendererRegistry",
            Description = "The members of the renderer registry the Markdown view reads (it also holds the renderers of parts, tools, artifacts, structured data and citations). The registry is a singleton service: register at startup, through ConfigureRenderers of AddBitBlazorUIAIServices, and treat the entries as immutable afterwards.",
            Parameters =
            [
                new() { Name = "MathRenderer", Type = "BitAiMathRenderer?", DefaultValue = "null", Description = "Renders the mathematics of every view when set, in place of the fallback (the source in monospace, under bit-aimdv-mth / bit-aimdv-mtb with a data-math attribute a script can typeset from).", LinkType = LinkType.Link, Href = "#math-context" },
                new() { Name = "RegisterCodeLanguage(string language, BitAiCodeBlockRenderer renderer)", Type = "void", DefaultValue = "", Description = "Registers a renderer for the fenced blocks of one language (mermaid, vega, ...), matched without regard to case, in place of the code view.", LinkType = LinkType.Link, Href = "#code-block-context" },
                new() { Name = "FindCodeLanguage(string? language)", Type = "BitAiCodeBlockRenderer?", DefaultValue = "", Description = "The renderer registered for a language, or null for the built-in code view." },
            ]
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "image-mode-enum",
            Name = "BitAiImageMode",
            Description = "How the images of the Markdown are treated (the Mode of a BitAiImagePolicy).",
            Items =
            [
                new() { Name = "Allow", Value = "0", Description = "Show every image whose URL scheme is safe." },
                new() { Name = "AllowListedHosts", Value = "1", Description = "Show images only from the listed hosts; the others get a placeholder that loads them on request." },
                new() { Name = "Block", Value = "2", Description = "Show no remote images; each is replaced by its alternative text." },
                new() { Name = "ClickToLoad", Value = "3", Description = "Show a placeholder that loads the image when the reader asks for it." },
            ]
        },
        new()
        {
            Id = "image-decision-enum",
            Name = "BitAiImageDecision",
            Description = "What a BitAiImagePolicy decided about one image.",
            Items =
            [
                new() { Name = "Show", Value = "0", Description = "Render the image." },
                new() { Name = "AskFirst", Value = "1", Description = "Render a placeholder button that loads the image when activated." },
                new() { Name = "Hide", Value = "2", Description = "Render the alternative text in place of the image." },
            ]
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-AiMarkdownView-color", DefaultValue = "inherit", Description = "Text color of the prose." },
        new() { Name = "--bit-AiMarkdownView-font-size", DefaultValue = "inherit", Description = "Text size of the prose." },
        new() { Name = "--bit-AiMarkdownView-line-height", DefaultValue = "1.6", Description = "Line height of the prose." },
        new() { Name = "--bit-AiMarkdownView-block-spacing", DefaultValue = "spacing(2)", Description = "Room between two blocks (paragraphs, lists, tables, code blocks, ...); a heading gets one and a half times as much above it." },
        new() { Name = "--bit-AiMarkdownView-heading-color", DefaultValue = "inherit", Description = "Text color of the headings." },
        new() { Name = "--bit-AiMarkdownView-heading-font-weight", DefaultValue = "var(--bit-tpg-fw-semibold)", Description = "Weight of the headings." },
        new() { Name = "--bit-AiMarkdownView-link-color", DefaultValue = "var(--bit-clr-pri-fg)", Description = "Text color of the links (and of the footnote links)." },
        new() { Name = "--bit-AiMarkdownView-link-hover-color", DefaultValue = "the link color", Description = "Text color of a link under the pointer." },
        new() { Name = "--bit-AiMarkdownView-code-color", DefaultValue = "inherit", Description = "Text color of the code spans and of the mathematics fallback." },
        new() { Name = "--bit-AiMarkdownView-code-background", DefaultValue = "8% of var(--bit-clr-fg-pri)", Description = "Background of the code spans and of the mathematics fallback: a translucent wash of the text color, so it reads on whatever the message sits on." },
        new() { Name = "--bit-AiMarkdownView-code-radius", DefaultValue = "var(--bit-shp-radius-sm)", Description = "Corner radius of the code spans and of the mathematics fallback." },
        new() { Name = "--bit-AiMarkdownView-code-font-family", DefaultValue = "var(--bit-tpg-font-family-mono)", Description = "Font of the code spans and of the mathematics fallback." },
        new() { Name = "--bit-AiMarkdownView-marker-color", DefaultValue = "var(--bit-clr-fg-ter)", Description = "Bullets and numbers of the lists." },
        new() { Name = "--bit-AiMarkdownView-quote-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "Text color of the block quotes." },
        new() { Name = "--bit-AiMarkdownView-quote-border-color", DefaultValue = "var(--bit-clr-brd-pri)", Description = "Bar down the start edge of the block quotes." },
        new() { Name = "--bit-AiMarkdownView-rule-color", DefaultValue = "var(--bit-clr-brd-sec)", Description = "Thematic breaks, and the line above the footnotes." },
        new() { Name = "--bit-AiMarkdownView-table-border-color", DefaultValue = "var(--bit-clr-brd-sec)", Description = "Border of the tables and of their rows." },
        new() { Name = "--bit-AiMarkdownView-table-header-background", DefaultValue = "4% of var(--bit-clr-fg-pri)", Description = "Background of the header row of the tables." },
        new() { Name = "--bit-AiMarkdownView-table-font-size", DefaultValue = "var(--bit-tpg-fs-sm)", Description = "Text size of the tables." },
        new() { Name = "--bit-AiMarkdownView-table-radius", DefaultValue = "var(--bit-shp-radius-surface)", Description = "Corner radius of the tables." },
        new() { Name = "--bit-AiMarkdownView-image-radius", DefaultValue = "var(--bit-shp-radius-surface)", Description = "Corner radius of the images." },
        new() { Name = "--bit-AiMarkdownView-muted-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "The footnotes, and the alternative text of a blocked image." },
        new() { Name = "--bit-AiMarkdownView-caret-color", DefaultValue = "var(--bit-clr-pri)", Description = "The caret after streaming text." },
    ];



    private const string basicMarkdown = """
        # Choosing a render mode

        Blazor renders on the **server**, over a circuit, or in the *browser*, on WebAssembly - and ~~one size fits all~~
        each page can pick its own. Start with [the docs](https://learn.microsoft.com/aspnet/core/blazor) or
        https://blazorui.bitplatform.dev, and set it with `@rendermode`.

        1. Measure the first paint.
        2. Measure the cost of every update:
           - on the server, in messages;
           - in the browser, in milliseconds.
        """;

    private readonly BitAiMarkdownDocument basicDocument = BitAiMarkdownParser.Parse(basicMarkdown);

    private const string blocksMarkdown = """
        | Mode | Runs on | First paint | Offline |
        | :--- | --- | :---: | ---: |
        | Server | The server, over a circuit | Fast | No |
        | WebAssembly | The browser | Slower | Yes |

        > Rendering more often than the eye can follow is not smoother. It is just louder.

        - [x] Pick a render mode
        - [ ] Measure the first paint

        ---

        ```csharp
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();
        ```
        """;

    private readonly BitAiMarkdownDocument blocksDocument = BitAiMarkdownParser.Parse(blocksMarkdown);

    private const string openMarkdown = "The answer is still being written, so its last paragraph is open";

    private readonly BitAiMarkdownDocument openDocument = BitAiMarkdownParser.Parse(openMarkdown, isComplete: false);

    private const string footnotesMarkdown = """
        ## Setup

        Server rendering keeps the state on the server[^1], which is also why a dropped connection loses it[^1].
        WebAssembly downloads the runtime once[^2].

        [^1]: Over a SignalR circuit.
        [^2]: And caches it for the next visit.
        """;

    private readonly BitAiMarkdownDocument footnotesDocument = BitAiMarkdownParser.Parse(footnotesMarkdown);

    [Inject] private BitAiRendererRegistry Renderers { get; set; } = default!;

    private const string mathMarkdown = """
        The energy of a body at rest is $E = mc^2$, and the area of a circle is $\pi r^2$.

        $$
        \sigma = \sqrt{\sum_{i=1}^{n} (x_i - \mu)^2 / n}
        $$
        """;

    private readonly BitAiMarkdownDocument mathDocument = BitAiMarkdownParser.Parse(mathMarkdown);

    private bool typesetMath;
    private BitAiMathRenderer? previousMathRenderer;

    private void ToggleMathRenderer()
    {
        if (typesetMath)
        {
            previousMathRenderer = Renderers.MathRenderer;
            Renderers.MathRenderer = RenderMath;
        }
        else
        {
            Renderers.MathRenderer = previousMathRenderer;
        }
    }

    public void Dispose()
    {
        // The registry is app-wide: leave it as this page found it.
        if (typesetMath) Renderers.MathRenderer = previousMathRenderer;
    }

    private static readonly Dictionary<string, string> mathSymbols = new()
    {
        ["alpha"] = "α", ["beta"] = "β", ["pi"] = "π", ["mu"] = "μ", ["sigma"] = "σ", ["theta"] = "θ",
        ["sum"] = "∑", ["int"] = "∫", ["infty"] = "∞", ["sqrt"] = "√", ["times"] = "×", ["cdot"] = "·",
        ["leq"] = "≤", ["geq"] = "≥", ["neq"] = "≠", ["approx"] = "≈",
    };

    // A tiny typesetter - superscripts, subscripts and a few symbols - where an app would use KaTeX or MathJax. The
    // expression is model output, so it is only ever written as text, never as markup.
    private static RenderFragment RenderMath(BitAiMathContext context) => builder =>
    {
        builder.OpenElement(0, context.IsBlock ? "div" : "span");
        builder.AddAttribute(1, "style", context.IsBlock ? "text-align: center; font: italic 1.25em serif;" : "font: italic 1.1em serif;");

        foreach (var (text, tag) in Typeset(context.Expression))
        {
            if (tag is null)
            {
                builder.AddContent(2, text);
                continue;
            }

            builder.OpenElement(3, tag);
            builder.AddContent(4, text);
            builder.CloseElement();
        }

        builder.CloseElement();
    };

    private static IEnumerable<(string Text, string? Tag)> Typeset(string tex)
    {
        var text = new StringBuilder();

        for (var i = 0; i < tex.Length; i++)
        {
            if (tex[i] is '^' or '_' && i + 1 < tex.Length)
            {
                if (text.Length > 0) yield return (Symbols(text.ToString()), null);
                text.Clear();

                var tag = tex[i] == '^' ? "sup" : "sub";
                var start = i + 1;
                var end = tex[start] == '{' ? tex.IndexOf('}', start) : start;
                if (end < 0) end = tex.Length - 1;
                if (tex[start] == '\\') while (end + 1 < tex.Length && char.IsLetter(tex[end + 1])) end++;

                yield return (Symbols(tex[start..(end + 1)].Trim('{', '}')), tag);
                i = end;
            }
            else if (tex[i] is not ('{' or '}'))
            {
                text.Append(tex[i]);
            }
        }

        if (text.Length > 0) yield return (Symbols(text.ToString()), null);
    }

    private static string Symbols(string tex) =>
        Regex.Replace(tex, @"\\([a-zA-Z]+)", m => mathSymbols.GetValueOrDefault(m.Groups[1].Value, m.Groups[1].Value));

    private const string barsMarkdown = """
        Where the time of a first visit goes:

        ```bars
        Download: 420
        Start the runtime: 180
        First render: 60
        ```
        """;

    private readonly BitAiMarkdownDocument barsDocument = BitAiMarkdownParser.Parse(barsMarkdown);

    private const string openBarsMarkdown = """
        And of a second visit:

        ```bars
        Download: 0
        Start the run
        """;

    private readonly BitAiMarkdownDocument openBarsDocument = BitAiMarkdownParser.Parse(openBarsMarkdown, isComplete: false);

    protected override void OnInitialized()
    {
        // An app registers its renderers once, at startup: AddBitBlazorUIAIServices(ai => ai.ConfigureRenderers(...)).
        Renderers.RegisterCodeLanguage("bars", RenderBars);
    }

    // Renders a ```bars block - one "label: value" a line - as a bar chart, and waits for the block to be complete.
    private static RenderFragment RenderBars(BitAiCodeBlockContext context) => builder =>
    {
        if (context.IsStreaming)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "style", "padding: 1rem; font-style: italic; opacity: 0.7;");
            builder.AddContent(2, "Drawing the chart...");
            builder.CloseElement();
            return;
        }

        var bars = context.Code.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                               .Select(line => line.Split(':', 2, StringSplitOptions.TrimEntries))
                               .Where(pair => pair.Length == 2 && double.TryParse(pair[1], out _))
                               .Select(pair => (Label: pair[0], Value: double.Parse(pair[1])))
                               .ToArray();
        var max = bars.Length == 0 ? 1 : Math.Max(1, bars.Max(b => b.Value));

        builder.OpenElement(3, "div");
        builder.AddAttribute(4, "role", "img");
        builder.AddAttribute(5, "aria-label", string.Join(", ", bars.Select(b => $"{b.Label} {b.Value}")));
        builder.AddAttribute(6, "style", "display: grid; grid-template-columns: max-content 1fr max-content; gap: 0.5rem 0.75rem; align-items: center;");

        foreach (var (label, value) in bars)
        {
            builder.OpenElement(7, "span");
            builder.AddContent(8, label);
            builder.CloseElement();

            builder.OpenElement(9, "span");
            builder.AddAttribute(10, "style", $"height: 0.75rem; border-radius: 0.375rem; background-color: var(--bit-clr-pri); width: {Math.Round(value / max * 100)}%;");
            builder.CloseElement();

            builder.OpenElement(11, "span");
            builder.AddContent(12, $"{value} ms");
            builder.CloseElement();
        }

        builder.CloseElement();
    };

    private const string citedMarkdown = """
        Interactive Server keeps component state on the server and streams UI diffs over a SignalR circuit [1].
        If that circuit drops, the state goes with it unless you have persisted it yourself [2]. A number no
        citation has, such as [3], stays the text it is.
        """;

    private readonly BitAiMarkdownDocument citedDocument = BitAiMarkdownParser.Parse(citedMarkdown);

    private readonly BitAiCitation[] citations =
    [
        new("c1", new BitAiCitationSource("s1", "Blazor render modes")
        {
            Url = "https://learn.microsoft.com/aspnet/core/blazor/components/render-modes",
            SiteName = "Microsoft Learn",
        }) { Number = 1 },
        new("c2", new BitAiCitationSource("s2", "SignalR circuit lifetime")
        {
            Url = "https://learn.microsoft.com/aspnet/core/blazor/fundamentals/signalr",
            SiteName = "Microsoft Learn",
        }) { Number = 2 },
    ];

    private string? clickedSource;

    private const string imagesMarkdown = """
        From this site: ![A portrait of a woman](_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female-72.png)

        From a listed host: ![A portrait of a woman, from a CDN](https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/persona-female.png)

        From another host: ![A clock](https://img.icons8.com/fluency/2x/clock.png)

        Inline: ![A blue square](data:image/svg+xml,%3Csvg%20xmlns='http://www.w3.org/2000/svg'%20width='48'%20height='48'%3E%3Crect%20width='48'%20height='48'%20rx='8'%20fill='%232563eb'/%3E%3C/svg%3E)
        """;

    private readonly BitAiMarkdownDocument imagesDocument = BitAiMarkdownParser.Parse(imagesMarkdown);

    private readonly List<BitChoiceGroupItem<string>> imageModeItems =
    [
        new() { Text = "Allow", Value = nameof(BitAiImageMode.Allow) },
        new() { Text = "AllowListedHosts", Value = nameof(BitAiImageMode.AllowListedHosts) },
        new() { Text = "ClickToLoad", Value = nameof(BitAiImageMode.ClickToLoad) },
        new() { Text = "Block", Value = nameof(BitAiImageMode.Block) },
    ];

    private string? imageMode = nameof(BitAiImageMode.Allow);
    private bool allowDataImages = true;
    private BitAiUiOptions imageOptions = BitAiUiOptions.Default;

    private void UpdateImageOptions()
    {
        var policy = imageMode switch
        {
            nameof(BitAiImageMode.AllowListedHosts) => BitAiImagePolicy.AllowListedHosts("static2.sharepointonline.com"),
            nameof(BitAiImageMode.ClickToLoad) => BitAiImagePolicy.ClickToLoad,
            nameof(BitAiImageMode.Block) => BitAiImagePolicy.Block,
            _ => BitAiImagePolicy.Allow,
        };

        imageOptions = BitAiUiOptions.Default with { Images = policy with { AllowDataImages = allowDataImages } };
    }

    private const string untrustedMarkdown = """
        A link a model was talked into: [claim your prize](javascript:alert('pwned')).

        Press <kbd>Ctrl</kbd> + <kbd>K</kbd> to search.

        <details><summary>An HTML block</summary>Rendered as markup only when raw HTML is allowed.</details>
        """;

    private static readonly BitAiMarkdownOptions rawHtmlMarkdownOptions = BitAiMarkdownOptions.Default with { AllowRawHtml = true };

    private bool allowRawHtml;
    private BitAiMarkdownDocument untrustedDocument = BitAiMarkdownParser.Parse(untrustedMarkdown);
    private BitAiUiOptions rawHtmlOptions = BitAiUiOptions.Default;

    private void UpdateRawHtml()
    {
        var markdownOptions = allowRawHtml ? rawHtmlMarkdownOptions : BitAiMarkdownOptions.Default;

        // Both are needed: the parser keeps the markup, and the view renders what the options in force allow.
        untrustedDocument = BitAiMarkdownParser.Parse(untrustedMarkdown, markdownOptions);
        rawHtmlOptions = BitAiUiOptions.Default with { Markdown = markdownOptions };
    }

    private const string optionsMarkdown = """
        | Option | Default |
        | --- | --- |
        | Tables | on |

        - [x] a task that is done
        - [ ] one that is not

        ~~Struck through~~, a bare URL https://bitplatform.dev and $x^2$.
        One line,
        and the next.
        """;

    private bool parseTables = true;
    private bool parseTaskLists = true;
    private bool parseStrikethrough = true;
    private bool parseAutoLinks = true;
    private bool parseMath = true;
    private bool parseSoftBreaks;
    private BitAiMarkdownDocument optionsDocument = BitAiMarkdownParser.Parse(optionsMarkdown);

    private void Reparse()
    {
        var options = new BitAiMarkdownOptions
        {
            Tables = parseTables,
            TaskLists = parseTaskLists,
            Strikethrough = parseStrikethrough,
            AutoLinks = parseAutoLinks,
            Math = parseMath,
            SoftBreaksAsHardBreaks = parseSoftBreaks,
        };

        optionsDocument = BitAiMarkdownParser.Parse(optionsMarkdown, options);
    }

    private const string paramsMarkdown = """
        > Every link of a view inside the BitParams, such as [this one](https://bitplatform.dev), is bold.
        """;

    private readonly BitAiMarkdownDocument paramsDocument = BitAiMarkdownParser.Parse(paramsMarkdown);

    private readonly BitAiMarkdownViewParams[] markdownParams =
    [
        new() { Styles = new() { Link = "font-weight: bold;", Quote = "font-style: italic;" } },
    ];

    private const string styleMarkdown = """
        #### A styled answer

        Some prose with a [link](https://bitplatform.dev) and `inline code`.

        - a first point
        - a second point

        > A quote.
        """;

    private readonly BitAiMarkdownDocument styleDocument = BitAiMarkdownParser.Parse(styleMarkdown);

    private const string rtlMarkdown = """
        #### دو حالت رندر

        بلیزر روی **سرور** یا در **مرورگر** رندر می‌کند:

        - روی سرور، از راه یک اتصال؛
        - در مرورگر، روی WebAssembly.

        > رندر بیشتر از آنچه چشم می‌بیند، روان‌تر نیست.

        | حالت | اجرا | نخستین نمایش |
        | --- | --- | ---: |
        | سرور | روی سرور | سریع |
        | وب‌اسمبلی | در مرورگر | کندتر |
        """;

    private readonly BitAiMarkdownDocument rtlDocument = BitAiMarkdownParser.Parse(rtlMarkdown);
}

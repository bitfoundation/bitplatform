namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.MarkdownView;

public partial class BitAiMarkdownViewDemo
{
    private readonly string example1RazorCode = @"
<BitAiMarkdownView Document=""basicDocument"" />";
    private readonly string example1CsharpCode = @"
private const string basicMarkdown = """"""
    # Choosing a render mode

    Blazor renders on the **server**, over a circuit, or in the *browser*, on WebAssembly - and ~~one size fits all~~
    each page can pick its own. Start with [the docs](https://learn.microsoft.com/aspnet/core/blazor) or
    https://blazorui.bitplatform.dev, and set it with `@rendermode`.

    1. Measure the first paint.
    2. Measure the cost of every update:
       - on the server, in messages;
       - in the browser, in milliseconds.
    """""";

private readonly BitAiMarkdownDocument basicDocument = BitAiMarkdownParser.Parse(basicMarkdown);";

    private readonly string example2RazorCode = @"
<BitAiMarkdownView Document=""blocksDocument"" />";
    private readonly string example2CsharpCode = @"
private const string blocksMarkdown = """"""
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
    """""";

private readonly BitAiMarkdownDocument blocksDocument = BitAiMarkdownParser.Parse(blocksMarkdown);";

    private readonly string example3RazorCode = @"
<BitAiMarkdownView Document=""openDocument"" ShowCaret />";
    private readonly string example3CsharpCode = @"
private const string openMarkdown = ""The answer is still being written, so its last paragraph is open"";

private readonly BitAiMarkdownDocument openDocument = BitAiMarkdownParser.Parse(openMarkdown, isComplete: false);";

    private readonly string example4RazorCode = @"
<BitAiMarkdownView Id=""answer"" Document=""footnotesDocument"" />";
    private readonly string example4CsharpCode = @"
private const string footnotesMarkdown = """"""
    ## Setup

    Server rendering keeps the state on the server[^1], which is also why a dropped connection loses it[^1].
    WebAssembly downloads the runtime once[^2].

    [^1]: Over a SignalR circuit.
    [^2]: And caches it for the next visit.
    """""";

private readonly BitAiMarkdownDocument footnotesDocument = BitAiMarkdownParser.Parse(footnotesMarkdown);";

    private readonly string example5RazorCode = @"
@using System.Text
@using System.Text.RegularExpressions

<BitToggle @bind-Value=""typesetMath"" @bind-Value:after=""ToggleMathRenderer"" Text=""Use a math renderer of my own"" />

<CascadingValue Value=""pageRenderers"" IsFixed=""true"">
    <BitAiMarkdownView Document=""mathDocument"" />
</CascadingValue>";
    private readonly string example5CsharpCode = @"
// An app registers its renderers once, at startup: AddBitBlazorUIAIServices(ai => ai.ConfigureRenderers(...)). That
// registry is one instance for the whole app, so a page that wants renderers of its own cascades a registry of its own.
private readonly BitAiRendererRegistry pageRenderers = new();

private const string mathMarkdown = """"""
    The energy of a body at rest is $E = mc^2$, and the area of a circle is $\pi r^2$.

    $$
    \sigma = \sqrt{\sum_{i=1}^{n} (x_i - \mu)^2 / n}
    $$
    """""";

private readonly BitAiMarkdownDocument mathDocument = BitAiMarkdownParser.Parse(mathMarkdown);

private bool typesetMath;

private void ToggleMathRenderer() => pageRenderers.MathRenderer = typesetMath ? RenderMath : null;

private static readonly Dictionary<string, string> mathSymbols = new()
{
    [""alpha""] = ""α"", [""beta""] = ""β"", [""pi""] = ""π"", [""mu""] = ""μ"", [""sigma""] = ""σ"", [""theta""] = ""θ"",
    [""sum""] = ""∑"", [""int""] = ""∫"", [""infty""] = ""∞"", [""sqrt""] = ""√"", [""times""] = ""×"", [""cdot""] = ""·"",
    [""leq""] = ""≤"", [""geq""] = ""≥"", [""neq""] = ""≠"", [""approx""] = ""≈"",
};

// A tiny typesetter - superscripts, subscripts and a few symbols - where an app would use KaTeX or MathJax. The
// expression is model output, so it is only ever written as text, never as markup.
private static RenderFragment RenderMath(BitAiMathContext context) => builder =>
{
    builder.OpenElement(0, context.IsBlock ? ""div"" : ""span"");
    builder.AddAttribute(1, ""style"", context.IsBlock ? ""text-align: center; font: italic 1.25em serif;"" : ""font: italic 1.1em serif;"");

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

            var tag = tex[i] == '^' ? ""sup"" : ""sub"";
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
    Regex.Replace(tex, @""\\([a-zA-Z]+)"", m => mathSymbols.GetValueOrDefault(m.Groups[1].Value, m.Groups[1].Value));";

    private readonly string example6RazorCode = @"
<CascadingValue Value=""pageRenderers"" IsFixed=""true"">
    <BitAiMarkdownView Document=""barsDocument"" />

    <BitAiMarkdownView Document=""openBarsDocument"" />
</CascadingValue>";
    private readonly string example6CsharpCode = @"
private readonly BitAiRendererRegistry pageRenderers = new();

private const string barsMarkdown = """"""
    Where the time of a first visit goes:

    ```bars
    Download: 420
    Start the runtime: 180
    First render: 60
    ```
    """""";

private readonly BitAiMarkdownDocument barsDocument = BitAiMarkdownParser.Parse(barsMarkdown);

private const string openBarsMarkdown = """"""
    And of a second visit:

    ```bars
    Download: 0
    Start the run
    """""";

private readonly BitAiMarkdownDocument openBarsDocument = BitAiMarkdownParser.Parse(openBarsMarkdown, isComplete: false);

protected override void OnInitialized()
{
    // An app registers its renderers once, at startup: AddBitBlazorUIAIServices(ai => ai.ConfigureRenderers(...)).
    pageRenderers.RegisterCodeLanguage(""bars"", RenderBars);
}

// Renders a ```bars block - one ""label: value"" a line - as a bar chart, and waits for the block to be complete.
private static RenderFragment RenderBars(BitAiCodeBlockContext context) => builder =>
{
    if (context.IsStreaming)
    {
        builder.OpenElement(0, ""div"");
        builder.AddAttribute(1, ""style"", ""padding: 1rem; font-style: italic; opacity: 0.7;"");
        builder.AddContent(2, ""Drawing the chart..."");
        builder.CloseElement();
        return;
    }

    var bars = context.Code.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                           .Select(line => line.Split(':', 2, StringSplitOptions.TrimEntries))
                           .Where(pair => pair.Length == 2 && double.TryParse(pair[1], out _))
                           .Select(pair => (Label: pair[0], Value: double.Parse(pair[1])))
                           .ToArray();
    var max = bars.Length == 0 ? 1 : Math.Max(1, bars.Max(b => b.Value));

    builder.OpenElement(3, ""div"");
    builder.AddAttribute(4, ""role"", ""img"");
    builder.AddAttribute(5, ""aria-label"", string.Join("", "", bars.Select(b => $""{b.Label} {b.Value}"")));
    builder.AddAttribute(6, ""style"", ""display: grid; grid-template-columns: max-content 1fr max-content; gap: 0.5rem 0.75rem; align-items: center;"");

    foreach (var (label, value) in bars)
    {
        builder.OpenElement(7, ""span"");
        builder.AddContent(8, label);
        builder.CloseElement();

        builder.OpenElement(9, ""span"");
        builder.AddAttribute(10, ""style"", $""height: 0.75rem; border-radius: 0.375rem; background-color: var(--bit-clr-pri); width: {Math.Round(value / max * 100)}%;"");
        builder.CloseElement();

        builder.OpenElement(11, ""span"");
        builder.AddContent(12, $""{value} ms"");
        builder.CloseElement();
    }

    builder.CloseElement();
};";

    private readonly string example7RazorCode = @"
<BitAiMarkdownView Document=""citedDocument""
                   Citations=""citations""
                   OnCitationClick=""(BitAiCitation citation) => clickedSource = citation.Source.Title"" />

<div>Activated: <b>@(clickedSource ?? ""none yet"")</b></div>";
    private readonly string example7CsharpCode = @"
private const string citedMarkdown = """"""
    Interactive Server keeps component state on the server and streams UI diffs over a SignalR circuit [1].
    If that circuit drops, the state goes with it unless you have persisted it yourself [2]. A number no
    citation has, such as [3], stays the text it is.
    """""";

private readonly BitAiMarkdownDocument citedDocument = BitAiMarkdownParser.Parse(citedMarkdown);

private readonly BitAiCitation[] citations =
[
    new(""c1"", new BitAiCitationSource(""s1"", ""Blazor render modes"")
    {
        Url = ""https://learn.microsoft.com/aspnet/core/blazor/components/render-modes"",
        SiteName = ""Microsoft Learn"",
    }) { Number = 1 },
    new(""c2"", new BitAiCitationSource(""s2"", ""SignalR circuit lifetime"")
    {
        Url = ""https://learn.microsoft.com/aspnet/core/blazor/fundamentals/signalr"",
        SiteName = ""Microsoft Learn"",
    }) { Number = 2 },
];

private string? clickedSource;";

    private readonly string example8RazorCode = @"
<BitToggle @bind-Value=""customChips"" @bind-Value:after=""ToggleCitationRenderer"" Text=""Use a citation chip of my own"" />

<CascadingValue Value=""pageRenderers"" IsFixed=""true"">
    <BitAiMarkdownView Document=""citedDocument""
                       Citations=""citations""
                       OnCitationClick=""(BitAiCitation citation) => chipSource = citation.Source.Title"" />
</CascadingValue>

<div>Activated: <b>@(chipSource ?? ""none yet"")</b></div>";
    private readonly string example8CsharpCode = @"
// The registered registry is one instance for the whole app: a page with renderers of its own cascades its own.
private readonly BitAiRendererRegistry pageRenderers = new();

// citedMarkdown, citedDocument and citations as in the previous example.

private bool customChips;
private string? chipSource;

private void ToggleCitationRenderer() => pageRenderers.CitationRenderer = customChips ? RenderCitationChip : null;

// A pill naming the site of the source beside its number. ActivateAsync raises the OnCitationClick of the view the chip
// is drawn in, as a click on the built-in chip does. The source is model output, so it is only ever written as text.
private static RenderFragment RenderCitationChip(BitAiCitationContext context) => builder =>
{
    var citation = context.Citation;

    builder.OpenElement(0, ""button"");
    builder.AddAttribute(1, ""type"", ""button"");
    builder.AddAttribute(2, ""title"", citation.Source.Title);
    builder.AddAttribute(3, ""style"", ""margin-inline: 0.125rem; padding: 0 0.5rem; border: none; border-radius: 1rem; font: inherit; font-size: 0.75em; line-height: 1.5; cursor: pointer; color: var(--bit-clr-pri-text); background-color: var(--bit-clr-pri);"");
    builder.AddAttribute(4, ""onclick"", EventCallback.Factory.Create(context, context.ActivateAsync));
    builder.AddContent(5, $""{citation.Number} · {citation.Source.SiteName ?? citation.Source.Title}"");
    builder.CloseElement();
};";

    private readonly string example9RazorCode = @"
<BitChoiceGroup Label=""Policy"" Items=""imageModeItems"" @bind-Value=""imageMode"" @bind-Value:after=""UpdateImageOptions"" Horizontal />
<BitToggle @bind-Value=""allowDataImages"" @bind-Value:after=""UpdateImageOptions"" Text=""AllowDataImages"" />

<BitAiOptionsProvider UiOptions=""imageOptions"">
    <BitAiMarkdownView Document=""imagesDocument"" />
</BitAiOptionsProvider>";
    private readonly string example9CsharpCode = @"
[Inject] private NavigationManager Navigation { get; set; } = default!;

private const string imagesMarkdown = """"""
    From this site: ![A portrait of a woman](_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female-72.png)

    From a listed host: ![A portrait of a woman, from a CDN](https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/persona-female.png)

    From another host: ![A clock](https://img.icons8.com/fluency/2x/clock.png)

    Inline: ![A blue square](data:image/svg+xml,%3Csvg%20xmlns='http://www.w3.org/2000/svg'%20width='48'%20height='48'%3E%3Crect%20width='48'%20height='48'%20rx='8'%20fill='%232563eb'/%3E%3C/svg%3E)
    """""";

private readonly BitAiMarkdownDocument imagesDocument = BitAiMarkdownParser.Parse(imagesMarkdown);

private readonly List<BitChoiceGroupItem<string>> imageModeItems =
[
    new() { Text = ""Allow"", Value = nameof(BitAiImageMode.Allow) },
    new() { Text = ""AllowListedHosts"", Value = nameof(BitAiImageMode.AllowListedHosts) },
    new() { Text = ""ClickToLoad"", Value = nameof(BitAiImageMode.ClickToLoad) },
    new() { Text = ""Block"", Value = nameof(BitAiImageMode.Block) },
];

private string? imageMode = nameof(BitAiImageMode.Allow);
private bool allowDataImages = true;
private BitAiUiOptions imageOptions = BitAiUiOptions.Default;

private void UpdateImageOptions()
{
    var policy = imageMode switch
    {
        // A relative image is fetched from the host of the page, so that host is listed beside the CDN.
        nameof(BitAiImageMode.AllowListedHosts) => BitAiImagePolicy.AllowListedHosts(""static2.sharepointonline.com"", new Uri(Navigation.BaseUri).Host),
        nameof(BitAiImageMode.ClickToLoad) => BitAiImagePolicy.ClickToLoad,
        nameof(BitAiImageMode.Block) => BitAiImagePolicy.Block,
        _ => BitAiImagePolicy.Allow,
    };

    imageOptions = BitAiUiOptions.Default with { Images = policy with { AllowDataImages = allowDataImages } };
}";

    private readonly string example10RazorCode = @"
<BitToggle @bind-Value=""allowRawHtml"" @bind-Value:after=""UpdateRawHtml"" Text=""AllowRawHtml (trusted Markdown only)"" />

<BitAiOptionsProvider UiOptions=""rawHtmlOptions"">
    <BitAiMarkdownView Document=""untrustedDocument"" />
</BitAiOptionsProvider>";
    private readonly string example10CsharpCode = @"
private const string untrustedMarkdown = """"""
    A link a model was talked into: [claim your prize](javascript:alert('pwned')).

    Press <kbd>Ctrl</kbd> + <kbd>K</kbd> to search.

    <details><summary>An HTML block</summary>Rendered as markup only when raw HTML is allowed.</details>
    """""";

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
}";

    private readonly string example11RazorCode = @"
<BitStack Horizontal Wrap AutoHeight Gap=""0.5rem 1.5rem"">
    <BitToggle @bind-Value=""parseTables"" @bind-Value:after=""Reparse"" Text=""Tables"" />
    <BitToggle @bind-Value=""parseTaskLists"" @bind-Value:after=""Reparse"" Text=""TaskLists"" />
    <BitToggle @bind-Value=""parseStrikethrough"" @bind-Value:after=""Reparse"" Text=""Strikethrough"" />
    <BitToggle @bind-Value=""parseAutoLinks"" @bind-Value:after=""Reparse"" Text=""AutoLinks"" />
    <BitToggle @bind-Value=""parseMath"" @bind-Value:after=""Reparse"" Text=""Math"" />
    <BitToggle @bind-Value=""parseSoftBreaks"" @bind-Value:after=""Reparse"" Text=""SoftBreaksAsHardBreaks"" />
</BitStack>

<BitAiMarkdownView Document=""optionsDocument"" />";
    private readonly string example11CsharpCode = @"
private const string optionsMarkdown = """"""
    | Option | Default |
    | --- | --- |
    | Tables | on |

    - [x] a task that is done
    - [ ] one that is not

    ~~Struck through~~, a bare URL https://bitplatform.dev and $x^2$.
    One line,
    and the next.
    """""";

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
}";

    private readonly string example12RazorCode = @"
<BitParams Parameters=""markdownParams"">
    <BitAiMarkdownView Document=""paramsDocument"" />

    <BitAiMarkdownView Document=""paramsDocument"" Styles=""@(new() { Link = ""color: mediumseagreen;"" })"" />
</BitParams>";
    private readonly string example12CsharpCode = @"
private const string paramsMarkdown = """"""
    > A link of a view inside the BitParams, such as [this one](https://bitplatform.dev), is bold - unless its view sets Styles of its own.
    """""";

private readonly BitAiMarkdownDocument paramsDocument = BitAiMarkdownParser.Parse(paramsMarkdown);

private readonly BitAiMarkdownViewParams[] markdownParams =
[
    new() { Styles = new() { Link = ""font-weight: bold;"", Quote = ""font-style: italic;"" } },
];";

    private readonly string example13RazorCode = @"
<style>
    .custom-class {
        padding: 0.5rem 1rem;
        border-radius: 1rem;
        background-color: #1e293b;
        color: #e2e8f0;
        --bit-AiMarkdownView-link-color: #93c5fd;
        --bit-AiMarkdownView-quote-color: #cbd5e1;
    }

    .custom-root {
        border-inline-start: 4px solid tomato;
        padding-inline-start: 0.75rem;
    }

    .custom-link {
        color: tomato;
        text-decoration-style: wavy;
    }

    .custom-item::marker {
        color: tomato;
    }
</style>

<BitAiMarkdownView Document=""styleDocument"" Style=""padding: 0.5rem; border: 1px dashed dodgerblue;"" />

<BitAiMarkdownView Document=""styleDocument"" Class=""custom-class"" />

<BitAiMarkdownView Document=""styleDocument""
                   Styles=""@(new() { Heading = ""color: tomato;"", InlineCode = ""color: tomato;"", Quote = ""font-style: italic;"" })"" />

<BitAiMarkdownView Document=""styleDocument""
                   Classes=""@(new() { Root = ""custom-root"", Link = ""custom-link"", ListItem = ""custom-item"" })"" />

<div style=""--bit-AiMarkdownView-heading-color: light-dark(#6d28d9, #a78bfa); --bit-AiMarkdownView-link-color: light-dark(#6d28d9, #a78bfa); --bit-AiMarkdownView-quote-border-color: #8b5cf6; --bit-AiMarkdownView-code-color: #4c1d95; --bit-AiMarkdownView-code-background: #ede9fe; --bit-AiMarkdownView-block-spacing: 0.5rem;"">
    <BitAiMarkdownView Document=""styleDocument"" />
</div>";
    private readonly string example13CsharpCode = @"
private const string styleMarkdown = """"""
    #### A styled answer

    Some prose with a [link](https://bitplatform.dev) and `inline code`.

    - a first point
    - a second point

    > A quote.
    """""";

private readonly BitAiMarkdownDocument styleDocument = BitAiMarkdownParser.Parse(styleMarkdown);";

    private readonly string example14RazorCode = @"
<BitAiMarkdownView Dir=""BitDir.Rtl"" Document=""rtlDocument"" />";
    private readonly string example14CsharpCode = @"
private const string rtlMarkdown = """"""
    #### دو حالت رندر

    بلیزر روی **سرور** یا در **مرورگر** رندر می‌کند:

    - روی سرور، از راه یک اتصال؛
    - در مرورگر، روی WebAssembly.

    > رندر بیشتر از آنچه چشم می‌بیند، روان‌تر نیست.

    | حالت | اجرا | نخستین نمایش |
    | --- | --- | ---: |
    | سرور | روی سرور | سریع |
    | وب‌اسمبلی | در مرورگر | کندتر |
    """""";

private readonly BitAiMarkdownDocument rtlDocument = BitAiMarkdownParser.Parse(rtlMarkdown);";
}

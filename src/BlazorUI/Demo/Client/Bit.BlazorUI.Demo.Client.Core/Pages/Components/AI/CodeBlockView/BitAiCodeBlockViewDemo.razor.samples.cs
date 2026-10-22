namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.CodeBlockView;

public partial class BitAiCodeBlockViewDemo
{
    private readonly string example1RazorCode = @"
<BitAiCodeBlockView Language=""csharp"" Code=""@csharpCode"" />

<BitAiCodeBlockView Code=""A block with no language is plain text."" />";
    private readonly string example1CsharpCode = @"
private const string csharpCode = """"""
    using Bit.BlazorUI;

    // Registers the AI components with a transport of your own.
    builder.Services.AddBitBlazorUIAIServices(ai => ai.UseTransport<MyTransport>());

    var app = builder.Build();
    app.Run();
    """""";";

    private readonly string example2RazorCode = @"
<BitAiCodeBlockView FileName=""appsettings.json"" Language=""json"" Code=""@jsonCode"" />";
    private readonly string example2CsharpCode = @"
private const string jsonCode = """"""
    {
      ""Logging"": { ""LogLevel"": { ""Default"": ""Information"" } },
      ""AllowedHosts"": ""*"",
      ""Ai"": { ""Model"": ""demo-model-1"", ""MaxTokens"": 4096, ""Stream"": true }
    }
    """""";";

    private readonly string example3RazorCode = @"
<BitAiCodeBlockView ShowHeader=""false"" Language=""bash"" Code=""dotnet add package Bit.BlazorUI.AI"" />";

    private readonly string example4RazorCode = @"
@implements IAsyncDisposable

<BitButton OnClick=""StreamAsync"" Disabled=""isStreaming"" IconName=""@BitIconName.Play"">Stream the code</BitButton>

<BitAiCodeBlockView Language=""python"" Code=""@streamedCode"" IsStreaming=""isStreaming"" />";
    private readonly string example4CsharpCode = @"
private const string pythonCode = """"""
    def fibonacci(n: int) -> list[int]:
        # The first n Fibonacci numbers.
        numbers = [0, 1]
        while len(numbers) < n:
            numbers.append(numbers[-1] + numbers[-2])
        return numbers[:n]

    print(fibonacci(10))
    """""";

private string streamedCode = string.Empty;
private bool isStreaming;
private CancellationTokenSource? streamCts;
private BitAiRenderScheduler? scheduler;

private async Task StreamAsync()
{
    streamCts = new CancellationTokenSource();
    scheduler ??= new BitAiRenderScheduler(() => InvokeAsync(StateHasChanged));

    streamedCode = string.Empty;
    isStreaming = true;

    try
    {
        for (var i = 0; i < pythonCode.Length; i += 3)
        {
            await Task.Delay(30, streamCts.Token);

            streamedCode += pythonCode.Substring(i, Math.Min(3, pythonCode.Length - i));
            scheduler.Request();
        }
    }
    catch (OperationCanceledException) { }
    finally
    {
        isStreaming = false;
        await scheduler.FlushAsync();
    }
}

public async ValueTask DisposeAsync()
{
    streamCts?.Cancel();
    if (scheduler is not null) await scheduler.DisposeAsync();
}";

    private readonly string example5RazorCode = @"
<BitToggle @bind-Value=""showLineNumbers"" Text=""ShowLineNumbers"" />
<BitToggle @bind-Value=""showCodeCopy"" Text=""ShowCodeCopy"" />

<BitAiOptionsProvider UiOptions=""@codeOptions"">
    <BitAiCodeBlockView Language=""csharp"" Code=""@csharpCode"" />
</BitAiOptionsProvider>";
    private readonly string example5CsharpCode = @"
private const string csharpCode = """"""
    using Bit.BlazorUI;

    // Registers the AI components with a transport of your own.
    builder.Services.AddBitBlazorUIAIServices(ai => ai.UseTransport<MyTransport>());

    var app = builder.Build();
    app.Run();
    """""";

private bool showLineNumbers = true;
private bool showCodeCopy = true;
private BitAiUiOptions codeOptions => BitAiUiOptions.Default with { ShowLineNumbers = showLineNumbers, ShowCodeCopy = showCodeCopy };";

    private readonly string example6RazorCode = @"
<BitDropdown Label=""Language"" Items=""languageItems"" @bind-Value=""selectedLanguage"" Style=""max-width: 16rem;"" />

<div>Aliases: <b>@SelectedAliases</b></div>

<BitAiCodeBlockView Language=""@selectedLanguage"" Code=""@SelectedSample"" />";
    private readonly string example6CsharpCode = @"
private const string pythonCode = """"""
    def fibonacci(n: int) -> list[int]:
        # The first n Fibonacci numbers.
        numbers = [0, 1]
        while len(numbers) < n:
            numbers.append(numbers[-1] + numbers[-2])
        return numbers[:n]

    print(fibonacci(10))
    """""";

private const string jsonCode = """"""
    {
      ""Logging"": { ""LogLevel"": { ""Default"": ""Information"" } },
      ""AllowedHosts"": ""*"",
      ""Ai"": { ""Model"": ""demo-model-1"", ""MaxTokens"": 4096, ""Stream"": true }
    }
    """""";

private readonly List<BitDropdownItem<string>> languageItems =
    [.. BitAiBuiltInLanguages.All.Select(language => new BitDropdownItem<string> { Text = language.Id, Value = language.Id })];

private string? selectedLanguage = ""csharp"";

private string SelectedAliases => string.Join("", "", BitAiLanguageRegistry.Find(selectedLanguage)?.Aliases ?? []);

private string SelectedSample => languageSamples.GetValueOrDefault(selectedLanguage ?? string.Empty, string.Empty);

private static readonly Dictionary<string, string> languageSamples = new()
{
    [""csharp""] = """"""
        [Authorize]
        public sealed record Message(string Author, string Text, DateTimeOffset At)
        {
            public bool IsRecent => DateTimeOffset.UtcNow - At < TimeSpan.FromMinutes(5);
        }
        """""",
    [""javascript""] = """"""
        // Debounces a function by the given delay.
        export function debounce(fn, delay = 250) {
          let timer;
          return (...args) => {
            clearTimeout(timer);
            timer = setTimeout(() => fn(...args), delay);
          };
        }
        """""",
    [""python""] = pythonCode,
    [""json""] = jsonCode,
    [""html""] = """"""
        <!-- A form the model proposed -->
        <form method=""post"" action=""/subscribe"">
          <label for=""email"">Email</label>
          <input id=""email"" type=""email"" required>
          <button type=""submit"">Subscribe</button>
        </form>
        """""",
    [""css""] = """"""
        .card:hover {
          color: var(--bit-clr-pri);
          box-shadow: 0 2px 8px rgb(0 0 0 / 0.2) !important;
          background: url(https://example.com/bg.png) no-repeat;
        }
        """""",
    [""sql""] = """"""
        -- The ten most active users this month.
        SELECT u.name, COUNT(*) AS messages
        FROM users u
        JOIN messages m ON m.user_id = u.id
        WHERE m.created_at >= '2026-10-01'
        GROUP BY u.name
        ORDER BY messages DESC
        LIMIT 10;
        """""",
    [""bash""] = """"""
        #!/usr/bin/env bash
        set -euo pipefail
        for project in src/*/; do
          echo ""Building $project""
          dotnet build ""$project"" --configuration Release
        done
        """""",
    [""powershell""] = """"""
        # Lists the largest files under a folder.
        $root = Join-Path $env:USERPROFILE ""Downloads""
        Get-ChildItem -Path $root -Recurse -File |
            Where-Object { $_.Length -gt 10MB } |
            Sort-Object Length -Descending
        """""",
    [""go""] = """"""
        package main

        import ""fmt""

        func main() {
            for i := 0; i < 3; i++ {
                fmt.Println(""tick"", i)
            }
        }
        """""",
    [""rust""] = """"""
        #[derive(Debug)]
        struct Token<'a> { text: &'a str }

        fn longest<'a>(x: &'a str, y: &'a str) -> &'a str {
            if x.len() > y.len() { x } else { y }
        }
        """""",
    [""java""] = """"""
        @Override
        public String toString() {
            // A readable form for the logs.
            return String.format(""User[id=%d, name=%s]"", id, name);
        }
        """""",
    [""yaml""] = """"""
        # A GitHub Actions job.
        build:
          runs-on: ubuntu-latest
          steps:
            - uses: actions/checkout@v4
            - run: dotnet test --no-restore
              continue-on-error: false
        """""",
    [""toml""] = """"""
        # Project settings.
        [package]
        name = ""chat""
        version = ""0.3.1""
        publish = false
        """""",
    [""diff""] = """"""
        --- a/Program.cs
        +++ b/Program.cs
        @@ -1,3 +1,3 @@
        -builder.Services.AddBitBlazorUIServices();
        +builder.Services.AddBitBlazorUIAIServices();
         var app = builder.Build();
        """""",
};";

    private readonly string example7RazorCode = @"
<BitAiCodeBlockView Language=""graphql"" Code=""@graphQlCode"" />";
    private readonly string example7CsharpCode = @"
// An app registers its languages once, at startup (in Program.cs, beside AddBitBlazorUIAIServices).
private static readonly BitAiLanguageDefinition graphQl = new(""graphql"")
{
    Aliases = [""gql""],
    Keywords = new HashSet<string>([""query"", ""mutation"", ""subscription"", ""fragment"", ""on"", ""type"", ""input"", ""enum"", ""schema"", ""extend"", ""implements"", ""interface"", ""union"", ""scalar"", ""directive""], StringComparer.Ordinal),
    Types = new HashSet<string>([""ID"", ""String"", ""Int"", ""Float"", ""Boolean""], StringComparer.Ordinal),
    Constants = new HashSet<string>([""true"", ""false"", ""null""], StringComparer.Ordinal),
    LineComments = [""#""],
    BlockComments = [],
    StringDelimiters = ['""'],
    AnnotationPrefix = '@',
};

private const string graphQlCode = """"""
    # The conversations of the signed-in user.
    query Conversations($first: Int = 20) @cached(ttl: 60) {
      conversations(first: $first, archived: false) {
        id
        title
        messages(last: 1) { text createdAt }
      }
    }
    """""";

protected override void OnInitialized()
{
    BitAiLanguageRegistry.Register(graphQl);
}";

    private readonly string example8RazorCode = @"
<BitToggle @bind-Value=""customPalette"" Text=""A palette of my own"" />

<div style=""@(customPalette ? paletteStyle : null)"">
    <BitAiCodeBlockView Language=""csharp"" Code=""@tokenCsharpCode"" />
    <BitAiCodeBlockView Language=""html"" Code=""@tokenHtmlCode"" />
    <BitAiCodeBlockView Language=""json"" Code=""@tokenJsonCode"" />
    <BitAiCodeBlockView Language=""diff"" Code=""@tokenDiffCode"" />
    <BitAiCodeBlockView Language=""rust"" Code=""@tokenRustCode"" />
    <BitAiCodeBlockView Language=""powershell"" Code=""@tokenPowerShellCode"" />
</div>";
    private readonly string example8CsharpCode = @"
private bool customPalette;

private const string paletteStyle =
    ""--bit-AiCodeBlockView-background: #1e1e2e; --bit-AiCodeBlockView-color: #cdd6f4; "" +
    ""--bit-AiCodeBlockView-header-background: #181825; --bit-AiCodeBlockView-header-color: #a6adc8; "" +
    ""--bit-AiCodeBlockView-border-color: #313244; --bit-AiCodeBlockView-line-number-color: #6c7086; "" +
    ""--bit-AiCodeBlockView-keyword-color: #cba6f7; --bit-AiCodeBlockView-type-color: #f9e2af; "" +
    ""--bit-AiCodeBlockView-string-color: #a6e3a1; --bit-AiCodeBlockView-number-color: #fab387; "" +
    ""--bit-AiCodeBlockView-comment-color: #7f849c; --bit-AiCodeBlockView-function-color: #89b4fa; "" +
    ""--bit-AiCodeBlockView-operator-color: #89dceb; --bit-AiCodeBlockView-punctuation-color: #9399b2; "" +
    ""--bit-AiCodeBlockView-property-color: #74c7ec; --bit-AiCodeBlockView-tag-color: #f38ba8; "" +
    ""--bit-AiCodeBlockView-attribute-color: #f9e2af; --bit-AiCodeBlockView-constant-color: #fab387; "" +
    ""--bit-AiCodeBlockView-inserted-color: #a6e3a1; --bit-AiCodeBlockView-deleted-color: #f38ba8; "" +
    ""--bit-AiCodeBlockView-meta-color: #7f849c; --bit-AiCodeBlockView-label-color: #f5c2e7; "" +
    ""--bit-AiCodeBlockView-variable-color: #f2cdcd;"";

private const string tokenCsharpCode = """"""
    [Obsolete(""Use CountAsync"")]
    public static int Count(string path, bool recursive = false)
    {
        // Keywords, types, strings, numbers, comments, functions, operators and constants.
        var total = Directory.GetFiles(path).Length + 0x10;
        return recursive ? total * 2 : total;
    }
    """""";

private const string tokenHtmlCode = """"""
    <button type=""submit"" class=""primary"" disabled>Send</button>
    """""";

private const string tokenJsonCode = """"""
    { ""name"": ""bit"", ""stars"": 1200, ""archived"": false, ""license"": null }
    """""";

private const string tokenDiffCode = """"""
    --- a/appsettings.json
    +++ b/appsettings.json
    @@ -2 +2 @@
    -  ""Model"": ""demo-model-1"",
    +  ""Model"": ""demo-model-2"",
    """""";

private const string tokenRustCode = """"""
    fn first<'a>(items: &'a [String]) -> Option<&'a String> {
        'outer: for item in items { break 'outer; }
        items.first()
    }
    """""";

private const string tokenPowerShellCode = """"""
    $files = Get-ChildItem -Path $env:TEMP -Recurse | Where-Object { $_.Length -gt 1MB }
    """""";";

    private readonly string example9RazorCode = @"
<BitParams Parameters=""codeBlockParams"">
    <BitAiCodeBlockView Language=""bash"" Code=""dotnet new blazor -o MyChat"" />
    <BitAiCodeBlockView Language=""bash"" Code=""dotnet add package Bit.BlazorUI.AI"" />
    <BitAiCodeBlockView Language=""bash"" Code=""dotnet run"" ShowHeader=""true"" />
</BitParams>";
    private readonly string example9CsharpCode = @"
private readonly BitAiCodeBlockViewParams[] codeBlockParams = [new() { ShowHeader = false }];";

    private readonly string example10RazorCode = @"
<style>
    .custom-class {
        border-width: 2px;
        border-style: dashed;
        border-color: tomato;
    }

    .custom-root {
        border-radius: 0;
        border-inline-start: 4px solid rebeccapurple;
    }

    .custom-label {
        color: rebeccapurple;
        font-weight: bold;
    }

    .custom-pre {
        line-height: 2;
    }
</style>

<BitAiCodeBlockView Language=""bash"" Code=""@styleCode"" Style=""box-shadow: 0 0 0.5rem dodgerblue;"" />

<BitAiCodeBlockView Language=""bash"" Code=""@styleCode"" Class=""custom-class"" />

<BitAiCodeBlockView Language=""bash"" Code=""@styleCode""
                    Styles=""@(new() { Header = ""background-color: tomato; color: white;"", Code = ""font-size: 1rem;"" })"" />

<BitAiCodeBlockView Language=""bash"" Code=""@styleCode""
                    Classes=""@(new() { Root = ""custom-root"", Label = ""custom-label"", Pre = ""custom-pre"" })"" />

<div style=""--bit-AiCodeBlockView-background: #0f172a; --bit-AiCodeBlockView-color: #e2e8f0; --bit-AiCodeBlockView-header-background: #1e293b; --bit-AiCodeBlockView-header-color: #94a3b8; --bit-AiCodeBlockView-border-color: #334155; --bit-AiCodeBlockView-radius: 1rem;"">
    <BitAiCodeBlockView Language=""bash"" Code=""@styleCode"" />
</div>";
    private readonly string example10CsharpCode = @"
private const string styleCode = ""dotnet add package Bit.BlazorUI.AI"";";

    private readonly string example11RazorCode = @"
<BitAiCodeBlockView Dir=""BitDir.Rtl"" FileName=""مثال.cs"" Language=""csharp"" Code=""@rtlCode"" />";
    private readonly string example11CsharpCode = @"
private const string rtlCode = """"""
    // سلام دنیا
    Console.WriteLine(""سلام دنیا"");
    """""";";
}

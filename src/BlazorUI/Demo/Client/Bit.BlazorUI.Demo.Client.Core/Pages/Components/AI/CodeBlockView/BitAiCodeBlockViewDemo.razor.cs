namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.CodeBlockView;

public partial class BitAiCodeBlockViewDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Classes",
            Type = "BitAiCodeBlockViewClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the code block.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Code",
            Type = "string",
            DefaultValue = "string.Empty",
            Description = "The source to display. It is rendered as text, never as markup, and it is what the copy button copies - exactly, without the line numbers.",
        },
        new()
        {
            Name = "FileName",
            Type = "string?",
            DefaultValue = "null",
            Description = "A file name shown in the header in place of the language (which still picks the highlighter).",
        },
        new()
        {
            Name = "IsStreaming",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the block is still receiving content, which holds the highlighting back: the code is shown as plain text while it is true, and highlighted once when it turns false.",
        },
        new()
        {
            Name = "Language",
            Type = "string?",
            DefaultValue = "null",
            Description = "The language identifier used to pick a highlighter (an id or alias of BitAiLanguageRegistry, matched without regard to case; an unknown one is plain text), also shown in the header when there is no file name - text when it is null too.",
            LinkType = LinkType.Link,
            Href = "#built-in-languages",
        },
        new()
        {
            Name = "ShowHeader",
            Type = "bool",
            DefaultValue = "true",
            Description = "Shows the header bar with the language (or file name) and the copy button. The copy button also needs BitAiUiOptions.ShowCodeCopy, which is on by default.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAiCodeBlockViewClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the code block.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitAiCodeBlockViewClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitAiCodeBlockView.",
                },
                new()
                {
                    Name = "Header",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the header bar of the BitAiCodeBlockView, which holds the language and the copy button.",
                },
                new()
                {
                    Name = "Label",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the language (or file name) label in the header of the BitAiCodeBlockView.",
                },
                new()
                {
                    Name = "CopyButton",
                    Type = "BitAiCopyButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the copy button (a BitAiCopyButton) in the header of the BitAiCodeBlockView.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/copybutton/#class-styles",
                },
                new()
                {
                    Name = "Body",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the scroll container of the code of the BitAiCodeBlockView.",
                },
                new()
                {
                    Name = "Pre",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the pre element of the BitAiCodeBlockView.",
                },
                new()
                {
                    Name = "Code",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the code element of the BitAiCodeBlockView (which also carries the class language-<Language>).",
                },
                new()
                {
                    Name = "Line",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for every numbered line of the BitAiCodeBlockView (rendered only while BitAiUiOptions.ShowLineNumbers is on).",
                },
            ]
        },
        new()
        {
            Id = "built-in-languages",
            Title = "BitAiBuiltInLanguages",
            Description = "The languages registered by default, chosen by what models write in chat: application languages, the data formats around them and the shell. Each is a static BitAiLanguageDefinition property, registered under its id and every alias; anything else falls back to plain text.",
            Parameters =
            [
                new() { Name = "CSharp", Type = "BitAiLanguageDefinition", DefaultValue = "", Description = "csharp - aliases cs, c#, dotnet." },
                new() { Name = "JavaScript", Type = "BitAiLanguageDefinition", DefaultValue = "", Description = "javascript, TypeScript included - aliases js, jsx, typescript, ts, tsx, mjs, cjs." },
                new() { Name = "Python", Type = "BitAiLanguageDefinition", DefaultValue = "", Description = "python - aliases py, python3." },
                new() { Name = "Json", Type = "BitAiLanguageDefinition", DefaultValue = "", Description = "json, keys highlighted as properties - aliases jsonc, json5." },
                new() { Name = "Html", Type = "BitAiLanguageDefinition", DefaultValue = "", Description = "html, with the tag-aware scanner - aliases xml, svg, xhtml, razor, cshtml, vue." },
                new() { Name = "Css", Type = "BitAiLanguageDefinition", DefaultValue = "", Description = "css - aliases scss, sass, less." },
                new() { Name = "Sql", Type = "BitAiLanguageDefinition", DefaultValue = "", Description = "sql, matched without regard to case - aliases postgresql, mysql, tsql, sqlite, plsql." },
                new() { Name = "Shell", Type = "BitAiLanguageDefinition", DefaultValue = "", Description = "bash - aliases sh, shell, zsh, console." },
                new() { Name = "PowerShell", Type = "BitAiLanguageDefinition", DefaultValue = "", Description = "powershell, with $variables and hyphenated commands - aliases ps1, pwsh, ps, posh." },
                new() { Name = "Go", Type = "BitAiLanguageDefinition", DefaultValue = "", Description = "go - alias golang." },
                new() { Name = "Rust", Type = "BitAiLanguageDefinition", DefaultValue = "", Description = "rust, with lifetimes and labels - alias rs." },
                new() { Name = "Java", Type = "BitAiLanguageDefinition", DefaultValue = "", Description = "java - aliases kotlin, kt, groovy, scala." },
                new() { Name = "Yaml", Type = "BitAiLanguageDefinition", DefaultValue = "", Description = "yaml - alias yml." },
                new() { Name = "Toml", Type = "BitAiLanguageDefinition", DefaultValue = "", Description = "toml - aliases ini, cfg, conf, properties, editorconfig." },
                new() { Name = "Diff", Type = "BitAiLanguageDefinition", DefaultValue = "", Description = "diff, with added, removed and header lines - alias patch." },
                new() { Name = "All", Type = "IReadOnlyList<BitAiLanguageDefinition>", DefaultValue = "", Description = "Every built-in language, in the order above." },
            ]
        },
        new()
        {
            Id = "language-registry",
            Title = "BitAiLanguageRegistry",
            Description = "The static registry of the languages the highlighter knows, keyed by id and alias without regard to case. It starts with BitAiBuiltInLanguages.All; register once, at startup.",
            Parameters =
            [
                new()
                {
                    Name = "Register(BitAiLanguageDefinition language)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Adds a language, or replaces the one of the same id - under every alias it was registered with too, so ```cs follows a replaced csharp.",
                },
                new()
                {
                    Name = "Find(string? name)",
                    Type = "BitAiLanguageDefinition?",
                    DefaultValue = "",
                    Description = "The language registered under an id or alias, or null.",
                },
                new()
                {
                    Name = "IsSupported(string? name)",
                    Type = "bool",
                    DefaultValue = "",
                    Description = "Whether a language is registered under the name, which is whether a block of it is highlighted.",
                },
                new()
                {
                    Name = "All",
                    Type = "IReadOnlyCollection<BitAiLanguageDefinition>",
                    DefaultValue = "",
                    Description = "The distinct registered languages, ordered by id.",
                },
            ]
        },
        new()
        {
            Id = "language-definition",
            Title = "BitAiLanguageDefinition",
            Description = "Describes a language well enough to highlight it: a lexical description rather than a grammar, which colors the fragments a chat holds correctly and never fails on incomplete input. Created with new BitAiLanguageDefinition(id), the rest set as init-only properties.",
            Parameters =
            [
                new() { Name = "Id", Type = "string", DefaultValue = "", Description = "The canonical identifier, matching the fence info string. Set through the constructor; it cannot be blank." },
                new() { Name = "Aliases", Type = "IReadOnlyList<string>", DefaultValue = "[]", Description = "Other names that select this language." },
                new() { Name = "Keywords", Type = "IReadOnlySet<string>", DefaultValue = "empty", Description = "Words highlighted as keywords (a hyphenated word too, where HyphenatedCommands is on)." },
                new() { Name = "Types", Type = "IReadOnlySet<string>", DefaultValue = "empty", Description = "Words highlighted as type names." },
                new() { Name = "Constants", Type = "IReadOnlySet<string>", DefaultValue = "empty", Description = "Words highlighted as constants, such as boolean and null literals." },
                new() { Name = "LineComments", Type = "IReadOnlyList<string>", DefaultValue = "[\"//\"]", Description = "Sequences that start a comment running to the end of the line." },
                new() { Name = "BlockComments", Type = "IReadOnlyList<(string Open, string Close)>", DefaultValue = "[(\"/*\", \"*/\")]", Description = "Paired sequences that delimit a block comment." },
                new() { Name = "StringDelimiters", Type = "IReadOnlyList<char>", DefaultValue = "['\"', '\\'']", Description = "Characters that delimit a string literal." },
                new() { Name = "StringEscapes", Type = "bool", DefaultValue = "true", Description = "Whether StringEscapeCharacter escapes the next character inside a string." },
                new() { Name = "StringEscapeCharacter", Type = "char", DefaultValue = "'\\\\'", Description = "The character that escapes the next one inside a string: a backslash in most languages, a backtick in PowerShell." },
                new() { Name = "MultiLineStrings", Type = "bool", DefaultValue = "false", Description = "Whether a string literal may span lines." },
                new() { Name = "IsMarkup", Type = "bool", DefaultValue = "false", Description = "Whether the language is markup, which switches to the tag-aware scanner (tags, attributes, attribute values)." },
                new() { Name = "ColonProperties", Type = "bool", DefaultValue = "false", Description = "Whether identifiers before a colon are highlighted as properties, as in JSON and YAML." },
                new() { Name = "IgnoreCase", Type = "bool", DefaultValue = "false", Description = "Whether words are matched case-insensitively, as in SQL." },
                new() { Name = "AnnotationPrefix", Type = "char?", DefaultValue = "null", Description = "The character that introduces an annotation or attribute, such as @. A [ only counts at the start of a line, as C# attributes are written; a # may be followed by [ or ![, as Rust attributes are." },
                new() { Name = "Lifetimes", Type = "bool", DefaultValue = "false", Description = "Whether an apostrophe before a name is a lifetime or label rather than an unterminated character literal, as in Rust's 'a and 'outer:." },
                new() { Name = "UrlFunctions", Type = "bool", DefaultValue = "false", Description = "Whether url(...) holds a string even when unquoted, as in CSS, so the // of an address is not read as a comment." },
                new() { Name = "VariablePrefix", Type = "char?", DefaultValue = "null", Description = "The character that introduces a variable, such as PowerShell's $: the sigil and the name after it, or a braced name such as ${my var}, are one variable token." },
                new() { Name = "HyphenatedCommands", Type = "bool", DefaultValue = "false", Description = "Whether hyphenated words are commands and switches, as in PowerShell: Get-ChildItem is a function and -Recurse a parameter, while a hyphenated word listed in Keywords, such as -eq, is a keyword." },
            ]
        },
        new()
        {
            Id = "syntax-highlighter",
            Title = "BitAiSyntaxHighlighter",
            Description = "The static highlighter the code block uses, for an app that colors code anywhere else.",
            Parameters =
            [
                new()
                {
                    Name = "Highlight(string? code, string? language)",
                    Type = "IReadOnlyList<BitAiCodeToken>",
                    DefaultValue = "",
                    Description = "The tokens of the code in the language registered under that id or alias (BitAiLanguageRegistry), in order; none for an empty code or a language that is not registered. It never fails on incomplete code.",
                    LinkType = LinkType.Link,
                    Href = "#code-token",
                },
            ]
        },
        new()
        {
            Id = "code-token",
            Title = "BitAiCodeToken",
            Description = "One highlighted span of the code, a readonly record struct: offsets rather than substrings, so highlighting a large file allocates one list rather than a string per token.",
            Parameters =
            [
                new() { Name = "Kind", Type = "BitAiTokenKind", DefaultValue = "", Description = "The category of the token, which selects its CSS class.", LinkType = LinkType.Link, Href = "#token-kind-enum" },
                new() { Name = "Start", Type = "int", DefaultValue = "", Description = "Where the token starts in the code, in UTF-16 code units." },
                new() { Name = "Length", Type = "int", DefaultValue = "", Description = "How long the token is." },
                new() { Name = "Slice(string source)", Type = "ReadOnlySpan<char>", DefaultValue = "", Description = "The text of the token, read from the code it was produced from." },
            ]
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "token-kind-enum",
            Name = "BitAiTokenKind",
            Description = "The category of a highlighted token, which picks its CSS class and the public variable that colors it.",
            Items =
            [
                new() { Name = "Plain", Value = "0", Description = "Text with no special meaning: no span of its own, the color of the code." },
                new() { Name = "Keyword", Value = "1", Description = "A language keyword: bit-aicbv-kwd, --bit-AiCodeBlockView-keyword-color." },
                new() { Name = "Type", Value = "2", Description = "A built-in or well-known type name: bit-aicbv-typ, --bit-AiCodeBlockView-type-color." },
                new() { Name = "String", Value = "3", Description = "A string literal, including interpolated ones: bit-aicbv-str, --bit-AiCodeBlockView-string-color." },
                new() { Name = "Number", Value = "4", Description = "A numeric literal: bit-aicbv-num, --bit-AiCodeBlockView-number-color." },
                new() { Name = "Comment", Value = "5", Description = "A comment, set in italics: bit-aicbv-cmt, --bit-AiCodeBlockView-comment-color." },
                new() { Name = "Operator", Value = "6", Description = "An operator: bit-aicbv-opr, --bit-AiCodeBlockView-operator-color." },
                new() { Name = "Punctuation", Value = "7", Description = "Brackets, commas and semicolons: bit-aicbv-pnc, --bit-AiCodeBlockView-punctuation-color." },
                new() { Name = "Function", Value = "8", Description = "An identifier in call position, a command: bit-aicbv-fnc, --bit-AiCodeBlockView-function-color." },
                new() { Name = "Property", Value = "9", Description = "A property or field name, including a JSON key: bit-aicbv-prp, --bit-AiCodeBlockView-property-color." },
                new() { Name = "Tag", Value = "10", Description = "A markup tag name: bit-aicbv-tag, --bit-AiCodeBlockView-tag-color." },
                new() { Name = "Attribute", Value = "11", Description = "A markup attribute name, or an annotation: bit-aicbv-atr, --bit-AiCodeBlockView-attribute-color." },
                new() { Name = "Constant", Value = "12", Description = "A language constant such as a boolean or null literal: bit-aicbv-cst, --bit-AiCodeBlockView-constant-color." },
                new() { Name = "Inserted", Value = "13", Description = "An added line in a diff: bit-aicbv-ins, --bit-AiCodeBlockView-inserted-color." },
                new() { Name = "Deleted", Value = "14", Description = "A removed line in a diff: bit-aicbv-del, --bit-AiCodeBlockView-deleted-color." },
                new() { Name = "Meta", Value = "15", Description = "A file header or hunk marker in a diff: bit-aicbv-met, --bit-AiCodeBlockView-meta-color." },
                new() { Name = "Label", Value = "16", Description = "A lifetime or loop label, such as Rust's 'a or 'outer, set in italics: bit-aicbv-lab, --bit-AiCodeBlockView-label-color." },
                new() { Name = "Variable", Value = "17", Description = "A variable introduced by a sigil, such as PowerShell's $name: bit-aicbv-vrb, --bit-AiCodeBlockView-variable-color." },
            ]
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-AiCodeBlockView-background", DefaultValue = "4% of var(--bit-clr-fg-pri)", Description = "Background of the block: a translucent wash of the text color, so it reads on whatever the message sits on." },
        new() { Name = "--bit-AiCodeBlockView-color", DefaultValue = "inherit", Description = "Text color of the code (and of the plain text between the tokens)." },
        new() { Name = "--bit-AiCodeBlockView-border-color", DefaultValue = "var(--bit-clr-brd-sec)", Description = "Border of the block and the line under its header." },
        new() { Name = "--bit-AiCodeBlockView-radius", DefaultValue = "var(--bit-shp-radius-surface)", Description = "Corner radius of the block." },
        new() { Name = "--bit-AiCodeBlockView-header-background", DefaultValue = "4% of var(--bit-clr-fg-pri)", Description = "Background of the header bar." },
        new() { Name = "--bit-AiCodeBlockView-header-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "Text color of the header bar." },
        new() { Name = "--bit-AiCodeBlockView-padding", DefaultValue = "spacing(1.5)", Description = "Padding around the code." },
        new() { Name = "--bit-AiCodeBlockView-font-family", DefaultValue = "var(--bit-tpg-font-family-mono)", Description = "Font of the code and of the header label." },
        new() { Name = "--bit-AiCodeBlockView-font-size", DefaultValue = "var(--bit-tpg-fs-sm)", Description = "Text size of the code." },
        new() { Name = "--bit-AiCodeBlockView-line-number-color", DefaultValue = "var(--bit-clr-fg-ter)", Description = "Color of the line numbers." },
        new() { Name = "--bit-AiCodeBlockView-keyword-color", DefaultValue = "var(--bit-clr-pri-fg)", Description = "Color of the keywords." },
        new() { Name = "--bit-AiCodeBlockView-type-color", DefaultValue = "var(--bit-clr-suc-fg)", Description = "Color of the type names." },
        new() { Name = "--bit-AiCodeBlockView-string-color", DefaultValue = "var(--bit-clr-swr-fg)", Description = "Color of the string and character literals." },
        new() { Name = "--bit-AiCodeBlockView-number-color", DefaultValue = "var(--bit-clr-sec-fg)", Description = "Color of the numeric literals." },
        new() { Name = "--bit-AiCodeBlockView-comment-color", DefaultValue = "color-mix(in srgb, var(--bit-clr-fg-ter), var(--bit-clr-fg-pri) 15%)", Description = "Color of the comments (set in italics). The tertiary text color has a little of the text color mixed in, which keeps it legible on the tint of the block." },
        new() { Name = "--bit-AiCodeBlockView-function-color", DefaultValue = "color-mix(in srgb, var(--bit-clr-wrn-fg), var(--bit-clr-fg-pri) 15%)", Description = "Color of the function and command names. The warning shade has a little of the text color mixed in, which keeps it legible on the tint of the block." },
        new() { Name = "--bit-AiCodeBlockView-operator-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "Color of the operators." },
        new() { Name = "--bit-AiCodeBlockView-punctuation-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "Color of the punctuation." },
        new() { Name = "--bit-AiCodeBlockView-property-color", DefaultValue = "var(--bit-clr-pri-fg)", Description = "Color of the keys and properties (JSON, YAML, CSS)." },
        new() { Name = "--bit-AiCodeBlockView-tag-color", DefaultValue = "var(--bit-clr-err-fg)", Description = "Color of the markup tags." },
        new() { Name = "--bit-AiCodeBlockView-attribute-color", DefaultValue = "var(--bit-clr-sec-fg)", Description = "Color of the markup attributes and the annotations." },
        new() { Name = "--bit-AiCodeBlockView-constant-color", DefaultValue = "var(--bit-clr-sec-fg)", Description = "Color of true, false, null and the other constants." },
        new() { Name = "--bit-AiCodeBlockView-inserted-color", DefaultValue = "var(--bit-clr-suc-fg)", Description = "Color of the lines a diff adds." },
        new() { Name = "--bit-AiCodeBlockView-deleted-color", DefaultValue = "var(--bit-clr-err-fg)", Description = "Color of the lines a diff removes." },
        new() { Name = "--bit-AiCodeBlockView-meta-color", DefaultValue = "color-mix(in srgb, var(--bit-clr-fg-ter), var(--bit-clr-fg-pri) 15%)", Description = "Color of the file headers and the hunk markers of a diff. The tertiary text color has a little of the text color mixed in, as the comments' has." },
        new() { Name = "--bit-AiCodeBlockView-label-color", DefaultValue = "the type color", Description = "Color of the lifetimes and loop labels (set in italics); it falls back to --bit-AiCodeBlockView-type-color." },
        new() { Name = "--bit-AiCodeBlockView-variable-color", DefaultValue = "the property color", Description = "Color of the variables written with a sigil ($name); it falls back to --bit-AiCodeBlockView-property-color." },
    ];



    private const string csharpCode = """
        using Bit.BlazorUI;

        // Registers the AI components with a transport of your own.
        builder.Services.AddBitBlazorUIAIServices(ai => ai.UseTransport<MyTransport>());

        var app = builder.Build();
        app.Run();
        """;

    private const string jsonCode = """
        {
          "Logging": { "LogLevel": { "Default": "Information" } },
          "AllowedHosts": "*",
          "Ai": { "Model": "demo-model-1", "MaxTokens": 4096, "Stream": true }
        }
        """;

    private const string pythonCode = """
        def fibonacci(n: int) -> list[int]:
            # The first n Fibonacci numbers.
            numbers = [0, 1]
            while len(numbers) < n:
                numbers.append(numbers[-1] + numbers[-2])
            return numbers[:n]

        print(fibonacci(10))
        """;

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
    }

    private bool showLineNumbers = true;
    private bool showCodeCopy = true;
    private BitAiUiOptions codeOptions => BitAiUiOptions.Default with { ShowLineNumbers = showLineNumbers, ShowCodeCopy = showCodeCopy };

    private readonly List<BitDropdownItem<string>> languageItems =
        [.. BitAiBuiltInLanguages.All.Select(language => new BitDropdownItem<string> { Text = language.Id, Value = language.Id })];

    private string? selectedLanguage = "csharp";

    private string SelectedAliases => string.Join(", ", BitAiLanguageRegistry.Find(selectedLanguage)?.Aliases ?? []);

    private string SelectedSample => languageSamples.GetValueOrDefault(selectedLanguage ?? string.Empty, string.Empty);

    private static readonly Dictionary<string, string> languageSamples = new()
    {
        ["csharp"] = """
            [Authorize]
            public sealed record Message(string Author, string Text, DateTimeOffset At)
            {
                public bool IsRecent => DateTimeOffset.UtcNow - At < TimeSpan.FromMinutes(5);
            }
            """,
        ["javascript"] = """
            // Debounces a function by the given delay.
            export function debounce(fn, delay = 250) {
              let timer;
              return (...args) => {
                clearTimeout(timer);
                timer = setTimeout(() => fn(...args), delay);
              };
            }
            """,
        ["python"] = pythonCode,
        ["json"] = jsonCode,
        ["html"] = """
            <!-- A form the model proposed -->
            <form method="post" action="/subscribe">
              <label for="email">Email</label>
              <input id="email" type="email" required>
              <button type="submit">Subscribe</button>
            </form>
            """,
        ["css"] = """
            .card:hover {
              color: var(--bit-clr-pri);
              box-shadow: 0 2px 8px rgb(0 0 0 / 0.2) !important;
              background: url(https://example.com/bg.png) no-repeat;
            }
            """,
        ["sql"] = """
            -- The ten most active users this month.
            SELECT u.name, COUNT(*) AS messages
            FROM users u
            JOIN messages m ON m.user_id = u.id
            WHERE m.created_at >= '2026-10-01'
            GROUP BY u.name
            ORDER BY messages DESC
            LIMIT 10;
            """,
        ["bash"] = """
            #!/usr/bin/env bash
            set -euo pipefail
            for project in src/*/; do
              echo "Building $project"
              dotnet build "$project" --configuration Release
            done
            """,
        ["powershell"] = """
            # Lists the largest files under a folder.
            $root = Join-Path $env:USERPROFILE "Downloads"
            Get-ChildItem -Path $root -Recurse -File |
                Where-Object { $_.Length -gt 10MB } |
                Sort-Object Length -Descending
            """,
        ["go"] = """
            package main

            import "fmt"

            func main() {
                for i := 0; i < 3; i++ {
                    fmt.Println("tick", i)
                }
            }
            """,
        ["rust"] = """
            #[derive(Debug)]
            struct Token<'a> { text: &'a str }

            fn longest<'a>(x: &'a str, y: &'a str) -> &'a str {
                if x.len() > y.len() { x } else { y }
            }
            """,
        ["java"] = """
            @Override
            public String toString() {
                // A readable form for the logs.
                return String.format("User[id=%d, name=%s]", id, name);
            }
            """,
        ["yaml"] = """
            # A GitHub Actions job.
            build:
              runs-on: ubuntu-latest
              steps:
                - uses: actions/checkout@v4
                - run: dotnet test --no-restore
                  continue-on-error: false
            """,
        ["toml"] = """
            # Project settings.
            [package]
            name = "chat"
            version = "0.3.1"
            publish = false
            """,
        ["diff"] = """
            --- a/Program.cs
            +++ b/Program.cs
            @@ -1,3 +1,3 @@
            -builder.Services.AddBitBlazorUIServices();
            +builder.Services.AddBitBlazorUIAIServices();
             var app = builder.Build();
            """,
    };

    // An app registers its languages once, at startup (in Program.cs, beside AddBitBlazorUIAIServices).
    private static readonly BitAiLanguageDefinition graphQl = new("graphql")
    {
        Aliases = ["gql"],
        Keywords = new HashSet<string>(["query", "mutation", "subscription", "fragment", "on", "type", "input", "enum", "schema", "extend", "implements", "interface", "union", "scalar", "directive"], StringComparer.Ordinal),
        Types = new HashSet<string>(["ID", "String", "Int", "Float", "Boolean"], StringComparer.Ordinal),
        Constants = new HashSet<string>(["true", "false", "null"], StringComparer.Ordinal),
        LineComments = ["#"],
        BlockComments = [],
        StringDelimiters = ['"'],
        AnnotationPrefix = '@',
    };

    private const string graphQlCode = """
        # The conversations of the signed-in user.
        query Conversations($first: Int = 20) @cached(ttl: 60) {
          conversations(first: $first, archived: false) {
            id
            title
            messages(last: 1) { text createdAt }
          }
        }
        """;

    protected override void OnInitialized()
    {
        BitAiLanguageRegistry.Register(graphQl);
    }

    private bool customPalette;

    private const string paletteStyle =
        "--bit-AiCodeBlockView-background: #1e1e2e; --bit-AiCodeBlockView-color: #cdd6f4; " +
        "--bit-AiCodeBlockView-header-background: #181825; --bit-AiCodeBlockView-header-color: #a6adc8; " +
        "--bit-AiCodeBlockView-border-color: #313244; --bit-AiCodeBlockView-line-number-color: #6c7086; " +
        "--bit-AiCodeBlockView-keyword-color: #cba6f7; --bit-AiCodeBlockView-type-color: #f9e2af; " +
        "--bit-AiCodeBlockView-string-color: #a6e3a1; --bit-AiCodeBlockView-number-color: #fab387; " +
        "--bit-AiCodeBlockView-comment-color: #7f849c; --bit-AiCodeBlockView-function-color: #89b4fa; " +
        "--bit-AiCodeBlockView-operator-color: #89dceb; --bit-AiCodeBlockView-punctuation-color: #9399b2; " +
        "--bit-AiCodeBlockView-property-color: #74c7ec; --bit-AiCodeBlockView-tag-color: #f38ba8; " +
        "--bit-AiCodeBlockView-attribute-color: #f9e2af; --bit-AiCodeBlockView-constant-color: #fab387; " +
        "--bit-AiCodeBlockView-inserted-color: #a6e3a1; --bit-AiCodeBlockView-deleted-color: #f38ba8; " +
        "--bit-AiCodeBlockView-meta-color: #7f849c; --bit-AiCodeBlockView-label-color: #f5c2e7; " +
        "--bit-AiCodeBlockView-variable-color: #f2cdcd;";

    private const string tokenCsharpCode = """
        [Obsolete("Use CountAsync")]
        public static int Count(string path, bool recursive = false)
        {
            // Keywords, types, strings, numbers, comments, functions, operators and constants.
            var total = Directory.GetFiles(path).Length + 0x10;
            return recursive ? total * 2 : total;
        }
        """;

    private const string tokenHtmlCode = """
        <button type="submit" class="primary" disabled>Send</button>
        """;

    private const string tokenJsonCode = """
        { "name": "bit", "stars": 1200, "archived": false, "license": null }
        """;

    private const string tokenDiffCode = """
        --- a/appsettings.json
        +++ b/appsettings.json
        @@ -2 +2 @@
        -  "Model": "demo-model-1",
        +  "Model": "demo-model-2",
        """;

    private const string tokenRustCode = """
        fn first<'a>(items: &'a [String]) -> Option<&'a String> {
            'outer: for item in items { break 'outer; }
            items.first()
        }
        """;

    private const string tokenPowerShellCode = """
        $files = Get-ChildItem -Path $env:TEMP -Recurse | Where-Object { $_.Length -gt 1MB }
        """;

    private string? highlighterCode = "var total = Math.Max(count, 1) * 2; // never zero";

    private readonly BitAiCodeBlockViewParams[] codeBlockParams = [new() { ShowHeader = false }];

    private const string styleCode = "dotnet add package Bit.BlazorUI.AI";

    private const string rtlCode = """
        // سلام دنیا
        Console.WriteLine("سلام دنیا");
        """;
}

using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.ArtifactView;

public partial class BitAiArtifactViewDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Artifact",
            Type = "BitAiArtifact",
            DefaultValue = "null",
            Description = "The artifact to show (required). The panel follows it as it changes - a streaming artifact re-renders it at most once per render interval of the AI options - and picks its body by its Kind: the renderer the app registered for that kind, else the built-in one.",
            LinkType = LinkType.Link,
            Href = "#artifact",
        },
        new()
        {
            Name = "Classes",
            Type = "BitAiArtifactViewClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the artifact view.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "InitialVersion",
            Type = "int?",
            DefaultValue = "null",
            Description = "The version to show until the user picks another, for example the one a particular turn produced, so a later revision does not rewrite what an earlier turn appears to have said. The latest version is shown while it is unset, or when the artifact has no such version. Handing the panel another artifact drops the user's pick.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAiArtifactViewClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the artifact view.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitAiArtifactViewClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitAiArtifactView."
                },
                new()
                {
                    Name = "Overlay",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the overlay laid over the page behind the full-screen panel of the BitAiArtifactView."
                },
                new()
                {
                    Name = "Panel",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the panel - the header and the body - of the BitAiArtifactView."
                },
                new()
                {
                    Name = "Header",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the header of the BitAiArtifactView."
                },
                new()
                {
                    Name = "Icon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the icon of the kind of the artifact in the header of the BitAiArtifactView."
                },
                new()
                {
                    Name = "Title",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the title of the BitAiArtifactView."
                },
                new()
                {
                    Name = "Loading",
                    Type = "BitLoadingClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitEllipsisLoading the header of the BitAiArtifactView shows while the artifact streams.",
                    LinkType = LinkType.Link,
                    Href = "/components/loading/#class-styles"
                },
                new()
                {
                    Name = "VersionDropdown",
                    Type = "BitDropdownClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitDropdown that picks the version shown by the BitAiArtifactView.",
                    LinkType = LinkType.Link,
                    Href = "/components/dropdown/#class-styles"
                },
                new()
                {
                    Name = "DownloadButton",
                    Type = "BitButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the download BitButton of the BitAiArtifactView.",
                    LinkType = LinkType.Link,
                    Href = "/components/button/#button-class-styles"
                },
                new()
                {
                    Name = "ExpandButton",
                    Type = "BitButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the full-screen toggle BitButton of the BitAiArtifactView.",
                    LinkType = LinkType.Link,
                    Href = "/components/button/#button-class-styles"
                },
                new()
                {
                    Name = "Body",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the body of the BitAiArtifactView."
                },
                new()
                {
                    Name = "Content",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the container of a document or a JSON tree in the body of the BitAiArtifactView."
                },
                new()
                {
                    Name = "Image",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the image an image or an SVG artifact is shown as by the BitAiArtifactView."
                },
                new()
                {
                    Name = "ImagePlaceholder",
                    Type = "BitActionButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the button (a BitActionButton) the BitAiArtifactView shows in place of an image or an SVG artifact the image policy loads only on request.",
                    LinkType = LinkType.Link,
                    Href = "/components/actionbutton/#class-styles"
                },
                new()
                {
                    Name = "BlockedImage",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the title the BitAiArtifactView shows in place of an image or an SVG artifact the image policy blocks."
                },
                new()
                {
                    Name = "Frame",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the sandboxed frame an HTML artifact is previewed in by the BitAiArtifactView."
                },
                new()
                {
                    Name = "Table",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the box of the table a CSV or TSV artifact is shown as by the BitAiArtifactView."
                },
                new()
                {
                    Name = "HeaderCell",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each header cell of the table of a CSV or TSV artifact of the BitAiArtifactView."
                },
                new()
                {
                    Name = "Cell",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each body cell of the table of a CSV or TSV artifact of the BitAiArtifactView."
                },
            ]
        },
        new()
        {
            Id = "artifact",
            Title = "BitAiArtifact",
            Description = "A substantial piece of content produced alongside a conversation and revised across turns. It belongs to the conversation rather than to a message (BitAiConversation.Artifacts, GetOrAddArtifact, FindArtifact), so a rewrite three turns later updates one object with a version history instead of scattering near-identical copies through the transcript. A session creates and fills it from the artifact events of its transport; an app creates one to show content of its own. Safe for one writer and concurrent readers.",
            Parameters =
            [
                new()
                {
                    Name = "new(artifactId, kind, title)",
                    Type = "constructor",
                    DefaultValue = "",
                    Description = "Creates a streaming artifact at version 1; a null artifactId generates one. Append or SetContent its content, then Complete it."
                },
                new()
                {
                    Name = "ArtifactId",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The identifier, unique within a conversation."
                },
                new()
                {
                    Name = "Kind",
                    Type = "BitAiArtifactKind",
                    DefaultValue = "",
                    Description = "The kind of the artifact, which picks its renderer.",
                    LinkType = LinkType.Link,
                    Href = "#artifact-kind-enum"
                },
                new()
                {
                    Name = "Title",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The title in the header, which also names the downloaded file. Changed through SetTitle."
                },
                new()
                {
                    Name = "Content",
                    Type = "string?",
                    DefaultValue = "\"\"",
                    Description = "The content of the current version, as text."
                },
                new()
                {
                    Name = "Language",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "A language or format hint: the language code is highlighted in and downloaded as (csharp, typescript, python, ...), and tsv for a tab-separated table."
                },
                new()
                {
                    Name = "Url",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "An address for content that is not inline text, such as the source of an image."
                },
                new()
                {
                    Name = "Version",
                    Type = "int",
                    DefaultValue = "1",
                    Description = "The one-based number of the current version, incremented by every revision."
                },
                new()
                {
                    Name = "IsStreaming",
                    Type = "bool",
                    DefaultValue = "true",
                    Description = "Whether the current version is still being written: the panel shows its loading dots and previews an HTML page or an SVG only once it is false."
                },
                new()
                {
                    Name = "Versions",
                    Type = "IReadOnlyList<BitAiArtifactVersion>",
                    DefaultValue = "[]",
                    Description = "Every committed version, oldest first; the current draft is not included.",
                    LinkType = LinkType.Link,
                    Href = "#artifact-version"
                },
                new()
                {
                    Name = "Metadata",
                    Type = "IReadOnlyDictionary<string, object?>?",
                    DefaultValue = "null",
                    Description = "Application-defined values carried through to a registered artifact renderer."
                },
                new()
                {
                    Name = "CreatedAt / UpdatedAt",
                    Type = "DateTimeOffset",
                    DefaultValue = "",
                    Description = "When the artifact was created, and when its current version last changed."
                },
                new()
                {
                    Name = "Length / ChangeVersion",
                    Type = "int",
                    DefaultValue = "0",
                    Description = "How many characters the content holds, without materializing it; and a counter bumped on every change, for cheap render checks."
                },
                new()
                {
                    Name = "Append(delta)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Appends content to the current version, in linear time however long the artifact grows."
                },
                new()
                {
                    Name = "SetContent(content)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Replaces the content of the current version."
                },
                new()
                {
                    Name = "SetTitle(title)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Renames the artifact."
                },
                new()
                {
                    Name = "Complete(summary?)",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Commits the current content as a version, with an optional summary of what changed, and stops streaming. Completing it again records nothing new, but can add the summary the first call lacked."
                },
                new()
                {
                    Name = "BeginRevision()",
                    Type = "void",
                    DefaultValue = "",
                    Description = "Starts the next version, empty and streaming, completing the current one first if it is not; the previous version stays in Versions for the selector."
                },
                new()
                {
                    Name = "Restore(version)",
                    Type = "bool",
                    DefaultValue = "",
                    Description = "Brings the content of an earlier version back as a new version, summarized \"Restored version N\"; false when there is no such version."
                },
                new()
                {
                    Name = "Changed",
                    Type = "event Action<BitAiArtifact>",
                    DefaultValue = "",
                    Description = "Raised after every change, which is what the panel follows the artifact by."
                },
            ]
        },
        new()
        {
            Id = "artifact-version",
            Title = "BitAiArtifactVersion",
            Description = "An immutable snapshot of an artifact at one version, recorded by Complete - one entry of Versions and of the version selector.",
            Parameters =
            [
                new()
                {
                    Name = "Version",
                    Type = "int",
                    DefaultValue = "",
                    Description = "The one-based version number."
                },
                new()
                {
                    Name = "Content",
                    Type = "string?",
                    DefaultValue = "",
                    Description = "The content at this version, which the panel shows, copies and downloads when the version is picked."
                },
                new()
                {
                    Name = "CreatedAt",
                    Type = "DateTimeOffset",
                    DefaultValue = "",
                    Description = "When this version was committed."
                },
                new()
                {
                    Name = "Summary",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "An optional description of what changed, from Complete(summary) or the Summary of a BitAiArtifactCompletedEvent."
                },
            ]
        },
        new()
        {
            Id = "artifact-context",
            Title = "BitAiArtifactContext",
            Description = "What a renderer registered with BitAiRendererRegistry.RegisterArtifact(kind, context => ...) is handed, through ConfigureRenderers of AddBitBlazorUIAIServices. The renderer draws the body of every panel of that kind, inside a BitAiRendererBoundary.",
            Parameters =
            [
                new()
                {
                    Name = "Artifact",
                    Type = "BitAiArtifact",
                    DefaultValue = "",
                    Description = "The artifact being shown.",
                    LinkType = LinkType.Link,
                    Href = "#artifact"
                },
                new()
                {
                    Name = "IsExpanded",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Whether the panel is full screen, for a renderer that lays itself out to the room it has."
                },
            ]
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "artifact-kind-enum",
            Name = "BitAiArtifactKind",
            Description = "The kind of an artifact, which picks its renderer. A string-backed value type rather than an enum, compared ordinally and case-insensitively, so a transport can send - and an app register a renderer for - a kind the package does not know: new BitAiArtifactKind(\"mermaid\"), or a string assigned to it. A kind with neither a registered renderer nor a built-in rendering is shown as its code.",
            Items =
            [
                new() { Name = "Document", Value = "\"document\"", Description = "Prose in Markdown, rendered as streaming Markdown and downloaded as .md." },
                new() { Name = "Code", Value = "\"code\"", Description = "Source code, highlighted in its Language and downloaded with that language's extension." },
                new() { Name = "Html", Value = "\"html\"", Description = "A self-contained HTML page, previewed in a sandboxed frame once complete (shown as its code while it streams) and downloaded as .html." },
                new() { Name = "Json", Value = "\"json\"", Description = "A JSON value, rendered as a collapsible tree and downloaded as .json." },
                new() { Name = "Table", Value = "\"table\"", Description = "Tabular data: CSV (or TSV when the Language is tsv) as a table whose first row is the header, or a JSON array of objects as a BitAiDataTable; downloaded as .csv, .tsv or .json." },
                new() { Name = "Chart", Value = "\"chart\"", Description = "A chart specification, for a renderer the app registers; shown as its code otherwise." },
                new() { Name = "Image", Value = "\"image\"", Description = "A raster or vector image, shown from its Url or from a data URL as its content, as the image policy of the options decides." },
                new() { Name = "Svg", Value = "\"svg\"", Description = "SVG markup, shown through an img once complete - so no script inside it runs - as the image policy of the options decides, and downloaded as .svg." },
                new() { Name = "Diagram", Value = "\"diagram\"", Description = "A diagram description, for a renderer the app registers; shown as its code otherwise." },
            ]
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-AiArtifactView-background", DefaultValue = "var(--bit-clr-bg-pri)", Description = "Background of the panel." },
        new() { Name = "--bit-AiArtifactView-color", DefaultValue = "var(--bit-clr-fg-pri)", Description = "Color of the text of the panel." },
        new() { Name = "--bit-AiArtifactView-border-color", DefaultValue = "var(--bit-clr-brd-pri)", Description = "Color of the frame of the panel." },
        new() { Name = "--bit-AiArtifactView-radius", DefaultValue = "var(--bit-shp-radius-surface)", Description = "Corner of the panel." },
        new() { Name = "--bit-AiArtifactView-header-background", DefaultValue = "var(--bit-clr-bg-sec)", Description = "Background of the header." },
        new() { Name = "--bit-AiArtifactView-header-padding", DefaultValue = "spacing(1) spacing(1.5)", Description = "Inset of the header." },
        new() { Name = "--bit-AiArtifactView-header-gap", DefaultValue = "spacing(1)", Description = "Room between the parts of the header." },
        new() { Name = "--bit-AiArtifactView-divider-color", DefaultValue = "var(--bit-clr-brd-sec)", Description = "Color of the rule under the header." },
        new() { Name = "--bit-AiArtifactView-icon-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "Color of the icon of the kind beside the title." },
        new() { Name = "--bit-AiArtifactView-title-font-size", DefaultValue = "var(--bit-tpg-fs-sm)", Description = "Size of the title." },
        new() { Name = "--bit-AiArtifactView-title-font-weight", DefaultValue = "var(--bit-tpg-fw-semibold)", Description = "Weight of the title." },
        new() { Name = "--bit-AiArtifactView-max-height", DefaultValue = "spacing(64)", Description = "Height past which the body of an inline panel scrolls; the full-screen panel fills the screen instead." },
        new() { Name = "--bit-AiArtifactView-content-padding", DefaultValue = "spacing(1.5) spacing(2)", Description = "Inset of a document or a JSON tree in the body, and of what stands in for an image or an SVG artifact the image policy holds back (its placeholder button or its title)." },
        new() { Name = "--bit-AiArtifactView-muted-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "Color of the title shown in place of an image or an SVG artifact the image policy blocks." },
        new() { Name = "--bit-AiArtifactView-frame-height", DefaultValue = "spacing(52)", Description = "Height of the frame of an HTML page in an inline panel; the full-screen panel stretches it to the body." },
        new() { Name = "--bit-AiArtifactView-frame-background", DefaultValue = "white", Description = "Background of the frame of an HTML page - the white page a generated document expects, kept in a dark theme too." },
        new() { Name = "--bit-AiArtifactView-expanded-inset", DefaultValue = "spacing(2); none on a small screen", Description = "Room between the full-screen panel and the edges of the screen." },
        new() { Name = "--bit-AiArtifactView-overlay-background", DefaultValue = "var(--bit-clr-bg-overlay)", Description = "Background of the overlay behind the full-screen panel." },
        new() { Name = "--bit-AiArtifactView-table-divider-color", DefaultValue = "var(--bit-clr-brd-sec)", Description = "Color of the rules between the rows of a CSV or TSV table." },
        new() { Name = "--bit-AiArtifactView-table-header-background", DefaultValue = "var(--bit-clr-bg-sec)", Description = "Background of the header row of a CSV or TSV table." },
    ];



    // A finished artifact, as a session leaves it once the transport has completed it.
    private static BitAiArtifact Completed(BitAiArtifactKind kind, string title, string? content, string? language = null, string? url = null)
    {
        var artifact = new BitAiArtifact(null, kind, title) { Language = language, Url = url };
        if (content is not null) artifact.SetContent(content);
        artifact.Complete();
        return artifact;
    }

    private readonly BitAiArtifact programArtifact = Completed(BitAiArtifactKind.Code, "Retry.cs", """
        public static async Task<T> RetryAsync<T>(Func<Task<T>> action, int attempts = 3)
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    return await action();
                }
                catch (HttpRequestException) when (attempt < attempts)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
                }
            }
        }
        """, "csharp");

    private readonly BitAiArtifact documentArtifact = Completed(BitAiArtifactKind.Document, "Release notes", """
        # Release notes

        ## What is new

        - **Artifacts** keep a version history across turns.
        - **Structured output** fills in while the JSON is still streaming.

        | Area | Change |
        | --- | --- |
        | Chat | Faster streaming |
        | Forms | JSON Schema support |

        ```bash
        dotnet add package Bit.BlazorUI.AI
        ```
        """);

    private readonly BitAiArtifact pageArtifact = Completed(BitAiArtifactKind.Html, "Counter page", """
        <!doctype html>
        <html>
        <body style="font-family: system-ui, sans-serif; padding: 1rem">
          <h2>A generated page</h2>
          <p>It runs its own script, in a sandbox that keeps it away from the page around it.</p>
          <button id="counter">Clicked 0 times</button>
          <script>
            let clicks = 0;
            const button = document.getElementById('counter');
            button.onclick = () => button.textContent = 'Clicked ' + (++clicks) + ' times';
          </script>
        </body>
        </html>
        """);

    private readonly BitAiArtifact jsonArtifact = Completed(BitAiArtifactKind.Json, "Settings", """
        { "theme": "dark", "fontSize": 14, "autosave": true, "recent": ["README.md", "Program.cs"], "proxy": null }
        """);

    private readonly BitAiArtifact csvArtifact = Completed(BitAiArtifactKind.Table, "Prices", """"
        product,price,stock
        "Keyboard, wireless",89.50,12
        Mouse,20,140
        "Monitor 27""",329,4
        """", "csv");

    private readonly BitAiArtifact tsvArtifact = Completed(BitAiArtifactKind.Table, "Regions", "region\tsales\tgrowth\nEurope\t1.2M\t8%\nAmericas\t2.4M\t5%\nAsia\t1.9M\t12%", "tsv");

    private readonly BitAiArtifact jsonTableArtifact = Completed(BitAiArtifactKind.Table, "Team", """
        [
          { "name": "Ada", "role": "Engineer", "remote": true },
          { "name": "Grace", "role": "Architect", "remote": false }
        ]
        """);

    private readonly BitAiArtifact imageArtifact = Completed(BitAiArtifactKind.Image, "Mountain lake", null,
                                                              url: "/_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img1.jpg");

    private readonly BitAiArtifact svgArtifact = Completed(BitAiArtifactKind.Svg, "Badge", """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 240 80" width="240" height="80">
          <rect width="240" height="80" rx="16" fill="#0065ef" />
          <text x="120" y="52" font-size="32" font-family="sans-serif" text-anchor="middle" fill="white">bit AI</text>
          <script>alert('never runs')</script>
        </svg>
        """);

    private readonly BitAiArtifact chartArtifact = Completed(BitAiArtifactKind.Chart, "Monthly sales", """
        { "type": "bar", "labels": ["Jan", "Feb", "Mar"], "data": [120, 180, 150] }
        """, "json");

    private readonly BitAiArtifact mermaidArtifact = Completed(new BitAiArtifactKind("mermaid"), "Sign-in flow", """
        flowchart LR
            User --> Login --> Check{Valid?}
            Check -- yes --> Home
            Check -- no --> Login
        """);

    // A scripted transport (Services/AI) that writes a README into the artifact art-readme, and revises it on the next turn.
    private readonly BitAiChatSession readmeSession = new(AiDemoContentTransports.Artifacts());
    private BitAiRenderScheduler? renderScheduler;

    protected override void OnInitialized()
    {
        // The session says so on every streamed delta; the scheduler turns any number of those into one render per 50 ms.
        renderScheduler = new(() => InvokeAsync(StateHasChanged));
        readmeSession.StateChanged += renderScheduler.Request;
    }

    // The artifact belongs to the conversation, found by its id.
    private BitAiArtifact? ReadmeArtifact => readmeSession.Conversation.FindArtifact("art-readme");

    private Task WriteReadmeAsync() => readmeSession.SubmitAsync(ReadmeArtifact is null ? "Write me a README" : "Tighten it and add an install step");

    public async ValueTask DisposeAsync()
    {
        if (renderScheduler is not null) await renderScheduler.DisposeAsync();

        await readmeSession.DisposeAsync();
    }

    private readonly BitAiArtifact pricingArtifact = Revised();

    private static BitAiArtifact Revised()
    {
        var artifact = new BitAiArtifact("pricing", BitAiArtifactKind.Code, "pricing.ts") { Language = "typescript" };

        artifact.SetContent("export const price = 10;");
        artifact.Complete("First draft");

        artifact.BeginRevision();
        artifact.SetContent("export const price = (quantity: number) => quantity * 10;");
        artifact.Complete("Priced per unit");

        artifact.BeginRevision();
        artifact.SetContent("export const price = (quantity: number) => quantity * (quantity >= 10 ? 9 : 10);");
        artifact.Complete("A bulk discount");

        return artifact;
    }

    private void RestoreFirstVersion() => pricingArtifact.Restore(1);

    private readonly BitAiArtifact persianArtifact = Completed(BitAiArtifactKind.Document, "یادداشت انتشار", """
        # یادداشت انتشار

        - **آرتیفکت‌ها** تاریخچه نسخه‌ها را نگه می‌دارند.
        - **خروجی ساختاریافته** همزمان با رسیدن JSON پر می‌شود.
        """);
}

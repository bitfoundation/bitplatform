namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.ArtifactView;

public partial class BitAiArtifactViewDemo
{
    private const string CompletedSampleCode = """
// A finished artifact, as a session leaves it once the transport has completed it.
private static BitAiArtifact Completed(BitAiArtifactKind kind, string title, string? content, string? language = null, string? url = null)
{
    var artifact = new BitAiArtifact(null, kind, title) { Language = language, Url = url };
    if (content is not null) artifact.SetContent(content);
    artifact.Complete();
    return artifact;
}
""";

    private const string ProgramSampleCode = """"
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
"""";

    private const string DocumentSampleCode = """"
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
"""";

    private const string PageSampleCode = """"
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
"""";

    private const string PricingSampleCode = """
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
""";

    private readonly string example1RazorCode = """
<BitAiArtifactView Artifact="programArtifact" />
""";
    private readonly string example1CsharpCode = CompletedSampleCode + "\n" + ProgramSampleCode;

    private readonly string example2RazorCode = """
<BitAiArtifactView Artifact="documentArtifact" />
""";
    private readonly string example2CsharpCode = CompletedSampleCode + "\n" + DocumentSampleCode;

    private readonly string example3RazorCode = """
<BitAiArtifactView Artifact="pageArtifact" />
""";
    private readonly string example3CsharpCode = CompletedSampleCode + "\n" + PageSampleCode;

    private readonly string example4RazorCode = """
<BitAiArtifactView Artifact="jsonArtifact" />

<BitAiArtifactView Artifact="csvArtifact" />

<BitAiArtifactView Artifact="tsvArtifact" />

<BitAiArtifactView Artifact="jsonTableArtifact" />
""";
    private readonly string example4CsharpCode = CompletedSampleCode + """""

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
""""";

    private readonly string example5RazorCode = """
<BitAiArtifactView Artifact="imageArtifact" />

<BitAiArtifactView Artifact="svgArtifact" />

<BitAiOptionsProvider UiOptions="clickToLoadOptions">
    <BitAiArtifactView Artifact="imageArtifact" />
</BitAiOptionsProvider>

<BitAiOptionsProvider UiOptions="blockOptions">
    <BitAiArtifactView Artifact="svgArtifact" />
</BitAiOptionsProvider>
""";
    private readonly string example5CsharpCode = CompletedSampleCode + """"

private readonly BitAiUiOptions clickToLoadOptions = BitAiUiOptions.Default with { Images = BitAiImagePolicy.ClickToLoad };
private readonly BitAiUiOptions blockOptions = BitAiUiOptions.Default with { Images = BitAiImagePolicy.Block };

private readonly BitAiArtifact imageArtifact = Completed(BitAiArtifactKind.Image, "Mountain lake", null,
                                                          url: "/_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img4.jpg");

private readonly BitAiArtifact svgArtifact = Completed(BitAiArtifactKind.Svg, "Badge", """
    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 240 80" width="240" height="80">
      <rect width="240" height="80" rx="16" fill="#0065ef" />
      <text x="120" y="52" font-size="32" font-family="sans-serif" text-anchor="middle" fill="white">bit AI</text>
      <script>alert('never runs')</script>
    </svg>
    """);
"""";

    private readonly string example6RazorCode = """
<BitAiArtifactView Artifact="chartArtifact" />

<BitAiArtifactView Artifact="mermaidArtifact" />
""";
    private readonly string example6CsharpCode = CompletedSampleCode + """"

private readonly BitAiArtifact chartArtifact = Completed(BitAiArtifactKind.Chart, "Monthly sales", """
    { "type": "bar", "labels": ["Jan", "Feb", "Mar"], "data": [120, 180, 150] }
    """, "json");

// A kind of the app's own: shown as its code until a renderer is registered for it.
private readonly BitAiArtifact mermaidArtifact = Completed(new BitAiArtifactKind("mermaid"), "Sign-in flow", """
    flowchart LR
        User --> Login --> Check{Valid?}
        Check -- yes --> Home
        Check -- no --> Login
    """);
"""";

    private readonly string example7RazorCode = """
<CascadingValue Value="artifactRenderers" IsFixed="true">
    <BitAiArtifactView Artifact="chartArtifact" />

    <BitAiArtifactView Artifact="brokenChartArtifact" />
</CascadingValue>
""";
    private readonly string example7CsharpCode = CompletedSampleCode + """"

private readonly BitAiArtifact chartArtifact = Completed(BitAiArtifactKind.Chart, "Monthly sales", """
    { "type": "bar", "labels": ["Jan", "Feb", "Mar"], "data": [120, 180, 150] }
    """, "json");

private readonly BitAiArtifact brokenChartArtifact = Completed(BitAiArtifactKind.Chart, "Quarterly sales", """
    { "type": "bar", "labels": ["Q1", "Q2"] }
    """, "json");

// An app registers its renderers once, at startup:
//   AddBitBlazorUIAIServices(ai => ai.ConfigureRenderers(renderers => renderers.RegisterArtifact(BitAiArtifactKind.Chart, RenderChart)));
// That registry is one instance for the whole app, so a page that wants renderers of its own cascades a registry of its own.
private readonly BitAiRendererRegistry artifactRenderers = CreateArtifactRenderers();

private static BitAiRendererRegistry CreateArtifactRenderers()
{
    var renderers = new BitAiRendererRegistry();
    renderers.RegisterArtifact(BitAiArtifactKind.Chart, RenderChart);
    return renderers;
}

// Draws a { "labels": [...], "data": [...] } spec as bars, taller in the full-screen panel. A half-written spec is
// waited for, and the labels are model output, so they are only ever written as text. (using System.Text.Json;)
private static RenderFragment RenderChart(BitAiArtifactContext context) => builder =>
{
    if (context.IsLatest && context.Artifact.IsStreaming)
    {
        builder.AddContent(0, "Drawing the chart...");
        return;
    }

    using var spec = JsonDocument.Parse(context.Content ?? "{}");
    var labels = spec.RootElement.GetProperty("labels").EnumerateArray().Select(l => l.GetString()).ToArray();
    var values = spec.RootElement.GetProperty("data").EnumerateArray().Select(v => v.GetDouble()).ToArray();
    var max = Math.Max(1, values.Max());
    var height = context.IsExpanded ? 24 : 8;

    builder.OpenElement(1, "div");
    builder.AddAttribute(2, "role", "img");
    builder.AddAttribute(3, "aria-label", string.Join(", ", labels.Zip(values, (label, value) => $"{label} {value}")));
    builder.AddAttribute(4, "style", "display: flex; align-items: flex-end; gap: 1rem; padding: 1rem;");

    for (var i = 0; i < values.Length; i++)
    {
        builder.OpenElement(5, "div");
        builder.AddAttribute(6, "style", "flex: 1; display: flex; flex-direction: column; align-items: center; gap: 0.25rem;");

        builder.OpenElement(7, "div");
        builder.AddAttribute(8, "style", $"width: 100%; height: {Math.Round(values[i] / max * height * 16)}px; border-radius: 0.25rem; background-color: var(--bit-clr-pri);");
        builder.CloseElement();

        builder.OpenElement(9, "span");
        builder.AddContent(10, i < labels.Length ? labels[i] : null);
        builder.CloseElement();

        builder.CloseElement();
    }

    builder.CloseElement();
};
"""";

    private readonly string example8RazorCode = """
@implements IAsyncDisposable

<BitButton OnClick="WriteReadmeAsync" Disabled="readmeSession.IsGenerating">@(ReadmeArtifact is null ? "Write a README" : "Revise it")</BitButton>

@if (ReadmeArtifact is { } readme)
{
    <BitAiArtifactView Artifact="readme" />
}
""";
    private readonly string example8CsharpCode = """
// A scripted transport that writes a README into the artifact art-readme, and revises it on the next turn (see the next tab).
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
""";
    private const string example8TransportCode = """"
public static IBitAiTransport Artifacts() =>
    new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
    {
        const string artifactId = "art-readme";

        // Every turn after the first is a revision, numbered in the body, so no version comes out the same as the one
        // before it.
        var revision = request.Messages.Count(m => m.Role.IsUser) - 1;
        var isRevision = revision > 0;

        await yielder(new BitAiMessageStartedEvent());

        var intro = revision switch
        {
            0 => "Here is a first draft. Tell me what to change, and I will revise it.",
            1 => "Revised. I tightened the opening and added the install step.",
            _ => $"Revised again, into revision {revision}.",
        };

        await foreach (var evt in AiScript.Write(intro, 4, 12, token)) await yielder(evt);

        // Opening the same artifact id again starts its next version.
        await yielder(new BitAiArtifactCreatedEvent(artifactId, BitAiArtifactKind.Document, "Project README")
        {
            Language = "markdown",
        });

        var body = isRevision
            ? $"""
              # Bit.BlazorUI.AI

              The AI UI layer of bit BlazorUI: conversations, streaming, tools, agents, and artifacts.

              ## Install

              ```bash
              dotnet add package Bit.BlazorUI.AI
              ```

              ## Why

              Chat is the easy part. What takes the time is everything around it: partial JSON,
              tool approval, citation anchoring, and not re-rendering the world on every token.

              _Revision {revision}._
              """
            : """
              # Bit.BlazorUI.AI

              The AI components of bit BlazorUI.

              ## Why

              Chat is the easy part. Everything around it is what takes the time.
              """;

        foreach (var slice in AiScript.Slice(body, 12))
        {
            await AiScript.Pause(60, token);
            await yielder(new BitAiArtifactDeltaEvent(artifactId, slice));
        }

        await yielder(new BitAiArtifactCompletedEvent(artifactId)
        {
            Summary = revision switch
            {
                0 => "First draft",
                1 => "Tightened the opening, added install",
                _ => $"Revision {revision}",
            },
        });

        await yielder(new BitAiMessageCompletedEvent());
    }));
"""";
    private readonly DemoCodeFile[] example8CodeFiles =
    [
        new("AiDemoContentTransports.cs", example8TransportCode),
    ];

    private readonly string example9RazorCode = """
<BitButton OnClick="RestoreFirstVersion">Restore version 1</BitButton>

<BitAiArtifactView Artifact="pricingArtifact" />

<BitAiArtifactView Artifact="pricingArtifact" InitialVersion="1" />

<ul>
    @foreach (var version in pricingArtifact.Versions)
    {
        <li>Version @version.Version: @version.Summary</li>
    }
</ul>
""";
    private readonly string example9CsharpCode = PricingSampleCode + """

private void RestoreFirstVersion() => pricingArtifact.Restore(1);
""";

    private readonly string example10RazorCode = """
<BitAiArtifactView Artifact="pageArtifact" />

<BitAiArtifactView Artifact="pricingArtifact" Disabled />
""";
    private readonly string example10CsharpCode = CompletedSampleCode + "\n" + PageSampleCode + "\n" + PricingSampleCode;

    private readonly string example11RazorCode = """
<BitParams Parameters="artifactViewParams">
    <BitAiArtifactView Artifact="programArtifact" />

    <BitAiArtifactView Artifact="jsonArtifact" Styles="@(new() { Title = "font-style: italic" })" />
</BitParams>
""";
    private readonly string example11CsharpCode = CompletedSampleCode + "\n" + ProgramSampleCode + """"

private readonly BitAiArtifact jsonArtifact = Completed(BitAiArtifactKind.Json, "Settings", """
    { "theme": "dark", "fontSize": 14, "autosave": true, "recent": ["README.md", "Program.cs"], "proxy": null }
    """);

private readonly BitAiArtifactViewParams[] artifactViewParams =
[
    new() { Styles = new() { Panel = "border-radius: 1rem", Header = "background-color: var(--bit-clr-pri-light)", Title = "color: var(--bit-clr-pri)" } },
];
"""";

    private readonly string example12RazorCode = """
<style>
    .custom-panel {
        border-width: 0.125rem;
        border-color: #8a2be2;
        border-radius: 0;
    }

    .custom-header {
        color: white;
        background-color: #8a2be2;
        --bit-Button-color: white;
    }
</style>

<BitAiArtifactView Artifact="programArtifact"
                   Style="max-width: 32rem"
                   Styles="@(new() { Header = "background-color: #1e1e2e; color: #cdd6f4; --bit-Button-color: #cdd6f4", Icon = "color: #89b4fa", Title = "font-family: monospace" })" />

<BitAiArtifactView Artifact="documentArtifact" Classes="@(new() { Panel = "custom-panel", Header = "custom-header" })" />

<div style="--bit-AiArtifactView-radius: 1rem; --bit-AiArtifactView-border-color: #d2691e; --bit-AiArtifactView-header-background: #d2691e1a; --bit-AiArtifactView-title-font-size: 1rem; --bit-AiArtifactView-max-height: 10rem">
    <BitAiArtifactView Artifact="documentArtifact" />
</div>
""";
    private readonly string example12CsharpCode = CompletedSampleCode + "\n" + ProgramSampleCode + "\n" + DocumentSampleCode;

    private readonly string example13RazorCode = """
<BitAiArtifactView Dir="BitDir.Rtl" Artifact="persianArtifact" />

<div dir="rtl">
    <BitAiArtifactView Artifact="persianArtifact" />
</div>
""";
    private readonly string example13CsharpCode = CompletedSampleCode + """"

private readonly BitAiArtifact persianArtifact = Completed(BitAiArtifactKind.Document, "یادداشت انتشار", """
    # یادداشت انتشار

    - **آرتیفکت‌ها** تاریخچه نسخه‌ها را نگه می‌دارند.
    - **خروجی ساختاریافته** همزمان با رسیدن JSON پر می‌شود.
    """);
"""";
}

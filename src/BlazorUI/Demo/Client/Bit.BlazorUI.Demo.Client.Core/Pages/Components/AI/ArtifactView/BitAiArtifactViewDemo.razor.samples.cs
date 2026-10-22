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
""";
    private readonly string example5CsharpCode = CompletedSampleCode + """"

private readonly BitAiArtifact imageArtifact = Completed(BitAiArtifactKind.Image, "Mountain lake", null,
                                                          url: "/_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img1.jpg");

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
@implements IAsyncDisposable

<BitButton OnClick="WriteReadmeAsync" Disabled="readmeSession.IsGenerating">@(ReadmeArtifact is null ? "Write a README" : "Revise it")</BitButton>

@if (ReadmeArtifact is { } readme)
{
    <BitAiArtifactView Artifact="readme" />
}
""";
    private readonly string example7CsharpCode = """
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
    private const string example7TransportCode = """"
public static IBitAiTransport Artifacts() =>
    new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
    {
        const string artifactId = "art-readme";
        var isRevision = request.Messages.Count(m => m.Role.IsUser) > 1;

        await yielder(new BitAiMessageStartedEvent());

        var intro = isRevision
            ? "Revised. I tightened the opening and added the install step."
            : "Here is a first draft. Tell me what to change, and I will revise it.";

        await foreach (var evt in AiScript.Write(intro, 4, 12, token)) await yielder(evt);

        // Opening the same artifact id again starts its next version.
        await yielder(new BitAiArtifactCreatedEvent(artifactId, BitAiArtifactKind.Document, "Project README")
        {
            Language = "markdown",
        });

        var body = isRevision
            ? """
              # Bit.BlazorUI.AI

              The AI UI layer of bit BlazorUI: conversations, streaming, tools, agents, and artifacts.

              ## Install

              ```bash
              dotnet add package Bit.BlazorUI.AI
              ```

              ## Why

              Chat is the easy part. What takes the time is everything around it: partial JSON,
              tool approval, citation anchoring, and not re-rendering the world on every token.
              """
            : """
              # Bit.BlazorUI.AI

              The AI components of bit BlazorUI.

              ## Why

              Chat is the easy part. Everything around it is what takes the time.
              """;

        foreach (var slice in AiScript.Slice(body, 24))
        {
            await AiScript.Pause(45, token);
            await yielder(new BitAiArtifactDeltaEvent(artifactId, slice));
        }

        await yielder(new BitAiArtifactCompletedEvent(artifactId)
        {
            Summary = isRevision ? "Tightened the opening, added install" : "First draft",
        });

        await yielder(new BitAiMessageCompletedEvent());
    }));
"""";
    private readonly DemoCodeFile[] example7CodeFiles =
    [
        new("AiDemoContentTransports.cs", example7TransportCode),
    ];

    private readonly string example8RazorCode = """
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
    private readonly string example8CsharpCode = PricingSampleCode + """

private void RestoreFirstVersion() => pricingArtifact.Restore(1);
""";

    private readonly string example9RazorCode = """
<BitAiArtifactView Artifact="pageArtifact" />

<BitAiArtifactView Artifact="pricingArtifact" Disabled />
""";
    private readonly string example9CsharpCode = CompletedSampleCode + "\n" + PageSampleCode + "\n" + PricingSampleCode;

    private readonly string example10RazorCode = """
<style>
    .custom-panel {
        border-width: 0.125rem;
        border-color: #8a2be2;
        border-radius: 0;
    }

    .custom-header {
        color: white;
        background-color: #8a2be2;
    }
</style>

<BitAiArtifactView Artifact="programArtifact"
                   Style="max-width: 32rem"
                   Styles="@(new() { Header = "background-color: #1e1e2e; color: #cdd6f4", Icon = "color: #89b4fa", Title = "font-family: monospace" })" />

<BitAiArtifactView Artifact="documentArtifact" Classes="@(new() { Panel = "custom-panel", Header = "custom-header" })" />

<div style="--bit-AiArtifactView-radius: 1rem; --bit-AiArtifactView-border-color: #d2691e; --bit-AiArtifactView-header-background: #d2691e1a; --bit-AiArtifactView-title-font-size: 1rem; --bit-AiArtifactView-max-height: 10rem">
    <BitAiArtifactView Artifact="documentArtifact" />
</div>
""";
    private readonly string example10CsharpCode = CompletedSampleCode + "\n" + ProgramSampleCode + "\n" + DocumentSampleCode;

    private readonly string example11RazorCode = """
<BitAiArtifactView Dir="BitDir.Rtl" Artifact="persianArtifact" />

<div dir="rtl">
    <BitAiArtifactView Artifact="persianArtifact" />
</div>
""";
    private readonly string example11CsharpCode = CompletedSampleCode + """"

private readonly BitAiArtifact persianArtifact = Completed(BitAiArtifactKind.Document, "یادداشت انتشار", """
    # یادداشت انتشار

    - **آرتیفکت‌ها** تاریخچه نسخه‌ها را نگه می‌دارند.
    - **خروجی ساختاریافته** همزمان با رسیدن JSON پر می‌شود.
    """);
"""";
}

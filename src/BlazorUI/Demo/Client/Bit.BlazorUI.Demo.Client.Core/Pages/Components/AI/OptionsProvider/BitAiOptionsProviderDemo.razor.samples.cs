namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.OptionsProvider;

public partial class BitAiOptionsProviderDemo
{
    private readonly string example1RazorCode = """
<BitToggle Label="Inline citations" @bind-Value="inlineCitations" @bind-Value:after="BuildBasicOptions" />

<BitAiOptionsProvider UiOptions="basicOptions">
    <BitAiStreamingMarkdown Text="@answer" Citations="citations" />
</BitAiOptionsProvider>
""";
    private readonly string example1CsharpCode = """
private static readonly BitAiCitation[] citations =
[
    new("c1", new BitAiCitationSource("render-modes", "Blazor render modes")
    {
        Kind = BitAiSourceKind.Web,
        Url = "https://learn.microsoft.com/aspnet/core/blazor/components/render-modes",
        SiteName = "Microsoft Learn",
    }) { Number = 1 },
    new("c2", new BitAiCitationSource("circuits", "SignalR circuit lifetime")
    {
        Kind = BitAiSourceKind.Document,
        Url = "https://learn.microsoft.com/aspnet/core/blazor/fundamentals/signalr",
        SiteName = "Microsoft Learn",
    }) { Number = 2 },
];

private const string answer = "Interactive Server keeps component state on the server [1], and loses it with the circuit [2].";

private bool inlineCitations = true;
private BitAiUiOptions basicOptions = BitAiUiOptions.Default;

private void BuildBasicOptions() => basicOptions = BitAiUiOptions.Default with { InlineCitations = inlineCitations };
""";

    private readonly string example2RazorCode = """
<BitAiOptionsProvider UiOptions="germanOptions">
    <BitAiAgentStatusBadge Status="BitAiAgentRunStatus.Running" />
    <BitAiAgentStatusBadge Status="BitAiAgentRunStatus.WaitingForInput" />
    <BitAiAgentStatusBadge Status="BitAiAgentRunStatus.Completed" />

    <BitAiTokenUsageView Usage="usage" />

    <BitAiContextMeter Used="110_000" Window="128_000" />

    <BitAiSourceList Citations="citations" />
</BitAiOptionsProvider>
""";
    private readonly string example2CsharpCode = """
// citations is the one of the Basic example.
private static readonly BitAiUiOptions germanOptions = BitAiUiOptions.Default with { Strings = new GermanAiStrings() };

private readonly BitAiTokenUsage usage = new() { InputTokens = 12_400, OutputTokens = 1_860, CachedInputTokens = 9_800 };

private sealed class GermanAiStrings : BitAiStrings
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    public override string AgentRunning => "Läuft";
    public override string WaitingForYou => "Wartet auf Ihre Antwort";
    public override string AgentCompleted => "Fertig";
    public override string Sources => "Quellen";
    public override string Context => "Kontext";
    public override string Tokens => "Token";
    public override string InputTokens => "Eingabe";
    public override string OutputTokens => "Ausgabe";
    public override string CachedTokens => "Zwischengespeichert";
    public override string ContextWarning => "Das Kontextfenster füllt sich";
    public override string ContextCritical => "Das Kontextfenster ist fast voll";
    public override string CitationLabel(int? number, string title) => number is null ? $"Quelle: {title}" : $"Quelle {number}: {title}";
    public override string ContextUsage(long used, long window) => string.Format(German, "{0:N0} / {1:N0} {2}", used, window, Tokens);
    public override string UsageLine(string label, long tokens) => string.Format(German, "{0}: {1:N0}", label, tokens);
    public override string FormatTokens(long tokens) => tokens < 1000
        ? tokens.ToString("N0", German)
        : string.Format(German, "{0:0.0} Tsd.", tokens / 1000.0);
}
""";

    private readonly string example3RazorCode = """
<BitAiOptionsProvider UiOptions="germanOptions">
    <BitAiStreamingMarkdown Text="@answer" Citations="citations" />
    <BitAiSourceList Citations="citations" />

    <BitAiOptionsProvider UiOptions="innerOptions">
        <BitAiStreamingMarkdown Text="@answer" Citations="citations" />
        <BitAiSourceList Citations="citations" />
    </BitAiOptionsProvider>

    <BitAiOptionsProvider>
        <BitAiStreamingMarkdown Text="@answer" Citations="citations" />
        <BitAiSourceList Citations="citations" />
    </BitAiOptionsProvider>
</BitAiOptionsProvider>
""";
    private readonly string example3CsharpCode = """
// answer, citations and germanOptions are the ones of the examples above.
private static readonly BitAiUiOptions innerOptions = germanOptions with { InlineCitations = false };
""";

    private readonly string example4RazorCode = """
<BitAiAgentRunView Run="usageRun" />

<BitAiOptionsProvider UiOptions="BitAiUiOptions.Default">
    <BitAiAgentRunView Run="usageRun" />
</BitAiOptionsProvider>
""";
    private readonly string example4CsharpCode = """
private readonly BitAiAgentRun usageRun = CreateUsageRun();

private static BitAiAgentRun CreateUsageRun()
{
    var run = new BitAiAgentRun(agentName: "Research agent") { Goal = "Summarize the release notes" };

    run.SetPlan(
    [
        new BitAiAgentPlanStep("fetch", "Fetch the release notes"),
        new BitAiAgentPlanStep("summarize", "Summarize them"),
    ]);
    run.SetStatus(BitAiAgentRunStatus.Running);
    run.Steps[0].Start();
    run.AddUsage(new BitAiTokenUsage { InputTokens = 1_850, OutputTokens = 420, Cost = 0.0119m, Currency = "USD" });

    return run;
}
""";
    private const string example4ProgramCode = """
builder.Services.AddBitBlazorUIAIServices(ai =>
{
    ai.UseTransport<MyTransport>();

    // The options every AI component outside a BitAiOptionsProvider uses.
    ai.ConfigureUi(options => options with
    {
        ShowUsage = true,
        ShowModelName = true,
    });
});
""";
    private readonly DemoCodeFile[] example4CodeFiles =
    [
        new("Program.cs", example4ProgramCode),
    ];

    private readonly string example5RazorCode = """
<BitButton OnClick="StartStreamingRun" Disabled="isStreaming">Start the run</BitButton>

@if (streamingRun is not null)
{
    <BitAiOptionsProvider UiOptions="fastOptions">
        <BitAiAgentRunView Run="streamingRun" />
    </BitAiOptionsProvider>

    <BitAiOptionsProvider UiOptions="slowOptions">
        <BitAiAgentRunView Run="streamingRun" />
    </BitAiOptionsProvider>
}
""";
    private readonly string example5CsharpCode = """
private readonly BitAiUiOptions fastOptions = BitAiUiOptions.Default with { ShowUsage = true };
private readonly BitAiUiOptions slowOptions = BitAiUiOptions.Default with { ShowUsage = true, RenderInterval = TimeSpan.FromSeconds(1) };
private BitAiAgentRun? streamingRun;
private bool isStreaming;

private async Task StartStreamingRun()
{
    isStreaming = true;

    var run = new BitAiAgentRun(agentName: "Writer") { Goal = "Draft the announcement" };
    streamingRun = run;

    run.SetPlan([new BitAiAgentPlanStep("draft", "Draft it")]);
    run.SetStatus(BitAiAgentRunStatus.Running);
    run.Steps[0].Start();

    for (var i = 0; i < 100; i++)
    {
        await Task.Delay(50);
        run.AddUsage(new BitAiTokenUsage { OutputTokens = 7 });
    }

    run.Steps[0].Complete(summary: "700 tokens written");
    run.SetStatus(BitAiAgentRunStatus.Completed);

    isStreaming = false;
}
""";

    private readonly string example6RazorCode = """
<BitThemeProvider Theme="compactTheme">
    <BitAiAgentStatusBadge Status="BitAiAgentRunStatus.Running" />
    <BitAiContextMeter Used="42_000" Window="128_000" />
    <BitAiSourceList Citations="citations" />
</BitThemeProvider>

<BitThemeProvider Theme="spaciousTheme">
    <BitAiAgentStatusBadge Status="BitAiAgentRunStatus.Running" />
    <BitAiContextMeter Used="42_000" Window="128_000" />
    <BitAiSourceList Citations="citations" />
</BitThemeProvider>

<div bit-theme="fluent-dark" style="padding: 1rem; color: var(--bit-clr-fg-pri); background-color: var(--bit-clr-bg-pri)">
    <BitAiAgentStatusBadge Status="BitAiAgentRunStatus.Running" />
    <BitAiContextMeter Used="42_000" Window="128_000" />
    <BitAiSourceList Citations="citations" />
</div>
""";
    private readonly string example6CsharpCode = """
// citations is the one of the Basic example.
private readonly BitTheme compactTheme = BitThemeDensityPresets.CreateCompactOverlay();
private readonly BitTheme spaciousTheme = BitThemeDensityPresets.CreateSpaciousOverlay();
""";
}

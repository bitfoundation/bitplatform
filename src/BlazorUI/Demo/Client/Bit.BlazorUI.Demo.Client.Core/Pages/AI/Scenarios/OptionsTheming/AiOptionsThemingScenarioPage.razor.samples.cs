namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.OptionsTheming;

public partial class AiOptionsThemingScenarioPage
{
    private readonly string example1RazorCode = """
@implements IAsyncDisposable

<BitChoiceGroup Label="Design system" TItem="BitChoiceGroupOption<string>" TValue="string" @bind-Value="designSystem">
    <BitChoiceGroupOption Text="Fluent" Value="@("fluent")" />
    <BitChoiceGroupOption Text="Fluent 2" Value="@("fluent2")" />
    <BitChoiceGroupOption Text="Material" Value="@("material")" />
    <BitChoiceGroupOption Text="Cupertino" Value="@("cupertino")" />
</BitChoiceGroup>

<BitChoiceGroup Label="Scheme" TItem="BitChoiceGroupOption<string>" TValue="string" @bind-Value="scheme" Horizontal>
    <BitChoiceGroupOption Text="Light" Value="@("light")" />
    <BitChoiceGroupOption Text="Dark" Value="@("dark")" />
</BitChoiceGroup>

<BitChoiceGroup Label="Density" TItem="BitChoiceGroupOption<string>" TValue="string" @bind-Value="density" Horizontal>
    <BitChoiceGroupOption Text="Compact" Value="@("0.85")" />
    <BitChoiceGroupOption Text="Default" Value="@("1")" />
    <BitChoiceGroupOption Text="Spacious" Value="@("1.15")" />
</BitChoiceGroup>

<BitToggle Label="Right to left" @bind-Value="rtl" />
<BitToggle Label="French strings" @bind-Value="french" @bind-Value:after="RebuildOptions" />
<BitToggle Label="Show usage" @bind-Value="showUsage" @bind-Value:after="RebuildOptions" />
<BitToggle Label="Show model name" @bind-Value="showModelName" @bind-Value:after="RebuildOptions" />
<BitToggle Label="Show timestamps" @bind-Value="showTimestamps" @bind-Value:after="RebuildOptions" />
<BitToggle Label="Show message actions" @bind-Value="showMessageActions" @bind-Value:after="RebuildOptions" />
<BitToggle Label="Line numbers in code" @bind-Value="showLineNumbers" @bind-Value:after="RebuildOptions" />

@* A scoped preset, a density multiplier and a direction, for this region alone. *@
<div bit-theme="@($"{designSystem}-{scheme}")"
     dir="@(rtl ? "rtl" : "ltr")"
     style="@($"height:560px;--bit-layout-density-scale:{density}")">
    <BitAiOptionsProvider UiOptions="options">
        <BitAiChat Session="session" Suggestions="suggestions" />
    </BitAiOptionsProvider>
</div>
""";

    private readonly string example1CsharpCode = """
private static readonly BitAiStrings French = new FrenchAiStrings();

private BitAiChatSession? session;
private BitAiUiOptions options = BitAiUiOptions.Default;

private string? designSystem = "fluent";
private string? scheme = "light";
private string? density = "1";
private bool rtl;
private bool french;
private bool showUsage = true;
private bool showModelName = true;
private bool showTimestamps;
private bool showMessageActions = true;
private bool showLineNumbers;

private readonly string[] suggestions = ["Show me what changed"];

[Inject] private BitAiChatSessionFactory SessionFactory { get; set; } = default!;

protected override void OnInitialized()
{
    session = SessionFactory.Create(AiDemoConversations.Long(6));
    RebuildOptions();
}

// A new record rather than a mutation: a new instance is what tells the provider to hand the change down.
private void RebuildOptions() => options = BitAiUiOptions.Default with
{
    Strings = french ? French : BitAiStrings.Default,
    ShowUsage = showUsage,
    ShowModelName = showModelName,
    ShowTimestamps = showTimestamps,
    ShowMessageActions = showMessageActions,
    ShowLineNumbers = showLineNumbers,
};

public ValueTask DisposeAsync() => session?.DisposeAsync() ?? ValueTask.CompletedTask;
""";

    private const string example1StringsCode = """
// Overrides what it translates, and keeps the English of the rest.
public sealed class FrenchAiStrings : BitAiStrings
{
    public override string PromptPlaceholder => "Écrivez un message...";
    public override string Send => "Envoyer";
    public override string Stop => "Arrêter";
    public override string Copy => "Copier";
    public override string Copied => "Copié";
    public override string Regenerate => "Régénérer";
    public override string GoodResponse => "Bonne réponse";
    public override string BadResponse => "Mauvaise réponse";
    public override string ScrollToLatest => "Aller au plus récent";
    public override string Thinking => "Réflexion";
    public override string Sources => "Sources";
    public override string AttachFile => "Joindre des fichiers";
    public override string ShowDetails => "Afficher les détails";
}
""";

    private const string example1ProgramCode = """
builder.Services.AddBitBlazorUIAIServices(ai =>
{
    ai.UseTransport(AiDemoTransports.Echo());

    // The defaults of the whole app; a BitAiOptionsProvider, or the UiOptions of one chat, override them.
    ai.ConfigureUi(options => options with
    {
        ShowUsage = true,
        ShowModelName = true,
        ShowTimestamps = false,
    });
});
""";

    private readonly DemoCodeFile[] example1CodeFiles =
    [
        new("FrenchAiStrings.cs", example1StringsCode),
        new("Program.cs", example1ProgramCode),
    ];
}

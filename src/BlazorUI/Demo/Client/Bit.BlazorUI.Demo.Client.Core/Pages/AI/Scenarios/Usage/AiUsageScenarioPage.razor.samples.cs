namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Usage;

public partial class AiUsageScenarioPage
{
    private readonly string example1RazorCode = """
@implements IAsyncDisposable

<div style="height: 560px">
    <BitAiChat Session="session"
               UiOptions="options"
               EmptyTitle="Pick a model and ask something"
               Suggestions="suggestions">
        <ComposerPrefixTemplate>
            <BitAiModelSelector @bind-ModelId="modelId"
                                OnModelSelected="OnModelSelected"
                                Disabled="@(session?.IsGenerating ?? false)" />
        </ComposerPrefixTemplate>
        <ComposerHintTemplate>
            @* The window of the model picked now, rather than of the one the last reply ran on. *@
            <BitAiContextMeter Conversation="session?.Conversation" Window="model?.ContextWindow" />
        </ComposerHintTemplate>
    </BitAiChat>
</div>
""";

    private readonly string example1CsharpCode = """
private readonly BitAiUiOptions options = BitAiUiOptions.Default with
{
    ShowUsage = true,
    ShowModelName = true,
    ShowTimestamps = true,
};

private BitAiChatSession? session;
private BitAiModel? model;
private string? modelId;

private readonly string[] suggestions = ["What did this turn cost?"];

// The catalog registered with ai.AddModels(...).
[Inject] private IBitAiModelCatalog Catalog { get; set; } = default!;

protected override void OnInitialized()
{
    model = Catalog.DefaultModel;
    modelId = model?.ModelId;
    Rebuild();
}

private async Task OnModelSelected(BitAiModel selected)
{
    model = selected;
    var previous = session;
    Rebuild();

    // The conversation carries over to the new session; the old one only has to stop.
    if (previous is not null) await previous.DisposeAsync();
}

private void Rebuild()
{
    if (model is null) return;

    var conversation = session?.Conversation ?? new BitAiConversation();
    session = new BitAiChatSession(
        AiDemoInteractiveTransports.Metered(model),
        conversation,
        new BitAiChatSessionOptions
        {
            RequestOptions = new BitAiRequestOptions { ModelId = model.ModelId, Temperature = 0.7 },
        });
}

public ValueTask DisposeAsync() => session?.DisposeAsync() ?? ValueTask.CompletedTask;
""";

    private const string example1ProgramCode = """
builder.Services.AddBitBlazorUIAIServices(ai =>
{
    ai.UseTransport(AiDemoTransports.Echo());

    // The models the selector offers, the providers they are grouped under, and the default.
    ai.AddModels(AiDemoCatalog.Models, AiDemoCatalog.Providers, defaultModelId: "demo-model-1");
});

// One of them: the rates are what the cost under each answer is priced with.
new BitAiModel("demo-model-1", "Demo Large")
{
    ProviderId = "demo",
    Family = "Demo Provider",
    Capabilities = BitAiModelCapabilities.Streaming | BitAiModelCapabilities.Tools | BitAiModelCapabilities.Vision,
    ContextWindow = 200_000,
    MaxOutputTokens = 16_000,
    InputPricePerMillion = 3m,
    OutputPricePerMillion = 15m,
};
""";

    private const string example1TransportCode = """
public static IBitAiTransport Metered(BitAiModel model) => new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
{
    await yielder(new BitAiMessageStartedEvent());
    await yielder(new BitAiMetadataEvent(new BitAiGenerationMetadata
    {
        ModelId = model.ModelId,
        ProviderId = model.ProviderId,
        ContextWindow = model.ContextWindow,
        Temperature = 0.7,
    }));

    var answer = $"This turn ran on {model.Name}.";
    await foreach (var evt in AiScript.Write(answer, 4, 10, token)) await yielder(evt);

    var input = request.Messages.Sum(m => m.GetText().Length) / 4;
    var output = answer.Length / 4;

    // What the provider reports; the components work out the rest.
    await yielder(new BitAiUsageUpdatedEvent(new BitAiTokenUsage
    {
        InputTokens = input,
        OutputTokens = output,
        CachedInputTokens = input / 3,
        Cost = model.EstimateCost(new BitAiTokenUsage { InputTokens = input, OutputTokens = output }),
        Currency = model.Currency,
    }));

    await yielder(new BitAiMessageCompletedEvent());
}));
""";

    private readonly DemoCodeFile[] example1CodeFiles =
    [
        new("Program.cs", example1ProgramCode),
        new("AiDemoInteractiveTransports.cs", example1TransportCode),
    ];
}

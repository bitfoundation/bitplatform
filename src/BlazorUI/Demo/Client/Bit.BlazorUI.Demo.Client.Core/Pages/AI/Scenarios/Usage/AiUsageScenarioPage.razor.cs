using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Usage;

public partial class AiUsageScenarioPage
{
    private readonly BitAiUiOptions _options = BitAiUiOptions.Default with
    {
        ShowUsage = true,
        ShowModelName = true,
        ShowTimestamps = true,
    };

    private BitAiChatSession? _session;
    private BitAiModel? _model;
    private string? _modelId;

    private readonly string[] _suggestions = ["What did this turn cost?"];

    [AutoInject] private IBitAiModelCatalog _catalog = default!;

    protected override Task OnInitAsync()
    {
        _model = _catalog.DefaultModel;
        _modelId = _model?.ModelId;
        Rebuild();

        return base.OnInitAsync();
    }

    private async Task OnModelSelected(BitAiModel model)
    {
        _model = model;
        var previous = _session;
        Rebuild();

        // The conversation carries over to the new session; the old one only has to stop.
        if (previous is not null) await previous.DisposeAsync();
    }

    private void Rebuild()
    {
        if (_model is null) return;

        var conversation = _session?.Conversation ?? new BitAiConversation();
        _session = new BitAiChatSession(
            AiDemoInteractiveTransports.Metered(_model),
            conversation,
            new BitAiChatSessionOptions
            {
                RequestOptions = new BitAiRequestOptions { ModelId = _model.ModelId, Temperature = 0.7 },
            });
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (_session is not null) await _session.DisposeAsync();

        await base.DisposeAsync(disposing);
    }
}

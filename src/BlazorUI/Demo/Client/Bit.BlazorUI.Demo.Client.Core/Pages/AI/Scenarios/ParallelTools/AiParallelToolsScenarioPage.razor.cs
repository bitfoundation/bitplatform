using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.ParallelTools;

public partial class AiParallelToolsScenarioPage
{
    private BitAiChatSession? _session;

    private readonly string[] _suggestions = ["Compare the weather in Oslo and Lisbon"];

    [AutoInject] private IBitAiToolRegistry _tools = default!;

    protected override Task OnInitAsync()
    {
        _session = new BitAiChatSession(AiDemoToolTransports.CallsTwoTools(), tools: _tools);

        return base.OnInitAsync();
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (_session is not null) await _session.DisposeAsync();

        await base.DisposeAsync(disposing);
    }
}

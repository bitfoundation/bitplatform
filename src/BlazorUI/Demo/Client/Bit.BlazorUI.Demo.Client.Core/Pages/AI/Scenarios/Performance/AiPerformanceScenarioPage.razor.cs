using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Performance;

public partial class AiPerformanceScenarioPage
{
    private readonly BitAiUiOptions _options = BitAiUiOptions.Default with
    {
        ShowTimestamps = true,
        VirtualizeThreshold = 60,
    };

    private BitAiChatSession? _session;

    [AutoInject] private BitAiChatSessionFactory _sessionFactory = default!;

    protected override Task OnInitAsync()
    {
        _session = _sessionFactory.Create(AiDemoConversations.Long(500));

        return base.OnInitAsync();
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (_session is not null) await _session.DisposeAsync();

        await base.DisposeAsync(disposing);
    }
}

namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Basic;

public partial class AiBasicScenarioPage
{
    private BitAiChatSession? _session;

    private readonly string[] _suggestions =
    [
        "What can these components do?",
        "Show me streaming",
        "Explain render modes",
    ];

    [AutoInject] private BitAiChatSessionFactory _sessionFactory = default!;

    protected override Task OnInitAsync()
    {
        _session = _sessionFactory.Create();

        return base.OnInitAsync();
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (_session is not null) await _session.DisposeAsync();

        await base.DisposeAsync(disposing);
    }
}

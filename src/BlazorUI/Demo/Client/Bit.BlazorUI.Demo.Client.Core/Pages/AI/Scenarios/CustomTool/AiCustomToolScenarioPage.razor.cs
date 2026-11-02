using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.CustomTool;

public partial class AiCustomToolScenarioPage
{
    private BitAiChatSession? _session;

    private readonly string[] _suggestions = ["Find the docs on render modes"];

    [AutoInject] private IBitAiToolRegistry _tools = default!;

    protected override Task OnInitAsync()
    {
        _session = new BitAiChatSession(
            AiDemoToolTransports.CallsTool(
                "search_docs",
                """{"query":"render modes"}""",
                "Two passages matched. The first covers choosing a mode per component; the second covers what happens when a circuit is lost."),
            tools: _tools);

        return base.OnInitAsync();
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (_session is not null) await _session.DisposeAsync();

        await base.DisposeAsync(disposing);
    }
}

using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Tools;

public partial class AiToolsScenarioPage
{
    private BitAiChatSession? _session;

    private readonly string[] _suggestions = ["What is the weather in Oslo?"];

    [AutoInject] private IBitAiToolRegistry _tools = default!;

    protected override Task OnInitAsync()
    {
        _session = new BitAiChatSession(
            AiDemoToolTransports.CallsTool(
                "get_weather",
                """{"city":"Oslo","days":1}""",
                "It is -3 degrees in Oslo with snow and 81% humidity. Pack for it."),
            tools: _tools);

        return base.OnInitAsync();
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (_session is not null) await _session.DisposeAsync();

        await base.DisposeAsync(disposing);
    }
}

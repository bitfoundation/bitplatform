using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Approval;

public partial class AiApprovalScenarioPage
{
    private BitAiChatSession? _session;

    private readonly string[] _suggestions = ["Delete the archived records"];

    [AutoInject] private IBitAiToolRegistry _tools = default!;
    [AutoInject] private IBitAiToolApprovalPolicy _policy = default!;

    protected override Task OnInitAsync()
    {
        _session = new BitAiChatSession(
            AiDemoToolTransports.CallsTool(
                "delete_records",
                """{"filter":"status = 'archived' AND updated < '2024-01-01'"}""",
                "Deleted 128 archived records. Nothing else was touched."),
            tools: _tools,
            approvalPolicy: _policy);

        return base.OnInitAsync();
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (_session is not null) await _session.DisposeAsync();

        await base.DisposeAsync(disposing);
    }
}

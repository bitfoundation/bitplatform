using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Approval;

public partial class AiApprovalScenarioPage
{
    private BitAiChatSession? _session;
    private BitAiChatSession? _transportSession;

    private readonly string[] _suggestions = ["Delete the archived records"];
    private readonly string[] _transportSuggestions = ["What is the weather in Lisbon?"];

    [AutoInject] private IBitAiToolRegistry _tools = default!;
    [AutoInject] private IBitAiToolApprovalPolicy _policy = default!;

    protected override Task OnInitAsync()
    {
        _session = new BitAiChatSession(
            AiDemoToolTransports.CallsTool(
                "delete_records",
                """{"filter":"status = 'archived' AND updated < '2024-01-01'"}""",
                "Deleted 128 archived records. Nothing else was touched.",
                declinedAnswer: "Understood - I did not delete anything; the archived records are still there."),
            tools: _tools,
            approvalPolicy: _policy);

        _transportSession = new BitAiChatSession(
            AiDemoToolTransports.CallsToolAskingApproval(
                "get_weather",
                """{"city":"Lisbon"}""",
                reason: "Looking up the weather tells the weather service which city you asked about. Go ahead?",
                answer: "It is 17 degrees and clear in Lisbon, with 54% humidity.",
                declinedAnswer: "Understood - I did not look it up, so the weather service was told nothing."),
            tools: _tools,
            approvalPolicy: _policy);

        return base.OnInitAsync();
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (_session is not null) await _session.DisposeAsync();
        if (_transportSession is not null) await _transportSession.DisposeAsync();

        await base.DisposeAsync(disposing);
    }
}

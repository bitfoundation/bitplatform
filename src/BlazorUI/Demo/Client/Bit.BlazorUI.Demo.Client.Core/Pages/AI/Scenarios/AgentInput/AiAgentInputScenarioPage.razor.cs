using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.AgentInput;

public partial class AiAgentInputScenarioPage
{
    private readonly BitAiChatSession _session = new(AiDemoAgentTransports.AsksForInput());

    private readonly string[] _suggestions = ["Migrate the database to the new schema"];

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        await _session.DisposeAsync();

        await base.DisposeAsync(disposing);
    }
}

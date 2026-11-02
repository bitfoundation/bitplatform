using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Errors;

public partial class AiErrorsScenarioPage
{
    private readonly BitAiChatSession _session = new(AiDemoInteractiveTransports.FailsThenRecovers());

    private readonly string[] _suggestions = ["Summarise the release notes"];

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        await _session.DisposeAsync();

        await base.DisposeAsync(disposing);
    }
}

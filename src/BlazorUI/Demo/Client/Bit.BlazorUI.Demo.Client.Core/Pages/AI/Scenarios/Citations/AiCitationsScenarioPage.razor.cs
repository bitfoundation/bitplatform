using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Citations;

public partial class AiCitationsScenarioPage
{
    private readonly BitAiChatSession _session = new(AiDemoContentTransports.Cited());
    private BitAiCitation? _selected;

    private readonly string[] _suggestions = ["What happens to state when a circuit drops?"];

    private void OnCitationClicked(BitAiCitation citation) => _selected = citation;

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        await _session.DisposeAsync();

        await base.DisposeAsync(disposing);
    }
}

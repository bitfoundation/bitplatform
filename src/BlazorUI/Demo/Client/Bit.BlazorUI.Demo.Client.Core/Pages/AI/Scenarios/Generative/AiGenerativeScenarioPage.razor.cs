using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Generative;

public partial class AiGenerativeScenarioPage
{
    private readonly BitAiChatSession _session = new(AiDemoInteractiveTransports.GenerativeUi());

    private readonly string[] _suggestions = ["What is the weather like in Oslo, Lisbon and Auckland?"];

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        await _session.DisposeAsync();

        await base.DisposeAsync(disposing);
    }
}

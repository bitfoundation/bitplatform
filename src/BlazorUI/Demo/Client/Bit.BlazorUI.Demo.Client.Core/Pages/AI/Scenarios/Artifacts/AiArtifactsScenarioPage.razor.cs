using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Artifacts;

public partial class AiArtifactsScenarioPage
{
    private readonly BitAiChatSession _session = new(AiDemoContentTransports.Artifacts());

    private readonly string[] _suggestions = ["Write me a README", "Tighten it and add an install step"];

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        await _session.DisposeAsync();

        await base.DisposeAsync(disposing);
    }
}

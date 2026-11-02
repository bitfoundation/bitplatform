using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Streaming;

public partial class AiStreamingScenarioPage
{
    private readonly BitAiChatSession _session = new(AiDemoTransports.Markdown());

    private readonly string[] _suggestions = ["Compare the render modes"];

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        await _session.DisposeAsync();

        await base.DisposeAsync(disposing);
    }
}

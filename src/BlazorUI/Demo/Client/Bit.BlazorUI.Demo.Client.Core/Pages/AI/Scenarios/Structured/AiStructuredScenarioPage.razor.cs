using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Structured;

public partial class AiStructuredScenarioPage
{
    private readonly BitAiChatSession _session = new(AiDemoContentTransports.Structured());

    private readonly string[] _suggestions = ["Compare our options"];

    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        await JSRuntime.InvokeVoid("highlightSnippet");
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        await _session.DisposeAsync();

        await base.DisposeAsync(disposing);
    }
}

using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Forms;

public partial class AiFormsScenarioPage
{
    private readonly BitAiChatSession _session = new(AiDemoInteractiveTransports.ProposesForm());
    private IReadOnlyDictionary<string, string?>? _values;
    private EventCallback<IReadOnlyDictionary<string, string?>> _onSubmitted;

    private readonly string[] _suggestions = ["Book a table for Friday"];

    protected override Task OnInitAsync()
    {
        _onSubmitted = EventCallback.Factory.Create<IReadOnlyDictionary<string, string?>>(this, values => _values = values);

        return base.OnInitAsync();
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        await _session.DisposeAsync();

        await base.DisposeAsync(disposing);
    }
}

namespace Boilerplate.Workbench.Components.Shell;

public partial class FlowsPanel
{
    private string? newFlowName;
    private string? stepNote;
    private List<BitDropdownItem<string>> flowItems = [];
    private string? flowsSignature;

    [AutoInject] private WorkbenchReviewSession session = default!;

    [Parameter] public string? SelectedFlowId { get; set; }

    [Parameter] public int PlayingIndex { get; set; } = -1;

    [Parameter] public EventCallback<string?> OnSelectFlow { get; set; }

    [Parameter] public EventCallback<int> OnPlay { get; set; }

    [Parameter] public EventCallback<string?> OnAddStep { get; set; }

    [Parameter] public EventCallback<WorkbenchFlow> OnScreenshots { get; set; }

    private WorkbenchFlow? Flow => session.Flows.FirstOrDefault(f => f.Id == SelectedFlowId);

    protected override void OnInitialized()
    {
        session.Changed += OnSessionChanged;
    }

    protected override void OnParametersSet()
    {
        BuildFlowItems();
    }

    private void BuildFlowItems()
    {
        var signature = string.Join('|', session.Flows.Select(f => $"{f.Id}:{f.Name}"));

        if (signature == flowsSignature)
            return;

        flowsSignature = signature;
        flowItems = [.. session.Flows.Select(f => new BitDropdownItem<string> { Value = f.Id, Text = f.Name })];
    }

    private async Task Create()
    {
        if (session.CreateFlow(newFlowName ?? string.Empty) is not { } id)
            return;

        newFlowName = null;

        await OnSelectFlow.InvokeAsync(id);
    }

    private async Task AddStep()
    {
        var note = stepNote;
        stepNote = null;

        await OnAddStep.InvokeAsync(note);
    }

    private string DescribeView(WorkbenchScenario scenario)
    {
        var device = scenario.DevicePreset;
        var parts = new List<string> { device.Key == DevicePresets.CustomKey ? FormattableString.Invariant($"{device.Width}×{device.Height}") : device.Name };

        if (scenario.Theme is not WorkbenchThemeMode.Light) parts.Add(Localizer[$"Theme{scenario.Theme}"]);
        if (scenario.Culture is not null) parts.Add(scenario.Culture);
        if (scenario.Data is not WorkbenchDataState.Live) parts.Add(Localizer[$"Data{scenario.Data}"]);

        return string.Join(" · ", parts);
    }

    private void OnSessionChanged()
    {
        BuildFlowItems();
        InvokeAsync(StateHasChanged);
    }

    public override async ValueTask DisposeAsync()
    {
        session.Changed -= OnSessionChanged;

        await base.DisposeAsync();
    }
}

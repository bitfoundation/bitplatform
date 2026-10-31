namespace Boilerplate.Workbench.Components.Shell;

public partial class FlowsPanel
{
    private const string NewFlowValue = "+";

    private string? targetFlowId;
    private string? newFlowName;
    private string? note;
    private string? linkLabel;
    private string? addedFlowId;
    private bool isAdding;
    private List<BitDropdownItem<string>> flowItems = [];
    private string? flowsSignature;

    [AutoInject] private WorkbenchReviewSession session = default!;

    [Parameter] public string PageKey { get; set; } = default!;

    [Parameter] public string Query { get; set; } = string.Empty;

    [Parameter] public Func<Task<string?>>? CaptureState { get; set; }

    private string TargetFlowId => targetFlowId is { } id && (id == NewFlowValue || session.Flows.Any(f => f.Id == id)) ? id : session.Flows.LastOrDefault()?.Id ?? NewFlowValue;

    private WorkbenchFlow? TargetFlow => session.Flows.FirstOrDefault(f => f.Id == TargetFlowId);

    private bool CanAdd => isAdding is false
                           && (TargetFlowId != NewFlowValue || string.IsNullOrWhiteSpace(newFlowName) is false)
                           && (TargetFlow?.Screens.Length ?? 0) < WorkbenchReviewDocument.MaxScreens;

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
        flowItems = [.. session.Flows.Select(f => new BitDropdownItem<string> { Value = f.Id, Text = f.Name }),
                     new() { Value = NewFlowValue, Text = Localizer[nameof(WorkbenchStrings.NewFlow)] }];
    }

    private async Task Add()
    {
        isAdding = true;

        try
        {
            var flowId = TargetFlowId == NewFlowValue ? session.CreateFlow(newFlowName ?? string.Empty) : TargetFlowId;

            if (flowId is null)
                return;

            var state = CaptureState is null ? null : await CaptureState();

            await session.AddScreen(flowId, PageKey, Query, note, state, linkLabel);

            targetFlowId = flowId;
            addedFlowId = flowId;
            newFlowName = null;
            note = null;
            linkLabel = null;
        }
        finally
        {
            isAdding = false;
        }
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

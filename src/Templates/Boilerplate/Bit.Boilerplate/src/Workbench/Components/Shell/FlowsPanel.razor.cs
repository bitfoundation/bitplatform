namespace Boilerplate.Workbench.Components.Shell;

/// <summary>
/// The Flows tab: lists the review's flows and, in dev mode only, adds the selected view as the next screen of a
/// flow, with an optional arrow from that flow's last screen.
/// </summary>
public partial class FlowsPanel
{
    // The dropdown value that stands for a new flow.
    private const string NewFlowValue = "+";

    private bool isAdding;
    private List<BitDropdownItem<string>> flowItems = [];
    // The flow ids and names the dropdown items were built from, so the items are rebuilt only when those change.
    private string? flowsSignature;

    [AutoInject] private WorkbenchReviewSession session = default!;

    /// <summary>What's chosen and typed in the form, which the page keeps as this panel comes and goes.</summary>
    [Parameter] public WorkbenchFlowDraft Draft { get; set; } = new();

    [Parameter] public string PageKey { get; set; } = default!;

    [Parameter] public string Query { get; set; } = string.Empty;

    /// <summary>Reads the selected view's page state, so the added screen opens the way the view shows it.</summary>
    [Parameter] public Func<Task<string?>>? CaptureState { get; set; }

    /// <summary>The chosen flow while it exists, otherwise the last flow, or a new flow when there is none.</summary>
    private string TargetFlowId => Draft.TargetFlowId is { } id && (id == NewFlowValue || session.Flows.Any(f => f.Id == id)) ? id : session.Flows.LastOrDefault()?.Id ?? NewFlowValue;

    private WorkbenchFlow? TargetFlow => session.Flows.FirstOrDefault(f => f.Id == TargetFlowId);

    private bool CanAdd => isAdding is false
                           && (TargetFlowId != NewFlowValue || string.IsNullOrWhiteSpace(Draft.NewFlowName) is false)
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

    /// <summary>
    /// Adds the view with its state, note and arrow label, creating the flow first when a new one is chosen; a new flow
    /// whose first screen couldn't be saved is removed again.
    /// </summary>
    private async Task Add()
    {
        isAdding = true;
        string? flowId = TargetFlowId;
        string? createdFlowId = null;

        try
        {
            var state = CaptureState is null ? null : await CaptureState();

            if (flowId == NewFlowValue)
            {
                flowId = createdFlowId = session.CreateFlow(Draft.NewFlowName ?? string.Empty);
            }

            if (flowId is null)
                return;

            await session.AddScreen(flowId, PageKey, Query, Draft.Note, state, Draft.LinkLabel);

            Draft.TargetFlowId = flowId;
            Draft.AddedFlowId = flowId;
            Draft.NewFlowName = null;
            Draft.Note = null;
            Draft.LinkLabel = null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            if (createdFlowId is not null)
            {
                session.DeleteFlow(createdFlowId);
            }

            SnackBarService.Error(Localizer[nameof(WorkbenchStrings.AddScreenFailed)], exception.Message);
        }
        finally
        {
            isAdding = false;
        }
    }

    // Changed is raised on the thread that changed the review, so the items are rebuilt on the renderer's.
    private void OnSessionChanged() => InvokeAsync(() =>
    {
        BuildFlowItems();
        StateHasChanged();
    });

    public override async ValueTask DisposeAsync()
    {
        session.Changed -= OnSessionChanged;

        await base.DisposeAsync();
    }
}

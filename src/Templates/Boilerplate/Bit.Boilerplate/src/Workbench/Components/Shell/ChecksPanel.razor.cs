namespace Boilerplate.Workbench.Components.Shell;

public partial class ChecksPanel
{
    private bool isRunning;
    private bool hasFailed;
    private string? checkedPaneId;
    private WorkbenchCheckResult? result;

    [Parameter] public string PaneId { get; set; } = default!;

    protected override void OnParametersSet()
    {
        if (checkedPaneId is not null && checkedPaneId != PaneId)
        {
            result = null;
            checkedPaneId = null;
        }
    }

    private async Task Run()
    {
        isRunning = true;
        hasFailed = false;

        try
        {
            result = await JSRuntime.InvokeAsync<WorkbenchCheckResult?>("workbench.checkFrame", PaneId);
            checkedPaneId = PaneId;
            hasFailed = result is null;
        }
        catch (JSException)
        {
            hasFailed = true;
        }
        finally
        {
            isRunning = false;
        }
    }

    private async Task Highlight(string selector)
    {
        await JSRuntime.InvokeAsync<bool>("workbench.highlightInFrame", PaneId, selector);
    }
}

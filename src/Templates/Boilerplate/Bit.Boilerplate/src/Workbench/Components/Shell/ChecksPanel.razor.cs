namespace Boilerplate.Workbench.Components.Shell;

/// <summary>
/// The Checks tab: runs axe-core and the search for colors that don't come from the theme inside the selected view's
/// frame, and lists what they find.
/// </summary>
public partial class ChecksPanel
{
    private bool isRunning;
    private bool hasFailed;
    // The view the result was found on, as CurrentView put it then.
    private string? checkedView;
    private WorkbenchCheckResult? result;

    [Parameter] public string PaneId { get; set; } = default!;

    /// <summary>The selected view's page and scenario, so a result is dropped once the view shows something else.</summary>
    [Parameter] public string? View { get; set; }

    private string CurrentView => $"{PaneId}|{View}";

    protected override void OnParametersSet()
    {
        // A result belongs to the view it ran on, so selecting or changing the view clears it.
        if (checkedView is not null && checkedView != CurrentView)
        {
            result = null;
            checkedView = null;
        }
    }

    /// <summary>Runs the checks in the view's frame; a null result means the frame didn't answer in time.</summary>
    private async Task Run()
    {
        var view = CurrentView;

        isRunning = true;
        hasFailed = false;

        try
        {
            var found = await JSRuntime.InvokeAsync<WorkbenchCheckResult?>("workbench.checkFrame", PaneId);

            // The view changed while the checks ran, so the result describes a view that is no longer shown.
            if (view != CurrentView)
                return;

            result = found;
            checkedView = view;
            hasFailed = found is null;
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

    /// <summary>Outlines the element a finding names inside the view's frame.</summary>
    private async Task Highlight(string selector)
    {
        try
        {
            await JSRuntime.InvokeAsync<bool>("workbench.highlightInFrame", PaneId, selector);
        }
        catch (JSException)
        {
            // The frame has moved on, so there is nothing left to outline.
        }
    }
}

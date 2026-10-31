namespace Boilerplate.Workbench.Components.Preview;

public partial class PreviewSurface
{
    [Parameter] public WorkbenchPageInfo Page { get; set; } = default!;

    [Parameter] public bool ShowStatusBar { get; set; }
}

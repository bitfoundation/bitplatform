namespace Boilerplate.Workbench.Components.Pages.AppPages.Dashboard;

/// <summary>The Workbench copy of the app's status card: one figure with its label and icon.</summary>
public partial class OverallStatusCard
{
    [Parameter] public bool Loading { get; set; }
    [Parameter] public int Value { get; set; }
    [Parameter] public string? Label { get; set; }
    [Parameter] public string? IconName { get; set; }
}

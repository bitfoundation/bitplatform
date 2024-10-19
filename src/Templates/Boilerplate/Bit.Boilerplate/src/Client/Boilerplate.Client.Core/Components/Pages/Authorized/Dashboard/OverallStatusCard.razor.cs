namespace Boilerplate.Client.Core.Components.Pages.Authorized.Dashboard;

public partial class OverallStatusCard
{
    [Parameter] public bool Loading { get; set; }
    [Parameter] public int Value { get; set; }
    [Parameter] public string? Label { get; set; }
    [Parameter] public string? IconName { get; set; }
}

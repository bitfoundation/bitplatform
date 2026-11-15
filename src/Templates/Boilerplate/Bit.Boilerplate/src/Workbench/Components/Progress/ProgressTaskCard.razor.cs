namespace Boilerplate.Workbench.Components.Progress;

/// <summary>A task in the Progress page's list, and on the home page: its title, agent, how far its steps are and what waits for the user.</summary>
public partial class ProgressTaskCard
{
    [Parameter, EditorRequired] public WorkbenchProgressTask ProgressTask { get; set; } = default!;

    [Parameter] public bool Selected { get; set; }
}

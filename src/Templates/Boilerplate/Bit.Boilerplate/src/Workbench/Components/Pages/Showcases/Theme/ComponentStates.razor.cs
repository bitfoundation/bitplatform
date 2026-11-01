namespace Boilerplate.Workbench.Components.Pages.Showcases.Theme;

/// <summary>One component's block on the Theme page: its name over a grid of its states.</summary>
public partial class ComponentStates
{
    [Parameter, EditorRequired] public string Name { get; set; } = default!;

    /// <summary>Gives each state more room, for wide components like text fields and dropdowns.</summary>
    [Parameter] public bool Wide { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }
}

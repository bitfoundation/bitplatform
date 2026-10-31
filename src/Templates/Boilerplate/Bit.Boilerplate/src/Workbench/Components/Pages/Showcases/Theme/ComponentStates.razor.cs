namespace Boilerplate.Workbench.Components.Pages.Showcases.Theme;

public partial class ComponentStates
{
    [Parameter, EditorRequired] public string Name { get; set; } = default!;

    [Parameter] public bool Wide { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }
}

namespace Boilerplate.Workbench.Components.Pages.Showcases.Theme;

/// <summary>One component's block on the Theme page: its name over a grid of its states.</summary>
public partial class ComponentStates
{
    [Parameter, EditorRequired] public string Name { get; set; } = default!;

    /// <summary>Gives each state more room, for wide components like text fields and dropdowns.</summary>
    [Parameter] public bool Wide { get; set; }

    /// <summary>Gives each state the whole width, for large components like grids, maps and editors.</summary>
    [Parameter] public bool Full { get; set; }

    /// <summary>Lets each state take the width its component needs and wraps them, for components that size themselves, like calendars.</summary>
    [Parameter] public bool Flow { get; set; }

    /// <summary>The narrowest each state's column gets, such as 20rem, for components wider than <see cref="Wide"/> gives.</summary>
    [Parameter] public string? CellWidth { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

    private string? StatesClass => Full ? "full" : Flow ? "flow" : CellWidth is not null ? "sized" : Wide ? "wide" : null;
}

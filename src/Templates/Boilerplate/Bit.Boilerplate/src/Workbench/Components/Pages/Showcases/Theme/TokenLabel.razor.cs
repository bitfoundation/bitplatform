namespace Boilerplate.Workbench.Components.Pages.Showcases.Theme;

/// <summary>A token's name over the value it has in the view, both in small type under its sample.</summary>
public partial class TokenLabel
{
    [Parameter, EditorRequired] public string Name { get; set; } = default!;

    /// <summary>The value the browser resolved; empty until it has been read.</summary>
    [Parameter] public string? Value { get; set; }
}

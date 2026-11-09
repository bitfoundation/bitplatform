namespace Boilerplate.Workbench.Components.Pages.Showcases.Theme;

/// <summary>One section of the Theme page: a card with its heading, an optional line under it, and its content.</summary>
public partial class ThemeSection
{
    /// <summary>The section's key, also its element id: the same as its tab's key and its Section switch value.</summary>
    [Parameter, EditorRequired] public string Id { get; set; } = default!;

    [Parameter, EditorRequired] public string Title { get; set; } = default!;

    [Parameter] public string? Hint { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>The section's element, for scrolling it into view.</summary>
    public ElementReference Element { get; private set; }
}

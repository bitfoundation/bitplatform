namespace Boilerplate.Workbench.Components.Pages.Showcases.Theme;

public partial class StateSample
{
    [Parameter] public string? NameKey { get; set; }

    [Parameter] public string? Name { get; set; }

    [Parameter] public string? Force { get; set; }

    [Parameter] public string? Target { get; set; }

    [Parameter] public int? Index { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }
}

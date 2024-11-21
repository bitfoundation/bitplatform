namespace Bit.BlazorUI;

public partial class _BitNavBarChild
{
    [Parameter] public RenderFragment? ChildContent { get; set; }

    [Parameter] public string? Class { get; set; }

    [Parameter] public bool Disabled { get; set; }

    [Parameter] public string? Href { get; set; }

    [Parameter] public EventCallback OnClick { get; set; }

    [Parameter] public string? Style { get; set; }

    [Parameter] public string? Target { get; set; }

    [Parameter] public string? Title { get; set; }

}

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.AccordionList;

public class Section
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? Info { get; set; }

    public bool IsEnabled { get; set; } = true;

    public string? Class { get; set; }

    public string? Style { get; set; }

    public string? Image { get; set; }

    public RenderFragment<Section>? Content { get; set; }

    public Action<Section>? Clicked { get; set; }
}

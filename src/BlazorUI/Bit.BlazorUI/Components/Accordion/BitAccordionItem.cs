
namespace Bit.BlazorUI;

public class BitAccordionItem
{
    public RenderFragment? Content { get; set; }

    public string? Description { get; set; }

    public bool IsExpanded { get; set; }

    public bool IsEnabled { get; set; } = true;

    public string? Title { get; set; }

    public string? Text { get; set; }
}

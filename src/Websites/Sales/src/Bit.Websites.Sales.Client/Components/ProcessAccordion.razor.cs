namespace Bit.Websites.Sales.Client.Components;

public partial class ProcessAccordion
{
    [Parameter] public string? Title { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
}

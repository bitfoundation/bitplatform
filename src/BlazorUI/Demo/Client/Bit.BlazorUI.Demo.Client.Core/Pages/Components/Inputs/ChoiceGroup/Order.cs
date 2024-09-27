namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.ChoiceGroup;

public class Order
{
    public string? Name { get; set; }
    public string? ItemValue { get; set; }
    public string? ImageAddress { get; set; }
    public string? ImageDescription { get; set; }
    public BitImageSize? ImageSize { get; set; }
    public string? SelectedImageAddress { get; set; }
    public string? IconName { get; set; }
    public bool IsDisabled { get; set; }
    public string? Class { get; set; }
    public string? Style { get; set; }
    public RenderFragment<Order>? Fragment { get; set; }
    public int Idx { get; set; }
}

using System.Drawing;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.ChoiceGroup;

public class ChoiceModel
{
    public string Name { get; set; }
    public string ItemValue { get; set; }
    public string ImageAddress { get; set; }
    public string ImageDescription { get; set; }
    public Size? ImageSize { get; set; }
    public string SelectedImageAddress { get; set; }
    public BitIconName? IconName { get; set; }
    public bool IsEnabled { get; set; } = true;
}

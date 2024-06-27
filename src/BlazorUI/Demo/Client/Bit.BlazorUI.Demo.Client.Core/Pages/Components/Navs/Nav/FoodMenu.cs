namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Nav;

public class FoodMenu
{
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public bool IsExpanded { get; set; }
    public List<FoodMenu> Childs { get; set; } = [];
    public string? Comment { get; set; }
}

using System.Collections.Generic;

namespace Bit.BlazorUI.Demo.Web.Pages.Components.Nav;

public class FoodMenu
{
    public string Name { get; set; } = string.Empty;
    public BitIconName Icon { get; set; }
    public bool IsExpanded { get; set; }
    public List<FoodMenu> Childs { get; set; } = new();
}

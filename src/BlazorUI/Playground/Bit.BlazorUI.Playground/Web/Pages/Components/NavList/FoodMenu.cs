using System.Collections.Generic;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.NavList;

public class FoodMenu
{
    public string Name { get; set; } = string.Empty;
    public BitIconName Icon { get; set; }
    public List<FoodMenu> Childs { get; set; } = new();
}

using System.Collections.Generic;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.NavList;

public class NavItemModel
{    
    public string Text { get; set; } = string.Empty;
    public string Title { get; set; }
    public string Url { get; set; }
    public string Target { get; set; }
    public BitIconName IconName { get; set; }
    public string ExpandAriaLabel { get; set; }
    public string CollapseAriaLabel { get; set; }
    public bool IsExpanded { get; set; }
    public bool IsEnabled { get; set; } = true;
    public List<NavItemModel> Items { get; set; } = new();
}

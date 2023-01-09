using System.Collections.Generic;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.NavList;

public class NavMenuModel
{
    public string Name { get; set; }
    public string TitleAttribute { get; set; }
    public string Url { get; set; }
    public string Target { get; set; }
    public string Key { get; set; }
    public bool IsExpanded { get; set; }
    public BitIconName IconName { get; set; }

    public List<NavMenuModel> Items { get; set; }
}

using Bit.Client.Web.BlazorUI;

namespace TodoTemplate.App.Components;

public partial class NavMenu
{
    private readonly List<BitNavLinkItem> NavLinks = new()
    {
        new BitNavLinkItem
        {
            Name = "Home",
            Url = "/",
            IconName = BitIconName.Home,
            Key = "Home"
        },
        new BitNavLinkItem
        {
            Name = "Todo",
            Url = "/todo",
            IconName = BitIconName.ToDoLogoOutline,
            Key = "Todo"
        },
        new BitNavLinkItem
        {
            Name = "Sign out",
            IconName = BitIconName.SignOut,
            Key = "SignOut"
        }
    };
}

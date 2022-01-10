using Bit.Client.Web.BlazorUI;

namespace TodoTemplate.App.Components
{
    public partial class NavMenu
    {
        private readonly List<BitNavLinkItem> NavLinks = new()
        {
            new BitNavLinkItem
            {
                Name = "Home",
                Url = "/",
                Icon = "Home",
                Key = "Home"
            },
            new BitNavLinkItem
            {
                Name = "To do",
                Url = "/to-do",
                Icon = "TaskLogo",
                Key = "ToDo"
            },
            new BitNavLinkItem
            {
                Name = "Log out",
                Icon = "ReleaseGate",
                Key = "LogOut"
            }
        };
    }
}

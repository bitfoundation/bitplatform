﻿namespace TodoTemplate.App.Components;

public partial class NavMenu
{
    [Inject]
    public ITodoTemplateAuthenticationService TodoTemplateAuthenticationService { get; set; } = default!;

    public NavMenu()
    {
        NavLinks = new()
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
                OnClick = (item) =>
                {
                    TodoTemplateAuthenticationService.SignOut();
                },
                IconName = BitIconName.SignOut,
                Key = "SignOut"
            }
        };
    }

    public List<BitNavLinkItem> NavLinks { get; set; }
}

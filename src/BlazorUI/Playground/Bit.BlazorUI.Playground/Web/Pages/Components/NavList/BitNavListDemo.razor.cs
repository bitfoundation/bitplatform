using System.Collections.Generic;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.NavList;

public partial class BitNavListDemo
{
    public List<NavMenuModel> navListItems { get; set; } = new List<NavMenuModel>
    {
        new NavMenuModel {
            Name = "Name 1",
            IconName = BitIconName.Calculator,
            Items = new List<NavMenuModel>
            {
                new NavMenuModel {
                    Name = "Name 1-1",
                    Target = "_blank",
                    Url = "http://msn.com",
                },
                new NavMenuModel {
                    Name = "Name 1-2",
                    Url = "http://msn.com"
                },
                new NavMenuModel
                {
                    Name = "Name 1-5",
                    Url = "http://msn.com",
                    Items = new List<NavMenuModel>
                    {
                        new NavMenuModel {
                            Name = "Name 1-5-1",
                            Url = "http://msn.com",
                        },
                        new NavMenuModel {
                            Name = "Name 1-5-2",
                            Url = "http://msn.com"
                        },
                        new NavMenuModel {
                            Name = "Name 1-5-3",
                            Url = "http://msn.com"
                        },
                    }
                },
            }
        },
        new NavMenuModel {
            Name = "Name 2",
            Url = "http://msn.com",
        },
        new NavMenuModel {
            Name = "Name 3",
            Url = "http://msn.com",
        },
    };

    public List<NavMenuModel> navListGroupItems { get; set; } = new List<NavMenuModel>
    {
        new NavMenuModel {
            Name = "Name 1",
            Url = "http://msn.com",
            TitleAttribute = "First Group",
            IsExpanded = true,
            Items = new List<NavMenuModel>
            {
                new NavMenuModel
                {
                    Name = "1-1",
                    Url = "http://msn.com",
                },
                new NavMenuModel
                {
                    Name = "1-2",
                    Url = "http://msn.com",
                },
                new NavMenuModel
                {
                    Name = "1-3",
                    Url = "http://msn.com",
                },
            }
        },
        new NavMenuModel {
            Name = "Name 2",
            Url = "http://msn.com",
            Items = new List<NavMenuModel>
            {
                new NavMenuModel
                {
                    Name = "2-1",
                    Url = "http://msn.com",
                },
                new NavMenuModel
                {
                    Name = "2-2",
                    Url = "http://msn.com",
                },
                new NavMenuModel
                {
                    Name = "2-3",
                    Url = "http://msn.com",
                },
            }
        },
        new NavMenuModel {
            Name = "Name 3",
            Url = "http://msn.com",
            Items = new List<NavMenuModel>
            {
                new NavMenuModel
                {
                    Name = "3-1",
                    Url = "http://msn.com",
                },
                new NavMenuModel
                {
                    Name = "3-2",
                    Url = "http://msn.com",
                },
                new NavMenuModel
                {
                    Name = "3-3",
                    Url = "http://msn.com",
                },
            }
        },

    };
}

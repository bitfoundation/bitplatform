using System.Collections.Generic;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.NavList;

public partial class BitNavListDemo
{
    public List<NavMenuModel> navListItems { get; set; } = new List<NavMenuModel>
    {
        new NavMenuModel {
            Name = "Name 1",
            Items = new List<NavMenuModel>
            {
                new NavMenuModel {
                    Name = "Name 1-1",
                },
                new NavMenuModel {
                    Name = "Name 1-2",
                },
                new NavMenuModel
                {
                    Name = "Name 1-5",
                    Items = new List<NavMenuModel>
                    {
                        new NavMenuModel {
                            Name = "Name 1-5-1",
                        },
                        new NavMenuModel {
                            Name = "Name 1-5-2",
                        },
                        new NavMenuModel {
                            Name = "Name 1-5-3",
                        },
                    }
                },
            }
        },
        new NavMenuModel {
            Name = "Name 2",
        },
        new NavMenuModel {
            Name = "Name 3",
        },
    };

    public List<NavMenuModel> navListGroupItems { get; set; } = new List<NavMenuModel>
    {
        new NavMenuModel {
            Name = "Name 1",
            Items = new List<NavMenuModel>
            {
                new NavMenuModel
                {
                    Name = "1-1",
                },
                new NavMenuModel
                {
                    Name = "1-2",
                },
                new NavMenuModel
                {
                    Name = "1-3",
                },
            }
        },
        new NavMenuModel {
            Name = "Name 2",
            Items = new List<NavMenuModel>
            {
                new NavMenuModel
                {
                    Name = "2-1",
                },
                new NavMenuModel
                {
                    Name = "2-2",
                },
                new NavMenuModel
                {
                    Name = "2-3",
                },
            }
        },
        new NavMenuModel {
            Name = "Name 3",
            Items = new List<NavMenuModel>
            {
                new NavMenuModel
                {
                    Name = "3-1",
                },
                new NavMenuModel
                {
                    Name = "3-2",
                },
                new NavMenuModel
                {
                    Name = "3-3",
                },
            }
        },

    };
}

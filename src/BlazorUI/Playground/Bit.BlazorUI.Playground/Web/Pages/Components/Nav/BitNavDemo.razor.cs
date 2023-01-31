using System.Collections.Generic;
using System.Linq;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Nav;

public partial class BitNavDemo
{

    // Basic
    private static readonly List<BitNavItem> BitPlatformNavMenu = new()
    {
        new BitNavItem
        {
            Text = "Bit Platform",
            Items = new List<BitNavItem>
            {
                new BitNavItem { Text = "Home", Url = "https://bitplatform.dev/" },
                new BitNavItem
                {
                    Text = "Products & Services",
                    Items = new List<BitNavItem>
                    {
                        new BitNavItem
                        {
                            Text = "Project Templates",
                            Items = new List<BitNavItem>
                            {
                                new BitNavItem { Text = "TodoTemplate", Url = "https://bitplatform.dev/todo-template/overview" },
                                new BitNavItem { Text = "AdminPanel", Url = "https://bitplatform.dev/admin-panel/overview" },
                            }
                        },
                        new BitNavItem { Text = "BlazorUI", Url = "https://bitplatform.dev/components" },
                        new BitNavItem { Text = "Cloud hosting solutions", Url = "https://bitplatform.dev/#", IsEnabled = false },
                        new BitNavItem { Text = "Bit academy", Url = "https://bitplatform.dev/#", IsEnabled = false },
                    }
                },
                new BitNavItem { Text = "Pricing", Url = "https://bitplatform.dev/pricing" },
                new BitNavItem { Text = "About", Url = "https://bitplatform.dev/about-us" },
                new BitNavItem { Text = "Contact us", Url = "https://bitplatform.dev/contact-us" },
            },
        },
        new BitNavItem
        {
            Text = "Community",
            Items = new List<BitNavItem>
            {
                new BitNavItem { Text = "Linkedin", Url = "https://www.linkedin.com/company/bitplatformhq/about/" },
                new BitNavItem { Text = "Twitter", Url = "https://twitter.com/bitplatformhq" },
                new BitNavItem { Text = "Github repo", Url = "https://github.com/bitfoundation/bitplatform" },
            }
        },
        new BitNavItem { Text = "Iconography", Url = "/icons" },
    };

    // Grouped
    private static readonly List<BitNavItem> CarNavMenu = new()
    {
        new BitNavItem
        {
            Text = "Mercedes-Benz",
            ExpandAriaLabel = "Mercedes-Benz Expanded",
            CollapseAriaLabel = "Mercedes-Benz Collapsed",
            Title = "Mercedes-Benz Car Models",
            IsExpanded = true,
            Items = new List<BitNavItem>
            {
                new BitNavItem
                {
                    Text = "SUVs",
                    Items = new List<BitNavItem>
                    {
                        new BitNavItem { Text = "GLA", Url = "https://www.mbusa.com/en/vehicles/class/gla/suv", Target = "_blank" },
                        new BitNavItem { Text = "GLB", Url = "https://www.mbusa.com/en/vehicles/class/glb/suv", Target = "_blank" },
                        new BitNavItem { Text = "GLC", Url = "https://www.mbusa.com/en/vehicles/class/glc/suv", Target = "_blank" },
                    }
                },
                new BitNavItem
                {
                    Text = "Sedans & Wagons",
                    Items = new List<BitNavItem>
                    {
                        new BitNavItem { Text = "A Class", Url = "https://www.mbusa.com/en/vehicles/class/a-class/sedan", Target = "_blank" },
                        new BitNavItem { Text = "C Class", Url = "https://www.mbusa.com/en/vehicles/class/c-class/sedan", Target = "_blank" },
                        new BitNavItem { Text = "E Class", Url = "https://www.mbusa.com/en/vehicles/class/e-class/sedan", Target = "_blank" },
                    }
                },
                new BitNavItem
                {
                    Text = "Coupes",
                    Items = new List<BitNavItem>
                    {
                        new BitNavItem { Text = "CLA Coupe", Url = "https://www.mbusa.com/en/vehicles/class/cla/coupe", Target = "_blank" },
                        new BitNavItem { Text = "C Class Coupe", Url = "https://www.mbusa.com/en/vehicles/class/c-class/coupe", Target = "_blank" },
                        new BitNavItem { Text = "E Class Coupe", Url = "https://www.mbusa.com/en/vehicles/class/e-class/coupe", Target = "_blank" },
                    }
                },
            }
        },
        new BitNavItem
        {
            Text = "Tesla",
            ExpandAriaLabel = "Tesla Expanded",
            CollapseAriaLabel= "Tesla Collapsed",
            Title = "Tesla Car Models",
            Items = new List<BitNavItem>
            {
                new BitNavItem { Text = "Model S", Url = "https://www.tesla.com/models", Target = "_blank" },
                new BitNavItem { Text = "Model X", Url = "https://www.tesla.com/modelx", Target = "_blank" },
                new BitNavItem { Text = "Model Y", Url = "https://www.tesla.com/modely", Target = "_blank" },
            }
        },
    };

    // Manual
    private static readonly List<BitNavItem> FoodNavMenu = new()
    {
        new BitNavItem
        {
            Text = "Fast-Food",
            IconName = BitIconName.HeartBroken,
            IsExpanded = true,
            Items = new List<BitNavItem>
            {
                new BitNavItem
                {
                    Text = "Burgers",
                    Items = new List<BitNavItem>
                    {
                        new BitNavItem { Text = "Beef Burger" },
                        new BitNavItem { Text = "Veggie Burger" },
                        new BitNavItem { Text = "Bison Burger" },
                        new BitNavItem { Text = "Wild Salmon Burger" },
                    }
                },
                new BitNavItem
                {
                    Text = "Pizzas",
                    Items = new List<BitNavItem>
                    {
                        new BitNavItem { Text = "Cheese Pizza" },
                        new BitNavItem { Text = "Veggie Pizza" },
                        new BitNavItem { Text = "Pepperoni Pizza" },
                        new BitNavItem { Text = "Meat Pizza" },
                    }
                },
                new BitNavItem { Text    = "French Fries" },
            }
        },
        new BitNavItem
        {
            Text = "Fruits",
            IconName = BitIconName.Health,
            Items = new List<BitNavItem>
            {
                new BitNavItem { Text = "Aplle" },
                new BitNavItem { Text = "Orange" },
                new BitNavItem { Text = "Benana" },
            }
        },
        new BitNavItem { Text = "Ice Cream" },
        new BitNavItem { Text = "Cookie" },
    };

    private static readonly List<BitDropDownItem> FoodMenuDropDownItems = new()
    {
        new BitDropDownItem
        {
            Text = "Beef Burger",
            Value = "Beef Burger",
        },
        new BitDropDownItem
        {
            Text = "Veggie Burger",
            Value = "Veggie Burger",
        },
        new BitDropDownItem
        {
            Text = "Bison Burger",
            Value = "Bison Burger",
        },
        new BitDropDownItem
        {
            Text = "Wild Salmon Burger",
            Value = "Wild Salmon Burger",
        },
        new BitDropDownItem
        {
            Text = "Cheese Pizza",
            Value = "Cheese Pizza",
        },
        new BitDropDownItem
        {
            Text = "Veggie Pizza",
            Value = "Veggie Pizza",
        },
        new BitDropDownItem
        {
            Text = "Pepperoni Pizza",
            Value = "Pepperoni Pizza",
        },
        new BitDropDownItem
        {
            Text = "Meat Pizza",
            Value = "Meat Pizza",
        },
        new BitDropDownItem
        {
            Text = "French Fries",
            Value = "French Fries",
        },
        new BitDropDownItem
        {
            Text = "Aplle",
            Value = "Aplle",
        },
        new BitDropDownItem
        {
            Text = "Orange",
            Value = "Orange",
        },
        new BitDropDownItem
        {
            Text = "Benana",
            Value = "Benana",
        },
        new BitDropDownItem
        {
            Text = "Ice Cream",
            Value = "Ice Cream",
        },
        new BitDropDownItem
        {
            Text = "Cookie",
            Value = "Cookie",
        },
    };
    private static List<BitNavItem> Flatten(IList<BitNavItem> e) => e.SelectMany(c => Flatten(c.Items)).Concat(e).ToList();
    private BitNavItem SelectedItemNav = FoodNavMenu[0].Items[2];
    private string SelectedItemText = FoodNavMenu[0].Items[2].Text;

    private BitNavItem ClickedItem;
    private BitNavItem SelectedItem;
    private BitNavItem ToggledItem;

    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter
        {
            Name = "DefaultSelectedItem",
            Type = "BitNavItem?",
            Description = "The initially selected item in manual mode.",
            Href = "#nav-item",
            LinkType = LinkType.Link,
        },
        new ComponentParameter
        {
            Name = "HeaderTemplate",
            Type = "RenderFragment<BitNavItem>?",
            Description = "Used to customize how content inside the group header is rendered.",
        },
        new ComponentParameter
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<BitNavItem>?",
            Description = "Used to customize how content inside the link tag is rendered.",
        },
        new ComponentParameter
        {
            Name = "Items",
            Type = "IList<BitNavItem>",
            DefaultValue = "new List<BitNavItem>()",
            Description = "A collection of link items to display in the navigation bar.",
        },
        new ComponentParameter
        {
            Name = "Mode",
            Type = "BitNavMode",
            DefaultValue = "BitNavMode.Automatic",
            Description = "Determines how the navigation will be handled. The default value is Automatic.",
            Href = "#nav-mode-enum",
            LinkType = LinkType.Link
        },
        new ComponentParameter
        {
            Name = "OnItemClick",
            Type = "EventCallback<BitNavItem>",
            Description = "Callback invoked when an item is clicked.",
        },
        new ComponentParameter
        {
            Name = "OnSelectItem",
            Type = "EventCallback<BitNavItem>",
            Description = "Callback invoked when an item is selected.",
        },
        new ComponentParameter
        {
            Name = "OnItemToggle",
            Type = "EventCallback<BitNavItem>",
            Description = "Callback invoked when a group header is clicked and Expanded or Collapse.",
        },
        new ComponentParameter
        {
            Name = "RenderType",
            Type = "BitNavRenderType",
            DefaultValue = "BitNavRenderType.Normal",
            Description = "The way to render nav links.",
            Href = "#nav-render-type-enum",
            LinkType = LinkType.Link,
        },
        new ComponentParameter
        {
            Name = "SelectedItem",
            Type = "BitNavItem?",
            Description = "Selected item to show in Nav.",
        },
    };
    private readonly List<ComponentSubParameter> componentSubParameters = new()
    {
        new ComponentSubParameter()
        {
            Id = "nav-item",
            Title = "BitNavItem",
            Parameters = new List<ComponentParameter>()
            {
               new ComponentParameter()
               {
                   Name = "AriaLabel",
                   Type = "string?",
                   Description = "Aria label for nav link. Ignored if collapseAriaLabel or expandAriaLabel is provided",
               },
               new ComponentParameter()
               {
                   Name = "AriaCurrent",
                   Type = "BitNavLinkItemAriaCurrent",
                   DefaultValue = "BitNavItemAriaCurrent.Page",
                   Description = "Aria-current token for active nav links. Must be a valid token value, and defaults to 'page'",
                   Href = "#nav-item-aria-current-enum",
                   LinkType = LinkType.Link,
               },
               new ComponentParameter()
               {
                   Name = "CollapseAriaLabel",
                   Type = "string?",
                   Description = "Aria label when items is collapsed and can be expanded.",
               },
               new ComponentParameter()
               {
                   Name = "ExpandAriaLabel",
                   Type = "string?",
                   Description = "Aria label when group is collapsed and can be expanded.",
               },
               new ComponentParameter()
               {
                   Name = "ForceAnchor",
                   Type = "bool",
                   Description = "(Optional) By default, any link with onClick defined will render as a button. Set this property to true to override that behavior. (Links without onClick defined will render as anchors by default.)",
               },
               new ComponentParameter()
               {
                   Name = "Items",
                   Type = "IList<BitNavItem>",
                   DefaultValue = "new List<BitNavItem>()",
                   Description = "A list of items to render as children of the current item.",
               },
               new ComponentParameter()
               {
                   Name = "IconName",
                   Type = "BitIconName",
                   Description = "Name of an icon to render next to this link button.",
               },
               new ComponentParameter()
               {
                   Name = "IsExpanded",
                   Type = "bool",
                   Description = "Whether or not the link is in an expanded state.",
               },
               new ComponentParameter()
               {
                   Name = "IsEnabled",
                   Type = "bool",
                   Description = "Whether or not the link is enabled.",
               },
               new ComponentParameter()
               {
                   Name = "Key",
                   Type = "string?",
                   Description = "A unique value to use as a key or id of the item.",
               },
               new ComponentParameter()
               {
                   Name = "Style",
                   Type = "string?",
                   Description = "Custom style for the each item element.",
               },
               new ComponentParameter()
               {
                   Name = "Text",
                   Type = "string",
                   DefaultValue = "string.Empty",
                   Description = "Text to render for this link.",
               },
               new ComponentParameter()
               {
                   Name = "Title",
                   Type = "string?",
                   Description = "Text for title tooltip.",
               },
               new ComponentParameter()
               {
                   Name = "Target",
                   Type = "string?",
                   Description = "Link target, specifies how to open the link.",
               },
               new ComponentParameter()
               {
                   Name = "Url",
                   Type = "string?",
                   Description = "URL to navigate to for this link.",
               }
            }
        }
    };
    private readonly List<EnumParameter> enumParameters = new()
    {
        new EnumParameter()
        {
            Id = "nav-mode-enum",
            Title = "BitNavMode Enum",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name = "Automatic",
                    Description = "The value of selected key will change using NavigationManager and the current url inside the component.",
                    Value = "0",
                },
                new EnumItem()
                {
                    Name = "Manual",
                    Description = "Selected key changes will be sent back to the parent component and the component won't change its value.",
                    Value = "1",
                }
            }
        },
        new EnumParameter()
        {
            Id = "nav-render-type-enum",
            Title = "BitNavRenderType Enum",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name = "Normal",
                    Value = "0",
                },
                new EnumItem()
                {
                    Name = "Grouped",
                    Value = "1",
                }
            }
        },
        new EnumParameter()
        {
            Id = "nav-item-aria-current-enum",
            Title = "BitNavItemAriaCurrent Enum",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name = "Page",
                    Value = "0",
                },
                new EnumItem()
                {
                    Name = "Step",
                    Value = "1",
                },
                new EnumItem()
                {
                    Name = "Location",
                    Value = "2",
                },
                new EnumItem()
                {
                    Name = "Date",
                    Value = "3",
                },
                new EnumItem()
                {
                    Name = "Time",
                    Value = "4",
                },
                new EnumItem()
                {
                    Name = "True",
                    Value = "5",
                },

            }
        },
    };

    #region Sample Code 1

    private static string example1HTMLCode = @"
<BitNav Items=""BitPlatformNavMenu"" />
";

    private static string example1CSharpCode = @"
private static readonly List<BitNavItem> BitPlatformNavMenu = new()
{
    new BitNavItem
    {
        Text = ""Bit Platform"",
        Items = new List<BitNavItem>
        {
            new BitNavItem { Text = ""Home"", Url = ""https://bitplatform.dev/"" },
            new BitNavItem
            {
                Text = ""Products & Services"",
                Items = new List<BitNavItem>
                {
                    new BitNavItem
                    {
                        Text = ""Project Templates"",
                        Items = new List<BitNavItem>
                        {
                            new BitNavItem { Text = ""TodoTemplate"", Url = ""https://bitplatform.dev/todo-template/overview"" },
                            new BitNavItem { Text = ""AdminPanel"", Url = ""https://bitplatform.dev/admin-panel/overview"" },
                        }
                    },
                    new BitNavItem { Text = ""BlazorUI"", Url = ""https://bitplatform.dev/components"" },
                    new BitNavItem { Text = ""Cloud hosting solutions"", Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                    new BitNavItem { Text = ""Bit academy"", Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                }
            },
            new BitNavItem { Text = ""Pricing"", Url = ""https://bitplatform.dev/pricing"" },
            new BitNavItem { Text = ""About"", Url = ""https://bitplatform.dev/about-us"" },
            new BitNavItem { Text = ""Contact us"", Url = ""https://bitplatform.dev/contact-us"" },
        },
    },
    new BitNavItem
    {
        Text = ""Community"",
        Items = new List<BitNavItem>
        {
            new BitNavItem { Text = ""Linkedin"", Url = ""https://www.linkedin.com/company/bitplatformhq/about/"" },
            new BitNavItem { Text = ""Twitter"", Url = ""https://twitter.com/bitplatformhq"" },
            new BitNavItem { Text = ""Github repo"", Url = ""https://github.com/bitfoundation/bitplatform"" },
        }
    },
    new BitNavItem { Text = ""Iconography"", Url = ""/icons"" },
};
";

    #endregion

    #region Sample Code 2

    private static string example2HTMLCode = @"
<BitNav Items=""CarNavMenu"" RenderType=""BitNavRenderType.Grouped"" />
";

    private static string example2CSharpCode = @"
private static readonly List<BitNavItem> CarNavMenu = new()
{
    new BitNavItem
    {
        Text = ""Mercedes-Benz"",
        ExpandAriaLabel = ""Mercedes-Benz Expanded"",
        CollapseAriaLabel = ""Mercedes-Benz Collapsed"",
        Title = ""Mercedes-Benz Car Models"",
        IsExpanded = true,
        Items = new List<BitNavItem>
        {
            new BitNavItem
            {
                Text = ""SUVs"",
                Items = new List<BitNavItem>
                {
                    new BitNavItem { Text = ""GLA"", Url = ""https://www.mbusa.com/en/vehicles/class/gla/suv"", Target = ""_blank"" },
                    new BitNavItem { Text = ""GLB"", Url = ""https://www.mbusa.com/en/vehicles/class/glb/suv"", Target = ""_blank"" },
                    new BitNavItem { Text = ""GLC"", Url = ""https://www.mbusa.com/en/vehicles/class/glc/suv"", Target = ""_blank"" },
                }
            },
            new BitNavItem
            {
                Text = ""Sedans & Wagons"",
                Items = new List<BitNavItem>
                {
                    new BitNavItem { Text = ""A Class"", Url = ""https://www.mbusa.com/en/vehicles/class/a-class/sedan"", Target = ""_blank"" },
                    new BitNavItem { Text = ""C Class"", Url = ""https://www.mbusa.com/en/vehicles/class/c-class/sedan"", Target = ""_blank"" },
                    new BitNavItem { Text = ""E Class"", Url = ""https://www.mbusa.com/en/vehicles/class/e-class/sedan"", Target = ""_blank"" },
                }
            },
            new BitNavItem
            {
                Text = ""Coupes"",
                Items = new List<BitNavItem>
                {
                    new BitNavItem { Text = ""CLA Coupe"", Url = ""https://www.mbusa.com/en/vehicles/class/cla/coupe"", Target = ""_blank"" },
                    new BitNavItem { Text = ""C Class Coupe"", Url = ""https://www.mbusa.com/en/vehicles/class/c-class/coupe"", Target = ""_blank"" },
                    new BitNavItem { Text = ""E Class Coupe"", Url = ""https://www.mbusa.com/en/vehicles/class/e-class/coupe"", Target = ""_blank"" },
                }
            },
        }
    },
    new BitNavItem
    {
        Text = ""Tesla"",
        ExpandAriaLabel = ""Tesla Expanded"",
        CollapseAriaLabel= ""Tesla Collapsed"",
        Title = ""Tesla Car Models"",
        Items = new List<BitNavItem>
        {
            new BitNavItem { Text = ""Model S"", Url = ""https://www.tesla.com/models"", Target = ""_blank"" },
            new BitNavItem { Text = ""Model X"", Url = ""https://www.tesla.com/modelx"", Target = ""_blank"" },
            new BitNavItem { Text = ""Model Y"", Url = ""https://www.tesla.com/modely"", Target = ""_blank"" },
        }
    },
};
";

    #endregion

    #region Sample Code 3

    private static string example3HTMLCode = @"
                <div>
                    <BitLabel>Basic</BitLabel>
                    <BitNav Items=""FoodNavMenu""
                            DefaultSelectedItem=""FoodNavMenu[0].Items[2]""
                            Mode=""BitNavMode.Manual"" />
                </div>

                <div class=""margin-top"">
                    <BitLabel>Two-Way Bind</BitLabel>

                    <BitNav @bind-SelectedItem=""SelectedItemNav""
                            Items=""FoodNavMenu""
                            Mode=""BitNavMode.Manual""
                            OnSelectItem=""(item) => SelectedItemText = FoodMenuDropDownItems.FirstOrDefault(i => i.Text == item.Text).Text"" />

                    <BitDropDown @bind-Value=""SelectedItemText""
                                 Label=""Select Item""
                                 Items=""FoodMenuDropDownItems""
                                 OnSelectItem=""(item) => SelectedItemNav = Flatten(FoodNavMenu).FirstOrDefault(i => i.Text == item.Value)"" />
                </div>
";

    private static string example3CSharpCode = @"
private static readonly List<BitNavItem> FoodNavMenu = new()
{
    new BitNavItem
    {
        Text = ""Fast-Food"",
        IconName = BitIconName.HeartBroken,
        IsExpanded = true,
        Items = new List<BitNavItem>
        {
            new BitNavItem
            {
                Text = ""Burgers"",
                Items = new List<BitNavItem>
                {
                    new BitNavItem { Text = ""Beef Burger"" },
                    new BitNavItem { Text = ""Veggie Burger"" },
                    new BitNavItem { Text = ""Bison Burger"" },
                    new BitNavItem { Text = ""Wild Salmon Burger"" },
                }
            },
            new BitNavItem
            {
                Text = ""Pizzas"",
                Items = new List<BitNavItem>
                {
                    new BitNavItem { Text = ""Cheese Pizza"" },
                    new BitNavItem { Text = ""Veggie Pizza"" },
                    new BitNavItem { Text = ""Pepperoni Pizza"" },
                    new BitNavItem { Text = ""Meat Pizza"" },
                }
            },
            new BitNavItem { Text    = ""French Fries"" },
        }
    },
    new BitNavItem
    {
        Text = ""Fruits"",
        IconName = BitIconName.Health,
        Items = new List<BitNavItem>
        {
            new BitNavItem { Text = ""Aplle"" },
            new BitNavItem { Text = ""Orange"" },
            new BitNavItem { Text = ""Benana"" },
        }
    },
    new BitNavItem { Text = ""Ice Cream"" },
    new BitNavItem { Text = ""Cookie"" },
};

private static readonly List<BitDropDownItem> FoodMenuDropDownItems = new()
{
    new BitDropDownItem
    {
        Text = ""Beef Burger"",
        Value = ""Beef Burger"",
    },
    new BitDropDownItem
    {
        Text = ""Veggie Burger"",
        Value = ""Veggie Burger"",
    },
    new BitDropDownItem
    {
        Text = ""Bison Burger"",
        Value = ""Bison Burger"",
    },
    new BitDropDownItem
    {
        Text = ""Wild Salmon Burger"",
        Value = ""Wild Salmon Burger"",
    },
    new BitDropDownItem
    {
        Text = ""Cheese Pizza"",
        Value = ""Cheese Pizza"",
    },
    new BitDropDownItem
    {
        Text = ""Veggie Pizza"",
        Value = ""Veggie Pizza"",
    },
    new BitDropDownItem
    {
        Text = ""Pepperoni Pizza"",
        Value = ""Pepperoni Pizza"",
    },
    new BitDropDownItem
    {
        Text = ""Meat Pizza"",
        Value = ""Meat Pizza"",
    },
    new BitDropDownItem
    {
        Text = ""French Fries"",
        Value = ""French Fries"",
    },
    new BitDropDownItem
    {
        Text = ""Aplle"",
        Value = ""Aplle"",
    },
    new BitDropDownItem
    {
        Text = ""Orange"",
        Value = ""Orange"",
    },
    new BitDropDownItem
    {
        Text = ""Benana"",
        Value = ""Benana"",
    },
    new BitDropDownItem
    {
        Text = ""Ice Cream"",
        Value = ""Ice Cream"",
    },
    new BitDropDownItem
    {
        Text = ""Cookie"",
        Value = ""Cookie"",
    },
};

private static List<BitNavItem> Flatten(IList<BitNavItem> e) => e.SelectMany(c => Flatten(c.Items)).Concat(e).ToList();
private BitNavItem SelectedItemNav = FoodNavMenu[0].Items[2];
private string SelectedItemText = FoodNavMenu[0].Items[2].Text;
";

    #endregion

    #region Sample Code 4

    private static string example4HTMLCode = @"
<style>
    .nav-custom-header {
        font-size: 17px;
        font-weight: 600;
        color: green;
    }

    .nav-custom-item {
        display: flex;
        align-items: center;
        flex-flow: row nowrap;
        gap: 4px;
        color: #ff7800;
        font-weight: 600;
    }
</style>

<div>
    <BitLabel>Header Template (in Grouped mode)</BitLabel>
    <BitNav Items=""CarNavMenu"" RenderType=""BitNavRenderType.Grouped"">
        <HeaderTemplate Context=""item"">
            <div class=""nav-custom-header"">
                <BitIcon IconName=""BitIconName.FavoriteStarFill"" />
                <span>@item.Text</span>
            </div>
        </HeaderTemplate>
    </BitNav>
</div>

<div class=""margin-top"">
    <BitLabel>Item Template</BitLabel>
    <BitNav Items=""FoodNavMenu"" Mode=""BitNavMode.Manual"">
        <ItemTemplate Context=""item"">
            <div class=""nav-custom-item @(item.IsEnabled is false ? ""disabled-item"" : """")"">
                <BitCheckbox IsEnabled=""@(item.IsEnabled)"" />
                @if (item.IconName.HasValue)
                {
                    <BitIcon IconName=""@item.IconName.Value"" />
                }
                <span>@item.Text</span>
            </div>
        </ItemTemplate>
    </BitNav>
</div>
";

    private static string example4CSharpCode = @"
private static readonly List<BitNavItem> CarNavMenu = new()
{
    new BitNavItem
    {
        Text = ""Mercedes-Benz"",
        ExpandAriaLabel = ""Mercedes-Benz Expanded"",
        CollapseAriaLabel = ""Mercedes-Benz Collapsed"",
        Title = ""Mercedes-Benz Car Models"",
        IsExpanded = true,
        Items = new List<BitNavItem>
        {
            new BitNavItem
            {
                Text = ""SUVs"",
                Items = new List<BitNavItem>
                {
                    new BitNavItem { Text = ""GLA"", Url = ""https://www.mbusa.com/en/vehicles/class/gla/suv"", Target = ""_blank"" },
                    new BitNavItem { Text = ""GLB"", Url = ""https://www.mbusa.com/en/vehicles/class/glb/suv"", Target = ""_blank"" },
                    new BitNavItem { Text = ""GLC"", Url = ""https://www.mbusa.com/en/vehicles/class/glc/suv"", Target = ""_blank"" },
                }
            },
            new BitNavItem
            {
                Text = ""Sedans & Wagons"",
                Items = new List<BitNavItem>
                {
                    new BitNavItem { Text = ""A Class"", Url = ""https://www.mbusa.com/en/vehicles/class/a-class/sedan"", Target = ""_blank"" },
                    new BitNavItem { Text = ""C Class"", Url = ""https://www.mbusa.com/en/vehicles/class/c-class/sedan"", Target = ""_blank"" },
                    new BitNavItem { Text = ""E Class"", Url = ""https://www.mbusa.com/en/vehicles/class/e-class/sedan"", Target = ""_blank"" },
                }
            },
            new BitNavItem
            {
                Text = ""Coupes"",
                Items = new List<BitNavItem>
                {
                    new BitNavItem { Text = ""CLA Coupe"", Url = ""https://www.mbusa.com/en/vehicles/class/cla/coupe"", Target = ""_blank"" },
                    new BitNavItem { Text = ""C Class Coupe"", Url = ""https://www.mbusa.com/en/vehicles/class/c-class/coupe"", Target = ""_blank"" },
                    new BitNavItem { Text = ""E Class Coupe"", Url = ""https://www.mbusa.com/en/vehicles/class/e-class/coupe"", Target = ""_blank"" },
                }
            },
        }
    },
    new BitNavItem
    {
        Text = ""Tesla"",
        ExpandAriaLabel = ""Tesla Expanded"",
        CollapseAriaLabel= ""Tesla Collapsed"",
        Title = ""Tesla Car Models"",
        Items = new List<BitNavItem>
        {
            new BitNavItem { Text = ""Model S"", Url = ""https://www.tesla.com/models"", Target = ""_blank"" },
            new BitNavItem { Text = ""Model X"", Url = ""https://www.tesla.com/modelx"", Target = ""_blank"" },
            new BitNavItem { Text = ""Model Y"", Url = ""https://www.tesla.com/modely"", Target = ""_blank"" },
        }
    },
};

private static readonly List<BitNavItem> FoodNavMenu = new()
{
    new BitNavItem
    {
        Text = ""Fast-Food"",
        IconName = BitIconName.HeartBroken,
        IsExpanded = true,
        Items = new List<BitNavItem>
        {
            new BitNavItem
            {
                Text = ""Burgers"",
                Items = new List<BitNavItem>
                {
                    new BitNavItem { Text = ""Beef Burger"" },
                    new BitNavItem { Text = ""Veggie Burger"" },
                    new BitNavItem { Text = ""Bison Burger"" },
                    new BitNavItem { Text = ""Wild Salmon Burger"" },
                }
            },
            new BitNavItem
            {
                Text = ""Pizzas"",
                Items = new List<BitNavItem>
                {
                    new BitNavItem { Text = ""Cheese Pizza"" },
                    new BitNavItem { Text = ""Veggie Pizza"" },
                    new BitNavItem { Text = ""Pepperoni Pizza"" },
                    new BitNavItem { Text = ""Meat Pizza"" },
                }
            },
            new BitNavItem { Text    = ""French Fries"" },
        }
    },
    new BitNavItem
    {
        Text = ""Fruits"",
        IconName = BitIconName.Health,
        Items = new List<BitNavItem>
        {
            new BitNavItem { Text = ""Aplle"" },
            new BitNavItem { Text = ""Orange"" },
            new BitNavItem { Text = ""Benana"" },
        }
    },
    new BitNavItem { Text = ""Ice Cream"" },
    new BitNavItem { Text = ""Cookie"" },
};
";

    #endregion

    #region Sample Code 5

    private static string example5HTMLCode = @"
<BitNav Items=""FoodNavMenu""
        DefaultSelectedItem=""FoodNavMenu[0].Items[2]""
        Mode=""BitNavMode.Manual""
        OnItemClick=""(item) => ClickedItem = item""
        OnSelectItem=""(item) => SelectedItem = item""
        OnItemToggle=""(item) => ToggledItem = item"" />

<div class=""flex"">
    <span>Clicked Item: @ClickedItem?.Text</span>
    <span>Selected Item: @SelectedItem?.Text</span>
    <span>Toggled Item: @(ToggledItem is null ? ""N/A"" : $""{ToggledItem.Text} ({(ToggledItem.IsExpanded ? ""Expanded"" : ""Collapsed"")})"")</span>
</div>
";

    private static string example5CSharpCode = @"
private static readonly List<BitNavItem> FoodNavMenu = new()
{
    new BitNavItem
    {
        Text = ""Fast-Food"",
        IconName = BitIconName.HeartBroken,
        IsExpanded = true,
        Items = new List<BitNavItem>
        {
            new BitNavItem
            {
                Text = ""Burgers"",
                Items = new List<BitNavItem>
                {
                    new BitNavItem { Text = ""Beef Burger"" },
                    new BitNavItem { Text = ""Veggie Burger"" },
                    new BitNavItem { Text = ""Bison Burger"" },
                    new BitNavItem { Text = ""Wild Salmon Burger"" },
                }
            },
            new BitNavItem
            {
                Text = ""Pizzas"",
                Items = new List<BitNavItem>
                {
                    new BitNavItem { Text = ""Cheese Pizza"" },
                    new BitNavItem { Text = ""Veggie Pizza"" },
                    new BitNavItem { Text = ""Pepperoni Pizza"" },
                    new BitNavItem { Text = ""Meat Pizza"" },
                }
            },
            new BitNavItem { Text    = ""French Fries"" },
        }
    },
    new BitNavItem
    {
        Text = ""Fruits"",
        IconName = BitIconName.Health,
        Items = new List<BitNavItem>
        {
            new BitNavItem { Text = ""Aplle"" },
            new BitNavItem { Text = ""Orange"" },
            new BitNavItem { Text = ""Benana"" },
        }
    },
    new BitNavItem { Text = ""Ice Cream"" },
    new BitNavItem { Text = ""Cookie"" },
};

private BitNavItem ClickedItem;
private BitNavItem SelectedItem;
private BitNavItem ToggledItem;
";

    #endregion
}

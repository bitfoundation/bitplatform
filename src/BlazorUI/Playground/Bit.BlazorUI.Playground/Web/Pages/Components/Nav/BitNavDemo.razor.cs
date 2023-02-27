using System.Collections.Generic;
using System.Linq;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Nav;

public partial class BitNavDemo
{
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


    #region Basic

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

    private static List<BitNavItem> Flatten(IList<BitNavItem> e) => e.SelectMany(c => Flatten(c.Items)).Concat(e).ToList();
    private BitNavItem SelectedItemNav = FoodNavMenu[0].Items[2];
    private string SelectedItemText = FoodNavMenu[0].Items[2].Text;

    private BitNavItem ClickedItem;
    private BitNavItem SelectedItem;
    private BitNavItem ToggledItem;

    #endregion

    #region List

    // Basic
    private static readonly List<BitPlatformMenu> CustomBitPlatformNavMenu = new()
    {
        new BitPlatformMenu
        {
            Text = "Bit Platform",
            Links = new List<BitPlatformMenu>
            {
                new BitPlatformMenu { Text = "Home", Url = "https://bitplatform.dev/" },
                new BitPlatformMenu
                {
                    Text = "Products & Services",
                    Links = new List<BitPlatformMenu>
                    {
                        new BitPlatformMenu
                        {
                            Text = "Project Templates",
                            Links = new List<BitPlatformMenu>
                            {
                                new BitPlatformMenu { Text = "TodoTemplate", Url = "https://bitplatform.dev/todo-template/overview" },
                                new BitPlatformMenu { Text = "AdminPanel", Url = "https://bitplatform.dev/admin-panel/overview" },
                            }
                        },
                        new BitPlatformMenu { Text = "BlazorUI", Url = "https://bitplatform.dev/components" },
                        new BitPlatformMenu { Text = "Cloud hosting solutions", Url = "https://bitplatform.dev/#", IsEnabled = false },
                        new BitPlatformMenu { Text = "Bit academy", Url = "https://bitplatform.dev/#", IsEnabled = false },
                    }
                },
                new BitPlatformMenu { Text = "Pricing", Url = "https://bitplatform.dev/pricing" },
                new BitPlatformMenu { Text = "About", Url = "https://bitplatform.dev/about-us" },
                new BitPlatformMenu { Text = "Contact us", Url = "https://bitplatform.dev/contact-us" },
            },
        },
        new BitPlatformMenu
        {
            Text = "Community",
            Links = new List<BitPlatformMenu>
            {
                new BitPlatformMenu { Text = "Linkedin", Url = "https://www.linkedin.com/company/bitplatformhq/about/" },
                new BitPlatformMenu { Text = "Twitter", Url = "https://twitter.com/bitplatformhq" },
                new BitPlatformMenu { Text = "Github repo", Url = "https://github.com/bitfoundation/bitplatform" },
            }
        },
        new BitPlatformMenu { Text = "Iconography", Url = "/icons" },
    };

    // Grouped
    private static readonly List<CarMenu> CustomCarNavMenu = new()
    {
        new CarMenu
        {
            Name = "Mercedes-Benz",
            ExpandedAriaLabel = "Mercedes-Benz Expanded",
            CollapsedAriaLabel = "Mercedes-Benz Collapsed",
            Tooltip = "Mercedes-Benz Car Models",
            IsExpandedParent = true,
            Links = new List<CarMenu>
            {
                new CarMenu
                {
                    Name = "SUVs",
                    Links = new List<CarMenu>
                    {
                        new CarMenu { Name = "GLA", PageUrl = "https://www.mbusa.com/en/vehicles/class/gla/suv", UrlTarget = "_blank" },
                        new CarMenu { Name = "GLB", PageUrl = "https://www.mbusa.com/en/vehicles/class/glb/suv", UrlTarget = "_blank" },
                        new CarMenu { Name = "GLC", PageUrl = "https://www.mbusa.com/en/vehicles/class/glc/suv", UrlTarget = "_blank" },
                    }
                },
                new CarMenu
                {
                    Name = "Sedans & Wagons",
                    Links = new List<CarMenu>
                    {
                        new CarMenu { Name = "A Class", PageUrl = "https://www.mbusa.com/en/vehicles/class/a-class/sedan", UrlTarget = "_blank" },
                        new CarMenu { Name = "C Class", PageUrl = "https://www.mbusa.com/en/vehicles/class/c-class/sedan", UrlTarget = "_blank" },
                        new CarMenu { Name = "E Class", PageUrl = "https://www.mbusa.com/en/vehicles/class/e-class/sedan", UrlTarget = "_blank" },
                    }
                },
                new CarMenu
                {
                    Name = "Coupes",
                    Links = new List<CarMenu>
                    {
                        new CarMenu { Name = "CLA Coupe", PageUrl = "https://www.mbusa.com/en/vehicles/class/cla/coupe", UrlTarget = "_blank" },
                        new CarMenu { Name = "C Class Coupe", PageUrl = "https://www.mbusa.com/en/vehicles/class/c-class/coupe", UrlTarget = "_blank" },
                        new CarMenu { Name = "E Class Coupe", PageUrl = "https://www.mbusa.com/en/vehicles/class/e-class/coupe", UrlTarget = "_blank" },
                    }
                },
            }
        },
        new CarMenu
        {
            Name = "Tesla",
            ExpandedAriaLabel = "Tesla Expanded",
            CollapsedAriaLabel = "Tesla Collapsed",
            Tooltip = "Tesla Car Models",
            Links = new List<CarMenu>
            {
                new CarMenu { Name = "Model S", PageUrl = "https://www.tesla.com/models", UrlTarget = "_blank" },
                new CarMenu { Name = "Model X", PageUrl = "https://www.tesla.com/modelx", UrlTarget = "_blank" },
                new CarMenu { Name = "Model Y", PageUrl = "https://www.tesla.com/modely", UrlTarget = "_blank" },
            }
        },
    };

    // Manual
    private static readonly List<FoodMenu> CustomFoodNavMenu = new()
    {
        new FoodMenu
        {
            Name = "Fast-Food",
            Icon = BitIconName.HeartBroken,
            IsExpanded = true,
            Childs = new List<FoodMenu>
            {
                new FoodMenu
                {
                    Name = "Burgers",
                    Childs = new List<FoodMenu>
                    {
                        new FoodMenu { Name = "Beef Burger" },
                        new FoodMenu { Name = "Veggie Burger" },
                        new FoodMenu { Name = "Bison Burger" },
                        new FoodMenu { Name = "Wild Salmon Burger" },
                    }
                },
                new FoodMenu
                {
                    Name = "Pizzas",
                    Childs = new List<FoodMenu>
                    {
                        new FoodMenu { Name = "Cheese Pizza" },
                        new FoodMenu { Name = "Veggie Pizza" },
                        new FoodMenu { Name = "Pepperoni Pizza" },
                        new FoodMenu { Name = "Meat Pizza" },
                    }
                },
                new FoodMenu { Name = "French Fries" },
            }
        },
        new FoodMenu
        {
            Name = "Fruits",
            Icon = BitIconName.Health,
            Childs = new List<FoodMenu>
            {
                new FoodMenu { Name = "Aplle" },
                new FoodMenu { Name = "Orange" },
                new FoodMenu { Name = "Benana" },
            }
        },
        new FoodMenu { Name = "Ice Cream" },
        new FoodMenu { Name = "Cookie" },
    };

    private static List<FoodMenu> Flatten(IList<FoodMenu> e) => e.SelectMany(c => Flatten(c.Childs)).Concat(e).ToList();
    private FoodMenu CustomSelectedFood = CustomFoodNavMenu[0].Childs[2];
    private string CustomSelectedFoodName = CustomFoodNavMenu[0].Childs[2].Name;

    private FoodMenu CustomClickedItem;
    private FoodMenu CustomSelectedItem;
    private FoodMenu CustomToggledItem;

    #endregion

    #region Option

    private string SelectedOptionKey;

    private BitNavOption ClickedOption;
    private BitNavOption SelectedOption;
    private BitNavOption ToggledOption;

    #endregion

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

    private static string example1NavItemHTMLCode = @"
<BitNav Items=""BitPlatformNavMenu"" />
";
    private static string example1NavItemCSharpCode = @"
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

    private static string example2NavItemHTMLCode = @"
<BitNav Items=""CarNavMenu"" RenderType=""BitNavRenderType.Grouped"" />
";
    private static string example2NavItemCSharpCode = @"
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

    private static string example3NavItemHTMLCode = @"
<div class=""example-box"">
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
                OnSelectItem=""(BitNavItem item) => SelectedItemText = FoodMenuDropDownItems.FirstOrDefault(i => i.Text == item.Text).Text"" />

        <BitDropDown @bind-Value=""SelectedItemText""
                        Label=""Select Item""
                        Items=""FoodMenuDropDownItems""
                        OnSelectItem=""(item) => SelectedItemNav = Flatten(FoodNavMenu).FirstOrDefault(i => i.Text == item.Value)"" />
    </div>
";
    private static string example3NavItemCSharpCode = @"
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

    private static string example4NavItemHTMLCode = @"
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
    private static string example4NavItemCSharpCode = @"
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

    private static string example5NavItemHTMLCode = @"
<BitNav Items=""FoodNavMenu""
        DefaultSelectedItem=""FoodNavMenu[0].Items[2]""
        Mode=""BitNavMode.Manual""
        OnItemClick=""(BitNavItem item) => ClickedItem = item""
        OnSelectItem=""(BitNavItem item) => SelectedItem = item""
        OnItemToggle=""(BitNavItem item) => ToggledItem = item"" />

<div class=""flex"">
    <span>Clicked Item: @ClickedItem?.Text</span>
    <span>Selected Item: @SelectedItem?.Text</span>
    <span>Toggled Item: @(ToggledItem is null ? ""N/A"" : $""{ToggledItem.Text} ({(ToggledItem.IsExpanded ? ""Expanded"" : ""Collapsed"")})"")</span>
</div>
";
    private static string example5NavItemCSharpCode = @"
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

    private static string example1CustomItemHTMLCode = @"
<BitNav Items=""CustomBitPlatformNavMenu""
        TextField=""@nameof(BitPlatformMenu.Text)""
        UrlField=""@nameof(BitPlatformMenu.Url)""
        IsEnabledField=""@nameof(BitPlatformMenu.IsEnabled)""
        ItemsField=""@nameof(BitPlatformMenu.Links)"" />
";
    private static string example1CustomItemCSharpCode = @"
private static readonly List<BitPlatformMenu> CustomBitPlatformNavMenu = new()
{
    new BitPlatformMenu
    {
        Text = ""Bit Platform"",
        Links = new List<BitPlatformMenu>
        {
            new BitPlatformMenu { Text = ""Home"", Url = ""https://bitplatform.dev/"" },
            new BitPlatformMenu
            {
                Text = ""Products & Services"",
                Links = new List<BitPlatformMenu>
                {
                    new BitPlatformMenu
                    {
                        Text = ""Project Templates"",
                        Links = new List<BitPlatformMenu>
                        {
                            new BitPlatformMenu { Text = ""TodoTemplate"", Url = ""https://bitplatform.dev/todo-template/overview"" },
                            new BitPlatformMenu { Text = ""AdminPanel"", Url = ""https://bitplatform.dev/admin-panel/overview"" },
                        }
                    },
                    new BitPlatformMenu { Text = ""BlazorUI"", Url = ""https://bitplatform.dev/components"" },
                    new BitPlatformMenu { Text = ""Cloud hosting solutions"", Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                    new BitPlatformMenu { Text = ""Bit academy"", Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                }
            },
            new BitPlatformMenu { Text = ""Pricing"", Url = ""https://bitplatform.dev/pricing"" },
            new BitPlatformMenu { Text = ""About"", Url = ""https://bitplatform.dev/about-us"" },
            new BitPlatformMenu { Text = ""Contact us"", Url = ""https://bitplatform.dev/contact-us"" },
        },
    },
    new BitPlatformMenu
    {
        Text = ""Community"",
        Links = new List<BitPlatformMenu>
        {
            new BitPlatformMenu { Text = ""Linkedin"", Url = ""https://www.linkedin.com/company/bitplatformhq/about/"" },
            new BitPlatformMenu { Text = ""Twitter"", Url = ""https://twitter.com/bitplatformhq"" },
            new BitPlatformMenu { Text = ""Github repo"", Url = ""https://github.com/bitfoundation/bitplatform"" },
        }
    },
    new BitPlatformMenu { Text = ""Iconography"", Url = ""/icons"" },
};
";

    private static string example2CustomItemHTMLCode = @"
<BitNav Items=""CustomCarNavMenu""
        TextField=""@nameof(CarMenu.Name)""
        UrlField=""@nameof(CarMenu.PageUrl)""
        TargetField=""@nameof(CarMenu.UrlTarget)""
        TitleField=""@nameof(CarMenu.Tooltip)""
        IsExpandedField=""@nameof(CarMenu.IsExpandedParent)""
        CollapseAriaLabelField=""@nameof(CarMenu.CollapsedAriaLabel)""
        ExpandAriaLabelField=""@nameof(CarMenu.ExpandedAriaLabel)""
        ItemsField=""@nameof(CarMenu.Links)""
        RenderType=""BitNavRenderType.Grouped"" />
";
    private static string example2CustomItemCSharpCode = @"
private static readonly List<CarMenu> CustomCarNavMenu = new()
{
    new CarMenu
    {
        Name = ""Mercedes-Benz"",
        ExpandedAriaLabel = ""Mercedes-Benz Expanded"",
        CollapsedAriaLabel = ""Mercedes-Benz Collapsed"",
        Tooltip = ""Mercedes-Benz Car Models"",
        IsExpandedParent = true,
        Links = new List<CarMenu>
        {
            new CarMenu
            {
                Name = ""SUVs"",
                Links = new List<CarMenu>
                {
                    new CarMenu { Name = ""GLA"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/gla/suv"", UrlTarget = ""_blank"" },
                    new CarMenu { Name = ""GLB"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/glb/suv"", UrlTarget = ""_blank"" },
                    new CarMenu { Name = ""GLC"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/glc/suv"", UrlTarget = ""_blank"" },
                }
            },
            new CarMenu
            {
                Name = ""Sedans & Wagons"",
                Links = new List<CarMenu>
                {
                    new CarMenu { Name = ""A Class"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/a-class/sedan"", UrlTarget = ""_blank"" },
                    new CarMenu { Name = ""C Class"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/c-class/sedan"", UrlTarget = ""_blank"" },
                    new CarMenu { Name = ""E Class"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/e-class/sedan"", UrlTarget = ""_blank"" },
                }
            },
            new CarMenu
            {
                Name = ""Coupes"",
                Links = new List<CarMenu>
                {
                    new CarMenu { Name = ""CLA Coupe"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/cla/coupe"", UrlTarget = ""_blank"" },
                    new CarMenu { Name = ""C Class Coupe"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/c-class/coupe"", UrlTarget = ""_blank"" },
                    new CarMenu { Name = ""E Class Coupe"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/e-class/coupe"", UrlTarget = ""_blank"" },
                }
            },
        }
    },
    new CarMenu
    {
        Name = ""Tesla"",
        ExpandedAriaLabel = ""Tesla Expanded"",
        CollapsedAriaLabel = ""Tesla Collapsed"",
        Tooltip = ""Tesla Car Models"",
        Links = new List<CarMenu>
        {
            new CarMenu { Name = ""Model S"", PageUrl = ""https://www.tesla.com/models"", UrlTarget = ""_blank"" },
            new CarMenu { Name = ""Model X"", PageUrl = ""https://www.tesla.com/modelx"", UrlTarget = ""_blank"" },
            new CarMenu { Name = ""Model Y"", PageUrl = ""https://www.tesla.com/modely"", UrlTarget = ""_blank"" },
        }
    },
};
";

    private static string example3CustomItemHTMLCode = @"
<div>
    <BitLabel>Basic</BitLabel>
    <BitNav Items=""CustomFoodNavMenu""
            TextFieldSelector=""item => item.Name""
            IconNameFieldSelector=""item => item.Icon""
            IsExpandedFieldSelector=""item => item.IsExpanded""
            ItemsFieldSelector=""item => item.Childs""
            DefaultSelectedItem=""CustomFoodNavMenu[0].Childs[2]""
            Mode=""BitNavMode.Manual"" />
</div>

<div class=""margin-top"">
    <BitLabel>Two-Way Bind</BitLabel>

    <BitNav @bind-SelectedItem=""CustomSelectedFood""
            Items=""CustomFoodNavMenu""
            TextFieldSelector=""item => item.Name""
            IconNameFieldSelector=""item => item.Icon""
            IsExpandedFieldSelector=""item => item.IsExpanded""
            ItemsFieldSelector=""item => item.Childs""
            Mode=""BitNavMode.Manual""
            OnSelectItem=""(FoodMenu item) => CustomSelectedFoodName = FoodMenuDropDownItems.FirstOrDefault(i => i.Text == item.Name).Text"" />

    <BitDropDown @bind-Value=""CustomSelectedFoodName""
                    Label=""Select Item""
                    Items=""FoodMenuDropDownItems""
                    OnSelectItem=""(item) => CustomSelectedFood = Flatten(CustomFoodNavMenu).FirstOrDefault(i => i.Name == item.Value)"" />
</div>
";
    private static string example3CustomItemCSharpCode = @"
private static readonly List<FoodMenu> CustomFoodNavMenu = new()
{
    new FoodMenu
    {
        Name = ""Fast-Food"",
        Icon = BitIconName.HeartBroken,
        IsExpanded = true,
        Childs = new List<FoodMenu>
        {
            new FoodMenu
            {
                Name = ""Burgers"",
                Childs = new List<FoodMenu>
                {
                    new FoodMenu { Name = ""Beef Burger"" },
                    new FoodMenu { Name = ""Veggie Burger"" },
                    new FoodMenu { Name = ""Bison Burger"" },
                    new FoodMenu { Name = ""Wild Salmon Burger"" },
                }
            },
            new FoodMenu
            {
                Name = ""Pizzas"",
                Childs = new List<FoodMenu>
                {
                    new FoodMenu { Name = ""Cheese Pizza"" },
                    new FoodMenu { Name = ""Veggie Pizza"" },
                    new FoodMenu { Name = ""Pepperoni Pizza"" },
                    new FoodMenu { Name = ""Meat Pizza"" },
                }
            },
            new FoodMenu { Name = ""French Fries"" },
        }
    },
    new FoodMenu
    {
        Name = ""Fruits"",
        Icon = BitIconName.Health,
        Childs = new List<FoodMenu>
        {
            new FoodMenu { Name = ""Aplle"" },
            new FoodMenu { Name = ""Orange"" },
            new FoodMenu { Name = ""Benana"" },
        }
    },
    new FoodMenu { Name = ""Ice Cream"" },
    new FoodMenu { Name = ""Cookie"" },
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

private static List<FoodMenu> Flatten(IList<FoodMenu> e) => e.SelectMany(c => Flatten(c.Childs)).Concat(e).ToList();
private FoodMenu CustomSelectedFood = CustomFoodNavMenu[0].Childs[2];
private string CustomSelectedFoodName = CustomFoodNavMenu[0].Childs[2].Name;
";

    private static string example4CustomItemHTMLCode = @"
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
    <BitNav Items=""CustomCarNavMenu""
            TextField=""@nameof(CarMenu.Name)""
            UrlField=""@nameof(CarMenu.PageUrl)""
            TargetField=""@nameof(CarMenu.UrlTarget)""
            TitleField=""@nameof(CarMenu.Tooltip)""
            IsExpandedField=""@nameof(CarMenu.IsExpandedParent)""
            CollapseAriaLabelField=""@nameof(CarMenu.CollapsedAriaLabel)""
            ExpandAriaLabelField=""@nameof(CarMenu.ExpandedAriaLabel)""
            ItemsField=""@nameof(CarMenu.Links)""
            RenderType=""BitNavRenderType.Grouped"">

        <HeaderTemplate Context=""item"">
            <div class=""nav-custom-header"">
                <BitIcon IconName=""BitIconName.FavoriteStarFill"" />
                <span>@item.Name</span>
            </div>
        </HeaderTemplate>
    </BitNav>
</div>

<div class=""margin-top"">
    <BitLabel>Item Template</BitLabel>
    <BitNav Items=""CustomFoodNavMenu""
            TextFieldSelector=""item => item.Name""
            IconNameFieldSelector=""item => item.Icon""
            IsExpandedFieldSelector=""item => item.IsExpanded""
            ItemsFieldSelector=""item => item.Childs""
            Mode=""BitNavMode.Manual"">

        <ItemTemplate Context=""item"">
            <div class=""nav-custom-item"">
                <BitCheckbox />
                <BitIcon IconName=""@item.Icon"" />
                <span>@item.Name</span>
            </div>
        </ItemTemplate>
    </BitNav>
</div>
";
    private static string example4CustomItemCSharpCode = @"
private static readonly List<CarMenu> CustomCarNavMenu = new()
{
    new CarMenu
    {
        Name = ""Mercedes-Benz"",
        ExpandedAriaLabel = ""Mercedes-Benz Expanded"",
        CollapsedAriaLabel = ""Mercedes-Benz Collapsed"",
        Tooltip = ""Mercedes-Benz Car Models"",
        IsExpandedParent = true,
        Links = new List<CarMenu>
        {
            new CarMenu
            {
                Name = ""SUVs"",
                Links = new List<CarMenu>
                {
                    new CarMenu { Name = ""GLA"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/gla/suv"", UrlTarget = ""_blank"" },
                    new CarMenu { Name = ""GLB"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/glb/suv"", UrlTarget = ""_blank"" },
                    new CarMenu { Name = ""GLC"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/glc/suv"", UrlTarget = ""_blank"" },
                }
            },
            new CarMenu
            {
                Name = ""Sedans & Wagons"",
                Links = new List<CarMenu>
                {
                    new CarMenu { Name = ""A Class"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/a-class/sedan"", UrlTarget = ""_blank"" },
                    new CarMenu { Name = ""C Class"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/c-class/sedan"", UrlTarget = ""_blank"" },
                    new CarMenu { Name = ""E Class"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/e-class/sedan"", UrlTarget = ""_blank"" },
                }
            },
            new CarMenu
            {
                Name = ""Coupes"",
                Links = new List<CarMenu>
                {
                    new CarMenu { Name = ""CLA Coupe"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/cla/coupe"", UrlTarget = ""_blank"" },
                    new CarMenu { Name = ""C Class Coupe"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/c-class/coupe"", UrlTarget = ""_blank"" },
                    new CarMenu { Name = ""E Class Coupe"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/e-class/coupe"", UrlTarget = ""_blank"" },
                }
            },
        }
    },
    new CarMenu
    {
        Name = ""Tesla"",
        ExpandedAriaLabel = ""Tesla Expanded"",
        CollapsedAriaLabel = ""Tesla Collapsed"",
        Tooltip = ""Tesla Car Models"",
        Links = new List<CarMenu>
        {
            new CarMenu { Name = ""Model S"", PageUrl = ""https://www.tesla.com/models"", UrlTarget = ""_blank"" },
            new CarMenu { Name = ""Model X"", PageUrl = ""https://www.tesla.com/modelx"", UrlTarget = ""_blank"" },
            new CarMenu { Name = ""Model Y"", PageUrl = ""https://www.tesla.com/modely"", UrlTarget = ""_blank"" },
        }
    },
};

private static readonly List<FoodMenu> CustomFoodNavMenu = new()
{
    new FoodMenu
    {
        Name = ""Fast-Food"",
        Icon = BitIconName.HeartBroken,
        IsExpanded = true,
        Childs = new List<FoodMenu>
        {
            new FoodMenu
            {
                Name = ""Burgers"",
                Childs = new List<FoodMenu>
                {
                    new FoodMenu { Name = ""Beef Burger"" },
                    new FoodMenu { Name = ""Veggie Burger"" },
                    new FoodMenu { Name = ""Bison Burger"" },
                    new FoodMenu { Name = ""Wild Salmon Burger"" },
                }
            },
            new FoodMenu
            {
                Name = ""Pizzas"",
                Childs = new List<FoodMenu>
                {
                    new FoodMenu { Name = ""Cheese Pizza"" },
                    new FoodMenu { Name = ""Veggie Pizza"" },
                    new FoodMenu { Name = ""Pepperoni Pizza"" },
                    new FoodMenu { Name = ""Meat Pizza"" },
                }
            },
            new FoodMenu { Name = ""French Fries"" },
        }
    },
    new FoodMenu
    {
        Name = ""Fruits"",
        Icon = BitIconName.Health,
        Childs = new List<FoodMenu>
        {
            new FoodMenu { Name = ""Aplle"" },
            new FoodMenu { Name = ""Orange"" },
            new FoodMenu { Name = ""Benana"" },
        }
    },
    new FoodMenu { Name = ""Ice Cream"" },
    new FoodMenu { Name = ""Cookie"" },
};
";

    private static string example5CustomItemHTMLCode = @"
<BitNav Items=""CustomFoodNavMenu""
        TextFieldSelector=""item => item.Name""
        IconNameFieldSelector=""item => item.Icon""
        IsExpandedFieldSelector=""item => item.IsExpanded""
        ItemsFieldSelector=""item => item.Childs""
        DefaultSelectedItem=""CustomFoodNavMenu[0].Childs[2]""
        Mode=""BitNavMode.Manual""
        OnItemClick=""(FoodMenu item) => CustomClickedItem = item""
        OnSelectItem=""(FoodMenu item) => CustomSelectedItem = item""
        OnItemToggle=""(FoodMenu item) => CustomToggledItem = item"" />

<div class=""flex"">
    <span>Clicked Item: @CustomClickedItem?.Name</span>
    <span>Selected Item: @CustomSelectedItem?.Name</span>
    <span>Toggled Item: @(CustomToggledItem is null ? ""N/A"" : $""{CustomToggledItem.Name} ({(CustomToggledItem.IsExpanded ? ""Expanded"" : ""Collapsed"")})"")</span>
</div>
";
    private static string example5CustomItemCSharpCode = @"
private static readonly List<FoodMenu> CustomFoodNavMenu = new()
{
    new FoodMenu
    {
        Name = ""Fast-Food"",
        Icon = BitIconName.HeartBroken,
        IsExpanded = true,
        Childs = new List<FoodMenu>
        {
            new FoodMenu
            {
                Name = ""Burgers"",
                Childs = new List<FoodMenu>
                {
                    new FoodMenu { Name = ""Beef Burger"" },
                    new FoodMenu { Name = ""Veggie Burger"" },
                    new FoodMenu { Name = ""Bison Burger"" },
                    new FoodMenu { Name = ""Wild Salmon Burger"" },
                }
            },
            new FoodMenu
            {
                Name = ""Pizzas"",
                Childs = new List<FoodMenu>
                {
                    new FoodMenu { Name = ""Cheese Pizza"" },
                    new FoodMenu { Name = ""Veggie Pizza"" },
                    new FoodMenu { Name = ""Pepperoni Pizza"" },
                    new FoodMenu { Name = ""Meat Pizza"" },
                }
            },
            new FoodMenu { Name = ""French Fries"" },
        }
    },
    new FoodMenu
    {
        Name = ""Fruits"",
        Icon = BitIconName.Health,
        Childs = new List<FoodMenu>
        {
            new FoodMenu { Name = ""Aplle"" },
            new FoodMenu { Name = ""Orange"" },
            new FoodMenu { Name = ""Benana"" },
        }
    },
    new FoodMenu { Name = ""Ice Cream"" },
    new FoodMenu { Name = ""Cookie"" },
};

private FoodMenu CustomClickedItem;
private FoodMenu CustomSelectedItem;
private FoodMenu CustomToggledItem;
";

    private static string example1NavOptionHTMLCode = @"
<BitNav TItem=""BitNavOption"">
    <BitNavOption Text=""Bit Platform""
                    ExpandAriaLabel=""Bit Platform Expanded""
                    CollapseAriaLabel=""Bit Platform Collapsed"">
        <BitNavOption Text=""Home"" Url=""https://bitplatform.dev/"" Target=""_blank"" />
        <BitNavOption Text=""Products & Services"">
            <BitNavOption Text=""Project Templates"">
                <BitNavOption Text=""TodoTemplate"" Url=""https://bitplatform.dev/todo-template/overview"" Target=""_blank"" />
                <BitNavOption Text=""AdminPanel"" Url=""https://bitplatform.dev/admin-panel/overview"" Target=""_blank"" />
            </BitNavOption>
            <BitNavOption Text=""BlazorUI"" Url=""https://bitplatform.dev/components"" Target=""_blank"" />
            <BitNavOption Text=""Cloud hosting solutions"" IsEnabled=""false"" />
            <BitNavOption Text=""Bit academy"" IsEnabled=""false"" />
        </BitNavOption>
        <BitNavOption Text=""Pricing"" Url=""https://bitplatform.dev/pricing"" Target=""_blank"" />
        <BitNavOption Text=""About"" Url=""https://bitplatform.dev/about-us"" Target=""_blank"" />
        <BitNavOption Text=""Contact us"" Url=""https://bitplatform.dev/contact-us"" Target=""_blank"" />
    </BitNavOption>

    <BitNavOption Text=""Community""
                    ExpandAriaLabel=""Community Expanded""
                    CollapseAriaLabel=""Community Collapsed"">
        <BitNavOption Text=""Linkedin"" Url=""https://www.linkedin.com/company/bitplatformhq/about"" Target=""_blank"" />
        <BitNavOption Text=""Twitter"" Url=""https://twitter.com/bitplatformhq"" Target=""_blank"" />
        <BitNavOption Text=""Github repo"" Url=""https://github.com/bitfoundation/bitplatform"" Target=""_blank"" />
    </BitNavOption>

    <BitNavOption Text=""Iconography"" Url=""/icons"" Target=""_blank"" />
</BitNav>
";

    private static string example2NavOptionHTMLCode = @"
<BitNav TItem=""BitNavOption"" RenderType=""BitNavRenderType.Grouped"">
    <BitNavOption Text=""Mercedes-Benz""
                    ExpandAriaLabel=""Mercedes-Benz Expanded""
                    CollapseAriaLabel=""Mercedes-Benz Collapsed""
                    Title=""Mercedes-Benz Car Models""
                    IsExpanded=""true"">
        <BitNavOption Text=""SUVs"">
            <BitNavOption Text=""GLA"" Url=""https://www.mbusa.com/en/vehicles/class/gla/suv"" Target=""_blank"" />
            <BitNavOption Text=""GLB"" Url=""https://www.mbusa.com/en/vehicles/class/glb/suv"" Target=""_blank"" />
            <BitNavOption Text=""GLC"" Url=""https://www.mbusa.com/en/vehicles/class/glc/suv"" Target=""_blank"" />
        </BitNavOption>
        <BitNavOption Text=""Sedans & Wagons"">
            <BitNavOption Text=""A Class"" Url=""https://www.mbusa.com/en/vehicles/class/a-class/sedan"" Target=""_blank"" />
            <BitNavOption Text=""C Class"" Url=""https://www.mbusa.com/en/vehicles/class/c-class/sedan"" Target=""_blank"" />
            <BitNavOption Text=""E Class"" Url=""https://www.mbusa.com/en/vehicles/class/e-class/sedan"" Target=""_blank"" />
        </BitNavOption>
        <BitNavOption Text=""Coupes"">
            <BitNavOption Text=""CLA Coupe"" Url=""https://www.mbusa.com/en/vehicles/class/cla/coupe"" Target=""_blank"" />
            <BitNavOption Text=""C Class Coupe"" Url=""https://www.mbusa.com/en/vehicles/class/c-class/coupe"" Target=""_blank"" />
            <BitNavOption Text=""E Class Coupe"" Url=""https://www.mbusa.com/en/vehicles/class/e-class/coupe"" Target=""_blank"" />
        </BitNavOption>
    </BitNavOption>
    <BitNavOption Text=""Tesla""
                    ExpandAriaLabel=""Tesla Expanded""
                    CollapseAriaLabel=""Tesla Collapsed""
                    Title=""Tesla Car Models"">
        <BitNavOption Text=""Model S"" Url=""https://www.tesla.com/models"" Target=""_blank"" />
        <BitNavOption Text=""Model X"" Url=""https://www.tesla.com/modelx"" Target=""_blank"" />
        <BitNavOption Text=""Model Y"" Url=""https://www.tesla.com/modely"" Target=""_blank"" />
    </BitNavOption>
</BitNav>
";

    private static string example3NavOptionHTMLCode = @"
<div>
    <BitLabel>Basic</BitLabel>
    <BitNav TItem=""BitNavOption""
            Mode=""BitNavMode.Manual"">
        <BitNavOption Text=""Fast-Foods""
                        IconName=""BitIconName.HeartBroken"">
            <BitNavOption Text=""Burgers"">
                <BitNavOption Text=""Beef Burger"" Key=""Beef Burger"" />
                <BitNavOption Text=""Veggie Burger"" Key=""Veggie Burger"" />
                <BitNavOption Text=""Bison Burger"" Key=""Bison Burger"" />
                <BitNavOption Text=""Wild Salmon Burger"" Key=""Wild Salmon Burger"" />
            </BitNavOption>
            <BitNavOption Text=""Pizzas"">
                <BitNavOption Text=""Cheese Pizza"" Key=""Cheese Pizza"" />
                <BitNavOption Text=""Veggie Pizza"" Key=""Veggie Pizza"" />
                <BitNavOption Text=""Pepperoni Pizza"" Key=""Pepperoni Pizza"" />
                <BitNavOption Text=""Meat Pizza"" Key=""Meat Pizza"" />
            </BitNavOption>
            <BitNavOption Text=""French Fries"" Key=""French Fries"" />
        </BitNavOption>
        <BitNavOption Text=""Fruits"">
            <BitNavOption Text=""Aplle"" Key=""Aplle"" />
            <BitNavOption Text=""Orange"" Key=""Orange"" />
            <BitNavOption Text=""Benana"" Key=""Benana"" />
        </BitNavOption>
        <BitNavOption Text=""Ice Cream"" Key=""Ice Cream"" />
        <BitNavOption Text=""Cookie"" Key=""Cookie"" />
    </BitNav>
</div>

<div class=""margin-top"">
    <BitLabel>Two-Way Bind</BitLabel>
    <BitNav TItem=""BitNavOption""
            Mode=""BitNavMode.Manual"">
        <BitNavOption Text=""Fast-Foods""
                        IconName=""BitIconName.HeartBroken""
                        IsExpanded=""true"">
            <BitNavOption Text=""Burgers"">
                <BitNavOption Text=""Beef Burger"" Key=""Beef Burger"" />
                <BitNavOption Text=""Veggie Burger"" Key=""Veggie Burger"" />
                <BitNavOption Text=""Bison Burger"" Key=""Bison Burger"" />
                <BitNavOption Text=""Wild Salmon Burger"" Key=""Wild Salmon Burger"" />
            </BitNavOption>
            <BitNavOption Text=""Pizzas"">
                <BitNavOption Text=""Cheese Pizza"" Key=""Cheese Pizza"" />
                <BitNavOption Text=""Veggie Pizza"" Key=""Veggie Pizza"" />
                <BitNavOption Text=""Pepperoni Pizza"" Key=""Pepperoni Pizza"" />
                <BitNavOption Text=""Meat Pizza"" Key=""Meat Pizza"" />
            </BitNavOption>
            <BitNavOption Text=""French Fries"" Key=""French Fries"" />
        </BitNavOption>
        <BitNavOption Text=""Fruits"">
            <BitNavOption Text=""Aplle"" Key=""Aplle"" />
            <BitNavOption Text=""Orange"" Key=""Orange"" />
            <BitNavOption Text=""Benana"" Key=""Benana"" />
        </BitNavOption>
        <BitNavOption Text=""Ice Cream"" Key=""Ice Cream"" />
        <BitNavOption Text=""Cookie"" Key=""Cookie"" />
    </BitNav>

    <BitDropDown @bind-Value=""SelectedOptionKey""
                    DefaultValue=""French Fries""
                    Label=""Pick one""
                    Items=""FoodMenuDropDownItems"" />
</div>
";
    private static string example3NavOptionCSharpCode = @"
private string SelectedOptionKey;
";

    private static string example4NavOptionHTMLCode = @"
<div>
    <BitLabel>Header Template (in Grouped mode)</BitLabel>
    <BitNav TItem=""BitNavOption"" RenderType=""BitNavRenderType.Grouped"">
        <HeaderTemplate Context=""item"">
            <div class=""nav-custom-header"">
                <BitIcon IconName=""BitIconName.FavoriteStarFill"" />
                <span>@item.Text</span>
            </div>
        </HeaderTemplate>
        <ChildContent>
            <BitNavOption Text=""Mercedes-Benz""
                            ExpandAriaLabel=""Mercedes-Benz Expanded""
                            CollapseAriaLabel=""Mercedes-Benz Collapsed""
                            Title=""Mercedes-Benz Car Models""
                            IsExpanded=""true"">
                <BitNavOption Text=""SUVs"">
                    <BitNavOption Text=""GLA"" Url=""https://www.mbusa.com/en/vehicles/class/gla/suv"" Target=""_blank"" />
                    <BitNavOption Text=""GLB"" Url=""https://www.mbusa.com/en/vehicles/class/glb/suv"" Target=""_blank"" />
                    <BitNavOption Text=""GLC"" Url=""https://www.mbusa.com/en/vehicles/class/glc/suv"" Target=""_blank"" />
                </BitNavOption>
                <BitNavOption Text=""Sedans & Wagons"">
                    <BitNavOption Text=""A Class"" Url=""https://www.mbusa.com/en/vehicles/class/a-class/sedan"" Target=""_blank"" />
                    <BitNavOption Text=""C Class"" Url=""https://www.mbusa.com/en/vehicles/class/c-class/sedan"" Target=""_blank"" />
                    <BitNavOption Text=""E Class"" Url=""https://www.mbusa.com/en/vehicles/class/e-class/sedan"" Target=""_blank"" />
                </BitNavOption>
                <BitNavOption Text=""Coupes"">
                    <BitNavOption Text=""CLA Coupe"" Url=""https://www.mbusa.com/en/vehicles/class/cla/coupe"" Target=""_blank"" />
                    <BitNavOption Text=""C Class Coupe"" Url=""https://www.mbusa.com/en/vehicles/class/c-class/coupe"" Target=""_blank"" />
                    <BitNavOption Text=""E Class Coupe"" Url=""https://www.mbusa.com/en/vehicles/class/e-class/coupe"" Target=""_blank"" />
                </BitNavOption>
            </BitNavOption>
            <BitNavOption Text=""Tesla""
                            ExpandAriaLabel=""Tesla Expanded""
                            CollapseAriaLabel=""Tesla Collapsed""
                            Title=""Tesla Car Models"">
                <BitNavOption Text=""Model S"" Url=""https://www.tesla.com/models"" Target=""_blank"" />
                <BitNavOption Text=""Model X"" Url=""https://www.tesla.com/modelx"" Target=""_blank"" />
                <BitNavOption Text=""Model Y"" Url=""https://www.tesla.com/modely"" Target=""_blank"" />
            </BitNavOption>
        </ChildContent>
    </BitNav>
</div>

<div class=""margin-top"">
    <BitLabel>Option Template</BitLabel>
    <BitNav TItem=""BitNavOption"" Mode=""BitNavMode.Manual"">
        <ItemTemplate Context=""option"">
            <div class=""nav-custom-item"">
                <BitCheckbox IsEnabled=""@(option.IsEnabled)"" />
                <span>@option.Text</span>
            </div>
        </ItemTemplate>
        <ChildContent>
            <BitNavOption Text=""Fast-Foods""
                            IsExpanded=""true"">
                <BitNavOption Text=""Burgers"">
                    <BitNavOption Text=""Beef Burger"" />
                    <BitNavOption Text=""Veggie Burger"" />
                    <BitNavOption Text=""Bison Burger"" />
                    <BitNavOption Text=""Wild Salmon Burger"" />
                </BitNavOption>
                <BitNavOption Text=""Pizzas"">
                    <BitNavOption Text=""Cheese Pizza"" />
                    <BitNavOption Text=""Veggie Pizza"" />
                    <BitNavOption Text=""Pepperoni Pizza"" />
                    <BitNavOption Text=""Meat Pizza"" />
                </BitNavOption>
                <BitNavOption Text=""French Fries"" />
            </BitNavOption>
            <BitNavOption Text=""Fruits"">
                <BitNavOption Text=""Aplle"" />
                <BitNavOption Text=""Orange"" />
                <BitNavOption Text=""Benana"" />
            </BitNavOption>
            <BitNavOption Text=""Ice Cream"" />
            <BitNavOption Text=""Cookie"" />
        </ChildContent>
    </BitNav>
</div>
";

    private static string example5NavOptionHTMLCode = @"
<BitNav Mode=""BitNavMode.Manual""
        OnItemClick=""(BitNavOption option) => ClickedOption = option""
        OnSelectItem=""(BitNavOption option) => SelectedOption = option""
        OnItemToggle=""(BitNavOption option) => ToggledOption = option"">
    <BitNavOption Text=""Fast-Foods""
                    IconName=""BitIconName.HeartBroken""
                    IsExpanded=""true"">
        <BitNavOption Text=""Burgers"">
            <BitNavOption Text=""Beef Burger"" Key=""Beef Burger"" />
            <BitNavOption Text=""Veggie Burger"" Key=""Veggie Burger"" />
            <BitNavOption Text=""Bison Burger"" Key=""Bison Burger"" />
            <BitNavOption Text=""Wild Salmon Burger"" Key=""Wild Salmon Burger"" />
        </BitNavOption>
        <BitNavOption Text=""Pizzas"">
            <BitNavOption Text=""Cheese Pizza"" Key=""Cheese Pizza"" />
            <BitNavOption Text=""Veggie Pizza"" Key=""Veggie Pizza"" />
            <BitNavOption Text=""Pepperoni Pizza"" Key=""Pepperoni Pizza"" />
            <BitNavOption Text=""Meat Pizza"" Key=""Meat Pizza"" />
        </BitNavOption>
        <BitNavOption Text=""French Fries"" Key=""French Fries"" />
    </BitNavOption>
    <BitNavOption Text=""Fruits"">
        <BitNavOption Text=""Aplle"" Key=""Aplle"" />
        <BitNavOption Text=""Orange"" Key=""Orange"" />
        <BitNavOption Text=""Benana"" Key=""Benana"" />
    </BitNavOption>
    <BitNavOption Text=""Ice Cream"" Key=""Ice Cream"" />
    <BitNavOption Text=""Cookie"" Key=""Cookie"" />
</BitNav>

<div class=""flex"">
    <span>Clicked Option: @ClickedOption?.Text</span>
    <span>Selected Option: @SelectedOption?.Text</span>
    <span>Toggled Option: @(ToggledOption is null ? ""N/A"" : $""{ToggledOption.Text} ({(ToggledOption.IsExpanded ? ""Expanded"" : ""Collapsed"")})"")</span>
</div>
";
    private static string example5NavOptionCSharpCode = @"
private BitNavOption ClickedOption;
private BitNavOption SelectedOption;
private BitNavOption ToggledOption;
";
}

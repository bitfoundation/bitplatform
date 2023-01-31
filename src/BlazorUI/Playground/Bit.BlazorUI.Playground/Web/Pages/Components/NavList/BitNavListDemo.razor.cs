using System.Collections.Generic;
using System.Linq;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.NavList;

public partial class BitNavListDemo
{
    // Basic
    private static readonly List<BitPlatformMenu> BitPlatformNavMenu = new()
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
    private static readonly List<CarMenu> CarNavMenu = new()
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
    private static readonly List<FoodMenu> FoodNavMenu = new()
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
    private static List<FoodMenu> Flatten(IList<FoodMenu> e) => e.SelectMany(c => Flatten(c.Childs)).Concat(e).ToList();
    private FoodMenu SelectedFood = FoodNavMenu[0].Childs[2];
    private string SelectedFoodName = FoodNavMenu[0].Childs[2].Name;

    private FoodMenu ClickedItem;
    private FoodMenu SelectedItem;
    private FoodMenu ToggledItem;

    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter()
        {
            Name = "AriaCurrentField",
            Type = "string",
            DefaultValue = "AriaCurrent",
            Description = "Aria-current token for active nav item. Must be a valid token value, and defaults to 'page'."
        },
        new ComponentParameter()
        {
            Name = "AriaCurrentFieldSelector",
            Type = "Expression<Func<TItem, BitNavListItemAriaCurrent>>?",
            Href = "nav-item-aria-current",
            LinkType = LinkType.Link,
            Description = "Aria-current token for active nav item. Must be a valid token value, and defaults to 'page'."
        },
        new ComponentParameter()
        {
            Name = "AriaLabelField",
            Type = "string",
            DefaultValue = "AriaLabel",
            Description = "Aria label for the item. Ignored if collapseAriaLabel or expandAriaLabel is provided."
        },
        new ComponentParameter()
        {
            Name = "AriaLabelFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "Aria label for the item. Ignored if collapseAriaLabel or expandAriaLabel is provided."
        },
        new ComponentParameter()
        {
            Name = "DefaultSelectedItem",
            Type = "TItem?",
            Description = "The initially selected item in manual mode."
        },
        new ComponentParameter()
        {
            Name = "CollapseAriaLabelField",
            Type = "string",
            DefaultValue = "CollapseAriaLabel",
            Description = "Aria label when group is collapsed."
        },
        new ComponentParameter()
        {
            Name = "CollapseAriaLabelFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "Aria label when group is collapsed."
        },
        new ComponentParameter()
        {
            Name = "ExpandAriaLabelField",
            Type = "string",
            DefaultValue = "ExpandAriaLabel",
            Description = "Aria label when group is expanded."
        },
        new ComponentParameter()
        {
            Name = "ExpandAriaLabelFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "Aria label when group is expanded."
        },
        new ComponentParameter()
        {
            Name = "ForceAnchorField",
            Type = "string",
            DefaultValue = "ForceAnchor",
            Description = "(Optional) By default, any link with onClick defined will render as a button. Set this property to true to override that behavior. (Links without onClick defined will render as anchors by default.)"
        },
        new ComponentParameter()
        {
            Name = "ForceAnchorFieldelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "(Optional) By default, any link with onClick defined will render as a button. Set this property to true to override that behavior. (Links without onClick defined will render as anchors by default.)"
        },
        new ComponentParameter()
        {
            Name = "HeaderTemplate",
            Type = "RenderFragment<TItem>?",
            Description = "Used to customize how content inside the group header is rendered."
        },
        new ComponentParameter()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<TItem>?",
            Description = "Used to customize how content inside the item is rendered."
        },
        new ComponentParameter()
        {
            Name = "Items",
            Type = "IList<TItem>",
            DefaultValue = "new List<TItem>()",
            Description = "A collection of item to display in the navigation bar."
        },
        new ComponentParameter()
        {
            Name = "ItemsField",
            Type = "string",
            DefaultValue = "Items",
            Description = "A list of items to render as children of the current item."
        },
        new ComponentParameter()
        {
            Name = "ItemsFieldSelector",
            Type = "Expression<Func<TItem, IList<TItem>>>?",
            Description = "A list of items to render as children of the current item."
        },
        new ComponentParameter()
        {
            Name = "IconNameField",
            Type = "string",
            DefaultValue = "IconName",
            Description = "Name of an icon to render next to the item button."
        },
        new ComponentParameter()
        {
            Name = "IconNameFieldSelector",
            Type = "Expression<Func<TItem, BitIconName>>?",
            Description = "Name of an icon to render next to the item button."
        },
        new ComponentParameter()
        {
            Name = "IsExpandedField",
            Type = "string",
            DefaultValue = "IsExpanded",
            Description = "Whether or not the group is in an expanded state."
        },
        new ComponentParameter()
        {
            Name = "IsExpandedFieldSelector",
            Type = "Expression<Func<TItem, bool>>?",
            Description = "Whether or not the group is in an expanded state."
        },
        new ComponentParameter()
        {
            Name = "IsEnabledField",
            Type = "string",
            DefaultValue = "IsEnabled",
            Description = "Whether or not the item is disabled."
        },
        new ComponentParameter()
        {
            Name = "IsEnabledFieldSelector",
            Type = "Expression<Func<TItem, bool>>?",
            Description = "Whether or not the item is disabled."
        },
        new ComponentParameter()
        {
            Name = "Mode",
            Type = "BitNavMode",
            DefaultValue = "BitNavMode.Automatic",
            Href = "nav-mode-enum",
            LinkType = LinkType.Link,
            Description = "Determines how the navigation will be handled."
        },
        new ComponentParameter()
        {
            Name = "OnItemClick",
            Type = "EventCallback<TItem>",
            Description = "Callback invoked when an item is clicked."
        },
        new ComponentParameter()
        {
            Name = "OnSelectItem",
            Type = "EventCallback<TItem>",
            Description = "Callback invoked when an item is selected."
        },
        new ComponentParameter()
        {
            Name = "OnItemToggle",
            Type = "EventCallback<TItem>",
            Description = "Callback invoked when a group header is clicked and Expanded or Collapse."
        },
        new ComponentParameter()
        {
            Name = "RenderType",
            Type = "BitNavRenderType",
            DefaultValue = "BitNavRenderType.Normal",
            Href = "nav-render-type-enum",
            LinkType = LinkType.Link,
            Description = "The way to render nav items."
        },
        new ComponentParameter()
        {
            Name = "SelectedItem",
            Type = "TItem?",
            Description = "Selected item to show in Nav."
        },
        new ComponentParameter()
        {
            Name = "StyleField",
            Type = "string",
            DefaultValue = "Style",
            Description = "Custom style for the each item element."
        },
        new ComponentParameter()
        {
            Name = "StyleFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "Custom style for the each item element."
        },
        new ComponentParameter()
        {
            Name = "TextField",
            Type = "string",
            DefaultValue = "Name",
            Description = "Text to render for the item."
        },
        new ComponentParameter()
        {
            Name = "TextFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "Text to render for the item."
        },
        new ComponentParameter()
        {
            Name = "TitleField",
            Type = "string",
            DefaultValue= "Title",
            Description = "Text for the item tooltip."
        },
        new ComponentParameter()
        {
            Name = "TitleFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "Text for the item tooltip."
        },
        new ComponentParameter()
        {
            Name = "TargetField",
            Type = "string",
            DefaultValue = "Target",
            Description = "Link target, specifies how to open the item link."
        },
        new ComponentParameter()
        {
            Name = "TargetFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "Link target, specifies how to open the item link."
        },
        new ComponentParameter()
        {
            Name = "UrlField",
            Type = "string",
            DefaultValue = "Url",
            Description = "URL to navigate for the item link."
        },
        new ComponentParameter()
        {
            Name = "UrlFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "URL to navigate for the item link."
        },
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
<BitNavList Items=""BitPlatformNavMenu""
            TextField=""@nameof(BitPlatformMenu.Text)""
            UrlField=""@nameof(BitPlatformMenu.Url)""
            IsEnabledField=""@nameof(BitPlatformMenu.IsEnabled)""
            ItemsField=""@nameof(BitPlatformMenu.Links)"" />
";

    private static string example1CSharpCode = @"
public class BitPlatformMenu
{
    public string Text { get; set; } = string.Empty;
    public string Url { get; set; }
    public bool IsEnabled { get; set; } = true;
    public List<BitPlatformMenu> Links { get; set; } = new();
}

private static readonly List<BitPlatformMenu> BitPlatformNavMenu = new()
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

    #endregion

    #region Sample Code 2

    private static string example2HTMLCode = @"
<BitNavList Items=""CarNavMenu""
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

    private static string example2CSharpCode = @"
private static readonly List<CarMenu> CarNavMenu = new()
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

    #endregion

    #region Sample Code 3

    private static string example3HTMLCode = @"
<div>
    <BitLabel>Basic</BitLabel>
    <BitNavList Items=""FoodNavMenu""
                TextFieldSelector=""item => item.Name""
                IconNameFieldSelector=""item => item.Icon""
                IsExpandedFieldSelector=""item => item.IsExpanded""
                ItemsFieldSelector=""item => item.Childs""
                DefaultSelectedItem=""FoodNavMenu[0].Childs[2]""
                Mode=""BitNavMode.Manual"" />
</div>

<div class=""margin-top"">
    <BitLabel>Two-Way Bind</BitLabel>

    <BitNavList @bind-SelectedItem=""SelectedFood""
                Items=""FoodNavMenu""
                TextFieldSelector=""item => item.Name""
                IconNameFieldSelector=""item => item.Icon""
                IsExpandedFieldSelector=""item => item.IsExpanded""
                ItemsFieldSelector=""item => item.Childs""
                Mode=""BitNavMode.Manual""
                OnSelectItem=""(FoodMenu item) => SelectedFoodName = FoodMenuDropDownItems.FirstOrDefault(i => i.Text == item.Name).Text"" />

    <BitDropDown @bind-Value=""SelectedFoodName""
                    Label=""Select Item""
                    Items=""FoodMenuDropDownItems""
                    OnSelectItem=""(item) => SelectedFood = Flatten(FoodNavMenu).FirstOrDefault(i => i.Name == item.Value)"" />
</div>
";

    private static string example3CSharpCode = @"
public class FoodMenu
{
    public string Name { get; set; } = string.Empty;
    public BitIconName Icon { get; set; }
    public bool IsExpanded { get; set; }
    public List<FoodMenu> Childs { get; set; } = new();
}

private static readonly List<FoodMenu> FoodNavMenu = new()
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
private FoodMenu SelectedFood = FoodNavMenu[0].Childs[2];
private string SelectedFoodName = FoodNavMenu[0].Childs[2].Name;
";

    #endregion

    #region Sample Code 4

    private static string example4HTMLCode = @"
<style>
    .nav-list-custom-header {
        font-size: 17px;
        font-weight: 600;
        color: green;
    }

    .nav-list-custom-item {
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
    <BitNavList Items=""CarNavMenu""
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
            <div class=""nav-list-custom-header"">
                <BitIcon IconName=""BitIconName.FavoriteStarFill"" />
                <span>@item.Name</span>
            </div>
        </HeaderTemplate>
    </BitNavList>
</div>

<div class=""margin-top"">
    <BitLabel>Item Template</BitLabel>
    <BitNavList Items=""FoodNavMenu""
                TextFieldSelector=""item => item.Name""
                IconNameFieldSelector=""item => item.Icon""
                IsExpandedFieldSelector=""item => item.IsExpanded""
                ItemsFieldSelector=""item => item.Childs""
                Mode=""BitNavMode.Manual"">

        <ItemTemplate Context=""item"">
            <div class=""nav-list-custom-item"">
                <BitCheckbox />
                <BitIcon IconName=""@item.Icon"" />
                <span>@item.Name</span>
            </div>
        </ItemTemplate>
    </BitNavList>
</div>
";

    private static string example4CSharpCode = @"
public class CarMenu
{
    public string Name { get; set; } = string.Empty;
    public string Tooltip { get; set; }
    public string PageUrl { get; set; }
    public string UrlTarget { get; set; }
    public string ExpandedAriaLabel { get; set; }
    public string CollapsedAriaLabel { get; set; }
    public bool IsExpandedParent { get; set; }
    public List<CarMenu> Links { get; set; } = new();
}

public class FoodMenu
{
    public string Name { get; set; } = string.Empty;
    public BitIconName Icon { get; set; }
    public bool IsExpanded { get; set; }
    public List<FoodMenu> Childs { get; set; } = new();
}

private static readonly List<CarMenu> CarNavMenu = new()
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

private static readonly List<FoodMenu> FoodNavMenu = new()
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

    #endregion

    #region Sample Code 5

    private static string example5HTMLCode = @"
<BitNavList Items=""FoodNavMenu""
            TextFieldSelector=""item => item.Name""
            IconNameFieldSelector=""item => item.Icon""
            IsExpandedFieldSelector=""item => item.IsExpanded""
            ItemsFieldSelector=""item => item.Childs""
            DefaultSelectedItem=""FoodNavMenu[0].Childs[2]""
            Mode=""BitNavMode.Manual""
            OnItemClick=""(FoodMenu item) => ClickedItem = item""
            OnSelectItem=""(FoodMenu item) => SelectedItem = item""
            OnItemToggle=""(FoodMenu item) => ToggledItem = item"" />

<div class=""flex"">
    <span>Clicked Item: @ClickedItem?.Name</span>
    <span>Selected Item: @SelectedItem?.Name</span>
    <span>Toggled Item: @(ToggledItem is null ? ""N/A"" : $""{ToggledItem.Name} ({(ToggledItem.IsExpanded ? ""Expanded"" : ""Collapsed"")})"")</span>
</div>
";

    private static string example5CSharpCode = @"
private static readonly List<FoodMenu> FoodNavMenu = new()
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

private FoodMenu ClickedItem;
private FoodMenu SelectedItem;
private FoodMenu ToggledItem;
";

    #endregion
}

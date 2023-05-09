using Bit.BlazorUI.Demo.Client.Shared.Models;
using Bit.BlazorUI.Demo.Client.Shared.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Demo.Client.Shared.Pages.Components.Nav;

public partial class BitNavDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "AriaCurrentField",
            Type = "string",
            DefaultValue = "AriaCurrent",
            Description = "Aria-current token for active nav item. Must be a valid token value, and defaults to 'page'."
        },
        new()
        {
            Name = "AriaCurrentFieldSelector",
            Type = "Expression<Func<TItem, BitNavListItemAriaCurrent>>?",
            Href = "nav-item-aria-current",
            LinkType = LinkType.Link,
            Description = "Aria-current token for active nav item. Must be a valid token value, and defaults to 'page'."
        },
        new()
        {
            Name = "AriaLabelField",
            Type = "string",
            DefaultValue = "AriaLabel",
            Description = "Aria label for the item. Ignored if collapseAriaLabel or expandAriaLabel is provided."
        },
        new()
        {
            Name = "AriaLabelFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "Aria label for the item. Ignored if collapseAriaLabel or expandAriaLabel is provided."
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            Description = "Items to render as children.",
        },
        new()
        {
            Name = "ChildItemsField",
            Type = "string",
            DefaultValue = "Items",
            Description = "A list of items to render as children of the current item."
        },
        new()
        {
            Name = "ChildItemsFieldSelector",
            Type = "Expression<Func<TItem, IList<TItem>>>?",
            Description = "A list of items to render as children of the current item."
        },
        new()
        {
            Name = "ClassStyles",
            Type = "BitNavClassStyles",
            DefaultValue = "",
            Href = "class-styles",
            LinkType = LinkType.Link,
            Description = "Custom CSS classes/styles for different parts of the component."
        },
        new()
        {
            Name = "CollapseAriaLabelField",
            Type = "string",
            DefaultValue = "CollapseAriaLabel",
            Description = "Aria label when group is collapsed."
        },
        new()
        {
            Name = "CollapseAriaLabelFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "Aria label when group is collapsed."
        },
        new()
        {
            Name = "DefaultSelectedItem",
            Type = "TItem?",
            Description = "The initially selected item in manual mode."
        },
        new()
        {
            Name = "ExpandAriaLabelField",
            Type = "string",
            DefaultValue = "ExpandAriaLabel",
            Description = "Aria label when group is expanded."
        },
        new()
        {
            Name = "ExpandAriaLabelFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "Aria label when group is expanded."
        },
        new()
        {
            Name = "ForceAnchorField",
            Type = "string",
            DefaultValue = "ForceAnchor",
            Description = "(Optional) By default, any link with onClick defined will render as a button. Set this property to true to override that behavior. (Links without onClick defined will render as anchors by default.)"
        },
        new()
        {
            Name = "ForceAnchorFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "(Optional) By default, any link with onClick defined will render as a button. Set this property to true to override that behavior. (Links without onClick defined will render as anchors by default.)"
        },
        new()
        {
            Name = "HeaderTemplate",
            Type = "RenderFragment<TItem>?",
            Description = "Used to customize how content inside the group header is rendered."
        },
        new()
        {
            Name = "IconNameField",
            Type = "string",
            DefaultValue = "IconName",
            Description = "Name of an icon to render next to the item button."
        },
        new()
        {
            Name = "IconNameFieldSelector",
            Type = "Expression<Func<TItem, BitIconName>>?",
            Description = "Name of an icon to render next to the item button."
        },
        new()
        {
            Name = "IsExpandedField",
            Type = "string",
            DefaultValue = "IsExpanded",
            Description = "Whether or not the group is in an expanded state."
        },
        new()
        {
            Name = "IsExpandedFieldSelector",
            Type = "Expression<Func<TItem, bool>>?",
            Description = "Whether or not the group is in an expanded state."
        },
        new()
        {
            Name = "IsEnabledField",
            Type = "string",
            DefaultValue = "IsEnabled",
            Description = "Whether or not the item is disabled."
        },
        new()
        {
            Name = "IsEnabledFieldSelector",
            Type = "Expression<Func<TItem, bool>>?",
            Description = "Whether or not the item is disabled."
        },
        new()
        {
            Name = "Items",
            Type = "IList<TItem>",
            DefaultValue = "new List<TItem>()",
            Href="nav-item",
            LinkType = LinkType.Link,
            Description = "A collection of item to display in the navigation bar."
        },
        new()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<TItem>?",
            Description = "Used to customize how content inside the item is rendered."
        },
        new()
        {
            Name = "KeyField",
            Type = "string",
            DefaultValue = "Key",
            Description = "A unique value to use as a key or id of the item."
        },
        new()
        {
            Name = "KeyFieldSelector",
            Type = "Expression<Func<TItem, bool>>?",
            Description = "A unique value to use as a key or id of the item."
        },
        new()
        {
            Name = "Mode",
            Type = "BitNavMode",
            DefaultValue = "BitNavMode.Automatic",
            Href = "nav-mode-enum",
            LinkType = LinkType.Link,
            Description = "Determines how the navigation will be handled."
        },
        new()
        {
            Name = "OnItemClick",
            Type = "EventCallback<TItem>",
            Description = "Callback invoked when an item is clicked."
        },
        new()
        {
            Name = "OnItemToggle",
            Type = "EventCallback<TItem>",
            Description = "Callback invoked when a group header is clicked and Expanded or Collapse."
        },
        new()
        {
            Name = "OnSelectItem",
            Type = "EventCallback<TItem>",
            Description = "Callback invoked when an item is selected."
        },
        new()
        {
            Name = "RenderType",
            Type = "BitNavRenderType",
            DefaultValue = "BitNavRenderType.Normal",
            Href = "nav-render-type-enum",
            LinkType = LinkType.Link,
            Description = "The way to render nav items."
        },
        new()
        {
            Name = "SelectedItem",
            Type = "TItem?",
            Description = "Selected item to show in Nav."
        },
        new()
        {
            Name = "StyleField",
            Type = "string",
            DefaultValue = "Style",
            Description = "Custom style for the each item element."
        },
        new()
        {
            Name = "StyleFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "Custom style for the each item element."
        },
        new()
        {
            Name = "TextField",
            Type = "string",
            DefaultValue = "Name",
            Description = "Text to render for the item."
        },
        new()
        {
            Name = "TextFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "Text to render for the item."
        },
        new()
        {
            Name = "TitleField",
            Type = "string",
            DefaultValue= "Title",
            Description = "Text for the item tooltip."
        },
        new()
        {
            Name = "TitleFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "Text for the item tooltip."
        },
        new()
        {
            Name = "TargetField",
            Type = "string",
            DefaultValue = "Target",
            Description = "Link target, specifies how to open the item link."
        },
        new()
        {
            Name = "TargetFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "Link target, specifies how to open the item link."
        },
        new()
        {
            Name = "UrlField",
            Type = "string",
            DefaultValue = "Url",
            Description = "URL to navigate for the item link."
        },
        new()
        {
            Name = "UrlFieldSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "URL to navigate for the item link."
        }
    };

    private readonly List<ComponentSubClass> componentSubClasses = new()
    {
        new()
        {
            Id = "nav-item",
            Title = "BitNavItem",
            Parameters = new()
            {
               new()
               {
                   Name = "AriaLabel",
                   Type = "string?",
                   Description = "Aria label for nav link. Ignored if collapseAriaLabel or expandAriaLabel is provided",
               },
               new()
               {
                   Name = "AriaCurrent",
                   Type = "BitNavLinkItemAriaCurrent",
                   DefaultValue = "BitNavItemAriaCurrent.Page",
                   Description = "Aria-current token for active nav links. Must be a valid token value, and defaults to 'page'",
                   Href = "#nav-item-aria-current-enum",
                   LinkType = LinkType.Link,
               },
               new()
               {
                   Name = "CollapseAriaLabel",
                   Type = "string?",
                   Description = "Aria label when items is collapsed and can be expanded.",
               },
               new()
               {
                   Name = "ExpandAriaLabel",
                   Type = "string?",
                   Description = "Aria label when group is collapsed and can be expanded.",
               },
               new()
               {
                   Name = "ForceAnchor",
                   Type = "bool",
                   Description = "(Optional) By default, any link with onClick defined will render as a button. Set this property to true to override that behavior. (Links without onClick defined will render as anchors by default.)",
               },
               new()
               {
                   Name = "ChildItems",
                   Type = "IList<BitNavItem>",
                   DefaultValue = "new List<BitNavItem>()",
                   Description = "A list of items to render as children of the current item.",
               },
               new()
               {
                   Name = "IconName",
                   Type = "BitIconName",
                   Description = "Name of an icon to render next to this link button.",
               },
               new()
               {
                   Name = "IsExpanded",
                   Type = "bool",
                   Description = "Whether or not the link is in an expanded state.",
               },
               new()
               {
                   Name = "IsEnabled",
                   Type = "bool",
                   Description = "Whether or not the link is enabled.",
               },
               new()
               {
                   Name = "Key",
                   Type = "string?",
                   Description = "A unique value to use as a key or id of the item.",
               },
               new()
               {
                   Name = "Style",
                   Type = "string?",
                   Description = "Custom style for the each item element.",
               },
               new()
               {
                   Name = "Text",
                   Type = "string",
                   DefaultValue = "string.Empty",
                   Description = "Text to render for this link.",
               },
               new()
               {
                   Name = "Title",
                   Type = "string?",
                   Description = "Text for title tooltip.",
               },
               new()
               {
                   Name = "Target",
                   Type = "string?",
                   Description = "Link target, specifies how to open the link.",
               },
               new()
               {
                   Name = "Url",
                   Type = "string?",
                   Description = "URL to navigate to for this link.",
               }
            }
        },
        new ()
        {
            Id = "nav-option",
            Title = "BitNavOption",
            Parameters = new()
            {
               new()
               {
                   Name = "AriaLabel",
                   Type = "string?",
                   Description = "Aria label for nav link. Ignored if collapseAriaLabel or expandAriaLabel is provided",
               },
               new()
               {
                   Name = "AriaCurrent",
                   Type = "BitNavLinkItemAriaCurrent",
                   DefaultValue = "BitNavItemAriaCurrent.Page",
                   Description = "Aria-current token for active nav links. Must be a valid token value, and defaults to 'page'",
                   Href = "#nav-item-aria-current-enum",
                   LinkType = LinkType.Link,
               },
               new()
               {
                   Name = "CollapseAriaLabel",
                   Type = "string?",
                   Description = "Aria label when items is collapsed and can be expanded.",
               },
               new()
               {
                   Name = "ExpandAriaLabel",
                   Type = "string?",
                   Description = "Aria label when group is collapsed and can be expanded.",
               },
               new()
               {
                   Name = "ForceAnchor",
                   Type = "bool",
                   Description = "(Optional) By default, any link with onClick defined will render as a button. Set this property to true to override that behavior. (Links without onClick defined will render as anchors by default.)",
               },
               new()
               {
                   Name = "IconName",
                   Type = "BitIconName",
                   Description = "Name of an icon to render next to this link button.",
               },
               new()
               {
                   Name = "IsExpanded",
                   Type = "bool",
                   Description = "Whether or not the link is in an expanded state.",
               },
               new()
               {
                   Name = "IsEnabled",
                   Type = "bool",
                   Description = "Whether or not the link is enabled.",
               },
               new()
               {
                   Name = "Key",
                   Type = "string?",
                   Description = "A unique value to use as a key or id of the item.",
               },
               new()
               {
                   Name = "Style",
                   Type = "string?",
                   Description = "Custom style for the each item element.",
               },
               new()
               {
                   Name = "Text",
                   Type = "string",
                   DefaultValue = "string.Empty",
                   Description = "Text to render for this link.",
               },
               new()
               {
                   Name = "Title",
                   Type = "string?",
                   Description = "Text for title tooltip.",
               },
               new()
               {
                   Name = "Target",
                   Type = "string?",
                   Description = "Link target, specifies how to open the link.",
               },
               new()
               {
                   Name = "Url",
                   Type = "string?",
                   Description = "URL to navigate to for this link.",
               }
            }
        },
        new()
        {
            Id = "class-styles",
            Title = "BitNavClassStyles",
            Parameters = new()
            {
               new()
               {
                   Name = "Item",
                   Type = "BitClassStylePair?",
                   Description = "Custom CSS classes/styles for item.",
                   Href = "class-style-pair",
                   LinkType = LinkType.Link
               },
               new()
               {
                   Name = "SelectedItem",
                   Type = "BitClassStylePair?",
                   Description = "Custom CSS classes/styles for selected item.",
                   Href = "class-style-pair",
                   LinkType = LinkType.Link
               },
               new()
               {
                   Name = "ItemContainer",
                   Type = "BitClassStylePair?",
                   Description = "Custom CSS classes/styles for item container.",
                   Href = "class-style-pair",
                   LinkType = LinkType.Link,
               },
               new()
               {
                   Name = "SelectedItemContainer",
                   Type = "BitClassStylePair?",
                   Description = "Custom CSS classes/styles for selected item container.",
                   Href = "class-style-pair",
                   LinkType = LinkType.Link
               },
               new()
               {
                   Name = "ToggleButton",
                   Type = "BitClassStylePair?",
                   Description = "Custom CSS classes/styles for toggle button.",
                   Href = "class-style-pair",
                   LinkType = LinkType.Link
               },
            }
        },
        new()
        {
            Id = "class-style-pair",
            Title = "BitClassStylePair",
            Parameters = new()
            {
               new()
               {
                   Name = "Class",
                   Type = "string?",
                   Description = "Custom CSS class."
               },
               new()
               {
                   Name = "Style",
                   Type = "string?",
                   Description = "Custom CSS style."
               }
            }
        }
    };

    private readonly List<ComponentSubEnum> componentSubEnums = new()
    {
        new()
        {
            Id = "nav-mode-enum",
            Name = "BitNavMode",
            Items = new()
            {
                new()
                {
                    Name = "Automatic",
                    Description = "The value of selected key will change using NavigationManager and the current url inside the component.",
                    Value = "0",
                },
                new()
                {
                    Name = "Manual",
                    Description = "Selected key changes will be sent back to the parent component and the component won't change its value.",
                    Value = "1",
                }
            }
        },
        new()
        {
            Id = "nav-render-type-enum",
            Name = "BitNavRenderType",
            Items = new()
            {
                new()
                {
                    Name = "Normal",
                    Value = "0",
                },
                new()
                {
                    Name = "Grouped",
                    Value = "1",
                }
            }
        },
        new()
        {
            Id = "nav-item-aria-current-enum",
            Name = "BitNavItemAriaCurrent",
            Items = new()
            {
                new()
                {
                    Name = "Page",
                    Value = "0",
                },
                new()
                {
                    Name = "Step",
                    Value = "1",
                },
                new()
                {
                    Name = "Location",
                    Value = "2",
                },
                new()
                {
                    Name = "Date",
                    Value = "3",
                },
                new()
                {
                    Name = "Time",
                    Value = "4",
                },
                new()
                {
                    Name = "True",
                    Value = "5",
                },

            }
        },
    };



    // Basic
    private static readonly List<BitNavItem> BitPlatformNavMenu = new()
    {
        new()
        {
            Text = "Bit Platform",
            ChildItems = new()
            {
                new() { Text = "Home", Url = "https://bitplatform.dev/" },
                new()
                {
                    Text = "Products & Services",
                    ChildItems = new()
                    {
                        new()
                        {
                            Text = "Project Templates",
                            ChildItems = new()
                            {
                                new() { Text = "TodoTemplate", Url = "https://bitplatform.dev/todo-template/overview" },
                                new() { Text = "AdminPanel", Url = "https://bitplatform.dev/admin-panel/overview" },
                            }
                        },
                        new() { Text = "BlazorUI", Url = "https://bitplatform.dev/components" },
                        new() { Text = "Cloud hosting solutions", Url = "https://bitplatform.dev/#", IsEnabled = false },
                        new() { Text = "Bit academy", Url = "https://bitplatform.dev/#", IsEnabled = false },
                    }
                },
                new() { Text = "Pricing", Url = "https://bitplatform.dev/pricing" },
                new() { Text = "About", Url = "https://bitplatform.dev/about-us" },
                new() { Text = "Contact us", Url = "https://bitplatform.dev/contact-us" },
            },
        },
        new()
        {
            Text = "Community",
            ChildItems = new()
            {
                new() { Text = "LinkedIn", Url = "https://www.linkedin.com/company/bitplatformhq/about/" },
                new() { Text = "Twitter", Url = "https://twitter.com/bitplatformhq" },
                new() { Text = "GitHub repo", Url = "https://github.com/bitfoundation/bitplatform" },
            }
        },
        new() { Text = "Iconography", Url = "/icons" },
    };
    // Grouped
    private static readonly List<BitNavItem> CarNavMenu = new()
    {
        new()
        {
            Text = "Mercedes-Benz",
            ExpandAriaLabel = "Mercedes-Benz Expanded",
            CollapseAriaLabel = "Mercedes-Benz Collapsed",
            Title = "Mercedes-Benz Car Models",
            IsExpanded = true,
            ChildItems = new()
            {
                new()
                {
                    Text = "SUVs",
                    ChildItems = new()
                    {
                        new() { Text = "GLA", Url = "https://www.mbusa.com/en/vehicles/class/gla/suv", Target = "_blank" },
                        new() { Text = "GLB", Url = "https://www.mbusa.com/en/vehicles/class/glb/suv", Target = "_blank" },
                        new() { Text = "GLC", Url = "https://www.mbusa.com/en/vehicles/class/glc/suv", Target = "_blank" },
                    }
                },
                new()
                {
                    Text = "Sedans & Wagons",
                    ChildItems = new()
                    {
                        new() { Text = "A Class", Url = "https://www.mbusa.com/en/vehicles/class/a-class/sedan", Target = "_blank" },
                        new() { Text = "C Class", Url = "https://www.mbusa.com/en/vehicles/class/c-class/sedan", Target = "_blank" },
                        new() { Text = "E Class", Url = "https://www.mbusa.com/en/vehicles/class/e-class/sedan", Target = "_blank" },
                    }
                },
                new()
                {
                    Text = "Coupes",
                    ChildItems = new()
                    {
                        new() { Text = "CLA Coupe", Url = "https://www.mbusa.com/en/vehicles/class/cla/coupe", Target = "_blank" },
                        new() { Text = "C Class Coupe", Url = "https://www.mbusa.com/en/vehicles/class/c-class/coupe", Target = "_blank" },
                        new() { Text = "E Class Coupe", Url = "https://www.mbusa.com/en/vehicles/class/e-class/coupe", Target = "_blank" },
                    }
                },
            }
        },
        new()
        {
            Text = "Tesla",
            ExpandAriaLabel = "Tesla Expanded",
            CollapseAriaLabel= "Tesla Collapsed",
            Title = "Tesla Car Models",
            ChildItems = new List<BitNavItem>
            {
                new() { Text = "Model S", Url = "https://www.tesla.com/models", Target = "_blank" },
                new() { Text = "Model X", Url = "https://www.tesla.com/modelx", Target = "_blank" },
                new() { Text = "Model Y", Url = "https://www.tesla.com/modely", Target = "_blank" },
            }
        },
    };
    // Manual
    private static readonly List<BitNavItem> FoodNavMenu = new()
    {
        new()
        {
            Text = "Fast-Food",
            IconName = BitIconName.HeartBroken,
            IsExpanded = true,
            ChildItems = new()
            {
                new()
                {
                    Text = "Burgers",
                    ChildItems = new()
                    {
                        new() { Text = "Beef Burger" },
                        new() { Text = "Veggie Burger" },
                        new() { Text = "Bison Burger" },
                        new() { Text = "Wild Salmon Burger" },
                    }
                },
                new()
                {
                    Text = "Pizzas",
                    ChildItems = new()
                    {
                        new() { Text = "Cheese Pizza" },
                        new() { Text = "Veggie Pizza" },
                        new() { Text = "Pepperoni Pizza" },
                        new() { Text = "Meat Pizza" },
                    }
                },
                new() { Text    = "French Fries" },
            }
        },
        new()
        {
            Text = "Fruits",
            IconName = BitIconName.Health,
            ChildItems = new()
            {
                new() { Text = "Apple" },
                new() { Text = "Orange" },
                new() { Text = "Banana" },
            }
        },
        new() { Text = "Ice Cream" },
        new() { Text = "Cookie" },
    };

    private static List<BitNavItem> Flatten(IList<BitNavItem> e) => e.SelectMany(c => Flatten(c.ChildItems)).Concat(e).ToList();
    private BitNavItem? SelectedItemNav = FoodNavMenu[0].ChildItems[2];
    private string? SelectedItemText = FoodNavMenu[0].ChildItems[2].Text;

    private BitNavItem ClickedItem;
    private BitNavItem SelectedItem;
    private BitNavItem ToggledItem;


    // Basic
    private static readonly List<BitPlatformMenu> CustomBitPlatformNavMenu = new()
    {
        new()
        {
            Text = "Bit Platform",
            Links = new()
            {
                new() { Text = "Home", Url = "https://bitplatform.dev/" },
                new()
                {
                    Text = "Products & Services",
                    Links = new()
                    {
                        new()
                        {
                            Text = "Project Templates",
                            Links = new()
                            {
                                new() { Text = "TodoTemplate", Url = "https://bitplatform.dev/todo-template/overview" },
                                new() { Text = "AdminPanel", Url = "https://bitplatform.dev/admin-panel/overview" },
                            }
                        },
                        new() { Text = "BlazorUI", Url = "https://bitplatform.dev/components" },
                        new() { Text = "Cloud hosting solutions", Url = "https://bitplatform.dev/#", IsEnabled = false },
                        new() { Text = "Bit academy", Url = "https://bitplatform.dev/#", IsEnabled = false },
                    }
                },
                new() { Text = "Pricing", Url = "https://bitplatform.dev/pricing" },
                new() { Text = "About", Url = "https://bitplatform.dev/about-us" },
                new() { Text = "Contact us", Url = "https://bitplatform.dev/contact-us" },
            },
        },
        new()
        {
            Text = "Community",
            Links = new()
            {
                new() { Text = "Linkedin", Url = "https://www.linkedin.com/company/bitplatformhq/about/" },
                new() { Text = "Twitter", Url = "https://twitter.com/bitplatformhq" },
                new() { Text = "Github repo", Url = "https://github.com/bitfoundation/bitplatform" },
            }
        },
        new() { Text = "Iconography", Url = "/icons" },
    };
    // Grouped
    private static readonly List<CarMenu> CustomCarNavMenu = new()
    {
        new()
        {
            Name = "Mercedes-Benz",
            ExpandedAriaLabel = "Mercedes-Benz Expanded",
            CollapsedAriaLabel = "Mercedes-Benz Collapsed",
            Tooltip = "Mercedes-Benz Car Models",
            IsExpandedParent = true,
            Links = new()
            {
                new()
                {
                    Name = "SUVs",
                    Links = new()
                    {
                        new() { Name = "GLA", PageUrl = "https://www.mbusa.com/en/vehicles/class/gla/suv", UrlTarget = "_blank" },
                        new() { Name = "GLB", PageUrl = "https://www.mbusa.com/en/vehicles/class/glb/suv", UrlTarget = "_blank" },
                        new() { Name = "GLC", PageUrl = "https://www.mbusa.com/en/vehicles/class/glc/suv", UrlTarget = "_blank" },
                    }
                },
                new()
                {
                    Name = "Sedans & Wagons",
                    Links = new()
                    {
                        new() { Name = "A Class", PageUrl = "https://www.mbusa.com/en/vehicles/class/a-class/sedan", UrlTarget = "_blank" },
                        new() { Name = "C Class", PageUrl = "https://www.mbusa.com/en/vehicles/class/c-class/sedan", UrlTarget = "_blank" },
                        new() { Name = "E Class", PageUrl = "https://www.mbusa.com/en/vehicles/class/e-class/sedan", UrlTarget = "_blank" },
                    }
                },
                new()
                {
                    Name = "Coupes",
                    Links = new()
                    {
                        new() { Name = "CLA Coupe", PageUrl = "https://www.mbusa.com/en/vehicles/class/cla/coupe", UrlTarget = "_blank" },
                        new() { Name = "C Class Coupe", PageUrl = "https://www.mbusa.com/en/vehicles/class/c-class/coupe", UrlTarget = "_blank" },
                        new() { Name = "E Class Coupe", PageUrl = "https://www.mbusa.com/en/vehicles/class/e-class/coupe", UrlTarget = "_blank" },
                    }
                },
            }
        },
        new()
        {
            Name = "Tesla",
            ExpandedAriaLabel = "Tesla Expanded",
            CollapsedAriaLabel = "Tesla Collapsed",
            Tooltip = "Tesla Car Models",
            Links = new()
            {
                new() { Name = "Model S", PageUrl = "https://www.tesla.com/models", UrlTarget = "_blank" },
                new() { Name = "Model X", PageUrl = "https://www.tesla.com/modelx", UrlTarget = "_blank" },
                new() { Name = "Model Y", PageUrl = "https://www.tesla.com/modely", UrlTarget = "_blank" },
            }
        },
    };
    // Manual
    private static readonly List<FoodMenu> CustomFoodNavMenu = new()
    {
        new()
        {
            Name = "Fast-Food",
            Icon = BitIconName.HeartBroken,
            IsExpanded = true,
            Childs = new()
            {
                new()
                {
                    Name = "Burgers",
                    Childs = new()
                    {
                        new() { Name = "Beef Burger" },
                        new() { Name = "Veggie Burger" },
                        new() { Name = "Bison Burger" },
                        new() { Name = "Wild Salmon Burger" },
                    }
                },
                new()
                {
                    Name = "Pizzas",
                    Childs = new()
                    {
                        new() { Name = "Cheese Pizza" },
                        new() { Name = "Veggie Pizza" },
                        new() { Name = "Pepperoni Pizza" },
                        new() { Name = "Meat Pizza" },
                    }
                },
                new() { Name = "French Fries" },
            }
        },
        new()
        {
            Name = "Fruits",
            Icon = BitIconName.Health,
            Childs = new()
            {
                new() { Name = "Apple" },
                new() { Name = "Orange" },
                new() { Name = "Banana" },
            }
        },
        new() { Name = "Ice Cream" },
        new() { Name = "Cookie" },
    };

    private static List<FoodMenu> Flatten(IList<FoodMenu> e) => e.SelectMany(c => Flatten(c.Childs)).Concat(e).ToList();
    private FoodMenu? CustomSelectedFood = CustomFoodNavMenu[0].Childs[2];
    private string? CustomSelectedFoodName = CustomFoodNavMenu[0].Childs[2].Name;

    private FoodMenu CustomClickedItem;
    private FoodMenu CustomSelectedItem;
    private FoodMenu CustomToggledItem;


    private string SelectedOptionKey;

    private BitNavOption ClickedOption;
    private BitNavOption SelectedOption;
    private BitNavOption ToggledOption;


    private static readonly List<BitDropdownItem> FoodMenuDropdownItems = new()
    {
        new()
        {
            Text = "Beef Burger",
            Value = "Beef Burger",
        },
        new()
        {
            Text = "Veggie Burger",
            Value = "Veggie Burger",
        },
        new()
        {
            Text = "Bison Burger",
            Value = "Bison Burger",
        },
        new()
        {
            Text = "Wild Salmon Burger",
            Value = "Wild Salmon Burger",
        },
        new()
        {
            Text = "Cheese Pizza",
            Value = "Cheese Pizza",
        },
        new()
        {
            Text = "Veggie Pizza",
            Value = "Veggie Pizza",
        },
        new()
        {
            Text = "Pepperoni Pizza",
            Value = "Pepperoni Pizza",
        },
        new()
        {
            Text = "Meat Pizza",
            Value = "Meat Pizza",
        },
        new()
        {
            Text = "French Fries",
            Value = "French Fries",
        },
        new()
        {
            Text = "Apple",
            Value = "Apple",
        },
        new()
        {
            Text = "Orange",
            Value = "Orange",
        },
        new()
        {
            Text = "Banana",
            Value = "Banana",
        },
        new()
        {
            Text = "Ice Cream",
            Value = "Ice Cream",
        },
        new()
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
    new ()
    {
        Text = ""Bit Platform"",
        ChildItems = new List<BitNavItem>
        {
            new() { Text = ""Home"", Url = ""https://bitplatform.dev/"" },
            new()
            {
                Text = ""Products & Services"",
                ChildItems = new List<BitNavItem>
                {
                    new()
                    {
                        Text = ""Project Templates"",
                        ChildItems = new List<BitNavItem>
                        {
                            new() { Text = ""TodoTemplate"", Url = ""https://bitplatform.dev/todo-template/overview"" },
                            new() { Text = ""AdminPanel"", Url = ""https://bitplatform.dev/admin-panel/overview"" },
                        }
                    },
                    new() { Text = ""BlazorUI"", Url = ""https://bitplatform.dev/components"" },
                    new() { Text = ""Cloud hosting solutions"", Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                    new() { Text = ""Bit academy"", Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                }
            },
            new() { Text = ""Pricing"", Url = ""https://bitplatform.dev/pricing"" },
            new() { Text = ""About"", Url = ""https://bitplatform.dev/about-us"" },
            new() { Text = ""Contact us"", Url = ""https://bitplatform.dev/contact-us"" },
        },
    },
    new()
    {
        Text = ""Community"",
        ChildItems = new List<BitNavItem>
        {
            new() { Text = ""Linkedin"", Url = ""https://www.linkedin.com/company/bitplatformhq/about/"" },
            new() { Text = ""Twitter"", Url = ""https://twitter.com/bitplatformhq"" },
            new() { Text = ""Github repo"", Url = ""https://github.com/bitfoundation/bitplatform"" },
        }
    },
    new() { Text = ""Iconography"", Url = ""/icons"" },
};
";

    private static string example2NavItemHTMLCode = @"
<BitNav Items=""CarNavMenu"" RenderType=""BitNavRenderType.Grouped"" />
";
    private static string example2NavItemCSharpCode = @"
private static readonly List<BitNavItem> CarNavMenu = new()
{
    new()
    {
        Text = ""Mercedes-Benz"",
        ExpandAriaLabel = ""Mercedes-Benz Expanded"",
        CollapseAriaLabel = ""Mercedes-Benz Collapsed"",
        Title = ""Mercedes-Benz Car Models"",
        IsExpanded = true,
        ChildItems = new List<BitNavItem>
        {
            new()
            {
                Text = ""SUVs"",
                ChildItems = new List<BitNavItem>
                {
                    new() { Text = ""GLA"", Url = ""https://www.mbusa.com/en/vehicles/class/gla/suv"", Target = ""_blank"" },
                    new() { Text = ""GLB"", Url = ""https://www.mbusa.com/en/vehicles/class/glb/suv"", Target = ""_blank"" },
                    new() { Text = ""GLC"", Url = ""https://www.mbusa.com/en/vehicles/class/glc/suv"", Target = ""_blank"" },
                }
            },
            new()
            {
                Text = ""Sedans & Wagons"",
                ChildItems = new List<BitNavItem>
                {
                    new() { Text = ""A Class"", Url = ""https://www.mbusa.com/en/vehicles/class/a-class/sedan"", Target = ""_blank"" },
                    new() { Text = ""C Class"", Url = ""https://www.mbusa.com/en/vehicles/class/c-class/sedan"", Target = ""_blank"" },
                    new() { Text = ""E Class"", Url = ""https://www.mbusa.com/en/vehicles/class/e-class/sedan"", Target = ""_blank"" },
                }
            },
            new()
            {
                Text = ""Coupes"",
                ChildItems = new List<BitNavItem>
                {
                    new() { Text = ""CLA Coupe"", Url = ""https://www.mbusa.com/en/vehicles/class/cla/coupe"", Target = ""_blank"" },
                    new() { Text = ""C Class Coupe"", Url = ""https://www.mbusa.com/en/vehicles/class/c-class/coupe"", Target = ""_blank"" },
                    new() { Text = ""E Class Coupe"", Url = ""https://www.mbusa.com/en/vehicles/class/e-class/coupe"", Target = ""_blank"" },
                }
            },
        }
    },
    new()
    {
        Text = ""Tesla"",
        ExpandAriaLabel = ""Tesla Expanded"",
        CollapseAriaLabel= ""Tesla Collapsed"",
        Title = ""Tesla Car Models"",
        ChildItems = new List<BitNavItem>
        {
            new() { Text = ""Model S"", Url = ""https://www.tesla.com/models"", Target = ""_blank"" },
            new() { Text = ""Model X"", Url = ""https://www.tesla.com/modelx"", Target = ""_blank"" },
            new() { Text = ""Model Y"", Url = ""https://www.tesla.com/modely"", Target = ""_blank"" },
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
    new()
    {
        Text = ""Fast-Food"",
        IconName = BitIconName.HeartBroken,
        IsExpanded = true,
        ChildItems = new List<BitNavItem>
        {
            new() 
            {
                Text = ""Burgers"",
                ChildItems = new List<BitNavItem>
                {
                    new() { Text = ""Beef Burger"" },
                    new() { Text = ""Veggie Burger"" },
                    new() { Text = ""Bison Burger"" },
                    new() { Text = ""Wild Salmon Burger"" },
                }
            },
            new()
            {
                Text = ""Pizzas"",
                ChildItems = new List<BitNavItem>
                {
                    new() { Text = ""Cheese Pizza"" },
                    new() { Text = ""Veggie Pizza"" },
                    new() { Text = ""Pepperoni Pizza"" },
                    new() { Text = ""Meat Pizza"" },
                }
            },
            new() { Text    = ""French Fries"" },
        }
    },
    new()
    {
        Text = ""Fruits"",
        IconName = BitIconName.Health,
        ChildItems = new List<BitNavItem>
        {
            new() { Text = ""Aplle"" },
            new() { Text = ""Orange"" },
            new() { Text = ""Benana"" },
        }
    },
    new() { Text = ""Ice Cream"" },
    new() { Text = ""Cookie"" },
};

private static readonly List<BitDropDownItem> FoodMenuDropDownItems = new()
{
    new()
    {
        Text = ""Beef Burger"",
        Value = ""Beef Burger"",
    },
    new()
    {
        Text = ""Veggie Burger"",
        Value = ""Veggie Burger"",
    },
    new()
    {
        Text = ""Bison Burger"",
        Value = ""Bison Burger"",
    },
    new()
    {
        Text = ""Wild Salmon Burger"",
        Value = ""Wild Salmon Burger"",
    },
    new()
    {
        Text = ""Cheese Pizza"",
        Value = ""Cheese Pizza"",
    },
    new()
    {
        Text = ""Veggie Pizza"",
        Value = ""Veggie Pizza"",
    },
    new()
    {
        Text = ""Pepperoni Pizza"",
        Value = ""Pepperoni Pizza"",
    },
    new()
    {
        Text = ""Meat Pizza"",
        Value = ""Meat Pizza"",
    },
    new()
    {
        Text = ""French Fries"",
        Value = ""French Fries"",
    },
    new()
    {
        Text = ""Aplle"",
        Value = ""Aplle"",
    },
    new()
    {
        Text = ""Orange"",
        Value = ""Orange"",
    },
    new()
    {
        Text = ""Benana"",
        Value = ""Benana"",
    },
    new()
    {
        Text = ""Ice Cream"",
        Value = ""Ice Cream"",
    },
    new()
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
    new()
    {
        Text = ""Mercedes-Benz"",
        ExpandAriaLabel = ""Mercedes-Benz Expanded"",
        CollapseAriaLabel = ""Mercedes-Benz Collapsed"",
        Title = ""Mercedes-Benz Car Models"",
        IsExpanded = true,
        ChildItems = new List<BitNavItem>
        {
            new()
            {
                Text = ""SUVs"",
                ChildItems = new List<BitNavItem>
                {
                    new() { Text = ""GLA"", Url = ""https://www.mbusa.com/en/vehicles/class/gla/suv"", Target = ""_blank"" },
                    new() { Text = ""GLB"", Url = ""https://www.mbusa.com/en/vehicles/class/glb/suv"", Target = ""_blank"" },
                    new() { Text = ""GLC"", Url = ""https://www.mbusa.com/en/vehicles/class/glc/suv"", Target = ""_blank"" },
                }
            },
            new()
            {
                Text = ""Sedans & Wagons"",
                ChildItems = new List<BitNavItem>
                {
                    new() { Text = ""A Class"", Url = ""https://www.mbusa.com/en/vehicles/class/a-class/sedan"", Target = ""_blank"" },
                    new() { Text = ""C Class"", Url = ""https://www.mbusa.com/en/vehicles/class/c-class/sedan"", Target = ""_blank"" },
                    new() { Text = ""E Class"", Url = ""https://www.mbusa.com/en/vehicles/class/e-class/sedan"", Target = ""_blank"" },
                }
            },
            new()
            {
                Text = ""Coupes"",
                ChildItems = new List<BitNavItem>
                {
                    new() { Text = ""CLA Coupe"", Url = ""https://www.mbusa.com/en/vehicles/class/cla/coupe"", Target = ""_blank"" },
                    new() { Text = ""C Class Coupe"", Url = ""https://www.mbusa.com/en/vehicles/class/c-class/coupe"", Target = ""_blank"" },
                    new() { Text = ""E Class Coupe"", Url = ""https://www.mbusa.com/en/vehicles/class/e-class/coupe"", Target = ""_blank"" },
                }
            },
        }
    },
    new()
    {
        Text = ""Tesla"",
        ExpandAriaLabel = ""Tesla Expanded"",
        CollapseAriaLabel= ""Tesla Collapsed"",
        Title = ""Tesla Car Models"",
        ChildItems = new List<BitNavItem>
        {
            new() { Text = ""Model S"", Url = ""https://www.tesla.com/models"", Target = ""_blank"" },
            new() { Text = ""Model X"", Url = ""https://www.tesla.com/modelx"", Target = ""_blank"" },
            new() { Text = ""Model Y"", Url = ""https://www.tesla.com/modely"", Target = ""_blank"" },
        }
    },
};

private static readonly List<BitNavItem> FoodNavMenu = new()
{
    new()
    {
        Text = ""Fast-Food"",
        IconName = BitIconName.HeartBroken,
        IsExpanded = true,
        ChildItems = new List<BitNavItem>
        {
            new() 
            {
                Text = ""Burgers"",
                ChildItems = new List<BitNavItem>
                {
                    new() { Text = ""Beef Burger"" },
                    new() { Text = ""Veggie Burger"" },
                    new() { Text = ""Bison Burger"" },
                    new() { Text = ""Wild Salmon Burger"" },
                }
            },
            new()
            {
                Text = ""Pizzas"",
                ChildItems = new List<BitNavItem>
                {
                    new() { Text = ""Cheese Pizza"" },
                    new() { Text = ""Veggie Pizza"" },
                    new() { Text = ""Pepperoni Pizza"" },
                    new() { Text = ""Meat Pizza"" },
                }
            },
            new() { Text    = ""French Fries"" },
        }
    },
    new()
    {
        Text = ""Fruits"",
        IconName = BitIconName.Health,
        ChildItems = new List<BitNavItem>
        {
            new() { Text = ""Apple"" },
            new() { Text = ""Orange"" },
            new() { Text = ""Banana"" },
        }
    },
    new() { Text = ""Ice Cream"" },
    new() { Text = ""Cookie"" },
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
    new()
    {
        Text = ""Fast-Food"",
        IconName = BitIconName.HeartBroken,
        IsExpanded = true,
        ChildItems = new List<BitNavItem>
        {
            new() 
            {
                Text = ""Burgers"",
                ChildItems = new List<BitNavItem>
                {
                    new() { Text = ""Beef Burger"" },
                    new() { Text = ""Veggie Burger"" },
                    new() { Text = ""Bison Burger"" },
                    new() { Text = ""Wild Salmon Burger"" },
                }
            },
            new()
            {
                Text = ""Pizzas"",
                ChildItems = new List<BitNavItem>
                {
                    new() { Text = ""Cheese Pizza"" },
                    new() { Text = ""Veggie Pizza"" },
                    new() { Text = ""Pepperoni Pizza"" },
                    new() { Text = ""Meat Pizza"" },
                }
            },
            new() { Text    = ""French Fries"" },
        }
    },
    new()
    {
        Text = ""Fruits"",
        IconName = BitIconName.Health,
        ChildItems = new List<BitNavItem>
        {
            new() { Text = ""Aplle"" },
            new() { Text = ""Orange"" },
            new() { Text = ""Benana"" },
        }
    },
    new() { Text = ""Ice Cream"" },
    new() { Text = ""Cookie"" },
};

private BitNavItem ClickedItem;
private BitNavItem SelectedItem;
private BitNavItem ToggledItem;
";

    private static string example6NavItemHTMLCode = @"
<div class=""example-box"">
    <BitNav Items=""BitPlatformNavMenu""
            ClassStyles=""@(new() { ItemContainer = new() { Style=""border:1px solid green;margin:2px"" },
                                    ToggleButton = new() { Style=""color:cyan"" },
                                    Item = new() { Style=""color:red"" } })"" />
</div>";
    private static string example6NavItemCSharpCode = @"
private static readonly List<BitNavItem> BitPlatformNavMenu = new()
{
    new ()
    {
        Text = ""Bit Platform"",
        ChildItems = new List<BitNavItem>
        {
            new() { Text = ""Home"", Url = ""https://bitplatform.dev/"" },
            new()
            {
                Text = ""Products & Services"",
                ChildItems = new List<BitNavItem>
                {
                    new()
                    {
                        Text = ""Project Templates"",
                        ChildItems = new List<BitNavItem>
                        {
                            new() { Text = ""TodoTemplate"", Url = ""https://bitplatform.dev/todo-template/overview"" },
                            new() { Text = ""AdminPanel"", Url = ""https://bitplatform.dev/admin-panel/overview"" },
                        }
                    },
                    new() { Text = ""BlazorUI"", Url = ""https://bitplatform.dev/components"" },
                    new() { Text = ""Cloud hosting solutions"", Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                    new() { Text = ""Bit academy"", Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                }
            },
            new() { Text = ""Pricing"", Url = ""https://bitplatform.dev/pricing"" },
            new() { Text = ""About"", Url = ""https://bitplatform.dev/about-us"" },
            new() { Text = ""Contact us"", Url = ""https://bitplatform.dev/contact-us"" },
        },
    },
    new()
    {
        Text = ""Community"",
        ChildItems = new List<BitNavItem>
        {
            new() { Text = ""Linkedin"", Url = ""https://www.linkedin.com/company/bitplatformhq/about/"" },
            new() { Text = ""Twitter"", Url = ""https://twitter.com/bitplatformhq"" },
            new() { Text = ""Github repo"", Url = ""https://github.com/bitfoundation/bitplatform"" },
        }
    },
    new() { Text = ""Iconography"", Url = ""/icons"" },
};
";



    private static string example1CustomItemHTMLCode = @"
<BitNav Items=""CustomBitPlatformNavMenu""
        TextField=""@nameof(BitPlatformMenu.Text)""
        UrlField=""@nameof(BitPlatformMenu.Url)""
        IsEnabledField=""@nameof(BitPlatformMenu.IsEnabled)""
        ChildItemsField=""@nameof(BitPlatformMenu.Links)"" />
";
    private static string example1CustomItemCSharpCode = @"
public class BitPlatformMenu
{
    public string Text { get; set; } = string.Empty;
    public string Url { get; set; }
    public bool IsEnabled { get; set; } = true;
    public List<BitPlatformMenu> Links { get; set; } = new();
}

private static readonly List<BitPlatformMenu> CustomBitPlatformNavMenu = new()
{
    new()
    {
        Text = ""Bit Platform"",
        Links = new List<BitPlatformMenu>
        {
            new() { Text = ""Home"", Url = ""https://bitplatform.dev/"" },
            new()
            {
                Text = ""Products & Services"",
                Links = new List<BitPlatformMenu>
                {
                    new()
                    {
                        Text = ""Project Templates"",
                        Links = new List<BitPlatformMenu>
                        {
                            new() { Text = ""TodoTemplate"", Url = ""https://bitplatform.dev/todo-template/overview"" },
                            new() { Text = ""AdminPanel"", Url = ""https://bitplatform.dev/admin-panel/overview"" },
                        }
                    },
                    new() { Text = ""BlazorUI"", Url = ""https://bitplatform.dev/components"" },
                    new() { Text = ""Cloud hosting solutions"", Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                    new() { Text = ""Bit academy"", Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                }
            },
            new() { Text = ""Pricing"", Url = ""https://bitplatform.dev/pricing"" },
            new() { Text = ""About"", Url = ""https://bitplatform.dev/about-us"" },
            new() { Text = ""Contact us"", Url = ""https://bitplatform.dev/contact-us"" },
        },
    },
    new()
    {
        Text = ""Community"",
        Links = new List<BitPlatformMenu>
        {
            new() { Text = ""Linkedin"", Url = ""https://www.linkedin.com/company/bitplatformhq/about/"" },
            new() { Text = ""Twitter"", Url = ""https://twitter.com/bitplatformhq"" },
            new() { Text = ""Github repo"", Url = ""https://github.com/bitfoundation/bitplatform"" },
        }
    },
    new() { Text = ""Iconography"", Url = ""/icons"" },
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
        ChildItemsField=""@nameof(CarMenu.Links)""
        RenderType=""BitNavRenderType.Grouped"" />
";
    private static string example2CustomItemCSharpCode = @"
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

private static readonly List<CarMenu> CustomCarNavMenu = new()
{
    new()
    {
        Name = ""Mercedes-Benz"",
        ExpandedAriaLabel = ""Mercedes-Benz Expanded"",
        CollapsedAriaLabel = ""Mercedes-Benz Collapsed"",
        Tooltip = ""Mercedes-Benz Car Models"",
        IsExpandedParent = true,
        Links = new List<CarMenu>
        {
            new()
            {
                Name = ""SUVs"",
                Links = new List<CarMenu>
                {
                    new() { Name = ""GLA"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/gla/suv"", UrlTarget = ""_blank"" },
                    new() { Name = ""GLB"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/glb/suv"", UrlTarget = ""_blank"" },
                    new() { Name = ""GLC"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/glc/suv"", UrlTarget = ""_blank"" },
                }
            },
            new()
            {
                Name = ""Sedans & Wagons"",
                Links = new List<CarMenu>
                {
                    new() { Name = ""A Class"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/a-class/sedan"", UrlTarget = ""_blank"" },
                    new() { Name = ""C Class"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/c-class/sedan"", UrlTarget = ""_blank"" },
                    new() { Name = ""E Class"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/e-class/sedan"", UrlTarget = ""_blank"" },
                }
            },
            new()
            {
                Name = ""Coupes"",
                Links = new List<CarMenu>
                {
                    new() { Name = ""CLA Coupe"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/cla/coupe"", UrlTarget = ""_blank"" },
                    new() { Name = ""C Class Coupe"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/c-class/coupe"", UrlTarget = ""_blank"" },
                    new() { Name = ""E Class Coupe"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/e-class/coupe"", UrlTarget = ""_blank"" },
                }
            },
        }
    },
    new()
    {
        Name = ""Tesla"",
        ExpandedAriaLabel = ""Tesla Expanded"",
        CollapsedAriaLabel = ""Tesla Collapsed"",
        Tooltip = ""Tesla Car Models"",
        Links = new List<CarMenu>
        {
            new() { Name = ""Model S"", PageUrl = ""https://www.tesla.com/models"", UrlTarget = ""_blank"" },
            new() { Name = ""Model X"", PageUrl = ""https://www.tesla.com/modelx"", UrlTarget = ""_blank"" },
            new() { Name = ""Model Y"", PageUrl = ""https://www.tesla.com/modely"", UrlTarget = ""_blank"" },
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
            ChildItemsFieldSelector=""item => item.Childs""
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
            ChildItemsFieldSelector=""item => item.Childs""
            Mode=""BitNavMode.Manual""
            OnSelectItem=""(FoodMenu item) => CustomSelectedFoodName = FoodMenuDropDownItems.FirstOrDefault(i => i.Text == item.Name).Text"" />

    <BitDropDown @bind-Value=""CustomSelectedFoodName""
                    Label=""Select Item""
                    Items=""FoodMenuDropDownItems""
                    OnSelectItem=""(item) => CustomSelectedFood = Flatten(CustomFoodNavMenu).FirstOrDefault(i => i.Name == item.Value)"" />
</div>
";
    private static string example3CustomItemCSharpCode = @"
public class FoodMenu
{
    public string Name { get; set; } = string.Empty;
    public BitIconName Icon { get; set; }
    public bool IsExpanded { get; set; }
    public List<FoodMenu> Childs { get; set; } = new();
}

private static readonly List<FoodMenu> CustomFoodNavMenu = new()
{
    new()
    {
        Name = ""Fast-Food"",
        Icon = BitIconName.HeartBroken,
        IsExpanded = true,
        Childs = new List<FoodMenu>
        {
            new()
            {
                Name = ""Burgers"",
                Childs = new List<FoodMenu>
                {
                    new() { Name = ""Beef Burger"" },
                    new() { Name = ""Veggie Burger"" },
                    new() { Name = ""Bison Burger"" },
                    new() { Name = ""Wild Salmon Burger"" },
                }
            },
            new()
            {
                Name = ""Pizzas"",
                Childs = new List<FoodMenu>
                {
                    new() { Name = ""Cheese Pizza"" },
                    new() { Name = ""Veggie Pizza"" },
                    new() { Name = ""Pepperoni Pizza"" },
                    new() { Name = ""Meat Pizza"" },
                }
            },
            new() { Name = ""French Fries"" },
        }
    },
    new()
    {
        Name = ""Fruits"",
        Icon = BitIconName.Health,
        Childs = new List<FoodMenu>
        {
            new() { Name = ""Aplle"" },
            new() { Name = ""Orange"" },
            new() { Name = ""Benana"" },
        }
    },
    new() { Name = ""Ice Cream"" },
    new() { Name = ""Cookie"" },
};

private static readonly List<BitDropDownItem> FoodMenuDropDownItems = new()
{
    new()
    {
        Text = ""Beef Burger"",
        Value = ""Beef Burger"",
    },
    new()
    {
        Text = ""Veggie Burger"",
        Value = ""Veggie Burger"",
    },
    new()
    {
        Text = ""Bison Burger"",
        Value = ""Bison Burger"",
    },
    new()
    {
        Text = ""Wild Salmon Burger"",
        Value = ""Wild Salmon Burger"",
    },
    new()
    {
        Text = ""Cheese Pizza"",
        Value = ""Cheese Pizza"",
    },
    new()
    {
        Text = ""Veggie Pizza"",
        Value = ""Veggie Pizza"",
    },
    new()
    {
        Text = ""Pepperoni Pizza"",
        Value = ""Pepperoni Pizza"",
    },
    new()
    {
        Text = ""Meat Pizza"",
        Value = ""Meat Pizza"",
    },
    new()
    {
        Text = ""French Fries"",
        Value = ""French Fries"",
    },
    new()
    {
        Text = ""Aplle"",
        Value = ""Aplle"",
    },
    new()
    {
        Text = ""Orange"",
        Value = ""Orange"",
    },
    new()
    {
        Text = ""Benana"",
        Value = ""Benana"",
    },
    new()
    {
        Text = ""Ice Cream"",
        Value = ""Ice Cream"",
    },
    new()
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
            ChildItemsField=""@nameof(CarMenu.Links)""
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
            ChildItemsFieldSelector=""item => item.Childs""
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

private static readonly List<CarMenu> CustomCarNavMenu = new()
{
    new()
    {
        Name = ""Mercedes-Benz"",
        ExpandedAriaLabel = ""Mercedes-Benz Expanded"",
        CollapsedAriaLabel = ""Mercedes-Benz Collapsed"",
        Tooltip = ""Mercedes-Benz Car Models"",
        IsExpandedParent = true,
        Links = new List<CarMenu>
        {
            new()
            {
                Name = ""SUVs"",
                Links = new List<CarMenu>
                {
                    new() { Name = ""GLA"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/gla/suv"", UrlTarget = ""_blank"" },
                    new() { Name = ""GLB"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/glb/suv"", UrlTarget = ""_blank"" },
                    new() { Name = ""GLC"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/glc/suv"", UrlTarget = ""_blank"" },
                }
            },
            new()
            {
                Name = ""Sedans & Wagons"",
                Links = new List<CarMenu>
                {
                    new() { Name = ""A Class"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/a-class/sedan"", UrlTarget = ""_blank"" },
                    new() { Name = ""C Class"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/c-class/sedan"", UrlTarget = ""_blank"" },
                    new() { Name = ""E Class"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/e-class/sedan"", UrlTarget = ""_blank"" },
                }
            },
            new()
            {
                Name = ""Coupes"",
                Links = new List<CarMenu>
                {
                    new() { Name = ""CLA Coupe"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/cla/coupe"", UrlTarget = ""_blank"" },
                    new() { Name = ""C Class Coupe"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/c-class/coupe"", UrlTarget = ""_blank"" },
                    new() { Name = ""E Class Coupe"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/e-class/coupe"", UrlTarget = ""_blank"" },
                }
            },
        }
    },
    new()
    {
        Name = ""Tesla"",
        ExpandedAriaLabel = ""Tesla Expanded"",
        CollapsedAriaLabel = ""Tesla Collapsed"",
        Tooltip = ""Tesla Car Models"",
        Links = new List<CarMenu>
        {
            new() { Name = ""Model S"", PageUrl = ""https://www.tesla.com/models"", UrlTarget = ""_blank"" },
            new() { Name = ""Model X"", PageUrl = ""https://www.tesla.com/modelx"", UrlTarget = ""_blank"" },
            new() { Name = ""Model Y"", PageUrl = ""https://www.tesla.com/modely"", UrlTarget = ""_blank"" },
        }
    },
};

private static readonly List<FoodMenu> CustomFoodNavMenu = new()
{
    new()
    {
        Name = ""Fast-Food"",
        Icon = BitIconName.HeartBroken,
        IsExpanded = true,
        Childs = new List<FoodMenu>
        {
            new()
            {
                Name = ""Burgers"",
                Childs = new List<FoodMenu>
                {
                    new() { Name = ""Beef Burger"" },
                    new() { Name = ""Veggie Burger"" },
                    new() { Name = ""Bison Burger"" },
                    new() { Name = ""Wild Salmon Burger"" },
                }
            },
            new()
            {
                Name = ""Pizzas"",
                Childs = new List<FoodMenu>
                {
                    new() { Name = ""Cheese Pizza"" },
                    new() { Name = ""Veggie Pizza"" },
                    new() { Name = ""Pepperoni Pizza"" },
                    new() { Name = ""Meat Pizza"" },
                }
            },
            new() { Name = ""French Fries"" },
        }
    },
    new()
    {
        Name = ""Fruits"",
        Icon = BitIconName.Health,
        Childs = new List<FoodMenu>
        {
            new() { Name = ""Aplle"" },
            new() { Name = ""Orange"" },
            new() { Name = ""Benana"" },
        }
    },
    new() { Name = ""Ice Cream"" },
    new() { Name = ""Cookie"" },
};
";

    private static string example5CustomItemHTMLCode = @"
<BitNav Items=""CustomFoodNavMenu""
        TextFieldSelector=""item => item.Name""
        IconNameFieldSelector=""item => item.Icon""
        IsExpandedFieldSelector=""item => item.IsExpanded""
        ChildItemsFieldSelector=""item => item.Childs""
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
public class FoodMenu
{
    public string Name { get; set; } = string.Empty;
    public BitIconName Icon { get; set; }
    public bool IsExpanded { get; set; }
    public List<FoodMenu> Childs { get; set; } = new();
}

private static readonly List<FoodMenu> CustomFoodNavMenu = new()
{
    new()
    {
        Name = ""Fast-Food"",
        Icon = BitIconName.HeartBroken,
        IsExpanded = true,
        Childs = new List<FoodMenu>
        {
            new()
            {
                Name = ""Burgers"",
                Childs = new List<FoodMenu>
                {
                    new() { Name = ""Beef Burger"" },
                    new() { Name = ""Veggie Burger"" },
                    new() { Name = ""Bison Burger"" },
                    new() { Name = ""Wild Salmon Burger"" },
                }
            },
            new()
            {
                Name = ""Pizzas"",
                Childs = new List<FoodMenu>
                {
                    new() { Name = ""Cheese Pizza"" },
                    new() { Name = ""Veggie Pizza"" },
                    new() { Name = ""Pepperoni Pizza"" },
                    new() { Name = ""Meat Pizza"" },
                }
            },
            new() { Name = ""French Fries"" },
        }
    },
    new()
    {
        Name = ""Fruits"",
        Icon = BitIconName.Health,
        Childs = new List<FoodMenu>
        {
            new() { Name = ""Aplle"" },
            new() { Name = ""Orange"" },
            new() { Name = ""Benana"" },
        }
    },
    new() { Name = ""Ice Cream"" },
    new() { Name = ""Cookie"" },
};

private FoodMenu CustomClickedItem;
private FoodMenu CustomSelectedItem;
private FoodMenu CustomToggledItem;
";

    private static string example6CustomItemHTMLCode = @"
<div class=""example-box"">
    <BitNav Items=""CustomBitPlatformNavMenu""
            TextField=""@nameof(BitPlatformMenu.Text)""
            UrlField=""@nameof(BitPlatformMenu.Url)""
            IsEnabledField=""@nameof(BitPlatformMenu.IsEnabled)""
            ChildItemsField=""@nameof(BitPlatformMenu.Links)""
            ClassStyles=""@(new() { ItemContainer = new() { Style=""border:1px solid green;margin:2px"" },
                                    ToggleButton = new() { Style=""color:cyan"" },
                                    Item = new() { Style=""color:red"" } })"" />
</div>";
    private static string example6CustomItemCSharpCode = @"
public class BitPlatformMenu
{
    public string Text { get; set; } = string.Empty;
    public string Url { get; set; }
    public bool IsEnabled { get; set; } = true;
    public List<BitPlatformMenu> Links { get; set; } = new();
}

private static readonly List<BitPlatformMenu> CustomBitPlatformNavMenu = new()
{
    new()
    {
        Text = ""Bit Platform"",
        Links = new List<BitPlatformMenu>
        {
            new() { Text = ""Home"", Url = ""https://bitplatform.dev/"" },
            new()
            {
                Text = ""Products & Services"",
                Links = new List<BitPlatformMenu>
                {
                    new()
                    {
                        Text = ""Project Templates"",
                        Links = new List<BitPlatformMenu>
                        {
                            new() { Text = ""TodoTemplate"", Url = ""https://bitplatform.dev/todo-template/overview"" },
                            new() { Text = ""AdminPanel"", Url = ""https://bitplatform.dev/admin-panel/overview"" },
                        }
                    },
                    new() { Text = ""BlazorUI"", Url = ""https://bitplatform.dev/components"" },
                    new() { Text = ""Cloud hosting solutions"", Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                    new() { Text = ""Bit academy"", Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                }
            },
            new() { Text = ""Pricing"", Url = ""https://bitplatform.dev/pricing"" },
            new() { Text = ""About"", Url = ""https://bitplatform.dev/about-us"" },
            new() { Text = ""Contact us"", Url = ""https://bitplatform.dev/contact-us"" },
        },
    },
    new()
    {
        Text = ""Community"",
        Links = new List<BitPlatformMenu>
        {
            new() { Text = ""Linkedin"", Url = ""https://www.linkedin.com/company/bitplatformhq/about/"" },
            new() { Text = ""Twitter"", Url = ""https://twitter.com/bitplatformhq"" },
            new() { Text = ""Github repo"", Url = ""https://github.com/bitfoundation/bitplatform"" },
        }
    },
    new() { Text = ""Iconography"", Url = ""/icons"" },
};
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

    private static string example6NavOptionHTMLCode = @"
<div class=""example-box"">
    <BitNav TItem=""BitNavOption""
            ClassStyles=""@(new() { ItemContainer = new() { Style=""border:1px solid green;margin:2px"" },
                                    ToggleButton = new() { Style=""color:cyan"" },
                                    Item = new() { Style=""color:red"" } })"">
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
</div>";
}

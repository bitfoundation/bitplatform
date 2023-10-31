namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Nav;

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
            Type = "Expression<Func<TItem, BitNavAriaCurrent>>?",
            Href = "#nav-item-aria-current",
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
            Type = "Expression<Func<TItem, string>>?",
            DefaultValue = "null",
            Description = "Aria label for the item. Ignored if collapseAriaLabel or expandAriaLabel is provided."
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Items to render as children.",
        },
        new()
        {
            Name = "ChildItemsField",
            Type = "string",
            DefaultValue = "ChildItems",
            Description = "A list of items to render as children of the current item."
        },
        new()
        {
            Name = "ChildItemsFieldSelector",
            Type = "Expression<Func<TItem, IList<TItem>>>?",
            DefaultValue = "null",
            Description = "A list of items to render as children of the current item."
        },
        new()
        {
            Name = "Classes",
            Type = "BitNavClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitNav component.",
            Href = "#nav-class-styles",
            LinkType = LinkType.Link
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
            Type = "Expression<Func<TItem, string>>?",
            DefaultValue = "null",
            Description = "Aria label when group is collapsed."
        },
        new()
        {
            Name = "DefaultSelectedItem",
            Type = "TItem?",
            DefaultValue = "null",
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
            Type = "Expression<Func<TItem, string>>?",
            DefaultValue = "null",
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
            Type = "Expression<Func<TItem, bool>>?",
            DefaultValue = "null",
            Description = "(Optional) By default, any link with onClick defined will render as a button. Set this property to true to override that behavior. (Links without onClick defined will render as anchors by default.)"
        },
        new()
        {
            Name = "HeaderTemplate",
            Type = "RenderFragment<TItem>?",
            DefaultValue = "null",
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
            Type = "Expression<Func<TItem, string>>?",
            DefaultValue = "null",
            Description = "Name of an icon to render next to the item button."
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
            DefaultValue = "null",
            Description = "Whether or not the item is disabled."
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
            DefaultValue = "null",
            Description = "Whether or not the group is in an expanded state."
        },
        new()
        {
            Name = "Items",
            Type = "IList<TItem>",
            DefaultValue = "new List<TItem>()",
            Href="#nav-item",
            LinkType = LinkType.Link,
            Description = "A collection of item to display in the navigation bar."
        },
        new()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<TItem>?",
            DefaultValue = "null",
            Description = "Used to customize how content inside the item is rendered."
        },
        new()
        {
            Name = "ItemTemplateRenderMode",
            Type = "BitNavItemTemplateRenderMode",
            DefaultValue = "BitNavItemTemplateRenderMode.Normal",
            Href = "#nav-itemtemplate-rendermode",
            LinkType = LinkType.Link,
            Description = "The render mode of the custom ItemTemplate."
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
            Type = "Expression<Func<TItem, string>>?",
            DefaultValue = "null",
            Description = "A unique value to use as a key or id of the item."
        },
        new()
        {
            Name = "Mode",
            Type = "BitNavMode",
            DefaultValue = "BitNavMode.Automatic",
            Href = "#nav-mode-enum",
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
            Href = "#nav-render-type-enum",
            LinkType = LinkType.Link,
            Description = "The way to render nav items."
        },
        new()
        {
            Name = "SelectedItem",
            Type = "TItem?",
            DefaultValue = "null",
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
            Type = "Expression<Func<TItem, string>>?",
            DefaultValue = "null",
            Description = "Custom style for the each item element."
        },
        new()
        {
            Name = "Styles",
            Type = "BitNavClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitNav component.",
            Href = "#nav-class-styles",
            LinkType = LinkType.Link
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
            Type = "Expression<Func<TItem, string>>?",
            DefaultValue = "null",
            Description = "Link target, specifies how to open the item link."
        },
        new()
        {
            Name = "TemplateField",
            Type = "string",
            DefaultValue = "Template",
            Description = "The field name of the Template property in the nav item class."
        },
        new()
        {
            Name = "TemplateFieldSelector",
            Type = "Expression<Func<TItem, RenderFragment>>?",
            DefaultValue = "null",
            Description = "The field selector of the Template property in the nav item class."
        },
        new()
        {
            Name = "TemplateRenderModeField",
            Type = "string",
            DefaultValue = "TemplateRenderMode",
            Description = "The field name of the TemplateRenderMode property in the nav item class."
        },
        new()
        {
            Name = "TemplateRenderModeFieldSelector",
            Type = "Expression<Func<TItem, BitNavItemTemplateRenderMode>>?",
            DefaultValue = "null",
            Description = "The field selector of the TemplateRenderMode property in the nav item class."
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
            Type = "Expression<Func<TItem, string>>?",
            DefaultValue = "null",
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
            Type = "Expression<Func<TItem, string>>?",
            DefaultValue = "null",
            Description = "Text for the item tooltip."
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
            Type = "Expression<Func<TItem, string>>?",
            DefaultValue = "null",
            Description = "URL to navigate for the item link."
        },
        new()
        {
            Name = "AdditionalUrlsField",
            Type = "string",
            DefaultValue = "Url",
            Description = "Alternative URLs to be considered when auto mode tries to detect the selected item by the current URL."
        },
        new()
        {
            Name = "AdditionalUrlsFieldSelector",
            Type = "Expression<Func<TItem, IEnumerable<string>>>?",
            DefaultValue = "null",
            Description = "Alternative URLs to be considered when auto mode tries to detect the selected item by the current URL."
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
                   DefaultValue = "null",
                   Description = "Aria label when items is collapsed and can be expanded.",
               },
               new()
               {
                   Name = "ExpandAriaLabel",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Aria label when group is collapsed and can be expanded.",
               },
               new()
               {
                   Name = "ForceAnchor",
                   Type = "bool",
                   DefaultValue = "false",
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
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Name of an icon to render next to this link button.",
               },
               new()
               {
                   Name = "IsExpanded",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Whether or not the link is in an expanded state.",
               },
               new()
               {
                   Name = "IsEnabled",
                   Type = "bool",
                   DefaultValue = "true",
                   Description = "Whether or not the link is enabled.",
               },
               new()
               {
                   Name = "Key",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "A unique value to use as a key or id of the item.",
               },
               new()
               {
                   Name = "Style",
                   Type = "string?",
                   DefaultValue = "null",
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
                   DefaultValue = "null",
                   Description = "Text for title tooltip.",
               },
               new()
               {
                   Name = "Target",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Link target, specifies how to open the link.",
               },
               new()
               {
                   Name = "Template",
                   Type = "RenderFragment<BitNavItem>?",
                   DefaultValue = "null",
                   Description = "The custom template for the BitNavItem to render",
               },
               new()
               {
                   Name = "TemplateRenderMode",
                   Type = "BitNavItemTemplateRenderMode",
                   DefaultValue = "BitNavItemTemplateRenderMode.Normal",
                   Description = "The render mode of the BitNavItem's custom template.",
               },
               new()
               {
                   Name = "Url",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "URL to navigate to for this link.",
               }
            }
        },
        new()
        {
            Id = "nav-option",
            Title = "BitNavOption",
            Parameters = new()
            {
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
                   DefaultValue = "null",
                   Description = "Aria label when items is collapsed and can be expanded.",
               },
               new()
               {
                   Name = "ExpandAriaLabel",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Aria label when group is collapsed and can be expanded.",
               },
               new()
               {
                   Name = "ForceAnchor",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "(Optional) By default, any link with onClick defined will render as a button. Set this property to true to override that behavior. (Links without onClick defined will render as anchors by default.)",
               },
               new()
               {
                   Name = "IconName",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Name of an icon to render next to this link button.",
               },
               new()
               {
                   Name = "IsExpanded",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Whether or not the link is in an expanded state.",
               },
               new()
               {
                   Name = "Key",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "A unique value to use as a key or id of the item.",
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
                   DefaultValue = "null",
                   Description = "Text for title tooltip.",
               },
               new()
               {
                   Name = "Target",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Link target, specifies how to open the link.",
               },
               new()
               {
                   Name = "Url",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "URL to navigate to for this link.",
               }
            }
        },
        new()
        {
            Id = "nav-class-styles",
            Title = "BitNavClassStyles",
            Parameters = new()
            {
               new()
               {
                   Name = "Root",
                   Type = "BitClassStylePair?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitNav."
               },
               new()
               {
                   Name = "Item",
                   Type = "BitClassStylePair?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the item of the BitNav."
               },
               new()
               {
                   Name = "SelectedItem",
                   Type = "BitClassStylePair?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the selected item of the BitNav."
               },
               new()
               {
                   Name = "ItemContainer",
                   Type = "BitClassStylePair?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the item container of the BitNav."
               },
               new()
               {
                   Name = "ItemIcon",
                   Type = "BitClassStylePair?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the item icon of the BitNav."
               },
               new()
               {
                   Name = "SelectedItemContainer",
                   Type = "BitClassStylePair?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the selected item container of the BitNav."
               },
               new()
               {
                   Name = "ToggleButton",
                   Type = "BitClassStylePair?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the toggle button of the BitNav."
               },
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
        new()
        {
            Id = "nav-itemtemplate-rendermode",
            Name = "BitNavItemTemplateRenderMode",
            Items = new()
            {
                new()
                {
                    Name = "Normal",
                    Description = "Renders the template inside the button/anchor root element of the item.",
                    Value = "0",
                },
                new()
                {
                    Name = "Replace",
                    Description = "Replaces the button/anchor root element of the item.",
                    Value = "1",
                }
            }
        },
    };



    private readonly string example1NavItemRazorCode = @"
<BitNav Items=""BitPlatformNavMenu"" />";
    private readonly string example1NavItemCsharpCode = @"
private static readonly List<BitNavItem> BitPlatformNavMenu = new()
{
    new()
    {
        Text = ""bit platform"",
        ChildItems = new()
        {
            new() { Text = ""Home"", IconName = BitIconName.Home, Url = ""https://bitplatform.dev/"" },
            new()
            {
                Text = ""Products & Services"",
                ChildItems = new()
                {
                    new()
                    {
                        Text = ""Project Templates"",
                        ChildItems = new()
                        {
                            new() { Text = ""TodoTemplate"", IconName = BitIconName.ToDoLogoOutline, Url = ""https://bitplatform.dev/templates/overview"" },
                            new() { Text = ""AdminPanel"", IconName = BitIconName.LocalAdmin, Url = ""https://bitplatform.dev/templates/overview"" },
                        }
                    },
                    new() { Text = ""BlazorUI"", IconName = BitIconName.F12DevTools, Url = ""https://bitplatform.dev/components"" },
                    new() { Text = ""Cloud hosting solutions"", IconName = BitIconName.Cloud, Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                    new() { Text = ""Bit academy"", IconName = BitIconName.LearningTools, Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                }
            },
            new() { Text = ""Pricing"", IconName = BitIconName.Money, Url = ""https://bitplatform.dev/pricing"" },
            new() { Text = ""About"", IconName = BitIconName.Info, Url = ""https://bitplatform.dev/about-us"" },
            new() { Text = ""Contact us"", IconName = BitIconName.Contact, Url = ""https://bitplatform.dev/contact-us"" },
        },
    },
    new()
    {
        Text = ""Community"",
        ChildItems = new()
        {
            new() { Text = ""LinkedIn"", IconName = BitIconName.LinkedInLogo, Url = ""https://www.linkedin.com/company/bitplatformhq"" },
            new() { Text = ""Twitter"", IconName = BitIconName.Globe, Url = ""https://twitter.com/bitplatformhq"" },
            new() { Text = ""GitHub repo"", IconName = BitIconName.GitGraph, Url = ""https://github.com/bitfoundation/bitplatform"" },
        }
    },
    new() { Text = ""Iconography"", IconName = BitIconName.AppIconDefault, Url = ""/iconography"" },
};";

    private readonly string example2NavItemRazorCode = @"
<BitNav Items=""CarNavMenu"" RenderType=""BitNavRenderType.Grouped"" />";
    private readonly string example2NavItemCsharpCode = @"
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
};";

    private readonly string example3NavItemRazorCode = @"
<BitLabel>Basic</BitLabel>
<BitNav Items=""FoodNavMenu""
        DefaultSelectedItem=""FoodNavMenu[0].Items[2]""
        Mode=""BitNavMode.Manual"" />

<BitLabel>Two-Way Bind</BitLabel>
<BitNav @bind-SelectedItem=""SelectedItemNav""
        Items=""FoodNavMenu""
        Mode=""BitNavMode.Manual""
        OnSelectItem=""(BitNavItem item) => SelectedItemText = FoodMenuDropdownItems.FirstOrDefault(i => i.Text == item.Text).Text"" />

<BitDropdown @bind-Value=""SelectedItemText""
                Label=""Select Item""
                Items=""FoodMenuDropdownItems""
                OnSelectItem=""(item) => SelectedItemNav = Flatten(FoodNavMenu).FirstOrDefault(i => i.Text == item.Value)"" />";
    private readonly string example3NavItemCsharpCode = @"
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

private static readonly List<BitDropdownItem> FoodMenuDropdownItems = new()
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
private string SelectedItemText = FoodNavMenu[0].Items[2].Text;";

    private readonly string example4NavItemRazorCode = @"
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

<BitLabel>Header Template (in Grouped mode)</BitLabel>
<BitNav Items=""CarNavMenu"" RenderType=""BitNavRenderType.Grouped"">
    <HeaderTemplate Context=""item"">
        <div class=""nav-custom-header"">
            <BitIcon IconName=""@BitIconName.FavoriteStarFill"" />
            <span>@item.Text</span>
        </div>
    </HeaderTemplate>
</BitNav>

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
</BitNav>";
    private readonly string example4NavItemCsharpCode = @"
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
};";

    private readonly string example5NavItemRazorCode = @"
<BitNav Items=""FoodNavMenu""
        DefaultSelectedItem=""FoodNavMenu[0].Items[2]""
        Mode=""BitNavMode.Manual""
        OnItemClick=""(BitNavItem item) => ClickedItem = item""
        OnSelectItem=""(BitNavItem item) => SelectedItem = item""
        OnItemToggle=""(BitNavItem item) => ToggledItem = item"" />

<span>Clicked Item: @ClickedItem?.Text</span>
<span>Selected Item: @SelectedItem?.Text</span>
<span>Toggled Item: @(ToggledItem is null ? ""N/A"" : $""{ToggledItem.Text} ({(ToggledItem.IsExpanded ? ""Expanded"" : ""Collapsed"")})"")</span>";
    private readonly string example5NavItemCsharpCode = @"
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
private BitNavItem ToggledItem;";

    private readonly string example6NavItemRazorCode = @"
<BitNav Items=""BitPlatformNavMenu""
        Styles=""@(new() { ItemContainer = ""border: 1px solid green; margin: 2px;"",
                          ToggleButton = ""color: cyan;"",
                          Item = ""color: red;"",
                          ItemIcon = ""color: gold; margin-right: 15px;"" })"" />";
    private readonly string example6NavItemCsharpCode = @"
private static readonly List<BitNavItem> BitPlatformNavMenu = new()
{
    new()
    {
        Text = ""bit platform"",
        ChildItems = new()
        {
            new() { Text = ""Home"", IconName = BitIconName.Home, Url = ""https://bitplatform.dev/"" },
            new()
            {
                Text = ""Products & Services"",
                ChildItems = new()
                {
                    new()
                    {
                        Text = ""Project Templates"",
                        ChildItems = new()
                        {
                            new() { Text = ""TodoTemplate"", IconName = BitIconName.ToDoLogoOutline, Url = ""https://bitplatform.dev/templates/overview"" },
                            new() { Text = ""AdminPanel"", IconName = BitIconName.LocalAdmin, Url = ""https://bitplatform.dev/templates/overview"" },
                        }
                    },
                    new() { Text = ""BlazorUI"", IconName = BitIconName.F12DevTools, Url = ""https://bitplatform.dev/components"" },
                    new() { Text = ""Cloud hosting solutions"", IconName = BitIconName.Cloud, Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                    new() { Text = ""Bit academy"", IconName = BitIconName.LearningTools, Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                }
            },
            new() { Text = ""Pricing"", IconName = BitIconName.Money, Url = ""https://bitplatform.dev/pricing"" },
            new() { Text = ""About"", IconName = BitIconName.Info, Url = ""https://bitplatform.dev/about-us"" },
            new() { Text = ""Contact us"", IconName = BitIconName.Contact, Url = ""https://bitplatform.dev/contact-us"" },
        },
    },
    new()
    {
        Text = ""Community"",
        ChildItems = new()
        {
            new() { Text = ""LinkedIn"", IconName = BitIconName.LinkedInLogo, Url = ""https://www.linkedin.com/company/bitplatformhq"" },
            new() { Text = ""Twitter"", IconName = BitIconName.Globe, Url = ""https://twitter.com/bitplatformhq"" },
            new() { Text = ""GitHub repo"", IconName = BitIconName.GitGraph, Url = ""https://github.com/bitfoundation/bitplatform"" },
        }
    },
    new() { Text = ""Iconography"", IconName = BitIconName.AppIconDefault, Url = ""/iconography"" },
};";



    private readonly string example1CustomItemRazorCode = @"
<BitNav Items=""CustomBitPlatformNavMenu""
        TextField=""@nameof(BitPlatformMenu.Text)""
        UrlField=""@nameof(BitPlatformMenu.Url)""
        IconNameField=""@nameof(BitPlatformMenu.Icon)""
        IsEnabledField=""@nameof(BitPlatformMenu.IsEnabled)""
        ChildItemsField=""@nameof(BitPlatformMenu.Links)"" />";
    private readonly string example1CustomItemCsharpCode = @"
public class BitPlatformMenu
{
    public string Text { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Url { get; set; }
    public bool IsEnabled { get; set; } = true;
    public List<BitPlatformMenu> Links { get; set; } = new();
}

private static readonly List<BitPlatformMenu> CustomBitPlatformNavMenu = new()
{
    new()
    {
        Text = ""bit platform"",
        Links = new()
        {
            new() { Text = ""Home"", Icon = BitIconName.Home, Url = ""https://bitplatform.dev/"" },
            new()
            {
                Text = ""Products & Services"",
                Links = new()
                {
                    new()
                    {
                        Text = ""Project Templates"",
                        Links = new()
                        {
                            new() { Text = ""TodoTemplate"", Icon = BitIconName.ToDoLogoOutline, Url = ""https://bitplatform.dev/templates/overview"" },
                            new() { Text = ""AdminPanel"", Icon = BitIconName.LocalAdmin, Url = ""https://bitplatform.dev/templates/overview"" },
                        }
                    },
                    new() { Text = ""BlazorUI"", Icon = BitIconName.F12DevTools, Url = ""https://bitplatform.dev/components"" },
                    new() { Text = ""Cloud hosting solutions"", Icon = BitIconName.Cloud, Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                    new() { Text = ""Bit academy"", Icon = BitIconName.LearningTools, Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                }
            },
            new() { Text = ""Pricing"", Icon = BitIconName.Money, Url = ""https://bitplatform.dev/pricing"" },
            new() { Text = ""About"", Icon = BitIconName.Info, Url = ""https://bitplatform.dev/about-us"" },
            new() { Text = ""Contact us"", Icon = BitIconName.Contact, Url = ""https://bitplatform.dev/contact-us"" },
        },
    },
    new()
    {
        Text = ""Community"",
        Links = new()
        {
            new() { Text = ""Linkedin"", Icon = BitIconName.LinkedInLogo, Url = ""https://www.linkedin.com/company/bitplatformhq"" },
            new() { Text = ""Twitter"", Icon = BitIconName.Globe, Url = ""https://twitter.com/bitplatformhq"" },
            new() { Text = ""Github repo"", Icon = BitIconName.GitGraph, Url = ""https://github.com/bitfoundation/bitplatform"" },
        }
    },
    new() { Text = ""Iconography"", Icon = BitIconName.AppIconDefault, Url = ""/iconography"" },
};";

    private readonly string example2CustomItemRazorCode = @"
<BitNav Items=""CustomCarNavMenu""
        TextField=""@nameof(CarMenu.Name)""
        UrlField=""@nameof(CarMenu.PageUrl)""
        TargetField=""@nameof(CarMenu.UrlTarget)""
        TitleField=""@nameof(CarMenu.Tooltip)""
        IsExpandedField=""@nameof(CarMenu.IsExpandedParent)""
        CollapseAriaLabelField=""@nameof(CarMenu.CollapsedAriaLabel)""
        ExpandAriaLabelField=""@nameof(CarMenu.ExpandedAriaLabel)""
        ChildItemsField=""@nameof(CarMenu.Links)""
        RenderType=""BitNavRenderType.Grouped"" />";
    private readonly string example2CustomItemCsharpCode = @"
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
};";

    private readonly string example3CustomItemRazorCode = @"
<BitLabel>Basic</BitLabel>
<BitNav Items=""CustomFoodNavMenu""
        TextFieldSelector=""item => item.Name""
        IconNameFieldSelector=""item => item.Icon""
        IsExpandedFieldSelector=""item => item.IsExpanded""
        ChildItemsFieldSelector=""item => item.Childs""
        DefaultSelectedItem=""CustomFoodNavMenu[0].Childs[2]""
        Mode=""BitNavMode.Manual"" />

<BitLabel>Two-Way Bind</BitLabel>
<BitNav @bind-SelectedItem=""CustomSelectedFood""
        Items=""CustomFoodNavMenu""
        TextFieldSelector=""item => item.Name""
        IconNameFieldSelector=""item => item.Icon""
        IsExpandedFieldSelector=""item => item.IsExpanded""
        ChildItemsFieldSelector=""item => item.Childs""
        Mode=""BitNavMode.Manual""
        OnSelectItem=""(FoodMenu item) => CustomSelectedFoodName = FoodMenuDropdownItems.FirstOrDefault(i => i.Text == item.Name).Text"" />

<BitDropdown @bind-Value=""CustomSelectedFoodName""
             Label=""Select Item""
             Items=""FoodMenuDropdownItems""
             OnSelectItem=""(item) => CustomSelectedFood = Flatten(CustomFoodNavMenu).FirstOrDefault(i => i.Name == item.Value)"" />";
    private readonly string example3CustomItemCsharpCode = @"
public class FoodMenu
{
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; }
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

private static readonly List<BitDropdownItem> FoodMenuDropdownItems = new()
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
private string CustomSelectedFoodName = CustomFoodNavMenu[0].Childs[2].Name;";

    private readonly string example4CustomItemRazorCode = @"
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
            <BitIcon IconName=""@BitIconName.FavoriteStarFill"" />
            <span>@item.Name</span>
        </div>
    </HeaderTemplate>
</BitNav>

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
</BitNav>";
    private readonly string example4CustomItemCsharpCode = @"
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
    public string Icon { get; set; }
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
};";

    private readonly string example5CustomItemRazorCode = @"
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

<span>Clicked Item: @CustomClickedItem?.Name</span>
<span>Selected Item: @CustomSelectedItem?.Name</span>
<span>Toggled Item: @(CustomToggledItem is null ? ""N/A"" : $""{CustomToggledItem.Name} ({(CustomToggledItem.IsExpanded ? ""Expanded"" : ""Collapsed"")})"")</span>";
    private readonly string example5CustomItemCsharpCode = @"
public class FoodMenu
{
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; }
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
private FoodMenu CustomToggledItem;";

    private readonly string example6CustomItemRazorCode = @"
<BitNav Items=""CustomBitPlatformNavMenu""
        TextField=""@nameof(BitPlatformMenu.Text)""
        UrlField=""@nameof(BitPlatformMenu.Url)""
        IconNameField=""@nameof(BitPlatformMenu.Icon)""
        IsEnabledField=""@nameof(BitPlatformMenu.IsEnabled)""
        ChildItemsField=""@nameof(BitPlatformMenu.Links)""
        DescriptionField=""@nameof(BitPlatformMenu.Comment)""
        Styles=""@(new() { ItemContainer = ""border: 1px solid green; margin: 2px;"",
                          ToggleButton = ""color: cyan;"",
                          Item = ""color: red;"",
                          ItemIcon = ""color: gold; margin-right: 15px;"" })"" />";
    private readonly string example6CustomItemCsharpCode = @"
public class BitPlatformMenu
{
    public string Text { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Url { get; set; }
    public bool IsEnabled { get; set; } = true;
    public List<BitPlatformMenu> Links { get; set; } = new();
}

private static readonly List<BitPlatformMenu> CustomBitPlatformNavMenu = new()
{
    new()
    {
        Text = ""bit platform"",
        Links = new()
        {
            new() { Text = ""Home"", Icon = BitIconName.Home, Url = ""https://bitplatform.dev/"" },
            new()
            {
                Text = ""Products & Services"",
                Links = new()
                {
                    new()
                    {
                        Text = ""Project Templates"",
                        Links = new()
                        {
                            new() { Text = ""TodoTemplate"", Icon = BitIconName.ToDoLogoOutline, Url = ""https://bitplatform.dev/templates/overview"" },
                            new() { Text = ""AdminPanel"", Icon = BitIconName.LocalAdmin, Url = ""https://bitplatform.dev/templates/overview"" },
                        }
                    },
                    new() { Text = ""BlazorUI"", Icon = BitIconName.F12DevTools, Url = ""https://bitplatform.dev/components"" },
                    new() { Text = ""Cloud hosting solutions"", Icon = BitIconName.Cloud, Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                    new() { Text = ""Bit academy"", Icon = BitIconName.LearningTools, Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                }
            },
            new() { Text = ""Pricing"", Icon = BitIconName.Money, Url = ""https://bitplatform.dev/pricing"" },
            new() { Text = ""About"", Icon = BitIconName.Info, Url = ""https://bitplatform.dev/about-us"" },
            new() { Text = ""Contact us"", Icon = BitIconName.Contact, Url = ""https://bitplatform.dev/contact-us"" },
        },
    },
    new()
    {
        Text = ""Community"",
        Links = new()
        {
            new() { Text = ""Linkedin"", Icon = BitIconName.LinkedInLogo, Url = ""https://www.linkedin.com/company/bitplatformhq"" },
            new() { Text = ""Twitter"", Icon = BitIconName.Globe, Url = ""https://twitter.com/bitplatformhq"" },
            new() { Text = ""Github repo"", Icon = BitIconName.GitGraph, Url = ""https://github.com/bitfoundation/bitplatform"" },
        }
    },
    new() { Text = ""Iconography"", Icon = BitIconName.AppIconDefault, Url = ""/iconography"" },
};";



    private readonly string example1NavOptionRazorCode = @"
<BitNav TItem=""BitNavOption"">
    <BitNavOption Text=""bit platform""
                    ExpandAriaLabel=""bit platform Expanded""
                    CollapseAriaLabel=""bit platform Collapsed"">
        <BitNavOption Text=""Home"" IconName=""@BitIconName.Home"" Url=""https://bitplatform.dev/"" Target=""_blank"" />
        <BitNavOption Text=""Products & Services"">
            <BitNavOption Text=""Project Templates"">
                <BitNavOption IconName=""@BitIconName.ToDoLogoOutline"" Text=""TodoTemplate"" Url=""https://bitplatform.dev/templates/overview"" Target=""_blank"" />
                <BitNavOption IconName=""@BitIconName.LocalAdmin"" Text=""AdminPanel"" Url=""https://bitplatform.dev/templates/overview"" Target=""_blank"" />
            </BitNavOption>
            <BitNavOption Text=""BlazorUI"" IconName=""@BitIconName.F12DevTools"" Url=""https://bitplatform.dev/components"" Target=""_blank"" />
            <BitNavOption Text=""Cloud hosting solutions"" IconName=""@BitIconName.Cloud"" IsEnabled=""false"" />
            <BitNavOption Text=""Bit academy"" IconName=""@BitIconName.LearningTools"" IsEnabled=""false"" />
        </BitNavOption>
        <BitNavOption Text=""Pricing"" IconName=""@BitIconName.Money"" Url=""https://bitplatform.dev/pricing"" Target=""_blank"" />
        <BitNavOption Text=""About"" IconName=""@BitIconName.Info"" Url=""https://bitplatform.dev/about-us"" Target=""_blank"" />
        <BitNavOption Text=""Contact us"" IconName=""@BitIconName.Contact"" Url=""https://bitplatform.dev/contact-us"" Target=""_blank"" />
    </BitNavOption>

    <BitNavOption Text=""Community""
                    ExpandAriaLabel=""Community Expanded""
                    CollapseAriaLabel=""Community Collapsed"">
        <BitNavOption Text=""Linkedin"" IconName=""@BitIconName.LinkedInLogo"" Url=""https://www.linkedin.com/company/bitplatformhq"" Target=""_blank"" />
        <BitNavOption Text=""Twitter"" IconName=""@BitIconName.Globe"" Url=""https://twitter.com/bitplatformhq"" Target=""_blank"" />
        <BitNavOption Text=""Github repo"" IconName=""@BitIconName.GitGraph"" Url=""https://github.com/bitfoundation/bitplatform"" Target=""_blank"" />
    </BitNavOption>

    <BitNavOption Text=""Iconography"" IconName=""@BitIconName.AppIconDefault"" Url=""/iconography"" Target=""_blank"" />
</BitNav>";

    private readonly string example2NavOptionRazorCode = @"
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
</BitNav>";

    private readonly string example3NavOptionRazorCode = @"
<BitLabel>Basic</BitLabel>
<BitNav TItem=""BitNavOption""
        Mode=""BitNavMode.Manual"">
    <BitNavOption Text=""Fast-Foods""
                  IconName=""@BitIconName.HeartBroken"">
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

<BitLabel>Two-Way Bind</BitLabel>
<BitNav TItem=""BitNavOption""
        Mode=""BitNavMode.Manual"">
    <BitNavOption Text=""Fast-Foods""
                  IconName=""@BitIconName.HeartBroken""
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

<BitDropdown @bind-Value=""SelectedOptionKey""
             DefaultValue=""French Fries""
             Label=""Pick one""
             Items=""FoodMenuDropdownItems"" />";
    private readonly string example3NavOptionCsharpCode = @"
private string SelectedOptionKey;";

    private readonly string example4NavOptionRazorCode = @"
<BitLabel>Header Template (in Grouped mode)</BitLabel>
<BitNav TItem=""BitNavOption"" RenderType=""BitNavRenderType.Grouped"">
    <HeaderTemplate Context=""item"">
        <div class=""nav-custom-header"">
            <BitIcon IconName=""@BitIconName.FavoriteStarFill"" />
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
</BitNav>";

    private readonly string example5NavOptionRazorCode = @"
<BitNav Mode=""BitNavMode.Manual""
        OnItemClick=""(BitNavOption option) => ClickedOption = option""
        OnSelectItem=""(BitNavOption option) => SelectedOption = option""
        OnItemToggle=""(BitNavOption option) => ToggledOption = option"">
    <BitNavOption Text=""Fast-Foods""
                  IconName=""@BitIconName.HeartBroken""
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

<span>Clicked Option: @ClickedOption?.Text</span>
<span>Selected Option: @SelectedOption?.Text</span>
<span>Toggled Option: @(ToggledOption is null ? ""N/A"" : $""{ToggledOption.Text} ({(ToggledOption.IsExpanded ? ""Expanded"" : ""Collapsed"")})"")</span>";
    private readonly string example5NavOptionCsharpCode = @"
private BitNavOption ClickedOption;
private BitNavOption SelectedOption;
private BitNavOption ToggledOption;";

    private readonly string example6NavOptionRazorCode = @"
<BitNav TItem=""BitNavOption""
        Styles=""@(new() { ItemContainer = ""border: 1px solid green; margin: 2px;"",
                          ToggleButton = ""color: cyan;"",
                          Item = ""color: red;"",
                          ItemIcon = ""color: gold; margin-right: 15px;"" })"">
    <BitNavOption Text=""bit platform""
                  ExpandAriaLabel=""bit platform Expanded""
                  CollapseAriaLabel=""bit platform Collapsed"">
        <BitNavOption Text=""Home"" IconName=""@BitIconName.Home"" Url=""https://bitplatform.dev/"" Target=""_blank"" />
        <BitNavOption Text=""Products & Services"">
            <BitNavOption Text=""Project Templates"">
                <BitNavOption IconName=""@BitIconName.ToDoLogoTop"" Text=""TodoTemplate"" Url=""https://bitplatform.dev/templates/overview"" Target=""_blank"" />
                <BitNavOption IconName=""@BitIconName.Admin"" Text=""AdminPanel"" Url=""https://bitplatform.dev/templates/overview"" Target=""_blank"" />
            </BitNavOption>
            <BitNavOption Text=""BlazorUI"" IconName=""@BitIconName.F12DevTools"" Url=""https://bitplatform.dev/components"" Target=""_blank"" />
            <BitNavOption Text=""Cloud hosting solutions"" IconName=""@BitIconName.Cloud"" IsEnabled=""false"" />
            <BitNavOption Text=""Bit academy"" IconName=""@BitIconName.LearningTools"" IsEnabled=""false"" />
        </BitNavOption>
        <BitNavOption Text=""Pricing"" IconName=""@BitIconName.Money"" Url=""https://bitplatform.dev/pricing"" Target=""_blank"" />
        <BitNavOption Text=""About"" IconName=""@BitIconName.Info"" Url=""https://bitplatform.dev/about-us"" Target=""_blank"" />
        <BitNavOption Text=""Contact us"" IconName=""@BitIconName.Contact"" Url=""https://bitplatform.dev/contact-us"" Target=""_blank"" />
    </BitNavOption>

    <BitNavOption Text=""Community""
                  ExpandAriaLabel=""Community Expanded""
                  CollapseAriaLabel=""Community Collapsed"">
        <BitNavOption Text=""Linkedin"" IconName=""@BitIconName.LinkedInLogo"" Url=""https://www.linkedin.com/company/bitplatformhq"" Target=""_blank"" />
        <BitNavOption Text=""Twitter"" IconName=""@BitIconName.Globe"" Url=""https://twitter.com/bitplatformhq"" Target=""_blank"" />
        <BitNavOption Text=""Github repo"" IconName=""@BitIconName.GitGraph"" Url=""https://github.com/bitfoundation/bitplatform"" Target=""_blank"" />
    </BitNavOption>

    <BitNavOption Text=""Iconography"" IconName=""@BitIconName.AppIconDefault"" Url=""/iconography"" Target=""_blank"" />
</BitNav>";
}

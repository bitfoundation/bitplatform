namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Nav;

public partial class BitNavDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Items to render as children.",
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
            Name = "DefaultSelectedItem",
            Type = "TItem?",
            DefaultValue = "null",
            Description = "The initially selected item in manual mode."
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
            Description = "The render mode of the custom ItemTemplate.",
            LinkType = LinkType.Link,
            Href = "#nav-itemtemplate-rendermode",
        },
        new()
        {
            Name = "Mode",
            Type = "BitNavMode",
            DefaultValue = "BitNavMode.Automatic",
            Description = "Determines how the navigation will be handled.",
            LinkType = LinkType.Link,
            Href = "#nav-mode-enum",
        },
        new()
        {
            Name = "NameSelectors",
            Type = "BitNavNameSelectors<TItem>?",
            DefaultValue = "null",
            Description = "Names and selectors of the custom input type properties.",
            LinkType = LinkType.Link,
            Href = "#name-selectors",
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
            Description = "The way to render nav items.",
            LinkType = LinkType.Link,
            Href = "#nav-render-type-enum",
        },
        new()
        {
            Name = "ReversedChevron",
            Type = "bool",
            DefaultValue = "false",
            Description = "Reverses the location of the expander chevron."
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
            Name = "Styles",
            Type = "BitNavClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitNav component.",
            Href = "#nav-class-styles",
            LinkType = LinkType.Link
        }
    };

    private readonly List<ComponentSubClass> componentSubClasses = new()
    {
        new()
        {
            Id = "nav-item",
            Title = "BitNavItem",
            Parameters =
            [
               new()
               {
                   Name = "AriaCurrent",
                   Type = "BitNavAriaCurrent",
                   DefaultValue = "BitNavAriaCurrent.Page",
                   Description = " Aria-current token for active nav item. Must be a valid token value, and defaults to 'page'.",
                   Href = "#nav-aria-current-enum",
                   LinkType = LinkType.Link,
               },
               new()
               {
                   Name = "AriaLabel",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Aria label for nav item. Ignored if CollapseAriaLabel or ExpandAriaLabel is provided.",
               },
               new()
               {
                   Name = "Class",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS class for the nav item.",
               },
               new()
               {
                   Name = "ChildItems",
                   Type = "List<BitNavItem>",
                   DefaultValue = "[]",
                   Description = "A list of items to render as children of the current nav item.",
               },
               new()
               {
                   Name = "CollapseAriaLabel",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Aria label when nav item is collapsed and can be expanded.",
               },
               new()
               {
                   Name = "Data",
                   Type = "object?",
                   DefaultValue = "null",
                   Description = "The custom data for the nav item to provide additional state.",
               },
               new()
               {
                   Name = "Description",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The description for the nav item.",
               },
               new()
               {
                   Name = "ExpandAriaLabel",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Aria label when nav item is collapsed and can be expanded.",
               },
               new()
               {
                   Name = "ForceAnchor",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Forces an anchor element render instead of button.",
               },
               new()
               {
                   Name = "IconName",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Name of an icon to render next to the nav item.",
               },
               new()
               {
                   Name = "IsEnabled",
                   Type = "bool",
                   DefaultValue = "true",
                   Description = "Whether or not the nav item is enabled.",
               },
               new()
               {
                   Name = "IsExpanded",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Whether or not the nav item is in an expanded state.",
               },
               new()
               {
                   Name = "IsSeparator",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Indicates that the nav item should render as a separator.",
               },
               new()
               {
                   Name = "Key",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "A unique value to use as a key or id of the nav item.",
               },
               new()
               {
                   Name = "Style",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS style for the nav item.",
               },
               new()
               {
                   Name = "Target",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Link target, specifies how to open the nav item's link.",
               },
               new()
               {
                   Name = "Template",
                   Type = "RenderFragment<BitNavItem>?",
                   DefaultValue = "null",
                   Description = "The custom template for the nav item to render.",
               },
               new()
               {
                   Name = "TemplateRenderMode",
                   Type = "BitNavItemTemplateRenderMode",
                   DefaultValue = "BitNavItemTemplateRenderMode.Normal",
                   Description = "The render mode of the nav item's custom template.",
               },
               new()
               {
                   Name = "Text",
                   Type = "string",
                   DefaultValue = "string.Empty",
                   Description = "Text to render for the nav item.",
               },
               new()
               {
                   Name = "Title",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Text for the tooltip of the nav item.",
               },
               new()
               {
                   Name = "Url",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The nav item's link URL.",
               },
               new()
               {
                   Name = "AdditionalUrls",
                   Type = "IEnumerable<string>?",
                   DefaultValue = "null",
                   Description = "Alternative URLs to be considered when auto mode tries to detect the selected nav item by the current URL.",
               }
            ]
        },
        new()
        {
            Id = "nav-option",
            Title = "BitNavOption",
            Parameters =
            [
               new()
               {
                   Name = "AriaCurrent",
                   Type = "BitNavAriaCurrent",
                   DefaultValue = "BitNavAriaCurrent.Page",
                   Description = " Aria-current token for active nav option. Must be a valid token value, and defaults to 'page'.",
                   Href = "#nav-aria-current-enum",
                   LinkType = LinkType.Link,
               },
               new()
               {
                   Name = "AriaLabel",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Aria label for nav option. Ignored if CollapseAriaLabel or ExpandAriaLabel is provided.",
               },
               new()
               {
                   Name = "Class",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS class for the nav option.",
               },
               new()
               {
                   Name = "ChildContent",
                   Type = "RenderFragment?",
                   DefaultValue = "null",
                   Description = "A list of options to render as children of the current nav option.",
               },
               new()
               {
                   Name = "CollapseAriaLabel",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Aria label when nav option is collapsed and can be expanded.",
               },
               new()
               {
                   Name = "Data",
                   Type = "object?",
                   DefaultValue = "null",
                   Description = "The custom data for the nav option to provide additional state.",
               },
               new()
               {
                   Name = "Description",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The description for the nav option.",
               },
               new()
               {
                   Name = "ExpandAriaLabel",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Aria label when nav option is collapsed and can be expanded.",
               },
               new()
               {
                   Name = "ForceAnchor",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Forces an anchor element render instead of button.",
               },
               new()
               {
                   Name = "IconName",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Name of an icon to render next to the nav option.",
               },
               new()
               {
                   Name = "IsEnabled",
                   Type = "bool",
                   DefaultValue = "true",
                   Description = "Whether or not the nav option is enabled.",
               },
               new()
               {
                   Name = "IsExpanded",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Whether or not the nav option is in an expanded state.",
               },
               new()
               {
                   Name = "IsSeparator",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Indicates that the nav option should render as a separator.",
               },
               new()
               {
                   Name = "Key",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "A unique value to use as a key or id of the nav option.",
               },
               new()
               {
                   Name = "Style",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS style for the nav option.",
               },
               new()
               {
                   Name = "Target",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Link target, specifies how to open the nav option's link.",
               },
               new()
               {
                   Name = "Template",
                   Type = "RenderFragment<BitNavItem>?",
                   DefaultValue = "null",
                   Description = "The custom template for the nav option to render.",
               },
               new()
               {
                   Name = "TemplateRenderMode",
                   Type = "BitNavItemTemplateRenderMode",
                   DefaultValue = "BitNavItemTemplateRenderMode.Normal",
                   Description = "The render mode of the nav option's custom template.",
               },
               new()
               {
                   Name = "Text",
                   Type = "string",
                   DefaultValue = "string.Empty",
                   Description = "Text to render for the nav option.",
               },
               new()
               {
                   Name = "Title",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Text for the tooltip of the nav option.",
               },
               new()
               {
                   Name = "Url",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The nav option's link URL.",
               },
               new()
               {
                   Name = "AdditionalUrls",
                   Type = "IEnumerable<string>?",
                   DefaultValue = "null",
                   Description = "Alternative URLs to be considered when auto mode tries to detect the selected nav option by the current URL.",
               }
            ]
        },
        new()
        {
            Id = "name-selectors",
            Title = "BitNavNameSelectors<TItem>",
            Parameters =
            [
               new()
               {
                   Name = "AriaCurrent",
                   Type = "BitNameSelectorPair<TItem, BitNavAriaCurrent?>",
                   DefaultValue = "new(nameof(BitNavItem.AriaCurrent))",
                   Description = "The AriaCurrent field name and selector of the custom input class."
               },
               new()
               {
                   Name = "AriaLabel",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitNavItem.AriaLabel))",
                   Description = "The AriaLabel field name and selector of the custom input class."
               },
               new()
               {
                   Name = "Class",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitNavItem.Class))",
                   Description = "The Class field name and selector of the custom input class."
               },
               new()
               {
                   Name = "ChildItems",
                   Type = "BitNameSelectorPair<TItem, List<TItem>?>",
                   DefaultValue = "new(nameof(BitNavItem.ChildItems))",
                   Description = "The ChildItems field name and selector of the custom input class."
               },
               new()
               {
                   Name = "CollapseAriaLabel",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitNavItem.CollapseAriaLabel))",
                   Description = "The CollapseAriaLabel field name and selector of the custom input class."
               },
               new()
               {
                   Name = "Data",
                   Type = "BitNameSelectorPair<TItem, object?>",
                   DefaultValue = "new(nameof(BitNavItem.Data))",
                   Description = "The Data field name and selector of the custom input class."
               },
               new()
               {
                   Name = "Description",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitNavItem.Description))",
                   Description = "The Description field name and selector of the custom input class."
               },
               new()
               {
                   Name = "ExpandAriaLabel",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitNavItem.ExpandAriaLabel))",
                   Description = "The ExpandAriaLabel field name and selector of the custom input class."
               },
               new()
               {
                   Name = "ForceAnchor",
                   Type = "BitNameSelectorPair<TItem, bool?>",
                   DefaultValue = "new(nameof(BitNavItem.ForceAnchor))",
                   Description = "The ForceAnchor field name and selector of the custom input class."
               },
               new()
               {
                   Name = "IconName",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitNavItem.IconName))",
                   Description = "The IconName field name and selector of the custom input class."
               },
               new()
               {
                   Name = "IsEnabled",
                   Type = "BitNameSelectorPair<TItem, bool?>",
                   DefaultValue = "new(nameof(BitNavItem.IsEnabled))",
                   Description = "The IsEnabled field name and selector of the custom input class."
               },
               new()
               {
                   Name = "IsExpanded",
                   Type = "BitNameSelectorPair<TItem, bool?>",
                   DefaultValue = "new(nameof(BitNavItem.IsExpanded))",
                   Description = "The IsExpanded field name and selector of the custom input class."
               },
               new()
               {
                   Name = "IsSeparator",
                   Type = "BitNameSelectorPair<TItem, bool?>",
                   DefaultValue = "new(nameof(BitNavItem.IsSeparator))",
                   Description = "The IsSeparator field name and selector of the custom input class."
               },
               new()
               {
                   Name = "Key",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitNavItem.Key))",
                   Description = "The Key field name and selector of the custom input class."
               },
               new()
               {
                   Name = "Style",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitNavItem.Style))",
                   Description = "The Style field name and selector of the custom input class."
               },
               new()
               {
                   Name = "Target",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitNavItem.Target))",
                   Description = "The Target field name and selector of the custom input class."
               },
               new()
               {
                   Name = "TemplateRenderMode",
                   Type = "BitNameSelectorPair<TItem, BitNavItemTemplateRenderMode?>",
                   DefaultValue = "new(nameof(BitNavItem.TemplateRenderMode))",
                   Description = "The TemplateRenderMode field name and selector of the custom input class."
               },
               new()
               {
                   Name = "Text",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitNavItem.Text))",
                   Description = "The Text field name and selector of the custom input class."
               },
               new()
               {
                   Name = "Title",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitNavItem.Title))",
                   Description = "The Title field name and selector of the custom input class."
               },
               new()
               {
                   Name = "Url",
                   Type = "BitNameSelectorPair<TItem, string?>",
                   DefaultValue = "new(nameof(BitNavItem.Url))",
                   Description = "The Url field name and selector of the custom input class."
               },
               new()
               {
                   Name = "AdditionalUrls",
                   Type = "BitNameSelectorPair<TItem, IEnumerable<string>?>",
                   DefaultValue = "new(nameof(BitNavItem.AdditionalUrls))",
                   Description = "The AdditionalUrls field name and selector of the custom input class."
               },
            ]
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
                   Type = "String?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitNav."
               },
               new()
               {
                   Name = "Item",
                   Type = "String?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the item of the BitNav."
               },
               new()
               {
                   Name = "SelectedItem",
                   Type = "String?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the selected item of the BitNav."
               },
               new()
               {
                   Name = "ItemContainer",
                   Type = "String?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the item container of the BitNav."
               },
               new()
               {
                   Name = "ItemIcon",
                   Type = "String?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the item icon of the BitNav."
               },
               new()
               {
                   Name = "SelectedItemContainer",
                   Type = "String?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the selected item container of the BitNav."
               },
               new()
               {
                   Name = "ToggleButton",
                   Type = "String?",
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
            Items =
            [
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
            ]
        },
        new()
        {
            Id = "nav-render-type-enum",
            Name = "BitNavRenderType",
            Items =
            [
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
            ]
        },
        new()
        {
            Id = "nav-aria-current-enum",
            Name = "BitNavItemAriaCurrent",
            Items =
            [
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

            ]
        },
        new()
        {
            Id = "nav-itemtemplate-rendermode",
            Name = "BitNavItemTemplateRenderMode",
            Items =
            [
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
            ]
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
        Description = ""the bit platform description"",
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
                            new() { Text = ""Todo sample"", IconName = BitIconName.ToDoLogoOutline, Url = ""https://bitplatform.dev/templates/overview"" },
                            new() { Text = ""AdminPanel sample"", IconName = BitIconName.LocalAdmin, Url = ""https://bitplatform.dev/templates/overview"" },
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
            new() { Text = ""LinkedIn"", IconName = BitIconName.LinkedInLogo , Url = ""https://www.linkedin.com/company/bitplatformhq"" },
            new() { Text = ""Twitter"", IconName = BitIconName.Globe , Url = ""https://twitter.com/bitplatformhq"" },
            new() { Text = ""GitHub repo"", IconName = BitIconName.GitGraph , Url = ""https://github.com/bitfoundation/bitplatform"" },
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
                Text = ""Pizza"",
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
        <div class=""nav-custom-item"">
            <BitCheckbox />
            <BitIcon IconName=""@item.IconName"" />
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
                Text = ""Pizza"",
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
                Text = ""Pizza"",
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
                            new() { Text = ""Todo sample"", IconName = BitIconName.ToDoLogoOutline, Url = ""https://bitplatform.dev/templates/overview"" },
                            new() { Text = ""AdminPanel sample"", IconName = BitIconName.LocalAdmin, Url = ""https://bitplatform.dev/templates/overview"" },
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
        NameSelectors=""@(new() { IconName =  { Name = nameof(BitPlatformMenu.Icon) },
                                 ChildItems =  { Name = nameof(BitPlatformMenu.Links) },
                                 Description =  { Name = nameof(BitPlatformMenu.Comment) } })"" />";
    private readonly string example1CustomItemCsharpCode = @"
public class BitPlatformMenu
{
    public string Text { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Url { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsExpanded { get; set; }
    public List<BitPlatformMenu> Links { get; set; } = [];
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
                            new() { Text = ""Todo sample"", Icon = BitIconName.ToDoLogoOutline, Url = ""https://bitplatform.dev/templates/overview"" },
                            new() { Text = ""AdminPanel sample"", Icon = BitIconName.LocalAdmin, Url = ""https://bitplatform.dev/templates/overview"" },
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
        RenderType=""BitNavRenderType.Grouped""
        NameSelectors=""@(new() { Text =  { Name = nameof(CarMenu.Name) },
                                 Url =  { Name = nameof(CarMenu.PageUrl) },
                                 Target =  { Name = nameof(CarMenu.UrlTarget) },
                                 Title =  { Name = nameof(CarMenu.Tooltip) },
                                 IsExpanded =  { Name = nameof(CarMenu.IsExpandedParent) },
                                 CollapseAriaLabel =  { Name = nameof(CarMenu.CollapsedAriaLabel) },
                                 ExpandAriaLabel =  { Name = nameof(CarMenu.ExpandedAriaLabel) },
                                 ChildItems =  { Name = nameof(CarMenu.Links) },
                                 Description =  { Name = nameof(CarMenu.Comment) } })"" />";
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
    public List<CarMenu> Links { get; set; } = [];
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
<BitNav Items=""CustomFoodNavMenu""
        Mode=""BitNavMode.Manual""
        DefaultSelectedItem=""CustomFoodNavMenu[0].Childs[2]""
        NameSelectors=""@(new() { Text =  { Selector = item => item.Name },
                                 IconName =  { Selector = item => item.Icon },
                                 ChildItems =  { Selector = item => item.Childs },
                                 Description =  { Selector = item => item.Comment } })"" />


<BitNav @bind-SelectedItem=""CustomSelectedFood""
        Items=""CustomFoodNavMenu""
        Mode=""BitNavMode.Manual""
        DefaultSelectedItem=""CustomFoodNavMenu[0].Childs[2]""
        NameSelectors=""@(new() { Text =  { Selector = item => item.Name },
                                 IconName =  { Selector = item => item.Icon },
                                 ChildItems =  { Selector = item => item.Childs },
                                 Description =  { Selector = item => item.Comment } })""
        OnSelectItem=""(FoodMenu item) => CustomSelectedFoodName = FoodMenuDropdownItems.Single(i => i.Text == item.Name).Text"" />

<BitDropdown @bind-Value=""CustomSelectedFoodName""
             Label=""Select Item""
             Items=""FoodMenuDropdownItems""
             OnSelectItem=""(BitDropdownItem<string> item) => CustomSelectedFood = Flatten(CustomFoodNavMenu).Single(i => i.Name == item.Value)"" />";
    private readonly string example3CustomItemCsharpCode = @"
public class FoodMenu
{
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; }
    public bool IsExpanded { get; set; }
    public List<FoodMenu> Childs { get; set; } = [];
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
                Name = ""Pizza"",
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

<BitNav Items=""CustomCarNavMenu""
        RenderType=""BitNavRenderType.Grouped""
        NameSelectors=""@(new() { Text =  { Name = nameof(CarMenu.Name) },
                                 Url =  { Name = nameof(CarMenu.PageUrl) },
                                 Target =  { Name = nameof(CarMenu.UrlTarget) },
                                 Title =  { Name = nameof(CarMenu.Tooltip) },
                                 IsExpanded =  { Name = nameof(CarMenu.IsExpandedParent) },
                                 CollapseAriaLabel =  { Name = nameof(CarMenu.CollapsedAriaLabel) },
                                 ExpandAriaLabel =  { Name = nameof(CarMenu.ExpandedAriaLabel) },
                                 ChildItems =  { Name = nameof(CarMenu.Links) },
                                 Description =  { Name = nameof(CarMenu.Comment) } })"">
    <HeaderTemplate Context=""item"">
        <div class=""nav-custom-header"">
            <BitIcon IconName=""@BitIconName.FavoriteStarFill"" />
            <span>@item.Name</span>
        </div>
    </HeaderTemplate>
</BitNav>



<BitNav Items=""CustomFoodNavMenu""
        Mode=""BitNavMode.Manual""
        NameSelectors=""@(new() { Text =  { Selector = item => item.Name },
                                 IconName =  { Selector = item => item.Icon },
                                 ChildItems =  { Selector = item => item.Childs },
                                 Description =  { Selector = item => item.Comment } })"">
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
    public List<CarMenu> Links { get; set; } = [];
}

public class FoodMenu
{
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; }
    public bool IsExpanded { get; set; }
    public List<FoodMenu> Childs { get; set; } = [];
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
                Name = ""Pizza"",
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
        Mode=""BitNavMode.Manual""
        OnItemClick=""(FoodMenu item) => CustomClickedItem = item""
        OnSelectItem=""(FoodMenu item) => CustomSelectedItem = item""
        OnItemToggle=""(FoodMenu item) => CustomToggledItem = item""
        NameSelectors=""@(new() { Text =  { Selector = item => item.Name },
                                 IconName =  { Selector = item => item.Icon },
                                 ChildItems =  { Selector = item => item.Childs },
                                 Description =  { Selector = item => item.Comment } })"" />
<div>Clicked Item: @CustomClickedItem?.Name</div>
<div>Selected Item: @CustomSelectedItem?.Name</div>
<div>Toggled Item: @(CustomToggledItem is null ? ""N/A"" : $""{CustomToggledItem.Name} ({(CustomToggledItem.IsExpanded ? ""Expanded"" : ""Collapsed"")})"")</div>";
    private readonly string example5CustomItemCsharpCode = @"
public class FoodMenu
{
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; }
    public bool IsExpanded { get; set; }
    public List<FoodMenu> Childs { get; set; } = [];
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
                Name = ""Pizza"",
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
<BitNav Items=""CustomCustomStyleNavMenu""
        NameSelectors=""@(new() { IconName =  { Name = nameof(BitPlatformMenu.Icon) },
                                 ChildItems =  { Name = nameof(BitPlatformMenu.Links) },
                                 Description =  { Name = nameof(BitPlatformMenu.Comment) } })""
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
    public bool IsExpanded { get; set; }
    public List<BitPlatformMenu> Links { get; set; } = [];
}

private static readonly List<BitPlatformMenu> CustomCustomStyleNavMenu = new()
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
                            new() { Text = ""Todo sample"", Icon = BitIconName.ToDoLogoOutline, Url = ""https://bitplatform.dev/templates/overview"" },
                            new() { Text = ""AdminPanel sample"", Icon = BitIconName.LocalAdmin, Url = ""https://bitplatform.dev/templates/overview"" },
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
                <BitNavOption IconName=""@BitIconName.ToDoLogoOutline"" Text=""Todo sample"" Url=""https://bitplatform.dev/templates/overview"" Target=""_blank"" />
                <BitNavOption IconName=""@BitIconName.LocalAdmin"" Text=""AdminPanel sample"" Url=""https://bitplatform.dev/templates/overview"" Target=""_blank"" />
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
<BitNav TItem=""BitNavOption"" Mode=""BitNavMode.Manual"">
    <BitNavOption Text=""Fast foods"" Description=""List of fast foods""
                  IconName=""@BitIconName.HeartBroken"" IsExpanded=""true"">
        <BitNavOption Text=""Burgers"" Description=""List of burgers"">
            <BitNavOption Text=""Beef Burger"" Key=""Beef Burger"" />
            <BitNavOption Text=""Veggie Burger"" Key=""Veggie Burger"" />
            <BitNavOption Text=""Bison Burger"" Key=""Bison Burger"" />
            <BitNavOption Text=""Wild Salmon Burger"" Key=""Wild Salmon Burger"" />
        </BitNavOption>
        <BitNavOption Text=""Pizza"">
            <BitNavOption Text=""Cheese Pizza"" Key=""Cheese Pizza"" />
            <BitNavOption Text=""Veggie Pizza"" Key=""Veggie Pizza"" />
            <BitNavOption Text=""Pepperoni Pizza"" Key=""Pepperoni Pizza"" />
            <BitNavOption Text=""Meat Pizza"" Key=""Meat Pizza"" />
        </BitNavOption>
        <BitNavOption Text=""French Fries"" Key=""French Fries"" />
    </BitNavOption>
    <BitNavOption Text=""Fruits"" IconName=""@BitIconName.Health"">
        <BitNavOption Text=""Apple"" Key=""Apple"" />
        <BitNavOption Text=""Orange"" Key=""Orange"" />
        <BitNavOption Text=""Benana"" Key=""Benana"" />
    </BitNavOption>
    <BitNavOption Text=""Ice Cream"" Key=""Ice Cream"" />
    <BitNavOption Text=""Cookie"" Key=""Cookie"" />
</BitNav>

<BitLabel>Two-Way Bind</BitLabel>
<BitNav TItem=""BitNavOption"" Mode=""BitNavMode.Manual"">
    <BitNavOption Text=""Fast foods"" Description=""List of fast foods""
                  IconName=""@BitIconName.HeartBroken"" IsExpanded=""true"">
        <BitNavOption Text=""Burgers"" Description=""List of burgers"">
            <BitNavOption Text=""Beef Burger"" Key=""Beef Burger"" />
            <BitNavOption Text=""Veggie Burger"" Key=""Veggie Burger"" />
            <BitNavOption Text=""Bison Burger"" Key=""Bison Burger"" />
            <BitNavOption Text=""Wild Salmon Burger"" Key=""Wild Salmon Burger"" />
        </BitNavOption>
        <BitNavOption Text=""Pizza"">
            <BitNavOption Text=""Cheese Pizza"" Key=""Cheese Pizza"" />
            <BitNavOption Text=""Veggie Pizza"" Key=""Veggie Pizza"" />
            <BitNavOption Text=""Pepperoni Pizza"" Key=""Pepperoni Pizza"" />
            <BitNavOption Text=""Meat Pizza"" Key=""Meat Pizza"" />
        </BitNavOption>
        <BitNavOption Text=""French Fries"" Key=""French Fries"" />
    </BitNavOption>
    <BitNavOption Text=""Fruits"" IconName=""@BitIconName.Health"">
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
            <BitCheckbox />
            <BitIcon IconName=""@option.IconName"" />
            <span>@option.Text</span>
        </div>
    </ItemTemplate>
    <ChildContent>
        <BitNavOption Text=""Fast foods"" Description=""List of fast foods""
                      IconName=""@BitIconName.HeartBroken"" IsExpanded=""true"">
            <BitNavOption Text=""Burgers"" Description=""List of burgers"">
                <BitNavOption Text=""Beef Burger"" />
                <BitNavOption Text=""Veggie Burger"" />
                <BitNavOption Text=""Bison Burger"" />
                <BitNavOption Text=""Wild Salmon Burger"" />
            </BitNavOption>
            <BitNavOption Text=""Pizza"">
                <BitNavOption Text=""Cheese Pizza"" />
                <BitNavOption Text=""Veggie Pizza"" />
                <BitNavOption Text=""Pepperoni Pizza"" />
                <BitNavOption Text=""Meat Pizza"" />
            </BitNavOption>
            <BitNavOption Text=""French Fries"" />
        </BitNavOption>
        <BitNavOption Text=""Fruits"" IconName=""@BitIconName.Health"">
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
    <BitNavOption Text=""Fast foods"" Description=""List of fast foods""
                  IconName=""@BitIconName.HeartBroken"" IsExpanded=""true"">
        <BitNavOption Text=""Burgers"" Description=""List of burgers"">
            <BitNavOption Text=""Beef Burger"" Key=""Beef Burger"" />
            <BitNavOption Text=""Veggie Burger"" Key=""Veggie Burger"" />
            <BitNavOption Text=""Bison Burger"" Key=""Bison Burger"" />
            <BitNavOption Text=""Wild Salmon Burger"" Key=""Wild Salmon Burger"" />
        </BitNavOption>
        <BitNavOption Text=""Pizza"">
            <BitNavOption Text=""Cheese Pizza"" Key=""Cheese Pizza"" />
            <BitNavOption Text=""Veggie Pizza"" Key=""Veggie Pizza"" />
            <BitNavOption Text=""Pepperoni Pizza"" Key=""Pepperoni Pizza"" />
            <BitNavOption Text=""Meat Pizza"" Key=""Meat Pizza"" />
        </BitNavOption>
        <BitNavOption Text=""French Fries"" Key=""French Fries"" />
    </BitNavOption>
    <BitNavOption Text=""Fruits""  IconName=""@BitIconName.Health"">
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
                <BitNavOption IconName=""@BitIconName.ToDoLogoTop"" Text=""Todo sample"" Url=""https://bitplatform.dev/templates/overview"" Target=""_blank"" />
                <BitNavOption IconName=""@BitIconName.Admin"" Text=""AdminPanel sample"" Url=""https://bitplatform.dev/templates/overview"" Target=""_blank"" />
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

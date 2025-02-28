
namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.NavPanel;

public partial class BitNavPanelDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Accent",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The accent color of the nav.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "ChevronDownIcon",
            Type = "string?",
            DefaultValue = "null",
            Description = "The custom icon name of the chevron-down element of the BitNav component.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitNavPanelClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the nav panel.",
            Href = "#class-styles",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the nav.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "EmptyListTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template for when the search result is empty.",
        },
        new()
        {
            Name = "EmptyListMessage",
            Type = "string?",
            DefaultValue = "null",
            Description = "The custom message for when the search result is empty.",
        },
        new()
        {
            Name = "Footer",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template to render as the footer of the nav panel.",
        },
        new()
        {
            Name = "Header",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template to render as the header of the nav panel.",
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
            Name = "HeaderTemplateRenderMode",
            Type = "BitNavItemTemplateRenderMode",
            DefaultValue = "BitNavItemTemplateRenderMode.Normal",
            Description = "The render mode of the custom HeaderTemplate.",
            LinkType = LinkType.Link,
            Href = "#nav-itemtemplate-rendermode",
        },
        new()
        {
            Name = "IconNavUrl",
            Type = "string?",
            DefaultValue = "null",
            Description = "Renders an anchor wrapping the icon to navigate to the specified url.",
        },
        new()
        {
            Name = "IconUrl",
            Type = "string?",
            DefaultValue = "null",
            Description = "The icon url to show in the header of the nav panel.",
        },
        new()
        {
            Name = "IndentValue",
            Type = "int",
            DefaultValue = "16",
            Description = "The indentation value in px for each level of depth of child item."
        },
        new()
        {
            Name = "IndentPadding",
            Type = "int",
            DefaultValue = "27",
            Description = "The indentation padding in px for items without children (compensation space for chevron icon)."
        },
        new()
        {
            Name = "IndentReversedPadding",
            Type = "int",
            DefaultValue = "4",
            Description = "The indentation padding in px for items in reversed mode."
        },
        new()
        {
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines if the nav panel is open in small screens.",
        },
        new()
        {
           Name = "IsToggled",
           Type = "bool",
           DefaultValue = "false",
           Description = "Determines if the nav panel is in the toggled state.",
        },
        new()
        {
            Name = "Items",
            Type = "IList<TItem>",
            DefaultValue = "[]",
            Description = "A collection of items to display in the nav panel.",
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
            Name = "NavClasses",
            Type = "BitNavClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the nav component of the nav panel.",
        },
        new()
        {
           Name = "NavStyles",
           Type = "BitNavClassStyles?",
           DefaultValue = "null",
           Description = "Custom CSS styles for different parts of the nav component of the nav panel.",
        },
        new()
        {
            Name = "NoPad",
            Type = "bool",
            DefaultValue = "false",
            Description = "Disables the padded mode of the nav panel.",
        },
        new()
        {
            Name = "NoToggle",
            Type = "bool",
            DefaultValue = "false",
            Description = "Disables the toggle feature of the nav panel.",
        },
        new()
        {
            Name = "OnItemClick",
            Type = "EventCallback<TItem>",
            DefaultValue = "",
            Description = "Event fired up when an item is clicked.",
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
            Name = "Reselectable",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables recalling the select events when the same item is selected."
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
            Name = "SearchBoxClasses",
            Type = "BitSearchBoxClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the search box of the nav panel.",
        },
        new()
        {
            Name = "SearchBoxPlaceholder",
            Type = "string?",
            DefaultValue = "null",
            Description = "The placeholder of the input element of the search box of the nav panel.",
        },
        new()
        {
            Name = "SearchBoxStyles",
            Type = "BitSearchBoxClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the search box of the nav panel.",
        },
        new()
        {
            Name = "SingleExpand",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables the single-expand mode in the BitNav."
        },
        new()
        {
            Name = "Styles",
            Type = "BitNavPanelClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the nav panel.",
            Href = "#class-styles",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "Top",
            Type = "int",
            DefaultValue = "0",
            Description = "The top CSS property value of the root element of the nav panel in px.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
         {
            Id = "class-styles",
            Title = "BitNavPanelClassStyles",
            Parameters=
            [
                new()
                {
                    Name = "Overlay",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the overlay of the BitNavPanel.",
                },
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitNavPanel.",
                },
                new()
                {
                    Name = "Container",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the container of the BitNavPanel.",
                },
                new()
                {
                    Name = "Header",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the header container of the BitNavPanel.",
                },
                new()
                {
                    Name = "HeaderIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the header icon of the BitNavPanel.",
                },
                new()
                {
                    Name = "ToggleButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the toggle button of the BitNavPanel.",
                },
                new()
                {
                    Name = "SearchBox",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the search box of the BitNavPanel.",
                },
                new()
                {
                    Name = "ToggleSearchButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the toggle search button of the BitNavPanel.",
                },
                new()
                {
                    Name = "EmptyListMessage",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the empty list message of the BitNavPanel.",
                },
                new()
                {
                    Name = "Nav",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the nav component of the BitNavPanel.",
                },
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "color-enum",
            Name = "BitColor",
            Description = "Defines the general colors available in the bit BlazorUI.",
            Items =
            [
                new()
                {
                    Name= "Primary",
                    Description="Info Primary general color.",
                    Value="0",
                },
                new()
                {
                    Name= "Secondary",
                    Description="Secondary general color.",
                    Value="1",
                },
                new()
                {
                    Name= "Tertiary",
                    Description="Tertiary general color.",
                    Value="2",
                },
                new()
                {
                    Name= "Info",
                    Description="Info general color.",
                    Value="3",
                },
                new()
                {
                    Name= "Success",
                    Description="Success general color.",
                    Value="4",
                },
                new()
                {
                    Name= "Warning",
                    Description="Warning general color.",
                    Value="5",
                },
                new()
                {
                    Name= "SevereWarning",
                    Description="SevereWarning general color.",
                    Value="6",
                },
                new()
                {
                    Name= "Error",
                    Description="Error general color.",
                    Value="7",
                },
                new()
                {
                    Name= "PrimaryBackground",
                    Description="Primary background color.",
                    Value="8",
                },
                new()
                {
                    Name= "SecondaryBackground",
                    Description="Secondary background color.",
                    Value="9",
                },
                new()
                {
                    Name= "TertiaryBackground",
                    Description="Tertiary background color.",
                    Value="10",
                },
                new()
                {
                    Name= "PrimaryForeground",
                    Description="Primary foreground color.",
                    Value="11",
                },
                new()
                {
                    Name= "SecondaryForeground",
                    Description="Secondary foreground color.",
                    Value="12",
                },
                new()
                {
                    Name= "TertiaryForeground",
                    Description="Tertiary foreground color.",
                    Value="13",
                },
                new()
                {
                    Name= "PrimaryBorder",
                    Description="Primary border color.",
                    Value="14",
                },
                new()
                {
                    Name= "SecondaryBorder",
                    Description="Secondary border color.",
                    Value="15",
                },
                new()
                {
                    Name= "TertiaryBorder",
                    Description="Tertiary border color.",
                    Value="16",
                }
            ]
        },
        new()
        {
            Id = "nav-render-type-enum",
            Name = "BitNavRenderType",
            Description="Determines how the nav items are rendered visually.",
            Items =
            [
                new()
                {
                    Name = "Normal",
                    Value = "0",
                    Description="All items will be rendered normally only based on their own properties."
                },
                new()
                {
                    Name = "Grouped",
                    Value = "1",
                    Description="Root elements are rendered in a specific way that resembles a grouped list of items."
                }
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
    ];


    private bool basicIsOpen;
    private bool rtlIsOpen;
    private List<BitNavItem> basicNavItems =
    [
        new()
        {
            Text = "Home",
            IconName = BitIconName.Home,
            Url = "HomePage",
        },
        new()
        {
            Text = "AdminPanel",
            IconName = BitIconName.Admin,
            ChildItems =
            [
                new() {
                    Text = "Dashboard",
                    IconName = BitIconName.BarChartVerticalFill,
                    Url = "DashboardPage",
                },
                new() {
                    Text = "Categories",
                    IconName = BitIconName.BuildQueue,
                    Url = "CategoriesPage",
                },
                new() {
                    Text = "Products",
                    IconName = BitIconName.Product,
                    Url = "ProductsPage",
                }
            ]
        },
        new()
        {
            Text = "Todo",
            IconName = BitIconName.ToDoLogoOutline,
            Url = "TodoPage",
        },
        new()
        {
            Text = "Settings",
            IconName = BitIconName.Equalizer,
            Url = "SettingsPage"
        },
        new()
        {
            Text = "Terms",
            IconName = BitIconName.EntityExtraction,
            Url = "TermsPage",
        }
    ];



    private readonly string example1RazorCode = @"
<BitToggleButton @bind-IsChecked=""basicIsOpen"" OnText=""Close"" OffText=""Open"" />

<BitNavPanel @bind-IsOpen=""basicIsOpen"" Items=""basicNavItems"" NoPad />";
    private readonly string example1CsharpCode = @"
private bool basicIsOpen;

private List<BitNavItem> basicNavItems =
[
    new()
    {
        Text = ""Home"",
        IconName = BitIconName.Home,
        Url = ""HomePage"",
    },
    new()
    {
        Text = ""AdminPanel"",
        IconName = BitIconName.Admin,
        ChildItems =
        [
            new() {
                Text = ""Dashboard"",
                IconName = BitIconName.BarChartVerticalFill,
                Url = ""DashboardPage"",
            },
            new() {
                Text = ""Categories"",
                IconName = BitIconName.BuildQueue,
                Url = ""CategoriesPage"",
            },
            new() {
                Text = ""Products"",
                IconName = BitIconName.Product,
                Url = ""ProductsPage"",
            }
        ]
    },
    new()
    {
        Text = ""Todo"",
        IconName = BitIconName.ToDoLogoOutline,
        Url = ""TodoPage"",
    },
    new()
    {
        Text = ""Settings"",
        IconName = BitIconName.Equalizer,
        Url = ""SettingsPage""
    },
    new()
    {
        Text = ""Terms"",
        IconName = BitIconName.EntityExtraction,
        Url = ""TermsPage"",
    }
];";

    private readonly string example2RazorCode = @"
<BitToggleButton @bind-IsChecked=""rtlIsOpen"" OnText=""Close"" OffText=""Open"" />

<BitNavPanel @bind-IsOpen=""rtlIsOpen"" Items=""basicNavItems"" Dir=""BitDir.Rtl"" />";
    private readonly string example2CsharpCode = @"
private bool rtlIsOpen;

private List<BitNavItem> basicNavItems =
[
    new()
    {
        Text = ""Home"",
        IconName = BitIconName.Home,
        Url = ""HomePage"",
    },
    new()
    {
        Text = ""AdminPanel"",
        IconName = BitIconName.Admin,
        ChildItems =
        [
            new() {
                Text = ""Dashboard"",
                IconName = BitIconName.BarChartVerticalFill,
                Url = ""DashboardPage"",
            },
            new() {
                Text = ""Categories"",
                IconName = BitIconName.BuildQueue,
                Url = ""CategoriesPage"",
            },
            new() {
                Text = ""Products"",
                IconName = BitIconName.Product,
                Url = ""ProductsPage"",
            }
        ]
    },
    new()
    {
        Text = ""Todo"",
        IconName = BitIconName.ToDoLogoOutline,
        Url = ""TodoPage"",
    },
    new()
    {
        Text = ""Settings"",
        IconName = BitIconName.Equalizer,
        Url = ""SettingsPage""
    },
    new()
    {
        Text = ""Terms"",
        IconName = BitIconName.EntityExtraction,
        Url = ""TermsPage"",
    }
];";
}

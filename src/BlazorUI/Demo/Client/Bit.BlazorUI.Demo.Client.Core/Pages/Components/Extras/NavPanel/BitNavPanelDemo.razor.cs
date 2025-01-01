
namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.NavPanel;

public partial class BitNavPanelDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
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
            Name = "IconUrl",
            Type = "string?",
            DefaultValue = "null",
            Description = "The icon url to show in the header of the nav panel.",
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


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
            Name = "SearchBoxClasses",
            Type = "BitSearchBoxClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the search box of the nav panel.",
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
         },
         new()
         {
            Name = "Togglable",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables the toggle feature of the nav panel.",
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
                    Name = "Id",
                    Type = "string",
                    DefaultValue = "Guid.NewGuid().ToString()",
                    Description = "The id of the pdf reader instance and its canvas element(s).",
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

<BitNavPanel Items=""basicNavItems"" @bind-IsOpen=""basicIsOpen"" />";
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

<BitNavPanel Items=""basicNavItems"" @bind-IsOpen=""rtlIsOpen"" Dir=""BitDir.Rtl"" />";
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

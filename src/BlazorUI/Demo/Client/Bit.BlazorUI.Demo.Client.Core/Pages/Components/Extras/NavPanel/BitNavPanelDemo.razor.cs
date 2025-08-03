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
            Name = "AllExpanded",
            Type = "bool",
            DefaultValue = "false",
            Description = "Expands all items on first render."
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
            Name = "FitWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the nav panel with fit-content width.",
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
            Name = "FullWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the nav panel with full (100%) width.",
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
            Name = "HideToggle",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the toggle button.",
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
            Name = "NavMatch",
            Type = "BitNavMatch",
            DefaultValue = "null",
            Description = "Determines the global URL matching behavior of the nav.",
            LinkType = LinkType.Link,
            Href = "#nav-match-enum",
        },
        new()
        {
            Name = "NavMode",
            Type = "BitNavMode",
            DefaultValue = "BitNavMode.Automatic",
            Description = "Determines how the navigation will be handled.",
            LinkType = LinkType.Link,
            Href = "#nav-mode-enum",
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
            Name = "NoCollapse",
            Type = "bool",
            DefaultValue = "false",
            Description = "Disables and hides all collapse/expand buttons of the nav component.",
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
            Name = "NoSearchBox",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the search box from the nav panel.",
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
                    Name = "Toggled",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitNavPanel when toggled.",
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

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "Toggle",
            Type = "Task",
            Description = "Toggles the nav panel if possible.",
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
            Id = "nav-match-enum",
            Name = "BitNavMatch",
            Description = "Modifies the URL matching behavior for a BitNav<TItem>.",
            Items =
            [
                new()
                {
                    Name = "Exact",
                    Description = "Specifies that the nav item should be active when it matches exactly the current URL.",
                    Value = "0",
                },
                new()
                {
                    Name = "Prefix",
                    Description = "Specifies that the nav item should be active when it matches any prefix of the current URL.",
                    Value = "1",
                },
                new()
                {
                    Name = "Regex",
                    Description = "Specifies that the nav item should be active when its provided regex matches the current URL.",
                    Value = "2",
                },
                new()
                {
                    Name = "Wildcard",
                    Description = "Specifies that the nav item should be active when its provided wildcard matches the current URL.",
                    Value = "3",
                }
            ]
        },
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
    private bool fitWidthIsOpen;
    private bool fullWidthIsOpen;
    private bool noToggleIsOpen;
    private bool iconUrlIsOpen;
    private bool searchBoxPlaceholderIsOpen;
    private bool noSearchBoxIsOpen;
    private bool emptyListMessageIsOpen;
    private bool singleExpandIsOpen;
    private bool templateIsOpen;
    private bool eventIsOpen;
    private bool colorIsOpen;
    private bool classStyleIsOpen;
    private bool rtlIsOpen;

    private bool publicApiIsOpen;
    private BitNavPanel<BitNavItem> navPanelRef = default!;

    private BitNavItem? onItemClick;
    private BitNavItem? onItemToggle;

    private List<BitNavItem> basicNavItems =
    [
        new()
        {
            Text = "Home",
            IconName = BitIconName.Home,
            Url = "HomePage",
            Data = 13,
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
                    Data = 63,
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
            Url = "SettingsPage",
            Data = 85,
        },
        new()
        {
            Text = "Terms",
            IconName = BitIconName.EntityExtraction,
            Url = "TermsPage",
        }
    ];
    private List<BitNavItem> singleExpandNavItems =
    [
        new()
        {
            Text = "Home",
            IconName = BitIconName.Home,
            Url = "HomePage",
            Data = 13,
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
                    Data = 63,
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
            ChildItems =
            [
                new() {
                    Text = "Views",
                    IconName = BitIconName.BarChartVerticalFill,
                    Url = "ViewsPage",
                    Data = 63,
                },
                new() {
                    Text = "Users",
                    IconName = BitIconName.BuildQueue,
                    Url = "UsersPage",
                }
            ]
        },
        new()
        {
            Text = "Terms",
            IconName = BitIconName.EntityExtraction,
            Url = "TermsPage",
        }
    ];
    private List<BitNavItem> eventNavItems =
    [
        new()
        {
            Text = "Home",
            IconName = BitIconName.Home,
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
                },
                new() {
                    Text = "Categories",
                    IconName = BitIconName.BuildQueue,
                },
                new() {
                    Text = "Products",
                    IconName = BitIconName.Product,
                }
            ]
        },
        new()
        {
            Text = "Todo",
            IconName = BitIconName.ToDoLogoOutline,
        },
        new()
        {
            Text = "Settings",
            IconName = BitIconName.Equalizer,
        },
        new()
        {
            Text = "Terms",
            IconName = BitIconName.EntityExtraction,
        }
    ];
    private List<BitNavItem> rtlNavItems =
    [
        new()
        {
            Text = "خانه",
            IconName = BitIconName.Home,
            Url = "HomePage",
        },
        new()
        {
            Text = "ادمین پنل",
            IconName = BitIconName.Admin,
            ChildItems =
            [
                new() {
                    Text = "داشبورد",
                    IconName = BitIconName.BarChartVerticalFill,
                    Url = "DashboardPage",
                },
                new() {
                    Text = "دسته‌ها",
                    IconName = BitIconName.BuildQueue,
                    Url = "CategoriesPage",
                },
                new() {
                    Text = "کالاها",
                    IconName = BitIconName.Product,
                    Url = "ProductsPage",
                }
            ]
        },
        new()
        {
            Text = "وظایف",
            IconName = BitIconName.ToDoLogoOutline,
            Url = "TodoPage",
        },
        new()
        {
            Text = "تنظیمات",
            IconName = BitIconName.Equalizer,
            Url = "SettingsPage"
        },
        new()
        {
            Text = "قوانین",
            IconName = BitIconName.EntityExtraction,
            Url = "TermsPage",
        }
    ];

    private void HandleOnItemClick(BitNavItem item)
    {
        onItemClick = item;
    }

    private void HandleOnItemToggle(BitNavItem item)
    {
        onItemToggle = item;
    }
}

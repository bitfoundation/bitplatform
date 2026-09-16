namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.NavPanel;

public partial class BitNavPanelDemo
{
    private readonly string example1RazorCode = @"
<BitToggleButton @bind-IsChecked=""basicIsOpen"" OnText=""Close"" OffText=""Open"" />

<div style=""width:222px"">
    <BitNavPanel @bind-IsOpen=""basicIsOpen"" Items=""basicNavItems"" />
</div>";
    private readonly string example1CsharpCode = @"
private bool basicIsOpen;

private List<BitNavItem> basicNavItems =
[
    new()
    {
        Text = ""Home"",
        IconName = BitIconName.Home,
        Url = ""HomePage"",
        Data = 13,
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
                Data = 63,
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
        Url = ""SettingsPage"",
        Data = 85,
    },
    new()
    {
        Text = ""Terms"",
        IconName = BitIconName.EntityExtraction,
        Url = ""TermsPage"",
    }
];";

    private readonly string example2RazorCode = @"
<BitToggleButton @bind-IsChecked=""fitWidthIsOpen"" OnText=""Close"" OffText=""Open"" />

<BitNavPanel @bind-IsOpen=""fitWidthIsOpen"" Items=""basicNavItems"" FitWidth />";
    private readonly string example2CsharpCode = @"
private bool fitWidthIsOpen;

private List<BitNavItem> basicNavItems =
[
    new()
    {
        Text = ""Home"",
        IconName = BitIconName.Home,
        Url = ""HomePage"",
        Data = 13,
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
                Data = 63,
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
        Url = ""SettingsPage"",
        Data = 85,
    },
    new()
    {
        Text = ""Terms"",
        IconName = BitIconName.EntityExtraction,
        Url = ""TermsPage"",
    }
];";

    private readonly string example3RazorCode = @"
<BitToggleButton @bind-IsChecked=""fullWidthIsOpen"" OnText=""Close"" OffText=""Open"" />

<BitNavPanel @bind-IsOpen=""fullWidthIsOpen"" Items=""basicNavItems"" FullWidth />";
    private readonly string example3CsharpCode = @"
private bool fullWidthIsOpen;

private List<BitNavItem> basicNavItems =
[
    new()
    {
        Text = ""Home"",
        IconName = BitIconName.Home,
        Url = ""HomePage"",
        Data = 13,
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
                Data = 63,
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
        Url = ""SettingsPage"",
        Data = 85,
    },
    new()
    {
        Text = ""Terms"",
        IconName = BitIconName.EntityExtraction,
        Url = ""TermsPage"",
    }
];";

    private readonly string example4RazorCode = @"
<BitToggleButton @bind-IsChecked=""widthIsOpen"" OnText=""Close"" OffText=""Open"" />

<BitNavPanel @bind-IsOpen=""widthIsOpen"" Items=""basicNavItems"" Width=""260"" ToggledWidth=""72"" />";
    private readonly string example4CsharpCode = @"
private bool widthIsOpen;

private List<BitNavItem> basicNavItems =
[
    new()
    {
        Text = ""Home"",
        IconName = BitIconName.Home,
        Url = ""HomePage"",
        Data = 13,
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
                Data = 63,
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
        Url = ""SettingsPage"",
        Data = 85,
    },
    new()
    {
        Text = ""Terms"",
        IconName = BitIconName.EntityExtraction,
        Url = ""TermsPage"",
    }
];";

    private readonly string example5RazorCode = @"
<BitToggleButton @bind-IsChecked=""expandOnHoverIsOpen"" OnText=""Close"" OffText=""Open"" />

<div style=""width:222px"">
    <BitNavPanel @bind-IsOpen=""expandOnHoverIsOpen"" Items=""basicNavItems"" ExpandOnHover />
</div>";
    private readonly string example5CsharpCode = @"
private bool expandOnHoverIsOpen;

private List<BitNavItem> basicNavItems =
[
    new()
    {
        Text = ""Home"",
        IconName = BitIconName.Home,
        Url = ""HomePage"",
        Data = 13,
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
                Data = 63,
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
        Url = ""SettingsPage"",
        Data = 85,
    },
    new()
    {
        Text = ""Terms"",
        IconName = BitIconName.EntityExtraction,
        Url = ""TermsPage"",
    }
];";

    private readonly string example6RazorCode = @"
<BitToggleButton @bind-IsChecked=""noToggleIsOpen"" OnText=""Close"" OffText=""Open"" />

<div style=""width:222px"">
    <BitNavPanel @bind-IsOpen=""noToggleIsOpen"" Items=""basicNavItems"" NoToggle />
</div>";
    private readonly string example6CsharpCode = @"
private bool noToggleIsOpen;

private List<BitNavItem> basicNavItems =
[
    new()
    {
        Text = ""Home"",
        IconName = BitIconName.Home,
        Url = ""HomePage"",
        Data = 13,
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
                Data = 63,
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
        Url = ""SettingsPage"",
        Data = 85,
    },
    new()
    {
        Text = ""Terms"",
        IconName = BitIconName.EntityExtraction,
        Url = ""TermsPage"",
    }
];";

    private readonly string example7RazorCode = @"
<BitToggleButton @bind-IsChecked=""iconUrlIsOpen"" OnText=""Close"" OffText=""Open"" />

<div style=""width:222px"">
    <BitNavPanel @bind-IsOpen=""iconUrlIsOpen""
                 Items=""basicNavItems""
                 IconUrl=""/images/icon.png""
                 IconNavUrl=""https://bitplatform.dev"" />
</div>";
    private readonly string example7CsharpCode = @"
private bool iconUrlIsOpen;

private List<BitNavItem> basicNavItems =
[
    new()
    {
        Text = ""Home"",
        IconName = BitIconName.Home,
        Url = ""HomePage"",
        Data = 13,
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
                Data = 63,
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
        Url = ""SettingsPage"",
        Data = 85,
    },
    new()
    {
        Text = ""Terms"",
        IconName = BitIconName.EntityExtraction,
        Url = ""TermsPage"",
    }
];";

    private readonly string example8RazorCode = @"
<BitToggleButton @bind-IsChecked=""searchBoxPlaceholderIsOpen"" OnText=""Close"" OffText=""Open"" />

<div style=""width:240px"">
    <BitNavPanel @bind-IsOpen=""searchBoxPlaceholderIsOpen"" Items=""basicNavItems"" SearchBoxPlaceholder=""Search in menu items..."" />
</div>";
    private readonly string example8CsharpCode = @"
private bool searchBoxPlaceholderIsOpen;

private List<BitNavItem> basicNavItems =
[
    new()
    {
        Text = ""Home"",
        IconName = BitIconName.Home,
        Url = ""HomePage"",
        Data = 13,
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
                Data = 63,
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
        Url = ""SettingsPage"",
        Data = 85,
    },
    new()
    {
        Text = ""Terms"",
        IconName = BitIconName.EntityExtraction,
        Url = ""TermsPage"",
    }
];";

    private readonly string example9RazorCode = @"
<BitToggleButton @bind-IsChecked=""noSearchBoxIsOpen"" OnText=""Close"" OffText=""Open"" />

<div style=""width:240px"">
    <BitNavPanel @bind-IsOpen=""noSearchBoxIsOpen"" Items=""basicNavItems"" NoSearchBox />
</div>";
    private readonly string example9CsharpCode = @"
private bool noSearchBoxIsOpen;

private List<BitNavItem> basicNavItems =
[
    new()
    {
        Text = ""Home"",
        IconName = BitIconName.Home,
        Url = ""HomePage"",
        Data = 13,
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
                Data = 63,
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
        Url = ""SettingsPage"",
        Data = 85,
    },
    new()
    {
        Text = ""Terms"",
        IconName = BitIconName.EntityExtraction,
        Url = ""TermsPage"",
    }
];";

    private readonly string example10RazorCode = @"
<BitToggleButton @bind-IsChecked=""searchIsOpen"" OnText=""Close"" OffText=""Open"" />

<BitTextField Label=""Search text"" @bind-Value=""searchText"" Immediate />
<div>Last searched term: <b>@lastSearchedTerm</b></div>

<div style=""width:240px"">
    <BitNavPanel @bind-IsOpen=""searchIsOpen""
                 Items=""basicNavItems""
                 @bind-SearchText=""searchText""
                 SearchDebounceTime=""200""
                 OnSearch=""v => lastSearchedTerm = v""
                 SearchFilter=""(item, term) => item.Text.StartsWith(term, StringComparison.OrdinalIgnoreCase)"" />
</div>";
    private readonly string example10CsharpCode = @"
private bool searchIsOpen;
private string? searchText;
private string? lastSearchedTerm;

private List<BitNavItem> basicNavItems =
[
    new()
    {
        Text = ""Home"",
        IconName = BitIconName.Home,
        Url = ""HomePage"",
        Data = 13,
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
                Data = 63,
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
        Url = ""SettingsPage"",
        Data = 85,
    },
    new()
    {
        Text = ""Terms"",
        IconName = BitIconName.EntityExtraction,
        Url = ""TermsPage"",
    }
];";

    private readonly string example11RazorCode = @"
<BitToggleButton @bind-IsChecked=""emptyListMessageIsOpen"" OnText=""Close"" OffText=""Open"" />

<div style=""width:222px"">
    <BitNavPanel @bind-IsOpen=""emptyListMessageIsOpen"" Items=""basicNavItems"" EmptyListMessage=""There is no item found."" />
</div>";
    private readonly string example11CsharpCode = @"
private bool emptyListMessageIsOpen;

private List<BitNavItem> basicNavItems =
[
    new()
    {
        Text = ""Home"",
        IconName = BitIconName.Home,
        Url = ""HomePage"",
        Data = 13,
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
                Data = 63,
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
        Url = ""SettingsPage"",
        Data = 85,
    },
    new()
    {
        Text = ""Terms"",
        IconName = BitIconName.EntityExtraction,
        Url = ""TermsPage"",
    }
];";

    private readonly string example12RazorCode = @"
<BitToggleButton @bind-IsChecked=""selectionIsOpen"" OnText=""Close"" OffText=""Open"" />

<div>Selected item: <b>@selectedItem?.Text</b></div>
<BitButton OnClick=""() => selectedItem = basicNavItems[3]"">Select Settings</BitButton>

<div style=""width:222px"">
    <BitNavPanel @bind-IsOpen=""selectionIsOpen""
                 Items=""basicNavItems""
                 NavMode=""BitNavMode.Manual""
                 @bind-SelectedItem=""selectedItem""
                 DefaultSelectedItem=""basicNavItems[0]"" />
</div>";
    private readonly string example12CsharpCode = @"
private bool selectionIsOpen;
private BitNavItem? selectedItem;

private List<BitNavItem> basicNavItems =
[
    new()
    {
        Text = ""Home"",
        IconName = BitIconName.Home,
        Url = ""HomePage"",
        Data = 13,
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
                Data = 63,
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
        Url = ""SettingsPage"",
        Data = 85,
    },
    new()
    {
        Text = ""Terms"",
        IconName = BitIconName.EntityExtraction,
        Url = ""TermsPage"",
    }
];";

    private readonly string example13RazorCode = @"
<BitToggleButton @bind-IsChecked=""singleExpandIsOpen"" OnText=""Close"" OffText=""Open"" />

<div style=""width:222px"">
    <BitNavPanel @bind-IsOpen=""singleExpandIsOpen"" Items=""singleExpandNavItems"" SingleExpand />
</div>";
    private readonly string example13CsharpCode = @"
private bool singleExpandIsOpen;

private List<BitNavItem> singleExpandNavItems =
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
            new() { Text = ""Dashboard"", IconName = BitIconName.BarChartVerticalFill, Url = ""DashboardPage"" },
            new() { Text = ""Categories"", IconName = BitIconName.BuildQueue, Url = ""CategoriesPage"" },
            new() { Text = ""Products"", IconName = BitIconName.Product, Url = ""ProductsPage"" }
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
        ChildItems =
        [
            new() { Text = ""Views"", IconName = BitIconName.BarChartVerticalFill, Url = ""ViewsPage"" },
            new() { Text = ""Users"", IconName = BitIconName.BuildQueue, Url = ""UsersPage"" }
        ]
    },
    new()
    {
        Text = ""Terms"",
        IconName = BitIconName.EntityExtraction,
        Url = ""TermsPage"",
    }
];";

    private readonly string example14RazorCode = @"
<BitToggleButton @bind-IsChecked=""customIsOpen"" OnText=""Close"" OffText=""Open"" />

<div style=""width:222px"">
    <BitNavPanel @bind-IsOpen=""customIsOpen"" Items=""customNavItems"" NameSelectors=""customSelectors"" />
</div>";
    private readonly string example14CsharpCode = @"
private bool customIsOpen;

private static readonly BitNavNameSelectors<CustomNavItem> customSelectors = new()
{
    Text = { Name = nameof(CustomNavItem.Name) },
    IconName = { Name = nameof(CustomNavItem.Glyph) },
    ChildItems = { Name = nameof(CustomNavItem.Children) },
};

public class CustomNavItem
{
    public string? Name { get; set; }
    public string? Glyph { get; set; }
    public string? Url { get; set; }
    public List<CustomNavItem>? Children { get; set; }
}

private readonly List<CustomNavItem> customNavItems =
[
    new()
    {
        Name = ""Home"",
        Glyph = BitIconName.Home,
        Url = ""HomePage"",
    },
    new()
    {
        Name = ""AdminPanel"",
        Glyph = BitIconName.Admin,
        Children =
        [
            new() { Name = ""Dashboard"", Glyph = BitIconName.BarChartVerticalFill, Url = ""DashboardPage"" },
            new() { Name = ""Categories"", Glyph = BitIconName.BuildQueue, Url = ""CategoriesPage"" },
            new() { Name = ""Products"", Glyph = BitIconName.Product, Url = ""ProductsPage"" }
        ]
    },
    new()
    {
        Name = ""Settings"",
        Glyph = BitIconName.Equalizer,
        Url = ""SettingsPage"",
    }
];";

    private readonly string example15RazorCode = @"
<BitToggleButton @bind-IsChecked=""behaviorIsOpen"" OnText=""Close"" OffText=""Open"" />

<div style=""width:222px"">
    <BitNavPanel @bind-IsOpen=""behaviorIsOpen"" Items=""basicNavItems"" AutoFocus NoAutoClose NoOverlay NoSwipe />
</div>";
    private readonly string example15CsharpCode = @"
private bool behaviorIsOpen;

private List<BitNavItem> basicNavItems =
[
    new()
    {
        Text = ""Home"",
        IconName = BitIconName.Home,
        Url = ""HomePage"",
        Data = 13,
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
                Data = 63,
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
        Url = ""SettingsPage"",
        Data = 85,
    },
    new()
    {
        Text = ""Terms"",
        IconName = BitIconName.EntityExtraction,
        Url = ""TermsPage"",
    }
];";

    private readonly string example16RazorCode = @"
<BitToggleButton @bind-IsChecked=""templateIsOpen"" OnText=""Close"" OffText=""Open"" />

<BitNavPanel @bind-IsOpen=""templateIsOpen"" Items=""basicNavItems"" FitWidth NoToggle>
    <Header>
        <BitText Typography=""BitTypography.H5""><b>NavPanel</b> header</BitText>
    </Header>
    <ItemTemplate Context=""item"">
        <BitText><i><b>@item.Text</b></i></BitText>
        <BitSpacer />
        @if (item.Data is not null)
        {
            <BitTag Size=""BitSize.Small"" Color=""BitColor.Info"">@item.Data</BitTag>
        }
    </ItemTemplate>
    <Footer>
        <BitActionButton IconName=""@BitIconName.PowerButton"">Logout</BitActionButton>
    </Footer>
</BitNavPanel>";
    private readonly string example16CsharpCode = @"
private bool templateIsOpen;

private List<BitNavItem> basicNavItems =
[
    new()
    {
        Text = ""Home"",
        IconName = BitIconName.Home,
        Url = ""HomePage"",
        Data = 13,
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
                Data = 63,
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
        Url = ""SettingsPage"",
        Data = 85,
    },
    new()
    {
        Text = ""Terms"",
        IconName = BitIconName.EntityExtraction,
        Url = ""TermsPage"",
    }
];";

    private readonly string example17RazorCode = @"
<BitToggleButton @bind-IsChecked=""eventIsOpen"" OnText=""Close"" OffText=""Open"" />

<div>
    Clicked item: @onItemClick?.Text
    <br />
    Toggled item: @onItemToggle?.Text
</div>

<div style=""width:222px"">
    <BitNavPanel @bind-IsOpen=""eventIsOpen""
                 Items=""eventNavItems""
                 OnItemClick=""(BitNavItem item) => HandleOnItemClick(item)""
                 OnItemToggle=""(BitNavItem item) => HandleOnItemToggle(item)"" />
</div>";
    private readonly string example17CsharpCode = @"
private bool eventIsOpen;
private BitNavItem? onItemClick;
private BitNavItem? onItemToggle;

private void HandleOnItemClick(BitNavItem item)
{
    onItemClick = item;
}

private void HandleOnItemToggle(BitNavItem item)
{
    onItemToggle = item;
}

private List<BitNavItem> eventNavItems =
[
    new()
    {
        Text = ""Home"",
        IconName = BitIconName.Home,
    },
    new()
    {
        Text = ""AdminPanel"",
        IconName = BitIconName.Admin,
        ChildItems =
        [
            new() { Text = ""Dashboard"", IconName = BitIconName.BarChartVerticalFill },
            new() { Text = ""Categories"", IconName = BitIconName.BuildQueue },
            new() { Text = ""Products"", IconName = BitIconName.Product }
        ]
    },
    new()
    {
        Text = ""Todo"",
        IconName = BitIconName.ToDoLogoOutline,
    },
    new()
    {
        Text = ""Settings"",
        IconName = BitIconName.Equalizer,
    },
    new()
    {
        Text = ""Terms"",
        IconName = BitIconName.EntityExtraction,
    }
];";

    private readonly string example18RazorCode = @"
<BitToggleButton @bind-IsChecked=""publicApiIsOpen"" OnText=""Close"" OffText=""Open"" />

<BitButton OnClick=""() => navPanelRef.Toggle()"">Toggle</BitButton>
<BitButton OnClick=""() => navPanelRef.ExpandAll()"">Expand all</BitButton>
<BitButton OnClick=""() => navPanelRef.CollapseAll()"">Collapse all</BitButton>
<BitButton OnClick=""() => navPanelRef.FocusSearchBox()"">Focus search</BitButton>
<BitButton OnClick=""() => navPanelRef.ClearSearch()"">Clear search</BitButton>

<div style=""width:222px"">
    <BitNavPanel @bind-IsOpen=""publicApiIsOpen"" @ref=""navPanelRef"" Items=""basicNavItems"" HideToggle />
</div>";
    private readonly string example18CsharpCode = @"
private bool publicApiIsOpen;
private BitNavPanel<BitNavItem> navPanelRef = default!;

private List<BitNavItem> basicNavItems =
[
    new()
    {
        Text = ""Home"",
        IconName = BitIconName.Home,
        Url = ""HomePage"",
        Data = 13,
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
                Data = 63,
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
        Url = ""SettingsPage"",
        Data = 85,
    },
    new()
    {
        Text = ""Terms"",
        IconName = BitIconName.EntityExtraction,
        Url = ""TermsPage"",
    }
];";

    private readonly string example19RazorCode = @"
<BitToggleButton @bind-IsChecked=""groupedIsOpen"" OnText=""Close"" OffText=""Open"" />

<div style=""width:240px"">
    <BitNavPanel @bind-IsOpen=""groupedIsOpen""
                 Items=""singleExpandNavItems""
                 RenderType=""BitNavRenderType.Grouped""
                 IndentValue=""24""
                 ReversedChevron
                 NoPad />
</div>";
    private readonly string example19CsharpCode = @"
private bool groupedIsOpen;

private List<BitNavItem> singleExpandNavItems =
[
    new()
    {
        Text = ""Home"",
        IconName = BitIconName.Home,
        Url = ""HomePage"",
        Data = 13,
    },
    new()
    {
        Text = ""AdminPanel"",
        IconName = BitIconName.Admin,
        ChildItems =
        [
            new() { Text = ""Dashboard"", IconName = BitIconName.BarChartVerticalFill, Url = ""DashboardPage"" },
            new() { Text = ""Categories"", IconName = BitIconName.BuildQueue, Url = ""CategoriesPage"" },
            new() { Text = ""Products"", IconName = BitIconName.Product, Url = ""ProductsPage"" }
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
        ChildItems =
        [
            new() { Text = ""Views"", IconName = BitIconName.BarChartVerticalFill, Url = ""ViewsPage"" },
            new() { Text = ""Users"", IconName = BitIconName.BuildQueue, Url = ""UsersPage"" }
        ]
    },
    new()
    {
        Text = ""Terms"",
        IconName = BitIconName.EntityExtraction,
        Url = ""TermsPage"",
    }
];";

    private readonly string example20RazorCode = @"
<BitToggleButton @bind-IsChecked=""drawerIsOpen"" OnText=""Close"" OffText=""Open"" />

<div style=""width:222px"">
    <BitNavPanel @bind-IsOpen=""drawerIsOpen""
                 Items=""basicNavItems""
                 AutoFocus
                 ShowCloseButton
                 Position=""BitNavPanelPosition.End"" />
</div>";
    private readonly string example20CsharpCode = @"
private bool drawerIsOpen;

private List<BitNavItem> basicNavItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home, Url = ""HomePage"", Data = 13 },
    new()
    {
        Text = ""AdminPanel"",
        IconName = BitIconName.Admin,
        ChildItems =
        [
            new() { Text = ""Dashboard"", IconName = BitIconName.BarChartVerticalFill, Url = ""DashboardPage"" },
            new() { Text = ""Categories"", IconName = BitIconName.BuildQueue, Url = ""CategoriesPage"" },
            new() { Text = ""Products"", IconName = BitIconName.Product, Url = ""ProductsPage"" }
        ]
    },
    new() { Text = ""Todo"", IconName = BitIconName.ToDoLogoOutline, Url = ""TodoPage"" },
    new() { Text = ""Settings"", IconName = BitIconName.Equalizer, Url = ""SettingsPage"" }
];";

    private readonly string example21RazorCode = @"
<BitToggleButton @bind-IsChecked=""stickyIsOpen"" OnText=""Close"" OffText=""Open"" />

<div style=""width:240px"">
    <BitNavPanel @bind-IsOpen=""stickyIsOpen""
                 Items=""singleExpandNavItems""
                 Style=""height:264px""
                 AllExpanded
                 StickyEnds>
        <Footer>
            <BitActionButton IconName=""@BitIconName.PowerButton"">Logout</BitActionButton>
        </Footer>
    </BitNavPanel>
</div>";
    private readonly string example21CsharpCode = @"
private bool stickyIsOpen;

private List<BitNavItem> singleExpandNavItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home, Url = ""HomePage"" },
    new()
    {
        Text = ""AdminPanel"",
        IconName = BitIconName.Admin,
        ChildItems =
        [
            new() { Text = ""Dashboard"", IconName = BitIconName.BarChartVerticalFill, Url = ""DashboardPage"" },
            new() { Text = ""Categories"", IconName = BitIconName.BuildQueue, Url = ""CategoriesPage"" },
            new() { Text = ""Products"", IconName = BitIconName.Product, Url = ""ProductsPage"" }
        ]
    },
    new() { Text = ""Todo"", IconName = BitIconName.ToDoLogoOutline, Url = ""TodoPage"" },
    new()
    {
        Text = ""Settings"",
        IconName = BitIconName.Equalizer,
        ChildItems =
        [
            new() { Text = ""Views"", IconName = BitIconName.BarChartVerticalFill, Url = ""ViewsPage"" },
            new() { Text = ""Users"", IconName = BitIconName.BuildQueue, Url = ""UsersPage"" }
        ]
    },
    new() { Text = ""Terms"", IconName = BitIconName.EntityExtraction, Url = ""TermsPage"" }
];";

    private readonly string example22RazorCode = @"
<BitToggleButton @bind-IsChecked=""colorIsOpen"" OnText=""Close"" OffText=""Open"" />

<div style=""width:222px"">
    <BitNavPanel @bind-IsOpen=""colorIsOpen"" Items=""basicNavItems"" Color=""BitColor.Secondary"" Accent=""BitColor.SecondaryBackground"" />
</div>";
    private readonly string example22CsharpCode = @"
private bool colorIsOpen;

private List<BitNavItem> basicNavItems =
[
    new()
    {
        Text = ""Home"",
        IconName = BitIconName.Home,
        Url = ""HomePage"",
        Data = 13,
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
                Data = 63,
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
        Url = ""SettingsPage"",
        Data = 85,
    },
    new()
    {
        Text = ""Terms"",
        IconName = BitIconName.EntityExtraction,
        Url = ""TermsPage"",
    }
];";

    private readonly string example23RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitToggleButton @bind-IsChecked=""externalIconIsOpen"" OnText=""Close"" OffText=""Open"" />

<div style=""width:222px"">
    <BitNavPanel @bind-IsOpen=""externalIconIsOpen""
                 Items=""externalIconNavItems""
                 ToggleIcon=""@BitIconInfo.Fa(""solid bars"")""
                 ChevronDownIcon=""@BitIconInfo.Fa(""solid chevron-down"")"" />
</div>";
    private readonly string example23CsharpCode = @"
private bool externalIconIsOpen;

private readonly List<BitNavItem> externalIconNavItems =
[
    new()
    {
        Text = ""Home"",
        Icon = BitIconInfo.Fa(""solid house""),
        Url = ""HomePage"",
    },
    new()
    {
        Text = ""AdminPanel"",
        Icon = BitIconInfo.Fa(""solid user-shield""),
        ChildItems =
        [
            new() { Text = ""Dashboard"", Icon = BitIconInfo.Fa(""solid chart-simple""), Url = ""DashboardPage"" },
            new() { Text = ""Products"", Icon = BitIconInfo.Fa(""solid box""), Url = ""ProductsPage"" }
        ]
    },
    new()
    {
        Text = ""Settings"",
        Icon = BitIconInfo.Fa(""solid gear""),
        Url = ""SettingsPage"",
    }
];";

    private readonly string example24RazorCode = @"
<BitToggleButton @bind-IsChecked=""sizeIsOpen"" OnText=""Close"" OffText=""Open"" />

<div style=""width:180px"">
    <div>Small</div>
    <BitNavPanel @bind-IsOpen=""sizeIsOpen"" Items=""basicNavItems"" Size=""BitSize.Small"" NoSearchBox NoToggle />
</div>
<div style=""width:180px"">
    <div>Medium</div>
    <BitNavPanel @bind-IsOpen=""sizeIsOpen"" Items=""basicNavItems"" Size=""BitSize.Medium"" NoSearchBox NoToggle />
</div>
<div style=""width:180px"">
    <div>Large</div>
    <BitNavPanel @bind-IsOpen=""sizeIsOpen"" Items=""basicNavItems"" Size=""BitSize.Large"" NoSearchBox NoToggle />
</div>";
    private readonly string example24CsharpCode = @"
private bool sizeIsOpen;

private List<BitNavItem> basicNavItems =
[
    new()
    {
        Text = ""Home"",
        IconName = BitIconName.Home,
        Url = ""HomePage"",
        Data = 13,
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
                Data = 63,
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
        Url = ""SettingsPage"",
        Data = 85,
    },
    new()
    {
        Text = ""Terms"",
        IconName = BitIconName.EntityExtraction,
        Url = ""TermsPage"",
    }
];";

    private readonly string example25RazorCode = @"
<style>

@media(hover: hover) {
    .custom-nav-item:hover {
        color: #fff;
        border-radius: 7px;
        background-color: hsla(0,0%,100%,.1)
    }
}

.custom-nav-item-ico {
    color: #fff;
    font-weight: 600
}

.custom-nav-item-txt {
    color: #fff
}

.custom-input-container-searchbox {
    overflow: hidden;
    border-radius: 7px;
    align-items: center;
    border-color: hsla(0,0%,100%,.462745098);
    background-color: rgba(177,177,177,.4588235294)
}

.custom-focused-searchbox .custom-input-container-searchbox {
    border-width: 1px;
    border-color: hsla(0,0%,100%,.462745098)
}

.custom-clear-searchbox:hover {
    background: rgba(0,0,0,0)
}

.custom-icon-searchbox {
    color: #3a0647
}

.custom-icon-wrapper-searchbox {
    border-radius: 5px;
    background-color: rgba(0,0,0,0)
}

</style>

<BitToggleButton @bind-IsChecked=""classStyleIsOpen"" OnText=""Close"" OffText=""Open"" />

<div style=""width:222px"">
    <BitNavPanel @bind-IsOpen=""classStyleIsOpen""
                 Items=""basicNavItems""
                 Styles=""@(new() { Container = ""background-image: linear-gradient(180deg, rgb(5, 39, 103) 0%, #3a0647 70%);"" })""
                 NavClasses=""@(new() { ItemContainer = ""custom-nav-item"", ItemIcon = ""custom-nav-item-ico"", ItemText = ""custom-nav-item-txt"" })""
                 SearchBoxClasses=""@(new() { Icon = ""custom-icon-searchbox"",
                                             Focused = ""custom-focused-searchbox"",
                                             ClearButton = ""custom-clear-searchbox"",
                                             IconWrapper = ""custom-icon-wrapper-searchbox"",
                                             InputContainer = ""custom-input-container-searchbox"" })"" />
</div>";
    private readonly string example25CsharpCode = @"
private bool classStyleIsOpen;

private List<BitNavItem> basicNavItems =
[
    new()
    {
        Text = ""Home"",
        IconName = BitIconName.Home,
        Url = ""HomePage"",
        Data = 13,
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
                Data = 63,
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
        Url = ""SettingsPage"",
        Data = 85,
    },
    new()
    {
        Text = ""Terms"",
        IconName = BitIconName.EntityExtraction,
        Url = ""TermsPage"",
    }
];";

    private readonly string example26RazorCode = @"
<BitToggleButton @bind-IsChecked=""rtlIsOpen"" OnText=""Close"" OffText=""Open"" />

<BitNavPanel @bind-IsOpen=""rtlIsOpen"" Items=""rtlNavItems"" FitWidth Dir=""BitDir.Rtl"" />";
    private readonly string example26CsharpCode = @"
private bool rtlIsOpen;

private List<BitNavItem> rtlNavItems =
[
    new()
    {
        Text = ""خانه"",
        IconName = BitIconName.Home,
        Url = ""HomePage"",
    },
    new()
    {
        Text = ""ادمین پنل"",
        IconName = BitIconName.Admin,
        ChildItems =
        [
            new() { Text = ""داشبورد"", IconName = BitIconName.BarChartVerticalFill, Url = ""DashboardPage"" },
            new() { Text = ""دسته‌ها"", IconName = BitIconName.BuildQueue, Url = ""CategoriesPage"" },
            new() { Text = ""کالاها"", IconName = BitIconName.Product, Url = ""ProductsPage"" }
        ]
    },
    new()
    {
        Text = ""وظایف"",
        IconName = BitIconName.ToDoLogoOutline,
        Url = ""TodoPage"",
    },
    new()
    {
        Text = ""تنظیمات"",
        IconName = BitIconName.Equalizer,
        Url = ""SettingsPage""
    },
    new()
    {
        Text = ""قوانین"",
        IconName = BitIconName.EntityExtraction,
        Url = ""TermsPage"",
    }
];";
}

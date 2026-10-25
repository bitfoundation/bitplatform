namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.NavPanel;

public partial class BitNavPanelDemo
{
    private const string basicNavItemsBodyCsharpCode = @"
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

    private const string basicNavItemsCsharpCode = @"
private List<BitNavItem> basicNavItems =" + basicNavItemsBodyCsharpCode;

    private const string createBasicNavItemsCsharpCode = @"
private static List<BitNavItem> CreateBasicNavItems() =>" + basicNavItemsBodyCsharpCode;

    private const string expansionNavItemsBodyCsharpCode = @"
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

    private readonly string example1RazorCode = @"
<BitToggleButton @bind-IsChecked=""basicIsOpen"" OnText=""Close"" OffText=""Open"" />

<div style=""width:222px"">
    <BitNavPanel @bind-IsOpen=""basicIsOpen"" Items=""basicNavItems"" />
</div>";
    private readonly string example1CsharpCode = @"
private bool basicIsOpen;
" + basicNavItemsCsharpCode;

    private readonly string example2RazorCode = @"
<div>
    <div>FitWidth</div>
    <BitToggleButton @bind-IsChecked=""fitWidthIsOpen"" OnText=""Close"" OffText=""Open"" />
    <BitNavPanel @bind-IsOpen=""fitWidthIsOpen"" Items=""fitWidthNavItems"" FitWidth />
</div>

<div style=""width:260px"">
    <div>FullWidth</div>
    <BitToggleButton @bind-IsChecked=""fullWidthIsOpen"" OnText=""Close"" OffText=""Open"" />
    <BitNavPanel @bind-IsOpen=""fullWidthIsOpen"" Items=""fullWidthNavItems"" FullWidth />
</div>

<div>
    <div>Width / ToggledWidth</div>
    <BitToggleButton @bind-IsChecked=""widthIsOpen"" OnText=""Close"" OffText=""Open"" />
    <BitNavPanel @bind-IsOpen=""widthIsOpen"" Items=""widthNavItems"" Width=""200"" ToggledWidth=""72"" />
</div>";
    private readonly string example2CsharpCode = @"
private bool fitWidthIsOpen;
private bool fullWidthIsOpen;
private bool widthIsOpen;

// one tree per panel: the expanded state of an item lives on the item itself
private List<BitNavItem> fitWidthNavItems = CreateBasicNavItems();
private List<BitNavItem> fullWidthNavItems = CreateBasicNavItems();
private List<BitNavItem> widthNavItems = CreateBasicNavItems();
" + createBasicNavItemsCsharpCode;

    private readonly string example3RazorCode = @"
<div style=""width:222px"">
    <div>ExpandOnHover</div>
    <BitToggleButton @bind-IsChecked=""expandOnHoverIsOpen"" OnText=""Close"" OffText=""Open"" />
    <BitNavPanel @bind-IsOpen=""expandOnHoverIsOpen"" Items=""expandOnHoverNavItems"" ExpandOnHover />
</div>

<div style=""width:222px"">
    <div>NoToggle</div>
    <BitToggleButton @bind-IsChecked=""noToggleIsOpen"" OnText=""Close"" OffText=""Open"" />
    <BitNavPanel @bind-IsOpen=""noToggleIsOpen"" Items=""noToggleNavItems"" NoToggle />
</div>";
    private readonly string example3CsharpCode = @"
private bool expandOnHoverIsOpen;
private bool noToggleIsOpen;

// one tree per panel: the expanded state of an item lives on the item itself
private List<BitNavItem> expandOnHoverNavItems = CreateBasicNavItems();
private List<BitNavItem> noToggleNavItems = CreateBasicNavItems();
" + createBasicNavItemsCsharpCode;

    private readonly string example4RazorCode = @"
<div style=""width:222px"">
    <div>Logo</div>
    <BitToggleButton @bind-IsChecked=""iconUrlIsOpen"" OnText=""Close"" OffText=""Open"" />
    <BitNavPanel @bind-IsOpen=""iconUrlIsOpen""
                 Items=""iconUrlNavItems""
                 IconUrl=""/images/icon.png""
                 IconNavUrl=""https://bitplatform.dev""
                 IconAriaLabel=""bit platform home"" />
</div>

<div style=""width:222px"">
    <div>Logo and title</div>
    <BitToggleButton @bind-IsChecked=""headerTextIsOpen"" OnText=""Close"" OffText=""Open"" />
    <BitNavPanel @bind-IsOpen=""headerTextIsOpen""
                 Items=""headerTextNavItems""
                 IconUrl=""/images/icon.png""
                 IconNavUrl=""https://bitplatform.dev""
                 HeaderText=""BlazorUI"" />
</div>";
    private readonly string example4CsharpCode = @"
private bool iconUrlIsOpen;
private bool headerTextIsOpen;

// one tree per panel: the expanded state of an item lives on the item itself
private List<BitNavItem> iconUrlNavItems = CreateBasicNavItems();
private List<BitNavItem> headerTextNavItems = CreateBasicNavItems();
" + createBasicNavItemsCsharpCode;

    private readonly string example5RazorCode = @"
<BitToggleButton @bind-IsChecked=""searchIsOpen"" OnText=""Close"" OffText=""Open"" />

<BitTextField Label=""Search text"" @bind-Value=""searchText"" Immediate />
<div>Last searched term: <b>@lastSearchedTerm</b></div>

<div style=""width:240px"">
    <BitNavPanel @bind-IsOpen=""searchIsOpen""
                 Items=""searchNavItems""
                 @bind-SearchText=""searchText""
                 SearchDebounceTime=""200""
                 OnSearch=""v => lastSearchedTerm = v""
                 SearchFilter=""(item, term) => item.Text.StartsWith(term, StringComparison.OrdinalIgnoreCase)""
                 SearchBoxPlaceholder=""Search in menu items...""
                 EmptyListMessage=""There is no item found."" />
</div>";
    private readonly string example5CsharpCode = @"
private bool searchIsOpen;
private string? searchText;
private string? lastSearchedTerm;
" + basicNavItemsCsharpCode.Replace("basicNavItems", "searchNavItems");

    private readonly string example6RazorCode = @"
<BitToggleButton @bind-IsChecked=""selectionIsOpen"" OnText=""Close"" OffText=""Open"" />

<div>Selected item: <b>@selectedItem?.Text</b></div>
<BitButton OnClick=""() => selectedItem = selectionNavItems[3]"">Select Settings</BitButton>

<div style=""width:222px"">
    <BitNavPanel @bind-IsOpen=""selectionIsOpen""
                 Items=""selectionNavItems""
                 NavMode=""BitNavMode.Manual""
                 @bind-SelectedItem=""selectedItem""
                 DefaultSelectedItem=""selectionNavItems[0]"" />
</div>";
    private readonly string example6CsharpCode = @"
private bool selectionIsOpen;
private BitNavItem? selectedItem;

// The manual mode reports the clicked item instead of following it, so none of these items carries a URL:
// an item with one is still a link, and the click that selects it would navigate away.
private List<BitNavItem> selectionNavItems =
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

    private readonly string example7RazorCode = @"
<BitToggleButton @bind-IsChecked=""singleExpandIsOpen"" OnText=""Close"" OffText=""Open"" />

<div style=""width:222px"">
    <BitNavPanel @bind-IsOpen=""singleExpandIsOpen"" Items=""singleExpandNavItems"" SingleExpand />
</div>";
    private readonly string example7CsharpCode = @"
private bool singleExpandIsOpen;

private List<BitNavItem> singleExpandNavItems =" + expansionNavItemsBodyCsharpCode;

    private readonly string example8RazorCode = @"
<BitToggleButton @bind-IsChecked=""customIsOpen"" OnText=""Close"" OffText=""Open"" />

<div style=""width:222px"">
    <BitNavPanel @bind-IsOpen=""customIsOpen"" Items=""customNavItems"" NameSelectors=""customSelectors"" />
</div>";
    private readonly string example8CsharpCode = @"
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

    private readonly string example9RazorCode = @"
<div style=""width:222px"">
    <div>Modal, at the end edge</div>
    <BitToggleButton @bind-IsChecked=""drawerIsOpen"" OnText=""Close"" OffText=""Open"" />
    <BitNavPanel @bind-IsOpen=""drawerIsOpen""
                 Items=""drawerNavItems""
                 AutoFocus
                 ShowCloseButton
                 Placement=""BitPlacement.End"" />
</div>

<div style=""width:222px"">
    <div>Non-modal</div>
    <BitToggleButton @bind-IsChecked=""behaviorIsOpen"" OnText=""Close"" OffText=""Open"" />
    <BitNavPanel @bind-IsOpen=""behaviorIsOpen"" Items=""behaviorNavItems"" NoOverlay NoAutoClose NoSwipe />
</div>

<div style=""width:222px"">
    <div>A drawer on every screen</div>
    <BitToggleButton @bind-IsChecked=""alwaysDrawerIsOpen"" OnText=""Close"" OffText=""Open"" />
    <BitNavPanel @bind-IsOpen=""alwaysDrawerIsOpen""
                 Items=""alwaysDrawerNavItems""
                 ShowCloseButton
                 DrawerBreakpoint=""BitNavPanelBreakpoint.Always"" />
</div>";
    private readonly string example9CsharpCode = @"
private bool drawerIsOpen;
private bool behaviorIsOpen;
private bool alwaysDrawerIsOpen;

// one tree per panel: the expanded state of an item lives on the item itself
private List<BitNavItem> drawerNavItems = CreateBasicNavItems();
private List<BitNavItem> behaviorNavItems = CreateBasicNavItems();
private List<BitNavItem> alwaysDrawerNavItems = CreateBasicNavItems();
" + createBasicNavItemsCsharpCode;

    private readonly string example10RazorCode = @"
<BitToggleButton @bind-IsChecked=""templateIsOpen"" OnText=""Close"" OffText=""Open"" />

<BitNavPanel @bind-IsOpen=""templateIsOpen"" Items=""templateNavItems"" FitWidth NoToggle>
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
    private readonly string example10CsharpCode = @"
private bool templateIsOpen;
" + basicNavItemsCsharpCode.Replace("basicNavItems", "templateNavItems");

    private readonly string example11RazorCode = @"
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
    private readonly string example11CsharpCode = @"
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

    private readonly string example12RazorCode = @"
<BitToggleButton @bind-IsChecked=""publicApiIsOpen"" OnText=""Close"" OffText=""Open"" />

<BitButton OnClick=""() => navPanelRef.Toggle()"">Toggle</BitButton>
<BitButton OnClick=""() => navPanelRef.ExpandAll()"">Expand all</BitButton>
<BitButton OnClick=""() => navPanelRef.CollapseAll()"">Collapse all</BitButton>
<BitButton OnClick=""() => navPanelRef.FocusSearchBox()"">Focus search</BitButton>
<BitButton OnClick=""() => navPanelRef.ClearSearch()"">Clear search</BitButton>

<div style=""width:222px"">
    <BitNavPanel @bind-IsOpen=""publicApiIsOpen"" @ref=""navPanelRef"" Items=""publicApiNavItems"" HideToggle />
</div>";
    private readonly string example12CsharpCode = @"
private bool publicApiIsOpen;
private BitNavPanel<BitNavItem> navPanelRef = default!;
" + basicNavItemsCsharpCode.Replace("basicNavItems", "publicApiNavItems");

    private readonly string example13RazorCode = @"
<BitToggleButton @bind-IsChecked=""groupedIsOpen"" OnText=""Close"" OffText=""Open"" />

<div style=""width:240px"">
    <BitNavPanel @bind-IsOpen=""groupedIsOpen""
                 Items=""groupedNavItems""
                 RenderType=""BitNavRenderType.Grouped""
                 IndentValue=""24""
                 ReversedChevron />
</div>";
    private readonly string example13CsharpCode = @"
private bool groupedIsOpen;

private List<BitNavItem> groupedNavItems =" + expansionNavItemsBodyCsharpCode;

    private readonly string example14RazorCode = @"
<BitToggleButton @bind-IsChecked=""stickyIsOpen"" OnText=""Close"" OffText=""Open"" />

<div style=""width:240px"">
    <BitNavPanel @bind-IsOpen=""stickyIsOpen""
                 Items=""stickyNavItems""
                 Style=""height:264px""
                 AllExpanded
                 StickyEnds>
        <Footer>
            <BitActionButton IconName=""@BitIconName.PowerButton"">Logout</BitActionButton>
        </Footer>
    </BitNavPanel>
</div>";
    private readonly string example14CsharpCode = @"
private bool stickyIsOpen;

private List<BitNavItem> stickyNavItems =" + expansionNavItemsBodyCsharpCode;

    private readonly string example15RazorCode = @"
<BitParams Parameters=""navPanelParams"">
    <div style=""width:222px"">
        <div>Cascaded</div>
        <BitToggleButton @bind-IsChecked=""cascadeIsOpen"" OnText=""Close"" OffText=""Open"" />
        <BitNavPanel @bind-IsOpen=""cascadeIsOpen"" Items=""cascadeNavItems"" />
    </div>

    <div style=""width:222px"">
        <div>Own placeholder, cascaded rest</div>
        <BitToggleButton @bind-IsChecked=""cascadeOwnIsOpen"" OnText=""Close"" OffText=""Open"" />
        <BitNavPanel @bind-IsOpen=""cascadeOwnIsOpen"" Items=""cascadeOwnNavItems"" SearchBoxPlaceholder=""Its own placeholder"" />
    </div>
</BitParams>";
    private readonly string example15CsharpCode = @"
private bool cascadeIsOpen;
private bool cascadeOwnIsOpen;

private readonly BitNavPanelParams[] navPanelParams =
[
    new()
    {
        ExpandOnHover = true,
        SearchBoxPlaceholder = ""Find a page..."",
        EmptyListMessage = ""No page matches."",
        ToggleAriaLabel = ""Collapse or expand the menu"",
        SearchAnnouncementProvider = count => count == 1 ? ""One page matches."" : $""{count} pages match."",
    }
];

// one tree per panel: the expanded state of an item lives on the item itself
private List<BitNavItem> cascadeNavItems = CreateBasicNavItems();
private List<BitNavItem> cascadeOwnNavItems = CreateBasicNavItems();
" + createBasicNavItemsCsharpCode;

    private readonly string example16RazorCode = @"
<BitToggleButton @bind-IsChecked=""colorIsOpen"" OnText=""Close"" OffText=""Open"" />

<div style=""width:222px"">
    <BitNavPanel @bind-IsOpen=""colorIsOpen"" Items=""colorNavItems"" Color=""BitColor.Secondary"" Accent=""BitColor.SecondaryBackground"" />
</div>";
    private readonly string example16CsharpCode = @"
private bool colorIsOpen;
" + basicNavItemsCsharpCode.Replace("basicNavItems", "colorNavItems");

    private readonly string example17RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitToggleButton @bind-IsChecked=""externalIconIsOpen"" OnText=""Close"" OffText=""Open"" />

<div style=""width:222px"">
    <BitNavPanel @bind-IsOpen=""externalIconIsOpen""
                 Items=""externalIconNavItems""
                 ToggleIcon=""@BitIconInfo.Fa(""solid bars"")""
                 ChevronDownIcon=""@BitIconInfo.Fa(""solid chevron-down"")"" />
</div>";
    private readonly string example17CsharpCode = @"
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

    private readonly string example18RazorCode = @"
<BitToggleButton @bind-IsChecked=""sizeIsOpen"" OnText=""Close"" OffText=""Open"" />

<div style=""width:180px"">
    <div>Small</div>
    <BitNavPanel @bind-IsOpen=""sizeIsOpen"" Items=""sizeSmallNavItems"" Size=""BitSize.Small"" NoSearchBox NoToggle />
</div>
<div style=""width:180px"">
    <div>Medium</div>
    <BitNavPanel @bind-IsOpen=""sizeIsOpen"" Items=""sizeMediumNavItems"" Size=""BitSize.Medium"" NoSearchBox NoToggle />
</div>
<div style=""width:180px"">
    <div>Large</div>
    <BitNavPanel @bind-IsOpen=""sizeIsOpen"" Items=""sizeLargeNavItems"" Size=""BitSize.Large"" NoSearchBox NoToggle />
</div>";
    private readonly string example18CsharpCode = @"
private bool sizeIsOpen;

// one tree per panel: the expanded state of an item lives on the item itself
private List<BitNavItem> sizeSmallNavItems = CreateBasicNavItems();
private List<BitNavItem> sizeMediumNavItems = CreateBasicNavItems();
private List<BitNavItem> sizeLargeNavItems = CreateBasicNavItems();
" + createBasicNavItemsCsharpCode;

    private readonly string example19RazorCode = @"
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

<div style=""width:222px"">
    <div>Styles &amp; Classes</div>
    <BitToggleButton @bind-IsChecked=""classStyleIsOpen"" OnText=""Close"" OffText=""Open"" />
    <BitNavPanel @bind-IsOpen=""classStyleIsOpen""
                 Items=""classStyleNavItems""
                 Styles=""@(new() { Container = ""background-image: linear-gradient(180deg, rgb(5, 39, 103) 0%, #3a0647 70%);"" })""
                 NavClasses=""@(new() { ItemContainer = ""custom-nav-item"", ItemIcon = ""custom-nav-item-ico"", ItemText = ""custom-nav-item-txt"" })""
                 SearchBoxClasses=""@(new() { Icon = ""custom-icon-searchbox"",
                                             Focused = ""custom-focused-searchbox"",
                                             ClearButton = ""custom-clear-searchbox"",
                                             IconWrapper = ""custom-icon-wrapper-searchbox"",
                                             InputContainer = ""custom-input-container-searchbox"" })"" />
</div>

<div style=""width:222px"">
    <div>CSS variables</div>
    <BitToggleButton @bind-IsChecked=""cssVariablesIsOpen"" OnText=""Close"" OffText=""Open"" />
    <BitNavPanel @bind-IsOpen=""cssVariablesIsOpen""
                 Items=""cssVariablesNavItems""
                 Style=""--bit-NavPanel-background: var(--bit-clr-bg-sec);
                        --bit-NavPanel-border-width: 1px;
                        --bit-NavPanel-radius: 1rem;
                        --bit-NavPanel-shadow: var(--bit-shd-card);
                        --bit-NavPanel-gap: 0.25rem;
                        --bit-NavPanel-toggled-width: 3.5rem;
                        --bit-Nav-item-radius: 999px;"" />
</div>";
    private readonly string example19CsharpCode = @"
private bool classStyleIsOpen;
private bool cssVariablesIsOpen;

// one tree per panel: the expanded state of an item lives on the item itself
private List<BitNavItem> classStyleNavItems = CreateBasicNavItems();
private List<BitNavItem> cssVariablesNavItems = CreateBasicNavItems();
" + createBasicNavItemsCsharpCode;

    private readonly string example20RazorCode = @"
<BitToggleButton @bind-IsChecked=""rtlIsOpen"" OnText=""Close"" OffText=""Open"" />

<div dir=""rtl"">
    <div style=""width:222px"">
        <BitNavPanel @bind-IsOpen=""rtlIsOpen"" Items=""rtlNavItems"" Dir=""BitDir.Rtl"" />
    </div>
</div>";
    private readonly string example20CsharpCode = @"
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

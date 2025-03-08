
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

    private readonly string example3RazorCode = @"
<BitToggleButton @bind-IsChecked=""fullWidthIsOpen"" OnText=""Close"" OffText=""Open"" />

<div style=""width:333px"">
    <BitNavPanel @bind-IsOpen=""fullWidthIsOpen"" Items=""basicNavItems"" FullWidth />
</div>";
    private readonly string example3CsharpCode = @"
private bool fullWidthIsOpen;

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

    private readonly string example4RazorCode = @"
<BitToggleButton @bind-IsChecked=""noToggleIsOpen"" OnText=""Close"" OffText=""Open"" />

<div style=""width:222px"">
    <BitNavPanel @bind-IsOpen=""noToggleIsOpen"" Items=""basicNavItems"" NoToggle />
</div>";
    private readonly string example4CsharpCode = @"
private bool noToggleIsOpen;

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

    private readonly string example5RazorCode = @"
<BitToggleButton @bind-IsChecked=""iconUrlIsOpen"" OnText=""Close"" OffText=""Open"" />

<div style=""width:222px"">
    <BitNavPanel @bind-IsOpen=""iconUrlIsOpen"" Items=""basicNavItems"" IconNavUrl=""https://bitplatform.dev"" IconUrl=""/images/icon.png"" />
</div>";
    private readonly string example5CsharpCode = @"
private bool iconUrlIsOpen;

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

    private readonly string example6RazorCode = @"
<BitToggleButton @bind-IsChecked=""chevronDownIconIsOpen"" OnText=""Close"" OffText=""Open"" />

<div style=""width:222px"">
    <BitNavPanel @bind-IsOpen=""chevronDownIconIsOpen"" Items=""basicNavItems"" ChevronDownIcon=""@BitIconName.ColumnList"" />
</div>";
    private readonly string example6CsharpCode = @"
private bool chevronDownIconIsOpen;

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

    private readonly string example7RazorCode = @"
<BitToggleButton @bind-IsChecked=""reversedChevronIsOpen"" OnText=""Close"" OffText=""Open"" />

<div style=""width:222px"">
    <BitNavPanel @bind-IsOpen=""reversedChevronIsOpen"" Items=""basicNavItems"" ReversedChevron />
</div>";
    private readonly string example7CsharpCode = @"
private bool reversedChevronIsOpen;

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

    private readonly string example8RazorCode = @"
<BitToggleButton @bind-IsChecked=""searchBoxPlaceholderIsOpen"" OnText=""Close"" OffText=""Open"" />

<div style=""width:300px"">
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

    private readonly string example9RazorCode = @"
<BitToggleButton @bind-IsChecked=""emptyListMessageIsOpen"" OnText=""Close"" OffText=""Open"" />

<div style=""width:222px"">
    <BitNavPanel @bind-IsOpen=""emptyListMessageIsOpen"" Items=""basicNavItems"" EmptyListMessage=""There is no such item."" />
</div>";
    private readonly string example9CsharpCode = @"
private bool emptyListMessageIsOpen;

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

    private readonly string example10RazorCode = @"
<BitToggleButton @bind-IsChecked=""singleExpandIsOpen"" OnText=""Close"" OffText=""Open"" />

<BitToggleButton @bind-IsChecked=""isSingleExpand"" OnText=""Single expand"" OffText=""Multi expand"" />

<div style=""width:222px"">
    <BitNavPanel @bind-IsOpen=""singleExpandIsOpen"" Items=""singleExpandNavItems"" SingleExpand=""isSingleExpand"" />
</div>";
    private readonly string example10CsharpCode = @"
private bool singleExpandIsOpen;
private bool isSingleExpand = true;

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

    private readonly string example11RazorCode = @"
<BitToggleButton @bind-IsChecked=""templateIsOpen"" OnText=""Close"" OffText=""Open"" />

<BitNavPanel @bind-IsOpen=""templateIsOpen"" Items=""basicNavItems"" FitWidth NoToggle>
    <Header>
        <BitText Typography=""BitTypography.H4"" Color=""BitColor.Info"">Bit Menu</BitText>
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
    private readonly string example11CsharpCode = @"
private bool templateIsOpen;

private List<BitNavItem> templateNavItems =
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
    private readonly string example12CsharpCode = @"
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
            new() {
                Text = ""Dashboard"",
                IconName = BitIconName.BarChartVerticalFill,
            },
            new() {
                Text = ""Categories"",
                IconName = BitIconName.BuildQueue,
            },
            new() {
                Text = ""Products"",
                IconName = BitIconName.Product,
            }
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

    private readonly string example13RazorCode = @"
<BitToggleButton @bind-IsChecked=""colorIsOpen"" OnText=""Close"" OffText=""Open"" />

<div style=""width:222px"">
    <BitNavPanel @bind-IsOpen=""colorIsOpen"" Items=""colorNavItems"" Color=""BitColor.Secondary"" Accent=""BitColor.SecondaryBackground"" />
</div>";
    private readonly string example13CsharpCode = @"
private bool colorIsOpen;

private List<BitNavItem> colorNavItems =
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

    private readonly string example14RazorCode = @"
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
    private readonly string example14CsharpCode = @"
private bool classStyleIsOpen;

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

    private readonly string example15RazorCode = @"
<BitToggleButton @bind-IsChecked=""rtlIsOpen"" OnText=""Close"" OffText=""Open"" />

<BitNavPanel @bind-IsOpen=""rtlIsOpen"" Items=""rtlNavItems"" FitWidth Dir=""BitDir.Rtl"" />";
    private readonly string example15CsharpCode = @"
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
            new() {
                Text = ""داشبورد"",
                IconName = BitIconName.BarChartVerticalFill,
                Url = ""DashboardPage"",
            },
            new() {
                Text = ""دسته‌ها"",
                IconName = BitIconName.BuildQueue,
                Url = ""CategoriesPage"",
            },
            new() {
                Text = ""کالاها"",
                IconName = BitIconName.Product,
                Url = ""ProductsPage"",
            }
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

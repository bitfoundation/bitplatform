namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.NavBar;

public partial class _BitNavBarCustomDemo
{
    private readonly string example1RazorCode = @"
<BitNavBar Items=""basicNavBarCustoms""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />";
    private readonly string example1CsharpCode = @"
public class MenuItem
{
    public string? Title { get; set; }
    public string? Icon { get; set; }
}

private static readonly List<MenuItem> basicNavBarCustoms =
[
    new() { Title = ""Home"", Icon = BitIconName.Home  },
    new() { Title = ""Products"", Icon = BitIconName.ProductVariant },
    new() { Title = ""Academy"", Icon = BitIconName.LearningTools },
    new() { Title = ""Profile"", Icon = BitIconName.Contact },
];";

    private readonly string example2RazorCode = @"
<BitNavBar Items=""basicNavBarCustoms"" IsEnabled=""false""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />

<BitNavBar Items=""basicNavBarCustomsDisabled""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon },
                                    IsEnabled = { Selector = item => item.Disabled is false } })"" />";
    private readonly string example2CsharpCode = @"
public class MenuItem
{
    public string? Title { get; set; }
    public string? Icon { get; set; }
    public bool Disabled { get; set; }
}

private static readonly List<MenuItem> basicNavBarCustoms =
[
    new() { Title = ""Home"", Icon = BitIconName.Home  },
    new() { Title = ""Products"", Icon = BitIconName.ProductVariant },
    new() { Title = ""Academy"", Icon = BitIconName.LearningTools },
    new() { Title = ""Profile"", Icon = BitIconName.Contact },
];

private static readonly List<MenuItem> basicNavBarCustomsDisabled =
[
    new() { Title = ""Home"", Icon = BitIconName.Home  },
    new() { Title = ""Products"", Icon = BitIconName.ProductVariant },
    new() { Title = ""Academy"", Icon = BitIconName.LearningTools, Disabled = true },
    new() { Title = ""Profile"", Icon = BitIconName.Contact },
];";

    private readonly string example3RazorCode = @"
<BitNavBar Mode=""BitNavMode.Manual""
           Items=""basicNavBarCustoms""
           DefaultSelectedItem=""basicNavBarCustoms[0]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />";
    private readonly string example3CsharpCode = @"
public class MenuItem
{
    public string? Title { get; set; }
    public string? Icon { get; set; }
}

private static readonly List<MenuItem> basicNavBarCustoms =
[
    new() { Title = ""Home"", Icon = BitIconName.Home  },
    new() { Title = ""Products"", Icon = BitIconName.ProductVariant },
    new() { Title = ""Academy"", Icon = BitIconName.LearningTools },
    new() { Title = ""Profile"", Icon = BitIconName.Contact },
];";

    private readonly string example4RazorCode = @"
<BitNavBar IconOnly
           Items=""basicNavBarCustoms""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />";
    private readonly string example4CsharpCode = @"
public class MenuItem
{
    public string? Title { get; set; }
    public string? Icon { get; set; }
}

private static readonly List<MenuItem> basicNavBarCustoms =
[
    new() { Title = ""Home"", Icon = BitIconName.Home  },
    new() { Title = ""Products"", Icon = BitIconName.ProductVariant },
    new() { Title = ""Academy"", Icon = BitIconName.LearningTools },
    new() { Title = ""Profile"", Icon = BitIconName.Contact },
];";

    private readonly string example5RazorCode = @"
<BitNavBar FitWidth
           Items=""basicNavBarCustoms""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />";
    private readonly string example5CsharpCode = @"
public class MenuItem
{
    public string? Title { get; set; }
    public string? Icon { get; set; }
}

private static readonly List<MenuItem> basicNavBarCustoms =
[
    new() { Title = ""Home"", Icon = BitIconName.Home  },
    new() { Title = ""Products"", Icon = BitIconName.ProductVariant },
    new() { Title = ""Academy"", Icon = BitIconName.LearningTools },
    new() { Title = ""Profile"", Icon = BitIconName.Contact },
];";

    private readonly string example6RazorCode = @"
<BitNavBar Items=""basicNavBarCustoms""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"">
    <ItemTemplate Context=""custom"">
        <BitText Typography=""BitTypography.Caption1"" Color=""BitColor.Warning"">@custom.Title</BitText>
        <BitIcon IconName=""@custom.Icon"" Color=""BitColor.Success"" />
    </ItemTemplate>
</BitNavBar>

<BitNavBar Items=""templateNavBarCustoms""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon },
                                    Template = { Selector = item => item.Fragment } })"" />

@code {
    private static readonly List<MenuItem> templateNavBarCustoms =
    [
        new() { Title = ""Home"", Icon = BitIconName.Home  },
        new() { Title = ""Products"", Fragment = (item) => @<div style=""display:flex;flex-direction:column""><b>@item.Title</b><span>🎁</span></div> },
        new() { Title = ""Academy"", Icon = BitIconName.LearningTools },
        new() { Title = ""Profile"", Icon = BitIconName.Contact },
    ];
}";
    private readonly string example6CsharpCode = @"
public class MenuItem
{
    public string? Title { get; set; }
    public string? Icon { get; set; }
    public RenderFragment<MenuItem>? Fragment { get; set; }
}

private static readonly List<MenuItem> basicNavBarCustoms =
[
    new() { Title = ""Home"", Icon = BitIconName.Home  },
    new() { Title = ""Products"", Icon = BitIconName.ProductVariant },
    new() { Title = ""Academy"", Icon = BitIconName.LearningTools },
    new() { Title = ""Profile"", Icon = BitIconName.Contact },
];";

    private readonly string example7RazorCode = @"
<BitNavBar Items=""basicNavBarCustoms""
           OnItemClick=""(MenuItem item) => eventsClickedItem = item""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />

Clicked item: @eventsClickedItem?.Title";
    private readonly string example7CsharpCode = @"
private MenuItem? eventsClickedItem;

public class MenuItem
{
    public string? Title { get; set; }
    public string? Icon { get; set; }
}

private static readonly List<MenuItem> basicNavBarCustoms =
[
    new() { Title = ""Home"", Icon = BitIconName.Home  },
    new() { Title = ""Products"", Icon = BitIconName.ProductVariant },
    new() { Title = ""Academy"", Icon = BitIconName.LearningTools },
    new() { Title = ""Profile"", Icon = BitIconName.Contact },
];";

    private readonly string example8RazorCode = @"
<BitNavBar @bind-SelectedItem=""selectedItem""
            Items=""basicNavBarCustoms""
            Mode=""BitNavMode.Manual""
            DefaultSelectedItem=""basicNavBarCustoms[1]""
            NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />
Selected item: @selectedItem.Title

<BitNavBar Items=""basicNavBarCustoms""
           Mode=""BitNavMode.Manual""
           @bind-SelectedItem=""twoWaySelectedItem""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />
<BitChoiceGroup Horizontal Items=""@choiceGroupItems"" @bind-Value=""@twoWaySelectedItem"" />";
private readonly string example8CsharpCode = @"
private MenuItem selectedItem = basicNavBarCustoms[0];
private MenuItem twoWaySelectedItem = basicNavBarCustoms[0];

private static IEnumerable<BitChoiceGroupItem<MenuItem>> choiceGroupItems =
         basicNavBarCustoms.Select(i => new BitChoiceGroupItem<MenuItem>() { Id = i.Title, Text = i.Title, IsEnabled = true, Value = i });

public class MenuItem
{
    public string? Title { get; set; }
    public string? Icon { get; set; }
}

private static readonly List<MenuItem> basicNavBarCustoms =
[
    new() { Title = ""Home"", Icon = BitIconName.Home  },
    new() { Title = ""Products"", Icon = BitIconName.ProductVariant },
    new() { Title = ""Academy"", Icon = BitIconName.LearningTools },
    new() { Title = ""Profile"", Icon = BitIconName.Contact },
];";

    private readonly string example9RazorCode = @"
<BitToggle @bind-Value=""reselectable"" OnText=""Enabled recalling"" OffText=""Disabled recalling"" />

<BitNavBar Items=""basicNavBarCustoms""
           Mode=""BitNavMode.Manual""
           Reselectable=""reselectable""
           OnItemClick=""(MenuItem item) => countClick++""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />

Item click count: @countClick";
    private readonly string example9CsharpCode = @"
private int countClick;
private bool reselectable = true;

public class MenuItem
{
    public string? Title { get; set; }
    public string? Icon { get; set; }
}

private static readonly List<MenuItem> basicNavBarCustoms =
[
    new() { Title = ""Home"", Icon = BitIconName.Home  },
    new() { Title = ""Products"", Icon = BitIconName.ProductVariant },
    new() { Title = ""Academy"", Icon = BitIconName.LearningTools },
    new() { Title = ""Profile"", Icon = BitIconName.Contact },
];";

    private readonly string example10RazorCode = @"
<style>
    .mobile-frame {
        width: 375px;
        height: 712px;
        border: 16px solid #333;
        border-radius: 36px;
        background-color: #fff;
        box-shadow: 0 0 10px rgba(0, 0, 0, 0.1);
        position: relative;
        overflow: hidden;
    }

    .mobile-frame::after {
        content: '';
        display: block;
        width: 60px;
        height: 5px;
        background: #333;
        border-radius: 10px;
        position: absolute;
        top: 10px;
        left: 50%;
        transform: translateX(-50%);
    }

    .screen {
        width: 100%;
        height: 100%;
        overflow: auto;
    }

    .nav-menu {
        background-color: #101419;
    }
</style>

<div class=""mobile-frame"">
    <div class=""screen"">
        <BitLayout>
            <Header>
                <BitCard FullWidth>
                    <BitStack Horizontal HorizontalAlign=""BitAlignment.Center"" VerticalAlign=""BitAlignment.Center"">
                        <BitImage Src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo.svg"" Width=""50"" />
                        <BitText Typography=""BitTypography.H4"" Color=""BitColor.Info"">
                            bit BlazorUI
                        </BitText>
                    </BitStack>
                </BitCard>
            </Header>
            <Main>
                <BitStack HorizontalAlign=""BitAlignment.Center"" VerticalAlign=""BitAlignment.Center"">
                    <BitText Typography=""BitTypography.H4"" Color=""BitColor.PrimaryForeground"">
                        <BitIcon IconName=""@advancedSelectedItem?.Icon"" Color=""BitColor.PrimaryForeground"" Size=""BitSize.Large"" />
                        <span>@advancedSelectedItem?.Title</span>
                    </BitText>
                </BitStack>
            </Main>
            <Footer>
                <div class=""nav-menu"">
                    <BitNavBar Mode=""BitNavMode.Manual""
                               Items=""basicNavBarCustoms""
                               @bind-SelectedItem=""advancedSelectedItem""
                               NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                                        IconName = { Selector = item => item.Icon } })"" />
                </div>
            </Footer>
        </BitLayout>
    </div>
</div>";
    private readonly string example10CsharpCode = @"
private MenuItem advancedSelectedItem = basicNavBarCustoms[1];

public class MenuItem
{
    public string? Title { get; set; }
    public string? Icon { get; set; }
}

private static readonly List<MenuItem> basicNavBarCustoms =
[
    new() { Title = ""Home"", Icon = BitIconName.Home  },
    new() { Title = ""Products"", Icon = BitIconName.ProductVariant },
    new() { Title = ""Academy"", Icon = BitIconName.LearningTools },
    new() { Title = ""Profile"", Icon = BitIconName.Contact },
];";

    private readonly string example11RazorCode = @"
<BitNavBar Color=""BitColor.Primary""
           Items=""basicNavBarCustoms""
           DefaultSelectedItem=""basicNavBarCustoms[0]""
           Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />
<BitNavBar Color=""BitColor.Secondary""
           Items=""basicNavBarCustoms""
           DefaultSelectedItem=""basicNavBarCustoms[0]""
           Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />
<BitNavBar Color=""BitColor.Tertiary""
           Items=""basicNavBarCustoms""
           DefaultSelectedItem=""basicNavBarCustoms[0]""
           Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />
<BitNavBar Color=""BitColor.Info""
           Items=""basicNavBarCustoms""
           DefaultSelectedItem=""basicNavBarCustoms[0]""
           Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />
<BitNavBar Color=""BitColor.Success""
           Items=""basicNavBarCustoms""
           DefaultSelectedItem=""basicNavBarCustoms[0]""
           Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />
<BitNavBar Color=""BitColor.Warning""
           Items=""basicNavBarCustoms""
           DefaultSelectedItem=""basicNavBarCustoms[0]""
           Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />
<BitNavBar Color=""BitColor.SevereWarning""
           Items=""basicNavBarCustoms""
           DefaultSelectedItem=""basicNavBarCustoms[0]""
           Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />
<BitNavBar Color=""BitColor.Error""
           Items=""basicNavBarCustoms""
           DefaultSelectedItem=""basicNavBarCustoms[0]""
           Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />

<BitNavBar Color=""BitColor.PrimaryBackground""
           Items=""basicNavBarCustoms""
           DefaultSelectedItem=""basicNavBarCustoms[0]""
           Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                IconName = { Selector = item => item.Icon } })"" />
<BitNavBar Color=""BitColor.SecondaryBackground""
           Items=""basicNavBarCustoms""
           DefaultSelectedItem=""basicNavBarCustoms[0]""
           Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                IconName = { Selector = item => item.Icon } })"" />
<BitNavBar Color=""BitColor.TertiaryBackground""
           Items=""basicNavBarCustoms""
           DefaultSelectedItem=""basicNavBarCustoms[0]""
           Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                IconName = { Selector = item => item.Icon } })"" />

<BitNavBar Color=""BitColor.PrimaryForeground""
            Items=""basicNavBarCustoms""
            DefaultSelectedItem=""basicNavBarCustoms[0]""
            Mode=""BitNavMode.Manual""
            NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />
<BitNavBar Color=""BitColor.SecondaryForeground""
           Items=""basicNavBarCustoms""
           DefaultSelectedItem=""basicNavBarCustoms[0]""
           Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />
<BitNavBar Color=""BitColor.TertiaryForeground""
           Items=""basicNavBarCustoms""
           DefaultSelectedItem=""basicNavBarCustoms[0]""
           Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />
<BitNavBar Color=""BitColor.PrimaryBorder""
           Items=""basicNavBarCustoms""
           DefaultSelectedItem=""basicNavBarCustoms[0]""
           Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />
<BitNavBar Color=""BitColor.SecondaryBorder""
           Items=""basicNavBarCustoms""
           DefaultSelectedItem=""basicNavBarCustoms[0]""
           Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />
<BitNavBar Color=""BitColor.TertiaryBorder""
           Items=""basicNavBarCustoms""
           DefaultSelectedItem=""basicNavBarCustoms[0]""
           Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />";
    private readonly string example11CsharpCode = @"
public class MenuItem
{
    public string? Title { get; set; }
    public string? Icon { get; set; }
}

private static readonly List<MenuItem> basicNavBarCustoms =
[
    new() { Title = ""Home"", Icon = BitIconName.Home  },
    new() { Title = ""Products"", Icon = BitIconName.ProductVariant },
    new() { Title = ""Academy"", Icon = BitIconName.LearningTools },
    new() { Title = ""Profile"", Icon = BitIconName.Contact },
];";

    private readonly string example12RazorCode = @"
<style>
    .custom-class {
        margin: 1rem;
        border-radius: 1rem;
        box-shadow: aqua 0 0 1rem;
        background: linear-gradient(90deg, magenta, transparent) blue;
    }

    .custom-item {
        color: #ff7800;
        font-weight: 600;
    }

    .custom-item-ico {
        font-weight: bold;
        color: darkmagenta;
    }

    .custom-item-txt {
        font-weight: bold;
        font-style: italic;
    }
</style>

<BitNavBar Items=""basicNavBarCustoms""
           Style=""border-radius: 1rem; margin: 1rem; box-shadow: tomato 0 0 1rem;""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />
<BitNavBar Items=""basicNavBarCustoms""
           Class=""custom-class""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />

<BitNavBar Items=""basicNavBarCustomsClassStyle""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon },
                                    Class = { Selector = item => item.CssClass },
                                    Style = { Selector = item => item.Style }})"" />

<BitNavBar Items=""basicNavBarCustoms""
           Styles=""@(new() { ItemIcon = ""color: aqua;"", ItemText = ""color: tomato;"" })""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />
<BitNavBar Items=""basicNavBarCustoms""
           Classes=""@(new() { ItemIcon = ""custom-item-ico"", ItemText = ""custom-item-txt"" })""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />";
    private readonly string example12CsharpCode = @"
public class MenuItem
{
    public string? Title { get; set; }
    public string? Icon { get; set; }
    public string? CssClass { get; set; }
    public string? Style { get; set; }
}

private static readonly List<MenuItem> basicNavBarCustoms =
[
    new() { Title = ""Home"", Icon = BitIconName.Home  },
    new() { Title = ""Products"", Icon = BitIconName.ProductVariant },
    new() { Title = ""Academy"", Icon = BitIconName.LearningTools },
    new() { Title = ""Profile"", Icon = BitIconName.Contact },
];

private static readonly List<MenuItem> basicNavBarCustomsClassStyle =
[
    new() { Title = ""Home"", Icon = BitIconName.Home  },
    new() { Title = ""Products"", Icon = BitIconName.ProductVariant, CssClass = ""custom-item"" },
    new() { Title = ""Academy"", Icon = BitIconName.LearningTools, Style = ""color: #b6ff00;font-weight: 600;"" },
    new() { Title = ""Profile"", Icon = BitIconName.Contact },
];";


    private readonly string example13RazorCode = @"
<BitNavBar Dir=""BitDir.Rtl""
           Items=""rtlCustomsItems""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />";
    private readonly string example13CsharpCode = @"
private static readonly List<MenuItem> rtlCustomsItems =
[
    new() { Title = ""خانه"", Icon = BitIconName.Home  },
    new() { Title = ""محصولات"", Icon = BitIconName.ProductVariant },
    new() { Title = ""آکادمی"", Icon = BitIconName.LearningTools },
    new() { Title = ""پروفایل"", Icon = BitIconName.Contact },
];";
}

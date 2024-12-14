namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.NavBar;

public partial class _BitNavBarItemDemo
{
    private readonly string example1RazorCode = @"
<BitNavBar Items=""basicNavBarItems"" />";
    private readonly string example1CsharpCode = @"
private static readonly List<BitNavBarItem> basicNavBarItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Products"", IconName = BitIconName.ProductVariant },
    new() { Text = ""Academy"", IconName = BitIconName.LearningTools },
    new() { Text = ""Profile"", IconName = BitIconName.Contact },
];";

    private readonly string example2RazorCode = @"
<BitNavBar Items=""basicNavBarItems"" IsEnabled=""false"" />

<BitNavBar Items=""basicNavBarItemsDisabled"" />";
    private readonly string example2CsharpCode = @"
private static readonly List<BitNavBarItem> basicNavBarItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Products"", IconName = BitIconName.ProductVariant },
    new() { Text = ""Academy"", IconName = BitIconName.LearningTools },
    new() { Text = ""Profile"", IconName = BitIconName.Contact },
];

private static readonly List<BitNavBarItem> basicNavBarItemsDisabled =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Products"", IconName = BitIconName.ProductVariant },
    new() { Text = ""Academy"", IconName = BitIconName.LearningTools, IsEnabled = false },
    new() { Text = ""Profile"", IconName = BitIconName.Contact },
];";

    private readonly string example3RazorCode = @"
<BitNavBar Mode=""BitNavMode.Manual""
           Items=""basicNavBarItems""
           DefaultSelectedItem=""basicNavBarItems[0]"" />";
    private readonly string example3CsharpCode = @"
private static readonly List<BitNavBarItem> basicNavBarItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Products"", IconName = BitIconName.ProductVariant },
    new() { Text = ""Academy"", IconName = BitIconName.LearningTools },
    new() { Text = ""Profile"", IconName = BitIconName.Contact },
];";

    private readonly string example4RazorCode = @"
<BitNavBar Items=""basicNavBarItems"" IconOnly />";
    private readonly string example4CsharpCode = @"
private static readonly List<BitNavBarItem> basicNavBarItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Products"", IconName = BitIconName.ProductVariant },
    new() { Text = ""Academy"", IconName = BitIconName.LearningTools },
    new() { Text = ""Profile"", IconName = BitIconName.Contact },
];";

    private readonly string example5RazorCode = @"
<BitNavBar Items=""basicNavBarItems"" FitWidth />";
    private readonly string example5CsharpCode = @"
private static readonly List<BitNavBarItem> basicNavBarItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Products"", IconName = BitIconName.ProductVariant },
    new() { Text = ""Academy"", IconName = BitIconName.LearningTools },
    new() { Text = ""Profile"", IconName = BitIconName.Contact },
];";

    private readonly string example6RazorCode = @"
<BitNavBar Items=""basicNavBarItems"">
    <ItemTemplate Context=""item"">
        <BitText Typography=""BitTypography.Caption1"" Color=""BitColor.Warning"">@item.Text</BitText>
        <BitIcon IconName=""@item.IconName"" Color=""BitColor.Success"" />
    </ItemTemplate>
</BitNavBar>

<BitNavBar Items=""templateNavBarItems"" />

@code {
    private static readonly List<BitNavBarItem> templateNavBarItems =
    [
        new() { Text = ""Home"", IconName = BitIconName.Home  },
        new() { Text = ""Products"", Template = (item) => @<div style=""display:flex;flex-direction:column""><b>@item.Text</b><span>🎁</span></div> },
        new() { Text = ""Academy"", IconName = BitIconName.LearningTools },
        new() { Text = ""Profile"", IconName = BitIconName.Contact },
    ];
}";
    private readonly string example6CsharpCode = @"
private static readonly List<BitNavBarItem> basicNavBarItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Products"", IconName = BitIconName.ProductVariant },
    new() { Text = ""Academy"", IconName = BitIconName.LearningTools },
    new() { Text = ""Profile"", IconName = BitIconName.Contact },
];";

    private readonly string example7RazorCode = @"
<BitNavBar Items=""basicNavBarItems"" OnItemClick=""(BitNavBarItem item) => eventsClickedItem = item"" />

Clicked item: @eventsClickedItem?.Text";
    private readonly string example7CsharpCode = @"
private BitNavBarItem? eventsClickedItem;

private static readonly List<BitNavBarItem> basicNavBarItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Products"", IconName = BitIconName.ProductVariant },
    new() { Text = ""Academy"", IconName = BitIconName.LearningTools },
    new() { Text = ""Profile"", IconName = BitIconName.Contact },
];";

    private readonly string example8RazorCode = @"
<BitNavBar Items=""basicNavBarItems"" Mode=""BitNavMode.Manual"" @bind-SelectedItem=""selectedItem"" DefaultSelectedItem=""basicNavBarItems[1]"" />
Selected item: @selectedItem.Text

<BitNavBar Items=""basicNavBarItems"" Mode=""BitNavMode.Manual"" @bind-SelectedItem=""twoWaySelectedItem"" />
<BitChoiceGroup Horizontal Items=""@choiceGroupItems"" @bind-Value=""@twoWaySelectedItem"" />";
    private readonly string example8CsharpCode = @"
private BitNavBarItem selectedItem = basicNavBarItems[0];
private BitNavBarItem twoWaySelectedItem = basicNavBarItems[0];

private static IEnumerable<BitChoiceGroupItem<BitNavBarItem>> choiceGroupItems =
         basicNavBarItems.Select(i => new BitChoiceGroupItem<BitNavBarItem>() { Id = i.Text, Text = i.Text, IsEnabled = i.IsEnabled, Value = i });

private static readonly List<BitNavBarItem> basicNavBarItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Products"", IconName = BitIconName.ProductVariant },
    new() { Text = ""Academy"", IconName = BitIconName.LearningTools },
    new() { Text = ""Profile"", IconName = BitIconName.Contact },
];";

    private readonly string example9RazorCode = @"
<BitToggle @bind-Value=""reselectable"" OnText=""Enabled recalling"" OffText=""Disabled recalling"" />

<BitNavBar Items=""basicNavBarItems""
           Mode=""BitNavMode.Manual""
           Reselectable=""reselectable""
           OnItemClick=""(BitNavBarItem item) => countClick++"" />

Item click count: @countClick";
    private readonly string example9CsharpCode = @"
private int countClick;
private bool reselectable = true;

private static readonly List<BitNavBarItem> basicNavBarItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Products"", IconName = BitIconName.ProductVariant },
    new() { Text = ""Academy"", IconName = BitIconName.LearningTools },
    new() { Text = ""Profile"", IconName = BitIconName.Contact },
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
                        <BitIcon IconName=""@advancedSelectedItem.IconName"" Color=""BitColor.PrimaryForeground"" Size=""BitSize.Large"" />
                        <span>@advancedSelectedItem.Text</span>
                    </BitText>
                </BitStack>
            </Main>
            <Footer>
                <div class=""nav-menu"">
                    <BitNavBar Items=""basicNavBarItems""
                               Mode=""BitNavMode.Manual""
                               @bind-SelectedItem=""advancedSelectedItem"" />
                </div>
            </Footer>
        </BitLayout>
    </div>
</div>";
    private readonly string example10CsharpCode = @"
private BitNavBarItem advancedSelectedItem = basicNavBarItems[1];

private static readonly List<BitNavBarItem> basicNavBarItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Products"", IconName = BitIconName.ProductVariant },
    new() { Text = ""Academy"", IconName = BitIconName.LearningTools },
    new() { Text = ""Profile"", IconName = BitIconName.Contact },
];";

    private readonly string example11RazorCode = @"
<BitNavBar Color=""BitColor.Primary"" Items=""basicNavBarItems"" DefaultSelectedItem=""basicNavBarItems[0]"" Mode=""BitNavMode.Manual"" />
<BitNavBar Color=""BitColor.Secondary"" Items=""basicNavBarItems"" DefaultSelectedItem=""basicNavBarItems[0]"" Mode=""BitNavMode.Manual"" />
<BitNavBar Color=""BitColor.Tertiary"" Items=""basicNavBarItems"" DefaultSelectedItem=""basicNavBarItems[0]"" Mode=""BitNavMode.Manual"" />
<BitNavBar Color=""BitColor.Info"" Items=""basicNavBarItems"" DefaultSelectedItem=""basicNavBarItems[0]"" Mode=""BitNavMode.Manual"" />
<BitNavBar Color=""BitColor.Success"" Items=""basicNavBarItems"" DefaultSelectedItem=""basicNavBarItems[0]"" Mode=""BitNavMode.Manual"" />
<BitNavBar Color=""BitColor.Warning"" Items=""basicNavBarItems"" DefaultSelectedItem=""basicNavBarItems[0]"" Mode=""BitNavMode.Manual"" />
<BitNavBar Color=""BitColor.SevereWarning"" Items=""basicNavBarItems"" DefaultSelectedItem=""basicNavBarItems[0]"" Mode=""BitNavMode.Manual"" />
<BitNavBar Color=""BitColor.Error"" Items=""basicNavBarItems"" DefaultSelectedItem=""basicNavBarItems[0]"" Mode=""BitNavMode.Manual"" />

<BitNavBar Color=""BitColor.PrimaryBackground"" Items=""basicNavBarItems"" DefaultSelectedItem=""basicNavBarItems[0]"" Mode=""BitNavMode.Manual"" />
<BitNavBar Color=""BitColor.SecondaryBackground"" Items=""basicNavBarItems"" DefaultSelectedItem=""basicNavBarItems[0]"" Mode=""BitNavMode.Manual"" />
<BitNavBar Color=""BitColor.TertiaryBackground"" Items=""basicNavBarItems"" DefaultSelectedItem=""basicNavBarItems[0]"" Mode=""BitNavMode.Manual"" />

<BitNavBar Color=""BitColor.PrimaryForeground"" Items=""basicNavBarItems"" DefaultSelectedItem=""basicNavBarItems[0]"" Mode=""BitNavMode.Manual"" />
<BitNavBar Color=""BitColor.SecondaryForeground"" Items=""basicNavBarItems"" DefaultSelectedItem=""basicNavBarItems[0]"" Mode=""BitNavMode.Manual"" />
<BitNavBar Color=""BitColor.TertiaryForeground"" Items=""basicNavBarItems"" DefaultSelectedItem=""basicNavBarItems[0]"" Mode=""BitNavMode.Manual"" />
<BitNavBar Color=""BitColor.PrimaryBorder"" Items=""basicNavBarItems"" DefaultSelectedItem=""basicNavBarItems[0]"" Mode=""BitNavMode.Manual"" />
<BitNavBar Color=""BitColor.SecondaryBorder"" Items=""basicNavBarItems"" DefaultSelectedItem=""basicNavBarItems[0]"" Mode=""BitNavMode.Manual"" />
<BitNavBar Color=""BitColor.TertiaryBorder"" Items=""basicNavBarItems"" DefaultSelectedItem=""basicNavBarItems[0]"" Mode=""BitNavMode.Manual"" />";
    private readonly string example11CsharpCode = @"
private static readonly List<BitNavBarItem> basicNavBarItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Products"", IconName = BitIconName.ProductVariant },
    new() { Text = ""Academy"", IconName = BitIconName.LearningTools },
    new() { Text = ""Profile"", IconName = BitIconName.Contact },
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

<BitNavBar Items=""basicNavBarItems"" Style=""border-radius: 1rem; margin: 1rem; box-shadow: tomato 0 0 1rem;"" />
<BitNavBar Items=""basicNavBarItems"" Class=""custom-class"" />

<BitNavBar Items=""styleClassItems"" />

<BitNavBar Items=""basicNavBarItems"" Styles=""@(new() { ItemIcon = ""color: aqua;"", ItemText = ""color: tomato;"" })"" />
<BitNavBar Items=""basicNavBarItems"" Classes=""@(new() { ItemIcon = ""custom-item-ico"", ItemText = ""custom-item-txt"" })"" />";
    private readonly string example12CsharpCode = @"
private static readonly List<BitNavBarItem> basicNavBarItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Products"", IconName = BitIconName.ProductVariant },
    new() { Text = ""Academy"", IconName = BitIconName.LearningTools },
    new() { Text = ""Profile"", IconName = BitIconName.Contact },
];

private static readonly List<BitNavBarItem> styleClassItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Products"", IconName = BitIconName.ProductVariant, Class = ""custom-item"" },
    new() { Text = ""Academy"", IconName = BitIconName.LearningTools, Style = ""color: #b6ff00;font-weight: 600;"" },
    new() { Text = ""Profile"", IconName = BitIconName.Contact },
];";


    private readonly string example13RazorCode = @"
<BitNavBar Dir=""BitDir.Rtl"" Items=""rtlItems"" />";
    private readonly string example13CsharpCode = @"
private static readonly List<BitNavBarItem> rtlItems =
[
    new() { Text = ""خانه"", IconName = BitIconName.Home  },
    new() { Text = ""محصولات"", IconName = BitIconName.ProductVariant },
    new() { Text = ""آکادمی"", IconName = BitIconName.LearningTools },
    new() { Text = ""پروفایل"", IconName = BitIconName.Contact },
];";
}

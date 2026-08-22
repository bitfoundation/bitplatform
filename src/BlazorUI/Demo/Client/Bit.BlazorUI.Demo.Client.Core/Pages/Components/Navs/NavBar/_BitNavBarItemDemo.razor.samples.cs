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
<BitNavBar Items=""exactMatchItems"" />

<BitNavBar Items=""prefixMatchItems"" Match=""BitNavMatch.Prefix"" />

<BitNavBar Items=""patternMatchItems"" />

<BitNavBar Items=""additionalUrlsItems"" />";
    private readonly string example4CsharpCode = @"
private static readonly List<BitNavBarItem> exactMatchItems =
[
    new() { Text = ""NavBar"", IconName = BitIconName.GlobalNavButton, Url = ""/components/navbar"" },
    new() { Text = ""Nav"", IconName = BitIconName.BulletedList, Url = ""/components/nav"" },
];

private static readonly List<BitNavBarItem> prefixMatchItems =
[
    new() { Text = ""Components"", IconName = BitIconName.F12DevTools, Url = ""/components"" },
    new() { Text = ""Iconography"", IconName = BitIconName.AppIconDefault, Url = ""/iconography"" },
];

// The URL of a Wildcard or a Regex item is a pattern rather than a route, so these items are disabled:
// they still light up on a match, but a click cannot navigate to a URL no page answers.
private static readonly List<BitNavBarItem> patternMatchItems =
[
    new() { Text = ""/components/*"", IconName = BitIconName.F12DevTools, Url = ""/components/*"", Match = BitNavMatch.Wildcard, IsEnabled = false },
    new() { Text = ""^/components/b"", IconName = BitIconName.Code, Url = ""^/components/b"", Match = BitNavMatch.Regex, IsEnabled = false },
];

private static readonly List<BitNavBarItem> additionalUrlsItems =
[
    new() { Text = ""Navs"", IconName = BitIconName.GlobalNavButton, Url = ""/components/nav"", AdditionalUrls = [""/components/navbar"", ""/components/breadcrumb""] },
    new() { Text = ""Buttons"", IconName = BitIconName.ButtonControl, Url = ""/components/button"", AdditionalUrls = [""/components/togglebutton""] },
];";

    private readonly string example5RazorCode = @"
<BitNavBar Items=""basicNavBarItems"" IconOnly />";
    private readonly string example5CsharpCode = @"
private static readonly List<BitNavBarItem> basicNavBarItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Products"", IconName = BitIconName.ProductVariant },
    new() { Text = ""Academy"", IconName = BitIconName.LearningTools },
    new() { Text = ""Profile"", IconName = BitIconName.Contact },
];";

    private readonly string example6RazorCode = @"
<BitNavBar HideUnselectedText
           Mode=""BitNavMode.Manual""
           Items=""basicNavBarItems""
           DefaultSelectedItem=""basicNavBarItems[0]"" />";
    private readonly string example6CsharpCode = @"
private static readonly List<BitNavBarItem> basicNavBarItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Products"", IconName = BitIconName.ProductVariant },
    new() { Text = ""Academy"", IconName = BitIconName.LearningTools },
    new() { Text = ""Profile"", IconName = BitIconName.Contact },
];";

    private readonly string example7RazorCode = @"
<BitNavBar Items=""basicNavBarItems"" InlineText />

<BitNavBar Items=""basicNavBarItems"" Vertical FitWidth />

<BitNavBar Items=""basicNavBarItems"" Vertical InlineText FitWidth />";
    private readonly string example7CsharpCode = @"
private static readonly List<BitNavBarItem> basicNavBarItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Products"", IconName = BitIconName.ProductVariant },
    new() { Text = ""Academy"", IconName = BitIconName.LearningTools },
    new() { Text = ""Profile"", IconName = BitIconName.Contact },
];";

    private readonly string example8RazorCode = @"
<BitNavBar Items=""basicNavBarItems"" FitWidth />

<BitNavBar Items=""basicNavBarItems"" FullWidth />

<BitNavBar Items=""unevenNavBarItems"" />
<BitNavBar Items=""unevenNavBarItems"" Justified />";
    private readonly string example8CsharpCode = @"
private static readonly List<BitNavBarItem> basicNavBarItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Products"", IconName = BitIconName.ProductVariant },
    new() { Text = ""Academy"", IconName = BitIconName.LearningTools },
    new() { Text = ""Profile"", IconName = BitIconName.Contact },
];

private static readonly List<BitNavBarItem> unevenNavBarItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Products & services"", IconName = BitIconName.ProductVariant },
    new() { Text = ""Academy"", IconName = BitIconName.LearningTools },
    new() { Text = ""Me"", IconName = BitIconName.Contact },
];";

    private readonly string example9RazorCode = @"
<BitNavBar Items=""badgeNavBarItems"" />";
    private readonly string example9CsharpCode = @"
private static readonly List<BitNavBarItem> badgeNavBarItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Inbox"", IconName = BitIconName.Mail, Badge = ""12"" },
    new() { Text = ""Alerts"", IconName = BitIconName.Ringer, Badge = ""99+"", BadgeAriaLabel = ""more than 99 unread alerts"" },
    new() { Text = ""Profile"", IconName = BitIconName.Contact, Dot = true, BadgeAriaLabel = ""needs attention"" },
];";

    private readonly string example10RazorCode = @"
<BitNavBar Accent=""BitColor.Primary"" Items=""basicNavBarItems"" Mode=""BitNavMode.Manual"" DefaultSelectedItem=""basicNavBarItems[0]"" />
<BitNavBar Accent=""BitColor.Success"" Items=""basicNavBarItems"" Mode=""BitNavMode.Manual"" DefaultSelectedItem=""basicNavBarItems[0]"" />
<BitNavBar Accent=""BitColor.Error"" Items=""basicNavBarItems"" Mode=""BitNavMode.Manual"" DefaultSelectedItem=""basicNavBarItems[0]"" />

<BitNavBar Accent=""BitColor.SecondaryBackground"" Color=""BitColor.Info"" Items=""basicNavBarItems"" Mode=""BitNavMode.Manual"" DefaultSelectedItem=""basicNavBarItems[0]"" />";
    private readonly string example10CsharpCode = @"
private static readonly List<BitNavBarItem> basicNavBarItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Products"", IconName = BitIconName.ProductVariant },
    new() { Text = ""Academy"", IconName = BitIconName.LearningTools },
    new() { Text = ""Profile"", IconName = BitIconName.Contact },
];";

    private readonly string example11RazorCode = @"
<BitNavBar Items=""basicNavBarItems"">
    <ItemTemplate Context=""item"">
        <BitText Typography=""BitTypography.Caption1"" Color=""BitColor.Warning"">@item.Text</BitText>
        <BitIcon IconName=""@item.IconName"" Color=""BitColor.Success"" />
    </ItemTemplate>
</BitNavBar>

<BitNavBar Items=""templateNavBarItems"" />";
    private readonly string example11CsharpCode = @"
private static readonly List<BitNavBarItem> basicNavBarItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Products"", IconName = BitIconName.ProductVariant },
    new() { Text = ""Academy"", IconName = BitIconName.LearningTools },
    new() { Text = ""Profile"", IconName = BitIconName.Contact },
];

private static readonly List<BitNavBarItem> templateNavBarItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Products"", Template = (item) => @<div style=""display:flex;flex-direction:column""><b>@item.Text</b><span>&#127873;</span></div> },
    new() { Text = ""Academy"", IconName = BitIconName.LearningTools },
    new() { Text = ""Profile"", IconName = BitIconName.Contact },
];";

    private readonly string example12RazorCode = @"
<BitNavBar Items=""basicNavBarItems""
           Mode=""BitNavMode.Manual""
           OnItemClick=""(BitNavBarItem item) => eventsClickedItem = item""
           OnSelectItem=""(BitNavBarItem item) => eventsSelectedItem = item"" />

Clicked item: @eventsClickedItem?.Text
Selected item: @eventsSelectedItem?.Text";
    private readonly string example12CsharpCode = @"
private static readonly List<BitNavBarItem> basicNavBarItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Products"", IconName = BitIconName.ProductVariant },
    new() { Text = ""Academy"", IconName = BitIconName.LearningTools },
    new() { Text = ""Profile"", IconName = BitIconName.Contact },
];

private BitNavBarItem? eventsClickedItem;
private BitNavBarItem? eventsSelectedItem;";

    private readonly string example13RazorCode = @"
<BitNavBar @bind-SelectedItem=""selectedItem""
           Items=""basicNavBarItems""
           Mode=""BitNavMode.Manual""
           DefaultSelectedItem=""basicNavBarItems[1]"" />

Selected item: @selectedItem.Text


<BitNavBar @bind-SelectedItem=""twoWaySelectedItem""
           Items=""basicNavBarItems""
           Mode=""BitNavMode.Manual"" />

<BitChoiceGroup Horizontal Items=""@choiceGroupItems"" @bind-Value=""@twoWaySelectedItem"" />";
    private readonly string example13CsharpCode = @"
private static readonly List<BitNavBarItem> basicNavBarItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Products"", IconName = BitIconName.ProductVariant },
    new() { Text = ""Academy"", IconName = BitIconName.LearningTools },
    new() { Text = ""Profile"", IconName = BitIconName.Contact },
];

private static IEnumerable<BitChoiceGroupItem<BitNavBarItem>> choiceGroupItems =
     basicNavBarItems.Select(i => new BitChoiceGroupItem<BitNavBarItem>() { Id = i.Text, Text = i.Text, IsEnabled = i.IsEnabled, Value = i });

private BitNavBarItem selectedItem = basicNavBarItems[0];
private BitNavBarItem twoWaySelectedItem = basicNavBarItems[0];";

    private readonly string example14RazorCode = @"
<BitToggle @bind-Value=""reselectable"" OnText=""Enabled recalling"" OffText=""Disabled recalling"" />

<BitNavBar Items=""basicNavBarItems""
           Mode=""BitNavMode.Manual""
           Reselectable=""reselectable""
           OnItemClick=""(BitNavBarItem item) => countClick++"" />

Item click count: @countClick";
    private readonly string example14CsharpCode = @"
private static readonly List<BitNavBarItem> basicNavBarItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Products"", IconName = BitIconName.ProductVariant },
    new() { Text = ""Academy"", IconName = BitIconName.LearningTools },
    new() { Text = ""Profile"", IconName = BitIconName.Contact },
];

private int countClick;
private bool reselectable = true;";

    private readonly string example15RazorCode = @"
<BitNavBar SingleTabStop
           Mode=""BitNavMode.Manual""
           Items=""basicNavBarItems""
           DefaultSelectedItem=""basicNavBarItems[1]"" />

<BitNavBar WrapNavigation
           SingleTabStop
           Mode=""BitNavMode.Manual""
           Items=""basicNavBarItems""
           DefaultSelectedItem=""basicNavBarItems[1]"" />";
    private readonly string example15CsharpCode = @"
private static readonly List<BitNavBarItem> basicNavBarItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Products"", IconName = BitIconName.ProductVariant },
    new() { Text = ""Academy"", IconName = BitIconName.LearningTools },
    new() { Text = ""Profile"", IconName = BitIconName.Contact },
];";

    private readonly string example16RazorCode = @"
<div class=""mobile-frame"">
    <div class=""screen"">
        <BitSticky Top=""0"">
            <BitCard FullWidth>
                <BitStack Horizontal HorizontalAlign=""BitAlignment.Center"" VerticalAlign=""BitAlignment.Center"">
                    <BitImage Src=""/images/bit-logo.svg"" Width=""50"" />
                    <BitText Typography=""BitTypography.H4"" Color=""BitColor.Info"">bit BlazorUI</BitText>
                </BitStack>
            </BitCard>
        </BitSticky>
        <BitStack Alignment=""BitAlignment.Center"" AutoHeight Grows>
            <BitText Typography=""BitTypography.H4"" Color=""BitColor.PrimaryForeground"">
                <BitIcon IconName=""@advancedSelectedItem.IconName"" Color=""BitColor.PrimaryForeground"" Size=""BitSize.Large"" />
                <span>@advancedSelectedItem.Text</span>
            </BitText>
        </BitStack>
        <BitSticky Bottom=""0"">
            <BitCard FullWidth Style=""padding:2px"">
                <BitNavBar SafeArea
                           FullWidth
                           Accent=""BitColor.Primary""
                           Items=""basicNavBarItems""
                           Mode=""BitNavMode.Manual""
                           @bind-SelectedItem=""advancedSelectedItem"" />
            </BitCard>
        </BitSticky>
    </div>
</div>";
    private readonly string example16CsharpCode = @"
private static readonly List<BitNavBarItem> basicNavBarItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Products"", IconName = BitIconName.ProductVariant },
    new() { Text = ""Academy"", IconName = BitIconName.LearningTools },
    new() { Text = ""Profile"", IconName = BitIconName.Contact },
];

private BitNavBarItem advancedSelectedItem = basicNavBarItems[1];";

    private readonly string example17RazorCode = @"
<BitNavBar Mode=""BitNavMode.Manual""
           Items=""selectedIconItems""
           DefaultSelectedItem=""selectedIconItems[0]"" />";
    private readonly string example17CsharpCode = @"
private static readonly List<BitNavBarItem> selectedIconItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home, SelectedIconName = BitIconName.HomeSolid },
    new() { Text = ""Inbox"", IconName = BitIconName.Mail, SelectedIconName = BitIconName.MailSolid },
    new() { Text = ""Alerts"", IconName = BitIconName.Ringer, SelectedIconName = BitIconName.RingerSolid },
    new() { Text = ""Favorites"", IconName = BitIconName.Heart, SelectedIconName = BitIconName.HeartFill },
];";

    private readonly string example18RazorCode = @"
<BitNavBar Alignment=""BitAlignment.Start"" Items=""basicNavBarItems"" />

<BitNavBar Alignment=""BitAlignment.Center"" Items=""basicNavBarItems"" />

<BitNavBar Alignment=""BitAlignment.End"" Items=""basicNavBarItems"" />

<BitNavBar Alignment=""BitAlignment.SpaceBetween"" Items=""basicNavBarItems"" />

<BitNavBar Vertical FitWidth Alignment=""BitAlignment.Center"" Items=""basicNavBarItems"" />";
    private readonly string example18CsharpCode = @"
private static readonly List<BitNavBarItem> basicNavBarItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Products"", IconName = BitIconName.ProductVariant },
    new() { Text = ""Academy"", IconName = BitIconName.LearningTools },
    new() { Text = ""Profile"", IconName = BitIconName.Contact },
];";

    private readonly string example19RazorCode = @"
<BitNavBar Items=""basicNavBarItems"">
    <HeaderTemplate>
        <BitImage Src=""/images/bit-logo.svg"" Width=""32"" />
    </HeaderTemplate>
    <FooterTemplate>
        <BitButton IconOnly Title=""More"" Variant=""BitVariant.Text"" IconName=""@BitIconName.More"" />
    </FooterTemplate>
</BitNavBar>

<BitNavBar Vertical FitWidth IconOnly Items=""basicNavBarItems"">
    <HeaderTemplate>
        <BitButton IconOnly Title=""New"" IconName=""@BitIconName.Add"" />
    </HeaderTemplate>
    <FooterTemplate>
        <BitButton IconOnly Title=""Settings"" Variant=""BitVariant.Text"" IconName=""@BitIconName.Settings"" />
    </FooterTemplate>
</BitNavBar>";
    private readonly string example19CsharpCode = @"
private static readonly List<BitNavBarItem> basicNavBarItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Products"", IconName = BitIconName.ProductVariant },
    new() { Text = ""Academy"", IconName = BitIconName.LearningTools },
    new() { Text = ""Profile"", IconName = BitIconName.Contact },
];";

    private readonly string example20RazorCode = @"
<BitStack Horizontal>
    <BitButton OnClick=""AddDynamicItem"">Add item</BitButton>
    <BitButton OnClick=""RemoveDynamicItem"">Remove item</BitButton>
    <BitButton OnClick=""ReverseDynamicItems"">Reverse items</BitButton>
</BitStack>

<BitNavBar Mode=""BitNavMode.Manual""
           Items=""dynamicNavBarItems""
           @bind-SelectedItem=""dynamicSelectedItem"" />

Selected item: @dynamicSelectedItem?.Text";
    private readonly string example20CsharpCode = @"
private int dynamicItemsCount = 3;
private BitNavBarItem? dynamicSelectedItem;
private readonly List<BitNavBarItem> dynamicNavBarItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home },
    new() { Text = ""Products"", IconName = BitIconName.ProductVariant },
    new() { Text = ""Profile"", IconName = BitIconName.Contact },
];

private void AddDynamicItem()
{
    dynamicItemsCount++;
    dynamicNavBarItems.Add(new() { Text = $""Item {dynamicItemsCount}"", IconName = BitIconName.Tag });
}

private void RemoveDynamicItem()
{
    if (dynamicNavBarItems.Count == 0) return;

    dynamicNavBarItems.RemoveAt(dynamicNavBarItems.Count - 1);
}

private void ReverseDynamicItems() => dynamicNavBarItems.Reverse();";

    private readonly string example21RazorCode = @"
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
    private readonly string example21CsharpCode = @"
private static readonly List<BitNavBarItem> basicNavBarItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Products"", IconName = BitIconName.ProductVariant },
    new() { Text = ""Academy"", IconName = BitIconName.LearningTools },
    new() { Text = ""Profile"", IconName = BitIconName.Contact },
];";

    private readonly string example22RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitNavBar Items=""externalIconItems"" />";
    private readonly string example22CsharpCode = @"
private static readonly List<BitNavBarItem> externalIconItems =
[
    new() { Text = ""Home"", Icon = ""fa-solid fa-house"" },
    new() { Text = ""Products"", Icon = BitIconInfo.Css(""fa-solid fa-box"") },
    new() { Text = ""Academy"", Icon = BitIconInfo.Fa(""solid graduation-cap"") },
    new() { Text = ""Profile"", Icon = BitIconInfo.Fa(""solid user"") },
];";

    private readonly string example23RazorCode = @"
<BitNavBar Size=""BitSize.Small"" Items=""basicNavBarItems"" Mode=""BitNavMode.Manual"" DefaultSelectedItem=""basicNavBarItems[0]"" />
<BitNavBar Size=""BitSize.Medium"" Items=""basicNavBarItems"" Mode=""BitNavMode.Manual"" DefaultSelectedItem=""basicNavBarItems[0]"" />
<BitNavBar Size=""BitSize.Large"" Items=""basicNavBarItems"" Mode=""BitNavMode.Manual"" DefaultSelectedItem=""basicNavBarItems[0]"" />";
    private readonly string example23CsharpCode = @"
private static readonly List<BitNavBarItem> basicNavBarItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Products"", IconName = BitIconName.ProductVariant },
    new() { Text = ""Academy"", IconName = BitIconName.LearningTools },
    new() { Text = ""Profile"", IconName = BitIconName.Contact },
];";

    private readonly string example24RazorCode = @"
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

<BitNavBar Items=""badgeNavBarItems"" Styles=""@(new() { ItemIcon = ""color: aqua;"", ItemText = ""color: tomato;"", ItemBadge = ""background: darkmagenta;"" })"" />
<BitNavBar Items=""basicNavBarItems"" Classes=""@(new() { ItemIcon = ""custom-item-ico"", ItemText = ""custom-item-txt"" })"" />";
    private readonly string example24CsharpCode = @"
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
];

private static readonly List<BitNavBarItem> badgeNavBarItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Inbox"", IconName = BitIconName.Mail, Badge = ""12"" },
    new() { Text = ""Alerts"", IconName = BitIconName.Ringer, Badge = ""99+"", BadgeAriaLabel = ""more than 99 unread alerts"" },
    new() { Text = ""Profile"", IconName = BitIconName.Contact, Dot = true, BadgeAriaLabel = ""needs attention"" },
];";

    private readonly string example25RazorCode = @"
<BitNavBar Dir=""BitDir.Rtl"" Items=""rtlItems"" />";
    private readonly string example25CsharpCode = @"
private static readonly List<BitNavBarItem> rtlItems =
[
    new() { Text = ""خانه"", IconName = BitIconName.Home  },
    new() { Text = ""محصولات"", IconName = BitIconName.ProductVariant },
    new() { Text = ""آکادمی"", IconName = BitIconName.LearningTools },
    new() { Text = ""پروفایل"", IconName = BitIconName.Contact },
];";
}

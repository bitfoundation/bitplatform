namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.NavBar;

public partial class _BitNavBarCustomDemo
{
    private readonly string example1RazorCode = @"
<BitNavBar Items=""basicNavBarCustoms""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />";
    private readonly string example1CsharpCode = @"
public class MenuItem
{
    public string? Title { get; set; }
    public string? ImageName { get; set; }
    public BitIconInfo? Image { get; set; }
    public RenderFragment<MenuItem>? Fragment { get; set; }
    public string? CssClass { get; set; }
    public string? Style { get; set; }
    public bool Disabled { get; set; }
    public string? Link { get; set; }
    public IEnumerable<string>? ExtraLinks { get; set; }
    public BitNavMatch? Matching { get; set; }
    public BitNavItemTemplateRenderMode FragmentRenderMode { get; set; }
    public string? Counter { get; set; }
    public string? CounterLabel { get; set; }
    public bool Marker { get; set; }
    public string? SelectedImageName { get; set; }
    public BitIconInfo? SelectedImage { get; set; }
}

private static readonly List<MenuItem> basicNavBarCustoms =
[
    new() { Title = ""Home"", ImageName = BitIconName.Home  },
    new() { Title = ""Products"", ImageName = BitIconName.ProductVariant },
    new() { Title = ""Academy"", ImageName = BitIconName.LearningTools },
    new() { Title = ""Profile"", ImageName = BitIconName.Contact },
];";

    private readonly string example2RazorCode = @"
<BitNavBar Items=""basicNavBarCustoms"" IsEnabled=""false""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />

<BitNavBar Items=""basicNavBarCustomsDisabled""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName },
                                    IsEnabled = { Selector = item => item.Disabled is false } })"" />";
    private readonly string example2CsharpCode = @"
private static readonly List<MenuItem> basicNavBarCustoms =
[
    new() { Title = ""Home"", ImageName = BitIconName.Home  },
    new() { Title = ""Products"", ImageName = BitIconName.ProductVariant },
    new() { Title = ""Academy"", ImageName = BitIconName.LearningTools },
    new() { Title = ""Profile"", ImageName = BitIconName.Contact },
];

private static readonly List<MenuItem> basicNavBarCustomsDisabled =
[
    new() { Title = ""Home"", ImageName = BitIconName.Home  },
    new() { Title = ""Products"", ImageName = BitIconName.ProductVariant },
    new() { Title = ""Academy"", ImageName = BitIconName.LearningTools, Disabled = true },
    new() { Title = ""Profile"", ImageName = BitIconName.Contact },
];";

    private readonly string example3RazorCode = @"
<BitNavBar Mode=""BitNavMode.Manual""
           Items=""basicNavBarCustoms""
           DefaultSelectedItem=""basicNavBarCustoms[0]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />";
    private readonly string example3CsharpCode = @"
private static readonly List<MenuItem> basicNavBarCustoms =
[
    new() { Title = ""Home"", ImageName = BitIconName.Home  },
    new() { Title = ""Products"", ImageName = BitIconName.ProductVariant },
    new() { Title = ""Academy"", ImageName = BitIconName.LearningTools },
    new() { Title = ""Profile"", ImageName = BitIconName.Contact },
];";

    private readonly string example4RazorCode = @"
<BitNavBar Items=""exactMatchCustoms""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName },
                                    Url = { Selector = item => item.Link } })"" />

<BitNavBar Items=""prefixMatchCustoms"" Match=""BitNavMatch.Prefix""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName },
                                    Url = { Selector = item => item.Link } })"" />

<BitNavBar Items=""wildcardMatchCustoms""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName },
                                    Url = { Selector = item => item.Link },
                                    Match = { Selector = item => item.Matching },
                                    IsEnabled = { Selector = item => item.Disabled is false } })"" />

<BitNavBar Items=""regexMatchCustoms""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName },
                                    Url = { Selector = item => item.Link },
                                    Match = { Selector = item => item.Matching },
                                    IsEnabled = { Selector = item => item.Disabled is false } })"" />

<BitNavBar Items=""additionalUrlsCustoms""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName },
                                    Url = { Selector = item => item.Link },
                                    AdditionalUrls = { Selector = item => item.ExtraLinks } })"" />";
    private readonly string example4CsharpCode = @"
private static readonly List<MenuItem> exactMatchCustoms =
[
    new() { Title = ""NavBar"", ImageName = BitIconName.GlobalNavButton, Link = ""/components/navbar"" },
    new() { Title = ""Nav"", ImageName = BitIconName.BulletedList, Link = ""/components/nav"" },
];

private static readonly List<MenuItem> prefixMatchCustoms =
[
    new() { Title = ""Components"", ImageName = BitIconName.F12DevTools, Link = ""/components"" },
    new() { Title = ""Iconography"", ImageName = BitIconName.AppIconDefault, Link = ""/iconography"" },
];

// The URL of a Wildcard or a Regex item is a pattern rather than a route, so these items are disabled:
// they still light up on a match, but a click cannot navigate to a URL no page answers.
private static readonly List<MenuItem> wildcardMatchCustoms =
[
    new() { Title = ""/components/*"", ImageName = BitIconName.F12DevTools, Link = ""/components/*"", Matching = BitNavMatch.Wildcard, Disabled = true },
    new() { Title = ""/iconography/*"", ImageName = BitIconName.AppIconDefault, Link = ""/iconography/*"", Matching = BitNavMatch.Wildcard, Disabled = true },
];

private static readonly List<MenuItem> regexMatchCustoms =
[
    new() { Title = ""^/components/navbar$"", ImageName = BitIconName.Code, Link = ""^/components/navbar$"", Matching = BitNavMatch.Regex, Disabled = true },
    new() { Title = ""^/iconography$"", ImageName = BitIconName.Code, Link = ""^/iconography$"", Matching = BitNavMatch.Regex, Disabled = true },
];

private static readonly List<MenuItem> additionalUrlsCustoms =
[
    new() { Title = ""Navs"", ImageName = BitIconName.GlobalNavButton, Link = ""/components/nav"", ExtraLinks = [""/components/navbar"", ""/components/breadcrumb""] },
    new() { Title = ""Buttons"", ImageName = BitIconName.ButtonControl, Link = ""/components/button"", ExtraLinks = [""/components/togglebutton""] },
];";

    private readonly string example5RazorCode = @"
<BitNavBar Items=""basicNavBarCustoms"" IconOnly
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />

<BitNavBar HideUnselectedText
           Mode=""BitNavMode.Manual""
           Items=""basicNavBarCustoms""
           DefaultSelectedItem=""basicNavBarCustoms[0]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />

<BitNavBar Items=""basicNavBarCustoms"" InlineText
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />";
    private readonly string example5CsharpCode = @"
private static readonly List<MenuItem> basicNavBarCustoms =
[
    new() { Title = ""Home"", ImageName = BitIconName.Home  },
    new() { Title = ""Products"", ImageName = BitIconName.ProductVariant },
    new() { Title = ""Academy"", ImageName = BitIconName.LearningTools },
    new() { Title = ""Profile"", ImageName = BitIconName.Contact },
];";

    private readonly string example6RazorCode = @"
<BitNavBar Items=""basicNavBarCustoms"" FitWidth
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />

<BitNavBar Items=""basicNavBarCustoms"" FullWidth
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />

<BitNavBar Items=""unevenNavBarCustoms"" Justified
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />

<BitNavBar Alignment=""BitAlignment.Center"" Items=""basicNavBarCustoms""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />

<BitNavBar Alignment=""BitAlignment.SpaceBetween"" Items=""basicNavBarCustoms""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />";
    private readonly string example6CsharpCode = @"
private static readonly List<MenuItem> basicNavBarCustoms =
[
    new() { Title = ""Home"", ImageName = BitIconName.Home  },
    new() { Title = ""Products"", ImageName = BitIconName.ProductVariant },
    new() { Title = ""Academy"", ImageName = BitIconName.LearningTools },
    new() { Title = ""Profile"", ImageName = BitIconName.Contact },
];

private static readonly List<MenuItem> unevenNavBarCustoms =
[
    new() { Title = ""Home"", ImageName = BitIconName.Home  },
    new() { Title = ""Products & services"", ImageName = BitIconName.ProductVariant },
    new() { Title = ""Academy"", ImageName = BitIconName.LearningTools },
    new() { Title = ""Me"", ImageName = BitIconName.Contact },
];";

    private readonly string example7RazorCode = @"
<BitNavBar Mode=""BitNavMode.Manual""
           Items=""selectedIconCustoms""
           DefaultSelectedItem=""selectedIconCustoms[0]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName },
                                    SelectedIconName = { Selector = item => item.SelectedImageName } })"" />

<BitNavBar Filled
           Mode=""BitNavMode.Manual""
           Items=""selectedIconCustoms""
           DefaultSelectedItem=""selectedIconCustoms[0]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName },
                                    SelectedIconName = { Selector = item => item.SelectedImageName } })"" />

<BitNavBar Indicator=""BitNavBarIndicator.Line""
           Mode=""BitNavMode.Manual""
           Items=""selectedIconCustoms""
           DefaultSelectedItem=""selectedIconCustoms[0]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName },
                                    SelectedIconName = { Selector = item => item.SelectedImageName } })"" />

<BitNavBar Indicator=""BitNavBarIndicator.Pill""
           Mode=""BitNavMode.Manual""
           Items=""selectedIconCustoms""
           DefaultSelectedItem=""selectedIconCustoms[0]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName },
                                    SelectedIconName = { Selector = item => item.SelectedImageName } })"" />

<BitNavBar Indicator=""BitNavBarIndicator.Pill""
           Filled
           Mode=""BitNavMode.Manual""
           Items=""selectedIconCustoms""
           DefaultSelectedItem=""selectedIconCustoms[0]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName },
                                    SelectedIconName = { Selector = item => item.SelectedImageName } })"" />";
    private readonly string example7CsharpCode = @"
private static readonly List<MenuItem> selectedIconCustoms =
[
    new() { Title = ""Home"", ImageName = BitIconName.Home, SelectedImageName = BitIconName.HomeSolid },
    new() { Title = ""Inbox"", ImageName = BitIconName.Mail, SelectedImageName = BitIconName.MailSolid },
    new() { Title = ""Alerts"", ImageName = BitIconName.Ringer, SelectedImageName = BitIconName.RingerSolid },
    new() { Title = ""Favorites"", ImageName = BitIconName.Heart, SelectedImageName = BitIconName.HeartFill },
];";

    private readonly string example8RazorCode = @"
<BitButton OnClick=""@(() => scrollableSelectedCustom = scrollableNavBarCustoms[^1])"">Select the last item</BitButton>

<BitNavBar TItem=""MenuItem""
           Scrollable
           Mode=""BitNavMode.Manual""
           Items=""scrollableNavBarCustoms""
           @bind-SelectedItem=""scrollableSelectedCustom""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />

Selected item: @scrollableSelectedCustom?.Title";
    private readonly string example8CsharpCode = @"
private static readonly List<MenuItem> scrollableNavBarCustoms =
[
    new() { Title = ""Home"", ImageName = BitIconName.Home },
    new() { Title = ""Products"", ImageName = BitIconName.ProductVariant },
    new() { Title = ""Academy"", ImageName = BitIconName.LearningTools },
    new() { Title = ""Inbox"", ImageName = BitIconName.Mail },
    new() { Title = ""Alerts"", ImageName = BitIconName.Ringer },
    new() { Title = ""Favorites"", ImageName = BitIconName.Heart },
    new() { Title = ""Reports"", ImageName = BitIconName.ReportDocument },
    new() { Title = ""Settings"", ImageName = BitIconName.Settings },
    new() { Title = ""Support"", ImageName = BitIconName.Help },
    new() { Title = ""Profile"", ImageName = BitIconName.Contact },
];

private MenuItem? scrollableSelectedCustom;";

    private readonly string example9RazorCode = @"
<BitNavBar Items=""basicNavBarCustoms""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"">
    <HeaderTemplate>
        <BitImage Src=""/images/bit-logo.svg"" Width=""32"" Alt=""bit"" />
    </HeaderTemplate>
    <FooterTemplate>
        <BitButton IconOnly Title=""More"" Variant=""BitVariant.Text"" IconName=""@BitIconName.More"" />
    </FooterTemplate>
</BitNavBar>";
    private readonly string example9CsharpCode = @"
private static readonly List<MenuItem> basicNavBarCustoms =
[
    new() { Title = ""Home"", ImageName = BitIconName.Home  },
    new() { Title = ""Products"", ImageName = BitIconName.ProductVariant },
    new() { Title = ""Academy"", ImageName = BitIconName.LearningTools },
    new() { Title = ""Profile"", ImageName = BitIconName.Contact },
];";

    private readonly string example10RazorCode = @"
<BitNavBar Items=""basicNavBarCustoms"" Vertical FitWidth
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />

<BitNavBar Vertical
           FitWidth
           InlineText
           Indicator=""BitNavBarIndicator.Line""
           Items=""basicNavBarCustoms""
           Mode=""BitNavMode.Manual""
           DefaultSelectedItem=""basicNavBarCustoms[0]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />

<BitNavBar Vertical FitWidth IconOnly Alignment=""BitAlignment.Center"" Items=""basicNavBarCustoms""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"">
    <HeaderTemplate>
        <BitButton IconOnly Title=""New"" IconName=""@BitIconName.Add"" />
    </HeaderTemplate>
    <FooterTemplate>
        <BitButton IconOnly Title=""Settings"" Variant=""BitVariant.Text"" IconName=""@BitIconName.Settings"" />
    </FooterTemplate>
</BitNavBar>

<BitNavBar Scrollable
           Vertical
           FitWidth
           Style=""height:16rem""
           Mode=""BitNavMode.Manual""
           Items=""scrollableNavBarCustoms""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />";
    private readonly string example10CsharpCode = @"
private static readonly List<MenuItem> basicNavBarCustoms =
[
    new() { Title = ""Home"", ImageName = BitIconName.Home  },
    new() { Title = ""Products"", ImageName = BitIconName.ProductVariant },
    new() { Title = ""Academy"", ImageName = BitIconName.LearningTools },
    new() { Title = ""Profile"", ImageName = BitIconName.Contact },
];

private static readonly List<MenuItem> scrollableNavBarCustoms =
[
    new() { Title = ""Home"", ImageName = BitIconName.Home },
    new() { Title = ""Products"", ImageName = BitIconName.ProductVariant },
    new() { Title = ""Academy"", ImageName = BitIconName.LearningTools },
    new() { Title = ""Inbox"", ImageName = BitIconName.Mail },
    new() { Title = ""Alerts"", ImageName = BitIconName.Ringer },
    new() { Title = ""Favorites"", ImageName = BitIconName.Heart },
    new() { Title = ""Reports"", ImageName = BitIconName.ReportDocument },
    new() { Title = ""Settings"", ImageName = BitIconName.Settings },
    new() { Title = ""Support"", ImageName = BitIconName.Help },
    new() { Title = ""Profile"", ImageName = BitIconName.Contact },
];";

    private readonly string example11RazorCode = @"
<BitNavBar Items=""badgeNavBarCustoms""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName },
                                    Badge = { Selector = item => item.Counter },
                                    BadgeAriaLabel = { Selector = item => item.CounterLabel },
                                    Dot = { Selector = item => item.Marker } })"" />";
    private readonly string example11CsharpCode = @"
private static readonly List<MenuItem> badgeNavBarCustoms =
[
    new() { Title = ""Home"", ImageName = BitIconName.Home  },
    new() { Title = ""Inbox"", ImageName = BitIconName.Mail, Counter = ""12"" },
    new() { Title = ""Alerts"", ImageName = BitIconName.Ringer, Counter = ""99+"", CounterLabel = ""more than 99 unread alerts"" },
    new() { Title = ""Profile"", ImageName = BitIconName.Contact, Marker = true, CounterLabel = ""needs attention"" },
];";

    private readonly string example12RazorCode = @"
<BitNavBar Items=""basicNavBarCustoms""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"">
    <ItemTemplate Context=""custom"">
        <BitText Typography=""BitTypography.Caption1"" Color=""BitColor.Warning"">@custom.Title</BitText>
        <BitIcon IconName=""@custom.ImageName"" Color=""BitColor.Success"" />
    </ItemTemplate>
</BitNavBar>

<BitNavBar Items=""templateNavBarCustoms""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName },
                                    Template = { Selector = item => item.Fragment } })"" />

<BitNavBar Mode=""BitNavMode.Manual""
           Items=""replacedTemplateNavBarCustoms""
           DefaultSelectedItem=""replacedTemplateNavBarCustoms[0]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName },
                                    Template = { Selector = item => item.Fragment },
                                    TemplateRenderMode = { Selector = item => item.FragmentRenderMode } })"" />";
    private readonly string example12CsharpCode = @"
private static readonly List<MenuItem> basicNavBarCustoms =
[
    new() { Title = ""Home"", ImageName = BitIconName.Home  },
    new() { Title = ""Products"", ImageName = BitIconName.ProductVariant },
    new() { Title = ""Academy"", ImageName = BitIconName.LearningTools },
    new() { Title = ""Profile"", ImageName = BitIconName.Contact },
];

private static readonly List<MenuItem> templateNavBarCustoms =
[
    new() { Title = ""Home"", ImageName = BitIconName.Home  },
    new() { Title = ""Products"", Fragment = (item) => @<div style=""display:flex;flex-direction:column""><b>@item.Title</b><span>&#127873;</span></div> },
    new() { Title = ""Academy"", ImageName = BitIconName.LearningTools },
    new() { Title = ""Profile"", ImageName = BitIconName.Contact },
];

private static readonly List<MenuItem> replacedTemplateNavBarCustoms =
[
    new() { Title = ""Home"", ImageName = BitIconName.Home },
    new() { Title = ""Search"", ImageName = BitIconName.Search },
    new()
    {
        Title = ""New"",
        FragmentRenderMode = BitNavItemTemplateRenderMode.Replace,
        Fragment = (item) => @<BitButton IconOnly Title=""@item.Title"" IconName=""@BitIconName.Add"" Style=""align-self:center"" />
    },
    new() { Title = ""Alerts"", ImageName = BitIconName.Ringer },
    new() { Title = ""Profile"", ImageName = BitIconName.Contact },
];";

    private readonly string example13RazorCode = @"
<BitToggle @bind-Value=""reselectable"" Label=""Reselectable"" Inline />

<BitNavBar Items=""basicNavBarCustoms""
           Mode=""BitNavMode.Manual""
           Reselectable=""reselectable""
           OnItemClick=""(MenuItem item) => { eventsClickedItem = item; clickCount++; }""
           OnSelectItem=""(MenuItem item) => { eventsSelectedItem = item; selectCount++; }""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />

<div>Clicked item: @eventsClickedItem?.Title (@clickCount clicks)</div>
<div>Selected item: @eventsSelectedItem?.Title (@selectCount selections)</div>";
    private readonly string example13CsharpCode = @"
private static readonly List<MenuItem> basicNavBarCustoms =
[
    new() { Title = ""Home"", ImageName = BitIconName.Home  },
    new() { Title = ""Products"", ImageName = BitIconName.ProductVariant },
    new() { Title = ""Academy"", ImageName = BitIconName.LearningTools },
    new() { Title = ""Profile"", ImageName = BitIconName.Contact },
];

private bool reselectable;
private int clickCount;
private int selectCount;
private MenuItem? eventsClickedItem;
private MenuItem? eventsSelectedItem;";

    private readonly string example14RazorCode = @"
<BitNavBar TItem=""MenuItem""
           @bind-SelectedItem=""twoWaySelectedItem""
           Items=""basicNavBarCustoms""
           Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />

Selected item: @twoWaySelectedItem?.Title

<BitChoiceGroup Horizontal Items=""@choiceGroupItems"" @bind-Value=""@twoWaySelectedItem"" />";
    private readonly string example14CsharpCode = @"
private static readonly List<MenuItem> basicNavBarCustoms =
[
    new() { Title = ""Home"", ImageName = BitIconName.Home  },
    new() { Title = ""Products"", ImageName = BitIconName.ProductVariant },
    new() { Title = ""Academy"", ImageName = BitIconName.LearningTools },
    new() { Title = ""Profile"", ImageName = BitIconName.Contact },
];

private static IEnumerable<BitChoiceGroupItem<MenuItem>> choiceGroupItems =
     basicNavBarCustoms.Select(i => new BitChoiceGroupItem<MenuItem>() { Id = i.Title, Text = i.Title, IsEnabled = true, Value = i });

private MenuItem? twoWaySelectedItem;";

    private readonly string example15RazorCode = @"
<BitNavBar SingleTabStop
           Mode=""BitNavMode.Manual""
           Items=""basicNavBarCustoms""
           DefaultSelectedItem=""basicNavBarCustoms[1]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />

<BitNavBar WrapNavigation
           SingleTabStop
           Mode=""BitNavMode.Manual""
           Items=""basicNavBarCustoms""
           DefaultSelectedItem=""basicNavBarCustoms[1]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />

<BitNavBar SelectOnFocus
           SingleTabStop
           Mode=""BitNavMode.Manual""
           Items=""basicNavBarCustoms""
           DefaultSelectedItem=""basicNavBarCustoms[1]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />";
    private readonly string example15CsharpCode = @"
private static readonly List<MenuItem> basicNavBarCustoms =
[
    new() { Title = ""Home"", ImageName = BitIconName.Home  },
    new() { Title = ""Products"", ImageName = BitIconName.ProductVariant },
    new() { Title = ""Academy"", ImageName = BitIconName.LearningTools },
    new() { Title = ""Profile"", ImageName = BitIconName.Contact },
];";

    private readonly string example16RazorCode = @"
<BitStack Horizontal>
    <BitButton OnClick=""AddDynamicCustom"">Add item</BitButton>
    <BitButton OnClick=""RemoveDynamicCustom"">Remove item</BitButton>
    <BitButton OnClick=""ReverseDynamicCustoms"">Reverse items</BitButton>
</BitStack>

<BitNavBar TItem=""MenuItem""
           Mode=""BitNavMode.Manual""
           Items=""dynamicNavBarCustoms""
           @bind-SelectedItem=""dynamicSelectedCustom""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />

Selected item: @dynamicSelectedCustom?.Title";
    private readonly string example16CsharpCode = @"
private int dynamicCustomsCount = 3;
private MenuItem? dynamicSelectedCustom;
private readonly List<MenuItem> dynamicNavBarCustoms =
[
    new() { Title = ""Home"", ImageName = BitIconName.Home },
    new() { Title = ""Products"", ImageName = BitIconName.ProductVariant },
    new() { Title = ""Profile"", ImageName = BitIconName.Contact },
];

private void AddDynamicCustom()
{
    dynamicCustomsCount++;
    dynamicNavBarCustoms.Add(new() { Title = $""Item {dynamicCustomsCount}"", ImageName = BitIconName.Tag });
}

private void RemoveDynamicCustom()
{
    if (dynamicNavBarCustoms.Count == 0) return;

    dynamicNavBarCustoms.RemoveAt(dynamicNavBarCustoms.Count - 1);
}

private void ReverseDynamicCustoms() => dynamicNavBarCustoms.Reverse();";

    private readonly string example17RazorCode = @"
<BitParams Parameters=""navBarParams"">
    <BitNavBar Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]""
               NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                        IconName = { Selector = item => item.ImageName } })"" />

    <BitNavBar Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]"" Indicator=""BitNavBarIndicator.Line""
               NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                        IconName = { Selector = item => item.ImageName } })"" />
</BitParams>

<BitNavBar Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]"" Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />";
    private readonly string example17CsharpCode = @"
private static readonly BitNavBarParams[] navBarParams =
[
    new()
    {
        Mode = BitNavMode.Manual,
        Filled = true,
        Color = BitColor.Info,
        Indicator = BitNavBarIndicator.Pill,
    }
];

private static readonly List<MenuItem> basicNavBarCustoms =
[
    new() { Title = ""Home"", ImageName = BitIconName.Home  },
    new() { Title = ""Products"", ImageName = BitIconName.ProductVariant },
    new() { Title = ""Academy"", ImageName = BitIconName.LearningTools },
    new() { Title = ""Profile"", ImageName = BitIconName.Contact },
];";

    private readonly string example18RazorCode = @"
<BitNavBar Color=""BitColor.Primary"" Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]"" Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />
<BitNavBar Color=""BitColor.Secondary"" Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]"" Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />
<BitNavBar Color=""BitColor.Tertiary"" Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]"" Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />
<BitNavBar Color=""BitColor.Info"" Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]"" Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />
<BitNavBar Color=""BitColor.Success"" Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]"" Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />
<BitNavBar Color=""BitColor.Warning"" Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]"" Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />
<BitNavBar Color=""BitColor.SevereWarning"" Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]"" Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />
<BitNavBar Color=""BitColor.Error"" Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]"" Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />

<BitNavBar Color=""BitColor.PrimaryBackground"" Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]"" Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />
<BitNavBar Color=""BitColor.SecondaryBackground"" Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]"" Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />
<BitNavBar Color=""BitColor.TertiaryBackground"" Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]"" Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />

<BitNavBar Color=""BitColor.PrimaryForeground"" Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]"" Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />
<BitNavBar Color=""BitColor.SecondaryForeground"" Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]"" Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />
<BitNavBar Color=""BitColor.TertiaryForeground"" Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]"" Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />

<BitNavBar Color=""BitColor.PrimaryBorder"" Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]"" Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />
<BitNavBar Color=""BitColor.SecondaryBorder"" Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]"" Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />
<BitNavBar Color=""BitColor.TertiaryBorder"" Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]"" Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />";
    private readonly string example18CsharpCode = @"
private static readonly List<MenuItem> basicNavBarCustoms =
[
    new() { Title = ""Home"", ImageName = BitIconName.Home  },
    new() { Title = ""Products"", ImageName = BitIconName.ProductVariant },
    new() { Title = ""Academy"", ImageName = BitIconName.LearningTools },
    new() { Title = ""Profile"", ImageName = BitIconName.Contact },
];";

    private readonly string example19RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitNavBar Items=""externalIconCustoms""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    Icon = { Selector = item => item.Image } })"" />";
    private readonly string example19CsharpCode = @"
private static readonly List<MenuItem> externalIconCustoms =
[
    new() { Title = ""Home"", Image = ""fa-solid fa-house"" },
    new() { Title = ""Products"", Image = BitIconInfo.Css(""fa-solid fa-box"") },
    new() { Title = ""Academy"", Image = BitIconInfo.Fa(""solid graduation-cap"") },
    new() { Title = ""Profile"", Image = BitIconInfo.Fa(""solid user"") },
];";

    private readonly string example20RazorCode = @"
<BitNavBar Size=""BitSize.Small"" Items=""basicNavBarCustoms"" Mode=""BitNavMode.Manual"" DefaultSelectedItem=""basicNavBarCustoms[0]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />
<BitNavBar Size=""BitSize.Medium"" Items=""basicNavBarCustoms"" Mode=""BitNavMode.Manual"" DefaultSelectedItem=""basicNavBarCustoms[0]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />
<BitNavBar Size=""BitSize.Large"" Items=""basicNavBarCustoms"" Mode=""BitNavMode.Manual"" DefaultSelectedItem=""basicNavBarCustoms[0]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />";
    private readonly string example20CsharpCode = @"
private static readonly List<MenuItem> basicNavBarCustoms =
[
    new() { Title = ""Home"", ImageName = BitIconName.Home  },
    new() { Title = ""Products"", ImageName = BitIconName.ProductVariant },
    new() { Title = ""Academy"", ImageName = BitIconName.LearningTools },
    new() { Title = ""Profile"", ImageName = BitIconName.Contact },
];";

    private readonly string example21RazorCode = @"
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

    .floating-navbar {
        margin: 0 0.75rem 0.75rem;
        --bit-NavBar-background: var(--bit-clr-bg-sec);
        --bit-NavBar-border-radius: 999px;
        --bit-NavBar-shadow: 0 4px 16px rgba(0, 0, 0, 0.2);
        --bit-NavBar-padding-block: 4px;
        --bit-NavBar-padding-inline: 8px;
        --bit-NavBar-item-border-radius: 999px;
        --bit-NavBar-selected-color: var(--bit-clr-pri-text);
        --bit-NavBar-selected-background: var(--bit-clr-pri);
    }
</style>

<BitNavBar Items=""basicNavBarCustoms""
           Style=""border-radius: 1rem; margin: 1rem; box-shadow: tomato 0 0 1rem;""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />
<BitNavBar Items=""basicNavBarCustoms"" Class=""custom-class""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />

<BitNavBar Items=""basicNavBarCustomsClassStyle""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName },
                                    Class = { Selector = item => item.CssClass },
                                    Style = { Selector = item => item.Style } })"" />

<BitNavBar Items=""badgeNavBarCustoms""
           Styles=""@(new() { ItemIcon = ""color: aqua;"", ItemText = ""color: tomato;"", ItemBadge = ""background: darkmagenta;"" })""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName },
                                    Badge = { Selector = item => item.Counter },
                                    BadgeAriaLabel = { Selector = item => item.CounterLabel },
                                    Dot = { Selector = item => item.Marker } })"" />
<BitNavBar Items=""basicNavBarCustoms""
           Classes=""@(new() { ItemIcon = ""custom-item-ico"", ItemText = ""custom-item-txt"" })""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />

<BitNavBar Items=""basicNavBarCustoms""
           Class=""floating-navbar""
           Mode=""BitNavMode.Manual""
           DefaultSelectedItem=""basicNavBarCustoms[0]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />";
    private readonly string example21CsharpCode = @"
private static readonly List<MenuItem> basicNavBarCustoms =
[
    new() { Title = ""Home"", ImageName = BitIconName.Home  },
    new() { Title = ""Products"", ImageName = BitIconName.ProductVariant },
    new() { Title = ""Academy"", ImageName = BitIconName.LearningTools },
    new() { Title = ""Profile"", ImageName = BitIconName.Contact },
];

private static readonly List<MenuItem> basicNavBarCustomsClassStyle =
[
    new() { Title = ""Home"", ImageName = BitIconName.Home  },
    new() { Title = ""Products"", ImageName = BitIconName.ProductVariant, CssClass = ""custom-item"" },
    new() { Title = ""Academy"", ImageName = BitIconName.LearningTools, Style = ""color: #b6ff00;font-weight: 600;"" },
    new() { Title = ""Profile"", ImageName = BitIconName.Contact },
];

private static readonly List<MenuItem> badgeNavBarCustoms =
[
    new() { Title = ""Home"", ImageName = BitIconName.Home  },
    new() { Title = ""Inbox"", ImageName = BitIconName.Mail, Counter = ""12"" },
    new() { Title = ""Alerts"", ImageName = BitIconName.Ringer, Counter = ""99+"", CounterLabel = ""more than 99 unread alerts"" },
    new() { Title = ""Profile"", ImageName = BitIconName.Contact, Marker = true, CounterLabel = ""needs attention"" },
];";

    private readonly string example22RazorCode = @"
<BitNavBar Dir=""BitDir.Rtl""
           Items=""rtlCustomsItems""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />";
    private readonly string example22CsharpCode = @"
private static readonly List<MenuItem> rtlCustomsItems =
[
    new() { Title = ""خانه"", ImageName = BitIconName.Home  },
    new() { Title = ""محصولات"", ImageName = BitIconName.ProductVariant },
    new() { Title = ""آکادمی"", ImageName = BitIconName.LearningTools },
    new() { Title = ""پروفایل"", ImageName = BitIconName.Contact },
];";
}

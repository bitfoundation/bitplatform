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
    public string? Counter { get; set; }
    public bool Marker { get; set; }
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

<BitNavBar Items=""patternMatchCustoms""
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
private static readonly List<MenuItem> patternMatchCustoms =
[
    new() { Title = ""/components/*"", ImageName = BitIconName.F12DevTools, Link = ""/components/*"", Matching = BitNavMatch.Wildcard, Disabled = true },
    new() { Title = ""^/components/n"", ImageName = BitIconName.Code, Link = ""^/components/n"", Matching = BitNavMatch.Regex, Disabled = true },
];

private static readonly List<MenuItem> additionalUrlsCustoms =
[
    new() { Title = ""Navs"", ImageName = BitIconName.GlobalNavButton, Link = ""/components/nav"", ExtraLinks = [""/components/navbar"", ""/components/breadcrumb""] },
    new() { Title = ""Buttons"", ImageName = BitIconName.ButtonControl, Link = ""/components/button"", ExtraLinks = [""/components/togglebutton""] },
];";

    private readonly string example5RazorCode = @"
<BitNavBar IconOnly
           Items=""basicNavBarCustoms""
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
<BitNavBar HideUnselectedText
           Mode=""BitNavMode.Manual""
           Items=""basicNavBarCustoms""
           DefaultSelectedItem=""basicNavBarCustoms[0]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />";
    private readonly string example6CsharpCode = @"
private static readonly List<MenuItem> basicNavBarCustoms =
[
    new() { Title = ""Home"", ImageName = BitIconName.Home  },
    new() { Title = ""Products"", ImageName = BitIconName.ProductVariant },
    new() { Title = ""Academy"", ImageName = BitIconName.LearningTools },
    new() { Title = ""Profile"", ImageName = BitIconName.Contact },
];";

    private readonly string example7RazorCode = @"
<BitNavBar InlineText
           Items=""basicNavBarCustoms""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />

<BitNavBar Vertical FitWidth
           Items=""basicNavBarCustoms""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />

<BitNavBar Vertical InlineText FitWidth
           Items=""basicNavBarCustoms""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />";
    private readonly string example7CsharpCode = @"
private static readonly List<MenuItem> basicNavBarCustoms =
[
    new() { Title = ""Home"", ImageName = BitIconName.Home  },
    new() { Title = ""Products"", ImageName = BitIconName.ProductVariant },
    new() { Title = ""Academy"", ImageName = BitIconName.LearningTools },
    new() { Title = ""Profile"", ImageName = BitIconName.Contact },
];";

    private readonly string example8RazorCode = @"
<BitNavBar FitWidth
           Items=""basicNavBarCustoms""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />

<BitNavBar FullWidth
           Items=""basicNavBarCustoms""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />";
    private readonly string example8CsharpCode = @"
private static readonly List<MenuItem> basicNavBarCustoms =
[
    new() { Title = ""Home"", ImageName = BitIconName.Home  },
    new() { Title = ""Products"", ImageName = BitIconName.ProductVariant },
    new() { Title = ""Academy"", ImageName = BitIconName.LearningTools },
    new() { Title = ""Profile"", ImageName = BitIconName.Contact },
];";

    private readonly string example9RazorCode = @"
<BitNavBar Items=""badgeNavBarCustoms""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName },
                                    Badge = { Selector = item => item.Counter },
                                    Dot = { Selector = item => item.Marker } })"" />";
    private readonly string example9CsharpCode = @"
private static readonly List<MenuItem> badgeNavBarCustoms =
[
    new() { Title = ""Home"", ImageName = BitIconName.Home  },
    new() { Title = ""Inbox"", ImageName = BitIconName.Mail, Counter = ""12"" },
    new() { Title = ""Alerts"", ImageName = BitIconName.Ringer, Counter = ""99+"" },
    new() { Title = ""Profile"", ImageName = BitIconName.Contact, Marker = true },
];";

    private readonly string example10RazorCode = @"
<BitNavBar Accent=""BitColor.Primary"" Mode=""BitNavMode.Manual""
           Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />
<BitNavBar Accent=""BitColor.Success"" Mode=""BitNavMode.Manual""
           Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />
<BitNavBar Accent=""BitColor.Error"" Mode=""BitNavMode.Manual""
           Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />

<BitNavBar Accent=""BitColor.SecondaryBackground"" Color=""BitColor.Info"" Mode=""BitNavMode.Manual""
           Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />";
    private readonly string example10CsharpCode = @"
private static readonly List<MenuItem> basicNavBarCustoms =
[
    new() { Title = ""Home"", ImageName = BitIconName.Home  },
    new() { Title = ""Products"", ImageName = BitIconName.ProductVariant },
    new() { Title = ""Academy"", ImageName = BitIconName.LearningTools },
    new() { Title = ""Profile"", ImageName = BitIconName.Contact },
];";

    private readonly string example11RazorCode = @"
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
                                    Template = { Selector = item => item.Fragment } })"" />";
    private readonly string example11CsharpCode = @"
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
];";

    private readonly string example12RazorCode = @"
<BitNavBar Items=""basicNavBarCustoms""
           Mode=""BitNavMode.Manual""
           OnItemClick=""(MenuItem item) => eventsClickedItem = item""
           OnSelectItem=""(MenuItem item) => eventsSelectedItem = item""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />

Clicked item: @eventsClickedItem?.Title
Selected item: @eventsSelectedItem?.Title";
    private readonly string example12CsharpCode = @"
private static readonly List<MenuItem> basicNavBarCustoms =
[
    new() { Title = ""Home"", ImageName = BitIconName.Home  },
    new() { Title = ""Products"", ImageName = BitIconName.ProductVariant },
    new() { Title = ""Academy"", ImageName = BitIconName.LearningTools },
    new() { Title = ""Profile"", ImageName = BitIconName.Contact },
];

private MenuItem? eventsClickedItem;
private MenuItem? eventsSelectedItem;";

    private readonly string example13RazorCode = @"
<BitNavBar @bind-SelectedItem=""selectedItem""
           Items=""basicNavBarCustoms""
           Mode=""BitNavMode.Manual""
           DefaultSelectedItem=""basicNavBarCustoms[1]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />

Selected item: @selectedItem.Title


<BitNavBar Items=""basicNavBarCustoms""
           Mode=""BitNavMode.Manual""
           @bind-SelectedItem=""twoWaySelectedItem""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />

<BitChoiceGroup Horizontal Items=""@choiceGroupItems"" @bind-Value=""@twoWaySelectedItem"" />";
    private readonly string example13CsharpCode = @"
private static readonly List<MenuItem> basicNavBarCustoms =
[
    new() { Title = ""Home"", ImageName = BitIconName.Home  },
    new() { Title = ""Products"", ImageName = BitIconName.ProductVariant },
    new() { Title = ""Academy"", ImageName = BitIconName.LearningTools },
    new() { Title = ""Profile"", ImageName = BitIconName.Contact },
];

private static IEnumerable<BitChoiceGroupItem<MenuItem>> choiceGroupItems =
     basicNavBarCustoms.Select(i => new BitChoiceGroupItem<MenuItem>() { Id = i.Title, Text = i.Title, IsEnabled = true, Value = i });

private MenuItem selectedItem = basicNavBarCustoms[0];
private MenuItem twoWaySelectedItem = basicNavBarCustoms[0];";

    private readonly string example14RazorCode = @"
<BitToggle @bind-Value=""reselectable"" OnText=""Enabled recalling"" OffText=""Disabled recalling"" />

<BitNavBar Mode=""BitNavMode.Manual""
           Items=""basicNavBarCustoms""
           Reselectable=""reselectable""
           OnItemClick=""(MenuItem item) => countClick++""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />

Item click count: @countClick";
    private readonly string example14CsharpCode = @"
private static readonly List<MenuItem> basicNavBarCustoms =
[
    new() { Title = ""Home"", ImageName = BitIconName.Home  },
    new() { Title = ""Products"", ImageName = BitIconName.ProductVariant },
    new() { Title = ""Academy"", ImageName = BitIconName.LearningTools },
    new() { Title = ""Profile"", ImageName = BitIconName.Contact },
];

private int countClick;
private bool reselectable = true;";

    private readonly string example15RazorCode = @"
<BitNavBar SingleTabStop
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
                <BitIcon IconName=""@advancedSelectedItem?.ImageName"" Color=""BitColor.PrimaryForeground"" Size=""BitSize.Large"" />
                <span>@advancedSelectedItem?.Title</span>
            </BitText>
        </BitStack>
        <BitSticky Bottom=""0"">
            <BitCard FullWidth Style=""padding:2px"">
                <BitNavBar SafeArea
                           FullWidth
                           Accent=""BitColor.Primary""
                           Mode=""BitNavMode.Manual""
                           Items=""basicNavBarCustoms""
                           @bind-SelectedItem=""advancedSelectedItem""
                           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                                    IconName = { Selector = item => item.ImageName } })"" />
            </BitCard>
        </BitSticky>
    </div>
</div>";
    private readonly string example16CsharpCode = @"
private static readonly List<MenuItem> basicNavBarCustoms =
[
    new() { Title = ""Home"", ImageName = BitIconName.Home  },
    new() { Title = ""Products"", ImageName = BitIconName.ProductVariant },
    new() { Title = ""Academy"", ImageName = BitIconName.LearningTools },
    new() { Title = ""Profile"", ImageName = BitIconName.Contact },
];

private MenuItem advancedSelectedItem = basicNavBarCustoms[1];";

    private readonly string example17RazorCode = @"
<BitNavBar Color=""BitColor.Primary"" Mode=""BitNavMode.Manual""
           Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />
<BitNavBar Color=""BitColor.Secondary"" Mode=""BitNavMode.Manual""
           Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />
<BitNavBar Color=""BitColor.Tertiary"" Mode=""BitNavMode.Manual""
           Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />
<BitNavBar Color=""BitColor.Info"" Mode=""BitNavMode.Manual""
           Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />
<BitNavBar Color=""BitColor.Success"" Mode=""BitNavMode.Manual""
           Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />
<BitNavBar Color=""BitColor.Warning"" Mode=""BitNavMode.Manual""
           Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />
<BitNavBar Color=""BitColor.SevereWarning"" Mode=""BitNavMode.Manual""
           Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />
<BitNavBar Color=""BitColor.Error"" Mode=""BitNavMode.Manual""
           Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />

<BitNavBar Color=""BitColor.PrimaryBackground"" Mode=""BitNavMode.Manual""
           Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />
<BitNavBar Color=""BitColor.SecondaryBackground"" Mode=""BitNavMode.Manual""
           Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />
<BitNavBar Color=""BitColor.TertiaryBackground"" Mode=""BitNavMode.Manual""
           Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />

<BitNavBar Color=""BitColor.PrimaryForeground"" Mode=""BitNavMode.Manual""
           Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />
<BitNavBar Color=""BitColor.SecondaryForeground"" Mode=""BitNavMode.Manual""
           Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />
<BitNavBar Color=""BitColor.TertiaryForeground"" Mode=""BitNavMode.Manual""
           Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />

<BitNavBar Color=""BitColor.PrimaryBorder"" Mode=""BitNavMode.Manual""
           Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />
<BitNavBar Color=""BitColor.SecondaryBorder"" Mode=""BitNavMode.Manual""
           Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />
<BitNavBar Color=""BitColor.TertiaryBorder"" Mode=""BitNavMode.Manual""
           Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />";
    private readonly string example17CsharpCode = @"
private static readonly List<MenuItem> basicNavBarCustoms =
[
    new() { Title = ""Home"", ImageName = BitIconName.Home  },
    new() { Title = ""Products"", ImageName = BitIconName.ProductVariant },
    new() { Title = ""Academy"", ImageName = BitIconName.LearningTools },
    new() { Title = ""Profile"", ImageName = BitIconName.Contact },
];";

    private readonly string example18RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitNavBar Items=""externalIconCustoms""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    Icon = { Selector = item => item.Image } })"" />";
    private readonly string example18CsharpCode = @"
private static readonly List<MenuItem> externalIconCustoms =
[
    new() { Title = ""Home"", Image = ""fa-solid fa-house"" },
    new() { Title = ""Products"", Image = BitIconInfo.Css(""fa-solid fa-box"") },
    new() { Title = ""Academy"", Image = BitIconInfo.Fa(""solid graduation-cap"") },
    new() { Title = ""Profile"", Image = BitIconInfo.Fa(""solid user"") },
];";

    private readonly string example19RazorCode = @"
<BitNavBar Size=""BitSize.Small"" Mode=""BitNavMode.Manual""
           Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />
<BitNavBar Size=""BitSize.Medium"" Mode=""BitNavMode.Manual""
           Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />
<BitNavBar Size=""BitSize.Large"" Mode=""BitNavMode.Manual""
           Items=""basicNavBarCustoms"" DefaultSelectedItem=""basicNavBarCustoms[0]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />";
    private readonly string example19CsharpCode = @"
private static readonly List<MenuItem> basicNavBarCustoms =
[
    new() { Title = ""Home"", ImageName = BitIconName.Home  },
    new() { Title = ""Products"", ImageName = BitIconName.ProductVariant },
    new() { Title = ""Academy"", ImageName = BitIconName.LearningTools },
    new() { Title = ""Profile"", ImageName = BitIconName.Contact },
];";

    private readonly string example20RazorCode = @"
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
                                    IconName = { Selector = item => item.ImageName } })"" />

<BitNavBar Items=""basicNavBarCustoms""
           Class=""custom-class""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />

<BitNavBar Items=""basicNavBarCustomsClassStyle""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName },
                                    Class = { Selector = item => item.CssClass },
                                    Style = { Selector = item => item.Style }})"" />

<BitNavBar Items=""badgeNavBarCustoms""
           Styles=""@(new() { ItemIcon = ""color: aqua;"", ItemText = ""color: tomato;"", ItemBadge = ""background: darkmagenta;"" })""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName },
                                    Badge = { Selector = item => item.Counter },
                                    Dot = { Selector = item => item.Marker } })"" />

<BitNavBar Items=""basicNavBarCustoms""
           Classes=""@(new() { ItemIcon = ""custom-item-ico"", ItemText = ""custom-item-txt"" })""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />";
    private readonly string example20CsharpCode = @"
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
    new() { Title = ""Alerts"", ImageName = BitIconName.Ringer, Counter = ""99+"" },
    new() { Title = ""Profile"", ImageName = BitIconName.Contact, Marker = true },
];";

    private readonly string example21RazorCode = @"
<BitNavBar Dir=""BitDir.Rtl""
           Items=""rtlCustomsItems""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.ImageName } })"" />";
    private readonly string example21CsharpCode = @"
private static readonly List<MenuItem> rtlCustomsItems =
[
    new() { Title = ""خانه"", ImageName = BitIconName.Home  },
    new() { Title = ""محصولات"", ImageName = BitIconName.ProductVariant },
    new() { Title = ""آکادمی"", ImageName = BitIconName.LearningTools },
    new() { Title = ""پروفایل"", ImageName = BitIconName.Contact },
];";
}

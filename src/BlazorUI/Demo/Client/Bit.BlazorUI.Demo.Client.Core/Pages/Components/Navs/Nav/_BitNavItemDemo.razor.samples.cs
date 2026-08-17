namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Nav;

public partial class _BitNavItemDemo
{
    private readonly string example1RazorCode = @"
<BitNav Items=""basicNavItems"" />";
    private readonly string example1CsharpCode = @"
private static readonly List<BitNavItem> basicNavItems =
[
    new()
    {
        Text = ""bit platform"",
        Description = ""the bit platform description"",
        ChildItems =
        [
            new() { Text = ""Home"", IconName = BitIconName.Home, Url = ""https://bitplatform.dev/"" },
            new()
            {
                Text = ""Products & Services"",
                ChildItems =
                [
                    new()
                    {
                        Text = ""Project Templates"",
                        ChildItems =
                        [
                            new() { Text = ""Todo sample"", IconName = BitIconName.ToDoLogoOutline, Url = ""https://bitplatform.dev/templates/overview"" },
                            new() { Text = ""AdminPanel sample"", IconName = BitIconName.LocalAdmin, Url = ""https://bitplatform.dev/templates/overview"" },
                        ]
                    },
                    new() { Text = ""BlazorUI"", IconName = BitIconName.F12DevTools, Url = ""https://bitplatform.dev/components"" },
                    new() { Text = ""Cloud hosting solutions"", IconName = BitIconName.Cloud, Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                    new() { Text = ""Bit academy"", IconName = BitIconName.LearningTools, Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                ]
            },
            new() { Text = ""Pricing"", IconName = BitIconName.Money, Url = ""https://bitplatform.dev/pricing"" },
            new() { Text = ""About"", IconName = BitIconName.Info, Url = ""https://bitplatform.dev/about-us"" },
            new() { Text = ""Contact us"", IconName = BitIconName.Contact, Url = ""https://bitplatform.dev/contact-us"" },
        ],
    },
    new()
    {
        Text = ""Community"",
        ChildItems =
        [
            new() { Text = ""LinkedIn"", IconName = BitIconName.LinkedInLogo, Url = ""https://www.linkedin.com/company/bitplatformhq"" },
            new() { Text = ""Twitter"", IconName = BitIconName.Globe, Url = ""https://twitter.com/bitplatformhq"" },
            new() { Text = ""GitHub repo"", IconName = BitIconName.GitGraph, Url = ""https://github.com/bitfoundation/bitplatform"" },
        ]
    },
    new() { Text = ""Iconography"", IconName = BitIconName.AppIconDefault, Url = ""/iconography"" },
];";

    private readonly string example2RazorCode = @"
<BitNav Items=""basicNavItems"" FitWidth />

<BitNav Items=""basicNavItems"" FullWidth />";
    private readonly string example2CsharpCode = @"
private static readonly List<BitNavItem> basicNavItems = [ /* see the Basic example */ ];";

    private readonly string example3RazorCode = @"
<BitNav Items=""carNavItems"" RenderType=""BitNavRenderType.Grouped"" />";
    private readonly string example3CsharpCode = @"
private static readonly List<BitNavItem> carNavItems =
[
    new()
    {
        Text = ""Mercedes-Benz"",
        ExpandAriaLabel = ""Mercedes-Benz Expanded"",
        CollapseAriaLabel = ""Mercedes-Benz Collapsed"",
        Title = ""Mercedes-Benz Car Models"",
        IsExpanded = true,
        Description = ""Cars manufactured under the brand of Mercedes-Benz"",
        ChildItems =
        [
            new()
            {
                Text = ""SUVs"",
                ChildItems =
                [
                    new() { Text = ""GLA"", Url = ""https://www.mbusa.com/en/vehicles/class/gla/suv"", Target = ""_blank"" },
                    new() { Text = ""GLB"", Url = ""https://www.mbusa.com/en/vehicles/class/glb/suv"", Target = ""_blank"" },
                    new() { Text = ""GLC"", Url = ""https://www.mbusa.com/en/vehicles/class/glc/suv"", Target = ""_blank"" },
                ]
            },
            new()
            {
                Text = ""Sedans & Wagons"",
                ChildItems =
                [
                    new() { Text = ""A Class"", Url = ""https://www.mbusa.com/en/vehicles/class/a-class/sedan"", Target = ""_blank"" },
                    new() { Text = ""C Class"", Url = ""https://www.mbusa.com/en/vehicles/class/c-class/sedan"", Target = ""_blank"" },
                    new() { Text = ""E Class"", Url = ""https://www.mbusa.com/en/vehicles/class/e-class/sedan"", Target = ""_blank"" },
                ]
            },
            new()
            {
                Text = ""Coupes"",
                ChildItems =
                [
                    new() { Text = ""CLA Coupe"", Url = ""https://www.mbusa.com/en/vehicles/class/cla/coupe"", Target = ""_blank"" },
                    new() { Text = ""C Class Coupe"", Url = ""https://www.mbusa.com/en/vehicles/class/c-class/coupe"", Target = ""_blank"" },
                    new() { Text = ""E Class Coupe"", Url = ""https://www.mbusa.com/en/vehicles/class/e-class/coupe"", Target = ""_blank"" },
                ]
            },
        ]
    },
    new()
    {
        Text = ""Tesla"",
        ExpandAriaLabel = ""Tesla Expanded"",
        CollapseAriaLabel = ""Tesla Collapsed"",
        Title = ""Tesla Car Models"",
        ChildItems =
        [
            new() { Text = ""Model S"", Url = ""https://www.tesla.com/models"", Target = ""_blank"" },
            new() { Text = ""Model X"", Url = ""https://www.tesla.com/modelx"", Target = ""_blank"" },
            new() { Text = ""Model Y"", Url = ""https://www.tesla.com/modely"", Target = ""_blank"" },
        ]
    },
];";

    private readonly string example4RazorCode = @"
<BitNav Items=""separatorNavItems"" FitWidth />";
    private readonly string example4CsharpCode = @"
private static readonly List<BitNavItem> separatorNavItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home, Url = ""https://bitplatform.dev/"" },
    new() { Text = ""Pricing"", IconName = BitIconName.Money, Url = ""https://bitplatform.dev/pricing"" },
    new() { IsSeparator = true },
    new() { Text = ""LinkedIn"", IconName = BitIconName.LinkedInLogo, Url = ""https://www.linkedin.com/company/bitplatformhq"" },
    new() { Text = ""GitHub repo"", IconName = BitIconName.GitGraph, Url = ""https://github.com/bitfoundation/bitplatform"" },
    new() { IsSeparator = true },
    new() { Text = ""Contact us"", IconName = BitIconName.Contact, Url = ""https://bitplatform.dev/contact-us"" },
];";

    private readonly string example5RazorCode = @"
<BitNav Items=""foodNavItems""
        DefaultSelectedItem=""foodNavItems[0].ChildItems[2]""
        Mode=""BitNavMode.Manual"" />

<BitNav @bind-SelectedItem=""SelectedItemNav""
        Items=""foodNavItems""
        Mode=""BitNavMode.Manual""
        OnSelectItem=""(BitNavItem item) => SelectedItemText = FoodMenuDropdownItems.FirstOrDefault(i => i.Text == item.Text)?.Text"" />

<BitDropdown @bind-Value=""SelectedItemText""
             FitWidth
             Label=""Select Item""
             Items=""FoodMenuDropdownItems""
             OnSelectItem=""(BitDropdownItem<string> item) => SelectedItemNav = Flatten(foodNavItems).First(i => i.Text == item.Value)"" />";
    private readonly string example5CsharpCode = @"
private static readonly List<BitNavItem> foodNavItems =
[
    new()
    {
        Text = ""Fast foods"",
        IconName = BitIconName.HeartBroken,
        IsExpanded = true,
        Description = ""List of fast foods"",
        ChildItems =
        [
            new()
            {
                Text = ""Burgers"",
                Description = ""List of burgers"",
                ChildItems =
                [
                    new() { Text = ""Beef Burger"" },
                    new() { Text = ""Veggie Burger"" },
                    new() { Text = ""Bison Burger"" },
                    new() { Text = ""Wild Salmon Burger"" },
                ]
            },
            new()
            {
                Text = ""Pizza"",
                ChildItems =
                [
                    new() { Text = ""Cheese Pizza"" },
                    new() { Text = ""Veggie Pizza"" },
                    new() { Text = ""Pepperoni Pizza"" },
                    new() { Text = ""Meat Pizza"" },
                ]
            },
            new() { Text = ""French Fries"" },
        ]
    },
    new()
    {
        Text = ""Fruits"",
        IconName = BitIconName.Health,
        ChildItems =
        [
            new() { Text = ""Apple"" },
            new() { Text = ""Orange"" },
            new() { Text = ""Banana"" },
        ]
    },
    new() { Text = ""Ice Cream"" },
    new() { Text = ""Cookie"" },
];

private static List<BitNavItem> Flatten(IList<BitNavItem> e) => e.SelectMany(c => Flatten(c.ChildItems)).Concat(e).ToList();

private BitNavItem SelectedItemNav = foodNavItems[0].ChildItems[2];
private string? SelectedItemText = foodNavItems[0].ChildItems[2].Text;

private static readonly List<BitDropdownItem<string>> FoodMenuDropdownItems =
[
    new() { Text = ""Beef Burger"", Value = ""Beef Burger"" },
    new() { Text = ""Veggie Burger"", Value = ""Veggie Burger"" },
    new() { Text = ""Bison Burger"", Value = ""Bison Burger"" },
    new() { Text = ""Wild Salmon Burger"", Value = ""Wild Salmon Burger"" },
    new() { Text = ""Cheese Pizza"", Value = ""Cheese Pizza"" },
    new() { Text = ""Veggie Pizza"", Value = ""Veggie Pizza"" },
    new() { Text = ""Pepperoni Pizza"", Value = ""Pepperoni Pizza"" },
    new() { Text = ""Meat Pizza"", Value = ""Meat Pizza"" },
    new() { Text = ""French Fries"", Value = ""French Fries"" },
    new() { Text = ""Apple"", Value = ""Apple"" },
    new() { Text = ""Orange"", Value = ""Orange"" },
    new() { Text = ""Banana"", Value = ""Banana"" },
    new() { Text = ""Ice Cream"", Value = ""Ice Cream"" },
    new() { Text = ""Cookie"", Value = ""Cookie"" },
];";

    private readonly string example6RazorCode = @"
<BitToggle @bind-Value=""iconOnly"" Label=""Hide texts?"" Inline />

<BitNav Items=""iconOnlyNavItems"" Mode=""BitNavMode.Manual"" IconOnly=""iconOnly"" />";
    private readonly string example6CsharpCode = @"
private bool iconOnly;

private static readonly List<BitNavItem> iconOnlyNavItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home },
    new()
    {
        Text = ""AdminPanel sample"",
        IconName = BitIconName.LocalAdmin,
        ChildItems =
        [
            new() { Text = ""Dashboard"", IconName = BitIconName.ViewDashboard },
            new() { Text = ""Categories"", IconName = BitIconName.BuildQueue },
            new() { Text = ""Products"", IconName = BitIconName.Product },
        ]
    },
    new() { Text = ""Todo sample"", IconName = BitIconName.ToDoLogoOutline },
    new() { Text = ""BlazorUI"", IconName = BitIconName.F12DevTools },
    new() { Text = ""Bit academy"", IconName = BitIconName.LearningTools, IsEnabled = false },
    new() { Text = ""Contact us"", IconName = BitIconName.Contact },
];";

    private readonly string example7RazorCode = @"
<BitNav Items=""singleExpandNavItems"" SingleExpand FitWidth />";
    private readonly string example7CsharpCode = @"
private static readonly List<BitNavItem> singleExpandNavItems =
[
    new()
    {
        Text = ""Fast foods"",
        IconName = BitIconName.HeartBroken,
        ChildItems =
        [
            new() { Text = ""Burgers"", ChildItems = [new() { Text = ""Beef Burger"" }, new() { Text = ""Veggie Burger"" }] },
            new() { Text = ""Pizza"", ChildItems = [new() { Text = ""Cheese Pizza"" }, new() { Text = ""Meat Pizza"" }] },
            new() { Text = ""French Fries"" },
        ]
    },
    new()
    {
        Text = ""Fruits"",
        IconName = BitIconName.Health,
        ChildItems = [new() { Text = ""Apple"" }, new() { Text = ""Orange"" }, new() { Text = ""Banana"" }]
    },
    new()
    {
        Text = ""Drinks"",
        IconName = BitIconName.Coffee,
        ChildItems = [new() { Text = ""Water"" }, new() { Text = ""Tea"" }]
    },
];";

    private readonly string example8RazorCode = @"
<BitNav Items=""noCollapseNavItems"" AllExpanded NoCollapse />";
    private readonly string example8CsharpCode = @"
private static readonly List<BitNavItem> noCollapseNavItems = [ /* the same shape as basicNavItems */ ];";

    private readonly string example9RazorCode = @"
<BitNav Items=""chevronNavItems"" ReversedChevron AllExpanded FitWidth />

<BitNav Items=""chevronNavItems"" ChevronDownIconName=""@BitIconName.CircleAdditionSolid"" AllExpanded FitWidth />

<BitNav Items=""chevronNavItems"" IndentValue=""40"" IndentPadding=""40"" AllExpanded FitWidth />";
    private readonly string example9CsharpCode = @"
private static readonly List<BitNavItem> chevronNavItems =
[
    new()
    {
        Text = ""bit platform"",
        IconName = BitIconName.Website,
        ChildItems =
        [
            new() { Text = ""Home"", IconName = BitIconName.Home },
            new()
            {
                Text = ""Products & Services"",
                IconName = BitIconName.Product,
                ChildItems =
                [
                    new() { Text = ""BlazorUI"", IconName = BitIconName.F12DevTools },
                    new() { Text = ""Pricing"", IconName = BitIconName.Money },
                ]
            },
        ]
    },
    new() { Text = ""Iconography"", IconName = BitIconName.AppIconDefault },
];";

    private readonly string example10RazorCode = @"
<BitNav Items=""carNavItems"" RenderType=""BitNavRenderType.Grouped"">
    <HeaderTemplate Context=""item"">
        <div class=""nav-custom-header"">
            <BitIcon IconName=""@BitIconName.FavoriteStarFill"" />
            <span>@item.Text</span>
        </div>
    </HeaderTemplate>
</BitNav>

<BitNav Items=""foodNavItems"" Mode=""BitNavMode.Manual"">
    <ItemTemplate Context=""item"">
        <div class=""nav-custom-item"">
            <BitCheckbox />
            <BitIcon IconName=""@item.IconName"" />
            <span>@item.Text</span>
        </div>
    </ItemTemplate>
</BitNav>

<BitNav Items=""colorNavItems"" FitWidth ItemTemplateRenderMode=""BitNavItemTemplateRenderMode.Replace"">
    <ItemTemplate Context=""item"">
        <div class=""nav-custom-item"">
            <BitIcon IconName=""@item.IconName"" />
            <span>@item.Text</span>
            <BitTag Color=""BitColor.SecondaryBackground"">custom</BitTag>
        </div>
    </ItemTemplate>
</BitNav>";
    private readonly string example10CsharpCode = @"
private static readonly List<BitNavItem> colorNavItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home },
    new() { Text = ""Products"", IconName = BitIconName.Product },
    new() { Text = ""Settings"", IconName = BitIconName.Settings },
];";

    private readonly string example11RazorCode = @"
<BitStack Horizontal Wrap>
    <BitButton OnClick=""ExpandAllApiItems"">ExpandAll</BitButton>
    <BitButton OnClick=""CollapseAllApiItems"">CollapseAll</BitButton>
    <BitButton OnClick=""ToggleFruitsApiItem"">Toggle Fruits</BitButton>
    <BitButton OnClick=""ExpandFastFoodsApiItem"">Expand Fast foods</BitButton>
    <BitButton OnClick=""CollapseFastFoodsApiItem"">Collapse Fast foods</BitButton>
    <BitButton OnClick=""SelectIceCreamApiItem"">Select Ice Cream</BitButton>
    <BitButton OnClick=""FocusVeggieBurgerApiItem"">Focus Veggie Burger</BitButton>
</BitStack>

<BitNav @ref=""apiNavRef"" Items=""apiNavItems"" Mode=""BitNavMode.Manual"" FitWidth />";
    private readonly string example11CsharpCode = @"
private BitNav<BitNavItem>? apiNavRef;

private void ExpandAllApiItems() => apiNavRef?.ExpandAll();
private void CollapseAllApiItems() => apiNavRef?.CollapseAll();
private async Task ToggleFruitsApiItem() { if (apiNavRef is not null) await apiNavRef.ToggleItem(apiNavItems[1]); }
private async Task ExpandFastFoodsApiItem() { if (apiNavRef is not null) await apiNavRef.ExpandItem(apiNavItems[0]); }
private async Task CollapseFastFoodsApiItem() { if (apiNavRef is not null) await apiNavRef.CollapseItem(apiNavItems[0]); }
private async Task SelectIceCreamApiItem() { if (apiNavRef is not null) await apiNavRef.SelectItem(apiNavItems[2]); }
private async Task FocusVeggieBurgerApiItem() { if (apiNavRef is not null) await apiNavRef.FocusItem(apiNavItems[0].ChildItems[0].ChildItems[1]); }

private static readonly List<BitNavItem> apiNavItems =
[
    new()
    {
        Text = ""Fast foods"",
        IconName = BitIconName.HeartBroken,
        ChildItems =
        [
            new() { Text = ""Burgers"", ChildItems = [new() { Text = ""Beef Burger"" }, new() { Text = ""Veggie Burger"" }] },
            new() { Text = ""Pizza"", ChildItems = [new() { Text = ""Cheese Pizza"" }, new() { Text = ""Meat Pizza"" }] },
        ]
    },
    new()
    {
        Text = ""Fruits"",
        IconName = BitIconName.Health,
        ChildItems = [new() { Text = ""Apple"" }, new() { Text = ""Orange"" }]
    },
    new() { Text = ""Ice Cream"", IconName = BitIconName.Emoji2 },
    new() { Text = ""Cookie"", IconName = BitIconName.Cake },
];";

    private readonly string example12RazorCode = @"
<BitNav Items=""foodNavItems""
        Mode=""BitNavMode.Manual""
        OnItemClick=""(BitNavItem item) => ClickedItem = item""
        OnSelectItem=""(BitNavItem item) => SelectedItem = item""
        OnItemToggle=""(BitNavItem item) => ToggledItem = item"" />

<div>
    <span>Clicked Item: <b>@ClickedItem?.Text</b></span><br />
    <span>Selected Item: <b>@SelectedItem?.Text</b></span><br />
    <span>Toggled Item: <b>@(ToggledItem is null ? ""N/A"" : $""{ToggledItem.Text} ({(ToggledItem.IsExpanded ? ""Expanded"" : ""Collapsed"")})"")</b></span>
</div>";
    private readonly string example12CsharpCode = @"
private BitNavItem ClickedItem = default!;
private BitNavItem SelectedItem = default!;
private BitNavItem ToggledItem = default!;";

    private readonly string example13RazorCode = @"
<BitNav Items=""matchNavItems"" FitWidth />

<BitNav Items=""prefixMatchNavItems"" Match=""BitNavMatch.Prefix"" FitWidth />

<BitNav Items=""wildcardMatchNavItems"" Match=""BitNavMatch.Wildcard"" FitWidth />

<BitNav Items=""regexMatchNavItems"" Match=""BitNavMatch.Regex"" FitWidth />

<BitNav Items=""itemMatchNavItems"" Match=""BitNavMatch.Exact"" FitWidth />

<BitNav Items=""additionalUrlsNavItems"" FitWidth />";
    private readonly string example13CsharpCode = @"
private static readonly List<BitNavItem> matchNavItems =
[
    new() { Text = ""Nav (this page)"", IconName = BitIconName.GlobalNavButton, Url = ""/components/nav"" },
    new() { Text = ""Pivot"", IconName = BitIconName.MiniExpand, Url = ""/components/pivot"" },
];

private static readonly List<BitNavItem> prefixMatchNavItems =
[
    new() { Text = ""Components (/components)"", IconName = BitIconName.F12DevTools, Url = ""/components"" },
    new() { Text = ""Iconography (/iconography)"", IconName = BitIconName.AppIconDefault, Url = ""/iconography"" },
];

private static readonly List<BitNavItem> wildcardMatchNavItems =
[
    new() { Text = ""A component page (/components/*)"", IconName = BitIconName.F12DevTools, Url = ""/components/*"" },
    new() { Text = ""A pro page (/pro/**)"", IconName = BitIconName.Trophy2, Url = ""/pro/**"" },
];

private static readonly List<BitNavItem> regexMatchNavItems =
[
    new() { Text = ""Nav or NavBar (^/components/nav(bar)?$)"", IconName = BitIconName.GlobalNavButton, Url = ""^/components/nav(bar)?$"" },
    new() { Text = ""A page starting with P (^/components/p)"", IconName = BitIconName.Page, Url = ""^/components/p"" },
];

private static readonly List<BitNavItem> itemMatchNavItems =
[
    new() { Text = ""Components (its own Prefix)"", IconName = BitIconName.F12DevTools, Url = ""/components"", Match = BitNavMatch.Prefix },
    new() { Text = ""Pivot (the Exact of the nav)"", IconName = BitIconName.MiniExpand, Url = ""/components/pivot"" },
];

private static readonly List<BitNavItem> additionalUrlsNavItems =
[
    new()
    {
        Text = ""Navigation (also /components/nav)"",
        IconName = BitIconName.GlobalNavButton,
        Url = ""/components/navbar"",
        AdditionalUrls = [""/components/nav"", ""/components/breadcrumb""]
    },
    new() { Text = ""Inputs"", IconName = BitIconName.TextField, Url = ""/components/textfield"" },
];";

    private readonly string example14RazorCode = @"
<BitNav FitWidth Color=""BitColor.Primary"" Items=""colorNavItems"" Mode=""BitNavMode.Manual"" DefaultSelectedItem=""colorNavItems[0]"" />
<BitNav FitWidth Color=""BitColor.Secondary"" Items=""colorNavItems"" Mode=""BitNavMode.Manual"" DefaultSelectedItem=""colorNavItems[0]"" />
<BitNav FitWidth Color=""BitColor.Tertiary"" Items=""colorNavItems"" Mode=""BitNavMode.Manual"" DefaultSelectedItem=""colorNavItems[0]"" />
<BitNav FitWidth Color=""BitColor.Info"" Items=""colorNavItems"" Mode=""BitNavMode.Manual"" DefaultSelectedItem=""colorNavItems[0]"" />
<BitNav FitWidth Color=""BitColor.Success"" Items=""colorNavItems"" Mode=""BitNavMode.Manual"" DefaultSelectedItem=""colorNavItems[0]"" />
<BitNav FitWidth Color=""BitColor.Warning"" Items=""colorNavItems"" Mode=""BitNavMode.Manual"" DefaultSelectedItem=""colorNavItems[0]"" />
<BitNav FitWidth Color=""BitColor.SevereWarning"" Items=""colorNavItems"" Mode=""BitNavMode.Manual"" DefaultSelectedItem=""colorNavItems[0]"" />
<BitNav FitWidth Color=""BitColor.Error"" Items=""colorNavItems"" Mode=""BitNavMode.Manual"" DefaultSelectedItem=""colorNavItems[0]"" />

<BitNav FitWidth Accent=""BitColor.Primary"" Items=""accentNavItems"" Mode=""BitNavMode.Manual"" DefaultSelectedItem=""accentNavItems[0]"" />
<BitNav FitWidth Accent=""BitColor.Success"" Items=""accentNavItems"" Mode=""BitNavMode.Manual"" DefaultSelectedItem=""accentNavItems[0]"" />
<BitNav FitWidth Accent=""BitColor.Warning"" Items=""accentNavItems"" Mode=""BitNavMode.Manual"" DefaultSelectedItem=""accentNavItems[0]"" />
<BitNav FitWidth Accent=""BitColor.Error"" Items=""accentNavItems"" Mode=""BitNavMode.Manual"" DefaultSelectedItem=""accentNavItems[0]"" />";
    private readonly string example14CsharpCode = @"
private static readonly List<BitNavItem> colorNavItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home },
    new() { Text = ""Products"", IconName = BitIconName.Product },
    new() { Text = ""Settings"", IconName = BitIconName.Settings },
];

private static readonly List<BitNavItem> accentNavItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home },
    new() { Text = ""Products"", IconName = BitIconName.Product },
    new() { Text = ""Settings"", IconName = BitIconName.Settings },
];";

    private readonly string example15RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitNav Items=""externalIconNavItems"" FitWidth />

<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitNav Items=""bootstrapIconNavItems"" ChevronDownIcon=""bootstrapChevronIcon"" FitWidth />";
    private readonly string example15CsharpCode = @"
private static readonly List<BitNavItem> externalIconNavItems =
[
    new()
    {
        Text = ""bit platform"",
        Description = ""Nav with external icons (FontAwesome)"",
        ChildItems =
        [
            new() { Text = ""Home"", Icon = BitIconInfo.Css(""fa-solid fa-house""), Url = ""https://bitplatform.dev/"" },
            new()
            {
                Text = ""Products & Services"",
                ChildItems =
                [
                    new() { Text = ""BlazorUI"", Icon = BitIconInfo.Fa(""solid code""), Url = ""https://bitplatform.dev/components"" },
                    new() { Text = ""Pricing"", Icon = BitIconInfo.Css(""fa-solid fa-tag""), Url = ""https://bitplatform.dev/pricing"" },
                ]
            },
            new() { Text = ""About"", Icon = BitIconInfo.Fa(""solid circle-info""), Url = ""https://bitplatform.dev/about-us"" },
            new() { Text = ""Contact us"", Icon = BitIconInfo.Css(""fa-solid fa-envelope""), Url = ""https://bitplatform.dev/contact-us"" },
        ],
    },
    new() { Text = ""Iconography"", Icon = BitIconInfo.Css(""fa-solid fa-icons""), Url = ""/iconography"" },
];

private static readonly BitIconInfo bootstrapChevronIcon = BitIconInfo.Bi(""chevron-right"");

private static readonly List<BitNavItem> bootstrapIconNavItems =
[
    new()
    {
        Text = ""bit platform"",
        Description = ""Nav with external icons (Bootstrap Icons)"",
        ChildItems =
        [
            new() { Text = ""Home"", Icon = BitIconInfo.Bi(""house-fill""), Url = ""https://bitplatform.dev/"" },
            new() { Text = ""BlazorUI"", Icon = BitIconInfo.Bi(""code-slash""), Url = ""https://bitplatform.dev/components"" },
            new() { Text = ""Pricing"", Icon = BitIconInfo.Bi(""tag-fill""), Url = ""https://bitplatform.dev/pricing"" },
        ],
    },
    new() { Text = ""Iconography"", Icon = BitIconInfo.Bi(""emoji-smile""), Url = ""/iconography"" },
];";

    private readonly string example16RazorCode = @"
<BitNav FitWidth Size=""BitSize.Small"" Items=""sizeNavItems"" />

<BitNav FitWidth Size=""BitSize.Medium"" Items=""sizeNavItems"" />

<BitNav FitWidth Size=""BitSize.Large"" Items=""sizeNavItems"" />";
    private readonly string example16CsharpCode = @"
private static readonly List<BitNavItem> sizeNavItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home, Description = ""The main page"" },
    new() { Text = ""Products"", IconName = BitIconName.Product, Description = ""All of the products"" },
    new() { Text = ""Settings"", IconName = BitIconName.Settings, Description = ""The app settings"" },
];";

    private readonly string example17RazorCode = @"
<BitNav Items=""colorNavItems"" Style=""max-width: max-content; border: 1px solid tomato;"" />

<BitNav Items=""colorNavItems"" Class=""custom-class"" />

<BitNav Items=""styleClassNavItems"" FitWidth />

<BitNav Items=""customStyleNavItems""
        AllExpanded
        Mode=""BitNavMode.Manual""
        DefaultSelectedItem=""customStyleNavItems[1]""
        Styles=""@(new() { Root = ""background: #2a2a2a; border-radius: 8px; padding: 4px;"",
                          ItemContainer = ""border: 1px solid green; margin: 2px;"",
                          SelectedItemContainer = ""border-color: gold;"",
                          ToggleButton = ""color: cyan;"",
                          ToggleIcon = ""font-size: 12px;"",
                          Item = ""color: orangered;"",
                          SelectedItem = ""color: gold;"",
                          ItemIcon = ""color: gold; margin-inline-end: 15px;"",
                          ItemText = ""letter-spacing: 1px;"",
                          Description = ""color: darkseagreen;"",
                          Separator = ""border-color: cyan;"" })"" />

<BitNav Items=""customClassNavItems""
        AllExpanded
        RenderType=""BitNavRenderType.Grouped""
        Classes=""@(new() { Root = ""custom-root"",
                           Header = ""custom-header"",
                           HeaderText = ""custom-header-text"",
                           ItemContainer = ""custom-item-container"",
                           Item = ""custom-item"",
                           ItemIcon = ""custom-item-icon"",
                           ItemText = ""custom-item-text"",
                           ToggleIcon = ""custom-toggle-icon"",
                           Description = ""custom-description"" })"" />";
    private readonly string example17CsharpCode = @"
private static readonly List<BitNavItem> styleClassNavItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home, Style = ""background: rgba(255,99,71,0.2);"" },
    new() { Text = ""Products"", IconName = BitIconName.Product, Class = ""custom-item-list"" },
    new() { Text = ""Settings"", IconName = BitIconName.Settings },
];

private static readonly List<BitNavItem> customStyleNavItems =
[
    new()
    {
        Text = ""bit platform"",
        Description = ""the bit platform description"",
        ChildItems =
        [
            new() { Text = ""Home"", IconName = BitIconName.Home, Url = ""https://bitplatform.dev/"" },
            new() { Text = ""BlazorUI"", IconName = BitIconName.F12DevTools, Url = ""https://bitplatform.dev/components"" },
        ],
    },
    new() { Text = ""Iconography"", IconName = BitIconName.AppIconDefault, Url = ""/iconography"" },
    new() { IsSeparator = true },
    new() { Text = ""Contact us"", IconName = BitIconName.Contact, Url = ""https://bitplatform.dev/contact-us"" },
];

private static readonly List<BitNavItem> customClassNavItems =
[
    new()
    {
        Text = ""bit platform"",
        IconName = BitIconName.Website,
        Description = ""the bit platform description"",
        ChildItems =
        [
            new() { Text = ""Home"", IconName = BitIconName.Home, Url = ""https://bitplatform.dev/"" },
            new() { Text = ""BlazorUI"", IconName = BitIconName.F12DevTools, Url = ""https://bitplatform.dev/components"" },
        ],
    },
    new()
    {
        Text = ""Community"",
        IconName = BitIconName.Group,
        ChildItems = [new() { Text = ""GitHub repo"", IconName = BitIconName.GitGraph, Url = ""https://github.com/bitfoundation/bitplatform"" }]
    },
];";

    private readonly string example18RazorCode = @"
<div dir=""rtl"">
    <BitNav Dir=""BitDir.Rtl"" Items=""rtlNavItems"" />
</div>";
    private readonly string example18CsharpCode = @"
private static readonly List<BitNavItem> rtlNavItems =
[
    new()
    {
        Text = ""پلتفرمِ بیت"",
        Description = ""توضیحاتِ پلتفرمِ بیت"",
        ChildItems =
        [
            new() { Text = ""خانه"", IconName = BitIconName.Home, Url = ""https://bitplatform.dev/"" },
            new()
            {
                Text = ""محصولات و خدمات"",
                ChildItems =
                [
                    new()
                    {
                        Text = ""قالب های پروژه"",
                        ChildItems =
                        [
                            new() { Text = ""نمونه ی Todo"", IconName = BitIconName.ToDoLogoOutline, Url = ""https://bitplatform.dev/templates/overview"" },
                            new() { Text = ""نمونه ی AdminPanel"", IconName = BitIconName.LocalAdmin, Url = ""https://bitplatform.dev/templates/overview"" },
                        ]
                    },
                    new() { Text = ""رابط کاربری Blazor"", IconName = BitIconName.F12DevTools, Url = ""https://blazorui.bitplatform.dev/"" },
                    new() { Text = ""راه های هاست ابری"", IconName = BitIconName.Cloud, Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                    new() { Text = ""آکادمی بیت"", IconName = BitIconName.LearningTools, Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                ]
            },
            new() { Text = ""قیمت"", IconName = BitIconName.Money, Url = ""https://bitplatform.dev/pricing"" },
            new() { Text = ""درباره ما"", IconName = BitIconName.Info, Url = ""https://bitplatform.dev/about-us"" },
            new() { Text = ""ارتباط با ما"", IconName = BitIconName.Contact, Url = ""https://bitplatform.dev/contact-us"" },
        ],
    },
    new()
    {
        Text = ""انجمن ها"",
        ChildItems =
        [
            new() { Text = ""لینکدین"", IconName = BitIconName.LinkedInLogo, Url = ""https://www.linkedin.com/company/bitplatformhq"" },
            new() { Text = ""توییتر"", IconName = BitIconName.Globe, Url = ""https://twitter.com/bitplatformhq"" },
            new() { Text = ""گیتهاب"", IconName = BitIconName.GitGraph, Url = ""https://github.com/bitfoundation/bitplatform"" },
        ]
    },
    new() { Text = ""شمایل نگاری"", IconName = BitIconName.AppIconDefault, Url = ""/iconography"" },
];";
}

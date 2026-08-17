namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Nav;

public partial class _BitNavCustomDemo
{
    private readonly string example1RazorCode = @"
<BitNav Items=""customBasicNavItems"" NameSelectors=""sectionSelectors"" />";
    private readonly string example1CsharpCode = @"
public class Section
{
    public string Text { get; set; } = string.Empty;
    public string? ImageName { get; set; }
    public string? Url { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsExpanded { get; set; }
    public bool IsDivider { get; set; }
    public List<Section> Links { get; set; } = [];
    public string? Comment { get; set; }
    public string? Style { get; set; }
    public string? Class { get; set; }
}

// Only the renamed members are mapped; the rest keep matching BitNavItem by convention.
private static readonly BitNavNameSelectors<Section> sectionSelectors = new()
{
    IconName = { Name = nameof(Section.ImageName) },
    ChildItems = { Name = nameof(Section.Links) },
    Description = { Name = nameof(Section.Comment) },
    IsSeparator = { Name = nameof(Section.IsDivider) },
};

private static readonly List<Section> customBasicNavItems =
[
    new()
    {
        Text = ""bit platform"",
        Comment = ""the bit platform description"",
        Links =
        [
            new() { Text = ""Home"", ImageName = BitIconName.Home, Url = ""https://bitplatform.dev/"" },
            new()
            {
                Text = ""Products & Services"",
                Links =
                [
                    new()
                    {
                        Text = ""Project Templates"",
                        Links =
                        [
                            new() { Text = ""Todo sample"", ImageName = BitIconName.ToDoLogoOutline, Url = ""https://bitplatform.dev/templates/overview"" },
                            new() { Text = ""AdminPanel sample"", ImageName = BitIconName.LocalAdmin, Url = ""https://bitplatform.dev/templates/overview"" },
                        ]
                    },
                    new() { Text = ""BlazorUI"", ImageName = BitIconName.F12DevTools, Url = ""https://bitplatform.dev/components"" },
                    new() { Text = ""Cloud hosting solutions"", ImageName = BitIconName.Cloud, Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                    new() { Text = ""Bit academy"", ImageName = BitIconName.LearningTools, Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                ]
            },
            new() { Text = ""Pricing"", ImageName = BitIconName.Money, Url = ""https://bitplatform.dev/pricing"" },
            new() { Text = ""About"", ImageName = BitIconName.Info, Url = ""https://bitplatform.dev/about-us"" },
            new() { Text = ""Contact us"", ImageName = BitIconName.Contact, Url = ""https://bitplatform.dev/contact-us"" },
        ],
    },
    new()
    {
        Text = ""Community"",
        Links =
        [
            new() { Text = ""LinkedIn"", ImageName = BitIconName.LinkedInLogo, Url = ""https://www.linkedin.com/company/bitplatformhq"" },
            new() { Text = ""Twitter"", ImageName = BitIconName.Globe, Url = ""https://twitter.com/bitplatformhq"" },
            new() { Text = ""GitHub repo"", ImageName = BitIconName.GitGraph, Url = ""https://github.com/bitfoundation/bitplatform"" },
        ]
    },
    new() { Text = ""Iconography"", ImageName = BitIconName.AppIconDefault, Url = ""/iconography"" },
];";

    private readonly string example2RazorCode = @"
<BitNav Items=""customBasicNavItems"" FitWidth NameSelectors=""sectionSelectors"" />

<BitNav Items=""customBasicNavItems"" FullWidth NameSelectors=""sectionSelectors"" />";
    private readonly string example2CsharpCode = @"
private static readonly List<Section> customBasicNavItems = [ /* see the Basic example */ ];";

    private readonly string example3RazorCode = @"
<BitNav Items=""customCarNavItems"" RenderType=""BitNavRenderType.Grouped"" NameSelectors=""carSelectors"" />";
    private readonly string example3CsharpCode = @"
public class CarMenu
{
    public string Name { get; set; } = string.Empty;
    public string? Tooltip { get; set; }
    public string? PageUrl { get; set; }
    public string? UrlTarget { get; set; }
    public string? ExpandedAriaLabel { get; set; }
    public string? CollapsedAriaLabel { get; set; }
    public bool IsExpandedParent { get; set; }
    public string? Comment { get; set; }
    public List<CarMenu> Links { get; set; } = [];
}

private static readonly BitNavNameSelectors<CarMenu> carSelectors = new()
{
    Text = { Name = nameof(CarMenu.Name) },
    Url = { Name = nameof(CarMenu.PageUrl) },
    Target = { Name = nameof(CarMenu.UrlTarget) },
    Title = { Name = nameof(CarMenu.Tooltip) },
    IsExpanded = { Name = nameof(CarMenu.IsExpandedParent) },
    CollapseAriaLabel = { Name = nameof(CarMenu.CollapsedAriaLabel) },
    ExpandAriaLabel = { Name = nameof(CarMenu.ExpandedAriaLabel) },
    ChildItems = { Name = nameof(CarMenu.Links) },
    Description = { Name = nameof(CarMenu.Comment) },
};

private static readonly List<CarMenu> customCarNavItems =
[
    new()
    {
        Name = ""Mercedes-Benz"",
        ExpandedAriaLabel = ""Mercedes-Benz Expanded"",
        CollapsedAriaLabel = ""Mercedes-Benz Collapsed"",
        Tooltip = ""Mercedes-Benz Car Models"",
        IsExpandedParent = true,
        Comment = ""Cars manufactured under the brand of Mercedes-Benz"",
        Links =
        [
            new()
            {
                Name = ""SUVs"",
                Links =
                [
                    new() { Name = ""GLA"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/gla/suv"", UrlTarget = ""_blank"" },
                    new() { Name = ""GLB"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/glb/suv"", UrlTarget = ""_blank"" },
                    new() { Name = ""GLC"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/glc/suv"", UrlTarget = ""_blank"" },
                ]
            },
            new()
            {
                Name = ""Sedans & Wagons"",
                Links =
                [
                    new() { Name = ""A Class"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/a-class/sedan"", UrlTarget = ""_blank"" },
                    new() { Name = ""C Class"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/c-class/sedan"", UrlTarget = ""_blank"" },
                    new() { Name = ""E Class"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/e-class/sedan"", UrlTarget = ""_blank"" },
                ]
            },
            new()
            {
                Name = ""Coupes"",
                Links =
                [
                    new() { Name = ""CLA Coupe"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/cla/coupe"", UrlTarget = ""_blank"" },
                    new() { Name = ""C Class Coupe"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/c-class/coupe"", UrlTarget = ""_blank"" },
                    new() { Name = ""E Class Coupe"", PageUrl = ""https://www.mbusa.com/en/vehicles/class/e-class/coupe"", UrlTarget = ""_blank"" },
                ]
            },
        ]
    },
    new()
    {
        Name = ""Tesla"",
        ExpandedAriaLabel = ""Tesla Expanded"",
        CollapsedAriaLabel = ""Tesla Collapsed"",
        Tooltip = ""Tesla Car Models"",
        Links =
        [
            new() { Name = ""Model S"", PageUrl = ""https://www.tesla.com/models"", UrlTarget = ""_blank"" },
            new() { Name = ""Model X"", PageUrl = ""https://www.tesla.com/modelx"", UrlTarget = ""_blank"" },
            new() { Name = ""Model Y"", PageUrl = ""https://www.tesla.com/modely"", UrlTarget = ""_blank"" },
        ]
    },
];";

    private readonly string example4RazorCode = @"
<BitNav Items=""customSeparatorNavItems"" FitWidth NameSelectors=""sectionSelectors"" />";
    private readonly string example4CsharpCode = @"
private static readonly List<Section> customSeparatorNavItems =
[
    new() { Text = ""Home"", ImageName = BitIconName.Home, Url = ""https://bitplatform.dev/"" },
    new() { Text = ""Pricing"", ImageName = BitIconName.Money, Url = ""https://bitplatform.dev/pricing"" },
    new() { IsDivider = true },
    new() { Text = ""LinkedIn"", ImageName = BitIconName.LinkedInLogo, Url = ""https://www.linkedin.com/company/bitplatformhq"" },
    new() { Text = ""GitHub repo"", ImageName = BitIconName.GitGraph, Url = ""https://github.com/bitfoundation/bitplatform"" },
    new() { IsDivider = true },
    new() { Text = ""Contact us"", ImageName = BitIconName.Contact, Url = ""https://bitplatform.dev/contact-us"" },
];";

    private readonly string example5RazorCode = @"
<BitNav Items=""customFoodNavItems""
        Mode=""BitNavMode.Manual""
        NameSelectors=""foodSelectors""
        DefaultSelectedItem=""customFoodNavItems[0].Childs[2]"" />

<BitNav @bind-SelectedItem=""CustomSelectedFood""
        Items=""customFoodNavItems""
        Mode=""BitNavMode.Manual""
        NameSelectors=""foodSelectors""
        OnSelectItem=""(FoodMenu item) => CustomSelectedFoodName = FoodMenuDropdownItems.Single(i => i.Text == item.Name).Text"" />

<BitDropdown @bind-Value=""CustomSelectedFoodName""
             FitWidth
             Label=""Select Item""
             Items=""FoodMenuDropdownItems""
             OnSelectItem=""(BitDropdownItem<string> item) => CustomSelectedFood = Flatten(customFoodNavItems).Single(i => i.Name == item.Value)"" />";
    private readonly string example5CsharpCode = @"
public class FoodMenu
{
    public string Name { get; set; } = string.Empty;
    public string? Image { get; set; }
    public bool IsExpanded { get; set; }
    public string? Comment { get; set; }
    public List<FoodMenu> Childs { get; set; } = [];
}

// The same mapping written with Selector lambdas: it skips the reflection and is checked by the compiler.
private static readonly BitNavNameSelectors<FoodMenu> foodSelectors = new()
{
    Text = { Selector = item => item.Name },
    IconName = { Selector = item => item.Image },
    ChildItems = { Selector = item => item.Childs },
    Description = { Selector = item => item.Comment },
};

private static readonly List<FoodMenu> customFoodNavItems =
[
    new()
    {
        Name = ""Fast foods"",
        Image = BitIconName.HeartBroken,
        IsExpanded = true,
        Comment = ""List of fast foods"",
        Childs =
        [
            new()
            {
                Name = ""Burgers"",
                Comment = ""List of burgers"",
                Childs =
                [
                    new() { Name = ""Beef Burger"" },
                    new() { Name = ""Veggie Burger"" },
                    new() { Name = ""Bison Burger"" },
                    new() { Name = ""Wild Salmon Burger"" },
                ]
            },
            new()
            {
                Name = ""Pizza"",
                Childs =
                [
                    new() { Name = ""Cheese Pizza"" },
                    new() { Name = ""Veggie Pizza"" },
                    new() { Name = ""Pepperoni Pizza"" },
                    new() { Name = ""Meat Pizza"" },
                ]
            },
            new() { Name = ""French Fries"" },
        ]
    },
    new()
    {
        Name = ""Fruits"",
        Image = BitIconName.Health,
        Childs = [new() { Name = ""Apple"" }, new() { Name = ""Orange"" }, new() { Name = ""Banana"" }]
    },
    new() { Name = ""Ice Cream"" },
    new() { Name = ""Cookie"" },
];

private static List<FoodMenu> Flatten(IList<FoodMenu> e) => e.SelectMany(c => Flatten(c.Childs)).Concat(e).ToList();

private FoodMenu CustomSelectedFood = customFoodNavItems[0].Childs[2];
private string? CustomSelectedFoodName = customFoodNavItems[0].Childs[2].Name;";

    private readonly string example6RazorCode = @"
<BitToggle @bind-Value=""iconOnly"" Label=""Hide texts?"" Inline />

<BitNav Items=""customIconOnlyNavItems"" Mode=""BitNavMode.Manual"" IconOnly=""iconOnly"" NameSelectors=""sectionSelectors"" />";
    private readonly string example6CsharpCode = @"
private bool iconOnly;

private static readonly List<Section> customIconOnlyNavItems =
[
    new() { Text = ""Home"", ImageName = BitIconName.Home },
    new()
    {
        Text = ""AdminPanel sample"",
        ImageName = BitIconName.LocalAdmin,
        Links =
        [
            new() { Text = ""Dashboard"", ImageName = BitIconName.ViewDashboard },
            new() { Text = ""Categories"", ImageName = BitIconName.BuildQueue },
            new() { Text = ""Products"", ImageName = BitIconName.Product },
        ]
    },
    new() { Text = ""Todo sample"", ImageName = BitIconName.ToDoLogoOutline },
    new() { Text = ""BlazorUI"", ImageName = BitIconName.F12DevTools },
    new() { Text = ""Bit academy"", ImageName = BitIconName.LearningTools, IsEnabled = false },
    new() { Text = ""Contact us"", ImageName = BitIconName.Contact },
];";

    private readonly string example7RazorCode = @"
<BitNav Items=""customSingleExpandNavItems"" SingleExpand FitWidth NameSelectors=""sectionSelectors"" />";
    private readonly string example7CsharpCode = @"
private static readonly List<Section> customSingleExpandNavItems =
[
    new()
    {
        Text = ""Fast foods"",
        ImageName = BitIconName.HeartBroken,
        Links =
        [
            new() { Text = ""Burgers"", Links = [new() { Text = ""Beef Burger"" }, new() { Text = ""Veggie Burger"" }] },
            new() { Text = ""Pizza"", Links = [new() { Text = ""Cheese Pizza"" }, new() { Text = ""Meat Pizza"" }] },
            new() { Text = ""French Fries"" },
        ]
    },
    new()
    {
        Text = ""Fruits"",
        ImageName = BitIconName.Health,
        Links = [new() { Text = ""Apple"" }, new() { Text = ""Orange"" }, new() { Text = ""Banana"" }]
    },
    new()
    {
        Text = ""Drinks"",
        ImageName = BitIconName.Coffee,
        Links = [new() { Text = ""Water"" }, new() { Text = ""Tea"" }]
    },
];";

    private readonly string example8RazorCode = @"
<BitNav Items=""customNoCollapseNavItems"" AllExpanded NoCollapse NameSelectors=""sectionSelectors"" />";
    private readonly string example8CsharpCode = @"
private static readonly List<Section> customNoCollapseNavItems = [ /* the same shape as customBasicNavItems */ ];";

    private readonly string example9RazorCode = @"
<BitNav Items=""customChevronNavItems"" ReversedChevron AllExpanded FitWidth NameSelectors=""sectionSelectors"" />

<BitNav Items=""customChevronNavItems"" ChevronDownIconName=""@BitIconName.CircleAdditionSolid"" AllExpanded FitWidth NameSelectors=""sectionSelectors"" />

<BitNav Items=""customChevronNavItems"" IndentValue=""40"" IndentPadding=""40"" AllExpanded FitWidth NameSelectors=""sectionSelectors"" />";
    private readonly string example9CsharpCode = @"
private static readonly List<Section> customChevronNavItems =
[
    new()
    {
        Text = ""bit platform"",
        ImageName = BitIconName.Website,
        Links =
        [
            new() { Text = ""Home"", ImageName = BitIconName.Home },
            new()
            {
                Text = ""Products & Services"",
                ImageName = BitIconName.Product,
                Links =
                [
                    new() { Text = ""BlazorUI"", ImageName = BitIconName.F12DevTools },
                    new() { Text = ""Pricing"", ImageName = BitIconName.Money },
                ]
            },
        ]
    },
    new() { Text = ""Iconography"", ImageName = BitIconName.AppIconDefault },
];";

    private readonly string example10RazorCode = @"
<BitNav Items=""customCarNavItems"" RenderType=""BitNavRenderType.Grouped"" NameSelectors=""carSelectors"">
    <HeaderTemplate Context=""item"">
        <div class=""nav-custom-header"">
            <BitIcon IconName=""@BitIconName.FavoriteStarFill"" />
            <span>@item.Name</span>
        </div>
    </HeaderTemplate>
</BitNav>

<BitNav Items=""customFoodNavItems"" Mode=""BitNavMode.Manual"" NameSelectors=""foodSelectors"">
    <ItemTemplate Context=""item"">
        <div class=""nav-custom-item"">
            <BitCheckbox />
            <BitIcon IconName=""@item.Image"" />
            <span>@item.Name</span>
        </div>
    </ItemTemplate>
</BitNav>

<BitNav Items=""customColorNavItems"" FitWidth NameSelectors=""sectionSelectors"" ItemTemplateRenderMode=""BitNavItemTemplateRenderMode.Replace"">
    <ItemTemplate Context=""item"">
        <div class=""nav-custom-item"">
            <BitIcon IconName=""@item.ImageName"" />
            <span>@item.Text</span>
            <BitTag Color=""BitColor.SecondaryBackground"">custom</BitTag>
        </div>
    </ItemTemplate>
</BitNav>";
    private readonly string example10CsharpCode = @"
private static readonly List<Section> customColorNavItems =
[
    new() { Text = ""Home"", ImageName = BitIconName.Home },
    new() { Text = ""Products"", ImageName = BitIconName.Product },
    new() { Text = ""Settings"", ImageName = BitIconName.Settings },
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

<BitNav @ref=""apiNavRef"" Items=""customApiNavItems"" Mode=""BitNavMode.Manual"" FitWidth NameSelectors=""sectionSelectors"" />";
    private readonly string example11CsharpCode = @"
private BitNav<Section>? apiNavRef;

private void ExpandAllApiItems() => apiNavRef?.ExpandAll();
private void CollapseAllApiItems() => apiNavRef?.CollapseAll();
private async Task ToggleFruitsApiItem() { if (apiNavRef is not null) await apiNavRef.ToggleItem(customApiNavItems[1]); }
private async Task ExpandFastFoodsApiItem() { if (apiNavRef is not null) await apiNavRef.ExpandItem(customApiNavItems[0]); }
private async Task CollapseFastFoodsApiItem() { if (apiNavRef is not null) await apiNavRef.CollapseItem(customApiNavItems[0]); }
private async Task SelectIceCreamApiItem() { if (apiNavRef is not null) await apiNavRef.SelectItem(customApiNavItems[2]); }
private async Task FocusVeggieBurgerApiItem() { if (apiNavRef is not null) await apiNavRef.FocusItem(customApiNavItems[0].Links[0].Links[1]); }

private static readonly List<Section> customApiNavItems =
[
    new()
    {
        Text = ""Fast foods"",
        ImageName = BitIconName.HeartBroken,
        Links =
        [
            new() { Text = ""Burgers"", Links = [new() { Text = ""Beef Burger"" }, new() { Text = ""Veggie Burger"" }] },
            new() { Text = ""Pizza"", Links = [new() { Text = ""Cheese Pizza"" }, new() { Text = ""Meat Pizza"" }] },
        ]
    },
    new()
    {
        Text = ""Fruits"",
        ImageName = BitIconName.Health,
        Links = [new() { Text = ""Apple"" }, new() { Text = ""Orange"" }]
    },
    new() { Text = ""Ice Cream"", ImageName = BitIconName.Emoji2 },
    new() { Text = ""Cookie"", ImageName = BitIconName.Cake },
];";

    private readonly string example12RazorCode = @"
<BitNav Items=""customFoodNavItems""
        Mode=""BitNavMode.Manual""
        NameSelectors=""foodSelectors""
        OnItemClick=""(FoodMenu item) => CustomClickedItem = item""
        OnSelectItem=""(FoodMenu item) => CustomSelectedItem = item""
        OnItemToggle=""(FoodMenu item) => CustomToggledItem = item"" />

<div>
    <span>Clicked Item: <b>@CustomClickedItem?.Name</b></span><br />
    <span>Selected Item: <b>@CustomSelectedItem?.Name</b></span><br />
    <span>Toggled Item: <b>@(CustomToggledItem is null ? ""N/A"" : $""{CustomToggledItem.Name} ({(CustomToggledItem.IsExpanded ? ""Expanded"" : ""Collapsed"")})"")</b></span>
</div>";
    private readonly string example12CsharpCode = @"
private FoodMenu CustomClickedItem = default!;
private FoodMenu CustomSelectedItem = default!;
private FoodMenu CustomToggledItem = default!;";

    private readonly string example13RazorCode = @"
<BitNav Items=""customMatchNavItems"" NameSelectors=""matchSelectors"" FitWidth />

<BitNav Items=""customPrefixMatchNavItems"" NameSelectors=""matchSelectors"" Match=""BitNavMatch.Prefix"" FitWidth />

<BitNav Items=""customWildcardMatchNavItems"" NameSelectors=""matchSelectors"" Match=""BitNavMatch.Wildcard"" FitWidth />

<BitNav Items=""customRegexMatchNavItems"" NameSelectors=""matchSelectors"" Match=""BitNavMatch.Regex"" FitWidth />

<BitNav Items=""customItemMatchNavItems"" NameSelectors=""matchSelectors"" Match=""BitNavMatch.Exact"" FitWidth />

<BitNav Items=""customAdditionalUrlsNavItems"" NameSelectors=""matchSelectors"" FitWidth />";
    private readonly string example13CsharpCode = @"
public class Section
{
    public string Text { get; set; } = string.Empty;
    public string? ImageName { get; set; }
    public string? Url { get; set; }
    public BitNavMatch? UrlMatch { get; set; }
    public IEnumerable<string>? OtherUrls { get; set; }
    public List<Section> Links { get; set; } = [];
}

private static readonly BitNavNameSelectors<Section> matchSelectors = new()
{
    IconName = { Name = nameof(Section.ImageName) },
    ChildItems = { Name = nameof(Section.Links) },
    Match = { Name = nameof(Section.UrlMatch) },
    AdditionalUrls = { Name = nameof(Section.OtherUrls) },
};

private static readonly List<Section> customMatchNavItems =
[
    new() { Text = ""Nav (this page)"", ImageName = BitIconName.GlobalNavButton, Url = ""/components/nav"" },
    new() { Text = ""Pivot"", ImageName = BitIconName.MiniExpand, Url = ""/components/pivot"" },
];

private static readonly List<Section> customPrefixMatchNavItems =
[
    new() { Text = ""Components (/components)"", ImageName = BitIconName.F12DevTools, Url = ""/components"" },
    new() { Text = ""Iconography (/iconography)"", ImageName = BitIconName.AppIconDefault, Url = ""/iconography"" },
];

private static readonly List<Section> customWildcardMatchNavItems =
[
    new() { Text = ""A component page (/components/*)"", ImageName = BitIconName.F12DevTools, Url = ""/components/*"" },
    new() { Text = ""A pro page (/pro/**)"", ImageName = BitIconName.Trophy2, Url = ""/pro/**"" },
];

private static readonly List<Section> customRegexMatchNavItems =
[
    new() { Text = ""Nav or NavBar (^/components/nav(bar)?$)"", ImageName = BitIconName.GlobalNavButton, Url = ""^/components/nav(bar)?$"" },
    new() { Text = ""A page starting with P (^/components/p)"", ImageName = BitIconName.Page, Url = ""^/components/p"" },
];

private static readonly List<Section> customItemMatchNavItems =
[
    new() { Text = ""Components (its own Prefix)"", ImageName = BitIconName.F12DevTools, Url = ""/components"", UrlMatch = BitNavMatch.Prefix },
    new() { Text = ""Pivot (the Exact of the nav)"", ImageName = BitIconName.MiniExpand, Url = ""/components/pivot"" },
];

private static readonly List<Section> customAdditionalUrlsNavItems =
[
    new()
    {
        Text = ""Navigation (also /components/nav)"",
        ImageName = BitIconName.GlobalNavButton,
        Url = ""/components/navbar"",
        OtherUrls = [""/components/nav"", ""/components/breadcrumb""]
    },
    new() { Text = ""Inputs"", ImageName = BitIconName.TextField, Url = ""/components/textfield"" },
];";

    private readonly string example14RazorCode = @"
<BitNav FitWidth Color=""BitColor.Primary"" Items=""customColorNavItems"" NameSelectors=""sectionSelectors"" Mode=""BitNavMode.Manual"" DefaultSelectedItem=""customColorNavItems[0]"" />
<BitNav FitWidth Color=""BitColor.Secondary"" Items=""customColorNavItems"" NameSelectors=""sectionSelectors"" Mode=""BitNavMode.Manual"" DefaultSelectedItem=""customColorNavItems[0]"" />
<BitNav FitWidth Color=""BitColor.Tertiary"" Items=""customColorNavItems"" NameSelectors=""sectionSelectors"" Mode=""BitNavMode.Manual"" DefaultSelectedItem=""customColorNavItems[0]"" />
<BitNav FitWidth Color=""BitColor.Info"" Items=""customColorNavItems"" NameSelectors=""sectionSelectors"" Mode=""BitNavMode.Manual"" DefaultSelectedItem=""customColorNavItems[0]"" />
<BitNav FitWidth Color=""BitColor.Success"" Items=""customColorNavItems"" NameSelectors=""sectionSelectors"" Mode=""BitNavMode.Manual"" DefaultSelectedItem=""customColorNavItems[0]"" />
<BitNav FitWidth Color=""BitColor.Warning"" Items=""customColorNavItems"" NameSelectors=""sectionSelectors"" Mode=""BitNavMode.Manual"" DefaultSelectedItem=""customColorNavItems[0]"" />
<BitNav FitWidth Color=""BitColor.SevereWarning"" Items=""customColorNavItems"" NameSelectors=""sectionSelectors"" Mode=""BitNavMode.Manual"" DefaultSelectedItem=""customColorNavItems[0]"" />
<BitNav FitWidth Color=""BitColor.Error"" Items=""customColorNavItems"" NameSelectors=""sectionSelectors"" Mode=""BitNavMode.Manual"" DefaultSelectedItem=""customColorNavItems[0]"" />

<BitNav FitWidth Accent=""BitColor.Primary"" Items=""customAccentNavItems"" NameSelectors=""sectionSelectors"" Mode=""BitNavMode.Manual"" DefaultSelectedItem=""customAccentNavItems[0]"" />
<BitNav FitWidth Accent=""BitColor.Success"" Items=""customAccentNavItems"" NameSelectors=""sectionSelectors"" Mode=""BitNavMode.Manual"" DefaultSelectedItem=""customAccentNavItems[0]"" />
<BitNav FitWidth Accent=""BitColor.Warning"" Items=""customAccentNavItems"" NameSelectors=""sectionSelectors"" Mode=""BitNavMode.Manual"" DefaultSelectedItem=""customAccentNavItems[0]"" />
<BitNav FitWidth Accent=""BitColor.Error"" Items=""customAccentNavItems"" NameSelectors=""sectionSelectors"" Mode=""BitNavMode.Manual"" DefaultSelectedItem=""customAccentNavItems[0]"" />";
    private readonly string example14CsharpCode = @"
private static readonly List<Section> customColorNavItems =
[
    new() { Text = ""Home"", ImageName = BitIconName.Home },
    new() { Text = ""Products"", ImageName = BitIconName.Product },
    new() { Text = ""Settings"", ImageName = BitIconName.Settings },
];

private static readonly List<Section> customAccentNavItems =
[
    new() { Text = ""Home"", ImageName = BitIconName.Home },
    new() { Text = ""Products"", ImageName = BitIconName.Product },
    new() { Text = ""Settings"", ImageName = BitIconName.Settings },
];";

    private readonly string example15RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitNav Items=""customExternalIconNavItems"" FitWidth NameSelectors=""sectionIconSelectors"" />

<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitNav Items=""customBootstrapIconNavItems"" ChevronDownIcon=""bootstrapChevronIcon"" FitWidth NameSelectors=""sectionIconSelectors"" />";
    private readonly string example15CsharpCode = @"
private static readonly BitNavNameSelectors<Section> sectionIconSelectors = new()
{
    Icon = { Name = nameof(Section.ImageName) },
    ChildItems = { Name = nameof(Section.Links) },
    Description = { Name = nameof(Section.Comment) },
    IsSeparator = { Name = nameof(Section.IsDivider) },
};

private static readonly List<Section> customExternalIconNavItems =
[
    new()
    {
        Text = ""bit platform"",
        Comment = ""Nav with external icons (FontAwesome)"",
        Links =
        [
            new() { Text = ""Home"", ImageName = BitIconInfo.Css(""fa-solid fa-house""), Url = ""https://bitplatform.dev/"" },
            new()
            {
                Text = ""Products & Services"",
                Links =
                [
                    new() { Text = ""BlazorUI"", ImageName = BitIconInfo.Fa(""solid code""), Url = ""https://bitplatform.dev/components"" },
                    new() { Text = ""Pricing"", ImageName = BitIconInfo.Css(""fa-solid fa-tag""), Url = ""https://bitplatform.dev/pricing"" },
                ]
            },
            new() { Text = ""About"", ImageName = BitIconInfo.Fa(""solid circle-info""), Url = ""https://bitplatform.dev/about-us"" },
            new() { Text = ""Contact us"", ImageName = BitIconInfo.Css(""fa-solid fa-envelope""), Url = ""https://bitplatform.dev/contact-us"" },
        ],
    },
    new() { Text = ""Iconography"", ImageName = BitIconInfo.Css(""fa-solid fa-icons""), Url = ""/iconography"" },
];

private static readonly BitIconInfo bootstrapChevronIcon = BitIconInfo.Bi(""chevron-right"");

private static readonly List<Section> customBootstrapIconNavItems =
[
    new()
    {
        Text = ""bit platform"",
        Comment = ""Nav with external icons (Bootstrap Icons)"",
        Links =
        [
            new() { Text = ""Home"", ImageName = BitIconInfo.Bi(""house-fill""), Url = ""https://bitplatform.dev/"" },
            new() { Text = ""BlazorUI"", ImageName = BitIconInfo.Bi(""code-slash""), Url = ""https://bitplatform.dev/components"" },
            new() { Text = ""Pricing"", ImageName = BitIconInfo.Bi(""tag-fill""), Url = ""https://bitplatform.dev/pricing"" },
        ],
    },
    new() { Text = ""Iconography"", ImageName = BitIconInfo.Bi(""emoji-smile""), Url = ""/iconography"" },
];";

    private readonly string example16RazorCode = @"
<BitNav FitWidth Size=""BitSize.Small"" Items=""customSizeNavItems"" NameSelectors=""sectionSelectors"" />

<BitNav FitWidth Size=""BitSize.Medium"" Items=""customSizeNavItems"" NameSelectors=""sectionSelectors"" />

<BitNav FitWidth Size=""BitSize.Large"" Items=""customSizeNavItems"" NameSelectors=""sectionSelectors"" />";
    private readonly string example16CsharpCode = @"
private static readonly List<Section> customSizeNavItems =
[
    new() { Text = ""Home"", ImageName = BitIconName.Home, Comment = ""The main page"" },
    new() { Text = ""Products"", ImageName = BitIconName.Product, Comment = ""All of the products"" },
    new() { Text = ""Settings"", ImageName = BitIconName.Settings, Comment = ""The app settings"" },
];";

    private readonly string example17RazorCode = @"
<BitNav Items=""customColorNavItems"" NameSelectors=""sectionSelectors"" Style=""max-width: max-content; border: 1px solid tomato;"" />

<BitNav Items=""customColorNavItems"" NameSelectors=""sectionSelectors"" Class=""custom-class"" />

<BitNav Items=""customStyleClassNavItems"" FitWidth NameSelectors=""sectionSelectors"" />

<BitNav Items=""customCustomStyleNavItems""
        AllExpanded
        Mode=""BitNavMode.Manual""
        NameSelectors=""sectionSelectors""
        DefaultSelectedItem=""customCustomStyleNavItems[1]""
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

<BitNav Items=""customClassesNavItems""
        AllExpanded
        NameSelectors=""sectionSelectors""
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
private static readonly List<Section> customStyleClassNavItems =
[
    new() { Text = ""Home"", ImageName = BitIconName.Home, Style = ""background: rgba(255,99,71,0.2);"" },
    new() { Text = ""Products"", ImageName = BitIconName.Product, Class = ""custom-item-list"" },
    new() { Text = ""Settings"", ImageName = BitIconName.Settings },
];

private static readonly List<Section> customCustomStyleNavItems =
[
    new()
    {
        Text = ""bit platform"",
        Comment = ""the bit platform description"",
        Links =
        [
            new() { Text = ""Home"", ImageName = BitIconName.Home, Url = ""https://bitplatform.dev/"" },
            new() { Text = ""BlazorUI"", ImageName = BitIconName.F12DevTools, Url = ""https://bitplatform.dev/components"" },
        ],
    },
    new() { Text = ""Iconography"", ImageName = BitIconName.AppIconDefault, Url = ""/iconography"" },
    new() { IsDivider = true },
    new() { Text = ""Contact us"", ImageName = BitIconName.Contact, Url = ""https://bitplatform.dev/contact-us"" },
];

private static readonly List<Section> customClassesNavItems =
[
    new()
    {
        Text = ""bit platform"",
        ImageName = BitIconName.Website,
        Comment = ""the bit platform description"",
        Links =
        [
            new() { Text = ""Home"", ImageName = BitIconName.Home, Url = ""https://bitplatform.dev/"" },
            new() { Text = ""BlazorUI"", ImageName = BitIconName.F12DevTools, Url = ""https://bitplatform.dev/components"" },
        ],
    },
    new()
    {
        Text = ""Community"",
        ImageName = BitIconName.Group,
        Links = [new() { Text = ""GitHub repo"", ImageName = BitIconName.GitGraph, Url = ""https://github.com/bitfoundation/bitplatform"" }]
    },
];";

    private readonly string example18RazorCode = @"
<div dir=""rtl"">
    <BitNav Dir=""BitDir.Rtl"" Items=""customRtlNavItems"" NameSelectors=""sectionSelectors"" />
</div>";
    private readonly string example18CsharpCode = @"
private static readonly List<Section> customRtlNavItems =
[
    new()
    {
        Text = ""پلتفرمِ بیت"",
        Comment = ""توضیحاتِ پلتفرمِ بیت"",
        Links =
        [
            new() { Text = ""خانه"", ImageName = BitIconName.Home, Url = ""https://bitplatform.dev/"" },
            new()
            {
                Text = ""محصولات و خدمات"",
                Links =
                [
                    new()
                    {
                        Text = ""قالب های پروژه"",
                        Links =
                        [
                            new() { Text = ""نمونه ی Todo"", ImageName = BitIconName.ToDoLogoOutline, Url = ""https://bitplatform.dev/templates/overview"" },
                            new() { Text = ""نمونه ی AdminPanel"", ImageName = BitIconName.LocalAdmin, Url = ""https://bitplatform.dev/templates/overview"" },
                        ]
                    },
                    new() { Text = ""رابط کاربری Blazor"", ImageName = BitIconName.F12DevTools, Url = ""https://blazorui.bitplatform.dev/"" },
                    new() { Text = ""راه های هاست ابری"", ImageName = BitIconName.Cloud, Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                    new() { Text = ""آکادمی بیت"", ImageName = BitIconName.LearningTools, Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                ]
            },
            new() { Text = ""قیمت"", ImageName = BitIconName.Money, Url = ""https://bitplatform.dev/pricing"" },
            new() { Text = ""درباره ما"", ImageName = BitIconName.Info, Url = ""https://bitplatform.dev/about-us"" },
            new() { Text = ""ارتباط با ما"", ImageName = BitIconName.Contact, Url = ""https://bitplatform.dev/contact-us"" },
        ],
    },
    new()
    {
        Text = ""انجمن ها"",
        Links =
        [
            new() { Text = ""لینکدین"", ImageName = BitIconName.LinkedInLogo, Url = ""https://www.linkedin.com/company/bitplatformhq"" },
            new() { Text = ""توییتر"", ImageName = BitIconName.Globe, Url = ""https://twitter.com/bitplatformhq"" },
            new() { Text = ""گیتهاب"", ImageName = BitIconName.GitGraph, Url = ""https://github.com/bitfoundation/bitplatform"" },
        ]
    },
    new() { Text = ""شمایل نگاری"", ImageName = BitIconName.AppIconDefault, Url = ""/iconography"" },
];";
}

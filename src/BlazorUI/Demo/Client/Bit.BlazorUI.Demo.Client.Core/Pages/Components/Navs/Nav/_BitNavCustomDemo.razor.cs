namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Nav;

public partial class _BitNavCustomDemo
{
    private static readonly List<Section> CustomBitPlatformNavMenu =
    [
        new()
        {
            Text = "bit platform",
            Comment = "the bit platform description",
            Links =
            [
                new() { Text = "Home", Icon = BitIconName.Home, Url = "https://bitplatform.dev/" },
                new()
                {
                    Text = "Products & Services",
                    Links =
                    [
                        new()
                        {
                            Text = "Project Templates",
                            Links =
                            [
                                new() { Text = "Todo sample", Icon = BitIconName.ToDoLogoOutline, Url = "https://bitplatform.dev/templates/overview" },
                                new() { Text = "AdminPanel sample", Icon = BitIconName.LocalAdmin, Url = "https://bitplatform.dev/templates/overview" },
                            ]
                        },
                        new() { Text = "BlazorUI", Icon = BitIconName.F12DevTools, Url = "https://bitplatform.dev/components" },
                        new() { Text = "Cloud hosting solutions", Icon = BitIconName.Cloud, Url = "https://bitplatform.dev/#", IsEnabled = false },
                        new() { Text = "Bit academy", Icon = BitIconName.LearningTools, Url = "https://bitplatform.dev/#", IsEnabled = false },
                    ]
                },
                new() { Text = "Pricing", Icon = BitIconName.Money, Url = "https://bitplatform.dev/pricing" },
                new() { Text = "About", Icon = BitIconName.Info, Url = "https://bitplatform.dev/about-us" },
                new() { Text = "Contact us", Icon = BitIconName.Contact, Url = "https://bitplatform.dev/contact-us" },
            ],
        },
        new()
        {
            Text = "Community",
            Links =
            [
                new() { Text = "Linkedin", Icon = BitIconName.LinkedInLogo, Url = "https://www.linkedin.com/company/bitplatformhq" },
                new() { Text = "Twitter", Icon = BitIconName.Globe, Url = "https://twitter.com/bitplatformhq" },
                new() { Text = "Github repo", Icon = BitIconName.GitGraph, Url = "https://github.com/bitfoundation/bitplatform" },
            ]
        },
        new() { Text = "Iconography", Icon = BitIconName.AppIconDefault, Url = "/iconography" },
    ];

    private static readonly List<Section> CustomIconOnlyNavMenu =
    [
        new() { Text = "Home", Icon = BitIconName.Home },
        new() {
            Text = "AdminPanel sample",
            Icon = BitIconName.LocalAdmin,
            Links =
            [
                new() { Text = "Dashboard", Icon = BitIconName.ViewDashboard },
                new() { Text = "Categories", Icon = BitIconName.BuildQueue },
                new() { Text = "Products", Icon = BitIconName.Product },
            ]
        },
        new() { Text = "Todo sample", Icon = BitIconName.ToDoLogoOutline},
        new() { Text = "BlazorUI", Icon = BitIconName.F12DevTools },
        new() { Text = "Bit academy", Icon = BitIconName.LearningTools, IsEnabled = false },
        new() { Text = "Contact us", Icon = BitIconName.Contact },
    ];

    private static readonly List<CarMenu> CustomCarNavMenu =
    [
        new()
        {
            Name = "Mercedes-Benz",
            ExpandedAriaLabel = "Mercedes-Benz Expanded",
            CollapsedAriaLabel = "Mercedes-Benz Collapsed",
            Tooltip = "Mercedes-Benz Car Models",
            IsExpandedParent = true,
            Comment = "Cars manufactured under the brand of Mercedes-Benz",
            Links =
            [
                new()
                {
                    Name = "SUVs",
                    Links =
                    [
                        new() { Name = "GLA", PageUrl = "https://www.mbusa.com/en/vehicles/class/gla/suv", UrlTarget = "_blank" },
                        new() { Name = "GLB", PageUrl = "https://www.mbusa.com/en/vehicles/class/glb/suv", UrlTarget = "_blank" },
                        new() { Name = "GLC", PageUrl = "https://www.mbusa.com/en/vehicles/class/glc/suv", UrlTarget = "_blank" },
                    ]
                },
                new()
                {
                    Name = "Sedans & Wagons",
                    Links =
                    [
                        new() { Name = "A Class", PageUrl = "https://www.mbusa.com/en/vehicles/class/a-class/sedan", UrlTarget = "_blank" },
                        new() { Name = "C Class", PageUrl = "https://www.mbusa.com/en/vehicles/class/c-class/sedan", UrlTarget = "_blank" },
                        new() { Name = "E Class", PageUrl = "https://www.mbusa.com/en/vehicles/class/e-class/sedan", UrlTarget = "_blank" },
                    ]
                },
                new()
                {
                    Name = "Coupes",
                    Links =
                    [
                        new() { Name = "CLA Coupe", PageUrl = "https://www.mbusa.com/en/vehicles/class/cla/coupe", UrlTarget = "_blank" },
                        new() { Name = "C Class Coupe", PageUrl = "https://www.mbusa.com/en/vehicles/class/c-class/coupe", UrlTarget = "_blank" },
                        new() { Name = "E Class Coupe", PageUrl = "https://www.mbusa.com/en/vehicles/class/e-class/coupe", UrlTarget = "_blank" },
                    ]
                },
            ]
        },
        new()
        {
            Name = "Tesla",
            ExpandedAriaLabel = "Tesla Expanded",
            CollapsedAriaLabel = "Tesla Collapsed",
            Tooltip = "Tesla Car Models",
            Links =
            [
                new() { Name = "Model S", PageUrl = "https://www.tesla.com/models", UrlTarget = "_blank" },
                new() { Name = "Model X", PageUrl = "https://www.tesla.com/modelx", UrlTarget = "_blank" },
                new() { Name = "Model Y", PageUrl = "https://www.tesla.com/modely", UrlTarget = "_blank" },
            ]
        },
    ];

    private static readonly List<FoodMenu> CustomFoodNavMenu =
    [
        new()
        {
            Name = "Fast foods",
            Icon = BitIconName.HeartBroken,
            IsExpanded = true,
            Comment = "List of fast foods",
            Childs =
            [
                new()
                {
                    Name = "Burgers",
                    Comment = "List of burgers",
                    Childs =
                    [
                        new() { Name = "Beef Burger" },
                        new() { Name = "Veggie Burger" },
                        new() { Name = "Bison Burger" },
                        new() { Name = "Wild Salmon Burger" },
                    ]
                },
                new()
                {
                    Name = "Pizza",
                    Childs =
                    [
                        new() { Name = "Cheese Pizza" },
                        new() { Name = "Veggie Pizza" },
                        new() { Name = "Pepperoni Pizza" },
                        new() { Name = "Meat Pizza" },
                    ]
                },
                new() { Name = "French Fries" },
            ]
        },
        new()
        {
            Name = "Fruits",
            Icon = BitIconName.Health,
            Childs =
            [
                new() { Name = "Apple" },
                new() { Name = "Orange" },
                new() { Name = "Banana" },
            ]
        },
        new() { Name = "Ice Cream" },
        new() { Name = "Cookie" },
    ];

    private static readonly List<Section> CustomCustomStyleNavMenu =
    [
        new()
        {
            Text = "bit platform",
            Comment = "the bit platform description",
            Links =
            [
                new() { Text = "Home", Icon = BitIconName.Home, Url = "https://bitplatform.dev/" },
                new()
                {
                    Text = "Products & Services",
                    Links =
                    [
                        new()
                        {
                            Text = "Project Templates",
                            Links =
                            [
                                new() { Text = "Todo sample", Icon = BitIconName.ToDoLogoOutline, Url = "https://bitplatform.dev/templates/overview" },
                                new() { Text = "AdminPanel sample", Icon = BitIconName.LocalAdmin, Url = "https://bitplatform.dev/templates/overview" },
                            ]
                        },
                        new() { Text = "BlazorUI", Icon = BitIconName.F12DevTools, Url = "https://bitplatform.dev/components" },
                        new() { Text = "Cloud hosting solutions", Icon = BitIconName.Cloud, Url = "https://bitplatform.dev/#", IsEnabled = false },
                        new() { Text = "Bit academy", Icon = BitIconName.LearningTools, Url = "https://bitplatform.dev/#", IsEnabled = false },
                    ]
                },
                new() { Text = "Pricing", Icon = BitIconName.Money, Url = "https://bitplatform.dev/pricing" },
                new() { Text = "About", Icon = BitIconName.Info, Url = "https://bitplatform.dev/about-us" },
                new() { Text = "Contact us", Icon = BitIconName.Contact, Url = "https://bitplatform.dev/contact-us" },
            ],
        },
        new()
        {
            Text = "Community",
            Links =
            [
                new() { Text = "Linkedin", Icon = BitIconName.LinkedInLogo, Url = "https://www.linkedin.com/company/bitplatformhq" },
                new() { Text = "Twitter", Icon = BitIconName.Globe, Url = "https://twitter.com/bitplatformhq" },
                new() { Text = "Github repo", Icon = BitIconName.GitGraph, Url = "https://github.com/bitfoundation/bitplatform" },
            ]
        },
        new() { Text = "Iconography", Icon = BitIconName.AppIconDefault, Url = "/iconography" },
    ];

    private static readonly List<Section> CustomRtlBitPlatformNavMenu =
    [
        new()
        {
            Text = "پلتفرمِ بیت",
            Comment = "توضیحاتِ پلتفرمِ بیت",
            Links =
            [
                new() { Text = "خانه", Icon = BitIconName.Home, Url = "https://bitplatform.dev/" },
                new()
                {
                    Text = "محصولات و خدمات",
                    Links =
                    [
                        new()
                        {
                            Text = "قالب های پروژه",
                            Links =
                            [
                                new() { Text = "نمونه ی Todo", Icon = BitIconName.ToDoLogoOutline, Url = "https://bitplatform.dev/templates/overview" },
                                new() { Text = "نمونه ی AdminPanel", Icon = BitIconName.LocalAdmin, Url = "https://bitplatform.dev/templates/overview" },
                            ]
                        },
                        new() { Text = "رابط کاربری Blazor", Icon = BitIconName.F12DevTools, Url = "https://blazorui.bitplatform.dev/" },
                        new() { Text = "راه های هاست ابری", Icon = BitIconName.Cloud, Url = "https://bitplatform.dev/#", IsEnabled = false },
                        new() { Text = "آکادمی بیت", Icon = BitIconName.LearningTools, Url = "https://bitplatform.dev/#", IsEnabled = false },
                    ]
                },
                new() { Text = "قیمت", Icon = BitIconName.Money, Url = "https://bitplatform.dev/pricing" },
                new() { Text = "درباره ما", Icon = BitIconName.Info, Url = "https://bitplatform.dev/about-us" },
                new() { Text = "ارتباط با ما", Icon = BitIconName.Contact, Url = "https://bitplatform.dev/contact-us" },
            ],
        },
        new()
        {
            Text = "انجمن ها",
            Links =
            [
                new() { Text = "لینکدین", Icon = BitIconName.LinkedInLogo, Url = "https://www.linkedin.com/company/bitplatformhq" },
                new() { Text = "توییتر", Icon = BitIconName.Globe, Url = "https://twitter.com/bitplatformhq" },
                new() { Text = "گیتهاب", Icon = BitIconName.GitGraph, Url = "https://github.com/bitfoundation/bitplatform" },
            ]
        },
        new() { Text = "شمایل نگاری", Icon = BitIconName.AppIconDefault, Url = "/iconography" },
    ];

    private bool iconOnly;

    private static List<FoodMenu> Flatten(IList<FoodMenu> e) => e.SelectMany(c => Flatten(c.Childs)).Concat(e).ToList();
    private FoodMenu CustomSelectedFood = CustomFoodNavMenu[0].Childs[2];
    private string? CustomSelectedFoodName = CustomFoodNavMenu[0].Childs[2].Name;

    private FoodMenu CustomClickedItem = default!;
    private FoodMenu CustomSelectedItem = default!;
    private FoodMenu CustomToggledItem = default!;

    private static readonly List<BitDropdownItem<string>> FoodMenuDropdownItems =
    [
        new() { Text = "Beef Burger", Value = "Beef Burger" },
        new() { Text = "Veggie Burger", Value = "Veggie Burger" },
        new() { Text = "Bison Burger", Value = "Bison Burger" },
        new() { Text = "Wild Salmon Burger", Value = "Wild Salmon Burger" },
        new() { Text = "Cheese Pizza", Value = "Cheese Pizza" },
        new() { Text = "Veggie Pizza", Value = "Veggie Pizza" },
        new() { Text = "Pepperoni Pizza", Value = "Pepperoni Pizza" },
        new() { Text = "Meat Pizza", Value = "Meat Pizza" },
        new() { Text = "French Fries", Value = "French Fries" },
        new() { Text = "Apple", Value = "Apple" },
        new() { Text = "Orange", Value = "Orange" },
        new() { Text = "Banana", Value = "Banana" },
        new() { Text = "Ice Cream", Value = "Ice Cream" },
        new() { Text = "Cookie", Value = "Cookie" },
    ];



    private readonly string example1RazorCode = @"
<BitNav Items=""CustomBitPlatformNavMenu""
        NameSelectors=""@(new() { IconName =  { Name = nameof(Section.Icon) },
                                 ChildItems =  { Name = nameof(Section.Links) },
                                 Description =  { Name = nameof(Section.Comment) } })"" />";
    private readonly string example1CsharpCode = @"
public class Section
{
    public string Text { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Url { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsExpanded { get; set; }
    public List<Section> Links { get; set; } = [];
}

private static readonly List<Section> CustomBitPlatformNavMenu =
[
    new()
    {
        Text = ""bit platform"",
        Comment = ""the bit platform description"",
        Links =
        [
            new() { Text = ""Home"", Icon = BitIconName.Home, Url = ""https://bitplatform.dev/"" },
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
                            new() { Text = ""Todo sample"", Icon = BitIconName.ToDoLogoOutline, Url = ""https://bitplatform.dev/templates/overview"" },
                            new() { Text = ""AdminPanel sample"", Icon = BitIconName.LocalAdmin, Url = ""https://bitplatform.dev/templates/overview"" },
                        ]
                    },
                    new() { Text = ""BlazorUI"", Icon = BitIconName.F12DevTools, Url = ""https://bitplatform.dev/components"" },
                    new() { Text = ""Cloud hosting solutions"", Icon = BitIconName.Cloud, Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                    new() { Text = ""Bit academy"", Icon = BitIconName.LearningTools, Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                ]
            },
            new() { Text = ""Pricing"", Icon = BitIconName.Money, Url = ""https://bitplatform.dev/pricing"" },
            new() { Text = ""About"", Icon = BitIconName.Info, Url = ""https://bitplatform.dev/about-us"" },
            new() { Text = ""Contact us"", Icon = BitIconName.Contact, Url = ""https://bitplatform.dev/contact-us"" },
        ],
    },
    new()
    {
        Text = ""Community"",
        Links =
        [
            new() { Text = ""Linkedin"", Icon = BitIconName.LinkedInLogo, Url = ""https://www.linkedin.com/company/bitplatformhq"" },
            new() { Text = ""Twitter"", Icon = BitIconName.Globe, Url = ""https://twitter.com/bitplatformhq"" },
            new() { Text = ""Github repo"", Icon = BitIconName.GitGraph, Url = ""https://github.com/bitfoundation/bitplatform"" },
        ]
    },
    new() { Text = ""Iconography"", Icon = BitIconName.AppIconDefault, Url = ""/iconography"" },
];";

    private readonly string example2RazorCode = @"
<BitNav Items=""CustomBitPlatformNavMenu"" FitWidth
        NameSelectors=""@(new() { IconName =  { Name = nameof(Section.Icon) },
                                 ChildItems =  { Name = nameof(Section.Links) },
                                 Description =  { Name = nameof(Section.Comment) } })"" />";
    private readonly string example2CsharpCode = @"
public class Section
{
    public string Text { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Url { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsExpanded { get; set; }
    public List<Section> Links { get; set; } = [];
}

private static readonly List<Section> CustomBitPlatformNavMenu =
[
    new()
    {
        Text = ""bit platform"",
        Comment = ""the bit platform description"",
        Links =
        [
            new() { Text = ""Home"", Icon = BitIconName.Home, Url = ""https://bitplatform.dev/"" },
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
                            new() { Text = ""Todo sample"", Icon = BitIconName.ToDoLogoOutline, Url = ""https://bitplatform.dev/templates/overview"" },
                            new() { Text = ""AdminPanel sample"", Icon = BitIconName.LocalAdmin, Url = ""https://bitplatform.dev/templates/overview"" },
                        ]
                    },
                    new() { Text = ""BlazorUI"", Icon = BitIconName.F12DevTools, Url = ""https://bitplatform.dev/components"" },
                    new() { Text = ""Cloud hosting solutions"", Icon = BitIconName.Cloud, Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                    new() { Text = ""Bit academy"", Icon = BitIconName.LearningTools, Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                ]
            },
            new() { Text = ""Pricing"", Icon = BitIconName.Money, Url = ""https://bitplatform.dev/pricing"" },
            new() { Text = ""About"", Icon = BitIconName.Info, Url = ""https://bitplatform.dev/about-us"" },
            new() { Text = ""Contact us"", Icon = BitIconName.Contact, Url = ""https://bitplatform.dev/contact-us"" },
        ],
    },
    new()
    {
        Text = ""Community"",
        Links =
        [
            new() { Text = ""Linkedin"", Icon = BitIconName.LinkedInLogo, Url = ""https://www.linkedin.com/company/bitplatformhq"" },
            new() { Text = ""Twitter"", Icon = BitIconName.Globe, Url = ""https://twitter.com/bitplatformhq"" },
            new() { Text = ""Github repo"", Icon = BitIconName.GitGraph, Url = ""https://github.com/bitfoundation/bitplatform"" },
        ]
    },
    new() { Text = ""Iconography"", Icon = BitIconName.AppIconDefault, Url = ""/iconography"" },
];";

    private readonly string example3RazorCode = @"
<BitNav Items=""CustomCarNavMenu""
        RenderType=""BitNavRenderType.Grouped""
        NameSelectors=""@(new() { Text =  { Name = nameof(CarMenu.Name) },
                                 Url =  { Name = nameof(CarMenu.PageUrl) },
                                 Target =  { Name = nameof(CarMenu.UrlTarget) },
                                 Title =  { Name = nameof(CarMenu.Tooltip) },
                                 IsExpanded =  { Name = nameof(CarMenu.IsExpandedParent) },
                                 CollapseAriaLabel =  { Name = nameof(CarMenu.CollapsedAriaLabel) },
                                 ExpandAriaLabel =  { Name = nameof(CarMenu.ExpandedAriaLabel) },
                                 ChildItems =  { Name = nameof(CarMenu.Links) },
                                 Description =  { Name = nameof(CarMenu.Comment) } })"" />";
    private readonly string example3CsharpCode = @"
public class CarMenu
{
    public string Name { get; set; } = string.Empty;
    public string Tooltip { get; set; }
    public string PageUrl { get; set; }
    public string UrlTarget { get; set; }
    public string ExpandedAriaLabel { get; set; }
    public string CollapsedAriaLabel { get; set; }
    public bool IsExpandedParent { get; set; }
    public List<CarMenu> Links { get; set; } = [];
}

private static readonly List<CarMenu> CustomCarNavMenu =
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
<BitNav Items=""CustomFoodNavMenu""
        Mode=""BitNavMode.Manual""
        DefaultSelectedItem=""CustomFoodNavMenu[0].Childs[2]""
        NameSelectors=""@(new() { Text =  { Selector = item => item.Name },
                                 IconName =  { Selector = item => item.Icon },
                                 ChildItems =  { Selector = item => item.Childs },
                                 Description =  { Selector = item => item.Comment } })"" />


<BitNav @bind-SelectedItem=""CustomSelectedFood""
        Items=""CustomFoodNavMenu""
        Mode=""BitNavMode.Manual""
        DefaultSelectedItem=""CustomFoodNavMenu[0].Childs[2]""
        NameSelectors=""@(new() { Text =  { Selector = item => item.Name },
                                 IconName =  { Selector = item => item.Icon },
                                 ChildItems =  { Selector = item => item.Childs },
                                 Description =  { Selector = item => item.Comment } })""
        OnSelectItem=""(FoodMenu item) => CustomSelectedFoodName = FoodMenuDropdownItems.Single(i => i.Text == item.Name).Text"" />

<BitDropdown @bind-Value=""CustomSelectedFoodName""
             Label=""Select Item""
             Items=""FoodMenuDropdownItems""
             OnSelectItem=""(BitDropdownItem<string> item) => CustomSelectedFood = Flatten(CustomFoodNavMenu).Single(i => i.Name == item.Value)"" />";
    private readonly string example4CsharpCode = @"
public class FoodMenu
{
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; }
    public bool IsExpanded { get; set; }
    public List<FoodMenu> Childs { get; set; } = [];
}

private static readonly List<FoodMenu> CustomFoodNavMenu =
[
    new()
    {
        Name = ""Fast foods"",
        Icon = BitIconName.HeartBroken,
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
        Icon = BitIconName.Health,
        Childs =
        [
            new() { Name = ""Apple"" },
            new() { Name = ""Orange"" },
            new() { Name = ""Banana"" },
        ]
    },
    new() { Name = ""Ice Cream"" },
    new() { Name = ""Cookie"" },
];

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
];

private static List<FoodMenu> Flatten(IList<FoodMenu> e) => e.SelectMany(c => Flatten(c.Childs)).Concat(e).ToList();
private FoodMenu CustomSelectedFood = CustomFoodNavMenu[0].Childs[2];
private string CustomSelectedFoodName = CustomFoodNavMenu[0].Childs[2].Name;";

    private readonly string example5RazorCode = @"
<BitToggle @bind-Value=""iconOnly"" Label=""Hide texts?"" Inline />
<BitNav Items=""CustomIconOnlyNavMenu"" Mode=""BitNavMode.Manual"" IconOnly=""iconOnly""
        NameSelectors=""@(new() { IconName =  { Name = nameof(Section.Icon) },
                                 ChildItems =  { Name = nameof(Section.Links) },
                                 Description =  { Name = nameof(Section.Comment) } })"" />";
    private readonly string example5CsharpCode = @"
private bool iconOnly;

public class Section
{
    public string Text { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Url { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsExpanded { get; set; }
    public List<Section> Links { get; set; } = [];
}

private static readonly List<Section> CustomIconOnlyNavMenu =
[
    new() { Text = ""Home"", Icon = BitIconName.Home },
    new() {
        Text = ""AdminPanel sample"",
        Icon = BitIconName.LocalAdmin,
        Links =
        [
            new() { Text = ""Dashboard"", Icon = BitIconName.ViewDashboard },
            new() { Text = ""Categories"", Icon = BitIconName.BuildQueue },
            new() { Text = ""Products"", Icon = BitIconName.Product },
        ]
    },
    new() { Text = ""Todo sample"", Icon = BitIconName.ToDoLogoOutline},
    new() { Text = ""BlazorUI"", Icon = BitIconName.F12DevTools },
    new() { Text = ""Bit academy"", Icon = BitIconName.LearningTools, IsEnabled = false },
    new() { Text = ""Contact us"", Icon = BitIconName.Contact },
];";

    private readonly string example6RazorCode = @"
<style>
    .nav-custom-header {
        font-size: 17px;
        font-weight: 600;
        color: green;
    }

    .nav-custom-item {
        display: flex;
        align-items: center;
        flex-flow: row nowrap;
        gap: 4px;
        color: #ff7800;
        font-weight: 600;
    }
</style>

<BitNav Items=""CustomCarNavMenu""
        RenderType=""BitNavRenderType.Grouped""
        NameSelectors=""@(new() { Text =  { Name = nameof(CarMenu.Name) },
                                 Url =  { Name = nameof(CarMenu.PageUrl) },
                                 Target =  { Name = nameof(CarMenu.UrlTarget) },
                                 Title =  { Name = nameof(CarMenu.Tooltip) },
                                 IsExpanded =  { Name = nameof(CarMenu.IsExpandedParent) },
                                 CollapseAriaLabel =  { Name = nameof(CarMenu.CollapsedAriaLabel) },
                                 ExpandAriaLabel =  { Name = nameof(CarMenu.ExpandedAriaLabel) },
                                 ChildItems =  { Name = nameof(CarMenu.Links) },
                                 Description =  { Name = nameof(CarMenu.Comment) } })"">
    <HeaderTemplate Context=""item"">
        <div class=""nav-custom-header"">
            <BitIcon IconName=""@BitIconName.FavoriteStarFill"" />
            <span>@item.Name</span>
        </div>
    </HeaderTemplate>
</BitNav>



<BitNav Items=""CustomFoodNavMenu""
        Mode=""BitNavMode.Manual""
        NameSelectors=""@(new() { Text =  { Selector = item => item.Name },
                                 IconName =  { Selector = item => item.Icon },
                                 ChildItems =  { Selector = item => item.Childs },
                                 Description =  { Selector = item => item.Comment } })"">
    <ItemTemplate Context=""item"">
        <div class=""nav-custom-item"">
            <BitCheckbox />
            <BitIcon IconName=""@item.Icon"" />
            <span>@item.Name</span>
        </div>
    </ItemTemplate>
</BitNav>";
    private readonly string example6CsharpCode = @"
public class CarMenu
{
    public string Name { get; set; } = string.Empty;
    public string Tooltip { get; set; }
    public string PageUrl { get; set; }
    public string UrlTarget { get; set; }
    public string ExpandedAriaLabel { get; set; }
    public string CollapsedAriaLabel { get; set; }
    public bool IsExpandedParent { get; set; }
    public List<CarMenu> Links { get; set; } = [];
}

public class FoodMenu
{
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; }
    public bool IsExpanded { get; set; }
    public List<FoodMenu> Childs { get; set; } = [];
}

private static readonly List<CarMenu> CustomCarNavMenu =
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
];

private static readonly List<FoodMenu> CustomFoodNavMenu =
[
    new()
    {
        Name = ""Fast foods"",
        Icon = BitIconName.HeartBroken,
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
        Icon = BitIconName.Health,
        Childs =
        [
            new() { Name = ""Apple"" },
            new() { Name = ""Orange"" },
            new() { Name = ""Banana"" },
        ]
    },
    new() { Name = ""Ice Cream"" },
    new() { Name = ""Cookie"" },
];";

    private readonly string example7RazorCode = @"
<BitNav Items=""CustomFoodNavMenu""
        Mode=""BitNavMode.Manual""
        OnItemClick=""(FoodMenu item) => CustomClickedItem = item""
        OnSelectItem=""(FoodMenu item) => CustomSelectedItem = item""
        OnItemToggle=""(FoodMenu item) => CustomToggledItem = item""
        NameSelectors=""@(new() { Text =  { Selector = item => item.Name },
                                 IconName =  { Selector = item => item.Icon },
                                 ChildItems =  { Selector = item => item.Childs },
                                 Description =  { Selector = item => item.Comment } })"" />
<div>Clicked Item: @CustomClickedItem?.Name</div>
<div>Selected Item: @CustomSelectedItem?.Name</div>
<div>Toggled Item: @(CustomToggledItem is null ? ""N/A"" : $""{CustomToggledItem.Name} ({(CustomToggledItem.IsExpanded ? ""Expanded"" : ""Collapsed"")})"")</div>";
    private readonly string example7CsharpCode = @"
public class FoodMenu
{
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; }
    public bool IsExpanded { get; set; }
    public List<FoodMenu> Childs { get; set; } = [];
}

private static readonly List<FoodMenu> CustomFoodNavMenu =
[
    new()
    {
        Name = ""Fast foods"",
        Icon = BitIconName.HeartBroken,
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
        Icon = BitIconName.Health,
        Childs =
        [
            new() { Name = ""Apple"" },
            new() { Name = ""Orange"" },
            new() { Name = ""Banana"" },
        ]
    },
    new() { Name = ""Ice Cream"" },
    new() { Name = ""Cookie"" },
];

private FoodMenu CustomClickedItem;
private FoodMenu CustomSelectedItem;
private FoodMenu CustomToggledItem;";

    private readonly string example8RazorCode = @"
<BitNav Items=""CustomCustomStyleNavMenu""
        NameSelectors=""@(new() { IconName =  { Name = nameof(Section.Icon) },
                                 ChildItems =  { Name = nameof(Section.Links) },
                                 Description =  { Name = nameof(Section.Comment) } })""
        Styles=""@(new() { ItemContainer = ""border: 1px solid green; margin: 2px;"",
                          ToggleButton = ""color: cyan;"",
                          Item = ""color: red;"",
                          ItemIcon = ""color: gold; margin-right: 15px;"" })"" />";
    private readonly string example8CsharpCode = @"
public class Section
{
    public string Text { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Url { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsExpanded { get; set; }
    public List<Section> Links { get; set; } = [];
}

private static readonly List<Section> CustomCustomStyleNavMenu =
[
    new()
    {
        Text = ""bit platform"",
        Comment = ""the bit platform description"",
        Links =
        [
            new() { Text = ""Home"", Icon = BitIconName.Home, Url = ""https://bitplatform.dev/"" },
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
                            new() { Text = ""Todo sample"", Icon = BitIconName.ToDoLogoOutline, Url = ""https://bitplatform.dev/templates/overview"" },
                            new() { Text = ""AdminPanel sample"", Icon = BitIconName.LocalAdmin, Url = ""https://bitplatform.dev/templates/overview"" },
                        ]
                    },
                    new() { Text = ""BlazorUI"", Icon = BitIconName.F12DevTools, Url = ""https://bitplatform.dev/components"" },
                    new() { Text = ""Cloud hosting solutions"", Icon = BitIconName.Cloud, Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                    new() { Text = ""Bit academy"", Icon = BitIconName.LearningTools, Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                ]
            },
            new() { Text = ""Pricing"", Icon = BitIconName.Money, Url = ""https://bitplatform.dev/pricing"" },
            new() { Text = ""About"", Icon = BitIconName.Info, Url = ""https://bitplatform.dev/about-us"" },
            new() { Text = ""Contact us"", Icon = BitIconName.Contact, Url = ""https://bitplatform.dev/contact-us"" },
        ],
    },
    new()
    {
        Text = ""Community"",
        Links =
        [
            new() { Text = ""Linkedin"", Icon = BitIconName.LinkedInLogo, Url = ""https://www.linkedin.com/company/bitplatformhq"" },
            new() { Text = ""Twitter"", Icon = BitIconName.Globe, Url = ""https://twitter.com/bitplatformhq"" },
            new() { Text = ""Github repo"", Icon = BitIconName.GitGraph, Url = ""https://github.com/bitfoundation/bitplatform"" },
        ]
    },
    new() { Text = ""Iconography"", Icon = BitIconName.AppIconDefault, Url = ""/iconography"" },
];";

    private readonly string example9RazorCode = @"
<BitNav Dir=""BitDir.Rtl""
        Items=""CustomRtlBitPlatformNavMenu""
        NameSelectors=""@(new() { IconName =  { Name = nameof(Section.Icon) },
                                 ChildItems =  { Name = nameof(Section.Links) },
                                 Description =  { Name = nameof(Section.Comment) } })"" />";
    private readonly string example9CsharpCode = @"
public class Section
{
    public string Text { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Url { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsExpanded { get; set; }
    public List<Section> Links { get; set; } = [];
}

private static readonly List<Section> CustomRtlBitPlatformNavMenu =
[
    new()
    {
        Text = ""پلتفرمِ بیت"",
        Comment = ""توضیحاتِ پلتفرمِ بیت"",
        Links =
        [
            new() { Text = ""خانه"", Icon = BitIconName.Home, Url = ""https://bitplatform.dev/"" },
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
                            new() { Text = ""نمونه ی Todo"", Icon = BitIconName.ToDoLogoOutline, Url = ""https://bitplatform.dev/templates/overview"" },
                            new() { Text = ""نمونه ی AdminPanel"", Icon = BitIconName.LocalAdmin, Url = ""https://bitplatform.dev/templates/overview"" },
                        ]
                    },
                    new() { Text = ""رابط کاربری Blazor"", Icon = BitIconName.F12DevTools, Url = ""https://blazorui.bitplatform.dev/"" },
                    new() { Text = ""راه های هاست ابری"", Icon = BitIconName.Cloud, Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                    new() { Text = ""آکادمی بیت"", Icon = BitIconName.LearningTools, Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                ]
            },
            new() { Text = ""قیمت"", Icon = BitIconName.Money, Url = ""https://bitplatform.dev/pricing"" },
            new() { Text = ""درباره ما"", Icon = BitIconName.Info, Url = ""https://bitplatform.dev/about-us"" },
            new() { Text = ""ارتباط با ما"", Icon = BitIconName.Contact, Url = ""https://bitplatform.dev/contact-us"" },
        ],
    },
    new()
    {
        Text = ""انجمن ها"",
        Links =
        [
            new() { Text = ""لینکدین"", Icon = BitIconName.LinkedInLogo, Url = ""https://www.linkedin.com/company/bitplatformhq"" },
            new() { Text = ""توییتر"", Icon = BitIconName.Globe, Url = ""https://twitter.com/bitplatformhq"" },
            new() { Text = ""گیتهاب"", Icon = BitIconName.GitGraph, Url = ""https://github.com/bitfoundation/bitplatform"" },
        ]
    },
    new() { Text = ""شمایل نگاری"", Icon = BitIconName.AppIconDefault, Url = ""/iconography"" },
];";
}

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Nav;

public partial class _BitNavCustomDemo
{
    // The Section class only renames a handful of the members the nav expects, so only those are mapped and
    // the rest (Text, Url, IsEnabled, IsExpanded, Style, Class, ...) keep matching BitNavItem by convention.
    private static readonly BitNavNameSelectors<Section> sectionSelectors = new()
    {
        IconName = { Name = nameof(Section.ImageName) },
        ChildItems = { Name = nameof(Section.Links) },
        Description = { Name = nameof(Section.Comment) },
        IsSeparator = { Name = nameof(Section.IsDivider) },
    };

    // The external icons are carried by a BitIconInfo of their own instead of by the icon name string: a
    // BitIconInfo assigned to a string property is reduced to its Name, which drops the base class and the
    // prefix an external library needs (the "bi bi-" of Bootstrap Icons, for instance).
    private static readonly BitNavNameSelectors<Section> sectionIconSelectors = new()
    {
        Icon = { Name = nameof(Section.Icon) },
        ChildItems = { Name = nameof(Section.Links) },
        Description = { Name = nameof(Section.Comment) },
        IsSeparator = { Name = nameof(Section.IsDivider) },
    };

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

    // The same mapping, written with Selector lambdas instead of property names: it skips the reflection and
    // is checked by the compiler, at the cost of one lambda per member.
    private static readonly BitNavNameSelectors<FoodMenu> foodSelectors = new()
    {
        Text = { Selector = item => item.Name },
        IconName = { Selector = item => item.Image },
        ChildItems = { Selector = item => item.Childs },
        Description = { Selector = item => item.Comment },
    };



    private static readonly List<Section> customBasicNavItems =
    [
        new()
        {
            Text = "bit platform",
            Comment = "the bit platform description",
            Links =
            [
                new() { Text = "Home", ImageName = BitIconName.Home, Url = "https://bitplatform.dev/" },
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
                                new() { Text = "Todo sample", ImageName = BitIconName.ToDoLogoOutline, Url = "https://bitplatform.dev/templates/overview" },
                                new() { Text = "AdminPanel sample", ImageName = BitIconName.LocalAdmin, Url = "https://bitplatform.dev/templates/overview" },
                            ]
                        },
                        new() { Text = "BlazorUI", ImageName = BitIconName.F12DevTools, Url = "https://bitplatform.dev/components" },
                        new() { Text = "Cloud hosting solutions", ImageName = BitIconName.Cloud, Url = "https://bitplatform.dev/#", IsEnabled = false },
                        new() { Text = "Bit academy", ImageName = BitIconName.LearningTools, Url = "https://bitplatform.dev/#", IsEnabled = false },
                    ]
                },
                new() { Text = "Pricing", ImageName = BitIconName.Money, Url = "https://bitplatform.dev/pricing" },
                new() { Text = "About", ImageName = BitIconName.Info, Url = "https://bitplatform.dev/about-us" },
                new() { Text = "Contact us", ImageName = BitIconName.Contact, Url = "https://bitplatform.dev/contact-us" },
            ],
        },
        new()
        {
            Text = "Community",
            Links =
            [
                new() { Text = "LinkedIn", ImageName = BitIconName.LinkedInLogo, Url = "https://www.linkedin.com/company/bitplatformhq" },
                new() { Text = "Twitter", ImageName = BitIconName.Globe, Url = "https://twitter.com/bitplatformhq" },
                new() { Text = "GitHub repo", ImageName = BitIconName.GitGraph, Url = "https://github.com/bitfoundation/bitplatform" },
            ]
        },
        new() { Text = "Iconography", ImageName = BitIconName.AppIconDefault, Url = "/iconography" },
    ];

    private static readonly List<Section> customSeparatorNavItems =
    [
        new() { Text = "Home", ImageName = BitIconName.Home, Url = "https://bitplatform.dev/" },
        new() { Text = "Pricing", ImageName = BitIconName.Money, Url = "https://bitplatform.dev/pricing" },
        new() { IsDivider = true },
        new() { Text = "LinkedIn", ImageName = BitIconName.LinkedInLogo, Url = "https://www.linkedin.com/company/bitplatformhq" },
        new() { Text = "GitHub repo", ImageName = BitIconName.GitGraph, Url = "https://github.com/bitfoundation/bitplatform" },
        new() { IsDivider = true },
        new() { Text = "Contact us", ImageName = BitIconName.Contact, Url = "https://bitplatform.dev/contact-us" },
    ];

    private static readonly List<Section> customIconOnlyNavItems =
    [
        new() { Text = "Home", ImageName = BitIconName.Home },
        new() {
            Text = "AdminPanel sample",
            ImageName = BitIconName.LocalAdmin,
            Links =
            [
                new() { Text = "Dashboard", ImageName = BitIconName.ViewDashboard },
                new() { Text = "Categories", ImageName = BitIconName.BuildQueue },
                new() { Text = "Products", ImageName = BitIconName.Product },
            ]
        },
        new() { Text = "Todo sample", ImageName = BitIconName.ToDoLogoOutline},
        new() { Text = "BlazorUI", ImageName = BitIconName.F12DevTools },
        new() { Text = "Bit academy", ImageName = BitIconName.LearningTools, IsEnabled = false },
        new() { Text = "Contact us", ImageName = BitIconName.Contact },
    ];

    private static readonly List<CarMenu> customCarNavItems =
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

    private static readonly List<FoodMenu> customFoodNavItems =
    [
        new()
        {
            Name = "Fast foods",
            Image = BitIconName.HeartBroken,
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
            Image = BitIconName.Health,
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

    private static readonly List<Section> customSingleExpandNavItems =
    [
        new()
        {
            Text = "Fast foods",
            ImageName = BitIconName.HeartBroken,
            Links =
            [
                new() { Text = "Burgers", Links = [new() { Text = "Beef Burger" }, new() { Text = "Veggie Burger" }] },
                new() { Text = "Pizza", Links = [new() { Text = "Cheese Pizza" }, new() { Text = "Meat Pizza" }] },
                new() { Text = "French Fries" },
            ]
        },
        new()
        {
            Text = "Fruits",
            ImageName = BitIconName.Health,
            Links = [new() { Text = "Apple" }, new() { Text = "Orange" }, new() { Text = "Banana" }]
        },
        new()
        {
            Text = "Drinks",
            ImageName = BitIconName.Coffee,
            Links = [new() { Text = "Water" }, new() { Text = "Tea" }]
        },
    ];

    private static readonly List<Section> customChevronNavItems =
    [
        new()
        {
            Text = "bit platform",
            ImageName = BitIconName.Website,
            Links =
            [
                new() { Text = "Home", ImageName = BitIconName.Home },
                new()
                {
                    Text = "Products & Services",
                    ImageName = BitIconName.Product,
                    Links =
                    [
                        new() { Text = "BlazorUI", ImageName = BitIconName.F12DevTools },
                        new() { Text = "Pricing", ImageName = BitIconName.Money },
                    ]
                },
            ]
        },
        new() { Text = "Iconography", ImageName = BitIconName.AppIconDefault },
    ];

    private static readonly List<Section> customApiNavItems =
    [
        new()
        {
            Text = "Fast foods",
            ImageName = BitIconName.HeartBroken,
            Links =
            [
                new() { Text = "Burgers", Links = [new() { Text = "Beef Burger" }, new() { Text = "Veggie Burger" }] },
                new() { Text = "Pizza", Links = [new() { Text = "Cheese Pizza" }, new() { Text = "Meat Pizza" }] },
            ]
        },
        new()
        {
            Text = "Fruits",
            ImageName = BitIconName.Health,
            Links = [new() { Text = "Apple" }, new() { Text = "Orange" }]
        },
        new() { Text = "Ice Cream", ImageName = BitIconName.Emoji2 },
        new() { Text = "Cookie", ImageName = BitIconName.Cake },
    ];

    // The matching members of Section carry names of their own, so they are mapped just like the rest.
    private static readonly BitNavNameSelectors<Section> matchSelectors = new()
    {
        IconName = { Name = nameof(Section.ImageName) },
        ChildItems = { Name = nameof(Section.Links) },
        Match = { Name = nameof(Section.UrlMatch) },
        AdditionalUrls = { Name = nameof(Section.OtherUrls) },
    };

    private static readonly List<Section> customMatchNavItems =
    [
        new() { Text = "Nav (this page)", ImageName = BitIconName.GlobalNavButton, Url = "/components/nav" },
        new() { Text = "Pivot", ImageName = BitIconName.MiniExpand, Url = "/components/pivot" },
    ];

    private static readonly List<Section> customPrefixMatchNavItems =
    [
        new() { Text = "Components (/components)", ImageName = BitIconName.F12DevTools, Url = "/components" },
        new() { Text = "Iconography (/iconography)", ImageName = BitIconName.AppIconDefault, Url = "/iconography" },
    ];

    // The URL of a Wildcard or a Regex item is a pattern rather than a page, so these items are disabled:
    // they still light up when the pattern matches the current URL, but a click cannot follow them to a
    // route that does not exist.
    private static readonly List<Section> customWildcardMatchNavItems =
    [
        new() { Text = "A component page (/components/*)", ImageName = BitIconName.F12DevTools, Url = "/components/*", IsEnabled = false },
        new() { Text = "A pro page (/pro/**)", ImageName = BitIconName.Trophy2, Url = "/pro/**", IsEnabled = false },
    ];

    private static readonly List<Section> customRegexMatchNavItems =
    [
        new() { Text = @"Nav or NavBar (^/components/nav(bar)?$)", ImageName = BitIconName.GlobalNavButton, Url = "^/components/nav(bar)?$", IsEnabled = false },
        new() { Text = @"A page starting with P (^/components/p)", ImageName = BitIconName.Page, Url = "^/components/p", IsEnabled = false },
    ];

    private static readonly List<Section> customItemMatchNavItems =
    [
        new() { Text = "Components (its own Prefix)", ImageName = BitIconName.F12DevTools, Url = "/components", UrlMatch = BitNavMatch.Prefix },
        new() { Text = "Pivot (the Exact of the nav)", ImageName = BitIconName.MiniExpand, Url = "/components/pivot" },
    ];

    private static readonly List<Section> customAdditionalUrlsNavItems =
    [
        new()
        {
            Text = "Navigation (also /components/nav)",
            ImageName = BitIconName.GlobalNavButton,
            Url = "/components/navbar",
            OtherUrls = ["/components/nav", "/components/breadcrumb"]
        },
        new() { Text = "Inputs", ImageName = BitIconName.TextField, Url = "/components/textfield" },
    ];

    private static readonly List<Section> customColorNavItems =
    [
        new() { Text = "Home", ImageName = BitIconName.Home },
        new() { Text = "Products", ImageName = BitIconName.Product },
        new() { Text = "Settings", ImageName = BitIconName.Settings },
    ];

    private static readonly List<Section> customAccentNavItems =
    [
        new() { Text = "Home", ImageName = BitIconName.Home },
        new() { Text = "Products", ImageName = BitIconName.Product },
        new() { Text = "Settings", ImageName = BitIconName.Settings },
    ];

    private static readonly List<Section> customSizeNavItems =
    [
        new() { Text = "Home", ImageName = BitIconName.Home, Comment = "The main page" },
        new() { Text = "Products", ImageName = BitIconName.Product, Comment = "All of the products" },
        new() { Text = "Settings", ImageName = BitIconName.Settings, Comment = "The app settings" },
    ];

    private static readonly List<Section> customStyleClassNavItems =
    [
        new() { Text = "Home", ImageName = BitIconName.Home, Style = "background: rgba(255,99,71,0.2);" },
        new() { Text = "Products", ImageName = BitIconName.Product, Class = "custom-item-list" },
        new() { Text = "Settings", ImageName = BitIconName.Settings },
    ];

    private static readonly List<Section> customNoCollapseNavItems =
    [
        new()
        {
            Text = "bit platform",
            Comment = "the bit platform description",
            Links =
            [
                new() { Text = "Home", ImageName = BitIconName.Home, Url = "https://bitplatform.dev/" },
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
                                new() { Text = "Todo sample", ImageName = BitIconName.ToDoLogoOutline, Url = "https://bitplatform.dev/templates/overview" },
                                new() { Text = "AdminPanel sample", ImageName = BitIconName.LocalAdmin, Url = "https://bitplatform.dev/templates/overview" },
                            ]
                        },
                        new() { Text = "BlazorUI", ImageName = BitIconName.F12DevTools, Url = "https://bitplatform.dev/components" },
                        new() { Text = "Cloud hosting solutions", ImageName = BitIconName.Cloud, Url = "https://bitplatform.dev/#", IsEnabled = false },
                        new() { Text = "Bit academy", ImageName = BitIconName.LearningTools, Url = "https://bitplatform.dev/#", IsEnabled = false },
                    ]
                },
                new() { Text = "Pricing", ImageName = BitIconName.Money, Url = "https://bitplatform.dev/pricing" },
                new() { Text = "About", ImageName = BitIconName.Info, Url = "https://bitplatform.dev/about-us" },
                new() { Text = "Contact us", ImageName = BitIconName.Contact, Url = "https://bitplatform.dev/contact-us" },
            ],
        },
        new()
        {
            Text = "Community",
            Links =
            [
                new() { Text = "LinkedIn", ImageName = BitIconName.LinkedInLogo, Url = "https://www.linkedin.com/company/bitplatformhq" },
                new() { Text = "Twitter", ImageName = BitIconName.Globe, Url = "https://twitter.com/bitplatformhq" },
                new() { Text = "GitHub repo", ImageName = BitIconName.GitGraph, Url = "https://github.com/bitfoundation/bitplatform" },
            ]
        },
        new() { Text = "Iconography", ImageName = BitIconName.AppIconDefault, Url = "/iconography" },
    ];

    private static readonly List<Section> customExternalIconNavItems =
    [
        new()
        {
            Text = "bit platform",
            Comment = "Nav with external icons (FontAwesome)",
            Links =
            [
                new() { Text = "Home", Icon = BitIconInfo.Css("fa-solid fa-house"), Url = "https://bitplatform.dev/" },
                new()
                {
                    Text = "Products & Services",
                    Links =
                    [
                        new() { Text = "BlazorUI", Icon = BitIconInfo.Fa("solid code"), Url = "https://bitplatform.dev/components" },
                        new() { Text = "Pricing", Icon = BitIconInfo.Css("fa-solid fa-tag"), Url = "https://bitplatform.dev/pricing" },
                    ]
                },
                new() { Text = "About", Icon = BitIconInfo.Fa("solid circle-info"), Url = "https://bitplatform.dev/about-us" },
                new() { Text = "Contact us", Icon = BitIconInfo.Css("fa-solid fa-envelope"), Url = "https://bitplatform.dev/contact-us" },
            ],
        },
        new() { Text = "Iconography", Icon = BitIconInfo.Css("fa-solid fa-icons"), Url = "/iconography" },
    ];

    private static readonly BitIconInfo bootstrapChevronIcon = BitIconInfo.Bi("chevron-right");

    private static readonly List<Section> customBootstrapIconNavItems =
    [
        new()
        {
            Text = "bit platform",
            Comment = "Nav with external icons (Bootstrap Icons)",
            Links =
            [
                new() { Text = "Home", Icon = BitIconInfo.Bi("house-fill"), Url = "https://bitplatform.dev/" },
                new() { Text = "BlazorUI", Icon = BitIconInfo.Bi("code-slash"), Url = "https://bitplatform.dev/components" },
                new() { Text = "Pricing", Icon = BitIconInfo.Bi("tag-fill"), Url = "https://bitplatform.dev/pricing" },
            ],
        },
        new() { Text = "Iconography", Icon = BitIconInfo.Bi("emoji-smile"), Url = "/iconography" },
    ];

    private static readonly List<Section> customCustomStyleNavItems =
    [
        new()
        {
            Text = "bit platform",
            Comment = "the bit platform description",
            Links =
            [
                new() { Text = "Home", ImageName = BitIconName.Home, Url = "https://bitplatform.dev/" },
                new() { Text = "BlazorUI", ImageName = BitIconName.F12DevTools, Url = "https://bitplatform.dev/components" },
            ],
        },
        new() { Text = "Iconography", ImageName = BitIconName.AppIconDefault, Url = "/iconography" },
        new() { IsDivider = true },
        new() { Text = "Contact us", ImageName = BitIconName.Contact, Url = "https://bitplatform.dev/contact-us" },
    ];

    private static readonly List<Section> customClassesNavItems =
    [
        new()
        {
            Text = "bit platform",
            ImageName = BitIconName.Website,
            Comment = "the bit platform description",
            Links =
            [
                new() { Text = "Home", ImageName = BitIconName.Home, Url = "https://bitplatform.dev/" },
                new() { Text = "BlazorUI", ImageName = BitIconName.F12DevTools, Url = "https://bitplatform.dev/components" },
            ],
        },
        new()
        {
            Text = "Community",
            ImageName = BitIconName.Group,
            Links = [new() { Text = "GitHub repo", ImageName = BitIconName.GitGraph, Url = "https://github.com/bitfoundation/bitplatform" }]
        },
    ];

    private static readonly List<Section> customRtlNavItems =
    [
        new()
        {
            Text = "پلتفرمِ بیت",
            Comment = "توضیحاتِ پلتفرمِ بیت",
            Links =
            [
                new() { Text = "خانه", ImageName = BitIconName.Home, Url = "https://bitplatform.dev/" },
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
                                new() { Text = "نمونه ی Todo", ImageName = BitIconName.ToDoLogoOutline, Url = "https://bitplatform.dev/templates/overview" },
                                new() { Text = "نمونه ی AdminPanel", ImageName = BitIconName.LocalAdmin, Url = "https://bitplatform.dev/templates/overview" },
                            ]
                        },
                        new() { Text = "رابط کاربری Blazor", ImageName = BitIconName.F12DevTools, Url = "https://blazorui.bitplatform.dev/" },
                        new() { Text = "راه های هاست ابری", ImageName = BitIconName.Cloud, Url = "https://bitplatform.dev/#", IsEnabled = false },
                        new() { Text = "آکادمی بیت", ImageName = BitIconName.LearningTools, Url = "https://bitplatform.dev/#", IsEnabled = false },
                    ]
                },
                new() { Text = "قیمت", ImageName = BitIconName.Money, Url = "https://bitplatform.dev/pricing" },
                new() { Text = "درباره ما", ImageName = BitIconName.Info, Url = "https://bitplatform.dev/about-us" },
                new() { Text = "ارتباط با ما", ImageName = BitIconName.Contact, Url = "https://bitplatform.dev/contact-us" },
            ],
        },
        new()
        {
            Text = "انجمن ها",
            Links =
            [
                new() { Text = "لینکدین", ImageName = BitIconName.LinkedInLogo, Url = "https://www.linkedin.com/company/bitplatformhq" },
                new() { Text = "توییتر", ImageName = BitIconName.Globe, Url = "https://twitter.com/bitplatformhq" },
                new() { Text = "گیتهاب", ImageName = BitIconName.GitGraph, Url = "https://github.com/bitfoundation/bitplatform" },
            ]
        },
        new() { Text = "شمایل نگاری", ImageName = BitIconName.AppIconDefault, Url = "/iconography" },
    ];

    private bool iconOnly;

    private static List<FoodMenu> Flatten(IList<FoodMenu> e) => e.SelectMany(c => Flatten(c.Childs)).Concat(e).ToList();
    private FoodMenu CustomSelectedFood = customFoodNavItems[0].Childs[2];
    private string? CustomSelectedFoodName = customFoodNavItems[0].Childs[2].Name;

    private FoodMenu CustomClickedItem = default!;
    private FoodMenu CustomSelectedItem = default!;
    private FoodMenu CustomToggledItem = default!;

    private BitNav<Section>? apiNavRef;

    private void ExpandAllApiItems() => apiNavRef?.ExpandAll();
    private void CollapseAllApiItems() => apiNavRef?.CollapseAll();
    private async Task ToggleFruitsApiItem() { if (apiNavRef is not null) await apiNavRef.ToggleItem(customApiNavItems[1]); }
    private async Task ExpandFastFoodsApiItem() { if (apiNavRef is not null) await apiNavRef.ExpandItem(customApiNavItems[0]); }
    private async Task CollapseFastFoodsApiItem() { if (apiNavRef is not null) await apiNavRef.CollapseItem(customApiNavItems[0]); }
    private async Task SelectIceCreamApiItem() { if (apiNavRef is not null) await apiNavRef.SelectItem(customApiNavItems[2]); }
    private async Task FocusVeggieBurgerApiItem() { if (apiNavRef is not null) await apiNavRef.FocusItem(customApiNavItems[0].Links[0].Links[1]); }

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
}

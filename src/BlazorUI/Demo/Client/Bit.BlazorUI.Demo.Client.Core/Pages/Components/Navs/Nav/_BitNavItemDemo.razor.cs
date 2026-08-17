namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Nav;

public partial class _BitNavItemDemo
{
    private static readonly List<BitNavItem> basicNavItems =
    [
        new()
        {
            Text = "bit platform",
            Description = "the bit platform description",
            ChildItems =
            [
                new() { Text = "Home", IconName = BitIconName.Home, Url = "https://bitplatform.dev/" },
                new()
                {
                    Text = "Products & Services",
                    ChildItems =
                    [
                        new()
                        {
                            Text = "Project Templates",
                            ChildItems =
                            [
                                new() { Text = "Todo sample", IconName = BitIconName.ToDoLogoOutline, Url = "https://bitplatform.dev/templates/overview" },
                                new() { Text = "AdminPanel sample", IconName = BitIconName.LocalAdmin, Url = "https://bitplatform.dev/templates/overview" },
                            ]
                        },
                        new() { Text = "BlazorUI", IconName = BitIconName.F12DevTools, Url = "https://bitplatform.dev/components" },
                        new() { Text = "Cloud hosting solutions", IconName = BitIconName.Cloud, Url = "https://bitplatform.dev/#", IsEnabled = false },
                        new() { Text = "Bit academy", IconName = BitIconName.LearningTools, Url = "https://bitplatform.dev/#", IsEnabled = false },
                    ]
                },
                new() { Text = "Pricing", IconName = BitIconName.Money, Url = "https://bitplatform.dev/pricing" },
                new() { Text = "About", IconName = BitIconName.Info, Url = "https://bitplatform.dev/about-us" },
                new() { Text = "Contact us", IconName = BitIconName.Contact, Url = "https://bitplatform.dev/contact-us" },
            ],
        },
        new()
        {
            Text = "Community",
            ChildItems =
            [
                new() { Text = "LinkedIn", IconName = BitIconName.LinkedInLogo , Url = "https://www.linkedin.com/company/bitplatformhq" },
                new() { Text = "Twitter", IconName = BitIconName.Globe , Url = "https://twitter.com/bitplatformhq" },
                new() { Text = "GitHub repo", IconName = BitIconName.GitGraph , Url = "https://github.com/bitfoundation/bitplatform" },
            ]
        },
        new() { Text = "Iconography", IconName = BitIconName.AppIconDefault, Url = "/iconography" },
    ];

    private static readonly List<BitNavItem> separatorNavItems =
    [
        new() { Text = "Home", IconName = BitIconName.Home, Url = "https://bitplatform.dev/" },
        new() { Text = "Pricing", IconName = BitIconName.Money, Url = "https://bitplatform.dev/pricing" },
        new() { IsSeparator = true },
        new() { Text = "LinkedIn", IconName = BitIconName.LinkedInLogo, Url = "https://www.linkedin.com/company/bitplatformhq" },
        new() { Text = "GitHub repo", IconName = BitIconName.GitGraph, Url = "https://github.com/bitfoundation/bitplatform" },
        new() { IsSeparator = true },
        new() { Text = "Contact us", IconName = BitIconName.Contact, Url = "https://bitplatform.dev/contact-us" },
    ];

    private static readonly List<BitNavItem> iconOnlyNavItems =
    [
        new() { Text = "Home", IconName = BitIconName.Home },
        new() {
            Text = "AdminPanel sample",
            IconName = BitIconName.LocalAdmin,
            ChildItems =
            [
                new() { Text = "Dashboard", IconName = BitIconName.ViewDashboard },
                new() { Text = "Categories", IconName = BitIconName.BuildQueue },
                new() { Text = "Products", IconName = BitIconName.Product },
            ]
        },
        new() { Text = "Todo sample", IconName = BitIconName.ToDoLogoOutline},
        new() { Text = "BlazorUI", IconName = BitIconName.F12DevTools },
        new() { Text = "Bit academy", IconName = BitIconName.LearningTools, IsEnabled = false },
        new() { Text = "Contact us", IconName = BitIconName.Contact },
    ];

    private static readonly List<BitNavItem> carNavItems =
    [
        new()
        {
            Text = "Mercedes-Benz",
            ExpandAriaLabel = "Mercedes-Benz Expanded",
            CollapseAriaLabel = "Mercedes-Benz Collapsed",
            Title = "Mercedes-Benz Car Models",
            IsExpanded = true,
            Description = "Cars manufactured under the brand of Mercedes-Benz",
            ChildItems =
            [
                new()
                {
                    Text = "SUVs",
                    ChildItems =
                    [
                        new() { Text = "GLA", Url = "https://www.mbusa.com/en/vehicles/class/gla/suv", Target = "_blank" },
                        new() { Text = "GLB", Url = "https://www.mbusa.com/en/vehicles/class/glb/suv", Target = "_blank" },
                        new() { Text = "GLC", Url = "https://www.mbusa.com/en/vehicles/class/glc/suv", Target = "_blank" },
                    ]
                },
                new()
                {
                    Text = "Sedans & Wagons",
                    ChildItems =
                    [
                        new() { Text = "A Class", Url = "https://www.mbusa.com/en/vehicles/class/a-class/sedan", Target = "_blank" },
                        new() { Text = "C Class", Url = "https://www.mbusa.com/en/vehicles/class/c-class/sedan", Target = "_blank" },
                        new() { Text = "E Class", Url = "https://www.mbusa.com/en/vehicles/class/e-class/sedan", Target = "_blank" },
                    ]
                },
                new()
                {
                    Text = "Coupes",
                    ChildItems =
                    [
                        new() { Text = "CLA Coupe", Url = "https://www.mbusa.com/en/vehicles/class/cla/coupe", Target = "_blank" },
                        new() { Text = "C Class Coupe", Url = "https://www.mbusa.com/en/vehicles/class/c-class/coupe", Target = "_blank" },
                        new() { Text = "E Class Coupe", Url = "https://www.mbusa.com/en/vehicles/class/e-class/coupe", Target = "_blank" },
                    ]
                },
            ]
        },
        new()
        {
            Text = "Tesla",
            ExpandAriaLabel = "Tesla Expanded",
            CollapseAriaLabel= "Tesla Collapsed",
            Title = "Tesla Car Models",
            ChildItems =
            [
                new() { Text = "Model S", Url = "https://www.tesla.com/models", Target = "_blank" },
                new() { Text = "Model X", Url = "https://www.tesla.com/modelx", Target = "_blank" },
                new() { Text = "Model Y", Url = "https://www.tesla.com/modely", Target = "_blank" },
            ]
        },
    ];

    private static readonly List<BitNavItem> foodNavItems =
    [
        new()
        {
            Text = "Fast foods",
            IconName = BitIconName.HeartBroken,
            IsExpanded = true,
            Description = "List of fast foods",
            ChildItems =
            [
                new()
                {
                    Text = "Burgers",
                    Description = "List of burgers",
                    ChildItems =
                    [
                        new() { Text = "Beef Burger" },
                        new() { Text = "Veggie Burger" },
                        new() { Text = "Bison Burger" },
                        new() { Text = "Wild Salmon Burger" },
                    ]
                },
                new()
                {
                    Text = "Pizza",
                    ChildItems =
                    [
                        new() { Text = "Cheese Pizza" },
                        new() { Text = "Veggie Pizza" },
                        new() { Text = "Pepperoni Pizza" },
                        new() { Text = "Meat Pizza" },
                    ]
                },
                new() { Text = "French Fries" },
            ]
        },
        new()
        {
            Text = "Fruits",
            IconName = BitIconName.Health,
            ChildItems =
            [
                new() { Text = "Apple" },
                new() { Text = "Orange" },
                new() { Text = "Banana" },
            ]
        },
        new() { Text = "Ice Cream" },
        new() { Text = "Cookie" },
    ];

    private static readonly List<BitNavItem> singleExpandNavItems =
    [
        new()
        {
            Text = "Fast foods",
            IconName = BitIconName.HeartBroken,
            ChildItems =
            [
                new() { Text = "Burgers", ChildItems = [new() { Text = "Beef Burger" }, new() { Text = "Veggie Burger" }] },
                new() { Text = "Pizza", ChildItems = [new() { Text = "Cheese Pizza" }, new() { Text = "Meat Pizza" }] },
                new() { Text = "French Fries" },
            ]
        },
        new()
        {
            Text = "Fruits",
            IconName = BitIconName.Health,
            ChildItems =
            [
                new() { Text = "Apple" },
                new() { Text = "Orange" },
                new() { Text = "Banana" },
            ]
        },
        new()
        {
            Text = "Drinks",
            IconName = BitIconName.Coffee,
            ChildItems =
            [
                new() { Text = "Water" },
                new() { Text = "Tea" },
            ]
        },
    ];

    private static readonly List<BitNavItem> chevronNavItems =
    [
        new()
        {
            Text = "bit platform",
            IconName = BitIconName.Website,
            ChildItems =
            [
                new() { Text = "Home", IconName = BitIconName.Home },
                new()
                {
                    Text = "Products & Services",
                    IconName = BitIconName.Product,
                    ChildItems =
                    [
                        new() { Text = "BlazorUI", IconName = BitIconName.F12DevTools },
                        new() { Text = "Pricing", IconName = BitIconName.Money },
                    ]
                },
            ]
        },
        new() { Text = "Iconography", IconName = BitIconName.AppIconDefault },
    ];

    private static readonly List<BitNavItem> apiNavItems =
    [
        new()
        {
            Text = "Fast foods",
            IconName = BitIconName.HeartBroken,
            ChildItems =
            [
                new() { Text = "Burgers", ChildItems = [new() { Text = "Beef Burger" }, new() { Text = "Veggie Burger" }] },
                new() { Text = "Pizza", ChildItems = [new() { Text = "Cheese Pizza" }, new() { Text = "Meat Pizza" }] },
            ]
        },
        new()
        {
            Text = "Fruits",
            IconName = BitIconName.Health,
            ChildItems = [new() { Text = "Apple" }, new() { Text = "Orange" }]
        },
        new() { Text = "Ice Cream", IconName = BitIconName.Emoji2 },
        new() { Text = "Cookie", IconName = BitIconName.Cake },
    ];

    private static readonly List<BitNavItem> colorNavItems =
    [
        new() { Text = "Home", IconName = BitIconName.Home },
        new() { Text = "Products", IconName = BitIconName.Product },
        new() { Text = "Settings", IconName = BitIconName.Settings },
    ];

    private static readonly List<BitNavItem> accentNavItems =
    [
        new() { Text = "Home", IconName = BitIconName.Home },
        new() { Text = "Products", IconName = BitIconName.Product },
        new() { Text = "Settings", IconName = BitIconName.Settings },
    ];

    private static readonly List<BitNavItem> sizeNavItems =
    [
        new() { Text = "Home", IconName = BitIconName.Home, Description = "The main page" },
        new() { Text = "Products", IconName = BitIconName.Product, Description = "All of the products" },
        new() { Text = "Settings", IconName = BitIconName.Settings, Description = "The app settings" },
    ];

    private static readonly List<BitNavItem> styleClassNavItems =
    [
        new() { Text = "Home", IconName = BitIconName.Home, Style = "background: rgba(255,99,71,0.2);" },
        new() { Text = "Products", IconName = BitIconName.Product, Class = "custom-item-list" },
        new() { Text = "Settings", IconName = BitIconName.Settings },
    ];

    private static readonly List<BitNavItem> externalIconNavItems =
    [
        new()
        {
            Text = "bit platform",
            Description = "Nav with external icons (FontAwesome)",
            ChildItems =
            [
                new() { Text = "Home", Icon = BitIconInfo.Css("fa-solid fa-house"), Url = "https://bitplatform.dev/" },
                new()
                {
                    Text = "Products & Services",
                    ChildItems =
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

    private static readonly List<BitNavItem> bootstrapIconNavItems =
    [
        new()
        {
            Text = "bit platform",
            Description = "Nav with external icons (Bootstrap Icons)",
            ChildItems =
            [
                new() { Text = "Home", Icon = BitIconInfo.Bi("house-fill"), Url = "https://bitplatform.dev/" },
                new() { Text = "BlazorUI", Icon = BitIconInfo.Bi("code-slash"), Url = "https://bitplatform.dev/components" },
                new() { Text = "Pricing", Icon = BitIconInfo.Bi("tag-fill"), Url = "https://bitplatform.dev/pricing" },
            ],
        },
        new() { Text = "Iconography", Icon = BitIconInfo.Bi("emoji-smile"), Url = "/iconography" },
    ];

    private static readonly List<BitNavItem> noCollapseNavItems =
    [
        new()
        {
            Text = "bit platform",
            Description = "the bit platform description",
            ChildItems =
            [
                new() { Text = "Home", IconName = BitIconName.Home, Url = "https://bitplatform.dev/" },
                new()
                {
                    Text = "Products & Services",
                    ChildItems =
                    [
                        new()
                        {
                            Text = "Project Templates",
                            ChildItems =
                            [
                                new() { Text = "Todo sample", IconName = BitIconName.ToDoLogoOutline, Url = "https://bitplatform.dev/templates/overview" },
                                new() { Text = "AdminPanel sample", IconName = BitIconName.LocalAdmin, Url = "https://bitplatform.dev/templates/overview" },
                            ]
                        },
                        new() { Text = "BlazorUI", IconName = BitIconName.F12DevTools, Url = "https://bitplatform.dev/components" },
                        new() { Text = "Cloud hosting solutions", IconName = BitIconName.Cloud, Url = "https://bitplatform.dev/#", IsEnabled = false },
                        new() { Text = "Bit academy", IconName = BitIconName.LearningTools, Url = "https://bitplatform.dev/#", IsEnabled = false },
                    ]
                },
                new() { Text = "Pricing", IconName = BitIconName.Money, Url = "https://bitplatform.dev/pricing" },
                new() { Text = "About", IconName = BitIconName.Info, Url = "https://bitplatform.dev/about-us" },
                new() { Text = "Contact us", IconName = BitIconName.Contact, Url = "https://bitplatform.dev/contact-us" },
            ],
        },
        new()
        {
            Text = "Community",
            ChildItems =
            [
                new() { Text = "LinkedIn", IconName = BitIconName.LinkedInLogo , Url = "https://www.linkedin.com/company/bitplatformhq" },
                new() { Text = "Twitter", IconName = BitIconName.Globe , Url = "https://twitter.com/bitplatformhq" },
                new() { Text = "GitHub repo", IconName = BitIconName.GitGraph , Url = "https://github.com/bitfoundation/bitplatform" },
            ]
        },
        new() { Text = "Iconography", IconName = BitIconName.AppIconDefault, Url = "/iconography" },
    ];

    private static readonly List<BitNavItem> customStyleNavItems =
    [
        new()
        {
            Text = "bit platform",
            Description = "the bit platform description",
            ChildItems =
            [
                new() { Text = "Home", IconName = BitIconName.Home, Url = "https://bitplatform.dev/" },
                new() { Text = "BlazorUI", IconName = BitIconName.F12DevTools, Url = "https://bitplatform.dev/components" },
            ],
        },
        new() { Text = "Iconography", IconName = BitIconName.AppIconDefault, Url = "/iconography" },
        new() { IsSeparator = true },
        new() { Text = "Contact us", IconName = BitIconName.Contact, Url = "https://bitplatform.dev/contact-us" },
    ];

    private static readonly List<BitNavItem> customClassNavItems =
    [
        new()
        {
            Text = "bit platform",
            IconName = BitIconName.Website,
            Description = "the bit platform description",
            ChildItems =
            [
                new() { Text = "Home", IconName = BitIconName.Home, Url = "https://bitplatform.dev/" },
                new() { Text = "BlazorUI", IconName = BitIconName.F12DevTools, Url = "https://bitplatform.dev/components" },
            ],
        },
        new()
        {
            Text = "Community",
            IconName = BitIconName.Group,
            ChildItems =
            [
                new() { Text = "GitHub repo", IconName = BitIconName.GitGraph, Url = "https://github.com/bitfoundation/bitplatform" },
            ]
        },
    ];

    private static readonly List<BitNavItem> rtlNavItems =
    [
        new()
        {
            Text = "پلتفرمِ بیت",
            Description = "توضیحاتِ پلتفرمِ بیت",
            ChildItems =
            [
                new() { Text = "خانه", IconName = BitIconName.Home, Url = "https://bitplatform.dev/" },
                new()
                {
                    Text = "محصولات و خدمات",
                    ChildItems =
                    [
                        new()
                        {
                            Text = "قالب های پروژه",
                            ChildItems =
                            [
                                new() { Text = "نمونه ی Todo", IconName = BitIconName.ToDoLogoOutline, Url = "https://bitplatform.dev/templates/overview" },
                                new() { Text = "نمونه ی AdminPanel", IconName = BitIconName.LocalAdmin, Url = "https://bitplatform.dev/templates/overview" },
                            ]
                        },
                        new() { Text = "رابط کاربری Blazor", IconName = BitIconName.F12DevTools, Url = "https://blazorui.bitplatform.dev/" },
                        new() { Text = "راه های هاست ابری", IconName = BitIconName.Cloud, Url = "https://bitplatform.dev/#", IsEnabled = false },
                        new() { Text = "آکادمی بیت", IconName = BitIconName.LearningTools, Url = "https://bitplatform.dev/#", IsEnabled = false },
                    ]
                },
                new() { Text = "قیمت", IconName = BitIconName.Money, Url = "https://bitplatform.dev/pricing" },
                new() { Text = "درباره ما", IconName = BitIconName.Info, Url = "https://bitplatform.dev/about-us" },
                new() { Text = "ارتباط با ما", IconName = BitIconName.Contact, Url = "https://bitplatform.dev/contact-us" },
            ],
        },
        new()
        {
            Text = "انجمن ها",
            ChildItems =
            [
                new() { Text = "لینکدین", IconName = BitIconName.LinkedInLogo , Url = "https://www.linkedin.com/company/bitplatformhq" },
                new() { Text = "توییتر", IconName = BitIconName.Globe , Url = "https://twitter.com/bitplatformhq" },
                new() { Text = "گیتهاب", IconName = BitIconName.GitGraph , Url = "https://github.com/bitfoundation/bitplatform" },
            ]
        },
        new() { Text = "شمایل نگاری", IconName = BitIconName.AppIconDefault, Url = "/iconography" },
    ];

    private bool iconOnly;

    private static List<BitNavItem> Flatten(IList<BitNavItem> e) => e.SelectMany(c => Flatten(c.ChildItems)).Concat(e).ToList();
    private BitNavItem SelectedItemNav = foodNavItems[0].ChildItems[2];
    private string? SelectedItemText = foodNavItems[0].ChildItems[2].Text;

    private BitNavItem ClickedItem = default!;
    private BitNavItem SelectedItem = default!;
    private BitNavItem ToggledItem = default!;

    private BitNav<BitNavItem>? apiNavRef;

    private void ExpandAllApiItems() => apiNavRef?.ExpandAll();
    private void CollapseAllApiItems() => apiNavRef?.CollapseAll();
    private async Task ToggleFruitsApiItem() { if (apiNavRef is not null) await apiNavRef.ToggleItem(apiNavItems[1]); }
    private async Task SelectIceCreamApiItem() { if (apiNavRef is not null) await apiNavRef.SelectItem(apiNavItems[2]); }
    private async Task FocusCookieApiItem() { if (apiNavRef is not null) await apiNavRef.FocusItem(apiNavItems[3]); }

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

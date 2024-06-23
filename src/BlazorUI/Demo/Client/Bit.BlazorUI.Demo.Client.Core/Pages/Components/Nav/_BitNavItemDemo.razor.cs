namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Nav;

public partial class _BitNavItemDemo
{
    private static readonly List<BitNavItem> BitPlatformNavMenu =
    [
        new()
        {
            Text = "bit platform",
            Description = "the bit platform description",
            ChildItems = new()
            {
                new() { Text = "Home", IconName = BitIconName.Home, Url = "https://bitplatform.dev/" },
                new()
                {
                    Text = "Products & Services",
                    ChildItems = new()
                    {
                        new()
                        {
                            Text = "Project Templates",
                            ChildItems = new()
                            {
                                new() { Text = "Todo sample", IconName = BitIconName.ToDoLogoOutline, Url = "https://bitplatform.dev/templates/overview" },
                                new() { Text = "AdminPanel sample", IconName = BitIconName.LocalAdmin, Url = "https://bitplatform.dev/templates/overview" },
                            }
                        },
                        new() { Text = "BlazorUI", IconName = BitIconName.F12DevTools, Url = "https://bitplatform.dev/components" },
                        new() { Text = "Cloud hosting solutions", IconName = BitIconName.Cloud, Url = "https://bitplatform.dev/#", IsEnabled = false },
                        new() { Text = "Bit academy", IconName = BitIconName.LearningTools, Url = "https://bitplatform.dev/#", IsEnabled = false },
                    }
                },
                new() { Text = "Pricing", IconName = BitIconName.Money, Url = "https://bitplatform.dev/pricing" },
                new() { Text = "About", IconName = BitIconName.Info, Url = "https://bitplatform.dev/about-us" },
                new() { Text = "Contact us", IconName = BitIconName.Contact, Url = "https://bitplatform.dev/contact-us" },
            },
        },
        new()
        {
            Text = "Community",
            ChildItems = new()
            {
                new() { Text = "LinkedIn", IconName = BitIconName.LinkedInLogo , Url = "https://www.linkedin.com/company/bitplatformhq" },
                new() { Text = "Twitter", IconName = BitIconName.Globe , Url = "https://twitter.com/bitplatformhq" },
                new() { Text = "GitHub repo", IconName = BitIconName.GitGraph , Url = "https://github.com/bitfoundation/bitplatform" },
            }
        },
        new() { Text = "Iconography", IconName = BitIconName.AppIconDefault, Url = "/iconography" },
    ];

    private static readonly List<BitNavItem> CarNavMenu =
    [
        new()
        {
            Text = "Mercedes-Benz",
            ExpandAriaLabel = "Mercedes-Benz Expanded",
            CollapseAriaLabel = "Mercedes-Benz Collapsed",
            Title = "Mercedes-Benz Car Models",
            IsExpanded = true,
            Description = "Cars manufactured under the brand of Mercedes-Benz",
            ChildItems = new()
            {
                new()
                {
                    Text = "SUVs",
                    ChildItems = new()
                    {
                        new() { Text = "GLA", Url = "https://www.mbusa.com/en/vehicles/class/gla/suv", Target = "_blank" },
                        new() { Text = "GLB", Url = "https://www.mbusa.com/en/vehicles/class/glb/suv", Target = "_blank" },
                        new() { Text = "GLC", Url = "https://www.mbusa.com/en/vehicles/class/glc/suv", Target = "_blank" },
                    }
                },
                new()
                {
                    Text = "Sedans & Wagons",
                    ChildItems = new()
                    {
                        new() { Text = "A Class", Url = "https://www.mbusa.com/en/vehicles/class/a-class/sedan", Target = "_blank" },
                        new() { Text = "C Class", Url = "https://www.mbusa.com/en/vehicles/class/c-class/sedan", Target = "_blank" },
                        new() { Text = "E Class", Url = "https://www.mbusa.com/en/vehicles/class/e-class/sedan", Target = "_blank" },
                    }
                },
                new()
                {
                    Text = "Coupes",
                    ChildItems = new()
                    {
                        new() { Text = "CLA Coupe", Url = "https://www.mbusa.com/en/vehicles/class/cla/coupe", Target = "_blank" },
                        new() { Text = "C Class Coupe", Url = "https://www.mbusa.com/en/vehicles/class/c-class/coupe", Target = "_blank" },
                        new() { Text = "E Class Coupe", Url = "https://www.mbusa.com/en/vehicles/class/e-class/coupe", Target = "_blank" },
                    }
                },
            }
        },
        new()
        {
            Text = "Tesla",
            ExpandAriaLabel = "Tesla Expanded",
            CollapseAriaLabel= "Tesla Collapsed",
            Title = "Tesla Car Models",
            ChildItems = new List<BitNavItem>
            {
                new() { Text = "Model S", Url = "https://www.tesla.com/models", Target = "_blank" },
                new() { Text = "Model X", Url = "https://www.tesla.com/modelx", Target = "_blank" },
                new() { Text = "Model Y", Url = "https://www.tesla.com/modely", Target = "_blank" },
            }
        },
    ];

    private static readonly List<BitNavItem> FoodNavMenu =
    [
        new()
        {
            Text = "Fast foods",
            IconName = BitIconName.HeartBroken,
            IsExpanded = true,
            Description = "List of fast foods",
            ChildItems = new()
            {
                new()
                {
                    Text = "Burgers",
                    Description = "List of burgers",
                    ChildItems = new()
                    {
                        new() { Text = "Beef Burger" },
                        new() { Text = "Veggie Burger" },
                        new() { Text = "Bison Burger" },
                        new() { Text = "Wild Salmon Burger" },
                    }
                },
                new()
                {
                    Text = "Pizza",
                    ChildItems = new()
                    {
                        new() { Text = "Cheese Pizza" },
                        new() { Text = "Veggie Pizza" },
                        new() { Text = "Pepperoni Pizza" },
                        new() { Text = "Meat Pizza" },
                    }
                },
                new() { Text = "French Fries" },
            }
        },
        new()
        {
            Text = "Fruits",
            IconName = BitIconName.Health,
            ChildItems = new()
            {
                new() { Text = "Apple" },
                new() { Text = "Orange" },
                new() { Text = "Banana" },
            }
        },
        new() { Text = "Ice Cream" },
        new() { Text = "Cookie" },
    ];

    private static readonly List<BitNavItem> CustomStyleNavMenu =
    [
        new()
        {
            Text = "bit platform",
            Description = "the bit platform description",
            ChildItems = new()
            {
                new() { Text = "Home", IconName = BitIconName.Home, Url = "https://bitplatform.dev/" },
                new()
                {
                    Text = "Products & Services",
                    ChildItems = new()
                    {
                        new()
                        {
                            Text = "Project Templates",
                            ChildItems = new()
                            {
                                new() { Text = "Todo sample", IconName = BitIconName.ToDoLogoOutline, Url = "https://bitplatform.dev/templates/overview" },
                                new() { Text = "AdminPanel sample", IconName = BitIconName.LocalAdmin, Url = "https://bitplatform.dev/templates/overview" },
                            }
                        },
                        new() { Text = "BlazorUI", IconName = BitIconName.F12DevTools, Url = "https://bitplatform.dev/components" },
                        new() { Text = "Cloud hosting solutions", IconName = BitIconName.Cloud, Url = "https://bitplatform.dev/#", IsEnabled = false },
                        new() { Text = "Bit academy", IconName = BitIconName.LearningTools, Url = "https://bitplatform.dev/#", IsEnabled = false },
                    }
                },
                new() { Text = "Pricing", IconName = BitIconName.Money, Url = "https://bitplatform.dev/pricing" },
                new() { Text = "About", IconName = BitIconName.Info, Url = "https://bitplatform.dev/about-us" },
                new() { Text = "Contact us", IconName = BitIconName.Contact, Url = "https://bitplatform.dev/contact-us" },
            },
        },
        new()
        {
            Text = "Community",
            ChildItems = new()
            {
                new() { Text = "LinkedIn", IconName = BitIconName.LinkedInLogo , Url = "https://www.linkedin.com/company/bitplatformhq" },
                new() { Text = "Twitter", IconName = BitIconName.Globe , Url = "https://twitter.com/bitplatformhq" },
                new() { Text = "GitHub repo", IconName = BitIconName.GitGraph , Url = "https://github.com/bitfoundation/bitplatform" },
            }
        },
        new() { Text = "Iconography", IconName = BitIconName.AppIconDefault, Url = "/iconography" },
    ];

    private static readonly List<BitNavItem> RtlBitPlatformNavMenu =
    [
        new()
        {
            Text = "پلتفرمِ بیت",
            Description = "توضیحاتِ پلتفرمِ بیت",
            ChildItems = new()
            {
                new() { Text = "خانه", IconName = BitIconName.Home, Url = "https://bitplatform.dev/" },
                new()
                {
                    Text = "محصولات و خدمات",
                    ChildItems = new()
                    {
                        new()
                        {
                            Text = "قالب های پروژه",
                            ChildItems = new()
                            {
                                new() { Text = "نمونه ی Todo", IconName = BitIconName.ToDoLogoOutline, Url = "https://bitplatform.dev/templates/overview" },
                                new() { Text = "نمونه ی AdminPanel", IconName = BitIconName.LocalAdmin, Url = "https://bitplatform.dev/templates/overview" },
                            }
                        },
                        new() { Text = "رابط کاربری Blazor", IconName = BitIconName.F12DevTools, Url = "https://blazorui.bitplatform.dev/" },
                        new() { Text = "راه های هاست ابری", IconName = BitIconName.Cloud, Url = "https://bitplatform.dev/#", IsEnabled = false },
                        new() { Text = "آکادمی بیت", IconName = BitIconName.LearningTools, Url = "https://bitplatform.dev/#", IsEnabled = false },
                    }
                },
                new() { Text = "قیمت", IconName = BitIconName.Money, Url = "https://bitplatform.dev/pricing" },
                new() { Text = "درباره ما", IconName = BitIconName.Info, Url = "https://bitplatform.dev/about-us" },
                new() { Text = "ارتباط با ما", IconName = BitIconName.Contact, Url = "https://bitplatform.dev/contact-us" },
            },
        },
        new()
        {
            Text = "انجمن ها",
            ChildItems = new()
            {
                new() { Text = "لینکدین", IconName = BitIconName.LinkedInLogo , Url = "https://www.linkedin.com/company/bitplatformhq" },
                new() { Text = "توییتر", IconName = BitIconName.Globe , Url = "https://twitter.com/bitplatformhq" },
                new() { Text = "گیتهاب", IconName = BitIconName.GitGraph , Url = "https://github.com/bitfoundation/bitplatform" },
            }
        },
        new() { Text = "شمایل نگاری", IconName = BitIconName.AppIconDefault, Url = "/iconography" },
    ];

    private static List<BitNavItem> Flatten(IList<BitNavItem> e) => e.SelectMany(c => Flatten(c.ChildItems)).Concat(e).ToList();
    private BitNavItem SelectedItemNav = FoodNavMenu[0].ChildItems[2];
    private string? SelectedItemText = FoodNavMenu[0].ChildItems[2].Text;

    private BitNavItem ClickedItem = default!;
    private BitNavItem SelectedItem = default!;
    private BitNavItem ToggledItem = default!;

    private static readonly List<BitDropdownItem<string>> FoodMenuDropdownItems =
    [
        new()
        {
            Text = "Beef Burger",
            Value = "Beef Burger",
        },
        new()
        {
            Text = "Veggie Burger",
            Value = "Veggie Burger",
        },
        new()
        {
            Text = "Bison Burger",
            Value = "Bison Burger",
        },
        new()
        {
            Text = "Wild Salmon Burger",
            Value = "Wild Salmon Burger",
        },
        new()
        {
            Text = "Cheese Pizza",
            Value = "Cheese Pizza",
        },
        new()
        {
            Text = "Veggie Pizza",
            Value = "Veggie Pizza",
        },
        new()
        {
            Text = "Pepperoni Pizza",
            Value = "Pepperoni Pizza",
        },
        new()
        {
            Text = "Meat Pizza",
            Value = "Meat Pizza",
        },
        new()
        {
            Text = "French Fries",
            Value = "French Fries",
        },
        new()
        {
            Text = "Apple",
            Value = "Apple",
        },
        new()
        {
            Text = "Orange",
            Value = "Orange",
        },
        new()
        {
            Text = "Banana",
            Value = "Banana",
        },
        new()
        {
            Text = "Ice Cream",
            Value = "Ice Cream",
        },
        new()
        {
            Text = "Cookie",
            Value = "Cookie",
        },
    ];



    private readonly string example1NavItemRazorCode = @"
<BitNav Items=""BitPlatformNavMenu"" />";
    private readonly string example1NavItemCsharpCode = @"
private static readonly List<BitNavItem> BitPlatformNavMenu = new()
{
    new()
    {
        Text = ""bit platform"",
        Description = ""the bit platform description"",
        ChildItems = new()
        {
            new() { Text = ""Home"", IconName = BitIconName.Home, Url = ""https://bitplatform.dev/"" },
            new()
            {
                Text = ""Products & Services"",
                ChildItems = new()
                {
                    new()
                    {
                        Text = ""Project Templates"",
                        ChildItems = new()
                        {
                            new() { Text = ""Todo sample"", IconName = BitIconName.ToDoLogoOutline, Url = ""https://bitplatform.dev/templates/overview"" },
                            new() { Text = ""AdminPanel sample"", IconName = BitIconName.LocalAdmin, Url = ""https://bitplatform.dev/templates/overview"" },
                        }
                    },
                    new() { Text = ""BlazorUI"", IconName = BitIconName.F12DevTools, Url = ""https://bitplatform.dev/components"" },
                    new() { Text = ""Cloud hosting solutions"", IconName = BitIconName.Cloud, Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                    new() { Text = ""Bit academy"", IconName = BitIconName.LearningTools, Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                }
            },
            new() { Text = ""Pricing"", IconName = BitIconName.Money, Url = ""https://bitplatform.dev/pricing"" },
            new() { Text = ""About"", IconName = BitIconName.Info, Url = ""https://bitplatform.dev/about-us"" },
            new() { Text = ""Contact us"", IconName = BitIconName.Contact, Url = ""https://bitplatform.dev/contact-us"" },
        },
    },
    new()
    {
        Text = ""Community"",
        ChildItems = new()
        {
            new() { Text = ""LinkedIn"", IconName = BitIconName.LinkedInLogo , Url = ""https://www.linkedin.com/company/bitplatformhq"" },
            new() { Text = ""Twitter"", IconName = BitIconName.Globe , Url = ""https://twitter.com/bitplatformhq"" },
            new() { Text = ""GitHub repo"", IconName = BitIconName.GitGraph , Url = ""https://github.com/bitfoundation/bitplatform"" },
        }
    },
    new() { Text = ""Iconography"", IconName = BitIconName.AppIconDefault, Url = ""/iconography"" },
};";

    private readonly string example2NavItemRazorCode = @"
<BitNav Items=""CarNavMenu"" RenderType=""BitNavRenderType.Grouped"" />";
    private readonly string example2NavItemCsharpCode = @"
private static readonly List<BitNavItem> CarNavMenu = new()
{
    new()
    {
        Text = ""Mercedes-Benz"",
        ExpandAriaLabel = ""Mercedes-Benz Expanded"",
        CollapseAriaLabel = ""Mercedes-Benz Collapsed"",
        Title = ""Mercedes-Benz Car Models"",
        IsExpanded = true,
        ChildItems = new List<BitNavItem>
        {
            new()
            {
                Text = ""SUVs"",
                ChildItems = new List<BitNavItem>
                {
                    new() { Text = ""GLA"", Url = ""https://www.mbusa.com/en/vehicles/class/gla/suv"", Target = ""_blank"" },
                    new() { Text = ""GLB"", Url = ""https://www.mbusa.com/en/vehicles/class/glb/suv"", Target = ""_blank"" },
                    new() { Text = ""GLC"", Url = ""https://www.mbusa.com/en/vehicles/class/glc/suv"", Target = ""_blank"" },
                }
            },
            new()
            {
                Text = ""Sedans & Wagons"",
                ChildItems = new List<BitNavItem>
                {
                    new() { Text = ""A Class"", Url = ""https://www.mbusa.com/en/vehicles/class/a-class/sedan"", Target = ""_blank"" },
                    new() { Text = ""C Class"", Url = ""https://www.mbusa.com/en/vehicles/class/c-class/sedan"", Target = ""_blank"" },
                    new() { Text = ""E Class"", Url = ""https://www.mbusa.com/en/vehicles/class/e-class/sedan"", Target = ""_blank"" },
                }
            },
            new()
            {
                Text = ""Coupes"",
                ChildItems = new List<BitNavItem>
                {
                    new() { Text = ""CLA Coupe"", Url = ""https://www.mbusa.com/en/vehicles/class/cla/coupe"", Target = ""_blank"" },
                    new() { Text = ""C Class Coupe"", Url = ""https://www.mbusa.com/en/vehicles/class/c-class/coupe"", Target = ""_blank"" },
                    new() { Text = ""E Class Coupe"", Url = ""https://www.mbusa.com/en/vehicles/class/e-class/coupe"", Target = ""_blank"" },
                }
            },
        }
    },
    new()
    {
        Text = ""Tesla"",
        ExpandAriaLabel = ""Tesla Expanded"",
        CollapseAriaLabel= ""Tesla Collapsed"",
        Title = ""Tesla Car Models"",
        ChildItems = new List<BitNavItem>
        {
            new() { Text = ""Model S"", Url = ""https://www.tesla.com/models"", Target = ""_blank"" },
            new() { Text = ""Model X"", Url = ""https://www.tesla.com/modelx"", Target = ""_blank"" },
            new() { Text = ""Model Y"", Url = ""https://www.tesla.com/modely"", Target = ""_blank"" },
        }
    },
};";

    private readonly string example3NavItemRazorCode = @"
<BitLabel>Basic</BitLabel>
<BitNav Items=""FoodNavMenu""
        DefaultSelectedItem=""FoodNavMenu[0].Items[2]""
        Mode=""BitNavMode.Manual"" />

<BitLabel>Two-Way Bind</BitLabel>
<BitNav @bind-SelectedItem=""SelectedItemNav""
        Items=""FoodNavMenu""
        Mode=""BitNavMode.Manual""
        OnSelectItem=""(BitNavItem item) => SelectedItemText = FoodMenuDropdownItems.FirstOrDefault(i => i.Text == item.Text).Text"" />

<BitDropdown @bind-Value=""SelectedItemText""
                Label=""Select Item""
                Items=""FoodMenuDropdownItems""
                OnSelectItem=""(item) => SelectedItemNav = Flatten(FoodNavMenu).FirstOrDefault(i => i.Text == item.Value)"" />";
    private readonly string example3NavItemCsharpCode = @"
private static readonly List<BitNavItem> FoodNavMenu = new()
{
    new()
    {
        Text = ""Fast-Food"",
        IconName = BitIconName.HeartBroken,
        IsExpanded = true,
        ChildItems = new List<BitNavItem>
        {
            new() 
            {
                Text = ""Burgers"",
                ChildItems = new List<BitNavItem>
                {
                    new() { Text = ""Beef Burger"" },
                    new() { Text = ""Veggie Burger"" },
                    new() { Text = ""Bison Burger"" },
                    new() { Text = ""Wild Salmon Burger"" },
                }
            },
            new()
            {
                Text = ""Pizza"",
                ChildItems = new List<BitNavItem>
                {
                    new() { Text = ""Cheese Pizza"" },
                    new() { Text = ""Veggie Pizza"" },
                    new() { Text = ""Pepperoni Pizza"" },
                    new() { Text = ""Meat Pizza"" },
                }
            },
            new() { Text    = ""French Fries"" },
        }
    },
    new()
    {
        Text = ""Fruits"",
        IconName = BitIconName.Health,
        ChildItems = new List<BitNavItem>
        {
            new() { Text = ""Aplle"" },
            new() { Text = ""Orange"" },
            new() { Text = ""Benana"" },
        }
    },
    new() { Text = ""Ice Cream"" },
    new() { Text = ""Cookie"" },
};

private static readonly List<BitDropdownItem> FoodMenuDropdownItems = new()
{
    new()
    {
        Text = ""Beef Burger"",
        Value = ""Beef Burger"",
    },
    new()
    {
        Text = ""Veggie Burger"",
        Value = ""Veggie Burger"",
    },
    new()
    {
        Text = ""Bison Burger"",
        Value = ""Bison Burger"",
    },
    new()
    {
        Text = ""Wild Salmon Burger"",
        Value = ""Wild Salmon Burger"",
    },
    new()
    {
        Text = ""Cheese Pizza"",
        Value = ""Cheese Pizza"",
    },
    new()
    {
        Text = ""Veggie Pizza"",
        Value = ""Veggie Pizza"",
    },
    new()
    {
        Text = ""Pepperoni Pizza"",
        Value = ""Pepperoni Pizza"",
    },
    new()
    {
        Text = ""Meat Pizza"",
        Value = ""Meat Pizza"",
    },
    new()
    {
        Text = ""French Fries"",
        Value = ""French Fries"",
    },
    new()
    {
        Text = ""Aplle"",
        Value = ""Aplle"",
    },
    new()
    {
        Text = ""Orange"",
        Value = ""Orange"",
    },
    new()
    {
        Text = ""Benana"",
        Value = ""Benana"",
    },
    new()
    {
        Text = ""Ice Cream"",
        Value = ""Ice Cream"",
    },
    new()
    {
        Text = ""Cookie"",
        Value = ""Cookie"",
    },
};

private static List<BitNavItem> Flatten(IList<BitNavItem> e) => e.SelectMany(c => Flatten(c.Items)).Concat(e).ToList();
private BitNavItem SelectedItemNav = FoodNavMenu[0].Items[2];
private string SelectedItemText = FoodNavMenu[0].Items[2].Text;";

    private readonly string example4NavItemRazorCode = @"
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

<BitLabel>Header Template (in Grouped mode)</BitLabel>
<BitNav Items=""CarNavMenu"" RenderType=""BitNavRenderType.Grouped"">
    <HeaderTemplate Context=""item"">
        <div class=""nav-custom-header"">
            <BitIcon IconName=""@BitIconName.FavoriteStarFill"" />
            <span>@item.Text</span>
        </div>
    </HeaderTemplate>
</BitNav>

<BitLabel>Item Template</BitLabel>
<BitNav Items=""FoodNavMenu"" Mode=""BitNavMode.Manual"">
    <ItemTemplate Context=""item"">
        <div class=""nav-custom-item"">
            <BitCheckbox />
            <BitIcon IconName=""@item.IconName"" />
            <span>@item.Text</span>
        </div>
    </ItemTemplate>
</BitNav>";
    private readonly string example4NavItemCsharpCode = @"
private static readonly List<BitNavItem> CarNavMenu = new()
{
    new()
    {
        Text = ""Mercedes-Benz"",
        ExpandAriaLabel = ""Mercedes-Benz Expanded"",
        CollapseAriaLabel = ""Mercedes-Benz Collapsed"",
        Title = ""Mercedes-Benz Car Models"",
        IsExpanded = true,
        ChildItems = new List<BitNavItem>
        {
            new()
            {
                Text = ""SUVs"",
                ChildItems = new List<BitNavItem>
                {
                    new() { Text = ""GLA"", Url = ""https://www.mbusa.com/en/vehicles/class/gla/suv"", Target = ""_blank"" },
                    new() { Text = ""GLB"", Url = ""https://www.mbusa.com/en/vehicles/class/glb/suv"", Target = ""_blank"" },
                    new() { Text = ""GLC"", Url = ""https://www.mbusa.com/en/vehicles/class/glc/suv"", Target = ""_blank"" },
                }
            },
            new()
            {
                Text = ""Sedans & Wagons"",
                ChildItems = new List<BitNavItem>
                {
                    new() { Text = ""A Class"", Url = ""https://www.mbusa.com/en/vehicles/class/a-class/sedan"", Target = ""_blank"" },
                    new() { Text = ""C Class"", Url = ""https://www.mbusa.com/en/vehicles/class/c-class/sedan"", Target = ""_blank"" },
                    new() { Text = ""E Class"", Url = ""https://www.mbusa.com/en/vehicles/class/e-class/sedan"", Target = ""_blank"" },
                }
            },
            new()
            {
                Text = ""Coupes"",
                ChildItems = new List<BitNavItem>
                {
                    new() { Text = ""CLA Coupe"", Url = ""https://www.mbusa.com/en/vehicles/class/cla/coupe"", Target = ""_blank"" },
                    new() { Text = ""C Class Coupe"", Url = ""https://www.mbusa.com/en/vehicles/class/c-class/coupe"", Target = ""_blank"" },
                    new() { Text = ""E Class Coupe"", Url = ""https://www.mbusa.com/en/vehicles/class/e-class/coupe"", Target = ""_blank"" },
                }
            },
        }
    },
    new()
    {
        Text = ""Tesla"",
        ExpandAriaLabel = ""Tesla Expanded"",
        CollapseAriaLabel= ""Tesla Collapsed"",
        Title = ""Tesla Car Models"",
        ChildItems = new List<BitNavItem>
        {
            new() { Text = ""Model S"", Url = ""https://www.tesla.com/models"", Target = ""_blank"" },
            new() { Text = ""Model X"", Url = ""https://www.tesla.com/modelx"", Target = ""_blank"" },
            new() { Text = ""Model Y"", Url = ""https://www.tesla.com/modely"", Target = ""_blank"" },
        }
    },
};

private static readonly List<BitNavItem> FoodNavMenu = new()
{
    new()
    {
        Text = ""Fast-Food"",
        IconName = BitIconName.HeartBroken,
        IsExpanded = true,
        ChildItems = new List<BitNavItem>
        {
            new() 
            {
                Text = ""Burgers"",
                ChildItems = new List<BitNavItem>
                {
                    new() { Text = ""Beef Burger"" },
                    new() { Text = ""Veggie Burger"" },
                    new() { Text = ""Bison Burger"" },
                    new() { Text = ""Wild Salmon Burger"" },
                }
            },
            new()
            {
                Text = ""Pizza"",
                ChildItems = new List<BitNavItem>
                {
                    new() { Text = ""Cheese Pizza"" },
                    new() { Text = ""Veggie Pizza"" },
                    new() { Text = ""Pepperoni Pizza"" },
                    new() { Text = ""Meat Pizza"" },
                }
            },
            new() { Text    = ""French Fries"" },
        }
    },
    new()
    {
        Text = ""Fruits"",
        IconName = BitIconName.Health,
        ChildItems = new List<BitNavItem>
        {
            new() { Text = ""Apple"" },
            new() { Text = ""Orange"" },
            new() { Text = ""Banana"" },
        }
    },
    new() { Text = ""Ice Cream"" },
    new() { Text = ""Cookie"" },
};";

    private readonly string example5NavItemRazorCode = @"
<BitNav Items=""FoodNavMenu""
        Mode=""BitNavMode.Manual""
        OnItemClick=""(BitNavItem item) => ClickedItem = item""
        OnSelectItem=""(BitNavItem item) => SelectedItem = item""
        OnItemToggle=""(BitNavItem item) => ToggledItem = item"" />

<span>Clicked Item: @ClickedItem?.Text</span>
<span>Selected Item: @SelectedItem?.Text</span>
<span>Toggled Item: @(ToggledItem is null ? ""N/A"" : $""{ToggledItem.Text} ({(ToggledItem.IsExpanded ? ""Expanded"" : ""Collapsed"")})"")</span>";
    private readonly string example5NavItemCsharpCode = @"
private static readonly List<BitNavItem> FoodNavMenu = new()
{
    new()
    {
        Text = ""Fast-Food"",
        IconName = BitIconName.HeartBroken,
        IsExpanded = true,
        ChildItems = new List<BitNavItem>
        {
            new() 
            {
                Text = ""Burgers"",
                ChildItems = new List<BitNavItem>
                {
                    new() { Text = ""Beef Burger"" },
                    new() { Text = ""Veggie Burger"" },
                    new() { Text = ""Bison Burger"" },
                    new() { Text = ""Wild Salmon Burger"" },
                }
            },
            new()
            {
                Text = ""Pizza"",
                ChildItems = new List<BitNavItem>
                {
                    new() { Text = ""Cheese Pizza"" },
                    new() { Text = ""Veggie Pizza"" },
                    new() { Text = ""Pepperoni Pizza"" },
                    new() { Text = ""Meat Pizza"" },
                }
            },
            new() { Text    = ""French Fries"" },
        }
    },
    new()
    {
        Text = ""Fruits"",
        IconName = BitIconName.Health,
        ChildItems = new List<BitNavItem>
        {
            new() { Text = ""Aplle"" },
            new() { Text = ""Orange"" },
            new() { Text = ""Benana"" },
        }
    },
    new() { Text = ""Ice Cream"" },
    new() { Text = ""Cookie"" },
};

private BitNavItem ClickedItem;
private BitNavItem SelectedItem;
private BitNavItem ToggledItem;";

    private readonly string example6NavItemRazorCode = @"
<BitNav Items=""BitPlatformNavMenu""
        Styles=""@(new() { ItemContainer = ""border: 1px solid green; margin: 2px;"",
                          ToggleButton = ""color: cyan;"",
                          Item = ""color: red;"",
                          ItemIcon = ""color: gold; margin-right: 15px;"" })"" />";
    private readonly string example6NavItemCsharpCode = @"
private static readonly List<BitNavItem> BitPlatformNavMenu = new()
{
    new()
    {
        Text = ""bit platform"",
        ChildItems = new()
        {
            new() { Text = ""Home"", IconName = BitIconName.Home, Url = ""https://bitplatform.dev/"" },
            new()
            {
                Text = ""Products & Services"",
                ChildItems = new()
                {
                    new()
                    {
                        Text = ""Project Templates"",
                        ChildItems = new()
                        {
                            new() { Text = ""Todo sample"", IconName = BitIconName.ToDoLogoOutline, Url = ""https://bitplatform.dev/templates/overview"" },
                            new() { Text = ""AdminPanel sample"", IconName = BitIconName.LocalAdmin, Url = ""https://bitplatform.dev/templates/overview"" },
                        }
                    },
                    new() { Text = ""BlazorUI"", IconName = BitIconName.F12DevTools, Url = ""https://bitplatform.dev/components"" },
                    new() { Text = ""Cloud hosting solutions"", IconName = BitIconName.Cloud, Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                    new() { Text = ""Bit academy"", IconName = BitIconName.LearningTools, Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                }
            },
            new() { Text = ""Pricing"", IconName = BitIconName.Money, Url = ""https://bitplatform.dev/pricing"" },
            new() { Text = ""About"", IconName = BitIconName.Info, Url = ""https://bitplatform.dev/about-us"" },
            new() { Text = ""Contact us"", IconName = BitIconName.Contact, Url = ""https://bitplatform.dev/contact-us"" },
        },
    },
    new()
    {
        Text = ""Community"",
        ChildItems = new()
        {
            new() { Text = ""LinkedIn"", IconName = BitIconName.LinkedInLogo, Url = ""https://www.linkedin.com/company/bitplatformhq"" },
            new() { Text = ""Twitter"", IconName = BitIconName.Globe, Url = ""https://twitter.com/bitplatformhq"" },
            new() { Text = ""GitHub repo"", IconName = BitIconName.GitGraph, Url = ""https://github.com/bitfoundation/bitplatform"" },
        }
    },
    new() { Text = ""Iconography"", IconName = BitIconName.AppIconDefault, Url = ""/iconography"" },
};";

    private readonly string example7NavItemRazorCode = @"
<BitNav Dir=""BitDir.Rtl"" Items=""RtlBitPlatformNavMenu"" />";
    private readonly string example7NavItemCsharpCode = @"
private static readonly List<BitNavItem> RtlBitPlatformNavMenu = new()
{
    new()
    {
        Text = ""پلتفرمِ بیت"",
        Description = ""توضیحاتِ پلتفرمِ بیت"",
        ChildItems = new()
        {
            new() { Text = ""خانه"", IconName = BitIconName.Home, Url = ""https://bitplatform.dev/"" },
            new()
            {
                Text = ""محصولات و خدمات"",
                ChildItems = new()
                {
                    new()
                    {
                        Text = ""قالب های پروژه"",
                        ChildItems = new()
                        {
                            new() { Text = ""نمونه ی Todo"", IconName = BitIconName.ToDoLogoOutline, Url = ""https://bitplatform.dev/templates/overview"" },
                            new() { Text = ""نمونه ی AdminPanel"", IconName = BitIconName.LocalAdmin, Url = ""https://bitplatform.dev/templates/overview"" },
                        }
                    },
                    new() { Text = ""رابط کاربری Blazor"", IconName = BitIconName.F12DevTools, Url = ""https://blazorui.bitplatform.dev/"" },
                    new() { Text = ""راه های هاست ابری"", IconName = BitIconName.Cloud, Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                    new() { Text = ""آکادمی بیت"", IconName = BitIconName.LearningTools, Url = ""https://bitplatform.dev/#"", IsEnabled = false },
                }
            },
            new() { Text = ""قیمت"", IconName = BitIconName.Money, Url = ""https://bitplatform.dev/pricing"" },
            new() { Text = ""درباره ما"", IconName = BitIconName.Info, Url = ""https://bitplatform.dev/about-us"" },
            new() { Text = ""ارتباط با ما"", IconName = BitIconName.Contact, Url = ""https://bitplatform.dev/contact-us"" },
        },
    },
    new()
    {
        Text = ""انجمن ها"",
        ChildItems = new()
        {
            new() { Text = ""لینکدین"", IconName = BitIconName.LinkedInLogo , Url = ""https://www.linkedin.com/company/bitplatformhq"" },
            new() { Text = ""توییتر"", IconName = BitIconName.Globe , Url = ""https://twitter.com/bitplatformhq"" },
            new() { Text = ""گیتهاب"", IconName = BitIconName.GitGraph , Url = ""https://github.com/bitfoundation/bitplatform"" },
        }
    },
    new() { Text = ""شمایل نگاری"", IconName = BitIconName.AppIconDefault, Url = ""/iconography"" },
};";
}

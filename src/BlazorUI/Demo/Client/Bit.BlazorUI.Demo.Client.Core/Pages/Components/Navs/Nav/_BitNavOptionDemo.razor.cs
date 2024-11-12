namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Nav;

public partial class _BitNavOptionDemo
{
    private bool iconOnly;

    private BitNavOption ClickedOption = default!;
    private BitNavOption ToggledOption = default!;
    private BitNavOption SelectedOption = default!;

    private string? SelectedOptionKey = "French Fries";
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
<BitNav TItem=""BitNavOption"">
    <BitNavOption Text=""bit platform""
                  ExpandAriaLabel=""bit platform Expanded""
                  CollapseAriaLabel=""bit platform Collapsed"">
        <BitNavOption Text=""Home"" IconName=""@BitIconName.Home"" Url=""https://bitplatform.dev/"" Target=""_blank"" />
        <BitNavOption Text=""Products & Services"">
            <BitNavOption Text=""Project Templates"">
                <BitNavOption Text=""Todo sample"" IconName=""@BitIconName.ToDoLogoOutline"" Url=""https://bitplatform.dev/templates/overview"" Target=""_blank"" />
                <BitNavOption Text=""AdminPanel sample"" IconName=""@BitIconName.LocalAdmin"" Url=""https://bitplatform.dev/templates/overview"" Target=""_blank"" />
            </BitNavOption>
            <BitNavOption Text=""BlazorUI"" IconName=""@BitIconName.F12DevTools"" Url=""https://bitplatform.dev/components"" Target=""_blank"" />
            <BitNavOption Text=""Cloud hosting solutions"" IconName=""@BitIconName.Cloud"" IsEnabled=""false"" />
            <BitNavOption Text=""Bit academy"" IconName=""@BitIconName.LearningTools"" IsEnabled=""false"" />
        </BitNavOption>
        <BitNavOption Text=""Pricing"" IconName=""@BitIconName.Money"" Url=""https://bitplatform.dev/pricing"" Target=""_blank"" />
        <BitNavOption Text=""About"" IconName=""@BitIconName.Info"" Url=""https://bitplatform.dev/about-us"" Target=""_blank"" />
        <BitNavOption Text=""Contact us"" IconName=""@BitIconName.Contact"" Url=""https://bitplatform.dev/contact-us"" Target=""_blank"" />
    </BitNavOption>
    <BitNavOption Text=""Community""
                  ExpandAriaLabel=""Community Expanded""
                  CollapseAriaLabel=""Community Collapsed"">
        <BitNavOption Text=""Linkedin"" IconName=""@BitIconName.LinkedInLogo"" Url=""https://www.linkedin.com/company/bitplatformhq"" Target=""_blank"" />
        <BitNavOption Text=""Twitter"" IconName=""@BitIconName.Globe"" Url=""https://twitter.com/bitplatformhq"" Target=""_blank"" />
        <BitNavOption Text=""Github repo"" IconName=""@BitIconName.GitGraph"" Url=""https://github.com/bitfoundation/bitplatform"" Target=""_blank"" />
    </BitNavOption>
    <BitNavOption Text=""Iconography"" IconName=""@BitIconName.AppIconDefault"" Url=""/iconography"" Target=""_blank"" />
</BitNav>";

    private readonly string example2RazorCode = @"
<BitNav TItem=""BitNavOption"" FitWidth>
    <BitNavOption Text=""bit platform""
                  ExpandAriaLabel=""bit platform Expanded""
                  CollapseAriaLabel=""bit platform Collapsed"">
        <BitNavOption Text=""Home"" IconName=""@BitIconName.Home"" Url=""https://bitplatform.dev/"" Target=""_blank"" />
        <BitNavOption Text=""Products & Services"">
            <BitNavOption Text=""Project Templates"">
                <BitNavOption Text=""Todo sample"" IconName=""@BitIconName.ToDoLogoOutline"" Url=""https://bitplatform.dev/templates/overview"" Target=""_blank"" />
                <BitNavOption Text=""AdminPanel sample"" IconName=""@BitIconName.LocalAdmin"" Url=""https://bitplatform.dev/templates/overview"" Target=""_blank"" />
            </BitNavOption>
            <BitNavOption Text=""BlazorUI"" IconName=""@BitIconName.F12DevTools"" Url=""https://bitplatform.dev/components"" Target=""_blank"" />
            <BitNavOption Text=""Cloud hosting solutions"" IconName=""@BitIconName.Cloud"" IsEnabled=""false"" />
            <BitNavOption Text=""Bit academy"" IconName=""@BitIconName.LearningTools"" IsEnabled=""false"" />
        </BitNavOption>
        <BitNavOption Text=""Pricing"" IconName=""@BitIconName.Money"" Url=""https://bitplatform.dev/pricing"" Target=""_blank"" />
        <BitNavOption Text=""About"" IconName=""@BitIconName.Info"" Url=""https://bitplatform.dev/about-us"" Target=""_blank"" />
        <BitNavOption Text=""Contact us"" IconName=""@BitIconName.Contact"" Url=""https://bitplatform.dev/contact-us"" Target=""_blank"" />
    </BitNavOption>
    <BitNavOption Text=""Community""
                  ExpandAriaLabel=""Community Expanded""
                  CollapseAriaLabel=""Community Collapsed"">
        <BitNavOption Text=""Linkedin"" IconName=""@BitIconName.LinkedInLogo"" Url=""https://www.linkedin.com/company/bitplatformhq"" Target=""_blank"" />
        <BitNavOption Text=""Twitter"" IconName=""@BitIconName.Globe"" Url=""https://twitter.com/bitplatformhq"" Target=""_blank"" />
        <BitNavOption Text=""Github repo"" IconName=""@BitIconName.GitGraph"" Url=""https://github.com/bitfoundation/bitplatform"" Target=""_blank"" />
    </BitNavOption>
    <BitNavOption Text=""Iconography"" IconName=""@BitIconName.AppIconDefault"" Url=""/iconography"" Target=""_blank"" />
</BitNav>";

    private readonly string example3RazorCode = @"
< BitNav TItem=""BitNavOption"" RenderType=""BitNavRenderType.Grouped"">
    <BitNavOption Text=""Mercedes-Benz""
                  ExpandAriaLabel=""Mercedes-Benz Expanded""
                  CollapseAriaLabel=""Mercedes-Benz Collapsed""
                  Title=""Mercedes-Benz Car Models""
                  IsExpanded=""true"">
        <BitNavOption Text=""SUVs"">
            <BitNavOption Text=""GLA"" Url=""https://www.mbusa.com/en/vehicles/class/gla/suv"" Target=""_blank"" />
            <BitNavOption Text=""GLB"" Url=""https://www.mbusa.com/en/vehicles/class/glb/suv"" Target=""_blank"" />
            <BitNavOption Text=""GLC"" Url=""https://www.mbusa.com/en/vehicles/class/glc/suv"" Target=""_blank"" />
        </BitNavOption>
        <BitNavOption Text=""Sedans & Wagons"">
            <BitNavOption Text=""A Class"" Url=""https://www.mbusa.com/en/vehicles/class/a-class/sedan"" Target=""_blank"" />
            <BitNavOption Text=""C Class"" Url=""https://www.mbusa.com/en/vehicles/class/c-class/sedan"" Target=""_blank"" />
            <BitNavOption Text=""E Class"" Url=""https://www.mbusa.com/en/vehicles/class/e-class/sedan"" Target=""_blank"" />
        </BitNavOption>
        <BitNavOption Text=""Coupes"">
            <BitNavOption Text=""CLA Coupe"" Url=""https://www.mbusa.com/en/vehicles/class/cla/coupe"" Target=""_blank"" />
            <BitNavOption Text=""C Class Coupe"" Url=""https://www.mbusa.com/en/vehicles/class/c-class/coupe"" Target=""_blank"" />
            <BitNavOption Text=""E Class Coupe"" Url=""https://www.mbusa.com/en/vehicles/class/e-class/coupe"" Target=""_blank"" />
        </BitNavOption>
    </BitNavOption>
    <BitNavOption Text=""Tesla""
                  ExpandAriaLabel=""Tesla Expanded""
                  CollapseAriaLabel=""Tesla Collapsed""
                  Title=""Tesla Car Models"">
        <BitNavOption Text=""Model S"" Url=""https://www.tesla.com/models"" Target=""_blank"" />
        <BitNavOption Text=""Model X"" Url=""https://www.tesla.com/modelx"" Target=""_blank"" />
        <BitNavOption Text=""Model Y"" Url=""https://www.tesla.com/modely"" Target=""_blank"" />
    </BitNavOption>
</BitNav>";

    private readonly string example4RazorCode = @"
<BitNav TItem=""BitNavOption"" Mode=""BitNavMode.Manual"">
    <BitNavOption Text=""Fast foods"" Description=""List of fast foods""
                  IconName=""@BitIconName.HeartBroken"" IsExpanded=""true"">
        <BitNavOption Text=""Burgers"" Description=""List of burgers"">
            <BitNavOption Text=""Beef Burger"" Key=""Beef Burger"" />
            <BitNavOption Text=""Veggie Burger"" Key=""Veggie Burger"" />
            <BitNavOption Text=""Bison Burger"" Key=""Bison Burger"" />
            <BitNavOption Text=""Wild Salmon Burger"" Key=""Wild Salmon Burger"" />
        </BitNavOption>
        <BitNavOption Text=""Pizza"">
            <BitNavOption Text=""Cheese Pizza"" Key=""Cheese Pizza"" />
            <BitNavOption Text=""Veggie Pizza"" Key=""Veggie Pizza"" />
            <BitNavOption Text=""Pepperoni Pizza"" Key=""Pepperoni Pizza"" />
            <BitNavOption Text=""Meat Pizza"" Key=""Meat Pizza"" />
        </BitNavOption>
        <BitNavOption Text=""French Fries"" Key=""French Fries"" />
    </BitNavOption>
    <BitNavOption Text=""Fruits"" IconName=""@BitIconName.Health"">
        <BitNavOption Text=""Apple"" Key=""Apple"" />
        <BitNavOption Text=""Orange"" Key=""Orange"" />
        <BitNavOption Text=""Benana"" Key=""Benana"" />
    </BitNavOption>
    <BitNavOption Text=""Ice Cream"" Key=""Ice Cream"" />
    <BitNavOption Text=""Cookie"" Key=""Cookie"" />
</BitNav>

<BitNav Mode=""BitNavMode.Manual""
        OnSelectItem=""(BitNavOption option) => SelectedOptionKey = option.Key"">
    <BitNavOption Text=""Fast foods"" Description=""List of fast foods""
                  IconName=""@BitIconName.HeartBroken"" IsExpanded=""true"">
        <BitNavOption Text=""Burgers"" Description=""List of burgers"">
            <BitNavOption Text=""Beef Burger"" Key=""Beef Burger"" />
            <BitNavOption Text=""Veggie Burger"" Key=""Veggie Burger"" />
            <BitNavOption Text=""Bison Burger"" Key=""Bison Burger"" />
            <BitNavOption Text=""Wild Salmon Burger"" Key=""Wild Salmon Burger"" />
        </BitNavOption>
        <BitNavOption Text=""Pizza"">
            <BitNavOption Text=""Cheese Pizza"" Key=""Cheese Pizza"" />
            <BitNavOption Text=""Veggie Pizza"" Key=""Veggie Pizza"" />
            <BitNavOption Text=""Pepperoni Pizza"" Key=""Pepperoni Pizza"" />
            <BitNavOption Text=""Meat Pizza"" Key=""Meat Pizza"" />
        </BitNavOption>
        <BitNavOption Text=""French Fries"" Key=""French Fries"" />
    </BitNavOption>
    <BitNavOption Text=""Fruits"" IconName=""@BitIconName.Health"">
        <BitNavOption Text=""Aplle"" Key=""Aplle"" />
        <BitNavOption Text=""Orange"" Key=""Orange"" />
        <BitNavOption Text=""Benana"" Key=""Benana"" />
    </BitNavOption>
    <BitNavOption Text=""Ice Cream"" Key=""Ice Cream"" />
    <BitNavOption Text=""Cookie"" Key=""Cookie"" />
</BitNav>

<BitDropdown @bind-Value=""SelectedOptionKey""
             FitWidth
             Label=""Select Item""
             Items=""FoodMenuDropdownItems"" />";
    private readonly string example4CsharpCode = @"
private string SelectedOptionKey;

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

    private readonly string example5RazorCode = @"
<BitToggle @bind-Value=""iconOnly"" Label=""Hide texts?"" Inline />
<BitNav TItem=""BitNavOption"" Mode=""BitNavMode.Manual"" IconOnly=""iconOnly"">
    <BitNavOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavOption Text=""AdminPanel sample"" IconName=""@BitIconName.LocalAdmin"">
        <BitNavOption Text=""Dashboard"" IconName=""@BitIconName.ViewDashboard"" />
        <BitNavOption Text=""Categories"" IconName=""@BitIconName.BuildQueue"" />
        <BitNavOption Text=""Products"" IconName=""@BitIconName.Product"" />
    </BitNavOption>
    <BitNavOption Text=""Todo sample"" IconName=""@BitIconName.ToDoLogoOutline"" />
    <BitNavOption Text=""BlazorUI"" IconName=""@BitIconName.F12DevTools"" />
    <BitNavOption Text=""Bit academy"" IconName=""@BitIconName.LearningTools"" IsEnabled=""false"" />
    <BitNavOption Text=""Contact us"" IconName=""@BitIconName.Contact"" />
</BitNav>";
    private readonly string example5CsharpCode = @"
private bool iconOnly;";

    private readonly string example6RazorCode = @"
<BitNav TItem=""BitNavOption"" RenderType=""BitNavRenderType.Grouped"">
    <HeaderTemplate Context=""item"">
        <div class=""nav-custom-header"">
            <BitIcon IconName=""@BitIconName.FavoriteStarFill"" />
            <span>@item.Text</span>
        </div>
    </HeaderTemplate>
    <ChildContent>
        <BitNavOption Text=""Mercedes-Benz""
                      ExpandAriaLabel=""Mercedes-Benz Expanded""
                      CollapseAriaLabel=""Mercedes-Benz Collapsed""
                      Title=""Mercedes-Benz Car Models""
                      IsExpanded=""true"">
            <BitNavOption Text=""SUVs"">
                <BitNavOption Text=""GLA"" Url=""https://www.mbusa.com/en/vehicles/class/gla/suv"" Target=""_blank"" />
                <BitNavOption Text=""GLB"" Url=""https://www.mbusa.com/en/vehicles/class/glb/suv"" Target=""_blank"" />
                <BitNavOption Text=""GLC"" Url=""https://www.mbusa.com/en/vehicles/class/glc/suv"" Target=""_blank"" />
            </BitNavOption>
            <BitNavOption Text=""Sedans & Wagons"">
                <BitNavOption Text=""A Class"" Url=""https://www.mbusa.com/en/vehicles/class/a-class/sedan"" Target=""_blank"" />
                <BitNavOption Text=""C Class"" Url=""https://www.mbusa.com/en/vehicles/class/c-class/sedan"" Target=""_blank"" />
                <BitNavOption Text=""E Class"" Url=""https://www.mbusa.com/en/vehicles/class/e-class/sedan"" Target=""_blank"" />
            </BitNavOption>
            <BitNavOption Text=""Coupes"">
                <BitNavOption Text=""CLA Coupe"" Url=""https://www.mbusa.com/en/vehicles/class/cla/coupe"" Target=""_blank"" />
                <BitNavOption Text=""C Class Coupe"" Url=""https://www.mbusa.com/en/vehicles/class/c-class/coupe"" Target=""_blank"" />
                <BitNavOption Text=""E Class Coupe"" Url=""https://www.mbusa.com/en/vehicles/class/e-class/coupe"" Target=""_blank"" />
            </BitNavOption>
        </BitNavOption>
        <BitNavOption Text=""Tesla""
                      ExpandAriaLabel=""Tesla Expanded""
                      CollapseAriaLabel=""Tesla Collapsed""
                      Title=""Tesla Car Models"">
            <BitNavOption Text=""Model S"" Url=""https://www.tesla.com/models"" Target=""_blank"" />
            <BitNavOption Text=""Model X"" Url=""https://www.tesla.com/modelx"" Target=""_blank"" />
            <BitNavOption Text=""Model Y"" Url=""https://www.tesla.com/modely"" Target=""_blank"" />
        </BitNavOption>
    </ChildContent>
</BitNav>

<BitNav TItem=""BitNavOption"" Mode=""BitNavMode.Manual"">
    <ItemTemplate Context=""option"">
        <div class=""nav-custom-item"">
            <BitCheckbox />
            <BitIcon IconName=""@option.IconName"" />
            <span>@option.Text</span>
        </div>
    </ItemTemplate>
    <ChildContent>
        <BitNavOption Text=""Fast foods"" Description=""List of fast foods""
                      IconName=""@BitIconName.HeartBroken"" IsExpanded=""true"">
            <BitNavOption Text=""Burgers"" Description=""List of burgers"">
                <BitNavOption Text=""Beef Burger"" />
                <BitNavOption Text=""Veggie Burger"" />
                <BitNavOption Text=""Bison Burger"" />
                <BitNavOption Text=""Wild Salmon Burger"" />
            </BitNavOption>
            <BitNavOption Text=""Pizza"">
                <BitNavOption Text=""Cheese Pizza"" />
                <BitNavOption Text=""Veggie Pizza"" />
                <BitNavOption Text=""Pepperoni Pizza"" />
                <BitNavOption Text=""Meat Pizza"" />
            </BitNavOption>
            <BitNavOption Text=""French Fries"" />
        </BitNavOption>
        <BitNavOption Text=""Fruits"" IconName=""@BitIconName.Health"">
            <BitNavOption Text=""Aplle"" />
            <BitNavOption Text=""Orange"" />
            <BitNavOption Text=""Benana"" />
        </BitNavOption>
        <BitNavOption Text=""Ice Cream"" />
        <BitNavOption Text=""Cookie"" />
    </ChildContent>
</BitNav>";

    private readonly string example7RazorCode = @"
<BitNav Mode=""BitNavMode.Manual""
        OnItemClick=""(BitNavOption option) => ClickedOption = option""
        OnSelectItem=""(BitNavOption option) => SelectedOption = option""
        OnItemToggle=""(BitNavOption option) => ToggledOption = option"">
    <BitNavOption Text=""Fast foods"" Description=""List of fast foods""
                  IconName=""@BitIconName.HeartBroken"" IsExpanded=""true"">
        <BitNavOption Text=""Burgers"" Description=""List of burgers"">
            <BitNavOption Text=""Beef Burger"" Key=""Beef Burger"" />
            <BitNavOption Text=""Veggie Burger"" Key=""Veggie Burger"" />
            <BitNavOption Text=""Bison Burger"" Key=""Bison Burger"" />
            <BitNavOption Text=""Wild Salmon Burger"" Key=""Wild Salmon Burger"" />
        </BitNavOption>
        <BitNavOption Text=""Pizza"">
            <BitNavOption Text=""Cheese Pizza"" Key=""Cheese Pizza"" />
            <BitNavOption Text=""Veggie Pizza"" Key=""Veggie Pizza"" />
            <BitNavOption Text=""Pepperoni Pizza"" Key=""Pepperoni Pizza"" />
            <BitNavOption Text=""Meat Pizza"" Key=""Meat Pizza"" />
        </BitNavOption>
        <BitNavOption Text=""French Fries"" Key=""French Fries"" />
    </BitNavOption>
    <BitNavOption Text=""Fruits""  IconName=""@BitIconName.Health"">
        <BitNavOption Text=""Aplle"" Key=""Aplle"" />
        <BitNavOption Text=""Orange"" Key=""Orange"" />
        <BitNavOption Text=""Benana"" Key=""Benana"" />
    </BitNavOption>
    <BitNavOption Text=""Ice Cream"" Key=""Ice Cream"" />
    <BitNavOption Text=""Cookie"" Key=""Cookie"" />
</BitNav>

<span>Clicked Option: @ClickedOption?.Text</span>
<span>Selected Option: @SelectedOption?.Text</span>
<span>Toggled Option: @(ToggledOption is null ? ""N/A"" : $""{ToggledOption.Text} ({(ToggledOption.IsExpanded ? ""Expanded"" : ""Collapsed"")})"")</span>";
    private readonly string example7CsharpCode = @"
private BitNavOption ClickedOption;
private BitNavOption SelectedOption;
private BitNavOption ToggledOption;";

    private readonly string example8RazorCode = @"
<BitNav TItem=""BitNavOption""
        Styles=""@(new() { ItemContainer = ""border: 1px solid green; margin: 2px;"",
                          ToggleButton = ""color: cyan;"",
                          Item = ""color: red;"",
                          ItemIcon = ""color: gold; margin-right: 15px;"" })"">
    <BitNavOption Text=""bit platform""
                  ExpandAriaLabel=""bit platform Expanded""
                  CollapseAriaLabel=""bit platform Collapsed"">
        <BitNavOption Text=""Home"" IconName=""@BitIconName.Home"" Url=""https://bitplatform.dev/"" Target=""_blank"" />
        <BitNavOption Text=""Products & Services"">
            <BitNavOption Text=""Project Templates"">
                <BitNavOption IconName=""@BitIconName.ToDoLogoTop"" Text=""Todo sample"" Url=""https://bitplatform.dev/templates/overview"" Target=""_blank"" />
                <BitNavOption IconName=""@BitIconName.Admin"" Text=""AdminPanel sample"" Url=""https://bitplatform.dev/templates/overview"" Target=""_blank"" />
            </BitNavOption>
            <BitNavOption Text=""BlazorUI"" IconName=""@BitIconName.F12DevTools"" Url=""https://bitplatform.dev/components"" Target=""_blank"" />
            <BitNavOption Text=""Cloud hosting solutions"" IconName=""@BitIconName.Cloud"" IsEnabled=""false"" />
            <BitNavOption Text=""Bit academy"" IconName=""@BitIconName.LearningTools"" IsEnabled=""false"" />
        </BitNavOption>
        <BitNavOption Text=""Pricing"" IconName=""@BitIconName.Money"" Url=""https://bitplatform.dev/pricing"" Target=""_blank"" />
        <BitNavOption Text=""About"" IconName=""@BitIconName.Info"" Url=""https://bitplatform.dev/about-us"" Target=""_blank"" />
        <BitNavOption Text=""Contact us"" IconName=""@BitIconName.Contact"" Url=""https://bitplatform.dev/contact-us"" Target=""_blank"" />
    </BitNavOption>
    <BitNavOption Text=""Community""
                  ExpandAriaLabel=""Community Expanded""
                  CollapseAriaLabel=""Community Collapsed"">
        <BitNavOption Text=""Linkedin"" IconName=""@BitIconName.LinkedInLogo"" Url=""https://www.linkedin.com/company/bitplatformhq"" Target=""_blank"" />
        <BitNavOption Text=""Twitter"" IconName=""@BitIconName.Globe"" Url=""https://twitter.com/bitplatformhq"" Target=""_blank"" />
        <BitNavOption Text=""Github repo"" IconName=""@BitIconName.GitGraph"" Url=""https://github.com/bitfoundation/bitplatform"" Target=""_blank"" />
    </BitNavOption>
    <BitNavOption Text=""Iconography"" IconName=""@BitIconName.AppIconDefault"" Url=""/iconography"" Target=""_blank"" />
</BitNav>";

    private readonly string example9RazorCode = @"
<BitNav Dir=""BitDir.Rtl"" TItem=""BitNavOption"">
    <BitNavOption Text=""پلتفرمِ بیت"" Description=""توضیحاتِ پلتفرمِ بیت"">
        <BitNavOption Text=""خانه"" IconName=""@BitIconName.Home"" Url=""https://bitplatform.dev/"" Target=""_blank"" />
        <BitNavOption Text=""محصولات و خدمات"">
            <BitNavOption Text=""قالب های پروژه"">
                <BitNavOption IconName=""@BitIconName.ToDoLogoOutline"" Text=""نمونه ی Todo"" Url=""https://bitplatform.dev/templates/overview"" Target=""_blank"" />
                <BitNavOption IconName=""@BitIconName.LocalAdmin"" Text=""نمونه ی AdminPanel"" Url=""https://bitplatform.dev/templates/overview"" Target=""_blank"" />
            </BitNavOption>
            <BitNavOption Text=""رابط کاربری Blazor"" IconName=""@BitIconName.F12DevTools"" Url=""https://blazorui.bitplatform.dev/"" Target=""_blank"" />
            <BitNavOption Text=""راه های هاست ابری"" IconName=""@BitIconName.Cloud"" IsEnabled=""false"" />
            <BitNavOption Text=""آکادمی بیت"" IconName=""@BitIconName.LearningTools"" IsEnabled=""false"" />
        </BitNavOption>
        <BitNavOption Text=""قیمت"" IconName=""@BitIconName.Money"" Url=""https://bitplatform.dev/pricing"" Target=""_blank"" />
        <BitNavOption Text=""درباره ما"" IconName=""@BitIconName.Info"" Url=""https://bitplatform.dev/about-us"" Target=""_blank"" />
        <BitNavOption Text=""ارتباط با ما"" IconName=""@BitIconName.Contact"" Url=""https://bitplatform.dev/contact-us"" Target=""_blank"" />
    </BitNavOption>
    <BitNavOption Text=""انجمن ها"">
        <BitNavOption Text=""لینکدین"" IconName=""@BitIconName.LinkedInLogo"" Url=""https://www.linkedin.com/company/bitplatformhq"" Target=""_blank"" />
        <BitNavOption Text=""توییتر"" IconName=""@BitIconName.Globe"" Url=""https://twitter.com/bitplatformhq"" Target=""_blank"" />
        <BitNavOption Text=""گیتهاب"" IconName=""@BitIconName.GitGraph"" Url=""https://github.com/bitfoundation/bitplatform"" Target=""_blank"" />
    </BitNavOption>
    <BitNavOption Text=""شمایل نگاری"" IconName=""@BitIconName.AppIconDefault"" Url=""/iconography"" Target=""_blank"" />
</BitNav>";
}

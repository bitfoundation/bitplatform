namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Nav;

public partial class _BitNavOptionDemo
{
    private readonly string example1RazorCode = @"
<BitNav TItem=""BitNavOption"">
    <BitNavOption Text=""bit platform"" Description=""the bit platform description"">
        <BitNavOption Text=""Home"" IconName=""@BitIconName.Home"" Url=""https://bitplatform.dev/"" Target=""_blank"" />
        <BitNavOption Text=""Products & Services"">
            <BitNavOption Text=""Project Templates"">
                <BitNavOption Text=""Todo sample"" IconName=""@BitIconName.ToDoLogoOutline"" Url=""https://bitplatform.dev/templates/overview"" Target=""_blank"" />
                <BitNavOption Text=""AdminPanel sample"" IconName=""@BitIconName.LocalAdmin"" Url=""https://bitplatform.dev/templates/overview"" Target=""_blank"" />
            </BitNavOption>
            <BitNavOption Text=""BlazorUI"" IconName=""@BitIconName.F12DevTools"" Url=""https://bitplatform.dev/components"" Target=""_blank"" />
            <BitNavOption Text=""Cloud hosting solutions"" IconName=""@BitIconName.Cloud"" Url=""https://bitplatform.dev/#"" IsEnabled=""false"" />
            <BitNavOption Text=""Bit academy"" IconName=""@BitIconName.LearningTools"" Url=""https://bitplatform.dev/#"" IsEnabled=""false"" />
        </BitNavOption>
        <BitNavOption Text=""Pricing"" IconName=""@BitIconName.Money"" Url=""https://bitplatform.dev/pricing"" Target=""_blank"" />
        <BitNavOption Text=""About"" IconName=""@BitIconName.Info"" Url=""https://bitplatform.dev/about-us"" Target=""_blank"" />
        <BitNavOption Text=""Contact us"" IconName=""@BitIconName.Contact"" Url=""https://bitplatform.dev/contact-us"" Target=""_blank"" />
    </BitNavOption>
    <BitNavOption Text=""Community"">
        <BitNavOption Text=""LinkedIn"" IconName=""@BitIconName.LinkedInLogo"" Url=""https://www.linkedin.com/company/bitplatformhq"" Target=""_blank"" />
        <BitNavOption Text=""Twitter"" IconName=""@BitIconName.Globe"" Url=""https://twitter.com/bitplatformhq"" Target=""_blank"" />
        <BitNavOption Text=""GitHub repo"" IconName=""@BitIconName.GitGraph"" Url=""https://github.com/bitfoundation/bitplatform"" Target=""_blank"" />
    </BitNavOption>
    <BitNavOption Text=""Iconography"" IconName=""@BitIconName.AppIconDefault"" Url=""/iconography"" />
</BitNav>";

    private readonly string example2RazorCode = @"
<BitNav TItem=""BitNavOption"" FitWidth>
    <BitNavOption Text=""bit platform"" Description=""the bit platform description"">
        <BitNavOption Text=""Home"" IconName=""@BitIconName.Home"" Url=""https://bitplatform.dev/"" Target=""_blank"" />
        <BitNavOption Text=""BlazorUI"" IconName=""@BitIconName.F12DevTools"" Url=""https://bitplatform.dev/components"" Target=""_blank"" />
    </BitNavOption>
    <BitNavOption Text=""Iconography"" IconName=""@BitIconName.AppIconDefault"" Url=""/iconography"" />
</BitNav>

<BitNav TItem=""BitNavOption"" FullWidth>
    <BitNavOption Text=""bit platform"" Description=""the bit platform description"">
        <BitNavOption Text=""Home"" IconName=""@BitIconName.Home"" Url=""https://bitplatform.dev/"" Target=""_blank"" />
        <BitNavOption Text=""BlazorUI"" IconName=""@BitIconName.F12DevTools"" Url=""https://bitplatform.dev/components"" Target=""_blank"" />
    </BitNavOption>
    <BitNavOption Text=""Iconography"" IconName=""@BitIconName.AppIconDefault"" Url=""/iconography"" />
</BitNav>";

    private readonly string example3RazorCode = @"
<BitNav TItem=""BitNavOption"" RenderType=""BitNavRenderType.Grouped"">
    <BitNavOption Text=""Mercedes-Benz""
                  Title=""Mercedes-Benz Car Models""
                  Description=""Cars manufactured under the brand of Mercedes-Benz""
                  ExpandAriaLabel=""Mercedes-Benz Expanded""
                  CollapseAriaLabel=""Mercedes-Benz Collapsed""
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
    </BitNavOption>
    <BitNavOption Text=""Tesla""
                  Title=""Tesla Car Models""
                  ExpandAriaLabel=""Tesla Expanded""
                  CollapseAriaLabel=""Tesla Collapsed"">
        <BitNavOption Text=""Model S"" Url=""https://www.tesla.com/models"" Target=""_blank"" />
        <BitNavOption Text=""Model X"" Url=""https://www.tesla.com/modelx"" Target=""_blank"" />
        <BitNavOption Text=""Model Y"" Url=""https://www.tesla.com/modely"" Target=""_blank"" />
    </BitNavOption>
</BitNav>";

    private readonly string example4RazorCode = @"
<BitNav TItem=""BitNavOption"" FitWidth>
    <BitNavOption Text=""Home"" IconName=""@BitIconName.Home"" Url=""https://bitplatform.dev/"" Target=""_blank"" />
    <BitNavOption Text=""Pricing"" IconName=""@BitIconName.Money"" Url=""https://bitplatform.dev/pricing"" Target=""_blank"" />
    <BitNavOption IsSeparator=""true"" />
    <BitNavOption Text=""LinkedIn"" IconName=""@BitIconName.LinkedInLogo"" Url=""https://www.linkedin.com/company/bitplatformhq"" Target=""_blank"" />
    <BitNavOption Text=""GitHub repo"" IconName=""@BitIconName.GitGraph"" Url=""https://github.com/bitfoundation/bitplatform"" Target=""_blank"" />
    <BitNavOption IsSeparator=""true"" />
    <BitNavOption Text=""Contact us"" IconName=""@BitIconName.Contact"" Url=""https://bitplatform.dev/contact-us"" Target=""_blank"" />
</BitNav>";

    private readonly string example5RazorCode = @"
<BitNav TItem=""BitNavOption"" Mode=""BitNavMode.Manual"">
    <BitNavOption Text=""Fast foods"" Description=""List of fast foods""
                  IconName=""@BitIconName.HeartBroken"" IsExpanded=""true"">
        <BitNavOption Text=""Burgers"" Description=""List of burgers"">
            <BitNavOption Text=""Beef Burger"" Key=""Beef Burger"" />
            <BitNavOption Text=""Veggie Burger"" Key=""Veggie Burger"" />
        </BitNavOption>
        <BitNavOption Text=""Pizza"">
            <BitNavOption Text=""Cheese Pizza"" Key=""Cheese Pizza"" />
            <BitNavOption Text=""Meat Pizza"" Key=""Meat Pizza"" />
        </BitNavOption>
        <BitNavOption Text=""French Fries"" Key=""French Fries"" />
    </BitNavOption>
    <BitNavOption Text=""Fruits"" IconName=""@BitIconName.Health"">
        <BitNavOption Text=""Apple"" Key=""Apple"" />
        <BitNavOption Text=""Orange"" Key=""Orange"" />
        <BitNavOption Text=""Banana"" Key=""Banana"" />
    </BitNavOption>
    <BitNavOption Text=""Ice Cream"" Key=""Ice Cream"" />
    <BitNavOption Text=""Cookie"" Key=""Cookie"" />
</BitNav>

<BitNav Mode=""BitNavMode.Manual""
        OnSelectItem=""(BitNavOption option) => SelectedOptionKey = option.Key"">
    @* ... the same options ... *@
</BitNav>

<BitDropdown @bind-Value=""SelectedOptionKey""
             FitWidth
             Label=""Selected Item""
             Items=""FoodMenuDropdownItems"" />";
    private readonly string example5CsharpCode = @"
private string? SelectedOptionKey = ""French Fries"";

private static readonly List<BitDropdownItem<string>> FoodMenuDropdownItems =
[
    new() { Text = ""Beef Burger"", Value = ""Beef Burger"" },
    new() { Text = ""Veggie Burger"", Value = ""Veggie Burger"" },
    new() { Text = ""Cheese Pizza"", Value = ""Cheese Pizza"" },
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
    private readonly string example6CsharpCode = @"
private bool iconOnly;";

    private readonly string example7RazorCode = @"
<BitNav TItem=""BitNavOption"" SingleExpand FitWidth>
    <BitNavOption Text=""Fast foods"" IconName=""@BitIconName.HeartBroken"">
        <BitNavOption Text=""Burgers"">
            <BitNavOption Text=""Beef Burger"" />
            <BitNavOption Text=""Veggie Burger"" />
        </BitNavOption>
        <BitNavOption Text=""Pizza"">
            <BitNavOption Text=""Cheese Pizza"" />
            <BitNavOption Text=""Meat Pizza"" />
        </BitNavOption>
    </BitNavOption>
    <BitNavOption Text=""Fruits"" IconName=""@BitIconName.Health"">
        <BitNavOption Text=""Apple"" />
        <BitNavOption Text=""Orange"" />
    </BitNavOption>
    <BitNavOption Text=""Drinks"" IconName=""@BitIconName.Coffee"">
        <BitNavOption Text=""Water"" />
        <BitNavOption Text=""Tea"" />
    </BitNavOption>
</BitNav>";

    private readonly string example8RazorCode = @"
<BitNav TItem=""BitNavOption"" AllExpanded NoCollapse>
    <BitNavOption Text=""bit platform"" Description=""the bit platform description"">
        <BitNavOption Text=""Home"" IconName=""@BitIconName.Home"" Url=""https://bitplatform.dev/"" Target=""_blank"" />
        <BitNavOption Text=""Products & Services"">
            <BitNavOption Text=""BlazorUI"" IconName=""@BitIconName.F12DevTools"" Url=""https://bitplatform.dev/components"" Target=""_blank"" />
            <BitNavOption Text=""Pricing"" IconName=""@BitIconName.Money"" Url=""https://bitplatform.dev/pricing"" Target=""_blank"" />
        </BitNavOption>
    </BitNavOption>
    <BitNavOption Text=""Community"">
        <BitNavOption Text=""GitHub repo"" IconName=""@BitIconName.GitGraph"" Url=""https://github.com/bitfoundation/bitplatform"" Target=""_blank"" />
    </BitNavOption>
    <BitNavOption Text=""Iconography"" IconName=""@BitIconName.AppIconDefault"" Url=""/iconography"" />
</BitNav>";

    private readonly string example9RazorCode = @"
<BitNav TItem=""BitNavOption"" ReversedChevron AllExpanded FitWidth>
    <BitNavOption Text=""bit platform"" IconName=""@BitIconName.Website"">
        <BitNavOption Text=""Home"" IconName=""@BitIconName.Home"" />
        <BitNavOption Text=""Products & Services"" IconName=""@BitIconName.Product"">
            <BitNavOption Text=""BlazorUI"" IconName=""@BitIconName.F12DevTools"" />
        </BitNavOption>
    </BitNavOption>
    <BitNavOption Text=""Iconography"" IconName=""@BitIconName.AppIconDefault"" />
</BitNav>

<BitNav TItem=""BitNavOption"" ChevronDownIconName=""@BitIconName.CircleAdditionSolid"" AllExpanded FitWidth>
    @* ... the same options ... *@
</BitNav>

<BitNav TItem=""BitNavOption"" IndentValue=""40"" IndentPadding=""40"" AllExpanded FitWidth>
    @* ... the same options ... *@
</BitNav>";

    private readonly string example10RazorCode = @"
<BitNav TItem=""BitNavOption"" RenderType=""BitNavRenderType.Grouped"">
    <HeaderTemplate Context=""option"">
        <div class=""nav-custom-header"">
            <BitIcon IconName=""@BitIconName.FavoriteStarFill"" />
            <span>@option.Text</span>
        </div>
    </HeaderTemplate>
    <ChildContent>
        <BitNavOption Text=""Mercedes-Benz"" IsExpanded=""true"">
            <BitNavOption Text=""GLA"" Url=""https://www.mbusa.com/en/vehicles/class/gla/suv"" Target=""_blank"" />
            <BitNavOption Text=""GLB"" Url=""https://www.mbusa.com/en/vehicles/class/glb/suv"" Target=""_blank"" />
        </BitNavOption>
        <BitNavOption Text=""Tesla"">
            <BitNavOption Text=""Model S"" Url=""https://www.tesla.com/models"" Target=""_blank"" />
            <BitNavOption Text=""Model X"" Url=""https://www.tesla.com/modelx"" Target=""_blank"" />
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
        <BitNavOption Text=""Fast foods"" IconName=""@BitIconName.HeartBroken"" IsExpanded=""true"">
            <BitNavOption Text=""Burgers"" />
            <BitNavOption Text=""Pizza"" />
        </BitNavOption>
        <BitNavOption Text=""Fruits"" IconName=""@BitIconName.Health"">
            <BitNavOption Text=""Apple"" />
            <BitNavOption Text=""Orange"" />
        </BitNavOption>
    </ChildContent>
</BitNav>

<BitNav TItem=""BitNavOption"" FitWidth ItemTemplateRenderMode=""BitNavItemTemplateRenderMode.Replace"">
    <ItemTemplate Context=""option"">
        <div class=""nav-custom-item"">
            <BitIcon IconName=""@option.IconName"" />
            <span>@option.Text</span>
            <BitTag Color=""BitColor.SecondaryBackground"">custom</BitTag>
        </div>
    </ItemTemplate>
    <ChildContent>
        <BitNavOption Text=""Home"" IconName=""@BitIconName.Home"" />
        <BitNavOption Text=""Products"" IconName=""@BitIconName.Product"" />
        <BitNavOption Text=""Settings"" IconName=""@BitIconName.Settings"" />
    </ChildContent>
</BitNav>";

    private readonly string example11RazorCode = @"
<BitStack Horizontal Wrap>
    <BitButton OnClick=""ExpandAllApiOptions"">ExpandAll</BitButton>
    <BitButton OnClick=""CollapseAllApiOptions"">CollapseAll</BitButton>
</BitStack>

<BitNav @ref=""apiNavRef"" TItem=""BitNavOption"" Mode=""BitNavMode.Manual"" FitWidth>
    <BitNavOption Text=""Fast foods"" IconName=""@BitIconName.HeartBroken"">
        <BitNavOption Text=""Burgers"">
            <BitNavOption Text=""Beef Burger"" />
            <BitNavOption Text=""Veggie Burger"" />
        </BitNavOption>
        <BitNavOption Text=""Pizza"">
            <BitNavOption Text=""Cheese Pizza"" />
            <BitNavOption Text=""Meat Pizza"" />
        </BitNavOption>
    </BitNavOption>
    <BitNavOption Text=""Fruits"" IconName=""@BitIconName.Health"">
        <BitNavOption Text=""Apple"" />
        <BitNavOption Text=""Orange"" />
    </BitNavOption>
    <BitNavOption Text=""Ice Cream"" IconName=""@BitIconName.Emoji2"" />
    <BitNavOption Text=""Cookie"" IconName=""@BitIconName.Cake"" />
</BitNav>";
    private readonly string example11CsharpCode = @"
private BitNav<BitNavOption>? apiNavRef;

private void ExpandAllApiOptions() => apiNavRef?.ExpandAll();
private void CollapseAllApiOptions() => apiNavRef?.CollapseAll();";

    private readonly string example12RazorCode = @"
<BitNav Mode=""BitNavMode.Manual""
        OnItemClick=""(BitNavOption option) => ClickedOption = option""
        OnSelectItem=""(BitNavOption option) => SelectedOption = option""
        OnItemToggle=""(BitNavOption option) => ToggledOption = option"">
    <BitNavOption Text=""Fast foods"" Description=""List of fast foods""
                  IconName=""@BitIconName.HeartBroken"" IsExpanded=""true"">
        <BitNavOption Text=""Burgers"">
            <BitNavOption Text=""Beef Burger"" />
            <BitNavOption Text=""Veggie Burger"" />
        </BitNavOption>
        <BitNavOption Text=""Pizza"">
            <BitNavOption Text=""Cheese Pizza"" />
            <BitNavOption Text=""Meat Pizza"" />
        </BitNavOption>
        <BitNavOption Text=""French Fries"" />
    </BitNavOption>
    <BitNavOption Text=""Fruits"" IconName=""@BitIconName.Health"">
        <BitNavOption Text=""Apple"" />
        <BitNavOption Text=""Orange"" />
        <BitNavOption Text=""Banana"" />
    </BitNavOption>
    <BitNavOption Text=""Ice Cream"" />
    <BitNavOption Text=""Cookie"" />
</BitNav>

<div>
    <span>Clicked Item: <b>@ClickedOption?.Text</b></span><br />
    <span>Selected Item: <b>@SelectedOption?.Text</b></span><br />
    <span>Toggled Item: <b>@(ToggledOption is null ? ""N/A"" : $""{ToggledOption.Text} ({(ToggledOption.IsExpanded ? ""Expanded"" : ""Collapsed"")})"")</b></span>
</div>";
    private readonly string example12CsharpCode = @"
private BitNavOption ClickedOption = default!;
private BitNavOption ToggledOption = default!;
private BitNavOption SelectedOption = default!;";

    private readonly string example13RazorCode = @"
<BitNav TItem=""BitNavOption"" FitWidth Color=""BitColor.Primary"" Mode=""BitNavMode.Manual"">
    <BitNavOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavOption Text=""Products"" IconName=""@BitIconName.Product"" />
    <BitNavOption Text=""Settings"" IconName=""@BitIconName.Settings"" />
</BitNav>

<BitNav TItem=""BitNavOption"" FitWidth Color=""BitColor.Success"" Mode=""BitNavMode.Manual""> ... </BitNav>
<BitNav TItem=""BitNavOption"" FitWidth Color=""BitColor.Warning"" Mode=""BitNavMode.Manual""> ... </BitNav>
<BitNav TItem=""BitNavOption"" FitWidth Color=""BitColor.Error"" Mode=""BitNavMode.Manual""> ... </BitNav>

<BitNav TItem=""BitNavOption"" FitWidth Accent=""BitColor.Primary"" Mode=""BitNavMode.Manual""> ... </BitNav>
<BitNav TItem=""BitNavOption"" FitWidth Accent=""BitColor.Success"" Mode=""BitNavMode.Manual""> ... </BitNav>
<BitNav TItem=""BitNavOption"" FitWidth Accent=""BitColor.Error"" Mode=""BitNavMode.Manual""> ... </BitNav>";

    private readonly string example14RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitNav TItem=""BitNavOption"" FitWidth>
    <BitNavOption Text=""bit platform"" Description=""Nav with external icons (FontAwesome)"">
        <BitNavOption Text=""Home"" Icon=""fontAwesomeHomeIcon"" Url=""https://bitplatform.dev/"" Target=""_blank"" />
        <BitNavOption Text=""BlazorUI"" Icon=""fontAwesomeCodeIcon"" Url=""https://bitplatform.dev/components"" Target=""_blank"" />
        <BitNavOption Text=""Pricing"" Icon=""fontAwesomeTagIcon"" Url=""https://bitplatform.dev/pricing"" Target=""_blank"" />
    </BitNavOption>
    <BitNavOption Text=""Iconography"" Icon=""fontAwesomeIconsIcon"" Url=""/iconography"" />
</BitNav>

<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitNav TItem=""BitNavOption"" FitWidth ChevronDownIcon=""bootstrapChevronIcon"">
    <BitNavOption Text=""bit platform"" Description=""Nav with external icons (Bootstrap Icons)"">
        <BitNavOption Text=""Home"" Icon=""bootstrapHomeIcon"" Url=""https://bitplatform.dev/"" Target=""_blank"" />
        <BitNavOption Text=""BlazorUI"" Icon=""bootstrapCodeIcon"" Url=""https://bitplatform.dev/components"" Target=""_blank"" />
        <BitNavOption Text=""Pricing"" Icon=""bootstrapTagIcon"" Url=""https://bitplatform.dev/pricing"" Target=""_blank"" />
    </BitNavOption>
    <BitNavOption Text=""Iconography"" Icon=""bootstrapSmileIcon"" Url=""/iconography"" />
</BitNav>";
    private readonly string example14CsharpCode = @"
private static readonly BitIconInfo fontAwesomeHomeIcon = BitIconInfo.Css(""fa-solid fa-house"");
private static readonly BitIconInfo fontAwesomeCodeIcon = BitIconInfo.Fa(""solid code"");
private static readonly BitIconInfo fontAwesomeTagIcon = BitIconInfo.Css(""fa-solid fa-tag"");
private static readonly BitIconInfo fontAwesomeIconsIcon = BitIconInfo.Css(""fa-solid fa-icons"");

private static readonly BitIconInfo bootstrapChevronIcon = BitIconInfo.Bi(""chevron-right"");
private static readonly BitIconInfo bootstrapHomeIcon = BitIconInfo.Bi(""house-fill"");
private static readonly BitIconInfo bootstrapCodeIcon = BitIconInfo.Bi(""code-slash"");
private static readonly BitIconInfo bootstrapTagIcon = BitIconInfo.Bi(""tag-fill"");
private static readonly BitIconInfo bootstrapSmileIcon = BitIconInfo.Bi(""emoji-smile"");";

    private readonly string example15RazorCode = @"
<BitNav TItem=""BitNavOption"" FitWidth Size=""BitSize.Small"">
    <BitNavOption Text=""Home"" IconName=""@BitIconName.Home"" Description=""The main page"" />
    <BitNavOption Text=""Products"" IconName=""@BitIconName.Product"" Description=""All of the products"" />
    <BitNavOption Text=""Settings"" IconName=""@BitIconName.Settings"" Description=""The app settings"" />
</BitNav>

<BitNav TItem=""BitNavOption"" FitWidth Size=""BitSize.Medium""> ... </BitNav>

<BitNav TItem=""BitNavOption"" FitWidth Size=""BitSize.Large""> ... </BitNav>";

    private readonly string example16RazorCode = @"
<BitNav TItem=""BitNavOption"" Style=""max-width: max-content; border: 1px solid tomato;"">
    <BitNavOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavOption Text=""Products"" IconName=""@BitIconName.Product"" />
</BitNav>

<BitNav TItem=""BitNavOption"" Class=""custom-class"">
    <BitNavOption Text=""Home"" IconName=""@BitIconName.Home"" />
    <BitNavOption Text=""Products"" IconName=""@BitIconName.Product"" />
</BitNav>

<BitNav TItem=""BitNavOption"" FitWidth>
    <BitNavOption Text=""Home"" IconName=""@BitIconName.Home"" Style=""background: rgba(255,99,71,0.2);"" />
    <BitNavOption Text=""Products"" IconName=""@BitIconName.Product"" Class=""custom-item-list"" />
    <BitNavOption Text=""Settings"" IconName=""@BitIconName.Settings"" />
</BitNav>

<BitNav TItem=""BitNavOption""
        AllExpanded
        Mode=""BitNavMode.Manual""
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
                          Separator = ""border-color: cyan;"" })"">
    <BitNavOption Text=""bit platform"" Description=""the bit platform description"">
        <BitNavOption Text=""Home"" IconName=""@BitIconName.Home"" />
        <BitNavOption Text=""BlazorUI"" IconName=""@BitIconName.F12DevTools"" />
    </BitNavOption>
    <BitNavOption Text=""Iconography"" IconName=""@BitIconName.AppIconDefault"" />
    <BitNavOption IsSeparator=""true"" />
    <BitNavOption Text=""Contact us"" IconName=""@BitIconName.Contact"" />
</BitNav>

<BitNav TItem=""BitNavOption""
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
                           Description = ""custom-description"" })"">
    <BitNavOption Text=""bit platform"" IconName=""@BitIconName.Website"" Description=""the bit platform description"">
        <BitNavOption Text=""Home"" IconName=""@BitIconName.Home"" />
        <BitNavOption Text=""BlazorUI"" IconName=""@BitIconName.F12DevTools"" />
    </BitNavOption>
    <BitNavOption Text=""Community"" IconName=""@BitIconName.Group"">
        <BitNavOption Text=""GitHub repo"" IconName=""@BitIconName.GitGraph"" />
    </BitNavOption>
</BitNav>";

    private readonly string example17RazorCode = @"
<div dir=""rtl"">
    <BitNav TItem=""BitNavOption"" Dir=""BitDir.Rtl"">
        <BitNavOption Text=""پلتفرمِ بیت"" Description=""توضیحاتِ پلتفرمِ بیت"">
            <BitNavOption Text=""خانه"" IconName=""@BitIconName.Home"" Url=""https://bitplatform.dev/"" Target=""_blank"" />
            <BitNavOption Text=""محصولات و خدمات"">
                <BitNavOption Text=""رابط کاربری Blazor"" IconName=""@BitIconName.F12DevTools"" Url=""https://blazorui.bitplatform.dev/"" Target=""_blank"" />
                <BitNavOption Text=""راه های هاست ابری"" IconName=""@BitIconName.Cloud"" Url=""https://bitplatform.dev/#"" IsEnabled=""false"" />
            </BitNavOption>
            <BitNavOption Text=""قیمت"" IconName=""@BitIconName.Money"" Url=""https://bitplatform.dev/pricing"" Target=""_blank"" />
            <BitNavOption Text=""ارتباط با ما"" IconName=""@BitIconName.Contact"" Url=""https://bitplatform.dev/contact-us"" Target=""_blank"" />
        </BitNavOption>
        <BitNavOption Text=""انجمن ها"">
            <BitNavOption Text=""لینکدین"" IconName=""@BitIconName.LinkedInLogo"" Url=""https://www.linkedin.com/company/bitplatformhq"" Target=""_blank"" />
            <BitNavOption Text=""گیتهاب"" IconName=""@BitIconName.GitGraph"" Url=""https://github.com/bitfoundation/bitplatform"" Target=""_blank"" />
        </BitNavOption>
        <BitNavOption Text=""شمایل نگاری"" IconName=""@BitIconName.AppIconDefault"" Url=""/iconography"" />
    </BitNav>
</div>";
}

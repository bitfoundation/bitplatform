using System.Collections.Generic;
using System.Linq;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.NavGroup;

public partial class BitNavGroupDemo
{
    private string SelectedOptionKey;
    private static readonly List<BitDropDownItem> FoodMenuDropDownItems = new()
    {
        new BitDropDownItem
        {
            Text = "Beef Burger",
            Value = "Beef Burger",
        },
        new BitDropDownItem
        {
            Text = "Veggie Burger",
            Value = "Veggie Burger",
        },
        new BitDropDownItem
        {
            Text = "Bison Burger",
            Value = "Bison Burger",
        },
        new BitDropDownItem
        {
            Text = "Wild Salmon Burger",
            Value = "Wild Salmon Burger",
        },
        new BitDropDownItem
        {
            Text = "Cheese Pizza",
            Value = "Cheese Pizza",
        },
        new BitDropDownItem
        {
            Text = "Veggie Pizza",
            Value = "Veggie Pizza",
        },
        new BitDropDownItem
        {
            Text = "Pepperoni Pizza",
            Value = "Pepperoni Pizza",
        },
        new BitDropDownItem
        {
            Text = "Meat Pizza",
            Value = "Meat Pizza",
        },
        new BitDropDownItem
        {
            Text = "French Fries",
            Value = "French Fries",
        },
        new BitDropDownItem
        {
            Text = "Aplle",
            Value = "Aplle",
        },
        new BitDropDownItem
        {
            Text = "Orange",
            Value = "Orange",
        },
        new BitDropDownItem
        {
            Text = "Benana",
            Value = "Benana",
        },
        new BitDropDownItem
        {
            Text = "Ice Cream",
            Value = "Ice Cream",
        },
        new BitDropDownItem
        {
            Text = "Cookie",
            Value = "Cookie",
        },
    };

    private BitNavOption ClickedOption;
    private BitNavOption SelectedOption;
    private BitNavOption ToggledOption;

    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter
        {

        }
    };
    private readonly List<ComponentSubParameter> componentSubParameters = new()
    {
        new ComponentSubParameter()
        {
            Id = "nav-option",
            Title = "BitNavOption",
            Parameters = new List<ComponentParameter>()
            {
                new ComponentParameter
                {

                }
            }
        }
    };
    private readonly List<EnumParameter> enumParameters = new()
    {
        new EnumParameter()
        {
            Id = "nav-mode-enum",
            Title = "BitNavMode Enum",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name = "Automatic",
                    Description = "The value of selected key will change using NavigationManager and the current url inside the component.",
                    Value = "0",
                },
                new EnumItem()
                {
                    Name = "Manual",
                    Description = "Selected key changes will be sent back to the parent component and the component won't change its value.",
                    Value = "1",
                }
            }
        },
        new EnumParameter()
        {
            Id = "nav-render-type-enum",
            Title = "BitNavRenderType Enum",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name = "Normal",
                    Value = "0",
                },
                new EnumItem()
                {
                    Name = "Grouped",
                    Value = "1",
                }
            }
        },
        new EnumParameter()
        {
            Id = "nav-item-aria-current-enum",
            Title = "BitNavItemAriaCurrent Enum",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name = "Page",
                    Value = "0",
                },
                new EnumItem()
                {
                    Name = "Step",
                    Value = "1",
                },
                new EnumItem()
                {
                    Name = "Location",
                    Value = "2",
                },
                new EnumItem()
                {
                    Name = "Date",
                    Value = "3",
                },
                new EnumItem()
                {
                    Name = "Time",
                    Value = "4",
                },
                new EnumItem()
                {
                    Name = "True",
                    Value = "5",
                },

            }
        },
    };

    #region Sample Code 1

    private static string example1HTMLCode = @"
<BitNavGroup>
    <BitNavOption Text=""Bit Platform""
                  ExpandAriaLabel=""Bit Platform Expanded""
                  CollapseAriaLabel=""Bit Platform Collapsed""
                  IconName=""BitIconName.TabletMode""
                  IsExpanded=""true"">
        <BitNavOption Text=""Home"" Url=""https://bitplatform.dev/"" Target=""_blank"" />
        <BitNavOption Text=""Products & Services"">
            <BitNavOption Text=""Project Templates"">
                <BitNavOption Text=""TodoTemplate"" Url=""https://bitplatform.dev/todo-template/overview"" Target=""_blank"" />
                <BitNavOption Text=""AdminPanel"" Url=""https://bitplatform.dev/admin-panel/overview"" Target=""_blank"" />
            </BitNavOption>
            <BitNavOption Text=""BlazorUI"" Url=""https://bitplatform.dev/components"" Target=""_blank"" />
            <BitNavOption Text=""Cloud hosting solutions"" IsEnabled=""false"" />
            <BitNavOption Text=""Bit academy"" IsEnabled=""false"" />
        </BitNavOption>
        <BitNavOption Text=""Pricing"" Url=""https://bitplatform.dev/pricing"" Target=""_blank"" />
        <BitNavOption Text=""About"" Url=""https://bitplatform.dev/about-us"" Target=""_blank"" />
        <BitNavOption Text=""Contact us"" Url=""https://bitplatform.dev/contact-us"" Target=""_blank"" />
    </BitNavOption>

    <BitNavOption Text=""Community""
                  ExpandAriaLabel=""Community Expanded""
                  CollapseAriaLabel=""Community Collapsed""
                  IconName=""BitIconName.Heart"">
        <BitNavOption Text=""Linkedin"" Url=""https://www.linkedin.com/company/bitplatformhq/about"" Target=""_blank"" />
        <BitNavOption Text=""Twitter"" Url=""https://twitter.com/bitplatformhq"" Target=""_blank"" />
        <BitNavOption Text=""Github repo"" Url=""https://github.com/bitfoundation/bitplatform"" Target=""_blank"" />
    </BitNavOption>

    <BitNavOption Text=""Iconography"" Url=""/icons"" Target=""_blank"" />
</BitNavGroup>
";

    #endregion

    #region Sample Code 2

    private static string example2HTMLCode = @"
<BitNavGroup RenderType=""BitNavRenderType.Grouped"">
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
</BitNavGroup>
";

    #endregion

    #region Sample Code 3

    private static string example3HTMLCode = @"
<div>
    <BitLabel>Basic</BitLabel>
    <BitNavGroup Mode=""BitNavMode.Manual"" DefaultSelectedKey=""French Fries"">
        <BitNavOption Text=""Fast-Foods""
                        IconName=""BitIconName.HeartBroken""
                        IsExpanded=""true"">
            <BitNavOption Text=""Burgers"">
                <BitNavOption Text=""Beef Burger"" Key=""Beef Burger"" />
                <BitNavOption Text=""Veggie Burger"" Key=""Veggie Burger"" />
                <BitNavOption Text=""Bison Burger"" Key=""Bison Burger"" />
                <BitNavOption Text=""Wild Salmon Burger"" Key=""Wild Salmon Burger"" />
            </BitNavOption>
            <BitNavOption Text=""Pizzas"">
                <BitNavOption Text=""Cheese Pizza"" Key=""Cheese Pizza"" />
                <BitNavOption Text=""Veggie Pizza"" Key=""Veggie Pizza"" />
                <BitNavOption Text=""Pepperoni Pizza"" Key=""Pepperoni Pizza"" />
                <BitNavOption Text=""Meat Pizza"" Key=""Meat Pizza"" />
            </BitNavOption>
            <BitNavOption Text=""French Fries"" Key=""French Fries"" />
        </BitNavOption>
        <BitNavOption Text=""Fruits"">
            <BitNavOption Text=""Aplle"" Key=""Aplle"" />
            <BitNavOption Text=""Orange"" Key=""Orange"" />
            <BitNavOption Text=""Benana"" Key=""Benana"" />
        </BitNavOption>
        <BitNavOption Text=""Ice Cream"" Key=""Ice Cream"" />
        <BitNavOption Text=""Cookie"" Key=""Cookie"" />
    </BitNavGroup>
</div>

<div class=""margin-top"">
    <BitLabel>Two-Way Bind</BitLabel>
    <BitNavGroup @bind-SelectedKey=""SelectedOptionKey""
                 DefaultSelectedKey=""French Fries""
                 Mode=""BitNavMode.Manual"">
        <BitNavOption Text=""Fast-Foods""
                      IconName=""BitIconName.HeartBroken""
                      IsExpanded=""true"">
            <BitNavOption Text=""Burgers"">
                <BitNavOption Text=""Beef Burger"" Key=""Beef Burger"" />
                <BitNavOption Text=""Veggie Burger"" Key=""Veggie Burger"" />
                <BitNavOption Text=""Bison Burger"" Key=""Bison Burger"" />
                <BitNavOption Text=""Wild Salmon Burger"" Key=""Wild Salmon Burger"" />
            </BitNavOption>
            <BitNavOption Text=""Pizzas"">
                <BitNavOption Text=""Cheese Pizza"" Key=""Cheese Pizza"" />
                <BitNavOption Text=""Veggie Pizza"" Key=""Veggie Pizza"" />
                <BitNavOption Text=""Pepperoni Pizza"" Key=""Pepperoni Pizza"" />
                <BitNavOption Text=""Meat Pizza"" Key=""Meat Pizza"" />
            </BitNavOption>
            <BitNavOption Text=""French Fries"" Key=""French Fries"" />
        </BitNavOption>
        <BitNavOption Text=""Fruits"">
            <BitNavOption Text=""Aplle"" Key=""Aplle"" />
            <BitNavOption Text=""Orange"" Key=""Orange"" />
            <BitNavOption Text=""Benana"" Key=""Benana"" />
        </BitNavOption>
        <BitNavOption Text=""Ice Cream"" Key=""Ice Cream"" />
        <BitNavOption Text=""Cookie"" Key=""Cookie"" />
    </BitNavGroup>

    <BitDropDown @bind-Value=""SelectedOptionKey""
                 DefaultValue=""French Fries""
                 Label=""Pick one""
                 Items=""FoodMenuDropDownItems"" />
</div>
";

    private static string example3CSharpCode = @"
private string SelectedOptionKey;

private static readonly List<BitDropDownItem> FoodMenuDropDownItems = new()
{
    new BitDropDownItem
    {
        Text = ""Beef Burger"",
        Value = ""Beef Burger"",
    },
    new BitDropDownItem
    {
        Text = ""Veggie Burger"",
        Value = ""Veggie Burger"",
    },
    new BitDropDownItem
    {
        Text = ""Bison Burger"",
        Value = ""Bison Burger"",
    },
    new BitDropDownItem
    {
        Text = ""Wild Salmon Burger"",
        Value = ""Wild Salmon Burger"",
    },
    new BitDropDownItem
    {
        Text = ""Cheese Pizza"",
        Value = ""Cheese Pizza"",
    },
    new BitDropDownItem
    {
        Text = ""Veggie Pizza"",
        Value = ""Veggie Pizza"",
    },
    new BitDropDownItem
    {
        Text = ""Pepperoni Pizza"",
        Value = ""Pepperoni Pizza"",
    },
    new BitDropDownItem
    {
        Text = ""Meat Pizza"",
        Value = ""Meat Pizza"",
    },
    new BitDropDownItem
    {
        Text = ""French Fries"",
        Value = ""French Fries"",
    },
    new BitDropDownItem
    {
        Text = ""Aplle"",
        Value = ""Aplle"",
    },
    new BitDropDownItem
    {
        Text = ""Orange"",
        Value = ""Orange"",
    },
    new BitDropDownItem
    {
        Text = ""Benana"",
        Value = ""Benana"",
    },
    new BitDropDownItem
    {
        Text = ""Ice Cream"",
        Value = ""Ice Cream"",
    },
    new BitDropDownItem
    {
        Text = ""Cookie"",
        Value = ""Cookie"",
    },
};
";

    #endregion

    #region Sample Code 4

    private static string example4HTMLCode = @"
<style>
    .nav-group-custom-header {
        font-size: 17px;
        font-weight: 600;
        color: green;
    }

    .nav-group-custom-option {
        display: flex;
        align-items: center;
        flex-flow: row nowrap;
        gap: 4px;
        color: #ff7800;
        font-weight: 600;
    }
</style>

<div>
    <BitLabel>Header Template (in Grouped mode)</BitLabel>
    <BitNavGroup RenderType=""BitNavRenderType.Grouped"">
        <HeaderTemplate Context=""item"">
            <div class=""nav-group-custom-header"">
                <BitIcon IconName=""BitIconName.FavoriteStarFill"" />
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
    </BitNavGroup>
</div>

<div class=""margin-top"">
    <BitLabel>Option Template</BitLabel>
    <BitNavGroup Mode=""BitNavMode.Manual"">
        <OptionTemplate Context=""option"">
            <div class=""nav-group-custom-option"">
                <BitCheckbox IsEnabled=""@(option.IsEnabled)"" />
                <span>@option.Text</span>
            </div>
        </OptionTemplate>
        <ChildContent>
            <BitNavOption Text=""Fast-Foods""
                          IsExpanded=""true"">
                <BitNavOption Text=""Burgers"">
                    <BitNavOption Text=""Beef Burger"" />
                    <BitNavOption Text=""Veggie Burger"" />
                    <BitNavOption Text=""Bison Burger"" />
                    <BitNavOption Text=""Wild Salmon Burger"" />
                </BitNavOption>
                <BitNavOption Text=""Pizzas"">
                    <BitNavOption Text=""Cheese Pizza"" />
                    <BitNavOption Text=""Veggie Pizza"" />
                    <BitNavOption Text=""Pepperoni Pizza"" />
                    <BitNavOption Text=""Meat Pizza"" />
                </BitNavOption>
                <BitNavOption Text=""French Fries"" />
            </BitNavOption>
            <BitNavOption Text=""Fruits"">
                <BitNavOption Text=""Aplle"" />
                <BitNavOption Text=""Orange"" />
                <BitNavOption Text=""Benana"" />
            </BitNavOption>
            <BitNavOption Text=""Ice Cream"" />
            <BitNavOption Text=""Cookie"" />
        </ChildContent>
    </BitNavGroup>
</div>
";

    #endregion

    #region Sample Code 5

    private static string example5HTMLCode = @"
<BitNavGroup Mode=""BitNavMode.Manual""
             DefaultSelectedKey=""French Fries""
             OnOptionClick=""(option) => ClickedOption = option""
             OnSelectOption=""(option) => SelectedOption = option""
             OnOptionToggle=""(option) => ToggledOption = option"">
    <BitNavOption Text=""Fast-Foods""
                    IconName=""BitIconName.HeartBroken""
                    IsExpanded=""true"">
        <BitNavOption Text=""Burgers"">
            <BitNavOption Text=""Beef Burger"" Key=""Beef Burger"" />
            <BitNavOption Text=""Veggie Burger"" Key=""Veggie Burger"" />
            <BitNavOption Text=""Bison Burger"" Key=""Bison Burger"" />
            <BitNavOption Text=""Wild Salmon Burger"" Key=""Wild Salmon Burger"" />
        </BitNavOption>
        <BitNavOption Text=""Pizzas"">
            <BitNavOption Text=""Cheese Pizza"" Key=""Cheese Pizza"" />
            <BitNavOption Text=""Veggie Pizza"" Key=""Veggie Pizza"" />
            <BitNavOption Text=""Pepperoni Pizza"" Key=""Pepperoni Pizza"" />
            <BitNavOption Text=""Meat Pizza"" Key=""Meat Pizza"" />
        </BitNavOption>
        <BitNavOption Text=""French Fries"" Key=""French Fries"" />
    </BitNavOption>
    <BitNavOption Text=""Fruits"">
        <BitNavOption Text=""Aplle"" Key=""Aplle"" />
        <BitNavOption Text=""Orange"" Key=""Orange"" />
        <BitNavOption Text=""Benana"" Key=""Benana"" />
    </BitNavOption>
    <BitNavOption Text=""Ice Cream"" Key=""Ice Cream"" />
    <BitNavOption Text=""Cookie"" Key=""Cookie"" />
</BitNavGroup>

<div class=""flex"">
    <span>Clicked Option: @ClickedOption?.Text</span>
    <span>Selected Option: @SelectedOption?.Text</span>
    <span>Toggled Option: @(ToggledOption is null ? ""N/A"" : $""{ToggledOption.Text} ({(ToggledOption.IsExpanded ? ""Expanded"" : ""Collapsed"")})"")</span>
</div>
";

    private static string example5CSharpCode = @"
private BitNavOption ClickedOption;
private BitNavOption SelectedOption;
private BitNavOption ToggledOption;
";

    #endregion
}

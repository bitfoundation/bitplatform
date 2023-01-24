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
            Name = "DefaultSelectedItem",
            Type = "BitNavItem?",
            Description = "The initially selected item in manual mode.",
            Href = "#nav-item",
            LinkType = LinkType.Link,
        },
        new ComponentParameter
        {
            Name = "HeaderTemplate",
            Type = "RenderFragment<BitNavItem>?",
            Description = "Used to customize how content inside the group header is rendered.",
        },
        new ComponentParameter
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<BitNavItem>?",
            Description = "Used to customize how content inside the link tag is rendered.",
        },
        new ComponentParameter
        {
            Name = "Items",
            Type = "IList<BitNavItem>",
            DefaultValue = "new List<BitNavItem>()",
            Description = "A collection of link items to display in the navigation bar.",
        },
        new ComponentParameter
        {
            Name = "Mode",
            Type = "BitNavMode",
            DefaultValue = "BitNavMode.Automatic",
            Description = "Determines how the navigation will be handled. The default value is Automatic.",
            Href = "#nav-mode-enum",
            LinkType = LinkType.Link
        },
        new ComponentParameter
        {
            Name = "OnItemClick",
            Type = "EventCallback<BitNavItem>",
            Description = "Callback invoked when an item is clicked.",
        },
        new ComponentParameter
        {
            Name = "OnSelectItem",
            Type = "EventCallback<BitNavItem>",
            Description = "Callback invoked when an item is selected.",
        },
        new ComponentParameter
        {
            Name = "OnItemToggle",
            Type = "EventCallback<BitNavItem>",
            Description = "Callback invoked when a group header is clicked and Expanded or Collapse.",
        },
        new ComponentParameter
        {
            Name = "RenderType",
            Type = "BitNavRenderType",
            DefaultValue = "BitNavRenderType.Normal",
            Description = "The way to render nav links.",
            Href = "#nav-render-type-enum",
            LinkType = LinkType.Link,
        },
        new ComponentParameter
        {
            Name = "SelectedItem",
            Type = "BitNavItem?",
            Description = "Selected item to show in Nav.",
        },
    };
    private readonly List<ComponentSubParameter> componentSubParameters = new()
    {
        new ComponentSubParameter()
        {
            Id = "nav-item",
            Title = "BitNavItem",
            Parameters = new List<ComponentParameter>()
            {
               new ComponentParameter()
               {
                   Name = "AriaLabel",
                   Type = "string?",
                   Description = "Aria label for nav link. Ignored if collapseAriaLabel or expandAriaLabel is provided",
               },
               new ComponentParameter()
               {
                   Name = "AriaCurrent",
                   Type = "BitNavLinkItemAriaCurrent",
                   DefaultValue = "BitNavItemAriaCurrent.Page",
                   Description = "Aria-current token for active nav links. Must be a valid token value, and defaults to 'page'",
                   Href = "#nav-item-aria-current-enum",
                   LinkType = LinkType.Link,
               },
               new ComponentParameter()
               {
                   Name = "CollapseAriaLabel",
                   Type = "string?",
                   Description = "Aria label when items is collapsed and can be expanded.",
               },
               new ComponentParameter()
               {
                   Name = "ExpandAriaLabel",
                   Type = "string?",
                   Description = "Aria label when group is collapsed and can be expanded.",
               },
               new ComponentParameter()
               {
                   Name = "ForceAnchor",
                   Type = "bool",
                   Description = "(Optional) By default, any link with onClick defined will render as a button. Set this property to true to override that behavior. (Links without onClick defined will render as anchors by default.)",
               },
               new ComponentParameter()
               {
                   Name = "Items",
                   Type = "IList<BitNavItem>",
                   DefaultValue = "new List<BitNavItem>()",
                   Description = "A list of items to render as children of the current item.",
               },
               new ComponentParameter()
               {
                   Name = "IconName",
                   Type = "BitIconName",
                   Description = "Name of an icon to render next to this link button.",
               },
               new ComponentParameter()
               {
                   Name = "IsExpanded",
                   Type = "bool",
                   Description = "Whether or not the link is in an expanded state.",
               },
               new ComponentParameter()
               {
                   Name = "IsEnabled",
                   Type = "bool",
                   Description = "Whether or not the link is enabled.",
               },
               new ComponentParameter()
               {
                   Name = "Key",
                   Type = "string?",
                   Description = "A unique value to use as a key or id of the item.",
               },
               new ComponentParameter()
               {
                   Name = "Style",
                   Type = "string?",
                   Description = "Custom style for the each item element.",
               },
               new ComponentParameter()
               {
                   Name = "Text",
                   Type = "string",
                   DefaultValue = "string.Empty",
                   Description = "Text to render for this link.",
               },
               new ComponentParameter()
               {
                   Name = "Title",
                   Type = "string?",
                   Description = "Text for title tooltip.",
               },
               new ComponentParameter()
               {
                   Name = "Target",
                   Type = "string?",
                   Description = "Link target, specifies how to open the link.",
               },
               new ComponentParameter()
               {
                   Name = "Url",
                   Type = "string?",
                   Description = "URL to navigate to for this link.",
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

";

    private static string example1CSharpCode = @"

";

    #endregion

    #region Sample Code 2

    private static string example2HTMLCode = @"

";

    private static string example2CSharpCode = @"

";

    #endregion

    #region Sample Code 3

    private static string example3HTMLCode = @"

";

    private static string example3CSharpCode = @"

";

    #endregion

    #region Sample Code 4

    private static string example4HTMLCode = @"

";

    private static string example4CSharpCode = @"

";

    #endregion

    #region Sample Code 5

    private static string example5HTMLCode = @"

";

    private static string example5CSharpCode = @"

";

    #endregion
}

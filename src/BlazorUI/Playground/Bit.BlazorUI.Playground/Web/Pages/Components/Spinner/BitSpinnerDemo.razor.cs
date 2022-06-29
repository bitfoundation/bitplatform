using System.Collections.Generic;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Spinner;

public partial class BitSpinnerDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter()
        {
            Name = "AriaLive",
            Type = "BitSpinnerAriaLive",
            LinkType = LinkType.Link,
            Href= "#spinnerAriaLive-enum",
            DefaultValue = "BitSpinnerAriaLive.Polite",
            Description = "Politeness setting for label update announcement.",
        },
        new ComponentParameter()
        {
            Name = "Label",
            Type = "string",
            DefaultValue = "",
            Description = "The label to show next to the spinner. Label updates will be announced to the screen readers.",
        },
        new ComponentParameter()
        {
            Name = "LabelPosition",
            Type = "BitSpinnerLabelPosition",
            LinkType = LinkType.Link,
            Href = "#spinnerLabelPosition-enum",
            DefaultValue = "BitSpinnerLabelPosition.Top",
            Description = "The position of the label in regards to the spinner animation.",
        },
        new ComponentParameter()
        {
            Name = "Size",
            Type = "BitSpinnerSize",
            LinkType = LinkType.Link,
            Href = "#spinnerSize-enum",
            DefaultValue = "BitSpinnerSize.Medium",
            Description = "The size of spinner to render.",
        },
        new ComponentParameter()
        {
            Name = "Visibility",
            Type = "BitComponentVisibility",
            LinkType = LinkType.Link,
            Href = "#component-visibility-enum",
            DefaultValue = "BitComponentVisibility.Visible",
            Description = "Whether the component is Visible,Hidden,Collapsed.",
        },
    };

    private readonly List<EnumParameter> enumParameters = new()
    {
        new EnumParameter()
        {
            Id = "spinnerAriaLive-enum",
            Title = "BitSpinnerAriaLive Enum",
            Description = "",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name= "Assertive",
                    Description="",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "Polite",
                    Description="",
                    Value="1",
                },
                new EnumItem()
                {
                    Name= "Off",
                    Description="",
                    Value="2",
                },
            },
        },
        new EnumParameter()
        {
            Id = "spinnerLabelPosition-enum",
            Title = "BitSpinnerLabelPosition Enum",
            Description = "",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name= "Top",
                    Description="The label shows on the top of the spinner.",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "Right",
                    Description="The label shows on the right side of the spinner.",
                    Value="1",
                },
                new EnumItem()
                {
                    Name= "Bottom",
                    Description="The label shows on the bottom of the spinner.",
                    Value="2",
                },
                new EnumItem()
                {
                    Name= "Left",
                    Description="The label shows on the left side of the spinner.",
                    Value="3",
                },
            }
        },
        new EnumParameter()
        {
            Id = "spinnerSize-enum",
            Title = "BitSpinnerSize Enum",
            Description = "",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name= "Medium",
                    Description="20px Spinner diameter.",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "Large",
                    Description="28px Spinner diameter.",
                    Value="1",
                },
                new EnumItem()
                {
                    Name= "Small",
                    Description="16px Spinner diameter.",
                    Value="2",
                },
                new EnumItem()
                {
                    Name= "XSmall",
                    Description="12px Spinner diameter.",
                    Value="3",
                },
            },
        },
        new EnumParameter()
        {
            Id = "component-visibility-enum",
            Title = "BitComponentVisibility Enum",
            Description = "",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name= "Visible",
                    Description="Show content of the component.",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "Hidden",
                    Description="Hide content of the component,though the space it takes on the page remains.",
                    Value="1",
                },
                new EnumItem()
                {
                    Name= "Collapsed",
                    Description="Hide content of the component,though the space it takes on the page gone.",
                    Value="2",
                }
            }
        }
    };

    private readonly string example1HTMLCode = @"<BitSpinner Size=""BitSpinnerSize.XSmall"" />
<BitSpinner Size=""BitSpinnerSize.Small"" />
<BitSpinner Size=""BitSpinnerSize.Medium"" />
<BitSpinner Size=""BitSpinnerSize.Large"" />";

    private readonly string example2HTMLCode = @"<BitSpinner LabelPosition=""BitSpinnerLabelPosition.Top"" Label=""I am definitely loading..."" />
<BitSpinner LabelPosition=""BitSpinnerLabelPosition.Bottom"" Label=""Seriously, still loading..."" />
<BitSpinner LabelPosition=""BitSpinnerLabelPosition.Left"" Label=""Wait, wait..."" />
<BitSpinner LabelPosition=""BitSpinnerLabelPosition.Right"" Label=""Nope, still loading..."" />";
}

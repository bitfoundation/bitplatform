using Bit.BlazorUI.Demo.Client.Core.Models;
using Bit.BlazorUI.Demo.Client.Core.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Spinner;

public partial class BitSpinnerDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "AriaLive",
            Type = "BitSpinnerAriaLive",
            LinkType = LinkType.Link,
            Href= "#spinnerAriaLive-enum",
            DefaultValue = "BitSpinnerAriaLive.Polite",
            Description = "Politeness setting for label update announcement.",
        },
        new()
        {
            Name = "Label",
            Type = "string",
            DefaultValue = "",
            Description = "The label to show next to the spinner. Label updates will be announced to the screen readers.",
        },
        new()
        {
            Name = "LabelPosition",
            Type = "BitLabelPosition",
            LinkType = LinkType.Link,
            Href = "#spinnerLabelPosition-enum",
            DefaultValue = "BitLabelPosition.Top",
            Description = "The position of the label in regards to the spinner animation.",
        },
        new()
        {
            Name = "Size",
            Type = "BitSpinnerSize",
            LinkType = LinkType.Link,
            Href = "#spinnerSize-enum",
            DefaultValue = "BitSpinnerSize.Medium",
            Description = "The size of spinner to render.",
        }
    };

    private readonly List<ComponentSubEnum> componentSubEnums = new()
    {
        new()
        {
            Id = "spinnerAriaLive-enum",
            Name = "BitSpinnerAriaLive",
            Description = "",
            Items = new()
            {
                new()
                {
                    Name= "Assertive",
                    Description="",
                    Value="0",
                },
                new()
                {
                    Name= "Polite",
                    Description="",
                    Value="1",
                },
                new()
                {
                    Name= "Off",
                    Description="",
                    Value="2",
                },
            },
        },
        new()
        {
            Id = "spinnerLabelPosition-enum",
            Name = "BitLabelPosition",
            Description = "",
            Items = new()
            {
                new()
                {
                    Name= "Top",
                    Description="The label shows on the top of the spinner.",
                    Value="0",
                },
                new()
                {
                    Name= "Right",
                    Description="The label shows on the right side of the spinner.",
                    Value="1",
                },
                new()
                {
                    Name= "Bottom",
                    Description="The label shows on the bottom of the spinner.",
                    Value="2",
                },
                new()
                {
                    Name= "Left",
                    Description="The label shows on the left side of the spinner.",
                    Value="3",
                },
            }
        },
        new()
        {
            Id = "spinnerSize-enum",
            Name = "BitSpinnerSize",
            Description = "",
            Items = new()
            {
                new()
                {
                    Name= "Medium",
                    Description="20px Spinner diameter.",
                    Value="0",
                },
                new()
                {
                    Name= "Large",
                    Description="28px Spinner diameter.",
                    Value="1",
                },
                new()
                {
                    Name= "Small",
                    Description="16px Spinner diameter.",
                    Value="2",
                },
                new()
                {
                    Name= "XSmall",
                    Description="12px Spinner diameter.",
                    Value="3",
                },
            },
        }
    };



    private readonly string example1HTMLCode = @"
<BitSpinner Size=""BitSpinnerSize.XSmall"" />
<BitSpinner Size=""BitSpinnerSize.Small"" />
<BitSpinner Size=""BitSpinnerSize.Medium"" />
<BitSpinner Size=""BitSpinnerSize.Large"" />";

    private readonly string example2HTMLCode = @"
<BitSpinner LabelPosition=""BitLabelPosition.Top"" Label=""I am definitely loading..."" />
<BitSpinner LabelPosition=""BitLabelPosition.Bottom"" Label=""Seriously, still loading..."" />
<BitSpinner LabelPosition=""BitLabelPosition.Left"" Label=""Wait, wait..."" />
<BitSpinner LabelPosition=""BitLabelPosition.Right"" Label=""Nope, still loading..."" />";
}

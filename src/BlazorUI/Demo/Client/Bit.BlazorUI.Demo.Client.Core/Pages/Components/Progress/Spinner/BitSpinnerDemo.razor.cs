namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Progress.Spinner;

public partial class BitSpinnerDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
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
            Type = "string?",
            DefaultValue = "null",
            Description = "The label to show next to the spinner. Label updates will be announced to the screen readers.",
        },
        new()
        {
            Name = "LabelPosition",
            Type = "BitLabelPosition",
            LinkType = LinkType.Link,
            Href = "#spinner-labelPosition-enum",
            DefaultValue = "BitLabelPosition.Top",
            Description = "The position of the label in regards to the spinner animation.",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of spinner to render.",
            LinkType = LinkType.Link,
            Href = "#spinnerSize-enum",
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "spinnerAriaLive-enum",
            Name = "BitSpinnerAriaLive",
            Description = "",
            Items =
            [
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
            ],
        },
        new()
        {
            Id = "spinner-labelPosition-enum",
            Name = "BitLabelPosition",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Top",
                    Description="The label shows on the top of the spinner.",
                    Value="0",
                },
                new()
                {
                    Name= "End",
                    Description="The label shows on the end of the spinner.",
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
                    Name= "Start",
                    Description="The label shows on the start of the spinner.",
                    Value="3",
                },
            ]
        },
        new()
        {
            Id = "spinnerSize-enum",
            Name = "BitSize",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Small",
                    Description="12px Spinner diameter.",
                    Value="0",
                },
                new()
                {
                    Name= "Medium",
                    Description="20px Spinner diameter.",
                    Value="1",
                },
                new()
                {
                    Name= "Large",
                    Description="28px Spinner diameter.",
                    Value="2",
                },
            ],
        }
    ];



    private readonly string example1RazorCode = @"
<BitSpinner Size=""BitSize.Small"" />
<BitSpinner Size=""BitSize.Medium"" />
<BitSpinner Size=""BitSize.Large"" />";

    private readonly string example2RazorCode = @"
<BitSpinner LabelPosition=""BitLabelPosition.Top"" Label=""I am definitely loading..."" />
<BitSpinner LabelPosition=""BitLabelPosition.Bottom"" Label=""Seriously, still loading..."" />
<BitSpinner LabelPosition=""BitLabelPosition.Start"" Label=""Wait, wait..."" />
<BitSpinner LabelPosition=""BitLabelPosition.End"" Label=""Nope, still loading..."" />

<BitSpinner Dir=""BitDir.Rtl"" LabelPosition=""BitLabelPosition.Start"" Label=""در حال بار گذاری..."" />
<BitSpinner Dir=""BitDir.Rtl"" LabelPosition=""BitLabelPosition.End"" Label=""در حال بار گذاری..."" />";
}

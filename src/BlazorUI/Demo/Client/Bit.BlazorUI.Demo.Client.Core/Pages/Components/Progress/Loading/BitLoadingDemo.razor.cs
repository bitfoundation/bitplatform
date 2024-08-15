namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Progress.Loading;

public partial class BitLoadingDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the loading component.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "CustomColor",
            Type = "string?",
            DefaultValue = "null",
            Description = "The custom css color of the loading component.",
        },
        new()
        {
            Name = "CustomSize",
            Type = "int?",
            DefaultValue = "null",
            Description = "The custom size of the loading component in px.",
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text content of the label of the loading component.",
        },
        new()
        {
            Name = "LabelPosition",
            Type = "BitLabelPosition?",
            DefaultValue = "null",
            Description = "The position of the label of the loading component.",
            LinkType = LinkType.Link,
            Href = "#label-position-enum",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom content of the label of the loading component.",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The Size of the loading component.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "color-enum",
            Name = "BitColor",
            Description = "Defines the general colors available in the bit BlazorUI.",
            Items =
            [
                new()
                {
                    Name= "Primary",
                    Description="Info Primary general color.",
                    Value="0",
                },
                new()
                {
                    Name= "Secondary",
                    Description="Secondary general color.",
                    Value="1",
                },
                new()
                {
                    Name= "Tertiary",
                    Description="Tertiary general color.",
                    Value="2",
                },
                new()
                {
                    Name= "Info",
                    Description="Info general color.",
                    Value="3",
                },
                new()
                {
                    Name= "Success",
                    Description="Success general color.",
                    Value="4",
                },
                new()
                {
                    Name= "Warning",
                    Description="Warning general color.",
                    Value="5",
                },
                new()
                {
                    Name= "SevereWarning",
                    Description="SevereWarning general color.",
                    Value="6",
                },
                new()
                {
                    Name= "Error",
                    Description="Error general color.",
                    Value="7",
                }
            ]
        },
        new()
        {
            Id = "label-position-enum",
            Name = "BitLabelPosition",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Top",
                    Description="The label shows on the top of the button.",
                    Value="0",
                },
                new()
                {
                    Name= "End",
                    Description="The label shows on the end of the button.",
                    Value="1",
                },
                new()
                {
                    Name= "Bottom",
                    Description="The label shows on the bottom of the button.",
                    Value="2",
                },
                new()
                {
                    Name= "Start",
                    Description="The label shows on the start of the button.",
                    Value="3",
                },
            ]
        },
        new()
        {
            Id = "size-enum",
            Name = "BitSize",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Small",
                    Description="The small size button.",
                    Value="0",
                },
                new()
                {
                    Name= "Medium",
                    Description="The medium size button.",
                    Value="1",
                },
                new()
                {
                    Name= "Large",
                    Description="The large size button.",
                    Value="2",
                }
            ]
        },
    ];



    private readonly string example1RazorCode = @"
<BitBarsLoading />

<BitCircleLoading />

<BitDotsRingLoading />

<BitDualRingLoading />

<BitEllipsisLoading />

<BitGridLoading />

<BitHeartLoading />

<BitHourglassLoading />

<BitRingLoading />

<BitRippleLoading />

<BitRollerLoading />

<BitSpinnerLoading />";

    private readonly string example2RazorCode = @"
<BitGridLoading Label=""Loading"" />";

    private readonly string example3RazorCode = @"
<BitDotsRingLoading Label=""Top"" LabelPosition=""BitLabelPosition.Top"" />

<BitDotsRingLoading Label=""Bottom"" LabelPosition=""BitLabelPosition.Bottom"" />

<BitDotsRingLoading Label=""Start"" LabelPosition=""BitLabelPosition.Start"" />

<BitDotsRingLoading Label=""End"" LabelPosition=""BitLabelPosition.End"" />";

    private readonly string example4RazorCode = @"
<BitEllipsisLoading>
    <LabelTemplate>
        <div style=""color:green""><b>Loading</b></div>
    </LabelTemplate>
</BitEllipsisLoading>";

    private readonly string example5RazorCode = @"
<BitBarsLoading Label=""Primary"" Color=""BitColor.Primary"" />

<BitCircleLoading Label=""Secondary"" Color=""BitColor.Secondary"" />

<BitDotsRingLoading Label=""Tertiary"" Color=""BitColor.Tertiary"" />

<BitDualRingLoading Label=""Info"" Color=""BitColor.Info"" />

<BitEllipsisLoading Label=""Success"" Color=""BitColor.Success"" />

<BitGridLoading Label=""Warning"" Color=""BitColor.Warning"" />

<BitHeartLoading Label=""SevereWarning"" Color=""BitColor.SevereWarning"" />

<BitHourglassLoading Label=""Error"" Color=""BitColor.Error"" />";

    private readonly string example6RazorCode = @"
<BitBarsLoading Label=""brown"" CustomColor=""brown"" />

<BitCircleLoading Label=""rgb(0 107 185 / 75%)"" CustomColor=""rgb(0 107 185 / 75%)"" />

<BitDotsRingLoading Label=""#426985"" CustomColor=""#426985"" />

<BitDualRingLoading Label=""hsl(106 100% 22% / 1)"" CustomColor=""hsl(106 100% 22% / 1)"" />";

    private readonly string example7RazorCode = @"
<BitHourglassLoading Label=""Small"" Size=""BitSize.Small"" />

<BitHourglassLoading Label=""Medium"" Size=""BitSize.Medium"" />

<BitHourglassLoading Label=""Large"" Size=""BitSize.Large"" />

<BitHourglassLoading Label=""Custom (128)"" CustomSize=""128"" />";
}

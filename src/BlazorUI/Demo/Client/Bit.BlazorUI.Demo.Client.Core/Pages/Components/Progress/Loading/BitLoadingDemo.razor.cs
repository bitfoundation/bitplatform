namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Progress.Loading;

public partial class BitLoadingDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "ChildClass",
            Type = "string?",
            DefaultValue = "null",
            Description = "Custom CSS class for the root element of the loading component.",
        },
        new()
        {
            Name = "ChildStyle",
            Type = "string?",
            DefaultValue = "null",
            Description = "Custom CSS style for the root element of the loading component.",
        },
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
            LinkType = LinkType.Link,
            Href = "#size-enum",
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
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The Size of the loading component."
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
<BitBarsLoading Color=""BitColor.Primary"" />
                
<BitCircleLoading Color=""BitColor.Secondary"" />

<BitDotsRingLoading Color=""BitColor.Tertiary"" />

<BitDualRingLoading Color=""BitColor.Info"" />

<BitEllipsisLoading Color=""BitColor.Success"" />

<BitGridLoading Color=""BitColor.Warning"" />

<BitHeartLoading Color=""BitColor.SevereWarning"" />

<BitHourglassLoading Color=""BitColor.Error"" />";

    private readonly string example3RazorCode = @"
<BitBarsLoading CustomColor=""brown"" />

<BitCircleLoading CustomColor=""rgb(0 107 185 / 75%)"" />

<BitDotsRingLoading CustomColor=""#426985"" />

<BitDualRingLoading CustomColor=""hsl(106 100% 22% / 1)"" />";

    private readonly string example4RazorCode = @"
<BitBarsLoading Size=""BitSize.Small"" />

<BitBarsLoading Size=""BitSize.Medium"" />

<BitBarsLoading Size=""BitSize.Large"" />

<BitBarsLoading CustomSize=""128"" />";
}

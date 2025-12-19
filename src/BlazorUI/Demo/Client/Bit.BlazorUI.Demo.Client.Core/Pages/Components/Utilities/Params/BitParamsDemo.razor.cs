namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Params;

public partial class BitParamsDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content to which the values should be provided.",
        },
        new()
        {
            Name = "Parameters",
            Type = "IEnumerable<IBitComponentParams>?",
            DefaultValue = "null",
            Description = "List of parameters to provide for the children components.",
            LinkType = LinkType.Link,
            Href = "#component-params",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "component-params",
            Title = "IBitComponentParams",
            Description = "Defines the contract for parameters that can be cascaded by BitParams.",
            Parameters =
            [
                new()
                {
                    Name = "Name",
                    Type = "string",
                    DefaultValue = "",
                    Description = "Gets the name associated with the current instance of BitComponentParams.",
                }
            ]
        },
        new()
        {
            Id = "bit-component-base-params",
            Title = "BitComponentBaseParams",
            Description = "Base class shared by BitParams models to map BitComponentBase defaults.",
            Parameters =
            [
                new()
                {
                    Name = "AriaLabel",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Gets or sets the accessible label for the component, used by assistive technologies.",
                },
                new()
                {
                    Name = "Class",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Gets or sets the CSS class name(s) to apply to the rendered element.",
                },
                new()
                {
                    Name = "Dir",
                    Type = "BitDir?",
                    DefaultValue = "null",
                    Description = "Gets or sets the text directionality for the component's content.",
                },
                new()
                {
                    Name = "HtmlAttributes",
                    Type = "Dictionary<string, object>?",
                    DefaultValue = "null",
                    Description = "Captures additional HTML attributes to be applied to the rendered element, in addition to the component's parameters.",
                },
                new()
                {
                    Name = "Id",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Gets or sets the unique identifier for the component's root element.",
                },
                new()
                {
                    Name = "IsEnabled",
                    Type = "bool?",
                    DefaultValue = "true",
                    Description = "Gets or sets a value indicating whether the component is enabled and can respond to user interaction.",
                },
                new()
                {
                    Name = "Style",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Gets or sets the CSS style string to apply to the rendered element.",
                },
                new()
                {
                    Name = "TabIndex",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Gets or sets the tab order index for the component when navigating with the keyboard.",
                },
                new()
                {
                    Name = "Visibility",
                    Type = "BitVisibility?",
                    DefaultValue = "null",
                    Description = "Gets or sets the visibility state (visible, hidden, or collapsed) of the component.",
                }
            ]
        }
    ];



    private readonly List<IBitComponentParams> basicParams =
    [
        new BitCardParams { Background = BitColorKind.Tertiary, FullWidth = true, Style = "padding: 3rem" },
        new BitTagParams { Color = BitColor.Tertiary, Variant = BitVariant.Fill, Size = BitSize.Large },
        new BitTextParams { Typography = BitTypography.H5, Color = BitColor.Secondary, Gutter = true },
    ];



    private bool useAltParameters;

    private readonly List<IBitComponentParams> altParams =
    [
        new BitCardParams { Background = BitColorKind.Tertiary, FullWidth = false, Style = "padding: 1rem" },
        new BitTagParams { Color = BitColor.Secondary, Variant = BitVariant.Outline, Size = BitSize.Small },
        new BitTextParams { Typography = BitTypography.H4, Color = BitColor.Primary, Gutter = true },
    ];



    private readonly List<IBitComponentParams> nestedParentParams =
    [
        new BitCardParams { Background = BitColorKind.Tertiary, FullWidth = true, Style = "padding: 1rem" },
        new BitTagParams { Color = BitColor.Primary, Variant = BitVariant.Outline, Size = BitSize.Medium },
        new BitTextParams { Typography = BitTypography.Body2, Color = BitColor.PrimaryForeground }
    ];

    private readonly List<IBitComponentParams> nestedChildParams =
    [
        new BitCardParams { Background = BitColorKind.Secondary, FullWidth = true, Style = "padding: 0.75rem" },
        new BitTagParams { Color = BitColor.Warning, Variant = BitVariant.Fill, Size = BitSize.Small },
        new BitTextParams { Typography = BitTypography.Body2, Color = BitColor.Warning }
    ];



    private readonly string example1RazorCode = @"
<BitParams Parameters=""basicParams"">
    <BitCard>
        <BitText>BitText with provided parameters</BitText>
        <br />
        <BitTag>BitTag with provided parameters</BitTag>
    </BitCard>
</BitParams>";
    private readonly string example1CsharpCode = @"
private readonly List<IBitComponentParams> basicParams =
[
    new BitCardParams { Background = BitColorKind.Tertiary, FullWidth = true, Style = ""padding: 3rem"" },
    new BitTagParams { Color = BitColor.Tertiary, Variant = BitVariant.Fill, Size = BitSize.Large },
    new BitTextParams { Typography = BitTypography.H5, Color = BitColor.Secondary, Gutter = true },
];";

    private readonly string example2RazorCode = @"
<BitParams Parameters=""basicParams"">
    <BitCard Style=""padding:1rem"">
        <BitText Color=""BitColor.Primary"">
            BitText with provided and overriden parameters
        </BitText>
        <br />
        <BitTag Color=""BitColor.Secondary"">
            BitTag with provided and overriden parameters
        </BitTag>
    </BitCard>
</BitParams>";
    private readonly string example2CsharpCode = @"
private readonly List<IBitComponentParams> basicParams =
[
    new BitCardParams { Background = BitColorKind.Tertiary, FullWidth = true, Style = ""padding: 3rem"" },
    new BitTagParams { Color = BitColor.Tertiary, Variant = BitVariant.Fill, Size = BitSize.Large },
    new BitTextParams { Typography = BitTypography.H5, Color = BitColor.Secondary, Gutter = true },
];";

    private readonly string example3RazorCode = @"
<BitToggle @bind-Value=""useAltParameters"" Text=""Use alternate parameters"" />

<BitParams Parameters=""@(useAltParameters? altParams : basicParams)"">
    <BitCard>
        <BitText>BitText with provided parameters</BitText>
        <br />
        <BitTag>BitTag with provided parameters</BitTag>
    </BitCard>
</BitParams>";
    private readonly string example3CsharpCode = @"
private bool useAltParameters;

private readonly List<IBitComponentParams> basicParams =
[
    new BitCardParams { Background = BitColorKind.Tertiary, FullWidth = true, Style = ""padding: 3rem"" },
    new BitTagParams { Color = BitColor.Tertiary, Variant = BitVariant.Fill, Size = BitSize.Large },
    new BitTextParams { Typography = BitTypography.H5, Color = BitColor.Secondary, Gutter = true },
];

private readonly List<IBitComponentParams> altParams =
[
    new BitCardParams { Background = BitColorKind.Tertiary, FullWidth = false, Style = ""padding: 1rem"" },
    new BitTagParams { Color = BitColor.Secondary, Variant = BitVariant.Outline, Size = BitSize.Small },
    new BitTextParams { Typography = BitTypography.H4, Color = BitColor.Primary, Gutter = true },
];";

    private readonly string example4RazorCode = @"
<BitParams Parameters=""nestedParentParams"">
    <BitCard Style=""padding:1rem"">
        <BitText Typography=""BitTypography.H6"">Outer defaults</BitText>
        <BitText>These tags use the parent BitParams values.</BitText>
        <BitTag IconName=""@BitIconName.Globe"">Global</BitTag>
        <BitTag IconName=""@BitIconName.People"">Team</BitTag>

        <BitParams Parameters=""nestedChildParams"">
            <BitCard Style=""margin-top: 0.75rem; padding: 0.75rem"">
                <BitText Typography=""BitTypography.H6"">Nested overrides</BitText>
                <BitText>Inner BitParams changes colors and variants for this scope.</BitText>
                <BitTag IconName=""@BitIconName.Warning"">Alert</BitTag>
                <BitTag IconName=""@BitIconName.FavoriteStar"">Highlight</BitTag>
            </BitCard>
        </BitParams>
    </BitCard>
</BitParams>";
    private readonly string example4CsharpCode = @"
private readonly List<IBitComponentParams> nestedParentParams =
[
    new BitCardParams { Background = BitColorKind.Tertiary, FullWidth = true, Style = ""padding: 1rem"" },
    new BitTagParams { Color = BitColor.Primary, Variant = BitVariant.Outline, Size = BitSize.Medium },
    new BitTextParams { Typography = BitTypography.Body2, Color = BitColor.PrimaryForeground }
];

private readonly List<IBitComponentParams> nestedChildParams =
[
    new BitCardParams { Background = BitColorKind.Secondary, FullWidth = true, Style = ""padding: 0.75rem"" },
    new BitTagParams { Color = BitColor.Warning, Variant = BitVariant.Fill, Size = BitSize.Small },
    new BitTextParams { Typography = BitTypography.Body2, Color = BitColor.Warning }
];";
}

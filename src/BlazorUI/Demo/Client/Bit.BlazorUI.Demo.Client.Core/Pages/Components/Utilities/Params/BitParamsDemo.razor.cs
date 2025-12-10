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
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "ibit-component-params",
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



    private readonly string example1RazorCode = @"
<BitParams Parameters=""basicParams"">
    <div class=""bit-params-demo-grid"">
        <BitCard Class=""bit-params-demo-card"">
            <BitText Typography=""BitTypography.H6"">Release tags</BitText>
            <BitText>Tags and texts below inherit color, variant, and typography from BitParams.</BitText>
            <div class=""bit-params-demo-tags"">
                <BitTag IconName=""@BitIconName.Rocket"">Launch</BitTag>
                <BitTag IconName=""@BitIconName.Accept"">Ready</BitTag>
                <BitTag IconName=""@BitIconName.Tag"">Label</BitTag>
            </div>
        </BitCard>

        <BitCard Class=""bit-params-demo-card"">
            <BitText Typography=""BitTypography.H6"">Team</BitText>
            <BitText>Shared defaults keep typography and tag styling consistent.</BitText>
            <div class=""bit-params-demo-tags"">
                <BitTag IconName=""@BitIconName.Contact"">Owner</BitTag>
                <BitTag IconName=""@BitIconName.People"">Contributors</BitTag>
            </div>
        </BitCard>
    </div>
</BitParams>";

    private readonly string example1CsharpCode = @"
private readonly List<IBitComponentParams> basicParams =
[
    new BitTagParams { Color = BitColor.Primary, Variant = BitVariant.Fill, Size = BitSize.Medium },
    new BitTextParams { Typography = BitTypography.Body2, Color = BitColor.Secondary, Gutter = true },
    new BitCardParams { FullWidth = true, Style = ""padding: 1rem"" }
];";



    private readonly string example2RazorCode = @"
<BitParams Parameters=""overrideParams"">
    <BitCard Class=""bit-params-demo-card"">
        <BitText Typography=""BitTypography.H6"">Per-instance overrides</BitText>
        <div class=""bit-params-demo-tags"">
            <BitTag>Inherit</BitTag>
            <BitTag Color=""BitColor.Success"" Variant=""BitVariant.Outline"">Explicit color</BitTag>
            <BitTag Variant=""BitVariant.Text"" IconName=""@BitIconName.Info"">Text variant</BitTag>
            <BitTag Color=""BitColor.Error"" IconName=""@BitIconName.Error"">Critical</BitTag>
        </div>
        <BitText>Unspecified properties still come from the cascaded BitTagParams defaults.</BitText>
    </BitCard>
</BitParams>";

    private readonly string example2CsharpCode = @"
private readonly List<IBitComponentParams> overrideParams =
[
    new BitTagParams { Color = BitColor.Primary, Variant = BitVariant.Fill, Size = BitSize.Medium },
    new BitTextParams { Typography = BitTypography.Body2, Color = BitColor.Secondary }
];";



    private readonly string example3RazorCode = @"
<BitStack Gap=""0.75rem"">
    <BitToggle @bind-Value=""useAltPreset"" Text=""Use alternate preset"" />

    <BitParams Parameters=""ActiveParams"">
        <BitCard Class=""bit-params-demo-card"">
            <BitText Typography=""BitTypography.H6"">Preset-aware content</BitText>
            <BitText>Flip the toggle to swap the cascaded parameter set.</BitText>
            <div class=""bit-params-demo-tags"">
                <BitTag IconName=""@BitIconName.Heart"">Favorite</BitTag>
                <BitTag IconName=""@BitIconName.Pinned"">Pinned</BitTag>
                <BitTag IconName=""@BitIconName.Emoji2"">Mood</BitTag>
            </div>
        </BitCard>
    </BitParams>
</BitStack>";

    private readonly string example3CsharpCode = @"
private readonly List<IBitComponentParams> defaultPresetParams =
[
    new BitTagParams { Color = BitColor.Primary, Variant = BitVariant.Fill, Size = BitSize.Medium },
    new BitTextParams { Typography = BitTypography.Body2, Color = BitColor.Secondary },
    new BitCardParams { FullWidth = true, Style = ""padding: 1rem"" }
];

private readonly List<IBitComponentParams> alternatePresetParams =
[
    new BitTagParams { Color = BitColor.Tertiary, Variant = BitVariant.Outline, Size = BitSize.Medium },
    new BitTextParams { Typography = BitTypography.Body2, Color = BitColor.PrimaryForeground },
    new BitCardParams { FullWidth = true, Background = BitColorKind.Tertiary, Style = ""padding: 1rem"" }
];

private bool useAltPreset;

private IEnumerable<IBitComponentParams> ActiveParams => useAltPreset ? alternatePresetParams : defaultPresetParams;";
}

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Params;

public partial class BitParamsDemo
{
    private readonly string example1RazorCode = @"
<BitParams Parameters=""basicParams"">
    <BitStack Horizontal Wrap VerticalAlign=""BitAlignment.Center"">
        <BitButton IconName=""@BitIconName.Save"">Save</BitButton>
        <BitButton IconName=""@BitIconName.Share"">Share</BitButton>
        <BitButton IconName=""@BitIconName.Delete"" Variant=""BitVariant.Text"">Delete</BitButton>
    </BitStack>
    <br />
    <BitStack Horizontal Wrap>
        <BitTag Text=""Draft"" />
        <BitTag Text=""Internal"" />
    </BitStack>
</BitParams>";
    private readonly string example1CsharpCode = @"
private readonly List<IBitComponentParams> basicParams =
[
    new BitButtonParams { Variant = BitVariant.Outline, Color = BitColor.Tertiary, Size = BitSize.Small },
    new BitTagParams { Variant = BitVariant.Fill, Color = BitColor.Tertiary, Size = BitSize.Small },
];";

    private readonly string example2RazorCode = @"
<BitToggle @bind-Value=""isCompact"" Label=""Compact"" Inline />

<BitParams Parameters=""@([new BitButtonParams { Size = isCompact ? BitSize.Small : BitSize.Large },
                         new BitTagParams { Size = isCompact ? BitSize.Small : BitSize.Large }])"">
    <BitStack Horizontal Wrap VerticalAlign=""BitAlignment.Center"">
        <BitButton>Accept</BitButton>
        <BitButton Variant=""BitVariant.Outline"">Decline</BitButton>
        <BitTag Text=""Pending"" Color=""BitColor.Warning"" />
    </BitStack>
</BitParams>";
    private readonly string example2CsharpCode = @"
private bool isCompact = true;";

    private readonly string example3RazorCode = @"
<BitParams Parameters=""outerParams"">
    <BitStack Gap=""1rem"">
        <BitStack Horizontal Wrap VerticalAlign=""BitAlignment.Center"">
            <BitText>Outer:</BitText>
            <BitButton>Edit</BitButton>
            <BitButton>Copy</BitButton>
        </BitStack>

        <BitParams Parameters=""dangerParams"">
            <BitStack Horizontal Wrap VerticalAlign=""BitAlignment.Center"">
                <BitText>Nested (Color only):</BitText>
                <BitButton>Delete</BitButton>
                <BitButton>Purge</BitButton>
            </BitStack>
        </BitParams>

        <BitParams Isolated>
            <BitStack Horizontal Wrap VerticalAlign=""BitAlignment.Center"">
                <BitText>Isolated:</BitText>
                <BitButton>Defaults</BitButton>
            </BitStack>
        </BitParams>
    </BitStack>
</BitParams>";
    private readonly string example3CsharpCode = @"
private readonly List<IBitComponentParams> outerParams =
[
    new BitButtonParams { Variant = BitVariant.Outline, Size = BitSize.Small },
];

private readonly List<IBitComponentParams> dangerParams =
[
    new BitButtonParams { Color = BitColor.Error },
];";

    private readonly string example4RazorCode = @"
<BitToggle @bind-Value=""isSaving"" Label=""Saving"" Inline />
<BitToggle @bind-Value=""isViewOnly"" Label=""View only"" Inline />
<BitToggle @bind-Value=""isRtl"" Label=""RTL"" Inline />

<BitParams Dir=""@(isRtl ? BitDir.Rtl : null)"" IsEnabled=""@(isSaving is false)"" ReadOnly=""isViewOnly"">
    <BitStack Gap=""0.75rem"" Style=""max-width: 20rem"">
        <BitTextField Label=""Name"" @bind-Value=""formName"" />
        <BitNumberField Label=""Quantity"" @bind-Value=""formQuantity"" Mode=""BitSpinButtonMode.Inline"" />
        <BitCheckbox Label=""Gift wrap"" @bind-Value=""formGiftWrap"" />
        <BitStack Horizontal>
            <BitButton>Submit</BitButton>
            <BitButton IsEnabled Variant=""BitVariant.Outline"" OnClick=""() => isSaving = false"">Cancel</BitButton>
        </BitStack>
    </BitStack>
</BitParams>";
    private readonly string example4CsharpCode = @"
private bool isSaving;
private bool isViewOnly;
private bool isRtl;
private string? formName = ""Ada"";
private int formQuantity = 1;
private bool formGiftWrap;";
}

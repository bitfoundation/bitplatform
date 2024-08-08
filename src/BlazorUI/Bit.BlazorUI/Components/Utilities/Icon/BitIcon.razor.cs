namespace Bit.BlazorUI;

public partial class BitIcon : BitComponentBase
{
    /// <summary>
    /// The general color of the icon.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// The icon name for the icon shown.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public string? IconName { get; set; }

    /// <summary>
    /// The size of the icon.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }



    protected override string RootElementClass => "bit-ico";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-ico-pri",
            BitColor.Secondary => "bit-ico-sec",
            BitColor.Tertiary => "bit-ico-ter",
            BitColor.Info => "bit-ico-inf",
            BitColor.Success => "bit-ico-suc",
            BitColor.Warning => "bit-ico-wrn",
            BitColor.SevereWarning => "bit-ico-swr",
            BitColor.Error => "bit-ico-err",
            _ => "bit-ico-pri"
        });

        ClassBuilder.Register(() => IconName.HasValue() ? $"bit-icon bit-icon--{IconName}" : string.Empty);

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-ico-sm",
            BitSize.Medium => "bit-ico-md",
            BitSize.Large => "bit-ico-lg",
            _ => "bit-ico-md"
        });
    }
}

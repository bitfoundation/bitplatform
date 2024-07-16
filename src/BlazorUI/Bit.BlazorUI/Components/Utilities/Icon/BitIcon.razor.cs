namespace Bit.BlazorUI;

public partial class BitIcon : BitComponentBase
{
    /// <summary>
    /// The icon name for the icon shown.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public string? IconName { get; set; }

    /// <summary>
    /// The severity of the icon.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSeverity? Severity { get; set; }

    /// <summary>
    /// The size of the icon.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }



    protected override string RootElementClass => "bit-ico";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => IconName.HasValue() ? $"bit-icon bit-icon--{IconName}" : string.Empty);

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-ico-sm",
            BitSize.Medium => "bit-ico-md",
            BitSize.Large => "bit-ico-lg",
            _ => string.Empty
        });

        ClassBuilder.Register(() => Severity switch
        {
            BitSeverity.Info => "bit-ico-inf",
            BitSeverity.Success => "bit-ico-suc",
            BitSeverity.Warning => "bit-ico-wrn",
            BitSeverity.SevereWarning => "bit-ico-swr",
            BitSeverity.Error => "bit-ico-err",
            _ => string.Empty
        });
    }
}

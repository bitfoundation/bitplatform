namespace Bit.BlazorUI;

/// <summary>
/// A component for displaying icons that enhance visual communication and user experience.
/// Supports both built-in Fluent UI icons and external icon libraries.
/// </summary>
public partial class BitIcon : BitComponentBase
{
    /// <summary>
    /// Specifies the color theme of the icon.
    /// Default value is <see cref="BitColor.Primary"/>.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// Specifies the icon configuration for rendering icons from external icon libraries using custom CSS classes.
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property for external icon libraries such as FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use the <see cref="IconName"/> property instead.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// Specifies the name of the icon from the built-in Fluent UI icon library.
    /// This property is ignored when <see cref="Icon"/> is set.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public string? IconName { get; set; }

    /// <summary>
    /// Specifies the size of the icon.
    /// Default value is <see cref="BitSize.Medium"/>.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Specifies the visual styling variant of the icon.
    /// Default value is <see cref="BitVariant.Text"/>.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitVariant? Variant { get; set; }



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
            BitColor.PrimaryBackground => "bit-ico-pbg",
            BitColor.SecondaryBackground => "bit-ico-sbg",
            BitColor.TertiaryBackground => "bit-ico-tbg",
            BitColor.PrimaryForeground => "bit-ico-pfg",
            BitColor.SecondaryForeground => "bit-ico-sfg",
            BitColor.TertiaryForeground => "bit-ico-tfg",
            BitColor.PrimaryBorder => "bit-ico-pbr",
            BitColor.SecondaryBorder => "bit-ico-sbr",
            BitColor.TertiaryBorder => "bit-ico-tbr",
            _ => "bit-ico-pri"
        });

        ClassBuilder.Register(() => BitIconInfo.From(Icon, IconName)?.GetCssClasses());

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-ico-sm",
            BitSize.Medium => "bit-ico-md",
            BitSize.Large => "bit-ico-lg",
            _ => "bit-ico-md"
        });

        ClassBuilder.Register(() => Variant switch
        {
            BitVariant.Fill => "bit-ico-fil",
            BitVariant.Outline => "bit-ico-out",
            BitVariant.Text => "bit-ico-txt",
            _ => "bit-ico-txt"
        });
    }
}

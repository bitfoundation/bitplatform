namespace Bit.BlazorUI;

public partial class BitIcon : BitComponentBase
{
    private BitSize? size;
    private BitColor? color;
    private string? iconName;



    /// <summary>
    /// The color of icon.
    /// </summary>
    [Parameter]
    public BitColor? Color
    {
        get => color;
        set
        {
            if (color == value) return;

            color = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// The icon name for the icon shown.
    /// </summary>
    [Parameter]
    public string? IconName
    {
        get => iconName;
        set
        {
            if (iconName == value) return;

            iconName = value;

            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// The size of the icon.
    /// </summary>
    [Parameter]
    public BitSize? Size
    {
        get => size;
        set
        {
            if (size == value) return;

            size = value;

            ClassBuilder.Reset();
        }
    }

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

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Info => "bit-ico-inf",
            BitColor.Success => "bit-ico-suc",
            BitColor.Warning => "bit-ico-wrn",
            BitColor.SevereWarning => "bit-ico-swr",
            BitColor.Error => "bit-ico-err",
            _ => string.Empty
        });
    }
}

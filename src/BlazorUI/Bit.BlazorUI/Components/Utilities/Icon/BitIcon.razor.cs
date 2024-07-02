namespace Bit.BlazorUI;

public partial class BitIcon : BitComponentBase
{
    private BitSize? size;
    private string? iconName;
    private BitSeverity? severity;



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
    /// The severity of the icon.
    /// </summary>
    [Parameter]
    public BitSeverity? Severity
    {
        get => severity;
        set
        {
            if (severity == value) return;

            severity = value;
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

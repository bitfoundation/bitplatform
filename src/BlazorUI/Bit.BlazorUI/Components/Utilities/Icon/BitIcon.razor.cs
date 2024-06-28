namespace Bit.BlazorUI;

public partial class BitIcon
{
    private string? iconName;
    private BitIconSize size = BitIconSize.Medium;

    /// <summary>
    /// The icon name for the icon shown in the button
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
    /// Size of icon.
    /// </summary>
    [Parameter]
    public BitIconSize Size
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
            BitIconSize.Small => "bit-ico-sm",
            BitIconSize.Medium => "bit-ico-md",
            BitIconSize.Large => "bit-ico-lg",
            _ => string.Empty
        });
    }
}

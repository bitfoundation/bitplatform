namespace Bit.BlazorUI;

public partial class BitIcon
{
    private string? iconName;

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

    protected override string RootElementClass => "bit-ico";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => IconName.HasValue() ? $"bit-icon bit-icon--{IconName}" : string.Empty);
    }
}

namespace Bit.BlazorUI;

public partial class BitIcon
{
    /// <summary>
    /// The icon name for the icon shown in the button
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    protected override string RootElementClass => "bit-ico";

    private string? GetIconClass()
    {
        return IconName.HasValue() ? $"bit-icon bit-icon--{IconName} {ClassBuilder.Value}" : ClassBuilder.Value;
    }
}

namespace Bit.BlazorUI;

public partial class BitIcon
{
    /// <summary>
    /// The icon name for the icon shown in the button
    /// </summary>
    [Parameter] public BitIconName IconName { get; set; }

    protected override string RootElementClass => "bit-icon-component";
}

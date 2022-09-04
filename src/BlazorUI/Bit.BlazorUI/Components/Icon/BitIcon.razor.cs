namespace Bit.BlazorUI;

public partial class BitIcon
{
    private BitIconName _internalIcon;
    private string _internalIconName = string.Empty;

    /// <summary>
    /// The icon name for the icon shown in the button
    /// </summary>
    [Parameter] public BitIconName Icon { get; set; }

    protected override string RootElementClass => "bit-ico";

    protected override Task OnParametersSetAsync()
    {
        if (_internalIcon != Icon)
        {
            _internalIcon = Icon;
            _internalIconName = _internalIcon.GetName()!;
        }

        return base.OnParametersSetAsync();
    }
}

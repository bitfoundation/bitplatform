namespace Bit.BlazorUI;

public partial class BitIcon
{
    private BitIconName _internalIcon;
    private string _internalIconName = string.Empty;

    /// <summary>
    /// The icon name for the icon shown in the button
    /// </summary>
    [Parameter] public BitIconName IconName { get; set; }

    protected override string RootElementClass => "bit-ico";

    protected override Task OnInitializedAsync()
    {
        ClassBuilder.Register(() => $"bit-icon bit-icon--{_internalIconName}");

        return base.OnInitializedAsync();
    }

    protected override Task OnParametersSetAsync()
    {
        if (_internalIcon != IconName)
        {
            _internalIcon = IconName;
            _internalIconName = _internalIcon.GetName()!;
            ClassBuilder.Reset();
        }

        return base.OnParametersSetAsync();
    }
}

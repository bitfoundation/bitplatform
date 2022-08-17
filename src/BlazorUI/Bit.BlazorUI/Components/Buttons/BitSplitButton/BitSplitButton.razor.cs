
namespace Bit.BlazorUI;

public partial class BitSplitButton
{
    private BitSplitButtonItem Item = new();
    private bool IsOpenMenu;
    private string? SplitButtonId;
    private string? SplitButtonCalloutId;
    private string? SplitButtonOverlayId;

    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;

    /// <summary>
    /// 
    /// </summary>
    [Parameter] public string? AriaRoleDescription { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [Parameter] public IEnumerable<BitSplitButtonItem> Items { get; set; } = new List<BitSplitButtonItem>();

    /// <summary>
    /// The style of button, Possible values: Primary | Standard
    /// </summary>
    [Parameter]
    public BitButtonStyle ButtonStyle { get; set; } = BitButtonStyle.Primary;

    protected override string RootElementClass => "bit-splt-btn";

    protected override async Task OnInitializedAsync()
    {
        var item = Items.FirstOrDefault(i => i.DefaultIsSelected);

        if (item is not null)
        {
            Item = item;
        }
        else
        {
            Item = Items.First();
        }

        SplitButtonId = $"SplitButton-{UniqueId}";
        SplitButtonCalloutId = $"SplitButton-Callout-{UniqueId}";
        SplitButtonOverlayId = $"SplitButton-Overlay-{UniqueId}";

         await base.OnInitializedAsync();
    }

    private string GetButtonClasses()
    {
        if (IsEnabled is false) return string.Empty;

        return ButtonStyle is BitButtonStyle.Primary ? "primary" : "standard";
    }

    private void HandleOnClick(BitSplitButtonItem item)
    {
        if (item.IsEnabled is false || IsEnabled is false) return;

        if (item.OnClick is not null)
        {
            item.OnClick.Invoke();
        }
    }

    private async Task HandleMenuOpen()
    {
        if (IsEnabled is false) return;

        var obj = DotNetObjectReference.Create(this);
        await JSRuntime.InvokeVoidAsync("BitSplitButton.toggleSplitButtonCallout", obj, UniqueId, SplitButtonId, SplitButtonCalloutId, SplitButtonOverlayId, IsOpenMenu, true);

        IsOpenMenu = !IsOpenMenu;
    }

    private async Task HandleItemOnClick(BitSplitButtonItem item)
    {
        if (item.IsEnabled is false) return;

        Item = item;

        var obj = DotNetObjectReference.Create(this);
        await JSRuntime.InvokeVoidAsync("BitSplitButton.toggleSplitButtonCallout", obj, UniqueId, SplitButtonId, SplitButtonCalloutId, SplitButtonOverlayId, IsOpenMenu, true);
        
        IsOpenMenu = false;
    }

    //[JSInvokable("CloseCallout")]
    //public void CloseCalloutBeforeAnotherCalloutIsOpened()
    //{
    //    IsOpenMenu = false;
    //}

    private async Task CloseCallout()
    {
        var obj = DotNetObjectReference.Create(this);
        await JSRuntime.InvokeVoidAsync("BitSplitButton.toggleSplitButtonCallout", obj, UniqueId, SplitButtonId, SplitButtonCalloutId, SplitButtonOverlayId, IsOpenMenu, true);
        IsOpenMenu = false;
    }
}

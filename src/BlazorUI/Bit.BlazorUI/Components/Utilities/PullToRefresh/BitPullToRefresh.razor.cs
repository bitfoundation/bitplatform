
namespace Bit.BlazorUI;

public partial class BitPullToRefresh : BitComponentBase
{
    /// <summary>
    /// The content of the component.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    [Parameter] public string Anchor { get; set; } = "body";

    [Parameter] public int Threshold { get; set; } = 80;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    protected override string RootElementClass => "bit-ptr";

    protected override void RegisterCssClasses()
    {
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var dotnetObj = DotNetObjectReference.Create(this);
            await _js.BitPullToRefreshSetup(RootElement, Anchor, Threshold, dotnetObj);
        }

        await base.OnAfterRenderAsync(firstRender);
    }
}

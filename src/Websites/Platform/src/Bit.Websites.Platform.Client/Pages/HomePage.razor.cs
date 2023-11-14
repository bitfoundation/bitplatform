namespace Bit.Websites.Platform.Client.Pages;

public partial class HomePage
{
    [AutoInject] public IJSRuntime JSRuntime { get; set; } = default!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        /*if (firstRender)
        {
            await JSRuntime.InitTrustPilot();
        }*/
    }
}

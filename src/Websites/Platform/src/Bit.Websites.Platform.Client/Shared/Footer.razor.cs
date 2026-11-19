namespace Bit.Websites.Platform.Client.Shared;

public partial class Footer
{
    private async Task GoToTop()
    {
        await JSRuntime.InvokeVoidAsync("goToTop");
    }
}

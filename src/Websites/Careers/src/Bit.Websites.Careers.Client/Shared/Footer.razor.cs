namespace Bit.Websites.Careers.Client.Shared;

public partial class Footer
{

    private async Task BackToTop()
    {
        await JSRuntime.InvokeVoidAsync("App.backToTop");
    }
}

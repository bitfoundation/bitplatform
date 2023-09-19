namespace Bit.Websites.Careers.Web.Shared;

public partial class Footer
{

    private async Task BackToTop()
    {
        await JSRuntime.InvokeVoidAsync("App.backToTop");
    }
}

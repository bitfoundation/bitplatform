namespace Boilerplate.Client.Web.Services;

public partial class AdsService : IAdsService
{
    [AutoInject] private IJSRuntime jsRuntime = default!;


    public ValueTask Init<T>(string adUnitPath, DotNetObjectReference<T> dotnetObj) where T : class
    {
        return jsRuntime.InvokeVoidAsync("Ads.init", adUnitPath, dotnetObj);
    }

    public ValueTask Watch()
    {
        return jsRuntime.InvokeVoidAsync("Ads.watch");
    }
}

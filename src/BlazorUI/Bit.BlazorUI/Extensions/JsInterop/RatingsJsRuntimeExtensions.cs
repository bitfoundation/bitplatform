namespace Bit.BlazorUI;

internal static class RatingsJsRuntimeExtensions
{
    internal static ValueTask BitRatingsSetup(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Ratings.setup", id);
    }

    internal static ValueTask BitRatingsDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Ratings.dispose", id);
    }
}

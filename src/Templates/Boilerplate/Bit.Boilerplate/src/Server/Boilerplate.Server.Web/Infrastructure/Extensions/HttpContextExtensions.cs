namespace Microsoft.AspNetCore.Http;

public static class HttpContextExtensions
{
    /// <summary>
    /// Is CDN Edge or ASP.NET Core Output cache enabled for this response.
    /// </summary>
    public static bool IsSharedCacheEnabled(this HttpContext context)
    {
        if (context.Items.TryGetValue("AppResponseCachePolicy__SharedCacheEnabled", out var val) && val is true)
            return true;

        return false;
    }
}

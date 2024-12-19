namespace Boilerplate.Server.Api.Controllers;

public partial class AppControllerBase : ControllerBase
{
    [AutoInject] protected ServerApiSettings AppSettings = default!;

    [AutoInject] protected ServerApiSettings Settings = default!;

    [AutoInject] protected AppDbContext DbContext = default!;

    [AutoInject] protected IStringLocalizer<AppStrings> Localizer = default!;

    protected bool ValidateETag(string currentETag, out StatusCodeResult? statusCode)
    {
        statusCode = null;

        if (Request.Headers.TryGetValue("If-None-Match", out var requestETag) && requestETag == currentETag)
        {
            statusCode = StatusCode(StatusCodes.Status304NotModified);
            return true;
        }

        Response.Headers.ETag = currentETag;
        Response.Headers.CacheControl = $"public, no-cache, max-age={TimeSpan.FromDays(7).TotalSeconds}"; // no-cache doesn't mean don't cache, it means revalidate.

        return false;
    }
}

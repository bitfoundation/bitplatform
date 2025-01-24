namespace Boilerplate.Server.Api.Controllers;

public partial class AppControllerBase : ControllerBase
{
    [AutoInject] protected ServerApiSettings AppSettings = default!;

    [AutoInject] protected AppDbContext DbContext = default!;

    [AutoInject] protected IStringLocalizer<AppStrings> Localizer = default!;

    protected void SetCloudflareCache(string tag, TimeSpan? sharedMaxAge = null)
    {
        sharedMaxAge ??= TimeSpan.FromDays(7);

        Response.Headers["Cache-Tag"] = tag;

        Response.GetTypedHeaders().CacheControl = new()
        {
            Public = true,
            SharedMaxAge = sharedMaxAge
        };
    }
}

namespace Boilerplate.Server.Api.Infrastructure.RequestPipeline;

public class ForceUpdateMiddleware(RequestDelegate next, ServerApiSettings settings)
{
    private readonly RequestDelegate next = next;
    private readonly ServerApiSettings settings = settings;

    public async Task InvokeAsync(HttpContext context)
    {
        // A page request is let through: the app it loads shows the force update ui itself, once its own api calls are
        // refused here. Refusing the document instead would put this exception's json in front of the user.
        if (context.Request.IsPageRequest() is false
            && Version.TryParse(context.Request.Headers["X-App-Version"].FirstOrDefault(), out var appVersion)
            && Enum.TryParse<AppPlatformType>(context.Request.Headers["X-App-Platform"].FirstOrDefault(), ignoreCase: true, out var appPlatformType)
            && Enum.IsDefined(appPlatformType) // TryParse also accepts numeric strings, e.g. "999", which are not real platforms.
            && settings.SupportedAppVersions?.GetMinimumSupportedAppVersion(appPlatformType) is { } minVersion
            && appVersion < minVersion)
        {
            throw new ClientNotSupportedException().WithData("Reason", $"The client version '{appVersion}' is not supported. Minimum supported version is '{minVersion}'.");
        }

        await next(context);
    }
}

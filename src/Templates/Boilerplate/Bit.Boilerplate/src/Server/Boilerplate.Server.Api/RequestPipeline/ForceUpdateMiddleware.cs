namespace Boilerplate.Server.Api.RequestPipeline;

public class ForceUpdateMiddleware(RequestDelegate next, ServerApiSettings settings)
{
    private readonly RequestDelegate next = next;
    private readonly ServerApiSettings settings = settings;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-App-Version", out var appVersionHeaderValue)
            && appVersionHeaderValue.Any())
        {
            var appVersion = appVersionHeaderValue.Single()!;
            var appPlatformType = Enum.Parse<AppPlatformType>(context.Request.Headers["X-App-Platform"].Single()!);
            var minVersion = settings.SupportedAppVersions!.GetMinimumSupportedAppVersion(appPlatformType);
            if (minVersion != null && Version.Parse(appVersion) < minVersion)
            {
                throw new ClientNotSupportedException();
            }
        }

        await next(context);
    }
}

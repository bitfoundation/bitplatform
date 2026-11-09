namespace Boilerplate.Server.Api.Infrastructure.RequestPipeline;

public class ForceUpdateMiddleware(RequestDelegate next, ServerApiSettings settings)
{
    private static readonly Version serverVersion = typeof(ForceUpdateMiddleware).Assembly.GetName().Version!;

    private readonly RequestDelegate next = next;
    private readonly ServerApiSettings settings = settings;

    public async Task InvokeAsync(HttpContext context)
    {
        if (Version.TryParse(context.Request.Headers["X-App-Version"].FirstOrDefault(), out var appVersion)
            && Enum.TryParse<AppPlatformType>(context.Request.Headers["X-App-Platform"].FirstOrDefault(), ignoreCase: true, out var appPlatformType)
            && Enum.IsDefined(appPlatformType)) // TryParse also accepts numeric strings, e.g. "999", which are not real platforms.
        {
            // A minimum set for the platform wins. Without one, a client is supported unless its major version is behind the server's.
            var minVersion = settings.SupportedAppVersions?.GetMinimumSupportedAppVersion(appPlatformType) ?? MinimumForMajorOf(serverVersion);

            if (appVersion < minVersion)
            {
                throw new ClientNotSupportedException().WithData("Reason", $"The client version '{appVersion}' is not supported. Minimum supported version is '{minVersion}'.");
            }
        }

        await next(context);
    }

    public static Version MinimumForMajorOf(Version serverVersion) => new(serverVersion.Major, 0);
}

using Microsoft.AspNetCore.Mvc.Filters;

namespace Boilerplate.Server.Api.Filters;

public partial class ForceUpdateActionFilter : ActionFilterAttribute
{
    [AutoInject] private ServerApiSettings settings = default!;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.HttpContext.Request.Headers.TryGetValue("X-App-Version", out var appVersionHeaderValue)
                && appVersionHeaderValue.Any())
        {
            var appVersion = appVersionHeaderValue.Single()!;
            var appPlatformType = Enum.Parse<AppPlatformType>(context.HttpContext.Request.Headers["X-App-Platform"].Single()!);
            var minimumSupportedVersion = settings.SupportedAppVersions!.GetMinimumSupportedAppVersion(appPlatformType);
            if (minimumSupportedVersion != null && Version.Parse(appVersion) < minimumSupportedVersion)
                throw new ClientNotSupportedException();
        }

        base.OnActionExecuting(context);
    }
}

//-:cnd:noEmit
using Boilerplate.Client.Core;
using Microsoft.Maui.LifecycleEvents;

namespace Boilerplate.Client.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        AppRenderMode.IsBlazorHybrid = true;

        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .Configuration.AddClientConfigurations();

        var services = builder.Services;

        services.AddClientMauiServices();

        builder.ConfigureLifecycleEvents(lifecycle =>
        {
#if iOS || Mac
            lifecycle.AddiOS(ios =>
            {
                bool HandleAppLink(Foundation.NSUserActivity? userActivity)
                {
                    if (userActivity is not null && userActivity.ActivityType == Foundation.NSUserActivityType.BrowsingWeb && userActivity.WebPageUrl is not null)
                    {
                        var url = $"{userActivity.WebPageUrl.Path}?{userActivity.WebPageUrl.Query}";

                        var _ = Routes.OpenUniversalLink(url);

                        return true;
                    }

                    return false;
                }

                ios.FinishedLaunching((app, data)
                    => HandleAppLink(app.UserActivity));

                ios.ContinueUserActivity((app, userActivity, handler)
                    => HandleAppLink(userActivity));

                if (OperatingSystem.IsIOSVersionAtLeast(13) || OperatingSystem.IsMacCatalystVersionAtLeast(13))
                {
                    ios.SceneWillConnect((scene, sceneSession, sceneConnectionOptions)
                        => HandleAppLink(sceneConnectionOptions.UserActivities.ToArray()
                            .FirstOrDefault(a => a.ActivityType == Foundation.NSUserActivityType.BrowsingWeb)));

                    ios.SceneContinueUserActivity((scene, userActivity)
                        => HandleAppLink(userActivity));
                }
            });
#endif
        });

        var mauiApp = builder.Build();

        return mauiApp;
    }
}

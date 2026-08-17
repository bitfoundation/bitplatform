//+:cnd:noEmit
namespace Boilerplate.Client.Core.Components;

public partial class Routes
{
    [Parameter] public Type? Layout { get; set; }

    [AutoInject]
    NavigationManager? navigationManager
    {
        set
        {
            if (value is not null)
            {
                current = value;
                NavigationManagerProvider.TrySetResult();
            }
        }
        get;
    }
    private static NavigationManager? current;
    private static readonly TaskCompletionSource NavigationManagerProvider = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public static async Task OpenUniversalLink(string url, bool forceLoad = false, bool replace = false)
    {
        await NavigationManagerProvider.Task;

        var navigationManager = current!;

        if (CultureInfoManager.InvariantGlobalization is false &&
            forceLoad == false &&
            (AppPlatform.IsAndroid || AppPlatform.IsIos))
        {
            var urlCulture = new Uri(new Uri(navigationManager.BaseUri), url).GetCulture();
            forceLoad = urlCulture is not null && string.Equals(CultureInfo.CurrentUICulture.Name, urlCulture, StringComparison.InvariantCultureIgnoreCase) is false;
        }

        navigationManager.NavigateTo(url, forceLoad, replace);
    }
}

/// <summary>
/// This class is only a workaround for limitations we faced in razor files inside .NET project templates.
/// You could simply delete it in your own project and use the base class directly in Routes.razor file.
/// </summary>
public class AppRouter :
    //#if (brouter == true)
    Brouter
//#else
//#if (IsInsideProjectTemplate == true)
/*
//#endif
Microsoft.AspNetCore.Components.Routing.Router
//#if (IsInsideProjectTemplate == true)
*/
//#endif
//#endif
{ }

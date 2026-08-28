//+:cnd:noEmit
namespace Boilerplate.Client.Core.Components;

public partial class Routes : ComponentBase, IDisposable
{
    [Parameter] public Type? Layout { get; set; }

    [AutoInject] private PubSubService pubSubService { get; set; } = default!;

    private string? currentCulture;
    private Action? unsubscribeCultureChanged;

    protected override void OnInitialized()
    {
        currentCulture = CultureInfo.CurrentUICulture.Name;

        unsubscribeCultureChanged = pubSubService.Subscribe(ClientAppMessages.CULTURE_CHANGED, payload => InvokeAsync(async () =>
        {
            currentCulture = payload as string ?? CultureInfo.CurrentUICulture.Name;
            StateHasChanged();
        }));

        base.OnInitialized();
    }

    public void Dispose()
    {
        unsubscribeCultureChanged?.Invoke();
        unsubscribeCultureChanged = null;
        GC.SuppressFinalize(this);
    }

    [AutoInject]
    IServiceProvider? serviceProvider
    {
        set
        {
            if (value is not null)
            {
                currentServiceProvider = value;
                ServiceProviderReady.TrySetResult();
            }
        }
        get;
    }
    private static IServiceProvider? currentServiceProvider
    {
        get
        {
            if (AppPlatform.IsBlazorHybridOrBrowser is false)
                throw new InvalidOperationException();

            return field;
        }
        set;
    }
    private static readonly TaskCompletionSource ServiceProviderReady = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public static async Task OpenUniversalLink(string url, bool forceLoad = false, bool replace = false)
    {
        if (Uri.IsAppRelativeUrl(url, requireLeadingSlash: false) is false)
        {
            url = PageUrls.Home;
        }

        await ServiceProviderReady.Task;

        var navigationManager = currentServiceProvider!.GetRequiredService<NavigationManager>();

        if (CultureInfoManager.InvariantGlobalization is false
            && navigationManager.ToAbsoluteUri(url).GetCulture() is string culture
            && string.IsNullOrEmpty(culture) is false
            && string.Equals(culture, CultureInfo.CurrentUICulture.Name, StringComparison.InvariantCultureIgnoreCase) is false)
        {
            CultureInfoManager.SetCurrentCulture(culture);
            currentServiceProvider!.GetRequiredService<PubSubService>().Publish(ClientAppMessages.CULTURE_CHANGED, culture, persistent: true);
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
{ }
//#else
//#if (IsInsideProjectTemplate == true)
/*
//#endif
Microsoft.AspNetCore.Components.Routing.Router
{
    public AppRouter() => NotFoundPage = typeof(Pages.NotFoundPage);
}
//#if (IsInsideProjectTemplate == true)
*/
//#endif
//#endif

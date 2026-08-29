//+:cnd:noEmit
namespace Boilerplate.Client.Core.Components;

public partial class Routes : ComponentBase, IDisposable
{
    [Parameter] public Type? Layout { get; set; }

    [AutoInject] private PubSubService pubSubService { get; set; } = default!;
    //#if (brouter == true)
    [AutoInject] private IBrouter brouter { get; set; } = default!;
    //#endif

    /// <summary>
    /// The @key of the whole component tree: bumping it throws the current tree away and builds a fresh one. A plain
    /// counter rather than the value behind the restart, so anything can ask for one without this component knowing
    /// what changed, and two different changes never collide on the same key.
    /// </summary>
    private int softRestartKey = 1;
    private Action? unsubscribeSoftRestart;

    protected override void OnInitialized()
    {
        unsubscribeSoftRestart = pubSubService.Subscribe(ClientAppMessages.SOFT_RESTART, _ => InvokeAsync(() =>
        {
            //#if (brouter == true)
            brouter.ClearLoaderCache();
            //#endif

            softRestartKey++;
            StateHasChanged();
        }));

        base.OnInitialized();
    }

    public void Dispose()
    {
        unsubscribeSoftRestart?.Invoke();
        unsubscribeSoftRestart = null;
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
            currentServiceProvider!.GetRequiredService<PubSubService>().Publish(ClientAppMessages.SOFT_RESTART);
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

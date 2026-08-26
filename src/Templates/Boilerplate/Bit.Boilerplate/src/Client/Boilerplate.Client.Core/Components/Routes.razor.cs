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
        if (Uri.IsAppRelativeUrl(url, requireLeadingSlash: false) is false)
        {
            url = PageUrls.Home;
        }

        await NavigationManagerProvider.Task;

        var navigationManager = current!;

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

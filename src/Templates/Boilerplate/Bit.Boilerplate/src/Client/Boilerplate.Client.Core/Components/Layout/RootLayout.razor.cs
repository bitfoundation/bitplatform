using System.Reflection;

namespace Boilerplate.Client.Core.Components.Layout;

public partial class RootLayout : IDisposable
{
    private BitDir? currentDir;
    private string? currentUrl;
    private bool? isAuthenticated;
    private bool? isCrossLayoutPage;
    private AppThemeType? currentTheme;
    private RouteData? currentRouteData;
    private Action unsubscribeThemeChange = default!;
    private Action unsubscribeCultureChange = default!;
    private Action unsubscribeRouteDataUpdated = default!;


    [AutoInject] private Keyboard keyboard = default!;
    [AutoInject] private ThemeService themeService = default!;
    [AutoInject] private PubSubService pubSubService = default!;
    [AutoInject] private AuthenticationManager authManager = default!;
    [AutoInject] private IExceptionHandler exceptionHandler = default!;
    [AutoInject] private NavigationManager navigationManager = default!;
    [AutoInject] private IPrerenderStateService prerenderStateService = default!;


    [CascadingParameter] public Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;


    protected override async Task OnInitializedAsync()
    {
        try
        {
            navigationManager.LocationChanged += NavigationManagerLocationChanged;
            authManager.AuthenticationStateChanged += AuthenticationStateChanged;
            unsubscribeCultureChange = pubSubService.Subscribe(PubSubMessages.CULTURE_CHANGED, async _ =>
            {
                SetCurrentDir();
                StateHasChanged();
            });
            unsubscribeThemeChange = pubSubService.Subscribe(PubSubMessages.THEME_CHANGED, async payload =>
            {
                if (payload is null) return;
                currentTheme = (AppThemeType)payload;
                StateHasChanged();
            });
            unsubscribeRouteDataUpdated = pubSubService.Subscribe(PubSubMessages.ROUTE_DATA_UPDATED, async payload =>
            {
                currentRouteData = (RouteData?)payload;
                SetIsCrossLayout();
                StateHasChanged();
            });

            isAuthenticated = await prerenderStateService.GetValue(async () => (await AuthenticationStateTask).User.IsAuthenticated());

            SetCurrentDir();
            SetCurrentUrl();
            currentTheme = await themeService.GetCurrentTheme();

            await keyboard.Add(ButilKeyCodes.KeyD, OpenDiagnosticModal, ButilModifiers.Ctrl | ButilModifiers.Alt | ButilModifiers.Shift);
            await base.OnInitializedAsync();
        }
        catch (Exception exp)
        {
            exceptionHandler.Handle(exp);
        }
    }

    protected override void OnParametersSet()
    {
        // TODO: we can try to recover from exception after rendering the ErrorBoundary with this line.
        // but for now it's better to persist the error ui until a force refresh.
        // ErrorBoundaryRef.Recover();

        base.OnParametersSet();
    }


    private async void AuthenticationStateChanged(Task<AuthenticationState> task)
    {
        try
        {
            isAuthenticated = (await task).User.IsAuthenticated();
        }
        catch (Exception ex)
        {
            exceptionHandler.Handle(ex);
        }
        finally
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    private void NavigationManagerLocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
    {
        SetCurrentUrl();
        StateHasChanged();
    }


    private void SetCurrentDir()
    {
        currentDir = CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft ? BitDir.Rtl : null;
    }

    private void SetCurrentUrl()
    {
        var path = navigationManager.GetUriPath();

        currentUrl = Urls.All.SingleOrDefault(pageUrl =>
        {
            return pageUrl == Urls.HomePage
                    ? pageUrl == path
                    : path.StartsWith(pageUrl);
        });
    }

    private void SetIsCrossLayout()
    {
        // The cross-layout pages are the pages that are getting rendered in multiple layouts (authenticated and unauthenticated).

        if (currentRouteData is null)
        {
            isCrossLayoutPage = true;
            return;
        }

        var type = currentRouteData.PageType;

        if (type.GetCustomAttribute<AuthorizeAttribute>(inherit: true) is not null)
        {
            isCrossLayoutPage = false;
            return;
        }

        if (type.Namespace?.Contains("Client.Core.Components.Pages.Identity") ?? false)
        {
            isCrossLayoutPage = false;
            return;
        }

        isCrossLayoutPage = true;
    }

    private void OpenDiagnosticModal()
    {
        pubSubService.Publish(PubSubMessages.SHOW_DIAGNOSTIC_MODAL);
    }


    private string GetMainCssClass()
    {
        var authClass = isAuthenticated is false ? "unauthenticated"
                      : isAuthenticated is true ? "authenticated"
                      : string.Empty;

        var crossClass = isCrossLayoutPage is true ? " cross-layout" : string.Empty;

        return authClass + crossClass;
    }


    public void Dispose()
    {
        navigationManager.LocationChanged -= NavigationManagerLocationChanged;

        authManager.AuthenticationStateChanged -= AuthenticationStateChanged;

        unsubscribeThemeChange?.Invoke();
        unsubscribeCultureChange?.Invoke();
        unsubscribeRouteDataUpdated?.Invoke();

        _ = keyboard.Remove(OpenDiagnosticModal);
    }
}

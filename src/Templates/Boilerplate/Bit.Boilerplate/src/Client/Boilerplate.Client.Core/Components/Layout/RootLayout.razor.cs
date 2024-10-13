using Boilerplate.Client.Core.Components.Layout.Identity;
using Boilerplate.Client.Core.Components.Layout.Main;

namespace Boilerplate.Client.Core.Components.Layout;

public partial class RootLayout : IDisposable
{
    private BitDir? currentDir;
    private string? currentUrl;
    private bool? isAuthenticated;
    private bool? isAnonymousPage;
    private AppThemeType? currentTheme;
    private Action unsubscribeThemeChange = default!;
    private Action unsubscribeCultureChange = default!;


    [AutoInject] private IThemeService themeService = default!;
    [AutoInject] private IPubSubService pubSubService = default!;
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

            isAuthenticated = await prerenderStateService.GetValue(async () => (await AuthenticationStateTask).User.IsAuthenticated());

            SetCurrentDir();
            SetCurrentUrl();
            currentTheme = await themeService.GetCurrentTheme();

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


    private Type GetCurrentLayout()
    {
        return isAuthenticated is null
                ? typeof(EmptyLayout)
                : isAuthenticated is true
                    ? typeof(MainLayout)
                    : typeof(IdentityLayout);
    }

    private void SetCurrentDir()
    {
        currentDir = CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft ? BitDir.Rtl : null;
    }

    private void SetCurrentUrl()
    {
        var path = navigationManager.GetPath();

        currentUrl = Urls.All.SingleOrDefault(path.StartsWith);

        isAnonymousPage = Urls.AnonymousPages.Any(ap => currentUrl == ap);
    }


    public void Dispose()
    {
        navigationManager.LocationChanged -= NavigationManagerLocationChanged;

        authManager.AuthenticationStateChanged -= AuthenticationStateChanged;

        unsubscribeThemeChange();
        unsubscribeCultureChange();
    }
}

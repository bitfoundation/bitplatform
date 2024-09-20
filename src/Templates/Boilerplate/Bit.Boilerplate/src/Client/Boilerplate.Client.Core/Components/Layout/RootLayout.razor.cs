using Boilerplate.Client.Core.Components.Layout.Identity;
using Boilerplate.Client.Core.Components.Layout.Main;

namespace Boilerplate.Client.Core.Components.Layout;

public partial class RootLayout : IDisposable
{
    private bool disposed;
    private BitDir? currentDir;
    private string? currentUrl;
    private bool isAuthenticated;
    private Action unsubscribeCultureChange = default!;


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

            isAuthenticated = await prerenderStateService.GetValue(async () => (await AuthenticationStateTask).User.IsAuthenticated());

            unsubscribeCultureChange = pubSubService.Subscribe(PubSubMessages.CULTURE_CHANGED, async _ =>
            {
                SetCurrentDir();
                StateHasChanged();
            });

            SetCurrentDir();
            SetCurrentUrl();

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


    private Type GetCurrentLayout()
    {
        return isAuthenticated
                ? typeof(MainLayout)
                : currentUrl == Urls.HomePage
                    ? typeof(EmptyLayout)
                    : typeof(IdentityLayout);
    }

    private void SetCurrentDir()
    {
        currentDir = CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft ? BitDir.Rtl : null;
    }

    private void SetCurrentUrl()
    {
        currentUrl = navigationManager.Uri.Replace(navigationManager.BaseUri, "/", StringComparison.InvariantCultureIgnoreCase);
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


    public void Dispose()
    {
        Dispose(true);

        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed || disposing is false) return;

        navigationManager.LocationChanged -= NavigationManagerLocationChanged;

        authManager.AuthenticationStateChanged -= AuthenticationStateChanged;

        unsubscribeCultureChange();

        disposed = true;
    }
}

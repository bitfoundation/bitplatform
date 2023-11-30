using Microsoft.AspNetCore.Components.Web;

namespace Boilerplate.Client.Core.Components.Layout;

public partial class MainLayout : IDisposable
{
    private bool disposed;
    private bool isMenuOpen;
    private bool isUserAuthenticated;
    private ErrorBoundary errorBoundaryRef = default!;

    [AutoInject] private IPrerenderStateService prerenderStateService = default!;

    [AutoInject] private IExceptionHandler exceptionHandler = default!;

    [AutoInject] private AuthenticationManager authManager = default!;

    [CascadingParameter] public Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

    protected override void OnParametersSet()
    {
        // TODO: we can try to recover from exception after rendering the ErrorBoundary with this line.
        // but for now it's better to persist the error ui until a force refresh.
        // ErrorBoundaryRef.Recover();

        base.OnParametersSet();
    }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            authManager.AuthenticationStateChanged += VerifyUserIsAuthenticatedOrNot;

            isUserAuthenticated = await prerenderStateService.GetValue($"{nameof(MainLayout)}-isUserAuthenticated", async () => (await AuthenticationStateTask).User.IsAuthenticated());

            await base.OnInitializedAsync();
        }
        catch (Exception exp)
        {
            exceptionHandler.Handle(exp);
        }
    }

    async void VerifyUserIsAuthenticatedOrNot(Task<AuthenticationState> task)
    {
        try
        {
            isUserAuthenticated = (await task).User.IsAuthenticated();
        }
        catch (Exception ex)
        {
            exceptionHandler.Handle(ex);
        }
        finally
        {
            StateHasChanged();
        }
    }

    private void ToggleMenuHandler()
    {
        isMenuOpen = !isMenuOpen;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed) return;

        authManager.AuthenticationStateChanged -= VerifyUserIsAuthenticatedOrNot;

        disposed = true;
    }
}

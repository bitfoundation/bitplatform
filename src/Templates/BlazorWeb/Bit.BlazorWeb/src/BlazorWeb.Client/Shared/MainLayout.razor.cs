using Microsoft.AspNetCore.Components.Web;

namespace BlazorWeb.Client.Shared;

public partial class MainLayout : IDisposable
{
    private bool _disposed;
    private bool _isMenuOpen;
    private bool _isUserAuthenticated;
    private ErrorBoundary ErrorBoundaryRef = default!;

    [AutoInject] private IPrerenderStateService _prerenderStateService = default!;

    [AutoInject] private IExceptionHandler _exceptionHandler = default!;

    [AutoInject] private AppAuthenticationManager _authManager = default!;

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
            _authManager.AuthenticationStateChanged += VerifyUserIsAuthenticatedOrNot;

            _isUserAuthenticated = await _prerenderStateService.GetValue($"{nameof(MainLayout)}-isUserAuthenticated", async () => (await AuthenticationStateTask).User.IsAuthenticated());

            await base.OnInitializedAsync();
        }
        catch (Exception exp)
        {
            _exceptionHandler.Handle(exp);
        }
    }

    async void VerifyUserIsAuthenticatedOrNot(Task<AuthenticationState> task)
    {
        try
        {
            _isUserAuthenticated = (await task).User.IsAuthenticated();
        }
        catch (Exception ex)
        {
            _exceptionHandler.Handle(ex);
        }
        finally
        {
            StateHasChanged();
        }
    }

    private void ToggleMenuHandler()
    {
        _isMenuOpen = !_isMenuOpen;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        _authManager.AuthenticationStateChanged -= VerifyUserIsAuthenticatedOrNot;

        _disposed = true;
    }
}

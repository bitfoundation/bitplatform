using Microsoft.AspNetCore.Components.Web;

namespace AdminPanel.Client.Shared;

public partial class MainLayout : IDisposable
{
    private bool _disposed;
    private bool _isMenuOpen;
    private bool _isUserAuthenticated;
    private ErrorBoundary _errorBoundaryRef = default!;

    [AutoInject] private IStateService _stateService = default!;

    [AutoInject] private IExceptionHandler _exceptionHandler = default!;

    [AutoInject] private AppAuthenticationStateProvider _authStateProvider = default!;

    [AutoInject] private IJSRuntime _jsRuntime = default!;

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
            _authStateProvider.AuthenticationStateChanged += VerifyUserIsAuthenticatedOrNot;

            _isUserAuthenticated = await _stateService.GetValue($"{nameof(MainLayout)}-IsUserAuthenticated", _authStateProvider.IsUserAuthenticatedAsync);

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
            _isUserAuthenticated = await _authStateProvider.IsUserAuthenticatedAsync();
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

    private async Task ToggleMenuHandler()
    {
        _isMenuOpen = !_isMenuOpen;

        await _jsRuntime.SetBodyOverflow(_isMenuOpen);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed || disposing is false) return;

        _authStateProvider.AuthenticationStateChanged -= VerifyUserIsAuthenticatedOrNot;

        _disposed = true;
    }
}

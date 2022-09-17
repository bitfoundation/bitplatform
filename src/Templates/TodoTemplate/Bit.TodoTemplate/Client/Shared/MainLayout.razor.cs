using Microsoft.AspNetCore.Components.Web;

namespace TodoTemplate.Client.Shared;

public partial class MainLayout : IAsyncDisposable
{
    [AutoInject] private IStateService _stateService = default!;

    [AutoInject] private IExceptionHandler _exceptionHandler = default!;

    [AutoInject] private AppAuthenticationStateProvider _authStateProvider = default!;

    private ErrorBoundary ErrorBoundaryRef = default!;

    public bool IsUserAuthenticated { get; set; }
    public bool IsMenuOpen { get; set; } = false;

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

            IsUserAuthenticated = await _stateService.GetValue($"{nameof(MainLayout)}-{nameof(IsUserAuthenticated)}", _authStateProvider.IsUserAuthenticated);

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
            IsUserAuthenticated = await _authStateProvider.IsUserAuthenticated();
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
        IsMenuOpen = !IsMenuOpen;
    }

    public async ValueTask DisposeAsync()
    {
        _authStateProvider.AuthenticationStateChanged -= VerifyUserIsAuthenticatedOrNot;
    }
}

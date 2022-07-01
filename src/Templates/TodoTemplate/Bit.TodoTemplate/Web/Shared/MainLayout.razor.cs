using Microsoft.AspNetCore.Components.Web;

namespace TodoTemplate.App.Shared;

public partial class MainLayout : IAsyncDisposable
{
    private ErrorBoundary ErrorBoundaryRef = default!;

    public bool IsUserAuthenticated { get; set; }
    public bool IsMenuOpen { get; set; } = false;

    [AutoInject] private IStateService stateService = default!;

    [AutoInject] private IExceptionHandler exceptionHandler = default!;

    [AutoInject] private TodoTemplateAuthenticationStateProvider todoTemplateAuthenticationStateProvider = default!;

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
            todoTemplateAuthenticationStateProvider.AuthenticationStateChanged += VerifyUserIsAuthenticatedOrNot;

            IsUserAuthenticated = await stateService.GetValue($"{nameof(MainLayout)}-{nameof(IsUserAuthenticated)}", async () => await todoTemplateAuthenticationStateProvider.IsUserAuthenticated());

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
            IsUserAuthenticated = await todoTemplateAuthenticationStateProvider.IsUserAuthenticated();
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
        IsMenuOpen = !IsMenuOpen;
    }

    public async ValueTask DisposeAsync()
    {
        todoTemplateAuthenticationStateProvider.AuthenticationStateChanged -= VerifyUserIsAuthenticatedOrNot;
    }
}

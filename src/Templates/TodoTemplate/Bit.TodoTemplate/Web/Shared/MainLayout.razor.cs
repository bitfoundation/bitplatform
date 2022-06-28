using Microsoft.AspNetCore.Components.Web;

namespace TodoTemplate.App.Shared;
public partial class MainLayout : IAsyncDisposable
{
    private ErrorBoundary ErrorBoundaryRef = default!;

    public bool IsUserAuthenticated { get; set; }
    public bool IsMenuOpen { get; set; } = false;

    [AutoInject] private IStateService StateService { get; set; } = default!;

    [AutoInject] private IExceptionHandler ExceptionHandler { get; set; } = default!;

    [AutoInject] private TodoTemplateAuthenticationStateProvider TodoTemplateAuthenticationStateProvider { get; set; } = default!;

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
            TodoTemplateAuthenticationStateProvider.AuthenticationStateChanged += VerifyUserIsAuthenticatedOrNot;

            IsUserAuthenticated = await StateService.GetValue($"{nameof(MainLayout)}-{nameof(IsUserAuthenticated)}", async () => await TodoTemplateAuthenticationStateProvider.IsUserAuthenticated());

            await base.OnInitializedAsync();
        }
        catch (Exception exp)
        {
            ExceptionHandler.Handle(exp);
        }
    }

    async void VerifyUserIsAuthenticatedOrNot(Task<AuthenticationState> task)
    {
        try
        {
            IsUserAuthenticated = await TodoTemplateAuthenticationStateProvider.IsUserAuthenticated();
        }
        catch (Exception ex)
        {
            ExceptionHandler.Handle(ex);
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
        TodoTemplateAuthenticationStateProvider.AuthenticationStateChanged -= VerifyUserIsAuthenticatedOrNot;
    }
}

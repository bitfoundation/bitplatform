namespace Boilerplate.Client.Core.Components.Layout;

public partial class Header
{
    private bool disposed;
    private bool isUserAuthenticated;

    [Parameter] public EventCallback OnToggleMenu { get; set; }

    protected override async Task OnInitAsync()
    {
        AuthenticationManager.AuthenticationStateChanged += VerifyUserIsAuthenticatedOrNot;

        isUserAuthenticated = await PrerenderStateService.GetValue(async () => (await AuthenticationStateTask).User.IsAuthenticated());

        await base.OnInitAsync();
    }

    async void VerifyUserIsAuthenticatedOrNot(Task<AuthenticationState> task)
    {
        try
        {
            isUserAuthenticated = (await task).User.IsAuthenticated();
        }
        catch (Exception ex)
        {
            ExceptionHandler.Handle(ex);
        }
        finally
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task ToggleMenu()
    {
        await OnToggleMenu.InvokeAsync();
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        await base.DisposeAsync(disposing);

        if (disposed || disposing is false) return;

        AuthenticationManager.AuthenticationStateChanged -= VerifyUserIsAuthenticatedOrNot;

        disposed = true;
    }
}

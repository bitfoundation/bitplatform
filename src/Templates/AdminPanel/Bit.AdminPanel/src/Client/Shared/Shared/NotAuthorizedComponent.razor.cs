namespace AdminPanel.Client.Shared;

public partial class NotAuthorizedComponent
{
    private ClaimsPrincipal _user = default!;

    [CascadingParameter] public Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    protected async override Task OnParamsSetAsync()
    {
        _user = (await AuthenticationState).User;

        await base.OnParamsSetAsync();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);

        if (firstRender is false) return;

        try
        {
            if (_user.Identity?.IsAuthenticated is false)
            {
                RedirectToSignInPage();
            }
        }
        catch (Exception exp)
        {
            ExceptionHandler.Handle(exp);
        }
    }

    private async Task SignIn()
    {
        await AuthenticationService.SignOut();

        RedirectToSignInPage();
    }

    private void RedirectToSignInPage()
    {
        var redirectUrl = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);

        NavigationManager.NavigateTo($"/sign-in?redirectUrl={redirectUrl}");
    }
}

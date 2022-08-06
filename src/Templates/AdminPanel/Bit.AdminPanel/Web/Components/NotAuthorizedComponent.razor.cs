namespace AdminPanel.App.Components;

public partial class NotAuthorizedComponent
{
    [AutoInject] private IAuthenticationService authenticationService = default!;

    [CascadingParameter] public Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    public ClaimsPrincipal User { get; set; } = default!;

    async Task SignIn()
    {
        await authenticationService.SignOut();

        RedirectToSignInPage();
    }

    protected async override Task OnParamsSetAsync()
    {
        User = (await AuthenticationState).User;

        await base.OnParamsSetAsync();
    }

    void RedirectToSignInPage()
    {
        var redirectUrl = navigationManager.ToBaseRelativePath(navigationManager.Uri);
        navigationManager.NavigateTo($"/sign-in?redirectUrl={redirectUrl}");
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            if (User.Identity?.IsAuthenticated is false)
            {
                RedirectToSignInPage();
            }
        }

        base.OnAfterRender(firstRender);
    }
}

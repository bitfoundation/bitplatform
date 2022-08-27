namespace AdminPanel.App.Components;

public partial class NotAuthorizedComponent
{
    [CascadingParameter] public Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    public ClaimsPrincipal User { get; set; } = default!;

    async Task SignIn()
    {
        await AuthenticationService.SignOut();

        RedirectToSignInPage();
    }

    protected async override Task OnParamsSetAsync()
    {
        User = (await AuthenticationState).User;

        await base.OnParamsSetAsync();
    }

    void RedirectToSignInPage()
    {
        var redirectUrl = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
        NavigationManager.NavigateTo($"/sign-in?redirectUrl={redirectUrl}");
    }

    protected override void OnAfterRender(bool firstRender)
    {
        try
        {
            if (firstRender)
            {
                if (User.Identity?.IsAuthenticated is false)
                {
                    RedirectToSignInPage();
                }
            }
        }
        catch (Exception exp)
        {
            ExceptionHandler.Handle(exp);
        }

        base.OnAfterRender(firstRender);
    }
}

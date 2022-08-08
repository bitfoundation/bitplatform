namespace TodoTemplate.App.Components;

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

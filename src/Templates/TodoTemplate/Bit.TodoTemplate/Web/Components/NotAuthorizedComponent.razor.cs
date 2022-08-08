namespace TodoTemplate.App.Components;

public partial class NotAuthorizedComponent
{
    [CascadingParameter] public Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    public ClaimsPrincipal User { get; set; } = default!;

    async Task SignIn()
    {
        await _authenticationService.SignOut();

        RedirectToSignInPage();
    }

    protected async override Task OnParamsSetAsync()
    {
        User = (await AuthenticationState).User;

        await base.OnParamsSetAsync();
    }

    void RedirectToSignInPage()
    {
        var redirectUrl = _navigationManager.ToBaseRelativePath(_navigationManager.Uri);
        _navigationManager.NavigateTo($"/sign-in?redirectUrl={redirectUrl}");
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

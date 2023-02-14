namespace BlazorWeb.Web.Core;

public partial class NotAuthorizedComponent
{
    private ClaimsPrincipal _user { get; set; } = default!;

    [CascadingParameter] private Task<AuthenticationState> _authenticationStateTask { get; set; } = default!;

    protected async override Task OnParamsSetAsync()
    {
        _user = (await _authenticationStateTask).User;

        await base.OnParamsSetAsync();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        try
        {
            if (firstRender && _user.Identity?.IsAuthenticated is false)
            {
                RedirectToSignInPage();
            }
        }
        catch (Exception exp)
        {
            ExceptionHandler.Handle(exp);
        }

        base.OnAfterRender(firstRender);
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

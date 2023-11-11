namespace BlazorWeb.Client.Pages;

public partial class NotAuthorizedPage
{
    private ClaimsPrincipal _user { get; set; } = default!;

    [SupplyParameterFromQuery, Parameter] public string? RedirectUrl { get; set; }

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
        NavigationManager.NavigateTo($"/sign-in?redirectUrl={RedirectUrl ?? NavigationManager.ToBaseRelativePath(NavigationManager.Uri)}");
    }
}

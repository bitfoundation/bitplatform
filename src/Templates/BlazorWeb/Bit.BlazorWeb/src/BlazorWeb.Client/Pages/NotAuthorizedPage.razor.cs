namespace BlazorWeb.Client.Pages;

public partial class NotAuthorizedPage
{
    private ClaimsPrincipal _user { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "redirect-url"), Parameter] public string? RedirectUrl { get; set; }

    protected override async Task OnParamsSetAsync()
    {
        _user = (await AuthenticationStateTask).User;

        await base.OnParamsSetAsync();
    }

    protected override async Task OnAfterFirstRenderAsync()
    {
        if ((await AuthenticationStateTask).User.IsAuthenticated() is false)
        {
            // If neither the refresh_token nor the access_token is present, proceed to the sign-in page.
            await SignIn();
        }

        await base.OnAfterFirstRenderAsync();
    }

    private async Task SignIn()
    {
        await JSRuntime.RemoveToken();

        await AuthenticationStateProvider.RaiseAuthenticationStateHasChanged();

        RedirectToSignInPage();
    }

    private void RedirectToSignInPage()
    {
        NavigationManager.NavigateTo($"/sign-in?redirect-url={RedirectUrl ?? NavigationManager.ToBaseRelativePath(NavigationManager.Uri)}");
    }
}

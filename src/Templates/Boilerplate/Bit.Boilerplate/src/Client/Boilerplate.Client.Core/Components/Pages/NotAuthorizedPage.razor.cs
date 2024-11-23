namespace Boilerplate.Client.Core.Components.Pages;

public partial class NotAuthorizedPage
{
    private bool isRefreshingToken;
    private ClaimsPrincipal user = default!;

    [SupplyParameterFromQuery(Name = "return-url"), Parameter] public string? ReturnUrl { get; set; }

    protected override async Task OnParamsSetAsync()
    {
        user = (await AuthenticationStateTask).User;

        await base.OnParamsSetAsync();
    }

    protected override async Task OnAfterFirstRenderAsync()
    {
        var refreshToken = await StorageService.GetItem("refresh_token");

        // Let's update the access token by refreshing it when a refresh token is available.
        // Following this procedure, the newly acquired access token may now include the necessary roles or claims.
        // To prevent infinities redirect loop, let's append try_refreshing_token=false to the url, so we only redirect in case no try_refreshing_token=false is present

        if (string.IsNullOrEmpty(refreshToken) is false && ReturnUrl?.Contains("try_refreshing_token=false", StringComparison.InvariantCulture) is null or false)
        {
            isRefreshingToken = true;
            StateHasChanged();
            try
            {
                var accessToken = await AuthenticationManager.RefreshToken(requestedBy: nameof(NotAuthorizedPage));
                if (string.IsNullOrEmpty(accessToken) is false && ReturnUrl is not null)
                {
                    var @char = ReturnUrl.Contains('?') ? '&' : '?'; // The RedirectUrl may already include a query string.
                    NavigationManager.NavigateTo($"{ReturnUrl}{@char}try_refreshing_token=false", replace: true);
                    return;
                }
            }
            finally
            {
                isRefreshingToken = false;
                StateHasChanged();
            }
        }

        if ((await AuthenticationStateTask).User.IsAuthenticated() is false)
        {
            await SignOut();
        }

        await base.OnAfterFirstRenderAsync();
    }

    private async Task SignOut()
    {
        await AuthenticationManager.SignOut(deleteUserSessionFromServer: true, CurrentCancellationToken);
        var returnUrl = ReturnUrl ?? NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
        NavigationManager.NavigateTo(Urls.SignInPage + (string.IsNullOrEmpty(returnUrl) ? string.Empty : $"?return-url={returnUrl}"));
    }
}

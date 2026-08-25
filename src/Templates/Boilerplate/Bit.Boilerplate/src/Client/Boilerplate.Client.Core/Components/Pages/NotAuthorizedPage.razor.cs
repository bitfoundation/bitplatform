namespace Boilerplate.Client.Core.Components.Pages;

public partial class NotAuthorizedPage
{
    private bool lacksValidPrivilege;
    private bool isUpdatingAuthState = true;


    [SupplyParameterFromQuery(Name = "return-url"), Parameter]
    public string? ReturnUrl { get; set; }

    [SupplyParameterFromQuery(Name = "try_refreshing_token"), Parameter]
    public bool? TryRefreshingToken { get; set; }

    private string GetSafeReturnUrl() => Uri.IsAppRelativeUrl(ReturnUrl, requireLeadingSlash: false) ? ReturnUrl : PageUrls.Home;

    [AutoInject] private SignInModalService signInModalService = default!;


    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        try
        {
            var refreshToken = await StorageService.GetItem("refresh_token");

            // Let's update the access token by refreshing it when a refresh token is available.
            // Following this procedure, the newly acquired access token may now include the necessary roles or claims.
            // TryRefreshingToken is checked FIRST: arriving here having already been refreshed once for this
            // destination means the refresh cannot help, so a second round trip is pure cost.
            if (TryRefreshingToken is not false && string.IsNullOrEmpty(refreshToken) is false)
            {
                var accessToken = await AuthManager.RefreshToken(requestedBy: nameof(NotAuthorizedPage));
                if (string.IsNullOrEmpty(accessToken) is false && ReturnUrl is not null)
                {
                    var returnUrl = GetSafeReturnUrl();
                    var @char = returnUrl.Contains('?') ? '&' : '?'; // The RedirectUrl may already include a query string.
                    NavigationManager.NavigateTo($"{returnUrl}{@char}try_refreshing_token=false", replace: true);
                }
            }

            var user = (await AuthenticationStateTask).User;

            lacksValidPrivilege = (await AuthorizationService.IsAuthorized(user, AuthPolicies.PRIVILEGED_ACCESS)) is false;
        }
        finally
        {
            isUpdatingAuthState = false;
            StateHasChanged();
        }
    }

    private async Task SignIn()
    {
        await AuthManager.SignOut(CurrentCancellationToken);
        var returnUrl = ReturnUrl is null ? NavigationManager.GetRelativePath() : GetSafeReturnUrl();
        await signInModalService.SignIn(returnUrl);

        // Alternatively, you can redirect the user to the sign-in page.
        // NavigationManager.NavigateTo($"{PageUrls.SignIn}?return-url={Uri.EscapeDataString(returnUrl)}");
    }
}

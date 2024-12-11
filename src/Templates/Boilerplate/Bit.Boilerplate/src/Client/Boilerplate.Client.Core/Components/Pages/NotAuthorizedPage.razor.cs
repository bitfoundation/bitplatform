namespace Boilerplate.Client.Core.Components.Pages;

public partial class NotAuthorizedPage
{
    private bool lacksValidPrivilege;
    private bool isUpdatingAuthState = true;
    private ClaimsPrincipal user = default!;

    [SupplyParameterFromQuery(Name = "return-url"), Parameter] public string? ReturnUrl { get; set; }

    protected override async Task OnAfterFirstRenderAsync()
    {
        try
        {
            var refreshToken = await StorageService.GetItem("refresh_token");

            // Let's update the access token by refreshing it when a refresh token is available.
            // Following this procedure, the newly acquired access token may now include the necessary roles or claims.
            if (string.IsNullOrEmpty(refreshToken) is false)
            {
                var accessToken = await AuthManager.RefreshToken(requestedBy: nameof(NotAuthorizedPage));
                if (string.IsNullOrEmpty(accessToken) is false && ReturnUrl is not null && ReturnUrl.Contains("try_refreshing_token=false", StringComparison.InvariantCulture) is false)
                {
                    // To prevent infinities redirect loop, let's append try_refreshing_token=false to the url, so we only redirect in case no try_refreshing_token=false is present
                    var @char = ReturnUrl.Contains('?') ? '&' : '?'; // The RedirectUrl may already include a query string.
                    NavigationManager.NavigateTo($"{ReturnUrl}{@char}try_refreshing_token=false", replace: true);
                }
            }

            user = (await AuthenticationStateTask).User;

            lacksValidPrivilege = await AuthorizationService.AuthorizeAsync(user, AuthPolicies.PRIVILEGED_ACCESS) is { Succeeded: false };
        }
        finally
        {
            isUpdatingAuthState = false;
            StateHasChanged();
        }

        await base.OnAfterFirstRenderAsync();
    }

    private async Task SignOut()
    {
        await AuthManager.SignOut(CurrentCancellationToken);
        var returnUrl = ReturnUrl ?? NavigationManager.GetRelativePath();
        NavigationManager.NavigateTo(Urls.SignInPage + (string.IsNullOrEmpty(returnUrl) ? string.Empty : $"?return-url={returnUrl}"));
    }
}

public partial class RedirectToSignInPage : AppComponentBase
{
    [Parameter] public string? ReturnUrl { get; set; }

    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        await AuthManager.SignOut(CurrentCancellationToken);
        var returnUrl = ReturnUrl ?? NavigationManager.GetRelativePath();
        NavigationManager.NavigateTo(Urls.SignInPage + (string.IsNullOrEmpty(returnUrl) ? string.Empty : $"?return-url={returnUrl}"));
    }
}

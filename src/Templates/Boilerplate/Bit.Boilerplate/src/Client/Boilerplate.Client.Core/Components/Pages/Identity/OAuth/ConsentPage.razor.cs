//+:cnd:noEmit
using Boilerplate.Shared.Features.Identity.OAuth;
using Boilerplate.Shared.Features.Identity.OAuth.Dtos;

namespace Boilerplate.Client.Core.Components.Pages.Identity.OAuth;

/// <summary>
/// Everything shown here is described by the server, never read from the query string: a client name taken from the url
/// this page was reached by would be a phishing vector.
/// </summary>
public partial class ConsentPage
{
    [AutoInject] private IOAuthController oauthController = default!;
    [AutoInject] private IExternalNavigationService externalNavigationService = default!;

    /// <summary>Bound off the query string /oauth/authorize redirected here with; names are the wire names.</summary>
    [Parameter, SupplyParameterFromQuery(Name = "client_id")]
    public string? ClientId { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "redirect_uri")]
    public string? RedirectUri { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "response_type")]
    public string? ResponseType { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "scope")]
    public string? Scope { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "state")]
    public string? State { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "code_challenge")]
    public string? CodeChallenge { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "code_challenge_method")]
    public string? CodeChallengeMethod { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "resource")]
    public string? Resource { get; set; }

    //#if (multitenant == true)
    /// <summary>
    /// A string, not a <see cref="Guid"/>: the server tolerates a malformed value and forwards it, and a typed
    /// parameter would throw while binding, before this page could show the error.
    /// </summary>
    [Parameter, SupplyParameterFromQuery(Name = "tenant_id")]
    public string? TenantId { get; set; }
    //#endif

    /// <summary>Set when the client or its redirect uri could not be identified - there is nowhere safe to go back to.</summary>
    [Parameter, SupplyParameterFromQuery(Name = "error")]
    public string? ErrorQueryString { get; set; }

    private bool isLoading = true;
    private bool isSubmitting;
    private bool redirectToSignIn;
    private string? error;
    private string? leftFor;
    private OAuthConsentDto? consent;

    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        if (error is not null)
            return;

        if (redirectToSignIn)
        {
            // Relative, query string and all: the sign-in page only honours an app-relative return url.
            var returnUrl = NavigationManager.GetRelativePath();
            NavigationManager.NavigateTo($"{PageUrls.SignIn}?return-url={Uri.EscapeDataString(returnUrl)}", replace: true);
            return;
        }

        try
        {
            consent = await oauthController.Review(BuildRequest(), CurrentCancellationToken);
        }
        catch (KnownException exception)
        {
            error = exception.Message;
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        if (string.IsNullOrEmpty(ErrorQueryString) is false)
        {
            error = DescribeError(ErrorQueryString);
            isLoading = false;
            return;
        }

        redirectToSignIn = (await AuthenticationStateTask).User.IsAuthenticated() is false;
    }

    private Task Approve() => Submit(oauthController.Approve);

    private Task Deny() => Submit(oauthController.Deny);

    // Either answer is a server call whose reply is where the browser goes next; only the call differs.
    private async Task Submit(Func<OAuthAuthorizeRequestDto, CancellationToken, Task<OAuthApprovalDto>> answer)
    {
        if (isSubmitting) return;

        isSubmitting = true;

        try
        {
            var result = await answer(BuildRequest(), CurrentCancellationToken);

            await LeaveFor(result.RedirectUrl);
        }
        finally
        {
            isSubmitting = false;
        }
    }

    /// <summary>
    /// The destination belongs to the client: a hard navigation in a browser, but a hybrid WebView can follow neither a
    /// private-use scheme nor a loopback listener, so it goes to the OS and this page stays on <see cref="leftFor"/>.
    /// </summary>
    private async Task LeaveFor(string redirectUrl)
    {
        if (AppPlatform.IsBlazorHybrid is false)
        {
            NavigationManager.NavigateTo(redirectUrl, forceLoad: true, replace: true);
            return;
        }

        await externalNavigationService.NavigateTo(redirectUrl);

        leftFor = redirectUrl;
    }

    private OAuthAuthorizeRequestDto BuildRequest() => new()
    {
        ClientId = ClientId,
        RedirectUri = RedirectUri,
        ResponseType = ResponseType,
        Scope = Scope,
        State = State,
        CodeChallenge = CodeChallenge,
        CodeChallengeMethod = CodeChallengeMethod,
        Resource = Resource,
        //#if (multitenant == true)
        TenantId = Guid.TryParse(TenantId, out var tenantId) ? tenantId : null
        //#endif
    };

    /// <summary>The only failures that reach this page rather than the client.</summary>
    private string DescribeError(string errorCode) => errorCode switch
    {
        "invalid_client" => Localizer[nameof(AppStrings.OAuthErrorInvalidClient)],
        "invalid_redirect_uri" => Localizer[nameof(AppStrings.OAuthErrorInvalidRedirectUri)],
        _ => Localizer[nameof(AppStrings.OAuthErrorInvalidRequest)]
    };

    private string ScopeDescription(string scope) => scope switch
    {
        OAuthScopes.DevMcp => Localizer[nameof(AppStrings.OAuthScopeDevMcpDescription)],
        //#if (signalR == true)
        OAuthScopes.Chat => Localizer[nameof(AppStrings.OAuthScopeChatDescription)],
        //#endif
        _ => scope
    };
}

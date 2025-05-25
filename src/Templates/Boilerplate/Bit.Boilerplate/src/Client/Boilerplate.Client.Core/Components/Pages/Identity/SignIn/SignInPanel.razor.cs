using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Shared.Controllers.Identity;
using Microsoft.AspNetCore.Components.Routing;

namespace Boilerplate.Client.Core.Components.Pages.Identity.SignIn;

public partial class SignInPanel
{
    private bool isWaiting;
    private bool isOtpSent;
    private bool isNewUser; // Assume the user is existing unless they provide an email or phone number that doesn't exists in database.
    private bool showWebAuthn;
    private bool successfulSignIn;
    private bool requiresTwoFactor;
    private SignInPanelTab currentTab;
    private Action? pubSubUnsubscribe;
    private JsonElement? webAuthnAssertion;
    private SignInPanelType internalSignInPanelType;
    private readonly SignInRequestDto model = new();
    private AppDataAnnotationsValidator? validatorRef;
    private string ReturnUrl => ReturnUrlQueryString ?? NavigationManager.GetRelativePath() ?? Urls.HomePage;


    [Parameter, SupplyParameterFromQuery(Name = "return-url")]
    public string? ReturnUrlQueryString { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "userName")]
    public string? UserNameQueryString { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "email")]
    public string? EmailQueryString { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "phoneNumber")]
    public string? PhoneNumberQueryString { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "otp")]
    public string? OtpQueryString { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "error")]
    public string? ErrorQueryString { get; set; }

    [Parameter] public Action? OnSuccess { get; set; } // The SignInModalService will show this page as a modal dialog, and this action will be invoked when the sign-in is successful.
    [Parameter] public SignInPanelType SignInPanelType { get; set; } // Check out SignInModalService for more details

    [AutoInject] private IWebAuthnService webAuthnService = default!;
    [AutoInject] private ILocalHttpServer localHttpServer = default!;
    [AutoInject] private ITelemetryContext telemetryContext = default!;
    [AutoInject] private IIdentityController identityController = default!;
    [AutoInject] private IExternalNavigationService externalNavigationService = default!;

    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        internalSignInPanelType = SignInPanelType;

        model.UserName = UserNameQueryString;
        model.Email = EmailQueryString;
        model.PhoneNumber = PhoneNumberQueryString;

        if (string.IsNullOrEmpty(OtpQueryString) is false)
        {
            model.Otp = OtpQueryString;

            if (InPrerenderSession is false &&
                (string.IsNullOrEmpty(model.UserName) is false ||
                 string.IsNullOrEmpty(model.Email) is false ||
                 string.IsNullOrEmpty(model.PhoneNumber) is false))
            {
                await DoSignIn();
            }
        }

        if (string.IsNullOrEmpty(ErrorQueryString) is false)
        {
            SnackBarService.Error(ErrorQueryString);
        }
    }

    private async Task HideTfaAndOtpPanels(LocationChangingContext args)
    {
        // We're treating OtpPanel and TfaPanel as modal dialogs. This means that no matter where the user tries to navigate,
        // we will block the navigation and close either the TfaPanel or OtpPanel if it's visible.
        // The only exception to this is when the sign-in process is successful.
        if (successfulSignIn)
            return;

        args.PreventNavigation();

        webAuthnAssertion = null;

        isOtpSent = false;
        model.Otp = null;

        requiresTwoFactor = false;
        model.TwoFactorCode = null;

        await InvokeAsync(StateHasChanged);
    }

    private async Task DoSignIn()
    {
        if (isOtpSent && string.IsNullOrWhiteSpace(model.Otp)) return;

        isWaiting = true;
        successfulSignIn = false;

        StateHasChanged(); // Social sign-in callback will eventually call this method, so we need to update the UI immediately. See ClientPubSubMessages.SOCIAL_SIGN_IN references.

        try
        {
            if (requiresTwoFactor && string.IsNullOrWhiteSpace(model.TwoFactorCode)) return;

            if (webAuthnAssertion.HasValue)
            {
                var response = await identityController
                    .WithQueryIf(AppPlatform.IsBlazorHybrid, "origin", localHttpServer.Origin)
                    .VerifyWebAuthAndSignIn(
                        new VerifyWebAuthnAndSignInDto
                        {
                            ClientResponse = webAuthnAssertion.Value,
                            TfaCode = model.TwoFactorCode,
                            DeviceInfo = telemetryContext.Platform
                        },
                        CurrentCancellationToken);

                requiresTwoFactor = response.RequiresTwoFactor;

                if (requiresTwoFactor is false)
                {
                    successfulSignIn = true;
                    await AuthManager.StoreTokens(response!, model.RememberMe);
                }
            }
            else
            {
                CleanModel();

                if (validatorRef?.EditContext.Validate() is false) return;

                if (isNewUser is false)
                {
                    model.ReturnUrl = ReturnUrl;
                    model.DeviceInfo = telemetryContext.Platform;

                    requiresTwoFactor = await AuthManager.SignIn(model, CurrentCancellationToken);

                }
                else
                {
                    // Check out SignInModalService for more details
                    if (string.IsNullOrEmpty(model.Email) is false)
                    {
                        var signInResponse = await identityController.ConfirmEmail(new()
                        {
                            Token = model.Otp,
                            Email = model.Email,
                            DeviceInfo = telemetryContext.Platform
                        }, CurrentCancellationToken);

                        await AuthManager.StoreTokens(signInResponse, true);
                    }
                    else
                    {
                        var signInResponse = await identityController.ConfirmPhone(new()
                        {
                            Token = model.Otp,
                            PhoneNumber = model.PhoneNumber,
                            DeviceInfo = telemetryContext.Platform
                        }, CurrentCancellationToken);

                        await AuthManager.StoreTokens(signInResponse, true);
                    }
                }

                if (requiresTwoFactor is false)
                {
                    successfulSignIn = true;
                }
            }

            if (successfulSignIn)
            {
                if (OnSuccess is not null)
                {
                    OnSuccess.Invoke();
                }
                else
                {
                    NavigationManager.NavigateTo(ReturnUrl ?? Urls.HomePage, replace: true);
                }
            }
        }
        catch (BadRequestException e) when (e.Key == nameof(AppStrings.UserIsNotConfirmed))
        {
            ShowOtpForNewUsers();
        }
        catch (KnownException e)
        {
            // To disable the sign-in button until a specific time after a user lockout, use the value of `e.TryGetExtensionDataValue<TimeSpan>("TryAgainIn", out var tryAgainIn)`.
            webAuthnAssertion = null;
            SnackBarService.Error(e.Message);
        }
        finally
        {
            isWaiting = false;
        }
    }

    private async Task SocialSignIn(string provider)
    {
        try
        {
            pubSubUnsubscribe?.Invoke();
            pubSubUnsubscribe = PubSubService.Subscribe(ClientPubSubMessages.SOCIAL_SIGN_IN, async (uriString) =>
            {
                // Check out SignInModalService for more details
                var uri = uriString!.ToString();
                var queryIndex = uri!.IndexOf('?');
                var queryParams = AppQueryStringCollection.Parse(uri[queryIndex..]);

                string? GetValue(object? value)
                {
                    var valueAsString = value?.ToString();

                    if (string.IsNullOrEmpty(valueAsString)) return null;

                    return Uri.UnescapeDataString(valueAsString);
                }

                queryParams.TryGetValue("return-url", out var returnUrl);
                ReturnUrlQueryString = GetValue(returnUrl ?? Urls.HomePage);
                queryParams.TryGetValue("userName", out var userName);
                UserNameQueryString = GetValue(userName);
                queryParams.TryGetValue("email", out var email);
                EmailQueryString = GetValue(email);
                queryParams.TryGetValue("phoneNumber", out var phoneNumber);
                PhoneNumberQueryString = GetValue(phoneNumber);
                queryParams.TryGetValue("otp", out var otp);
                OtpQueryString = GetValue(otp);
                queryParams.TryGetValue("error", out var error);
                ErrorQueryString = GetValue(error);

                await InvokeAsync(OnInitAsync);
            });

            var port = localHttpServer.EnsureStarted();

            var redirectUrl = await identityController.GetSocialSignInUri(provider, ReturnUrl, port is -1 ? null : port, CurrentCancellationToken);

            await externalNavigationService.NavigateToAsync(redirectUrl);
        }
        catch (KnownException e)
        {
            SnackBarService.Error(e.Message);
        }
    }

    private async Task PasswordlessSignIn()
    {
        isWaiting = true;

        try
        {
            var userIds = await webAuthnService.GetWebAuthnConfiguredUserIds();

            if (AppPlatform.IsBlazorHybrid)
            {
                localHttpServer.EnsureStarted();
            }

            var options = await identityController
                .WithQueryIf(AppPlatform.IsBlazorHybrid, "origin", localHttpServer.Origin)
                .GetWebAuthnAssertionOptions(new() { UserIds = userIds }, CurrentCancellationToken);

            try
            {
                webAuthnAssertion = await webAuthnService.GetWebAuthnCredential(options);
            }
            catch (Exception ex)
            {
                // we can safely handle the exception thrown here since it mostly because of a timeout or user cancelling the native ui.
                ExceptionHandler.Handle(ex, AppEnvironment.IsDev() ? ExceptionDisplayKind.NonInterrupting : ExceptionDisplayKind.None);
                webAuthnAssertion = null;
                return;
            }

            await DoSignIn();
        }
        catch (KnownException e)
        {
            webAuthnAssertion = null;
            SnackBarService.Error(e.Message);
        }
        finally
        {
            isWaiting = false;
        }
    }

    private async Task SendOtp(bool resend)
    {
        try
        {
            CleanModel();

            if (model.Email is null && model.PhoneNumber is null) return;

            if (model.Email is not null && new EmailAddressAttribute().IsValid(model.Email) is false)
            {
                SnackBarService.Error(string.Format(AppStrings.EmailAddressAttribute_ValidationError, AppStrings.Email));
                return;
            }

            if (model.PhoneNumber is not null && new PhoneAttribute().IsValid(model.PhoneNumber) is false)
            {
                SnackBarService.Error(string.Format(AppStrings.PhoneAttribute_ValidationError, AppStrings.PhoneNumber));
                return;
            }

            var request = new IdentityRequestDto { UserName = model.UserName, Email = model.Email, PhoneNumber = model.PhoneNumber };

            await identityController.SendOtp(request, ReturnUrl, CurrentCancellationToken);

            isOtpSent = true;
        }
        catch (TooManyRequestsExceptions e)
        {
            isOtpSent = true;
            SnackBarService.Error(e.Message);
        }
        catch (BadRequestException e) when (e.Key == nameof(AppStrings.UserIsNotConfirmed))
        {
            ShowOtpForNewUsers();
        }
        catch (KnownException e)
        {
            SnackBarService.Error(e.Message);
        }
    }

    private async Task SendTfaToken()
    {
        try
        {
            if (webAuthnAssertion.HasValue is false)
            {
                CleanModel();

                await identityController.SendTwoFactorToken(model, CurrentCancellationToken);
            }
            else
            {
                await identityController
                    .WithQueryIf(AppPlatform.IsBlazorHybrid, "origin", localHttpServer.Origin)
                    .VerifyWebAuthAndSendTwoFactorToken(webAuthnAssertion.Value, CurrentCancellationToken);
            }

            SnackBarService.Success(Localizer[nameof(AppStrings.TfaTokenSentMessage)]);
        }
        catch (KnownException e)
        {
            SnackBarService.Error(e.Message);
        }
    }

    private void CleanModel()
    {
        if (currentTab is SignInPanelTab.Email)
        {
            model.PhoneNumber = null;
            if (validatorRef is null) return;

            validatorRef.EditContext.NotifyFieldChanged(validatorRef.EditContext.Field(nameof(SignInRequestDto.PhoneNumber)));
        }
        else
        {
            model.Email = null;
            if (validatorRef is null) return;

            validatorRef.EditContext.NotifyFieldChanged(validatorRef.EditContext.Field(nameof(SignInRequestDto.Email)));
        }
    }

    /// <summary>
    /// Check out <see cref="SignInModalService"/> for more details
    /// </summary>
    private void ShowOtpForNewUsers()
    {
        isNewUser = true;
        isOtpSent = true;
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        pubSubUnsubscribe?.Invoke();

        await base.DisposeAsync(disposing);
    }

    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        showWebAuthn = await webAuthnService.IsWebAuthnAvailable() && (await webAuthnService.GetWebAuthnConfiguredUserIds()).Any();

        StateHasChanged();
    }


    private async Task OnTabChange(BitPivotItem item)
    {
        currentTab = Enum.Parse<SignInPanelTab>(item.Key!);
    }

    private async Task ChangeSignInPanelType(SignInPanelType type)
    {
        internalSignInPanelType = type;
    }
}

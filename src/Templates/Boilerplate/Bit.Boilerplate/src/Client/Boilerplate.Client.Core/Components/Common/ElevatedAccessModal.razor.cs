namespace Boilerplate.Client.Core.Components.Common;

public partial class ElevatedAccessModal
{
    /// <summary>Reports the outcome to the <see cref="ElevatedAccessService"/> that opened this modal.</summary>
    [Parameter] public Action<bool>? OnResult { get; set; }


    [AutoInject] private IUserController userController = default!;
    [AutoInject] private IWebAuthnService webAuthnService = default!;
    [AutoInject] private ILocalHttpServer localHttpServer = default!;
    [AutoInject] private IIdentityController identityController = default!;


    private Guid userId;
    private string? code;
    private bool codeSent;
    private bool isSending;
    private bool isVerifying;
    private bool isCodeInvalid;
    private bool passkeyAvailable;
    private bool isPasskeyRunning;
    private string? deliveryMessage;
    private bool hasAuthenticatorApp;
    private TimeSpan? resendAvailableIn;
    private PeriodicTimer? resendTimer;


    private bool IsBusy => isSending || isVerifying || isPasskeyRunning;

    /// <summary>A code that arrived from an earlier send is answerable while a new one is still being sent.</summary>
    private bool CanVerify => isVerifying is false && isPasskeyRunning is false && code?.Length == 6;

    private string CodeLabel => Localizer[nameof(AppStrings.ElevatedAccessCodeLabel)];

    /// <summary>What to do next. Where a code went is never guessed here - only <see cref="deliveryMessage"/> knows.</summary>
    private string? CodeDescription
    {
        get
        {
            if (isCodeInvalid)
                return Localizer[nameof(AppStrings.ElevatedAccessInvalidCode)];

            if (deliveryMessage is not null)
                return deliveryMessage;

            if (hasAuthenticatorApp)
                return Localizer[nameof(AppStrings.ElevatedAccessUseAuthenticatorApp)];

            if (passkeyAvailable)
                return Localizer[nameof(AppStrings.ElevatedAccessUsePasskeyOrAskForCode)];

            return null;
        }
    }

    private string SendButtonText
    {
        get
        {
            if (resendAvailableIn is TimeSpan remaining)
                return Localizer[nameof(AppStrings.ElevatedAccessResendIn), remaining];

            return Localizer[codeSent ? nameof(AppStrings.ElevatedAccessResendCode) : nameof(AppStrings.ElevatedAccessSendCode)];
        }
    }


    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        var user = (await AuthenticationStateTask).User;

        userId = user.GetUserId();

        hasAuthenticatorApp = await AuthorizationService.IsAuthorized(user, AuthPolicies.TFA_ENABLED);
    }

    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        if (AppPlatform.IsBlazorHybrid)
        {
            localHttpServer.EnsureStarted();
        }

        passkeyAvailable = await webAuthnService.IsWebAuthnAvailable() && await webAuthnService.IsWebAuthnConfigured(userId);

        StateHasChanged();

        // Only an account that can produce no code of its own gets one sent unasked.
        if (hasAuthenticatorApp is false && passkeyAvailable is false)
        {
            await SendCode();
            StateHasChanged();
        }
    }

    /// <summary>Asks for a code on every confirmed channel (See UserController.SendElevatedAccessToken).</summary>
    private async Task SendCode()
    {
        if (IsBusy) return;

        isSending = true;
        deliveryMessage = null;
        isCodeInvalid = false;

        try
        {
            var sentTo = await userController.SendElevatedAccessToken(CurrentCancellationToken);

            codeSent = true;
            deliveryMessage = DescribeDelivery(sentTo);
        }
        catch (TooManyRequestsException exp)
        {
            // One code per elevation window, so reopening the modal lands here. Not an error to shout about.
            codeSent = true;
            deliveryMessage = Localizer[nameof(AppStrings.ElevatedAccessCodeAlreadySent)];

            if (exp.TryGetExtensionDataValue<TimeSpan>("TryAgainIn", out var tryAgainIn))
            {
                StartResendCountdown(tryAgainIn);
            }
        }
        finally
        {
            isSending = false;
        }
    }

    /// <summary>Names every channel the code landed on - it can be mailed, texted and pushed at once.</summary>
    private string DescribeDelivery(ElevatedAccessTokenSentDto sentTo)
    {
        string?[] channels =
        [
            sentTo.SentToEmail ? Localizer[nameof(AppStrings.ElevatedAccessChannelEmail)].Value : null,
            sentTo.SentToPhoneNumber ? Localizer[nameof(AppStrings.ElevatedAccessChannelPhone)].Value : null,
            sentTo.SentToOtherDevices ? Localizer[nameof(AppStrings.ElevatedAccessChannelOtherDevices)].Value : null
        ];

        var destinations = channels.OfType<string>().ToArray();

        if (destinations is [])
            return Localizer[nameof(AppStrings.ElevatedAccessNoChannel)];

        if (destinations is [var only])
            return Localizer[nameof(AppStrings.ElevatedAccessSentTo), only];

        return Localizer[nameof(AppStrings.ElevatedAccessSentToMany), string.Join(", ", destinations[..^1]), destinations[^1]];
    }

    /// <summary>Verifies the typed code by asking for a fresh access token with it; a wrong one keeps the modal open.</summary>
    private async Task Verify()
    {
        if (CanVerify is false) return;

        isVerifying = true;
        isCodeInvalid = false;

        try
        {
            var accessToken = await AuthManager.RefreshToken(requestedBy: nameof(ElevatedAccessModal), elevatedAccessToken: code);

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                isCodeInvalid = true;
                return;
            }

            Complete(true);
        }
        finally
        {
            isVerifying = false;
        }
    }

    /// <summary>Elevates against a passkey instead of a code (See IdentityController.ElevateByWebAuthn).</summary>
    private async Task ElevateWithPasskey()
    {
        if (IsBusy) return;

        isPasskeyRunning = true;

        try
        {
            var options = await identityController
                .WithQueryIf(AppPlatform.IsBlazorHybrid, "origin", localHttpServer.Origin)
                .GetWebAuthnAssertionOptions(new() { UserIds = [userId] }, CurrentCancellationToken);

            JsonElement assertion;
            try
            {
                assertion = await webAuthnService.GetWebAuthnCredential(options);
            }
            catch (Exception exp)
            {
                // A cancelled prompt, a timeout and a missing passkey all arrive the same way, and the platform
                // already showed its own dialog. The code path stays open.
                ExceptionHandler.Handle(exp, AppEnvironment.IsDevelopment() ? ExceptionDisplayKind.NonInterrupting : ExceptionDisplayKind.None);
                return;
            }

            var accessToken = await AuthManager.RefreshToken(requestedBy: nameof(ElevateWithPasskey), webAuthnClientResponse: assertion);

            if (string.IsNullOrWhiteSpace(accessToken) is false)
            {
                Complete(true);
            }
        }
        finally
        {
            isPasskeyRunning = false;
        }
    }

    /// <summary>Holds the send button shut for as long as the server said to, counting the wait down on it.</summary>
    private void StartResendCountdown(TimeSpan delay)
    {
        resendAvailableIn = delay;
        resendTimer?.Dispose();
        var timer = resendTimer = new PeriodicTimer(TimeSpan.FromSeconds(1), TimeProvider);
        var availableOn = TimeProvider.GetUtcNow() + delay;

        _ = Tick();

        // Stopped by disposing the timer, never by CurrentCancellationToken: DisposeAsync cancels that BEFORE it
        // reaches the override below, so this fire-and-forget loop would end in an exception nobody observes.
        async Task Tick()
        {
            while (await timer.WaitForNextTickAsync())
            {
                var remaining = availableOn - TimeProvider.GetUtcNow();

                resendAvailableIn = remaining > TimeSpan.Zero ? remaining : null;

                if (resendAvailableIn is null)
                {
                    timer.Dispose();
                }

                await InvokeAsync(StateHasChanged);
            }
        }
    }

    private void Complete(bool elevated)
    {
        OnResult?.Invoke(elevated);
    }

    protected override ValueTask DisposeAsync(bool disposing)
    {
        resendTimer?.Dispose();

        return base.DisposeAsync(disposing);
    }
}

namespace Boilerplate.Client.Core.Components.Pages.Settings.Account;

public partial class PasswordlessTab
{
    private bool isConfigured;
    private bool isAvailable = false;


    [AutoInject] IUserController userController = default!;
    [AutoInject] IWebAuthnService webAuthnService = default!;
    [AutoInject] ILocalHttpServer localHttpServer = default!;
    [AutoInject] IIdentityController identityController = default!;


    [Parameter] public UserDto? User { get; set; }

    protected override async Task OnParamsSetAsync()
    {
        await base.OnParamsSetAsync();

        if (User?.UserName is null) return;

        isAvailable = await webAuthnService.IsWebAuthnAvailable();
        isConfigured = await webAuthnService.IsWebAuthnConfigured(User.Id);
    }


    private async Task EnablePasswordless()
    {
        if (User?.UserName is null) return;

        // Only on Android this action will replace the current credential registered on the device,
        // since android won't show the user selection window when there are multiple credentials registered.
        // So it may be a good idea to show a confirm modal if this behavior is not appropriate for your app (as shown in the following commented lines):
        //var userIds = await JSRuntime.GetWebAuthnConfiguredUserIds();
        //if (userIds is not null && userIds.Length > 0)
        //{
        //    // show a warning or confirm modal
        //}

        var options = await userController
            .WithQueryIf(AppPlatform.IsBlazorHybrid, "origin", localHttpServer.Origin)
            .GetWebAuthnCredentialOptions(CurrentCancellationToken);

        JsonElement attestationResponse;
        try
        {
            attestationResponse = (await webAuthnService.CreateWebAuthnCredential(options));
        }
        catch (JSException ex)
        {
            // we can safely handle the exception thrown here since it mostly because of a timeout or user cancelling the native ui.
            ExceptionHandler.Handle(ex, AppEnvironment.IsDevelopment() ? ExceptionDisplayKind.NonInterrupting : ExceptionDisplayKind.None);
            return;
        }

        await userController
            .WithQueryIf(AppPlatform.IsBlazorHybrid, "origin", localHttpServer.Origin)
            .CreateWebAuthnCredential(attestationResponse, CurrentCancellationToken);

        await webAuthnService.SetWebAuthnConfiguredUserId(User.Id);

        isConfigured = true;

        SnackBarService.Success(Localizer[nameof(AppStrings.EnablePasswordlessSucsessMessage)]);
    }

    private async Task DisablePasswordless()
    {
        if (User?.UserName is null) return;

        var options = await identityController
            .WithQueryIf(AppPlatform.IsBlazorHybrid, "origin", localHttpServer.Origin)
            .GetWebAuthnAssertionOptions(new() { UserIds = [User.Id] }, CurrentCancellationToken);

        JsonElement assertion;
        try
        {
            assertion = (await webAuthnService.GetWebAuthnCredential(options));
        }
        catch (Exception ex)
        {
            // Regardless of whether the user actively cancelled the operation, it timed out, or the passkey is no
            // longer valid, the browser throws the same generic error, so we cannot distinguish the root cause.
            // The local flag is deliberately NOT cleared here: it used to be cleared in a `finally`, which meant a
            // cancelled ceremony flipped the UI to "not configured" while the server-side credential was untouched
            // and passwordless sign-in still worked - the UI asserted a security state the server did not hold.
            ExceptionHandler.Handle(ex, ExceptionDisplayKind.None);
            SnackBarService.Warning(Localizer[nameof(AppStrings.DisablePasswordlessFailedMessage)]);
            return;
        }

        // Proof of possession. A failed assertion does not return a value here - fido2.MakeAssertionAsync
        // throws on the server - so reaching the next line means verification passed. The server also consumes
        // the challenge at this point, so this assertion cannot be replayed for sign-in.
        await identityController
            .WithQueryIf(AppPlatform.IsBlazorHybrid, "origin", localHttpServer.Origin)
            .VerifyWebAuthAssertion(assertion, CurrentCancellationToken);

        try
        {
            await userController
                .WithQueryIf(AppPlatform.IsBlazorHybrid, "origin", localHttpServer.Origin)
                .DeleteWebAuthnCredential(assertion, CurrentCancellationToken);

            SnackBarService.Success(Localizer[nameof(AppStrings.DisablePasswordlessSucsessMessage)]);
        }
        catch (ResourceNotFoundException)
        {
            // The server has no such credential - the row was removed out of band (table wiped etc).
            // The local flag is the ONLY thing left claiming a passkey
            // exists, so clearing it here is the whole point: otherwise the user is stuck with a toggle that
            // says "configured" and can never be turned off.
            SnackBarService.Warning(Localizer[nameof(AppStrings.PasswordlessAlreadyRemovedMessage)]);
        }

        // Reached on success and on "already gone" - both mean the server holds no credential for this user.
        // NOT reached when the ceremony itself failed or was cancelled, which is the case that used to lie to the
        // user by reporting a revocation that never happened.
        await webAuthnService.RemoveWebAuthnConfiguredUserId(User.Id);
        isConfigured = false;
    }

    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        if (AppPlatform.IsBlazorHybrid)
        {
            localHttpServer.EnsureStarted();
        }
    }
}

using Boilerplate.Shared.Controllers.Identity;
using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Authorized.Settings.Account;

public partial class PasswordlessTab
{
    private bool isConfigured;


    [AutoInject] IUserController userController = default!;
    [AutoInject] IWebAuthnService webAuthnService = default!;
    [AutoInject] ILocalHttpServer localHttpServer = default!;
    [AutoInject] IIdentityController identityController = default!;


    [Parameter] public UserDto? User { get; set; }

    protected override async Task OnParamsSetAsync()
    {
        await base.OnParamsSetAsync();

        if (User?.UserName is null) return;

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

        object attestationResponse;
        try
        {
            attestationResponse = (await webAuthnService.CreateWebAuthnCredential(options));
        }
        catch (JSException ex)
        {
            // we can safely handle the exception thrown here since it mostly because of a timeout or user cancelling the native ui.
            ExceptionHandler.Handle(ex, AppEnvironment.IsDev() ? ExceptionDisplayKind.NonInterrupting : ExceptionDisplayKind.None);
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
            // we can safely handle the exception thrown here since it mostly because of a timeout or user cancelling the native ui.
            ExceptionHandler.Handle(ex, ExceptionDisplayKind.None);
            return;
        }

        var verifyResult = await identityController
            .WithQueryIf(AppPlatform.IsBlazorHybrid, "origin", localHttpServer.Origin)
            .VerifyWebAuthAssertion(assertion, CurrentCancellationToken);

        await userController
            .WithQueryIf(AppPlatform.IsBlazorHybrid, "origin", localHttpServer.Origin)
            .DeleteWebAuthnCredential(assertion, CurrentCancellationToken);

        await webAuthnService.RemoveWebAuthnConfiguredUserId(User.Id);

        isConfigured = false;

        SnackBarService.Success(Localizer[nameof(AppStrings.DisablePasswordlessSucsessMessage)]);
    }

    // Only for debugging purposes, uncomment the following lines and the corresponding lines in the razor file.
    //private async Task DeleteAll()
    //{
    //    await userController.DeleteAllWebAuthnCredentials(CurrentCancellationToken);

    //    await webAuthnService.RemoveWebAuthnConfigured();

    //    isConfigured = false;
    //}

    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        if (AppPlatform.IsBlazorHybrid)
        {
            localHttpServer.EnsureStarted();
        }
    }
}

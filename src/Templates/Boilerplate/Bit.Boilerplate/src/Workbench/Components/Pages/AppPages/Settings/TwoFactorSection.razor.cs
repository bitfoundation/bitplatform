namespace Boilerplate.Workbench.Components.Pages.AppPages.Settings;

public partial class TwoFactorSection
{
    private bool isWaiting;
    private bool isFailed;
    private bool isLoading = true;
    private bool isKeyCopiedShown;
    private bool isCodesCopiedShown;
    private string? verificationCode;

    [AutoInject] private SettingsStore store = default!;

    [CascadingParameter] public SettingsPage Page { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        Subscribe(store);

        if (IsPrerendering)
            return;

        await WhenStateRestored();
        await Load();
    }

    private async Task Load()
    {
        isLoading = true;
        isFailed = false;

        try
        {
            await ScenarioState.Load("settings.two-factor", () => true, () => false, CurrentCancellationToken);
            isLoading = false;
        }
        catch (WorkbenchSimulatedException)
        {
            isFailed = true;
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task EnableTwoFactorAuth()
    {
        if (string.IsNullOrWhiteSpace(verificationCode))
        {
            await Page.SnackBar!.Error(AppLocalizer[nameof(AppStrings.TfaVerificationCodeRequiredMessage)]);
            return;
        }

        if (await Send() is false)
            return;

        store.SetTwoFactor(true, SettingsDemoData.CreateRecoveryCodes());
        verificationCode = null;

        await Page.SnackBar!.Success(AppLocalizer[nameof(AppStrings.TwoFactorAuthenticationEnabled)]);
    }

    private async Task DisableTwoFactorAuth()
    {
        if (await Send() is false)
            return;

        store.SetTwoFactor(false, null);

        await Page.SnackBar!.Success(AppLocalizer[nameof(AppStrings.TwoFactorAuthenticationDisabled)]);
    }

    private async Task GenerateRecoveryCode()
    {
        if (await Send())
        {
            store.SetTwoFactor(true, SettingsDemoData.CreateRecoveryCodes());
        }
    }

    private async Task ResetAuthenticatorKey()
    {
        if (await Send())
        {
            await Page.SnackBar!.Warning(AppLocalizer[nameof(AppStrings.SignOutOfAllDevicesWarningMessage)]);
        }
    }

    private async Task<bool> Send()
    {
        if (isWaiting)
            return false;

        isWaiting = true;

        try
        {
            await ScenarioState.Save(CurrentCancellationToken);

            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        finally
        {
            isWaiting = false;
        }
    }

    private async Task CopySharedKeyToClipboard()
    {
        if (isKeyCopiedShown)
            return;

        await JSRuntime.InvokeVoidAsync("workbench.copy", SettingsDemoData.SharedKey);

        isKeyCopiedShown = true;
        StateHasChanged();

        await Task.Delay(1000);

        isKeyCopiedShown = false;
        StateHasChanged();
    }

    private async Task CopyRecoveryCodesToClipboard()
    {
        if (isCodesCopiedShown)
            return;

        await JSRuntime.InvokeVoidAsync("workbench.copy", string.Join(Environment.NewLine, store.RecoveryCodes ?? []));

        isCodesCopiedShown = true;
        StateHasChanged();

        await Task.Delay(1000);

        isCodesCopiedShown = false;
        StateHasChanged();
    }
}

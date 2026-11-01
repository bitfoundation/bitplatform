namespace Boilerplate.Workbench.Components.Pages.AppPages.Settings;

/// <summary>The Workbench copy of the app's two-factor section: any code is accepted, and every change lands in the store.</summary>
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

    /// <summary>A load with nothing to fetch, so the section shows the loading and error states the app's does.</summary>
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
            SnackBarService.Error(AppLocalizer[nameof(AppStrings.TfaVerificationCodeRequiredMessage)]);
            return;
        }

        if (await Send() is false)
            return;

        store.SetTwoFactor(true, SettingsDemoData.CreateRecoveryCodes());
        verificationCode = null;

        SnackBarService.Success(AppLocalizer[nameof(AppStrings.TwoFactorAuthenticationEnabled)]);
    }

    private async Task DisableTwoFactorAuth()
    {
        if (await Send() is false)
            return;

        store.SetTwoFactor(false, null);

        SnackBarService.Success(AppLocalizer[nameof(AppStrings.TwoFactorAuthenticationDisabled)]);
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
            SnackBarService.Warning(AppLocalizer[nameof(AppStrings.SignOutOfAllDevicesWarningMessage)]);
        }
    }

    /// <summary>Stands in for a request: false when one is already running or the section closed meanwhile.</summary>
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
        if (isKeyCopiedShown || await Copy(SettingsDemoData.SharedKey) is false)
            return;

        isKeyCopiedShown = true;
        StateHasChanged();

        await Task.Delay(1000);

        isKeyCopiedShown = false;
        StateHasChanged();
    }

    private async Task CopyRecoveryCodesToClipboard()
    {
        if (isCodesCopiedShown || await Copy(string.Join(Environment.NewLine, store.RecoveryCodes ?? [])) is false)
            return;

        isCodesCopiedShown = true;
        StateHasChanged();

        await Task.Delay(1000);

        isCodesCopiedShown = false;
        StateHasChanged();
    }

    /// <summary>Copies the text, or shows an error when the browser refuses, as one that allows copying only straight from a tap does.</summary>
    private async Task<bool> Copy(string text)
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("workbench.copy", text);

            return true;
        }
        catch (JSException)
        {
            SnackBarService.Error(Localizer[nameof(WorkbenchStrings.CopyFailed)]);

            return false;
        }
    }
}

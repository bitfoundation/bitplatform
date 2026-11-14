using System.Text.Json;
using Boilerplate.Workbench.Components.Pages.AppPages.Settings.Data;

namespace Boilerplate.WorkbenchTests.Features.AppPages;

/// <summary>The Settings page's two-factor section: setting it up, the recovery codes and their warnings, resetting the key and turning it off.</summary>
[TestClass, TestCategory("UITest")]
public class TwoFactorSectionUITests : SettingsPageTest
{
    private ILocator TwoFactor => Section(AppStrings.TfaTitle);

    private ILocator Verify => TwoFactor.GetByRole(AriaRole.Button, new() { Name = AppStrings.TfaConfigureAutAppVerifyButtonText, Exact = true });

    private ILocator Code => TwoFactor.GetByRole(AriaRole.Group, new() { Name = AppStrings.TfaConfigureAutAppVerificationCodeLabel, Exact = true });

    private ILocator RecoveryCodes => TwoFactor.GetByText(new Regex(@"^\d{5}-\d{5}$"));

    private ILocator Tab(string name) => TwoFactor.GetByRole(AriaRole.Tab, new() { Name = name, Exact = true });

    /// <summary>Opens a preview alone on the two-factor section of the sample account, with two-factor on and this many recovery codes left, which nothing on the page can set.</summary>
    private async Task OpenWithTwoFactorOn(int recoveryCodesLeft)
    {
        var (user, sessions) = SettingsSampleAccount.Sample(0, WorkbenchStrings.DemoUserYas, DateTimeOffset.UtcNow);

        // The account as the view keeps it (SettingsStore), made for the first user in English with two-factor on.
        var account = JsonSerializer.Serialize(new
        {
            ShapedFor = "0/en-US/True/False",
            User = user,
            Sessions = sessions,
            IsTwoFactorEnabled = true,
            AuthenticatorKey = SettingsSampleAccount.FirstKey,
            RecoveryCodesLeft = recoveryCodesLeft,
            HasPasskey = false,
            SeenUpload = WorkbenchProfilePictures.LastUpload(user.Id)
        });

        await OpenSettingsStore(account, $"&{WorkbenchScenario.SwitchQueryPrefix}section={PageUrls.SettingsSections.Tfa}&{WorkbenchScenario.SwitchQueryPrefix}{SettingsSampleAccount.TwoFactorSwitch}={SettingsSampleAccount.On}");
    }

    private async Task TurnOn()
    {
        await OpenSection(AppStrings.TfaTitle);
        await TypeCode(Code);

        await Expect(Snack(AppStrings.TwoFactorAuthenticationEnabled)).ToBeVisibleAsync();
        await Expect(Tab(AppStrings.TfaRecoveryCodesHeader)).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheSetup_Should_ShowTheStepsWithTheCodeAndKey(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.TfaTitle);

        await Expect(TwoFactor.GetByText(AppStrings.TfaConfigureAutAppTitle, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(TwoFactor.GetByRole(AriaRole.Img, new() { Name = AppStrings.TfaQrCodeAlt, Exact = true })).ToBeVisibleAsync();
        await Expect(TwoFactor.GetByText(SettingsSampleAccount.Format(SettingsSampleAccount.FirstKey), new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Code.GetByRole(AriaRole.Textbox)).ToHaveCountAsync(6);
        await Expect(TwoFactor.GetByRole(AriaRole.Link, new() { Name = "Android", Exact = true })).ToBeVisibleAsync();
        await Expect(TwoFactor.GetByRole(AriaRole.Link, new() { Name = "iOS", Exact = true })).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task Verifying_Should_RefuseAnEmptyCode(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.TfaTitle);

        await Press(Verify);

        await Expect(Snack(AppStrings.TfaVerificationCodeRequiredMessage)).ToBeVisibleAsync();
        await Expect(Verify).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheVerifyButton_Should_RefuseAnIncompleteCode(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.TfaTitle);

        await TypeCode(Code, "123");
        await Press(Verify);

        // As the server's: only a code the authenticator app shows, six digits here, turns it on.
        await Expect(View().GetByText(AppStrings.TfaInvalidCodeError, new() { Exact = true }).First).ToBeVisibleAsync();
        await Expect(Verify).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task AFullCode_Should_TurnItOnAndShowTenRecoveryCodesOnlyOnce(WorkbenchSurface surface)
    {
        await TurnOn();

        await Expect(RecoveryCodes).ToHaveCountAsync(10);

        await ReloadTheViewOnceSaved("\"IsTwoFactorEnabled\":true");

        // As the server keeps them: on, with the codes it can't show again, and a way to make new ones.
        await Expect(Tab(AppStrings.TfaRecoveryCodesHeader)).ToBeVisibleAsync();
        await Expect(RecoveryCodes).ToHaveCountAsync(0);
        await Expect(TwoFactor.GetByText(AppStrings.TfaRecoveryCodesGenerateWraning)).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheKey_Should_BeCopied(WorkbenchSurface surface)
    {
        await Context.GrantPermissionsAsync(["clipboard-read", "clipboard-write"]);

        await OpenSection(AppStrings.TfaTitle);

        await Press(TwoFactor.GetByRole(AriaRole.Button, new() { Name = AppStrings.Copy, Exact = true }));

        await Expect(TwoFactor.GetByRole(AriaRole.Button, new() { Name = AppStrings.Copied, Exact = true })).ToBeVisibleAsync();
        await WorkbenchTestReviewer.WaitFor(() => Page.EvaluateAsync<string>("() => navigator.clipboard.readText()"), text => text == SettingsSampleAccount.Format(SettingsSampleAccount.FirstKey));
        await Expect(TwoFactor.GetByRole(AriaRole.Button, new() { Name = AppStrings.Copy, Exact = true })).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task ARefusedCopy_Should_NotSayItCopied(WorkbenchSurface surface)
    {
        await Context.AddInitScriptAsync("navigator.clipboard.writeText = () => Promise.reject(new Error('Refused'))");

        await OpenSection(AppStrings.TfaTitle);

        await Press(TwoFactor.GetByRole(AriaRole.Button, new() { Name = AppStrings.Copy, Exact = true }));

        await Expect(TwoFactor.GetByRole(AriaRole.Button, new() { Name = AppStrings.Copy, Exact = true })).ToBeVisibleAsync();
        await Expect(TwoFactor.GetByRole(AriaRole.Button, new() { Name = AppStrings.Copied, Exact = true })).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task TheRecoveryCodes_Should_BeCopied(WorkbenchSurface surface)
    {
        await Context.GrantPermissionsAsync(["clipboard-read", "clipboard-write"]);

        await TurnOn();

        var first = await RecoveryCodes.First.InnerTextAsync();

        await Press(TwoFactor.GetByRole(AriaRole.Button, new() { Name = AppStrings.Copy, Exact = true }));

        await Expect(TwoFactor.GetByRole(AriaRole.Button, new() { Name = AppStrings.Copied, Exact = true })).ToBeVisibleAsync();
        await WorkbenchTestReviewer.WaitFor(() => Page.EvaluateAsync<string>("() => navigator.clipboard.readText()"), text => text.StartsWith(first, StringComparison.Ordinal));
    }

    [TestMethod, EverySurface]
    public async Task ResettingTheKey_Should_WarnAboutSigningOut(WorkbenchSurface surface)
    {
        await TurnOn();

        await Press(Tab(AppStrings.TfaAuthAppHeader));
        await Expect(TwoFactor.GetByText(AppStrings.TfaAuthAppWarning)).ToBeVisibleAsync();

        await Press(TwoFactor.GetByRole(AriaRole.Button, new() { Name = AppStrings.TfaAuthAppResetKeyButtonText, Exact = true }));

        await Expect(Snack(AppStrings.SignOutOfAllDevicesWarningMessage)).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task Disabling_Should_BringTheSetupBackAndStayAfterAReload(WorkbenchSurface surface)
    {
        await TurnOn();

        await Press(Tab(AppStrings.TfaDisable2faHeader));
        await Expect(TwoFactor.GetByText(AppStrings.TfaDisable2faWarning)).ToBeVisibleAsync();

        await Press(TwoFactor.GetByRole(AriaRole.Button, new() { Name = AppStrings.TfaDisable2faButtonText, Exact = true }));

        await Expect(Snack(AppStrings.TwoFactorAuthenticationDisabled)).ToBeVisibleAsync();
        await Expect(Verify).ToBeVisibleAsync();

        await ReloadTheViewOnceSaved("\"IsTwoFactorEnabled\":false");

        await Expect(Verify).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task NoRecoveryCodes_Should_OfferToGenerateThem(WorkbenchSurface surface)
    {
        await OpenWithTwoFactorOn(recoveryCodesLeft: 10);

        var section = Page.GetByRole(AriaRole.Region, new() { Name = AppStrings.TfaTitle, Exact = true });

        await Expect(section.GetByText(AppStrings.TfaRecoveryCodesGenerateWraning)).ToBeVisibleAsync();

        await Press(section.GetByRole(AriaRole.Button, new() { Name = AppStrings.TfaRecoveryCodesGenerateButtonText, Exact = true }));

        await Expect(section.GetByText(new Regex(@"^\d{5}-\d{5}$"))).ToHaveCountAsync(10);
        await Expect(section.GetByText(AppStrings.TfaRecoveryCodesGenerateWraning)).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task FewRecoveryCodesLeft_Should_WarnMoreTheFewerTheyAre(WorkbenchSurface surface)
    {
        foreach (var (left, title) in new[]
        {
            (3, Message(AppStrings.TfaRecoveryCodesThreeLeftTitle, "3")),
            (1, AppStrings.TfaRecoveryCodesOneLeftTitle),
            (0, AppStrings.TfaRecoveryCodesZeroLeftTitle)
        })
        {
            await OpenWithTwoFactorOn(left);

            await Expect(Page.GetByRole(AriaRole.Region, new() { Name = AppStrings.TfaTitle, Exact = true }).GetByText(title, new() { Exact = true })).ToBeVisibleAsync();
        }
    }
}

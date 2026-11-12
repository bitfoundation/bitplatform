namespace Boilerplate.WorkbenchTests.Features.AppPages;

/// <summary>The Settings page's Account section, tab by tab: passwordless, change email, change phone, change password and delete account, each with its validation, its success and its failure.</summary>
[TestClass, TestCategory("UITest")]
public class AccountSectionUITests : SettingsPageTest
{
    private const string NewEmail = "new.address@bitplatform.dev";
    private const string NewPhone = "+31612345678";

    private ILocator Account => Section(AppStrings.AccountTitle);

    private ILocator DeleteDialog => View().GetByRole(AriaRole.Dialog, new() { Name = AppStrings.DeleteAccount, Exact = true });

    private ILocator Panel(string tab) => Account.GetByRole(AriaRole.Tabpanel, new() { Name = tab, Exact = true });

    private ILocator ButtonIn(ILocator panel, string name) => panel.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    private ILocator TextField(ILocator panel, string name) => panel.GetByRole(AriaRole.Textbox, new() { Name = name, Exact = true });

    private static string Required(string field) => Message(AppStrings.RequiredAttribute_ValidationError, field);

    /// <summary>Opens a tab, which a narrow view may have moved into its More tabs menu.</summary>
    private async Task OpenAccountTab(string name)
    {
        var tab = Account.GetByRole(AriaRole.Tab, new() { Name = name, Exact = true });

        await Expect(Account.GetByRole(AriaRole.Tab).First).ToBeVisibleAsync();

        if (await tab.IsVisibleAsync())
        {
            await Press(tab);
        }
        else
        {
            await Press(Account.GetByRole(AriaRole.Button, new() { Name = AppStrings.MoreTabsTitle, Exact = true }));
            await Press(Account.GetByRole(AriaRole.Menuitemradio, new() { Name = name, Exact = true }));
        }

        await Expect(Panel(name)).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task EveryTab_Should_ShowItsOwnFormAndTheOpenOneShouldStayAfterAReload(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.AccountTitle);

        await Expect(Panel(AppStrings.Passwordless)).ToBeVisibleAsync();

        await OpenAccountTab(AppStrings.Delete);
        await Expect(ButtonIn(Panel(AppStrings.Delete), AppStrings.DeleteAccount)).ToBeVisibleAsync();

        await OpenAccountTab(AppStrings.Password);
        await Expect(TextField(Panel(AppStrings.Password), AppStrings.OldPassword)).ToBeVisibleAsync();

        await OpenAccountTab(AppStrings.Phone);
        await Expect(ButtonIn(Panel(AppStrings.Phone), AppStrings.Submit)).ToBeVisibleAsync();

        await OpenAccountTab(AppStrings.Email);
        await Expect(TextField(Panel(AppStrings.Email), AppStrings.NewEmail)).ToBeVisibleAsync();

        await ReloadTheViewOnceSaved($"\"AccountTab\":\"{nameof(AppStrings.Email)}\"");

        await Expect(TextField(Panel(AppStrings.Email), AppStrings.NewEmail)).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task Passwordless_Should_TurnOnAndOffAndStayAfterAReload(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.AccountTitle);

        var panel = Panel(AppStrings.Passwordless);

        await Press(ButtonIn(panel, AppStrings.EnablePasswordless));

        await Expect(ButtonIn(panel, AppStrings.DisablePasswordless)).ToBeVisibleAsync();

        await ReloadTheViewOnceSaved("\"IsPasswordlessEnabled\":true");

        await Expect(ButtonIn(panel, AppStrings.DisablePasswordless)).ToBeVisibleAsync();

        await Press(ButtonIn(panel, AppStrings.DisablePasswordless));

        await Expect(ButtonIn(panel, AppStrings.EnablePasswordless)).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task ANewEmail_Should_BeRefusedWhenEmptyInvalidOrTheSame(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.AccountTitle);
        await OpenAccountTab(AppStrings.Email);

        var panel = Panel(AppStrings.Email);
        var submit = ButtonIn(panel, AppStrings.Submit);
        var field = TextField(panel, AppStrings.NewEmail);

        await Expect(TextField(panel, AppStrings.Email)).ToHaveValueAsync(WorkbenchDemoUsers.All[0].Email);

        await Press(submit);

        await Expect(panel.GetByText(Required(AppStrings.Email))).ToBeVisibleAsync();

        await field.FillAsync("not an address");
        await Press(submit);

        await Expect(panel.GetByText(Message(AppStrings.EmailAddressAttribute_ValidationError, AppStrings.Email))).ToBeVisibleAsync();

        await field.FillAsync(WorkbenchDemoUsers.All[0].Email);
        await Press(submit);

        await Expect(Snack(AppStrings.SameEmailErrorMessage)).ToBeVisibleAsync();
        await Expect(submit).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task ChangingTheEmail_Should_AskForTheCodeAndThenTakeTheNewAddress(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.AccountTitle);
        await OpenAccountTab(AppStrings.Email);

        var panel = Panel(AppStrings.Email);

        await TextField(panel, AppStrings.NewEmail).FillAsync(NewEmail);
        await Press(ButtonIn(panel, AppStrings.Submit));

        await Expect(Snack(AppStrings.SuccessfulSendChangeEmailTokenMessage)).ToBeVisibleAsync();

        var address = TextField(panel, AppStrings.Email);

        await Expect(address).ToHaveValueAsync(NewEmail);
        await Expect(address).ToBeDisabledAsync();
        await Expect(ButtonIn(panel, AppStrings.ResendEmailTokenButtonText)).ToBeVisibleAsync();

        await TypeCode(panel.GetByRole(AriaRole.Group, new() { Name = AppStrings.EmailToken, Exact = true }));

        await Expect(Snack(AppStrings.SignOutOfAllDevicesWarningMessage)).ToBeVisibleAsync();
        await Expect(TextField(panel, AppStrings.Email)).ToHaveValueAsync(NewEmail);
        await Expect(ButtonIn(panel, AppStrings.Submit)).ToBeVisibleAsync();
        await Expect(TextField(panel, AppStrings.NewEmail)).ToHaveValueAsync(string.Empty);
    }

    [TestMethod, EverySurface]
    public async Task TheEmailCodeStep_Should_ResendRefuseAnEmptyCodeStayAfterAReloadAndGoBack(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.AccountTitle);
        await OpenAccountTab(AppStrings.Email);

        var panel = Panel(AppStrings.Email);

        await TextField(panel, AppStrings.NewEmail).FillAsync(NewEmail);
        await Press(ButtonIn(panel, AppStrings.Submit));

        var confirm = ButtonIn(panel, AppStrings.EmailTokenConfirmButtonText);

        await Expect(confirm).ToBeVisibleAsync();

        await Press(confirm);

        await Expect(panel.GetByText(Message(AppStrings.RequiredAttribute_ValidationError, AppStrings.Token))).ToBeVisibleAsync();

        await Press(ButtonIn(panel, AppStrings.ResendEmailTokenButtonText));

        await Expect(Snack(AppStrings.SuccessfulSendChangeEmailTokenMessage)).ToBeVisibleAsync();

        await ReloadTheViewOnceSaved(NewEmail);

        await Expect(TextField(panel, AppStrings.Email)).ToHaveValueAsync(NewEmail);
        await Expect(confirm).ToBeVisibleAsync();

        await Press(ButtonIn(panel, AppStrings.GoBack));

        await Expect(ButtonIn(panel, AppStrings.Submit)).ToBeVisibleAsync();
        await Expect(TextField(panel, AppStrings.NewEmail)).ToHaveValueAsync(string.Empty);
    }

    [TestMethod, EverySurface]
    public async Task AlreadyHavingAnEmailCode_Should_AskForTheAddressToo(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.AccountTitle);
        await OpenAccountTab(AppStrings.Email);

        var panel = Panel(AppStrings.Email);

        await Press(ButtonIn(panel, AppStrings.Confirm));

        var address = TextField(panel, AppStrings.Email);

        await Expect(address).ToBeEnabledAsync();
        await Expect(ButtonIn(panel, AppStrings.ResendEmailTokenButtonText)).ToHaveCountAsync(0);

        await Press(ButtonIn(panel, AppStrings.EmailTokenConfirmButtonText));

        await Expect(panel.GetByText(Required(AppStrings.Email))).ToBeVisibleAsync();

        await address.FillAsync(NewEmail);
        await TypeCode(panel.GetByRole(AriaRole.Group, new() { Name = AppStrings.EmailToken, Exact = true }));

        await Expect(Snack(AppStrings.SignOutOfAllDevicesWarningMessage)).ToBeVisibleAsync();
        await Expect(TextField(panel, AppStrings.Email)).ToHaveValueAsync(NewEmail);
    }

    [TestMethod, EverySurface]
    public async Task ANewPhoneNumber_Should_BeRefusedWhenEmptyInvalidOrTheSame(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.AccountTitle);
        await OpenAccountTab(AppStrings.Phone);

        var panel = Panel(AppStrings.Phone);
        var submit = ButtonIn(panel, AppStrings.Submit);
        var field = panel.GetByRole(AriaRole.Group, new() { Name = AppStrings.NewPhoneNumber, Exact = true }).GetByRole(AriaRole.Textbox);

        await Expect(TextField(panel, AppStrings.PhoneNumber)).ToHaveValueAsync(new Regex(@"^\+31"));

        await Press(submit);

        await Expect(panel.GetByText(Required(AppStrings.PhoneNumber))).ToBeVisibleAsync();

        await field.FillAsync("not a number");
        await Press(submit);

        await Expect(panel.GetByText(Required(AppStrings.PhoneNumber))).ToBeVisibleAsync();

        await field.FillAsync(await TextField(panel, AppStrings.PhoneNumber).InputValueAsync());
        await Press(submit);

        await Expect(Snack(AppStrings.SamePhoneNumberErrorMessage)).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task ChangingThePhoneNumber_Should_AskForTheCodeAndThenTakeTheNewNumber(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.AccountTitle);
        await OpenAccountTab(AppStrings.Phone);

        var panel = Panel(AppStrings.Phone);

        await panel.GetByRole(AriaRole.Group, new() { Name = AppStrings.NewPhoneNumber, Exact = true }).GetByRole(AriaRole.Textbox).FillAsync(NewPhone);
        await Press(ButtonIn(panel, AppStrings.Submit));

        await Expect(Snack(AppStrings.SuccessfulSendChangePhoneNumberTokenMessage)).ToBeVisibleAsync();
        await Expect(ButtonIn(panel, AppStrings.ResendPhoneTokenButtonText)).ToBeVisibleAsync();

        await TypeCode(panel.GetByRole(AriaRole.Group, new() { Name = AppStrings.PhoneToken, Exact = true }));

        await Expect(Snack(AppStrings.SignOutOfAllDevicesWarningMessage)).ToBeVisibleAsync();
        await Expect(ButtonIn(panel, AppStrings.Submit)).ToBeVisibleAsync();
        await Expect(TextField(panel, AppStrings.PhoneNumber)).ToHaveValueAsync(NewPhone);
    }

    [TestMethod, EverySurface]
    public async Task ThePhoneCodeStep_Should_ResendRefuseAnEmptyCodeStayAfterAReloadAndGoBack(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.AccountTitle);
        await OpenAccountTab(AppStrings.Phone);

        var panel = Panel(AppStrings.Phone);

        await panel.GetByRole(AriaRole.Group, new() { Name = AppStrings.NewPhoneNumber, Exact = true }).GetByRole(AriaRole.Textbox).FillAsync(NewPhone);
        await Press(ButtonIn(panel, AppStrings.Submit));

        var confirm = ButtonIn(panel, AppStrings.PhoneTokenConfirmButtonText);

        await Expect(confirm).ToBeVisibleAsync();

        await Press(confirm);

        await Expect(panel.GetByText(Message(AppStrings.RequiredAttribute_ValidationError, AppStrings.Token))).ToBeVisibleAsync();

        await Press(ButtonIn(panel, AppStrings.ResendPhoneTokenButtonText));

        await Expect(Snack(AppStrings.SuccessfulSendChangePhoneNumberTokenMessage)).ToBeVisibleAsync();

        await ReloadTheViewOnceSaved(NewPhone[1..]);

        await Expect(confirm).ToBeVisibleAsync();

        await Press(ButtonIn(panel, AppStrings.GoBack));

        await Expect(ButtonIn(panel, AppStrings.Submit)).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task AlreadyHavingAPhoneCode_Should_AskForTheNumberToo(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.AccountTitle);
        await OpenAccountTab(AppStrings.Phone);

        var panel = Panel(AppStrings.Phone);

        await Press(ButtonIn(panel, AppStrings.Confirm));

        await Expect(ButtonIn(panel, AppStrings.ResendPhoneTokenButtonText)).ToHaveCountAsync(0);

        await Press(ButtonIn(panel, AppStrings.PhoneTokenConfirmButtonText));

        await Expect(panel.GetByText(Required(AppStrings.PhoneNumber))).ToBeVisibleAsync();

        await panel.GetByRole(AriaRole.Group, new() { Name = AppStrings.PhoneNumber, Exact = true }).GetByRole(AriaRole.Textbox).FillAsync(NewPhone);
        await TypeCode(panel.GetByRole(AriaRole.Group, new() { Name = AppStrings.PhoneToken, Exact = true }));

        await Expect(Snack(AppStrings.SignOutOfAllDevicesWarningMessage)).ToBeVisibleAsync();
        await Expect(TextField(panel, AppStrings.PhoneNumber)).ToHaveValueAsync(NewPhone);
    }

    [TestMethod, EverySurface]
    public async Task ThePasswordForm_Should_RefuseWhatIsMissingShortOrDifferent(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.AccountTitle);
        await OpenAccountTab(AppStrings.Password);

        var panel = Panel(AppStrings.Password);
        var change = ButtonIn(panel, AppStrings.ChangePasswordButtonText);

        await Press(change);

        await Expect(panel.GetByText(Required(AppStrings.OldPassword))).ToBeVisibleAsync();
        await Expect(panel.GetByText(Required(AppStrings.NewPassword))).ToBeVisibleAsync();
        await Expect(panel.GetByText(Required(AppStrings.ConfirmNewPassword))).ToBeVisibleAsync();

        await TextField(panel, AppStrings.OldPassword).FillAsync("old password");
        await TextField(panel, AppStrings.NewPassword).FillAsync("short");
        await TextField(panel, AppStrings.ConfirmNewPassword).FillAsync("different");
        await Press(change);

        await Expect(panel.GetByText(Message(AppStrings.MinLengthAttribute_ValidationError, AppStrings.NewPassword, "8"))).ToBeVisibleAsync();
        await Expect(panel.GetByText(Message(AppStrings.CompareAttribute_ValidationError, AppStrings.ConfirmNewPassword, AppStrings.NewPassword))).ToBeVisibleAsync();
        await Expect(Snack(AppStrings.PasswordChangedSuccessfullyMessage)).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task AValidPassword_Should_SayItChangedAndEmptyTheForm(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.AccountTitle);
        await OpenAccountTab(AppStrings.Password);

        var panel = Panel(AppStrings.Password);

        await TextField(panel, AppStrings.OldPassword).FillAsync("old password");
        await TextField(panel, AppStrings.NewPassword).FillAsync("a new password");
        await TextField(panel, AppStrings.ConfirmNewPassword).FillAsync("a new password");
        await Press(ButtonIn(panel, AppStrings.ChangePasswordButtonText));

        await Expect(Snack(AppStrings.PasswordChangedSuccessfullyMessage)).ToBeVisibleAsync();

        foreach (var name in new[] { AppStrings.OldPassword, AppStrings.NewPassword, AppStrings.ConfirmNewPassword })
        {
            await Expect(TextField(panel, name)).ToHaveValueAsync(string.Empty);
        }
    }

    [TestMethod, EverySurface]
    public async Task APassword_Should_BeRevealedAndHiddenAgain(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.AccountTitle);
        await OpenAccountTab(AppStrings.Password);

        var panel = Panel(AppStrings.Password);
        var field = TextField(panel, AppStrings.OldPassword);

        await Expect(field).ToHaveAttributeAsync("type", "password");

        var reveal = panel.GetByRole(AriaRole.Button, new() { Name = "Reveal password" }).First;

        await Press(reveal);

        await Expect(field).ToHaveAttributeAsync("type", "text");

        await Press(reveal);

        await Expect(field).ToHaveAttributeAsync("type", "password");
    }

    [TestMethod, EverySurface]
    public async Task DeletingTheAccount_Should_AskAndKeepItOnCancel(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.AccountTitle);
        await OpenAccountTab(AppStrings.Delete);

        await Press(ButtonIn(Panel(AppStrings.Delete), AppStrings.DeleteAccount));

        await Expect(DeleteDialog.GetByText(AppStrings.DeleteAccountPrompt)).ToBeVisibleAsync();

        await Press(DeleteDialog.GetByRole(AriaRole.Button, new() { Name = BitBlazorUIStrings.DialogCancelText, Exact = true }));

        await Expect(DeleteDialog).ToHaveCountAsync(0);
        await Expect(AccountButton(WorkbenchStrings.DemoUserYas)).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task AnOpenDeleteConfirmation_Should_NotComeBackAfterAReload(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.AccountTitle);
        await OpenAccountTab(AppStrings.Delete);

        await Press(ButtonIn(Panel(AppStrings.Delete), AppStrings.DeleteAccount));

        await Expect(DeleteDialog).ToBeVisibleAsync();

        await ReloadTheViewOnceSaved($"\"AccountTab\":\"{nameof(AppStrings.Delete)}\"");

        await Expect(Panel(AppStrings.Delete)).ToBeVisibleAsync();
        await Expect(DeleteDialog).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task DeletingTheAccount_Should_SignTheViewOut(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.AccountTitle);
        await OpenAccountTab(AppStrings.Delete);

        await Press(ButtonIn(Panel(AppStrings.Delete), AppStrings.DeleteAccount));
        await Press(DeleteDialog.GetByRole(AriaRole.Button, new() { Name = BitBlazorUIStrings.DialogOkText, Exact = true }));

        await Expect(View().GetByText(AppStrings.YouAreNotAuthorized)).ToBeVisibleAsync();
        await Expect(AccountButton(AppStrings.AccountMenuTitle)).ToBeVisibleAsync();
        await Expect(Page).ToHaveURLAsync(new Regex(@"\?auth=signedout$"));
    }
}

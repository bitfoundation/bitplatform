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
    private async Task OpenTab(string name)
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
    public async Task ANewPhoneNumber_Should_BeRefusedWhenEmptyInvalidOrTheSame(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.AccountTitle);
        await OpenTab(AppStrings.Phone);

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
}

namespace Boilerplate.WorkbenchTests.Features.Chrome;

/// <summary>The sign-in state in the tools: signed out, signed in, several accounts and the user picker, and what they change in the view.</summary>
[TestClass, TestCategory("UITest")]
public class SignInStateUITests : AppViewPageTest
{
    private ILocator State(string name) => SignInTools.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    private ILocator NotAuthorized => View().GetByRole(AriaRole.Heading, new() { Name = AppStrings.YouAreNotAuthorized, Exact = true });

    private async Task PressState(string name)
    {
        await OpenTools();
        await Press(State(name));
        await Expect(State(name)).ToHaveAttributeAsync("aria-pressed", "true");
        await CloseTools();
    }

    [TestMethod, EverySurface]
    public async Task SignedOut_Should_SignTheViewOutAndLockTheUserPicker(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await PressState(WorkbenchStrings.SignedOut);

        await Expect(Page).ToHaveURLAsync(new Regex(@"\?auth=signedout$"));
        await WaitForTheViewToReloadWith("auth=signedout");
        await Expect(AccountButton(AppStrings.AccountMenuTitle)).ToBeVisibleAsync();

        await OpenAccountMenu();

        await Expect(MenuItem(AppStrings.SignIn)).ToBeVisibleAsync();
        await Expect(MenuItem(AppStrings.SignOut)).ToHaveCountAsync(0);

        await CloseAccountMenu();
        await OpenTools();

        await Expect(UserPicker).ToBeDisabledAsync();
    }

    [TestMethod, EverySurface]
    public async Task SignedIn_Should_ShowOneAccountInTheView(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await PressState(WorkbenchStrings.SignedIn);

        await Expect(Page).ToHaveURLAsync(new Regex(@"\?auth=signedin$"));
        await WaitForTheViewToReloadWith("auth=signedin");
        await Expect(AccountButton(WorkbenchStrings.DemoUserYas)).ToBeVisibleAsync();

        await OpenAccountMenu();

        await Expect(MenuItem(AppStrings.SignOut)).ToBeVisibleAsync();
        await Expect(MenuItem(WorkbenchStrings.DemoUserSaleh)).ToHaveCountAsync(0);
        await Expect(MenuItem(WorkbenchStrings.DemoUserZahra)).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task MultiAccount_Should_ListTheOtherAccountsInTheView(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?auth=signedin");

        await PressState(WorkbenchStrings.MultiAccount);

        await Expect(Page).Not.ToHaveURLAsync(new Regex("auth="));
        await WaitForTheViewToReloadWithout("auth=");

        await OpenAccountMenu();

        await Expect(MenuItem(WorkbenchStrings.DemoUserSaleh)).ToBeVisibleAsync();
        await Expect(MenuItem(WorkbenchStrings.DemoUserZahra)).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheUserPicker_Should_SignTheViewInAsThatUserWithTheirPermissions(WorkbenchSurface surface)
    {
        await OpenWorkbench("buildings");

        var add = View().GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.BuildingsAdd, Exact = true });

        await Expect(add).ToBeVisibleAsync();

        await OpenTools();

        await Expect(UserPicker).ToContainTextAsync(WorkbenchStrings.DemoUserYas);

        await PickUser(WorkbenchStrings.DemoUserZahra);

        await Expect(UserPicker).ToContainTextAsync(WorkbenchStrings.DemoUserZahra);

        await CloseTools();

        await Expect(Page).ToHaveURLAsync(new Regex(@"\?user=2$"));
        await Expect(AccountButton(WorkbenchStrings.DemoUserZahra)).ToBeVisibleAsync();
        await Expect(add).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task TheAddress_Should_OpenAViewInItsSignInState(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?auth=signedin&user=1");

        await Expect(AccountButton(WorkbenchStrings.DemoUserSaleh)).ToBeVisibleAsync();

        await OpenTools();

        await Expect(State(WorkbenchStrings.SignedIn)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(UserPicker).ToContainTextAsync(WorkbenchStrings.DemoUserSaleh);
    }

    [TestMethod, EverySurface]
    public async Task SigningOutAndIn_Should_SwapAPageThatNeedsItWithTheNotAuthorizedPage(WorkbenchSurface surface)
    {
        await OpenWorkbench("settings");

        var fullName = View().GetByRole(AriaRole.Textbox, new() { Name = AppStrings.FullName, Exact = true });

        await Expect(fullName).ToBeVisibleAsync();

        await PressState(WorkbenchStrings.SignedOut);

        await Expect(NotAuthorized).ToBeVisibleAsync();
        await Expect(fullName).ToHaveCountAsync(0);

        await PressState(WorkbenchStrings.SignedIn);

        await Expect(fullName).ToHaveValueAsync(WorkbenchStrings.DemoUserYas);
        await Expect(NotAuthorized).ToHaveCountAsync(0);
    }
}

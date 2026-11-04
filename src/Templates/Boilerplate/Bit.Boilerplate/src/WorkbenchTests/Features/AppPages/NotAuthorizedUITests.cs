//+:cnd:noEmit
using Boilerplate.WorkbenchTests.Features.Chrome;

namespace Boilerplate.WorkbenchTests.Features.AppPages;

/// <summary>The copy of the app's not-authorized page, as a signed-out visitor gets it on a page that needs signing in.</summary>
[TestClass, TestCategory("UITest")]
public class NotAuthorizedUITests : AppViewPageTest
{
    private ILocator Title => View().GetByRole(AriaRole.Heading, new() { Name = AppStrings.YouAreNotAuthorized, Exact = true });

    private ILocator SignIn => View().Locator(".body").GetByRole(AriaRole.Button, new() { Name = AppStrings.SignIn, Exact = true });

    [TestMethod, EverySurface]
    public async Task ASignedOutVisitor_Should_SeeItOnEveryPageThatNeedsSigningIn(WorkbenchSurface surface)
    {
        foreach (var page in new[]
        {
            //#if (module == "Admin")
            "dashboard",
            //#endif
            "settings", "pos-dashboard", "buildings", "assistant", "shifts"
        })
        {
            await OpenWorkbench(page, "?auth=signedout");

            await Expect(Title).ToBeVisibleAsync();
            await Expect(View().GetByText(AppStrings.YouAreNotAuthorizedMessage)).ToBeVisibleAsync();
            await Expect(SignIn).ToBeVisibleAsync();
        }
    }

    [TestMethod, EverySurface]
    public async Task APageThatNeedsNoSignIn_Should_OpenForASignedOutVisitor(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?auth=signedout");

        await Expect(PageTitle).ToHaveTextAsync(WorkbenchStrings.ThemePageTitle);
        await Expect(Title).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task SignIn_Should_PointAtTheUserPickerAndThePageShouldOpenOnceSignedIn(WorkbenchSurface surface)
    {
        await OpenWorkbench("settings", "?auth=signedout");

        await Press(SignIn);

        await Expect(SignInGuide).ToBeVisibleAsync();
        await Expect(SignInTools).ToBeInViewportAsync(new() { Ratio = 1 });

        await Press(SignInTools.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.SignedIn, Exact = true }));
        await Press(Button(WorkbenchStrings.GotIt));

        await Expect(SignInGuide).ToHaveCountAsync(0);

        await CloseTools();

        await Expect(View().GetByRole(AriaRole.Textbox, new() { Name = AppStrings.FullName, Exact = true })).ToHaveValueAsync(WorkbenchStrings.DemoUserYas);
        await Expect(Title).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task SignInInAPreviewAlone_Should_ExplainInAMessageBox(WorkbenchSurface surface)
    {
        await OpenPreview("settings", "?auth=signedout");

        await Press(Page.Locator(".body").GetByRole(AriaRole.Button, new() { Name = AppStrings.SignIn, Exact = true }));

        var note = Page.GetByText(WorkbenchStrings.SignInNote);

        await Expect(note).ToBeVisibleAsync();

        await Press(Page.GetByRole(AriaRole.Dialog).Filter(new() { HasText = WorkbenchStrings.SignInNote }).GetByRole(AriaRole.Button).Last);

        await Expect(note).ToHaveCountAsync(0);
    }
}

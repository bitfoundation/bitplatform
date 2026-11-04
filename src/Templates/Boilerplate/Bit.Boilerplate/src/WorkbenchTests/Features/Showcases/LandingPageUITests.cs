namespace Boilerplate.WorkbenchTests.Features.Showcases;

/// <summary>A landing page in a view: no app shell around it, a theme of its own, and its waitlist and phone demo.</summary>
[TestClass, TestCategory("UITest")]
public class LandingPageUITests : ShowcaseUITest
{
    private const string PageKey = "landing-minimal";
    private const string Email = "ada@example.com";

    private ILocator Heading => View().GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.NotchHeroTitle, Level = 1, Exact = true });

    private ILocator Submit => ViewButton(WorkbenchStrings.NotchJoinSubmit);

    private ILocator Joined => View().GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.LandingWaitlistDoneTitle, Exact = true });

    [TestMethod, EverySurface]
    public async Task TheLandingPage_Should_HaveNoAppShell(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Expect(Heading).ToBeVisibleAsync();
        await Expect(View().Locator(".main-container, .nav-bar")).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task AnAppPage_Should_StillHaveItsAppShell(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await Expect(View().Locator(".main-container")).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheLandingPage_Should_WearItsOwnThemeInDark(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?theme=dark");

        await Expect(Heading).ToBeVisibleAsync();
        await Expect(View().Locator("body")).ToHaveCSSAsync("background-color", "rgb(18, 18, 17)");
    }

    [TestMethod, EverySurface]
    public async Task JoiningTheWaitlist_Should_AskForAValidEmailAndThenConfirm(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Press(Submit);
        await Expect(View().GetByText(WorkbenchStrings.LandingWaitlistInvalidEmail, new() { Exact = true })).ToBeVisibleAsync();

        await View().GetByLabel(AppStrings.Email).FillAsync(Email);
        await Press(Submit);

        await Expect(Joined).ToBeVisibleAsync();
        await Expect(View().GetByText(Email)).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task AFailedJoin_Should_SaySoAndWorkOnTheNextTry(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?data=error");

        await View().GetByLabel(AppStrings.Email).FillAsync(Email);
        await Press(Submit);

        await Expect(View().GetByText(AppStrings.SomethingWentWrong, new() { Exact = true })).ToBeVisibleAsync();

        await Press(Submit);

        await Expect(Joined).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheDemosLine_Should_StayAfterAReload(WorkbenchSurface surface)
    {
        const string line = "Rain all day, and a good book.";

        await OpenWorkbench(PageKey);

        await View().GetByLabel(WorkbenchStrings.NotchDemoFieldLabel).FillAsync(line);
        await Press(ViewButton(WorkbenchStrings.NotchDemoSave));
        await Expect(View().GetByText(WorkbenchStrings.NotchDemoSaved, new() { Exact = true })).ToBeVisibleAsync();

        await ReloadTheView();

        await Expect(View().GetByText(line, new() { Exact = true })).ToBeVisibleAsync();
    }

    [TestMethod, OnDesktops]
    public async Task TheLanguageButton_Should_ChangeTheViewsLanguage(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?device=desktop-1536");

        await Press(View().Locator("button.lp-lang[lang='fa-IR']"));

        await Expect(Page).ToHaveURLAsync(new Regex("culture=fa-IR"));
        await WaitForViews();
        await Expect(View().Locator("html")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(View().GetByRole(AriaRole.Heading, new() { Name = InCulture("fa-IR", nameof(WorkbenchStrings.NotchHeroTitle)), Level = 1, Exact = true })).ToBeVisibleAsync();
    }
}

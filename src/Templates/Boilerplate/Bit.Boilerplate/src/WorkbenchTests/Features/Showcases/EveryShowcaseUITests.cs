namespace Boilerplate.WorkbenchTests.Features.Showcases;

/// <summary>What every showcase page does alike in a view: the sign-in it asks for, its language and its theme, and that it fits the screen in them.</summary>
[TestClass, TestCategory("UITest")]
public class EveryShowcaseUITests : ShowcaseUITest
{
    private static readonly string[] pagesThatNeedSignIn = ["pos-dashboard", "buildings", "assistant", "shifts"];

    /// <summary>Each page with the resource key of a text that shows when it has opened.</summary>
    private static readonly (string Key, string TextKey)[] pages =
    [
        ("theme", nameof(WorkbenchStrings.ThemeHeading)),
        ("pos-dashboard", nameof(WorkbenchStrings.PosCustomize)),
        ("buildings", nameof(WorkbenchStrings.BuildingsHeading)),
        ("assistant", nameof(WorkbenchStrings.AssistantWelcomeTitle)),
        ("shifts", nameof(WorkbenchStrings.ShiftsHeading)),
        ("landing-minimal", nameof(WorkbenchStrings.NotchHeroTitle))
    ];

    [TestMethod, EverySurface]
    public async Task APageThatNeedsSignIn_Should_ShowTheAppsNotAuthorizedPageWhenSignedOut(WorkbenchSurface surface)
    {
        foreach (var page in pagesThatNeedSignIn)
        {
            await OpenWorkbench(page, "?auth=signedout");

            await Expect(View().GetByText(AppStrings.YouAreNotAuthorized).First).ToBeVisibleAsync();
        }
    }

    [TestMethod, EverySurface]
    public async Task TheThemePage_Should_OpenWithoutSigningIn(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?auth=signedout");

        await Expect(View().GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.ThemeHeading, Level = 2, Exact = true })).ToBeVisibleAsync();
        await Expect(View().GetByText(AppStrings.YouAreNotAuthorized)).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    [TestCategory("Localization")]
    public async Task EveryPage_Should_TurnRightToLeftInPersianAndStillFit(WorkbenchSurface surface)
    {
        foreach (var (page, textKey) in pages)
        {
            await OpenWorkbench(page, "?culture=fa-IR");

            await Expect(View().Locator("html")).ToHaveAttributeAsync("dir", "rtl");
            await Expect(View().GetByText(InCulture("fa-IR", textKey)).Locator("visible=true").First).ToBeVisibleAsync();

            await ExpectNoSidewaysScroll();
        }
    }

    [TestMethod, EverySurface]
    public async Task EveryPage_Should_GoDarkAndStillFit(WorkbenchSurface surface)
    {
        foreach (var (page, textKey) in pages)
        {
            await OpenWorkbench(page, "?theme=dark");

            await Expect(View().Locator("html")).ToHaveAttributeAsync("bit-theme", new Regex("dark"));
            await Expect(View().GetByText(WorkbenchStrings.ResourceManager.GetString(textKey)!).Locator("visible=true").First).ToBeVisibleAsync();

            await ExpectNoSidewaysScroll();
        }
    }
}

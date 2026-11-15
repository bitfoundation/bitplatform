using Boilerplate.WorkbenchTests.Features.Showcases;

namespace Boilerplate.WorkbenchTests.Features.AppPages;

/// <summary>The Settings page's sections in the data states picked in the tools, as the app shows each: loading, empty, slow, error and mixed.</summary>
[TestClass, TestCategory("UITest")]
public class SettingsStatesUITests : SettingsPageTest
{
    private ILocator Shimmers(string title) => Section(title).Locator(".bit-smr");

    /// <summary>What the app says when its server can't be reached, as a failed load in the Error state is.</summary>
    private ILocator Unreachable => View().GetByText(AppStrings.TransientException, new() { Exact = true }).First;

    private ILocator FullName => Section(AppStrings.ProfileTitle).GetByRole(AriaRole.Textbox, new() { Name = AppStrings.FullName, Exact = true });

    [TestMethod, EverySurface]
    public async Task Loading_Should_KeepThePlaceholdersOfEverySection(WorkbenchSurface surface)
    {
        await OpenWorkbench("settings");

        await Expect(FullName).ToBeVisibleAsync();

        await PressInTheTools(WorkbenchStrings.DataLoading);

        await Expect(Page).ToHaveURLAsync(new Regex(@"\?data=loading$"));
        await WaitForTheViewToReloadWith("data=loading");
        await Expect(Shimmers(AppStrings.ProfileTitle).First).ToBeVisibleAsync();
        await Expect(FullName).ToHaveCountAsync(0);
        await Expect(Unreachable).ToHaveCountAsync(0);

        foreach (var title in new[] { AppStrings.TfaTitle, AppStrings.SessionsTitle })
        {
            await Press(SectionHeader(title));

            await Expect(Shimmers(title).First).ToBeVisibleAsync();
        }

        await Expect(Section(AppStrings.SessionsTitle).GetByText(AppStrings.CurrentSession, new() { Exact = true })).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task Empty_Should_ShowAProfileWithNothingFilledIn(WorkbenchSurface surface)
    {
        await OpenWorkbench("settings");

        await PressInTheTools(WorkbenchStrings.DataEmpty);

        await Expect(Page).ToHaveURLAsync(new Regex(@"\?data=empty$"));
        await Expect(FullName).ToHaveValueAsync(string.Empty);
        await Expect(Section(AppStrings.ProfileTitle).GetByRole(AriaRole.Combobox, new() { Name = AppStrings.BirthDate, Exact = true })).ToHaveValueAsync(string.Empty);
        await Expect(Section(AppStrings.ProfileTitle).GetByRole(AriaRole.Button, new() { Name = AppStrings.Remove, Exact = true })).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task Empty_Should_LeaveOnlyTheCurrentSession(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.SessionsTitle, "?data=empty");

        var sessions = Section(AppStrings.SessionsTitle);

        await Expect(sessions.GetByText(AppStrings.CurrentSession, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(sessions.GetByText(AppStrings.OtherSessions, new() { Exact = true })).ToHaveCountAsync(0);
        await Expect(sessions.GetByRole(AriaRole.Button, new() { Name = AppStrings.RevokeSessionTitle, Exact = true })).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task Error_Should_SayTheServerCantBeReachedAndLeaveTheProfileOnItsPlaceholder(WorkbenchSurface surface)
    {
        await OpenWorkbench("settings");

        await PressInTheTools(WorkbenchStrings.DataError);

        await Expect(Page).ToHaveURLAsync(new Regex(@"\?data=error$"));
        await Expect(Unreachable).ToBeVisibleAsync();
        await Expect(Shimmers(AppStrings.ProfileTitle).First).ToBeVisibleAsync();
        await Expect(FullName).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task Error_Should_LeaveTheTwoFactorSectionOnItsPlaceholder(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.TfaTitle, "?data=error");

        await Expect(Unreachable).ToBeVisibleAsync();
        await Expect(Shimmers(AppStrings.TfaTitle).First).ToBeVisibleAsync();
        await Expect(Section(AppStrings.TfaTitle).GetByRole(AriaRole.Button, new() { Name = AppStrings.TfaConfigureAutAppVerifyButtonText, Exact = true })).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task Error_Should_ListNoSessions(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.SessionsTitle, "?data=error");

        await Expect(Unreachable).ToBeVisibleAsync();
        await Expect(Section(AppStrings.SessionsTitle).GetByText(AppStrings.CurrentSession, new() { Exact = true })).ToHaveCountAsync(0);
        await Expect(Section(AppStrings.SessionsTitle).GetByRole(AriaRole.Listitem)).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task Slow_Should_ShowPlaceholdersFirstAndTheDataAfterAWhile(WorkbenchSurface surface)
    {
        await PlaceholderTimer.Watch(Page, "[role='region'] .bit-smr", "[role='region'] input.bit-tfl-inp");

        await OpenSection(AppStrings.ProfileTitle, "?data=slow");

        await Expect(FullName).ToHaveValueAsync(WorkbenchStrings.DemoUserYas);
        await Expect(Shimmers(AppStrings.ProfileTitle)).ToHaveCountAsync(0);

        Assert.IsGreaterThanOrEqualTo(PlaceholderTimer.SlowLoad, await PlaceholderTimer.Read(Page), "The profile came as fast as without the slow state.");
    }

    [TestMethod, EverySurface]
    public async Task Mixed_Should_GiveEachSectionAStateOfItsOwnInTheOrderTheyOpen(WorkbenchSurface surface)
    {
        await OpenWorkbench("settings", "?data=mixed");

        // The profile is asked for first, so it fails, as the app shows a failure.
        await Expect(Unreachable).ToBeVisibleAsync();
        await Expect(FullName).ToHaveCountAsync(0);

        await Press(SectionHeader(AppStrings.SessionsTitle));

        var sessions = Section(AppStrings.SessionsTitle);

        await Expect(sessions.GetByText(AppStrings.CurrentSession, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(sessions.GetByText(AppStrings.OtherSessions, new() { Exact = true })).ToHaveCountAsync(0);

        await Press(SectionHeader(AppStrings.TfaTitle));

        await Expect(Section(AppStrings.TfaTitle).GetByRole(AriaRole.Button, new() { Name = AppStrings.TfaConfigureAutAppVerifyButtonText, Exact = true })).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task BackToTheData_Should_ShowThePageAsItWas(WorkbenchSurface surface)
    {
        await OpenWorkbench("settings", "?data=error");

        await Expect(Unreachable).ToBeVisibleAsync();

        await PressInTheTools(WorkbenchStrings.DataLive);

        await Expect(Page).Not.ToHaveURLAsync(new Regex("data="));
        await Expect(FullName).ToHaveValueAsync(WorkbenchStrings.DemoUserYas);
    }
}

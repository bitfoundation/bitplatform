namespace Boilerplate.WorkbenchTests.Features.EditText;

/// <summary>Edit text mode: a click on a text traces it to its entry in the translation files, in every language.</summary>
[TestClass, TestCategory("UITest")]
public class EditTextUITests : WorkbenchPageTest
{
    private ILocator Heading => View().GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.ShiftsHeading });

    [TestMethod]
    public async Task EditText_Should_TraceATextToItsEntryAndSendAChangeToTheAgent()
    {
        await OpenWorkbench("shifts");

        await Button(WorkbenchStrings.ModeText).ClickAsync();
        await View().GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.ShiftsHeading }).ClickAsync();

        await Expect(Page.Locator(".text-panel")).ToContainTextAsync($"“{WorkbenchStrings.ShiftsHeading}”");
        await Expect(Page.Locator(".text-panel .key")).ToHaveTextAsync($"{nameof(WorkbenchStrings)} · {nameof(WorkbenchStrings.ShiftsHeading)}");

        var text = $"Weekly schedule {Guid.NewGuid():N}";

        await Page.GetByLabel(CultureInfoManager.SupportedCultures[0].DisplayName, new() { Exact = true }).FillAsync(text);
        await Page.Locator(".text-panel").GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.AskTheAgent }).ClickAsync();

        var threads = await WorkbenchTestReviewer.WaitFor(() => Server.Agent.GetThreads(CancellationToken.None), all => all.Any(t => t.Messages[0].Text.Contains(text, StringComparison.Ordinal)));
        var request = threads.Single(t => t.Messages[0].Text.Contains(text, StringComparison.Ordinal));

        Assert.AreEqual("shifts", request.PageKey);
        Assert.Contains(nameof(WorkbenchStrings.ShiftsHeading), request.Messages[0].Text);
        await Expect(Page.Locator("#wb-tab-review")).ToHaveAttributeAsync("aria-selected", "true");
    }

    [TestMethod, Surfaces(WorkbenchSurface.DesktopWithoutAgent)]
    public async Task EditText_Should_ShowEveryLanguageWithoutLettingItChange(WorkbenchSurface surface)
    {
        await OpenWorkbench("shifts");

        await Button(WorkbenchStrings.ModeText).ClickAsync();
        await Heading.ClickAsync();

        var panel = Page.Locator(".text-panel");

        await Expect(panel.GetByLabel(CultureInfoManager.SupportedCultures[0].DisplayName, new() { Exact = true })).ToHaveValueAsync(WorkbenchStrings.ShiftsHeading);
        await Expect(panel.GetByLabel(CultureInfoManager.SupportedCultures[0].DisplayName, new() { Exact = true })).Not.ToBeEditableAsync();
        await Expect(panel.GetByRole(AriaRole.Button)).ToHaveCountAsync(0);
    }
}

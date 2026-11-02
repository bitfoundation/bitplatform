using Bit.Bonboarding.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Bonboarding.Tests.E2E;

/// <summary>
/// The conditional and branching page: steps gated on the application's state and counted only when they will show,
/// a fork that Back retraces, and a step that will not move on until a field is filled in.
/// </summary>
[TestClass]
public sealed class FlowTests : BonboardingTest
{
    private static ILocator Field(IPage page, string label) =>
        page.Locator("label.bb-field").Filter(new() { HasText = label }).Locator("select");

    private static async Task<string?> ProgressTextAsync(IPage page) =>
        await page.Locator(".bit-bonboarding-popover .bit-bonboarding-progress-text").TextContentAsync();

    [TestMethod]
    public async Task Conditional_steps_show_and_count_only_for_the_state_they_ask_for()
    {
        var page = await OpenAsync("/flows");

        // A viewer with the beta off: the billing and beta steps are left out, and out of the count.
        await page.GetByRole(AriaRole.Button, new() { Name = "Run the conditional tour" }).ClickAsync();
        await WaitForStepAsync(page, "Everyone sees this");
        Assert.AreEqual("1 of 2", await ProgressTextAsync(page));
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(page.Locator(".bit-bonboarding-root")).ToHaveCountAsync(0);

        // An administrator with the beta on: both join the tour, and the beta tile it points at is rendered.
        await Field(page, "Signed in as").SelectOptionAsync("Administrator");
        await Field(page, "Feature flag").SelectOptionAsync("on");
        await Assertions.Expect(Field(page, "Feature flag")).ToHaveValueAsync("on");
        await Assertions.Expect(page.Locator("[data-bonboarding-anchor='beta-panel']")).ToHaveCountAsync(1);

        await page.GetByRole(AriaRole.Button, new() { Name = "Run the conditional tour" }).ClickAsync();
        await WaitForStepAsync(page, "Everyone sees this");
        Assert.AreEqual("1 of 4", await ProgressTextAsync(page));

        foreach (var title in new[] { "Billing", "Beta insights", "Your account" })
        {
            await page.Locator("[data-bonboarding-action='next']").ClickAsync();
            await WaitForStepAsync(page, title);
        }

        Assert.AreEqual("4 of 4", await ProgressTextAsync(page));
        AssertNoConsoleErrors("the conditional tour");
    }

    [TestMethod]
    public async Task Back_retraces_the_branch_that_was_taken()
    {
        var page = await OpenAsync("/flows");

        await page.GetByRole(AriaRole.Button, new() { Name = "Run the branching tour" }).ClickAsync();
        await WaitForStepAsync(page, "What brings you here?");

        await page.Locator(".bit-bonboarding-popover").GetByRole(AriaRole.Button, new() { Name = "I design" }).ClickAsync();
        await WaitForStepAsync(page, "For designers");
        await Assertions.Expect(page.Locator(".bb-badge-success")).ToContainTextAsync("Branch taken: design");

        await page.Locator("[data-bonboarding-action='next']").ClickAsync();
        await WaitForStepAsync(page, "Wherever you came from");

        // The step above this one in the definition is the developers' branch; Back goes where the user came from.
        await page.Locator("[data-bonboarding-action='previous']").ClickAsync();
        await WaitForStepAsync(page, "For designers");
    }

    [TestMethod]
    public async Task A_step_holds_until_its_field_is_valid_and_says_why()
    {
        var page = await OpenAsync("/flows");
        var name = page.GetByPlaceholder("Project name");

        await page.GetByRole(AriaRole.Button, new() { Name = "Run the validating tour" }).ClickAsync();
        await WaitForStepAsync(page, "Name your project");

        var validation = page.Locator(".bit-bonboarding-popover .bit-bonboarding-validation");

        await page.Locator("[data-bonboarding-action='next']").ClickAsync();
        await Assertions.Expect(validation).ToHaveTextAsync("Give the project a name first.");
        Assert.AreEqual("alert", await validation.GetAttributeAsync("role"));

        // The field is the spotlit target of a target-only step, so it can be typed into while the card is up.
        await name.ClickAsync();
        await page.Keyboard.TypeAsync("ab");
        await page.Locator("[data-bonboarding-action='next']").ClickAsync();
        await Assertions.Expect(validation).ToHaveTextAsync("Use at least three characters.");

        await name.ClickAsync();
        await page.Keyboard.TypeAsync("c");
        await page.Locator("[data-bonboarding-action='next']").ClickAsync();
        await WaitForStepAsync(page, "Thank you");
    }
}

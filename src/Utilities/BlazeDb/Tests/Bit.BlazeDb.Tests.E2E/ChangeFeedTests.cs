using Bit.BlazeDb.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.BlazeDb.Tests.E2E;

/// <summary>
/// Change notifications and a live query on WebAssembly, through the /changes page: each commit is
/// reported once, a rollback is not reported, and the live query re-runs only for the commits that
/// concern it - with the handlers running on the browser's one thread, inside the renderer.
/// </summary>
[TestClass]
public class ChangeFeedTests : BlazeDbTest
{
    [TestMethod]
    public async Task Commits_are_reported_once_and_rollbacks_never()
    {
        var page = await OpenAsync("/changes");
        var errors = CollectErrors(page);
        var feed = page.Locator(".bd-panel", new() { Has = page.Locator(".bd-panel-title", new() { HasText = "Change feed" }) });
        var rows = feed.Locator("tbody tr");
        var reruns = page.Locator(".bd-panel", new() { Has = page.Locator(".bd-panel-title", new() { HasText = "Live query: open items" }) })
            .Locator(".bd-panel-sub");

        await Assertions.Expect(reruns).ToHaveTextAsync("re-ran 0×");

        await page.GetByRole(AriaRole.Button, new() { Name = "Insert one" }).ClickAsync();
        await Assertions.Expect(rows).ToHaveCountAsync(1);
        await Assertions.Expect(rows.First).ToContainTextAsync("inserted");
        await Assertions.Expect(reruns).ToHaveTextAsync("re-ran 1×");

        await page.GetByRole(AriaRole.Button, new() { Name = "Write, then roll back" }).ClickAsync();
        await Assertions.Expect(page.Locator(".bd-foot")).ToContainTextAsync("Nothing was reported.");
        await Assertions.Expect(rows).ToHaveCountAsync(1);

        await page.GetByRole(AriaRole.Button, new() { Name = "Insert and delete in one transaction" }).ClickAsync();
        await Assertions.Expect(page.Locator(".bd-foot")).ToContainTextAsync("Nothing was reported.");
        await Assertions.Expect(rows).ToHaveCountAsync(1);

        await page.GetByRole(AriaRole.Button, new() { Name = "Three writes, one transaction" }).ClickAsync();
        await Assertions.Expect(rows).ToHaveCountAsync(2);
        // Newest first: two inserts, the inserted-then-updated row reported once, as an insert.
        await Assertions.Expect(rows.First).ToContainTextAsync("inserted");
        await Assertions.Expect(rows.First).Not.ToContainTextAsync("updated");

        await page.GetByRole(AriaRole.Button, new() { Name = "Complete the first open item" }).ClickAsync();
        await Assertions.Expect(rows).ToHaveCountAsync(3);
        await Assertions.Expect(rows.First).ToContainTextAsync("updated");
        await Assertions.Expect(reruns).ToHaveTextAsync("re-ran 3×");

        // Renaming a done row is reported, but cannot change a list of open ones.
        await page.GetByRole(AriaRole.Button, new() { Name = "Rename a done item" }).ClickAsync();
        await Assertions.Expect(rows).ToHaveCountAsync(4);
        await Assertions.Expect(reruns).ToHaveTextAsync("re-ran 3×");

        Assert.IsEmpty(errors, string.Join(Environment.NewLine, errors));
    }
}

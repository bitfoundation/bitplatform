using Bit.Bdnd.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bdnd.Tests.E2E;

/// <summary>
/// Every page of the demo: each one boots on WebAssembly, loads the browser module and registers every draggable
/// with it, shows its own source, and survives every button on it without an error; and moving between them in the
/// app leaves a page that still drags.
/// </summary>
[TestClass]
public sealed class DemoPagesTests : BdndTest
{
    /// <summary>Every routable page of the demo, read from the <c>@page</c> directives of its sources.</summary>
    public static IEnumerable<object[]> DemoRoutes => RepoLayout.DemoRoutes().Select(route => new object[] { route });

    [TestMethod]
    [DynamicData(nameof(DemoRoutes))]
    public async Task Every_demo_page_loads_interactively_and_runs_every_button_without_an_error(string path)
    {
        var page = await NewPageAsync();

        var response = await GotoAsync(page, path);
        Assert.AreEqual(200, response?.Status, $"{path} answered {response?.Status}");
        await WaitForInteractiveAsync(page.MainFrame);

        // A page with draggables has loaded the library's browser module, and registered every one of them with it:
        // an unregistered draggable is one nothing can drag.
        int draggables = await page.Locator(DraggableHosts).CountAsync();
        if (draggables > 0)
        {
            await WaitForBdndAsync(page.MainFrame);
            Assert.IsTrue(await page.EvaluateAsync<bool>(
                // The import map may point the module at a fingerprinted file, bit-bdnd.<hash>.js.
                @"() => performance.getEntriesByType('resource').some(e => /^\/_content\/Bit\.Bdnd\/bit-bdnd(\.\w+)?\.js$/.test(new URL(e.name).pathname))"),
                $"{path} has {draggables} draggable(s) but never loaded bit-bdnd.js");
        }

        // "View code" shows the page's own source, stripped of its routing directive.
        var toggle = page.Locator(".demo-code__toggle");
        if (await toggle.CountAsync() > 0)
        {
            await toggle.ClickAsync();
            var code = page.Locator(".demo-code__code");
            await Expect(code).ToBeVisibleAsync();
            var source = await code.TextContentAsync() ?? "";
            Assert.IsTrue(source.Length > 100, $"{path} shows a source of only {source.Length} characters");
            Assert.DoesNotContain("@page", source, $"{path}: the snippet still carries the routing directive");
        }

        // Every other enabled button of the page must run without throwing: a dead or broken control is a demo bug.
        var buttons = page.Locator(".content button:enabled:visible:not(.demo-code__toggle)");
        int count = await buttons.CountAsync();
        for (int i = 0; i < count; i++)
        {
            await buttons.Nth(i).ClickAsync(new LocatorClickOptions { Timeout = 3000 });
            await WaitForQuietAsync(page, 100);
        }
        await WaitForQuietAsync(page);

        // Draggables a button added (the stress test's "Add 10 boxes") are registered like the first ones.
        if (draggables > 0) await WaitForBdndAsync(page.MainFrame);

        Assert.IsFalse(await page.Locator("#blazor-error-ui").IsVisibleAsync(), $"{path} shows the unhandled-error banner");
        TestContext.WriteLine($"{path}: {draggables} draggable(s), {count} button(s) exercised");
    }

    [TestMethod]
    public async Task View_code_shows_the_source_of_the_page_and_hides_it_again()
    {
        var page = await OpenAsync("/dustbin/single");

        var toggle = page.GetByRole(AriaRole.Button, new() { Name = "View code" });
        await toggle.ClickAsync();

        var code = page.Locator(".demo-code__code");
        await Expect(code).ToBeVisibleAsync();
        var source = await code.TextContentAsync() ?? "";
        StringAssert.Contains(source, "<BdndDropTarget", "the snippet is not the page's markup");
        StringAssert.Contains(source, "private void HandleDrop(", "the snippet is missing the page's code block");
        Assert.DoesNotContain("@page", source, "the snippet still carries the routing directive");

        await page.GetByRole(AriaRole.Button, new() { Name = "Hide code" }).ClickAsync();
        await Expect(code).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Navigating_through_the_sidebar_leaves_a_page_that_still_drags()
    {
        var page = await OpenAsync("/");

        // Client-side navigation through every page: each one's draggables and drop targets are torn down (their
        // browser registrations disposed) and the next page's set up, all in the same WebAssembly instance.
        var links = page.Locator(".sidebar a.nav-link");
        int count = await links.CountAsync();
        Assert.IsGreaterThan(15, count, $"the sidebar offers only {count} pages");
        for (int i = 1; i < count; i++)
        {
            var href = await links.Nth(i).EvaluateAsync<string>("a => a.href");
            await links.Nth(i).ClickAsync();
            await page.WaitForURLAsync(href);
            await WaitForBdndAsync(page.MainFrame);
            await WaitForQuietAsync(page);
        }

        await links.Nth(0).ClickAsync();
        await page.WaitForURLAsync(BaseUrl + "/");

        // Back on the overview, a box still drops into the dustbin.
        var bin = page.Locator(".dustbin");
        await DragAsync(page, page.Locator(".box", new() { HasText = "Banana" }), bin,
            () => Expect(bin).ToHaveTextAsync("Release to drop"));
        await Expect(bin).ToHaveTextAsync("Dropped: Banana");
        Assert.IsFalse(await page.Locator("#blazor-error-ui").IsVisibleAsync(), "the unhandled-error banner is showing");
    }
}

using Bit.Bdnd.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bdnd.Tests.E2E;

/// <summary>
/// Every page of the demo: each one boots interactively (on WebAssembly, or over a circuit for the Blazor Server check
/// page), loads the browser module and registers every draggable with it, shows its own source, and survives every
/// button on it without an error; and moving between them in the app leaves a page that still drags.
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

        // "View code" comes after every sample of the page, outside its frame, and shows the page's own source first,
        // stripped of its routing directive, then any component of the page's it renders: each file is there, and
        // coloured.
        var toggle = page.Locator(".demo-code__toggle");
        if (await toggle.CountAsync() > 0)
        {
            Assert.IsTrue(await page.EvaluateAsync<bool>(
                """
                () => {
                    const code = document.querySelector('.demo-code');
                    return [...document.querySelectorAll('.bb-demo')].every(demo => {
                        const position = demo.compareDocumentPosition(code);
                        return (position & Node.DOCUMENT_POSITION_FOLLOWING) && !(position & Node.DOCUMENT_POSITION_CONTAINED_BY);
                    });
                }
                """),
                $"{path}: the code toggle is not under the sample");

            await toggle.ClickAsync();
            var names = page.Locator(".demo-code__file");
            var blocks = page.Locator(".demo-code__pre > code");
            await Expect(blocks.First).ToBeVisibleAsync();
            int files = await blocks.CountAsync();
            await Expect(names).ToHaveCountAsync(files);
            for (int i = 0; i < files; i++)
            {
                var name = await names.Nth(i).TextContentAsync();
                var source = await blocks.Nth(i).TextContentAsync() ?? "";
                Assert.IsTrue(source.Length > 100, $"{path}: {name} shows a source of only {source.Length} characters");
                Assert.IsGreaterThan(0, await blocks.Nth(i).Locator("[class^='tok-']").CountAsync(), $"{path}: {name} is not coloured");
                if (i == 0) Assert.DoesNotContain("@page", source, $"{path}: the snippet still carries the routing directive");
            }
        }

        // Every other enabled button of the page must run without throwing: a dead or broken control is a demo bug.
        // The page is the docs column (or the landing page), not the site chrome around it; the copy buttons the
        // site script hangs on its code blocks are that chrome too.
        var buttons = page.Locator(":is(.bb-docs-content, .bb-landing) button:enabled:visible:not(.demo-code__toggle, .bb-copy-btn)");
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

        var code = page.Locator(".demo-code__pre > code");
        await Expect(code).ToHaveCountAsync(1);
        await Expect(page.Locator(".demo-code__file")).ToHaveTextAsync("SingleTarget.razor");
        var source = await code.TextContentAsync() ?? "";
        StringAssert.Contains(source, "<BdndDropTarget", "the snippet is not the page's markup");
        StringAssert.Contains(source, "private void HandleDrop(", "the snippet is missing the page's code block");
        Assert.DoesNotContain("@page", source, "the snippet still carries the routing directive");
        // coloured: the component tag, the keywords and the strings are tokens of their own
        Assert.IsGreaterThan(0, await code.Locator(".tok-type", new() { HasText = "BdndDropTarget" }).CountAsync(), "the component tag is not coloured");
        Assert.IsGreaterThan(0, await code.Locator(".tok-key", new() { HasText = "private" }).CountAsync(), "the keywords are not coloured");
        Assert.IsGreaterThan(0, await code.Locator(".tok-str").CountAsync(), "the strings are not coloured");

        await page.GetByRole(AriaRole.Button, new() { Name = "Hide code" }).ClickAsync();
        await Expect(code).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task View_code_also_shows_the_components_the_page_renders()
    {
        var page = await OpenAsync("/sortable/simple");

        await page.GetByRole(AriaRole.Button, new() { Name = "View code" }).ClickAsync();

        await Expect(page.Locator(".demo-code__file")).ToHaveTextAsync(["Simple.razor", "SortableCard.razor"]);
        var card = page.Locator(".demo-code__pre > code").Nth(1);
        StringAssert.Contains(await card.TextContentAsync() ?? "", "OnHover", "the second file is not the sortable card");
        // the card's parameter is a member, not a type: `@Card.Text`
        Assert.AreEqual(0, await card.Locator(".tok-type", new() { HasTextRegex = new System.Text.RegularExpressions.Regex("^Card$") }).CountAsync(), "the card's Card parameter is coloured as a type");
    }

    [TestMethod]
    public async Task Navigating_through_the_sidebar_leaves_a_page_that_still_drags()
    {
        var page = await OpenAsync("/getting-started");

        // Client-side navigation through every page: each one's draggables and drop targets are torn down (their
        // browser registrations disposed) and the next page's set up, all in the same WebAssembly instance. The
        // Blazor Server check page is loaded afresh rather than navigated to (ServerHostingTests covers that).
        var links = page.Locator(".bb-docs-sidebar .bb-docs-section a:not([href='/hosting/server-check'])");
        int count = await links.CountAsync();
        Assert.IsGreaterThan(15, count, $"the sidebar offers only {count} pages");
        for (int i = 1; i < count; i++)
        {
            var href = await links.Nth(i).EvaluateAsync<string>("a => a.href");
            await links.Nth(i).ClickAsync();
            // a navigation in the page: the address is polled (WebKit's driver reports no navigation event for it),
            // and the page has rendered once the sidebar marks its link
            await Expect(page).ToHaveURLAsync(href);
            await Expect(links.Nth(i)).ToHaveClassAsync(new System.Text.RegularExpressions.Regex(@"\bactive\b"));
            await WaitForBdndAsync(page.MainFrame);
            await WaitForQuietAsync(page);
        }

        // Back on the first dustbin, torn down and set up again on the way, a box still drops into it. (The first
        // page, Getting started, has its sample below the fold, where the mouse cannot reach it.)
        await links.Filter(new LocatorFilterOptions { HasText = "Single target" }).ClickAsync();
        await Expect(page).ToHaveURLAsync(BaseUrl + "/dustbin/single");
        await Expect(links.Filter(new LocatorFilterOptions { HasText = "Single target" })).ToHaveClassAsync(new System.Text.RegularExpressions.Regex(@"\bactive\b"));
        await WaitForBdndAsync(page.MainFrame);

        var bin = page.Locator(".dustbin");
        await DragAsync(page, page.Locator(".box", new() { HasText = "Banana" }), bin,
            () => Expect(bin).ToHaveTextAsync("Release to drop"));
        await Expect(bin).ToHaveTextAsync("Last dropped: Banana (total 1)");
        Assert.IsFalse(await page.Locator("#blazor-error-ui").IsVisibleAsync(), "the unhandled-error banner is showing");
    }
}

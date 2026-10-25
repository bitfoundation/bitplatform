using Bit.Blow.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Blow.Tests.E2E;

/// <summary>
/// Every page of the demo, the main demos and the examples gallery alike: each one boots on WebAssembly, loads
/// the browser layer and survives every button on it without an error, and the gallery reaches all of them.
/// </summary>
[TestClass]
public sealed class DemoPagesTests : BlowTest
{
    /// <summary>Every routable page of the demo, read from the <c>@page</c> directives of its sources.</summary>
    public static IEnumerable<object[]> DemoRoutes => RepoLayout.DemoRoutes().Select(route => new object[] { route });

    [TestMethod]
    [DynamicData(nameof(DemoRoutes))]
    public async Task Every_demo_page_loads_interactively_and_runs_every_button_without_an_error(string path)
    {
        var page = await (await NewContextAsync()).NewPageAsync();
        var problems = TrackProblems(page, BaseUrl);

        var response = await GotoAsync(page, path);
        Assert.AreEqual(200, response?.Status, $"{path} answered {response?.Status}");
        await WaitForFlowAsync(page);

        // A page with a flow on it has loaded the library's browser layer, without which nothing is measured.
        int flows = await page.Locator(".bit-blow").CountAsync();
        if (flows > 0)
        {
            Assert.IsTrue(await page.EvaluateAsync<bool>(
                // The import map points the module at its fingerprinted file, bit-blow.<hash>.js.
                @"() => performance.getEntriesByType('resource').some(e => /^\/_content\/Bit\.Blow\/bit-blow(\.\w+)?\.js$/.test(new URL(e.name).pathname))"),
                $"{path} has {flows} flow(s) but never loaded bit-blow.js");
        }

        // "View code" comes after the page's samples, outside their frames - a page with a toggle has a sample frame -
        // and shows the page's own source, stripped of its routing directive and coloured.
        var toggle = page.Locator(".demo-code__toggle");
        if (await toggle.CountAsync() > 0)
        {
            Assert.IsTrue(await page.EvaluateAsync<bool>(
                """
                () => {
                    const demos = [...document.querySelectorAll('.bb-demo')];
                    return demos.length > 0 && [...document.querySelectorAll('.demo-code')].every(code => demos.every(demo => {
                        const position = demo.compareDocumentPosition(code);
                        return (position & Node.DOCUMENT_POSITION_FOLLOWING) && !(position & Node.DOCUMENT_POSITION_CONTAINED_BY);
                    }));
                }
                """),
                $"{path}: the code toggle is not under the sample");

            await toggle.ClickAsync();
            var code = page.Locator(".demo-code__pre > code");
            await Expect(code).ToBeVisibleAsync();
            var source = await code.TextContentAsync() ?? "";
            Assert.IsTrue(source.Length > 100, $"{path} shows a source of only {source.Length} characters");
            Assert.IsGreaterThan(0, await code.Locator("[class^='tok-']").CountAsync(), $"{path}: the source is not coloured");
            Assert.DoesNotContain("@page", source, $"{path}: the snippet still carries the routing directive");
            await toggle.ClickAsync();
        }

        // Every enabled button of the page must run without throwing: a dead or broken control is a demo bug. The
        // page is the docs column (or the landing page), not the site chrome around it; the copy buttons the site
        // script hangs on its code blocks are that chrome too.
        var buttons = page.Locator(":is(.bb-docs-content, .bb-landing) button:enabled:visible:not(.bb-copy-btn)");
        int count = Math.Min(await buttons.CountAsync(), 40);
        for (int i = 0; i < count; i++)
        {
            try
            {
                await buttons.Nth(i).ClickAsync(new LocatorClickOptions { Timeout = 3000, Force = true });
                await page.WaitForTimeoutAsync(60);
            }
            catch (TimeoutException)
            {
                // The click was superseded by a re-render (the button went away); that is not a page error.
            }
        }
        await WaitForQuietAsync(page);

        Assert.IsFalse(await page.Locator("#blazor-error-ui").IsVisibleAsync(), $"{path} shows the unhandled-error banner");

        string[] found;
        lock (problems) found = [.. problems];
        TestContext.WriteLine($"{path}: {flows} flow(s), {count} button(s) exercised, {found.Length} problem(s)");
        Assert.IsEmpty(found, $"{path} reported:{Environment.NewLine}{string.Join(Environment.NewLine, found)}");
    }

    [TestMethod]
    public async Task The_examples_gallery_links_to_every_example_page()
    {
        var page = await OpenAsync("/examples");

        var expected = RepoLayout.DemoRoutes().Where(r => r.StartsWith("/examples/", StringComparison.Ordinal)).ToArray();
        Assert.IsTrue(expected.Length > 50, $"only {expected.Length} example pages were found in the sources");

        // The cards on the index and the example links in the docs navigation both have to reach every page.
        foreach (var selector in new[] { "a.example-card", ".bb-docs-sidebar .bb-docs-section a" })
        {
            var links = await page.EvaluateAsync<string[]>(
                "(s) => [...document.querySelectorAll(s)].map(a => new URL(a.href).pathname).filter(p => p.startsWith('/examples/'))", selector);
            CollectionAssert.AreEquivalent(expected, links, $"the {selector} links do not match the example pages");
        }
    }

    [TestMethod]
    public async Task Navigating_between_the_demos_in_the_app_leaves_a_working_canvas_behind()
    {
        var page = await OpenAsync("/basic-flow");
        var problems = TrackProblems(page, BaseUrl);

        // Client-side navigation through every main demo - the introduction and the feature tours of the docs
        // navigation: each canvas is torn down (its resize observer and input guards disposed) and the next one
        // set up, all in the same WebAssembly instance.
        var links = page.Locator(".bb-docs-sidebar :is(ul[aria-label='Introduction'], ul[aria-label='Feature Tours']) a");
        int count = await links.CountAsync();
        Assert.IsTrue(count > 5, $"the sidebar offers only {count} demos");
        for (int i = 0; i < count; i++)
        {
            var href = await links.Nth(i).GetAttributeAsync("href");
            if (href == "/basic-flow") continue;
            await links.Nth(i).ClickAsync();
            await page.WaitForURLAsync(BaseUrl + href);
            await WaitForQuietAsync(page);
        }
        await links.Filter(new LocatorFilterOptions { HasText = "Basic Flow" }).ClickAsync();
        await page.WaitForURLAsync(BaseUrl + "/basic-flow");
        await WaitForFlowAsync(page);
        await ScrollFlowIntoViewAsync(page);
        await WaitForEdgesAnchoredAsync(page);

        // Back on the first demo, the canvas still measures and drags.
        var before = await NodeBoxAsync(page, "2");
        await DragAsync(page, before.Centre, before.Centre.Offset(50, 30));
        var after = await NodeBoxAsync(page, "2");
        AssertNear(new Point(before.X + 50, before.Y + 30), new Point(after.X, after.Y), 0.5, "node after navigating back");
        AssertNear(await HandleCentreAsync(page, "2", "target"), (await EdgeEndsAsync(page, "e1-2")).End, 1, "end of e1-2");

        string[] found;
        lock (problems) found = [.. problems];
        Assert.IsEmpty(found, string.Join(Environment.NewLine, found));
    }

    [TestMethod]
    public async Task View_code_shows_the_source_of_the_page()
    {
        var page = await OpenAsync("/examples/basic");

        await page.GetByRole(AriaRole.Button, new() { Name = "View code" }).ClickAsync();

        var code = page.Locator(".demo-code__pre > code");
        await Expect(code).ToBeVisibleAsync();
        var source = await code.TextContentAsync() ?? "";
        StringAssert.Contains(source, "<BlowCanvas", "the snippet is not the page's markup");
        Assert.DoesNotContain("@page", source, "the snippet still carries the routing directive");
        // coloured: the component tag, the keywords and the strings are tokens of their own
        Assert.IsGreaterThan(0, await code.Locator(".tok-type", new() { HasText = "BlowCanvas" }).CountAsync(), "the component tag is not coloured");
        Assert.IsGreaterThan(0, await code.Locator(".tok-key", new() { HasText = "private" }).CountAsync(), "the keywords are not coloured");
        Assert.IsGreaterThan(0, await code.Locator(".tok-str").CountAsync(), "the strings are not coloured");

        // Closing hides the source rather than dropping it, so that opening the panel again does not lex the page again.
        await page.GetByRole(AriaRole.Button, new() { Name = "Hide code" }).ClickAsync();
        await Expect(code).ToBeHiddenAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "View code" }).ClickAsync();
        await Expect(code).ToBeVisibleAsync();
    }
}

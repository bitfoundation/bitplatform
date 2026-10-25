using System.Text.RegularExpressions;
using Bit.Bortable.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bortable.Tests.E2E;

/// <summary>
/// Every page of the demo: each one boots on WebAssembly, loads the browser layer and the stylesheet when it has a
/// list, shows the code of each of its samples on request, and does all that without an error; an unknown address
/// answers with the not-found page; and navigating through the demos in the app leaves working lists behind.
/// </summary>
[TestClass]
public sealed class DemoPagesTests : BortableTest
{
    /// <summary>Every routable page of the demo, read from the <c>@page</c> directives of its sources.</summary>
    public static IEnumerable<object[]> DemoRoutes => RepoLayout.DemoRoutes().Select(route => new object[] { route });

    [TestMethod]
    [DynamicData(nameof(DemoRoutes))]
    public async Task Every_demo_page_loads_interactively_and_shows_its_code_without_an_error(string path)
    {
        var page = await (await NewContextAsync()).NewPageAsync();
        var problems = TrackProblems(page, BaseUrl);

        var response = await GotoAsync(page, path);
        Assert.AreEqual(200, response?.Status, $"{path} answered {response?.Status}");
        await WaitForListsAsync(page);
        await Expect(page.Locator("h1").First).ToBeVisibleAsync();

        // A page with lists on it has loaded the library's browser layer (WaitForListsAsync waits for it) and its
        // stylesheet, without which no drag state is drawn; and every list shows its items.
        int lists = await page.Locator(ListSelector).CountAsync();
        if (lists > 0)
        {
            Assert.IsTrue(await page.EvaluateAsync<bool>(
                "() => [...document.querySelectorAll('[data-bortable-list]')].every(l => getComputedStyle(l).position === 'relative')"),
                $"{path} has {lists} list(s) but bit-bortable.css is not applied to them");
            Assert.IsTrue(await page.Locator($"{ListSelector} > [data-bortable-item]").CountAsync() > 0, $"{path} shows no items");
        }

        // Every sample of the page is followed by a "View code" panel of its own, outside the sample's frame, which
        // opens on the code of that sample - its component and the files it names, each under its file name and
        // coloured, with no page chrome around it - and closes again.
        var panels = page.Locator(".demo-code");
        int samples = await page.Locator(".bb-demo").CountAsync();
        int codePanels = await panels.CountAsync();
        if (codePanels > 0)
        {
            Assert.AreEqual(samples, codePanels, $"{path} has {samples} sample(s) but {codePanels} code panel(s)");
            Assert.IsTrue(await page.EvaluateAsync<bool>(
                "() => [...document.querySelectorAll('.bb-demo')].every(demo => demo.nextElementSibling?.classList.contains('demo-code'))"),
                $"a sample of {path} is not followed by its code panel");

            var sources = new List<string>();
            for (int i = 0; i < codePanels; i++)
            {
                var panel = panels.Nth(i);
                var toggle = panel.Locator(".demo-code__toggle");
                await Expect(toggle).ToHaveTextAsync(new Regex("View code"));
                await toggle.ClickAsync();
                await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "true");

                var code = panel.Locator(".demo-code__pre > code");
                await Expect(code.First).ToBeVisibleAsync();
                int files = await code.CountAsync();
                await Expect(panel.Locator(".demo-code__file")).ToHaveCountAsync(files);
                for (int j = 0; j < files; j++)
                {
                    var name = await panel.Locator(".demo-code__file").Nth(j).TextContentAsync();
                    var source = await code.Nth(j).TextContentAsync() ?? "";
                    Assert.IsTrue(source.Length > 100, $"{path}, sample {i + 1}: {name} shows a source of only {source.Length} characters");
                    Assert.IsGreaterThan(0, await code.Nth(j).Locator("[class^='tok-']").CountAsync(), $"{path}, sample {i + 1}: {name} is not coloured");
                    Assert.DoesNotContain("bb-card", source, $"{path}, sample {i + 1}: {name} carries the page around the sample");
                    sources.Add(source);
                }

                await toggle.ClickAsync();
                await Expect(code).ToHaveCountAsync(0);
                await Expect(toggle).ToHaveTextAsync(new Regex("View code"));
            }
            // The page's lists are declared in its samples' code.
            if (lists > 0) Assert.IsTrue(sources.Any(s => s.Contains("<Bortable", StringComparison.Ordinal)), $"the code of {path} declares none of its lists");
        }
        await WaitForQuietAsync(page);

        Assert.IsFalse(await page.Locator("#blazor-error-ui").IsVisibleAsync(), $"{path} shows the unhandled-error banner");

        var found = Snapshot(problems);
        TestContext.WriteLine($"{path}: {lists} list(s), {codePanels} code panel(s), {found.Length} problem(s)");
        Assert.IsEmpty(found, $"{path} reported:{Environment.NewLine}{string.Join(Environment.NewLine, found)}");
    }

    [TestMethod]
    public async Task Each_sample_shows_its_own_code_only()
    {
        var page = await OpenAsync("/cloning");
        var panels = page.Locator(".demo-code");
        await Expect(panels).ToHaveCountAsync(2);

        await panels.Nth(0).Locator(".demo-code__toggle").ClickAsync();
        await panels.Nth(1).Locator(".demo-code__toggle").ClickAsync();

        await Expect(panels.Nth(0).Locator(".demo-code__file")).ToHaveTextAsync(["CloneSource.razor"]);
        await Expect(panels.Nth(1).Locator(".demo-code__file")).ToHaveTextAsync(["FormBuilder.razor"]);
        var first = await panels.Nth(0).Locator(".demo-code__pre > code").TextContentAsync() ?? "";
        var second = await panels.Nth(1).Locator(".demo-code__pre > code").TextContentAsync() ?? "";
        StringAssert.Contains(first, "Id=\"clone-source\"", "the first panel is not the clone source's code");
        Assert.DoesNotContain("clone-palette", first, "the first panel shows the second sample's code too");
        StringAssert.Contains(second, "Id=\"clone-palette\"", "the second panel is not the palette's code");
        Assert.DoesNotContain("clone-source", second, "the second panel shows the first sample's code too");
    }

    [TestMethod]
    public async Task A_sample_built_from_several_files_shows_each_of_them()
    {
        var page = await OpenAsync("/nested");

        await page.Locator(".demo-code__toggle").ClickAsync();

        await Expect(page.Locator(".demo-code__file")).ToHaveTextAsync(["NestedTree.razor", "NestedList.razor", "NestedItem.cs"]);
        var code = page.Locator(".demo-code__pre > code");
        await Expect(code).ToHaveCountAsync(3);
        StringAssert.Contains(await code.Nth(0).TextContentAsync(), "<NestedList");
        StringAssert.Contains(await code.Nth(1).TextContentAsync(), "<Bortable");
        StringAssert.Contains(await code.Nth(2).TextContentAsync(), "class NestedItem");

        // Each file is coloured in its own language: the component's markup as Razor, the model as C#.
        Assert.IsGreaterThan(0, await code.Nth(1).Locator(".tok-type", new() { HasText = "Bortable" }).CountAsync(), "the component tag is not coloured");
        Assert.IsGreaterThan(0, await code.Nth(2).Locator(".tok-key", new() { HasText = "class" }).CountAsync(), "the C# model's keywords are not coloured");
    }

    [TestMethod]
    public async Task An_unknown_address_answers_404_with_the_not_found_page()
    {
        var page = await (await NewContextAsync()).NewPageAsync();

        var response = await GotoAsync(page, "/no-such-page");

        Assert.AreEqual(404, response?.Status);
        await WaitForListsAsync(page);
        await Expect(page.Locator("h1")).ToHaveTextAsync("404");
        await Expect(page.Locator(".bb-header")).ToBeVisibleAsync();
        await Expect(page.Locator(ListSelector)).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Navigating_between_the_demos_in_the_app_leaves_working_lists_behind()
    {
        var page = await OpenAsync("/simple-list");
        var problems = TrackProblems(page, BaseUrl);

        // Client-side navigation through every demo: each page's lists are torn down (their drag handles and module
        // references released) and the next page's set up, all in the same WebAssembly instance.
        var links = page.Locator(".bb-docs-sidebar .bb-docs-section a");
        int count = await links.CountAsync();
        Assert.IsTrue(count > 10, $"the sidebar offers only {count} demos");
        for (int i = 1; i < count; i++)
        {
            var href = await links.Nth(i).GetAttributeAsync("href");
            await links.Nth(i).ClickAsync();
            await page.WaitForURLAsync(BaseUrl + "/" + href!.TrimStart('/'));
            await WaitForQuietAsync(page);
        }

        // The handle page again, whose drag handle is installed by the browser layer when the list renders.
        await links.Filter(new LocatorFilterOptions { HasText = "Handle" }).ClickAsync();
        await page.WaitForURLAsync(BaseUrl + "/handle");
        await Expect(page.Locator("#handle-list")).ToBeVisibleAsync();
        await WaitForQuietAsync(page);
        await DragAsync(page,
            (await BoxAsync(Item(page, "handle-list", "Item 6").Locator(".handle"))).Centre,
            (await BoxAsync(Item(page, "handle-list", "Item 1"))).Centre);
        await AssertOrderAsync(page, "handle-list", "handle-order", "Item 6", "Item 1", "Item 2", "Item 3", "Item 4", "Item 5");

        // And back on the first demo, the list still sorts.
        await links.Filter(new LocatorFilterOptions { HasText = "Simple list" }).ClickAsync();
        await page.WaitForURLAsync(BaseUrl + "/simple-list");
        await Expect(page.Locator("#simple-list")).ToBeVisibleAsync();
        await WaitForQuietAsync(page);
        await DragAsync(page,
            (await BoxAsync(Item(page, "simple-list", "Item 1"))).Centre,
            (await BoxAsync(Item(page, "simple-list", "Item 3"))).Centre);
        await AssertOrderAsync(page, "simple-list", "simple-order", "Item 2", "Item 3", "Item 1", "Item 4", "Item 5", "Item 6");

        var found = Snapshot(problems);
        Assert.IsEmpty(found, string.Join(Environment.NewLine, found));
    }
}

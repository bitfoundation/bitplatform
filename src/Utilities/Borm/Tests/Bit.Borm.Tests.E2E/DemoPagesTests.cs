using System.Text.RegularExpressions;
using Bit.Borm.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Borm.Tests.E2E;

/// <summary>
/// Every page of the demo: each one is prerendered with its forms in the first response, turns interactive on
/// WebAssembly with the library's stylesheet applied and the site's theme on its tokens, and survives every button on
/// it without an error. An unknown address gets the not-found page, and moving through the demos inside the app leaves
/// working forms behind.
/// </summary>
[TestClass]
public sealed class DemoPagesTests : BormTest
{
    /// <summary>Every routable page of the demo, read from the <c>@page</c> directives of its sources.</summary>
    public static IEnumerable<object[]> DemoRoutes => RepoLayout.DemoRoutes().Select(route => new object[] { route });

    [TestMethod]
    [DynamicData(nameof(DemoRoutes))]
    public async Task Every_demo_page_prerenders_turns_interactive_styled_and_runs_every_button_without_an_error(string path)
    {
        var page = await NewPageAsync(await NewContextAsync());
        var problems = TrackProblems(page, BaseUrl);

        var response = await GotoAsync(page, path);
        Assert.IsNotNull(response, $"{path} did not answer");
        Assert.AreEqual(200, response.Status, $"{path} answered {response.Status}");

        // Prerendered: the first response already carries the layout, marked as not interactive yet, and every
        // form the page goes on to show - before WebAssembly has run a line.
        var html = await response.TextAsync();
        StringAssert.Contains(html, "data-interactive=\"false\"", $"{path} was not prerendered");
        await WaitForInteractiveAsync(page);

        int forms = await page.Locator("form.bit-borm-form").CountAsync();
        Assert.AreEqual(forms, Regex.Matches(html, "<form class=\"bit-borm-form").Count,
            $"{path} shows {forms} form(s) once interactive, but the prerendered HTML did not carry the same number");

        // The stylesheet is not injected by the library: the host page links it, and it has to be there and parsed.
        // The link carries the fingerprinted name (bit-borm.<hash>.css) the static asset manifest hands out.
        Assert.IsTrue(await page.EvaluateAsync<bool>(
            @"() => [...document.styleSheets].some(s => s.href && /^\/_content\/Bit\.Borm\/bit-borm(\.\w+)?\.css$/.test(new URL(s.href).pathname) && s.cssRules.length > 0)"),
            $"{path} did not load _content/Bit.Borm/bit-borm.css");

        // ...and it styles the forms: the field gap and the width cap exist only in that stylesheet, and the demo's
        // re-tint puts the site's brand on the accent (wwwroot/demo.css) - in the light theme this context asks for.
        for (int i = 0; i < forms; i++)
        {
            var form = page.Locator("form.bit-borm-form").Nth(i);
            Assert.AreEqual("1rem", await ComputedAsync(form, "--bit-borm-gap"), $"form {i} of {path} has no theme tokens");
            Assert.AreEqual("720px", await ComputedAsync(form, "max-width"), $"form {i} of {path} is not capped by the stylesheet");
            Assert.AreEqual("#1276C6", await ComputedAsync(form, "--bit-borm-accent"), $"form {i} of {path} is not on the site's theme");
        }

        // Every enabled button of the page must run without throwing: a dead or broken control is a demo bug. The page
        // is the docs column (or the landing page), not the site chrome around it; the copy buttons the site script
        // hangs on its code blocks are that chrome too.
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
                // The click was superseded by a re-render (the button went away or was disabled); that is not a page error.
            }
        }

        // Let what the last click started - an options lookup, an async rule - finish and report.
        await page.WaitForTimeoutAsync(800);
        await WaitForQuietAsync(page);

        Assert.IsFalse(await page.Locator("#blazor-error-ui").IsVisibleAsync(), $"{path} shows the unhandled-error banner");

        string[] found;
        lock (problems) found = [.. problems];
        TestContext.WriteLine($"{path}: {forms} form(s), {count} button(s) exercised, {found.Length} problem(s)");
        Assert.IsEmpty(found, $"{path} reported:{Environment.NewLine}{string.Join(Environment.NewLine, found)}");
    }

    [TestMethod]
    public async Task An_unknown_address_gets_the_not_found_page_inside_the_app()
    {
        var page = await NewPageAsync(await NewContextAsync());

        var response = await GotoAsync(page, "/no-such-form");

        Assert.AreEqual(404, response?.Status, "an unknown address did not answer 404");
        await WaitForInteractiveAsync(page);
        await Expect(page.Locator("h1")).ToHaveTextAsync("404");
        await Expect(page.Locator("main")).ToContainTextAsync("There is no page at this address");

        // The site shell is there, and with it the way back to every demo.
        await Expect(page.Locator("header.bb-header")).ToBeVisibleAsync();
        await page.Locator("footer.bb-footer").GetByRole(AriaRole.Link, new() { Name = "Fluent builder + conditions", Exact = true }).ClickAsync();
        await page.WaitForURLAsync(BaseUrl + "/builder");
        await Expect(Form(page)).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Navigating_between_the_demos_in_the_app_leaves_a_working_form_behind()
    {
        var page = await OpenAsync("/getting-started");
        var problems = TrackProblems(page, BaseUrl);

        // Client-side navigation through every demo: each page's form state is disposed (an options lookup or an
        // async rule still in flight cancelled) and the next one built, all in the same WebAssembly instance.
        var links = page.Locator(".bb-docs-sidebar .bb-docs-section a");
        int count = await links.CountAsync();
        Assert.IsTrue(count > 15, $"the sidebar offers only {count} demos");
        for (int i = 1; i < count; i++)
        {
            var href = await links.Nth(i).GetAttributeAsync("href");
            await links.Nth(i).ClickAsync();
            await page.WaitForURLAsync($"{BaseUrl}/{href!.TrimStart('/')}");
            await Expect(page.Locator("h1")).ToBeVisibleAsync();
            await WaitForQuietAsync(page);
        }

        // Back on the builder demo, its form still validates and still submits.
        await links.Filter(new() { HasTextString = "Fluent builder" }).ClickAsync();
        await page.WaitForURLAsync(BaseUrl + "/builder");
        await SubmitButton(page).ClickAsync();
        await Expect(Errors(page, "FullName")).ToHaveTextAsync("This field is required.");
        await Expect(Control(page, "FullName")).ToBeFocusedAsync();

        string[] found;
        lock (problems) found = [.. problems];
        Assert.IsEmpty(found, string.Join(Environment.NewLine, found));
    }

    [TestMethod]
    public async Task View_source_shows_the_code_of_the_form_on_the_page()
    {
        var page = await OpenAsync("/builder");
        var code = page.Locator(".demo-source code");
        await Expect(code).ToBeHiddenAsync();

        await page.Locator(".demo-source summary").ClickAsync();

        await Expect(code).ToBeVisibleAsync();
        var source = await code.TextContentAsync() ?? "";
        StringAssert.Contains(source, "<BormView", "the snippet is not the page's markup");
        StringAssert.Contains(source, "BormBuilder.For<AccountSignup>()", "the snippet does not show how the schema is built");
    }
}

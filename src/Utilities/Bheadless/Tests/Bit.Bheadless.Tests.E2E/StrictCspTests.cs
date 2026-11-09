using System.Text.RegularExpressions;
using Bit.Bheadless.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bheadless.Tests.E2E;

/// <summary>
/// The components under a Content-Security-Policy whose <c>style-src</c> has no <c>'unsafe-inline'</c>, which refuses
/// every <c>style</c> attribute - in the markup and set from script alike. The demo serves <c>/strict-csp</c> under such
/// a policy and renders its components with <c>BitBhOptions.StrictCsp</c> (StrictCspRoutes.razor): no style attribute
/// of their own, their styles applied through the CSSOM by bit-bheadless-csp.js. A violation is a console error,
/// which fails any test here (see <see cref="DemoTest"/>); the policy's own report of it fails it with the details.
/// <para>No axe-core audit here: axe adds a <c>&lt;style&gt;</c> of its own, which the policy refuses like any other.
/// The components on the page are audited on their own pages, which run in the default mode.</para>
/// </summary>
[TestClass]
public sealed partial class StrictCspTests : DemoTest
{
    private const string Path = "/strict-csp";

    /// <summary>Records what the policy refuses, from before the first byte of the page is parsed.</summary>
    private const string RecordViolations =
        """
        window.__cspViolations = [];
        document.addEventListener('securitypolicyviolation', e =>
            window.__cspViolations.push(`${e.effectiveDirective}: ${e.sample || e.blockedURI} (${e.sourceFile}:${e.lineNumber})`));
        """;

    private static ILocator ByTestId(IPage page, string id) => page.Locator($"[data-testid='{id}']");

    private static Task<string> ComputedAsync(ILocator element, string property) =>
        element.EvaluateAsync<string>("(el, p) => getComputedStyle(el).getPropertyValue(p).trim()", property);

    private static Task<string> InlineAsync(ILocator element, string property) =>
        element.EvaluateAsync<string>("(el, p) => el.style.getPropertyValue(p)", property);

    private static Task<double> WidthAsync(ILocator element) =>
        element.EvaluateAsync<double>("el => el.getBoundingClientRect().width");

    private async Task<IPage> OpenStrictPageAsync()
    {
        var page = await NewPageAsync();
        await page.AddInitScriptAsync(RecordViolations);
        var response = await page.GotoAsync(BaseUrl + Path, new PageGotoOptions { Timeout = 90_000 });
        Assert.IsTrue(response is { Ok: true }, $"{Path} responded {response?.Status}.");
        await WaitForInteractiveAsync(page);
        return page;
    }

    private static async Task ExpectNoViolationsAsync(IPage page)
    {
        var violations = await page.EvaluateAsync<string[]>("() => window.__cspViolations");
        Assert.IsEmpty(violations, $"The policy refused:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [TestMethod]
    public async Task The_page_is_served_with_a_policy_that_refuses_inline_styles_and_its_markup_has_no_style_attribute()
    {
        var page = await NewPageAsync();
        var response = await page.APIRequest.GetAsync(BaseUrl + Path);
        Assert.IsTrue(response.Ok, $"{Path} responded {response.Status}.");

        Assert.IsTrue(response.Headers.TryGetValue("content-security-policy", out var policy), "No Content-Security-Policy header.");
        var styleSrc = policy.Split(';').Select(d => d.Trim()).Single(d => d.StartsWith("style-src ", StringComparison.Ordinal));
        Assert.AreEqual("style-src 'self'", styleSrc);

        var html = await response.TextAsync();
        var styled = StyleAttribute().Matches(html).Select(m => m.Value).ToArray();
        Assert.IsEmpty(styled, $"The prerendered markup has style attributes:{Environment.NewLine}{string.Join(Environment.NewLine, styled)}");
        Assert.Contains("data-bit-bh-style=", html);
    }

    [TestMethod]
    public async Task The_head_script_applies_the_styles_of_the_prerendered_markup_before_the_app_starts()
    {
        var page = await NewPageAsync();
        await page.AddInitScriptAsync(RecordViolations);
        await page.GotoAsync(BaseUrl + Path, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });

        // On the elements as the parser inserted them, before anything of the app has run. Read off the elements'
        // styles rather than measured: until its stylesheets have loaded the browser lays the page out empty.
        Assert.AreEqual("1px", await InlineAsync(ByTestId(page, "skip"), "width"));
        Assert.AreEqual("flex", await InlineAsync(ByTestId(page, "splitter"), "display"));
        Assert.AreEqual("40", await InlineAsync(ByTestId(page, "progress"), "--bit-bh-progress"));

        await WaitForInteractiveAsync(page);
        Assert.AreEqual(1d, await WidthAsync(ByTestId(page, "skip")), 0.5);
        await ExpectNoViolationsAsync(page);
    }

    [TestMethod]
    public async Task The_components_get_their_styles_through_the_CSSOM_and_keep_them_in_step_with_their_state()
    {
        var page = await OpenStrictPageAsync();

        // Static: layout and positioning the components need.
        Assert.AreEqual("flex", await ComputedAsync(ByTestId(page, "splitter"), "display"));
        Assert.AreEqual("scroll", await ComputedAsync(ByTestId(page, "scroll-viewport"), "overflow-y"));
        Assert.AreEqual("contents", await ComputedAsync(ByTestId(page, "tooltip"), "display"));
        var ratio = ByTestId(page, "aspect-ratio").Locator("xpath=..");
        var box = await ratio.EvaluateAsync<double[]>("el => { const r = el.getBoundingClientRect(); return [r.width, r.height]; }");
        Assert.AreEqual(box[0] / 2, box[1], 1, "The aspect ratio wrapper reserves half its width.");

        // Following the state.
        Assert.AreEqual("40", await ComputedAsync(ByTestId(page, "progress"), "--bit-bh-progress"));
        Assert.AreEqual("2", await ComputedAsync(ByTestId(page, "tree-child"), "--bit-bh-tree-level"));
        var thumb = ByTestId(page, "slider-thumb");
        Assert.AreEqual("absolute", await ComputedAsync(thumb, "position"));
        Assert.AreEqual("40%", await InlineAsync(thumb, "left"));

        await thumb.FocusAsync();
        await page.Keyboard.PressAsync("End");
        await PollAsync(() => InlineAsync(thumb, "left"), "100%", "the thumb's left after End");
        await PollAsync(() => InlineAsync(ByTestId(page, "slider-range"), "right"), "0%", "the range's right after End");

        await ExpectNoViolationsAsync(page);
    }

    [TestMethod]
    public async Task A_focusable_visually_hidden_element_shows_while_focused()
    {
        var page = await OpenStrictPageAsync();
        var skip = ByTestId(page, "skip");
        Assert.AreEqual(1d, await WidthAsync(skip), 0.5);

        await page.Locator(".csp-skip").FocusAsync();
        await PollAsync(() => WidthAsync(skip), w => w > 20, "the focused skip link to show");

        await ByTestId(page, "slider-thumb").FocusAsync();
        await PollAsync(() => WidthAsync(skip), w => Math.Abs(w - 1) < 0.5, "the skip link to hide again");
        await ExpectNoViolationsAsync(page);
    }

    [TestMethod]
    public async Task A_modal_dialog_opens_positioned_and_closes()
    {
        var page = await OpenStrictPageAsync();

        await ByTestId(page, "dialog-open").ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog);
        await Expect(dialog).ToBeVisibleAsync();
        Assert.AreEqual("fixed", await ComputedAsync(ByTestId(page, "dialog"), "position"));
        Assert.AreEqual("fixed", await ComputedAsync(ByTestId(page, "dialog-backdrop"), "position"));
        Assert.AreEqual("relative", await ComputedAsync(ByTestId(page, "dialog-panel"), "position"));

        await page.Keyboard.PressAsync("Escape");
        await Expect(dialog).ToBeHiddenAsync();
        await PollAsync(async () => (await FocusedAsync(page))?.Text, "Open the dialog", "the focus back on the trigger");
        await ExpectNoViolationsAsync(page);
    }

    /// <summary>A style attribute in a tag: <c> style="…"</c>, not <c>data-bit-bh-style="…"</c>.</summary>
    [GeneratedRegex(@"<[a-zA-Z][^>]*\sstyle\s*=\s*""[^""]*""", RegexOptions.IgnoreCase)]
    private static partial Regex StyleAttribute();
}

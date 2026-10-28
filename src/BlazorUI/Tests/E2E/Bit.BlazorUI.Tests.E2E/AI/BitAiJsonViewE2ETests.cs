using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiJsonView at /e2e/aijsonview in a real browser: the tree a JSON value is drawn as (open two levels deep,
/// a large container collapsed with its count), every kind of value in its theme color, expanding and collapsing a
/// container by keyboard (Tab, Enter, Space, the focus kept and its ring) and by pointer, containers told apart by their
/// path, the chevron of a collapsed container mirrored in a right-to-left tree (explicit and inherited) and its turn
/// following reduced motion, Disabled (the toggles and the page's handlers), the public variables and Styles / Classes.
/// </summary>
[TestClass]
public class BitAiJsonViewE2ETests : E2ETestBase
{
    private const string Route = "/e2e/aijsonview";

    private ILocator Root(string id) => Page.Locator($"#{id}");

    private ILocator Toggles(string id) => Root(id).Locator("button.bit-aijsv-tgl");

    private ILocator Toggle(string id, string key) => Toggles(id).Filter(new() { Has = Page.Locator(".bit-aijsv-key", new() { HasTextString = $"\"{key}\"" }) });

    [TestMethod]
    public async Task Basic_TheValue_IsATreeOpenTwoLevelsDeep()
    {
        await OpenAsync(Route);

        var root = Root("jsv-basic");
        await Expect(root).ToHaveClassAsync(HasClass("bit-aijsv"));

        // The root, tags and address are open; geo, two levels down, starts collapsed with its entry count.
        await Expect(Toggles("jsv-basic")).ToHaveCountAsync(4);
        var expanded = await Toggles("jsv-basic").EvaluateAllAsync<string[]>("bs => bs.map(b => b.getAttribute('aria-expanded'))");
        CollectionAssert.AreEqual(new[] { "true", "true", "true", "false" }, expanded);
        await Expect(Toggle("jsv-basic", "geo")).ToHaveTextAsync("\"geo\": { 1 }");
        await Expect(Toggle("jsv-basic", "tags")).ToHaveTextAsync("\"tags\": [");
        await Expect(Toggles("jsv-basic").First).ToHaveAttributeAsync("type", "button");

        await Expect(root.Locator(".bit-aijsv-key")).ToHaveTextAsync(["\"name\"", "\"age\"", "\"admin\"", "\"manager\"", "\"tags\"", "\"address\"", "\"city\"", "\"geo\""]);
        await Expect(root.Locator(".bit-aijsv-str")).ToHaveTextAsync(["\"Ada\"", "\"x\"", "\"y\"", "\"London\""]);
        await Expect(root.Locator(".bit-aijsv-num")).ToHaveTextAsync(["36"]);
        await Expect(root.Locator(".bit-aijsv-bln")).ToHaveTextAsync(["true"]);
        await Expect(root.Locator(".bit-aijsv-nul")).ToHaveTextAsync(["null"]);

        // Every row is a line of its own, and the entries of an open container are indented under it.
        await Expect(root.Locator(".bit-aijsv-row").First).ToHaveCSSAsync("display", "block");
        var group = root.Locator(".bit-aijsv-grp").First;
        Assert.IsTrue(AiE2EHelpers.ParsePx(await group.EvaluateAsync<string>("e => getComputedStyle(e).paddingInlineStart")) > 0, "The entries are indented.");
        var rootRow = await root.Locator(":scope > .bit-aijsv-row").First.RectAsync();
        var nameRow = await group.Locator(":scope > .bit-aijsv-row").First.Locator(".bit-aijsv-key").RectAsync();
        Assert.IsTrue(nameRow.Left > rootRow.Left, "The entries start further in than the brace of their container.");

        // The tree is set in the monospaced family of the theme.
        var mono = await root.EvaluateAsync<string>(
            "e => { const p = document.createElement('span'); p.style.fontFamily = 'var(--bit-tpg-font-family-mono)'; e.appendChild(p); const f = getComputedStyle(p).fontFamily; p.remove(); return f; }");
        await Expect(root).ToHaveCSSAsync("font-family", mono);
    }

    [TestMethod]
    public async Task Basic_NoValue_RendersAnEmptyRoot()
    {
        await OpenAsync(Route);

        await Expect(Root("jsv-null")).ToHaveClassAsync(HasClass("bit-aijsv"));
        Assert.AreEqual(0, await Root("jsv-null").EvaluateAsync<int>("e => e.childElementCount"));
    }

    [TestMethod]
    public async Task Colors_EachKindOfValue_TakesItsThemeColor()
    {
        await OpenAsync(Route);

        var root = Root("jsv-basic");
        await Expect(root.Locator(".bit-aijsv-key").First).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-wrn-fg)"));
        await Expect(root.Locator(".bit-aijsv-str").First).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-suc-fg)"));
        await Expect(root.Locator(".bit-aijsv-num").First).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-sec-fg)"));
        await Expect(root.Locator(".bit-aijsv-bln").First).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-pri-fg)"));
        await Expect(root.Locator(".bit-aijsv-nul").First).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-ter)"));
        await Expect(Toggles("jsv-basic").First).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-sec)"));

        // A toggle keeps nothing of a button's look but its cursor.
        await Expect(Toggles("jsv-basic").First).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
        await Expect(Toggles("jsv-basic").First).ToHaveCSSAsync("border-top-style", "none");
        await Expect(Toggles("jsv-basic").First).ToHaveCSSAsync("cursor", "pointer");
    }

    [TestMethod]
    public async Task Keyboard_EnterAndSpace_ToggleAContainerAndKeepTheFocusOnIt()
    {
        await OpenAsync(Route);

        await Page.Locator("#jsv-before").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        var rootToggle = Toggles("jsv-keys").First;
        await Expect(rootToggle).ToBeFocusedAsync();

        // The ring of the theme marks the toggle the keyboard is on.
        await Expect(rootToggle).Not.ToHaveCSSAsync("box-shadow", "none");
        await Expect(rootToggle).ToHaveCSSAsync("outline-style", "none");

        await Page.Keyboard.PressAsync("Tab");
        var a = Toggle("jsv-keys", "a");
        await Expect(a).ToBeFocusedAsync();
        await Expect(a).ToHaveAttributeAsync("aria-expanded", "true");

        await Page.Keyboard.PressAsync("Enter");
        await Expect(a).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(a).ToHaveTextAsync("\"a\": { 1 }");
        await Expect(a).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync(" ");
        await Expect(a).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(a).ToHaveTextAsync("\"a\": {");
        await Expect(a).ToBeFocusedAsync();
        await Expect(Root("jsv-keys").Locator(".bit-aijsv-key", new() { HasTextString = "\"b\"" })).ToBeVisibleAsync();

        // A scalar is not a stop of its own: the next Tab goes to the next container, then out of the tree.
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Toggle("jsv-keys", "c")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#jsv-after")).ToBeFocusedAsync();

        // Shift+Tab walks back.
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(Toggle("jsv-keys", "c")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(a).ToBeFocusedAsync();

        // A collapsed container takes the stops inside it out of the way: the next Tab after the root leaves the tree.
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(rootToggle).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(rootToggle).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(rootToggle).ToHaveTextAsync("{ 2 }");
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#jsv-after")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Pointer_AClick_TogglesTheContainerItIsOn()
    {
        await OpenAsync(Route);

        var geo = Toggle("jsv-basic", "geo");
        await geo.ClickAsync();
        await Expect(geo).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Root("jsv-basic").Locator(".bit-aijsv-key", new() { HasTextString = "\"lat\"" })).ToBeVisibleAsync();
        await Expect(Root("jsv-basic").Locator(".bit-aijsv-num")).ToHaveTextAsync(["36", "51.5"]);

        // Collapsing the root hides everything under it behind its count.
        var root = Toggles("jsv-basic").First;
        await root.ClickAsync();
        await Expect(root).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(root).ToHaveTextAsync("{ 6 }");
        await Expect(Toggles("jsv-basic")).ToHaveCountAsync(1);

        // Opening it again finds geo as the user left it.
        await root.ClickAsync();
        await Expect(Toggle("jsv-basic", "geo")).ToHaveAttributeAsync("aria-expanded", "true");
    }

    [TestMethod]
    public async Task Depth_DefaultExpandDepthAndLargeContainerSize_DecideWhatStartsOpen()
    {
        await OpenAsync(Route);

        await Expect(Toggle("jsv-depth1", "tags")).ToHaveTextAsync("\"tags\": [ 2 ]");
        await Expect(Toggle("jsv-depth1", "address")).ToHaveTextAsync("\"address\": { 2 }");
        await Expect(Toggles("jsv-depth1").First).ToHaveAttributeAsync("aria-expanded", "true");

        // Thirty entries are past the default size, so the array starts collapsed even at the root.
        await Expect(Toggles("jsv-large")).ToHaveCountAsync(1);
        await Expect(Toggles("jsv-large")).ToHaveTextAsync("[ 30 ]");
        await Expect(Root("jsv-large").Locator(".bit-aijsv-num")).ToHaveCountAsync(0);

        await Expect(Toggles("jsv-large-allowed")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Root("jsv-large-allowed").Locator(".bit-aijsv-num")).ToHaveCountAsync(30);
    }

    [TestMethod]
    public async Task Paths_TwoContainersOfTheSameName_ToggleApart()
    {
        await OpenAsync(Route);

        // The two items of the array start collapsed (one level deep), each "{ 1 }".
        var items = Root("jsv-paths").Locator(":scope > .bit-aijsv-grp > .bit-aijsv-row > button.bit-aijsv-tgl");
        await Expect(items).ToHaveCountAsync(2);
        await Expect(items).ToHaveTextAsync(["{ 1 }", "{ 1 }"]);

        await items.Nth(0).ClickAsync();
        await Expect(items.Nth(0)).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(items.Nth(1)).ToHaveAttributeAsync("aria-expanded", "false");

        var inner = Toggle("jsv-paths", "inner");
        await Expect(inner).ToHaveCountAsync(1);
        await inner.ClickAsync();
        await Expect(inner).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Root("jsv-paths").Locator(".bit-aijsv-num")).ToHaveTextAsync(["1"]);

        // The other item's "inner" is a container of its own: opening the second item finds it collapsed.
        await items.Nth(1).ClickAsync();
        await Expect(Toggle("jsv-paths", "inner")).ToHaveCountAsync(2);
        await Expect(Toggle("jsv-paths", "inner").Nth(1)).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Toggle("jsv-paths", "inner").Nth(0)).ToHaveAttributeAsync("aria-expanded", "true");
    }

    [TestMethod]
    [DataRow("jsv-ltr", false)]
    [DataRow("jsv-rtl", true)]
    [DataRow("jsv-rtl-inherited", true)]
    public async Task Rtl_TheChevronOfACollapsedContainer_PointsAlongTheReadingDirection(string id, bool rtl)
    {
        await OpenAsync(Route);

        if (id == "jsv-rtl") await Expect(Root(id)).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Root(id)).ToHaveCSSAsync("direction", rtl ? "rtl" : "ltr");

        var closed = Toggle(id, "closed");
        var chevron = closed.Locator("i.bit-aijsv-tic");
        await Expect(chevron).ToHaveClassAsync(HasClass("bit-aijsv-tcl"));
        await Expect(chevron).ToHaveClassAsync(HasClass("bit-icon--ChevronRight"));
        await Expect(chevron).ToHaveAttributeAsync("aria-hidden", "true");

        // Collapsed: to the right in a left-to-right tree, turned around to the left in a right-to-left one.
        await WaitForRotationAsync(chevron, rtl ? 180 : 0);

        // Open: turned down, whichever way the tree reads.
        await WaitForRotationAsync(Toggles(id).First.Locator("i.bit-aijsv-tic"), 90);

        await closed.ClickAsync();
        await Expect(closed).ToHaveAttributeAsync("aria-expanded", "true");
        await WaitForRotationAsync(chevron, 90);

        await closed.ClickAsync();
        await Expect(closed).ToHaveAttributeAsync("aria-expanded", "false");
        await WaitForRotationAsync(chevron, rtl ? 180 : 0);

        // The chevron sits on the side the row starts from, before the key.
        var icon = await chevron.RectAsync();
        var key = await closed.Locator(".bit-aijsv-key").RectAsync();
        if (rtl) Assert.IsTrue(icon.Left >= key.Right - 1, "The chevron is to the right of the key.");
        else Assert.IsTrue(icon.Right <= key.Left + 1, "The chevron is to the left of the key.");
    }

    [TestMethod]
    public async Task Motion_TheTurnOfTheChevron_FollowsReducedMotion()
    {
        await OpenAsync(Route);

        var chevron = Toggles("jsv-basic").First.Locator("i.bit-aijsv-tic");
        await Expect(chevron).ToHaveCSSAsync("transition-property", "transform");
        var full = AiE2EHelpers.ParseSeconds(await chevron.EvaluateAsync<string>("e => getComputedStyle(e).transitionDuration"));
        Assert.IsTrue(full >= 0.05, $"The chevron turns over a visible time, was {full}s.");

        // Reduced, the turn is as good as instant (the motion tokens collapse to a hair above zero).
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await AiE2EHelpers.PollAsync(
            async () => AiE2EHelpers.ParseSeconds(await chevron.EvaluateAsync<string>("e => getComputedStyle(e).transitionDuration")) < 0.001,
            "The turn of the chevron collapses under reduced motion.");
    }

    [TestMethod]
    public async Task Disabled_TheTogglesDoNothingAndThePageHandlersStayOff()
    {
        await OpenAsync(Route);

        var root = Root("jsv-disabled");
        await Expect(root).ToHaveClassAsync(HasClass("bit-dis"));

        var toggles = Toggles("jsv-disabled");
        await Expect(toggles).ToHaveCountAsync(2);
        foreach (var toggle in await toggles.AllAsync())
        {
            await Expect(toggle).ToBeDisabledAsync();
            await Expect(toggle).ToHaveAttributeAsync("aria-disabled", "true");
        }

        await Expect(toggles.First).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-dis)"));
        await Expect(toggles.First).ToHaveCSSAsync("cursor", "default");

        // A click that still reaches a toggle (a screen reader's) leaves it as it was.
        var closed = Toggle("jsv-disabled", "closed");
        await closed.EvaluateAsync("b => { b.disabled = false; b.click(); }");
        await NextFramesAsync();
        await Expect(closed).ToHaveAttributeAsync("aria-expanded", "false");

        // A click inside the tree runs none of the page's handlers on it, which the enabled tree does.
        await root.Locator(".bit-aijsv-key", new() { HasTextString = "\"open\"" }).ClickAsync();
        await Root("jsv-enabled").Locator(".bit-aijsv-key", new() { HasTextString = "\"open\"" }).ClickAsync();
        await Expect(Page.Locator("#jsv-enabled-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#jsv-disabled-clicks")).ToHaveTextAsync("0");

        // The values stay readable.
        await Expect(root.Locator(".bit-aijsv-num").First).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-sec-fg)"));
    }

    [TestMethod]
    public async Task Variables_OnTheInstance_RestyleTheTree()
    {
        await OpenAsync(Route);

        var root = Root("jsv-vars");
        await Expect(root).ToHaveCSSAsync("font-size", "18px");
        await Expect(root).ToHaveCSSAsync("line-height", "36px");
        await Expect(root.Locator(".bit-aijsv-grp").First).ToHaveCSSAsync("padding-left", "31px");
        await Expect(root.Locator(".bit-aijsv-key").First).ToHaveCSSAsync("color", "rgb(1, 2, 3)");
        await Expect(root.Locator(".bit-aijsv-str").First).ToHaveCSSAsync("color", "rgb(4, 5, 6)");
        await Expect(root.Locator(".bit-aijsv-num").First).ToHaveCSSAsync("color", "rgb(7, 8, 9)");
        await Expect(root.Locator(".bit-aijsv-bln").First).ToHaveCSSAsync("color", "rgb(10, 11, 12)");
        await Expect(root.Locator(".bit-aijsv-nul").First).ToHaveCSSAsync("color", "rgb(13, 14, 15)");
        await Expect(Toggles("jsv-vars").First).ToHaveCSSAsync("color", "rgb(16, 17, 18)");
        await Expect(Toggles("jsv-vars").First.Locator("i")).ToHaveCSSAsync("font-size", "13px");
    }

    [TestMethod]
    public async Task Styles_ReachTheirParts()
    {
        await OpenAsync(Route);

        var root = Root("jsv-styled");
        await Expect(root.Locator(".bit-aijsv-key").First).ToHaveCSSAsync("color", "rgb(40, 41, 42)");
        await Expect(root.Locator(".bit-aijsv-str").First).ToHaveCSSAsync("color", "rgb(43, 44, 45)");
        await Expect(root.Locator(".bit-aijsv-grp").First).ToHaveCSSAsync("padding-left", "50px");
        await Expect(Toggles("jsv-styled").First).ToHaveClassAsync(HasClass("jsv-toggle-class"));
        await Expect(root.Locator(".bit-aijsv-row").First).ToHaveClassAsync(HasClass("jsv-row-class"));
    }

    // Waits for the direction a right-pointing glyph ends up pointing in, in degrees clockwise from the right (a
    // transition may be running): its transform, then its scale - which the stylesheet may use to mirror it, and which
    // applies after the transform.
    private static async Task WaitForRotationAsync(ILocator element, double degrees)
    {
        var settled = await element.EvaluateAsync<double>(
            """
            (e, expected) => new Promise(resolve => {
                const started = performance.now();
                const read = () => {
                    const style = getComputedStyle(e);
                    const t = style.transform;
                    const m = t === 'none' ? new DOMMatrixReadOnly() : new DOMMatrixReadOnly(t);
                    const s = (style.scale && style.scale !== 'none' ? style.scale : '1').split(/\s+/).map(Number);
                    const sx = s[0], sy = s.length > 1 ? s[1] : s[0];
                    const x = m.a * sx, y = m.b * sy;
                    return (Math.atan2(y, x) * 180 / Math.PI + 360) % 360;
                };
                const check = () => {
                    const angle = read();
                    if (Math.abs(angle - expected) < 0.5 || performance.now() - started > 5000) return resolve(angle);
                    requestAnimationFrame(check);
                };
                check();
            })
            """, degrees);

        Assert.AreEqual(degrees, settled, 0.5, $"The chevron is turned {degrees} degrees.");
    }
}

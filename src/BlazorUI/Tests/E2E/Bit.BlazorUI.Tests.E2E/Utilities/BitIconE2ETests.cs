using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Utilities;

/// <summary>
/// Drives BitIcon (/e2e/icon) in a real browser: the built-in glyphs of the Icons package drawn by its loaded font with
/// a real width, every Variant (Text, Outline, Fill, Circular) enabled and disabled, every Rotate, RotateAngle, every
/// Flip, FlipRtl (inherited and explicit directions), every Animation running with its duration, delay, iteration
/// count and the reduced-motion slow-down that ForceAnimation undoes, FixedWidth, a clickable icon (button role,
/// tab stop, Enter and Space with the right arguments, Space not scrolling, the hover / press / focus states, the
/// 24px pointer target, disabled), custom SVG content and Inline, the naming and hiding rules, every Color on the
/// text and fill variants, the external sets (FontAwesome, Bootstrap, Material Icons / Symbols ligatures, Css, plain
/// strings, IconResolver), every Size, FontSize, the public --bit-Icon-* variables and the parameters winning over
/// them, the BitParams cascade, Style/Class, RTL, the prerender, runtime re-renders and unmount.
/// </summary>
[TestClass]
public class BitIconE2ETests : E2ETestBase
{
    private const string Path = "/e2e/icon";

    [TestMethod]
    public async Task Prerender_ThePageHtml_ContainsTheIconsBeforeTheCircuitConnects()
    {
        var html = await (await Page.APIRequest.GetAsync($"{BaseUrl}{Path}")).TextAsync();

        StringAssert.Contains(html, "<i id=\"ico-accept\" aria-hidden=\"true\" class=\"bit-icon bit-icon--Accept bit-ico-txt bit-ico\">");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");

        await OpenAsync(Path);
        await Expect(Page.Locator("#ico-accept")).ToHaveClassAsync(HasClass("bit-icon--Accept"));
    }

    [TestMethod]
    public async Task BuiltIn_AGlyphOfTheIconsPackage_IsDrawnByItsLoadedFont_WithARealWidth()
    {
        await OpenAsync(Path);
        await Page.EvaluateAsync("() => document.fonts.ready");

        Assert.IsTrue(await Page.EvaluateAsync<bool>("() => [...document.fonts].some(f => f.family.replace(/\"/g, '') === 'Fabric MDL2' && f.status === 'loaded')"),
                      "The Icons package's Fabric MDL2 font is loaded.");

        foreach (var (id, name) in new[] { ("#ico-accept", "Accept"), ("#ico-bus", "Bus"), ("#ico-pinned", "Pinned") })
        {
            var icon = Page.Locator(id);
            await Expect(icon).ToHaveClassAsync(HasClass($"bit-icon--{name}"));
            StringAssert.Contains(await ComputedStyleAsync(id, "font-family"), "Fabric MDL2");
            Assert.AreNotEqual("none", await PseudoAsync(id, "::before", "content"), $"{name} has a glyph.");
            Assert.IsTrue((await icon.BoundingBoxAsync())!.Width >= 12, $"{name} is drawn with a real width.");
        }

        // A name the set does not have draws nothing.
        Assert.AreEqual("none", await PseudoAsync("#ico-unknown", "::before", "content"));
        Assert.AreEqual(0, Math.Round((await Page.Locator("#ico-unknown").BoundingBoxAsync())!.Width));
    }

    [TestMethod]
    public async Task Basic_AnUnnamedIcon_IsADecorativePrimaryGlyphAtTheMediumSize()
    {
        await OpenAsync(Path);

        var icon = Page.Locator("#ico-accept");

        await Expect(icon).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(icon).Not.ToHaveAttributeAsync("role", new Regex(".*"));
        await Expect(icon).ToHaveCSSAsync("color", await ResolveAsync("color", "var(--bit-clr-pri)"));
        await Expect(icon).ToHaveCSSAsync("font-size", await ResolveAsync("font-size", "var(--bit-siz-icon-md)"));
        await Expect(icon).ToHaveCSSAsync("user-select", "none");
    }

    [TestMethod]
    public async Task Disabled_DrawsTheGlyphInTheDisabledColor()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#ico-disabled")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Page.Locator("#ico-disabled")).ToHaveCSSAsync("color", await ResolveAsync("color", "var(--bit-clr-pri-dis-text)"));
    }

    [TestMethod]
    public async Task Variant_TextIsTheBareGlyph_OutlineAndFillPutItInABox()
    {
        await OpenAsync(Path);

        var primary = await ResolveAsync("color", "var(--bit-clr-pri)");
        var onPrimary = await ResolveAsync("color", "var(--bit-clr-pri-text)");

        foreach (var id in new[] { "#ico-variant-default", "#ico-variant-Text" })
        {
            var text = Page.Locator(id);
            await Expect(text).ToHaveClassAsync(HasClass("bit-ico-txt"));
            await Expect(text).ToHaveCSSAsync("padding", "0px");
            await Expect(text).ToHaveCSSAsync("border-top-width", "0px");
            await Expect(text).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
            await Expect(text).ToHaveCSSAsync("color", primary);
        }

        var outline = Page.Locator("#ico-variant-Outline");
        await Expect(outline).ToHaveClassAsync(HasClass("bit-ico-out"));
        await Expect(outline).ToHaveCSSAsync("border-top-color", primary);
        await Expect(outline).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
        await Expect(outline).ToHaveCSSAsync("color", primary);
        Assert.IsTrue(Px(await ComputedStyleAsync("#ico-variant-Outline", "padding-top")) > 0);
        Assert.IsTrue(Px(await ComputedStyleAsync("#ico-variant-Outline", "border-top-width")) > 0);

        var fill = Page.Locator("#ico-variant-Fill");
        await Expect(fill).ToHaveClassAsync(HasClass("bit-ico-fil"));
        await Expect(fill).ToHaveCSSAsync("background-color", primary);
        await Expect(fill).ToHaveCSSAsync("color", onPrimary);

        Assert.IsTrue((await fill.BoundingBoxAsync())!.Width > (await Page.Locator("#ico-variant-Text").BoundingBoxAsync())!.Width);
    }

    [TestMethod]
    public async Task Variant_Circular_IsASquareCircle()
    {
        await OpenAsync(Path);

        foreach (var id in new[] { "#ico-variant-outline-circular", "#ico-variant-fill-circular" })
        {
            var box = await Page.Locator(id).BoundingBoxAsync();
            await Expect(Page.Locator(id)).ToHaveClassAsync(HasClass("bit-ico-cir"));
            Assert.AreEqual(box!.Width, box.Height, 0.5, "As wide as it is tall.");
            Assert.IsTrue(Px(await ComputedStyleAsync(id, "border-top-left-radius")) >= box.Width / 2, "Fully rounded.");
        }
    }

    [TestMethod]
    public async Task Variant_Disabled_EveryVariantTakesTheDisabledColors()
    {
        await OpenAsync(Path);

        var disText = await ResolveAsync("color", "var(--bit-clr-pri-dis-text)");
        var dis = await ResolveAsync("color", "var(--bit-clr-pri-dis)");

        await Expect(Page.Locator("#ico-variant-dis-Text")).ToHaveCSSAsync("color", disText);
        await Expect(Page.Locator("#ico-variant-dis-Outline")).ToHaveCSSAsync("color", disText);
        await Expect(Page.Locator("#ico-variant-dis-Outline")).ToHaveCSSAsync("border-top-color", dis);
        await Expect(Page.Locator("#ico-variant-dis-Fill")).ToHaveCSSAsync("color", disText);
        await Expect(Page.Locator("#ico-variant-dis-Fill")).ToHaveCSSAsync("background-color", dis);
    }

    [TestMethod]
    [DataRow("Rotate90", "bit-ico-rt90", "90deg")]
    [DataRow("Rotate180", "bit-ico-rt180", "180deg")]
    [DataRow("Rotate270", "bit-ico-rt270", "270deg")]
    [DataRow("45", null, "45deg")]
    [DataRow("neg30", null, "-30deg")]
    [DataRow("angle-wins", "bit-ico-rt90", "10deg")]
    public async Task Rotate_TurnsTheGlyph_AndRotateAngleWinsOverRotate(string id, string? className, string angle)
    {
        await OpenAsync(Path);

        var icon = Page.Locator($"#ico-rot-{id}");

        if (className is not null) await Expect(icon).ToHaveClassAsync(HasClass(className));
        await Expect(icon).ToHaveClassAsync(HasClass("bit-ico-trn"));
        await Expect(icon).ToHaveCSSAsync("rotate", angle);

        Assert.AreEqual("none", await ComputedStyleAsync("#ico-rot-none", "rotate"));
    }

    [TestMethod]
    public async Task Rotate_AnAngle_TurnsTheDrawnBox()
    {
        await OpenAsync(Path);

        var straight = await Page.Locator("#ico-rot-0-fill").BoundingBoxAsync();
        var turned = await Page.Locator("#ico-rot-45").BoundingBoxAsync();

        // A square turned by 45 degrees is a diamond whose box is the square's diagonal.
        Assert.AreEqual(straight!.Width * Math.Sqrt(2), turned!.Width, 1.5);
    }

    [TestMethod]
    [DataRow("Horizontal", "bit-ico-flh", "-1 1")]
    [DataRow("Vertical", "bit-ico-flv", "1 -1")]
    [DataRow("Both", "bit-ico-flb", "-1")]
    public async Task Flip_MirrorsTheGlyph(string flip, string className, string scale)
    {
        await OpenAsync(Path);

        var icon = Page.Locator($"#ico-flip-{flip}");

        await Expect(icon).ToHaveClassAsync(HasClass(className));
        await Expect(icon).ToHaveCSSAsync("scale", scale);
    }

    [TestMethod]
    public async Task Flip_AndARotation_Combine()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#ico-flip-rotate")).ToHaveCSSAsync("scale", "-1 1");
        await Expect(Page.Locator("#ico-flip-rotate")).ToHaveCSSAsync("rotate", "45deg");
    }

    [TestMethod]
    public async Task FlipRtl_MirrorsOnlyInARightToLeftDirection_InheritedOrExplicit()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#ico-fliprtl-ltr")).ToHaveClassAsync(HasClass("bit-ico-frt"));
        await Expect(Page.Locator("#ico-fliprtl-ltr")).ToHaveCSSAsync("scale", "1");
        await Expect(Page.Locator("#ico-fliprtl-rtl")).ToHaveCSSAsync("scale", "-1 1");
        await Expect(Page.Locator("#ico-fliprtl-dir")).ToHaveCSSAsync("scale", "-1 1");

        // An explicit left-to-right inside a right-to-left container is not mirrored, and a clock never is.
        await Expect(Page.Locator("#ico-fliprtl-explicit-ltr")).ToHaveCSSAsync("scale", "1");
        await Expect(Page.Locator("#ico-clock-rtl")).ToHaveCSSAsync("scale", "none");

        // A horizontal flip and the right-to-left mirror cancel each other out.
        await Expect(Page.Locator("#ico-fliprtl-flip")).ToHaveCSSAsync("scale", "1");
    }

    [TestMethod]
    [DataRow("Spin", "bit-ico-spn", "bit-ico-spin-anm")]
    [DataRow("SpinReverse", "bit-ico-spr", "bit-ico-spin-anm")]
    [DataRow("Pulse", "bit-ico-pls", "bit-ico-spin-anm")]
    [DataRow("Beat", "bit-ico-bet", "bit-ico-beat-anm")]
    [DataRow("Fade", "bit-ico-fad", "bit-ico-fade-anm")]
    [DataRow("Shake", "bit-ico-shk", "bit-ico-shake-anm")]
    [DataRow("Bounce", "bit-ico-bnc", "bit-ico-bounce-anm")]
    [DataRow("BeatFade", "bit-ico-btf", "bit-ico-beat-fade-anm")]
    public async Task Animation_EveryValue_LoopsTheIconInTheBrowser(string animation, string className, string keyframes)
    {
        await OpenAsync(Path);

        var id = $"#ico-anim-{animation}";
        var icon = Page.Locator(id);

        await Expect(icon).ToHaveClassAsync(HasClass(className));
        await Expect(icon).ToHaveCSSAsync("animation-name", keyframes);
        await Expect(icon).ToHaveCSSAsync("animation-iteration-count", "infinite");
        await Expect(icon).ToHaveCSSAsync("display", "inline-block");

        Assert.AreEqual("running", await icon.EvaluateAsync<string>("el => el.getAnimations()[0]?.playState"));

        // It actually moves: the animated property differs between two moments of a cycle.
        var first = await AnimatedValueAsync(id);
        await Page.WaitForFunctionAsync($"() => document.querySelector('{id}').getAnimations()[0].currentTime > 150");
        await Page.WaitForFunctionAsync($"([sel, first]) => {AnimatedValueScript} !== first", new object[] { id, first });

        if (animation == "SpinReverse") await Expect(icon).ToHaveCSSAsync("animation-direction", "reverse");
        if (animation == "Pulse") await Expect(icon).ToHaveCSSAsync("animation-timing-function", "steps(8)");
    }

    [TestMethod]
    public async Task Animation_DurationDelayAndIterationCount_ShapeTheLoop()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#ico-anim-duration")).ToHaveClassAsync(HasClass("bit-ico-anm"));
        await Expect(Page.Locator("#ico-anim-duration")).ToHaveCSSAsync("animation-duration", "4s");

        await Expect(Page.Locator("#ico-anim-delay")).ToHaveClassAsync(HasClass("bit-ico-dly"));
        await Expect(Page.Locator("#ico-anim-delay")).ToHaveCSSAsync("animation-delay", "0.4s");
        await Expect(Page.Locator("#ico-anim-delay")).ToHaveCSSAsync("animation-duration", "1.2s");

        // Three short cycles, and then it stops.
        await Expect(Page.Locator("#ico-anim-count")).ToHaveClassAsync(HasClass("bit-ico-itr"));
        await Expect(Page.Locator("#ico-anim-count")).ToHaveCSSAsync("animation-iteration-count", "3");
        await Page.WaitForFunctionAsync("() => document.getElementById('ico-anim-count').getAnimations().length === 0");

        // A negative count is no cycle at all rather than a loop.
        await Expect(Page.Locator("#ico-anim-negative")).ToHaveCSSAsync("animation-iteration-count", "0");

        // A turn stays under the animation.
        await Expect(Page.Locator("#ico-anim-turned")).ToHaveCSSAsync("rotate", "45deg");
        await Expect(Page.Locator("#ico-anim-turned")).ToHaveCSSAsync("animation-name", "bit-ico-beat-anm");
    }

    [TestMethod]
    public async Task Animation_ReducedMotion_SlowsTheLoopsDown_AndForceAnimationRestoresTheirSpeed()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#ico-anim-Spin")).ToHaveCSSAsync("animation-duration", "1.3s");

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });

        await Expect(Page.Locator("#ico-anim-Spin")).ToHaveCSSAsync("animation-duration", "4s");
        await Expect(Page.Locator("#ico-anim-Spin")).ToHaveCSSAsync("animation-timing-function", "linear");
        await Expect(Page.Locator("#ico-anim-Beat")).ToHaveCSSAsync("animation-duration", "3s");
        await Expect(Page.Locator("#ico-anim-duration")).ToHaveCSSAsync("animation-duration", "12s");

        // Still moving, only slower.
        Assert.AreEqual("running", await Page.Locator("#ico-anim-Spin").EvaluateAsync<string>("el => el.getAnimations()[0]?.playState"));

        var forced = Page.Locator("#ico-anim-force");
        await Expect(forced).ToHaveClassAsync(HasClass("bit-fam"));
        await Expect(forced).ToHaveCSSAsync("animation-duration", "1.3s");
    }

    [TestMethod]
    public async Task FixedWidth_GivesEveryGlyphTheSameBox_SoTheLabelsLineUp()
    {
        await OpenAsync(Path);

        var home = await Page.Locator("#ico-fixed-home").BoundingBoxAsync();
        var signOut = await Page.Locator("#ico-fixed-signout").BoundingBoxAsync();
        var fontSize = Px(await ComputedStyleAsync("#ico-fixed-home", "font-size"));

        await Expect(Page.Locator("#ico-fixed-home")).ToHaveClassAsync(HasClass("bit-ico-fxw"));
        Assert.AreEqual(fontSize * 1.25, home!.Width, 0.5, "1.25em wide.");
        Assert.AreEqual(home.Width, signOut!.Width, 0.5);
        await Expect(Page.Locator("#ico-fixed-home")).ToHaveCSSAsync("text-align", "center");

        var homeLabel = await Page.Locator("#fixed-home-label").BoundingBoxAsync();
        var signOutLabel = await Page.Locator("#fixed-signout-label").BoundingBoxAsync();
        Assert.AreEqual(homeLabel!.X, signOutLabel!.X, 0.5, "The labels start at the same place.");
    }

    [TestMethod]
    public async Task Clickable_IsANamedButtonTabStop_ThatAMouseClickActivates()
    {
        await OpenAsync(Path);

        var star = Page.Locator("#ico-star");
        await Expect(star).ToHaveAttributeAsync("role", "button");
        await Expect(star).ToHaveAttributeAsync("tabindex", "0");
        await Expect(star).ToHaveClassAsync(HasClass("bit-ico-int"));
        await Expect(star).ToHaveCSSAsync("cursor", "pointer");
        await Expect(star).Not.ToHaveAttributeAsync("aria-hidden", new Regex(".*"));
        // Named by its Title alone, and announced by it rather than by the private-use character its glyph is drawn with.
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Favorite", Exact = true })).ToHaveIdAsync("ico-star");
        await Expect(star).ToHaveAccessibleNameAsync("Favorite");
        await Expect(Page.Locator("#ico-delete")).ToHaveAccessibleNameAsync("Deleting is unavailable here");

        await star.ClickAsync();
        await Expect(Page.Locator("#starred")).ToHaveTextAsync("True");
        await Expect(star).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(star).ToHaveClassAsync(HasClass("bit-icon--FavoriteStarFill"));

        var refresh = Page.Locator("#ico-refresh");
        await refresh.ClickAsync();
        await Expect(Page.Locator("#refresh-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#refresh-args")).ToHaveTextAsync("detail=1;ctrl=False;shift=False");

        // An unnamed one falls back to its glyph's name.
        await Expect(Page.Locator("#ico-unnamed-click")).ToHaveAttributeAsync("aria-label", "Settings");
        await Page.Locator("#ico-unnamed-click").ClickAsync();
        await Expect(Page.Locator("#unnamed-clicks")).ToHaveTextAsync("1");

        // Its pointer target is at least 24px without growing the glyph.
        Assert.IsTrue(Px(await PseudoAsync("#ico-star", "::after", "width")) >= 24);
        Assert.IsTrue((await star.BoundingBoxAsync())!.Width < 24);
    }

    [TestMethod]
    public async Task Clickable_EnterAndSpace_ActivateIt_WithKeyboardArguments_AndSpaceDoesNotScroll()
    {
        await OpenAsync(Path);

        var refresh = Page.Locator("#ico-refresh");
        await refresh.FocusAsync();

        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#refresh-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#refresh-args")).ToHaveTextAsync("detail=0;ctrl=False;shift=False");

        await Page.Keyboard.PressAsync("Shift+Enter");
        await Expect(Page.Locator("#refresh-count")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#refresh-args")).ToHaveTextAsync("detail=0;ctrl=False;shift=True");

        var scrollBefore = await Page.EvaluateAsync<double>("() => window.scrollY");

        // The release is what activates it.
        await Page.Keyboard.DownAsync("Space");
        await Expect(Page.Locator("#refresh-count")).ToHaveTextAsync("2");
        await Page.Keyboard.UpAsync("Space");
        await Expect(Page.Locator("#refresh-count")).ToHaveTextAsync("3");

        Assert.AreEqual(scrollBefore, await Page.EvaluateAsync<double>("() => window.scrollY"), "Space did not scroll the page.");
    }

    [TestMethod]
    public async Task Clickable_ASpaceReleasedWithoutBeingPressedOnIt_ActivatesNothing()
    {
        await OpenAsync(Path);

        await Page.Locator("#ico-refresh").FocusAsync();
        await Page.Keyboard.UpAsync("Space");

        // Pressed here, released after the focus moved on: forgotten on the blur.
        await Page.Keyboard.DownAsync("Space");
        await Page.Locator("#ico-unnamed-click").FocusAsync();
        await Page.Locator("#ico-refresh").FocusAsync();
        await Page.Keyboard.UpAsync("Space");

        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#refresh-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Clickable_TheTabOrder_SkipsTheDisabledOne_WhichActivatesNothing()
    {
        await OpenAsync(Path);

        var delete = Page.Locator("#ico-delete");
        await Expect(delete).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(delete).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(delete).ToHaveAttributeAsync("title", "Deleting is unavailable here");
        await Expect(delete).ToHaveCSSAsync("cursor", "default");

        await Page.Locator("#before-clickable").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#ico-star")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#ico-refresh")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#ico-unnamed-click")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#after-clickable")).ToBeFocusedAsync();

        await delete.ClickAsync(new() { Force = true });
        await delete.DispatchEventAsync("click");
        await delete.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Page.Keyboard.PressAsync("Space");
        await Expect(Page.Locator("#delete-clicks")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Clickable_TheHoverPressAndFocusStates_MoveItsColors()
    {
        await OpenAsync(Path);

        var unnamed = Page.Locator("#ico-unnamed-click");
        await unnamed.HoverAsync();
        await Expect(unnamed).ToHaveCSSAsync("color", await ResolveAsync("color", "var(--bit-clr-pri-hover)"));

        await Page.Mouse.DownAsync();
        await Expect(unnamed).ToHaveCSSAsync("color", await ResolveAsync("color", "var(--bit-clr-pri-active)"));
        await Page.Mouse.UpAsync();

        // An outline icon fills under the pointer.
        var refresh = Page.Locator("#ico-refresh");
        await refresh.HoverAsync();
        await Expect(refresh).ToHaveCSSAsync("background-color", await ResolveAsync("background-color", "var(--bit-clr-pri-hover)"));
        await Expect(refresh).ToHaveCSSAsync("color", await ResolveAsync("color", "var(--bit-clr-pri-text)"));

        // The keyboard focus draws the ring.
        await Page.Mouse.MoveAsync(0, 0);
        await Page.Locator("#before-clickable").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        StringAssert.Contains(await ComputedStyleAsync("#ico-star", "box-shadow"), await ResolveAsync("color", "var(--bit-clr-pri-focus)"));
    }

    [TestMethod]
    public async Task CustomContent_ReplacesTheGlyph_AndKeepsTheIconsColorAndVariant()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#ico-custom > #custom-svg")).ToBeVisibleAsync();
        await Expect(Page.Locator("#custom-svg")).ToHaveCSSAsync("fill", await ResolveAsync("color", "var(--bit-clr-pri)"));
        await Expect(Page.Locator("#custom-fill-svg")).ToHaveCSSAsync("fill", await ResolveAsync("color", "var(--bit-clr-pri-text)"));

        var fontSize = Px(await ComputedStyleAsync("#ico-custom", "font-size"));
        Assert.AreEqual(fontSize, (await Page.Locator("#custom-svg").BoundingBoxAsync())!.Width, 0.5, "1em of the icon's size.");

        await Expect(Page.Locator("#ico-inline")).ToHaveClassAsync(HasClass("bit-ico-inl"));
        await Expect(Page.Locator("#ico-inline")).ToHaveCSSAsync("vertical-align", $"{(-0.125 * fontSize).ToString(CultureInfo.InvariantCulture)}px");
        await Expect(Page.Locator("#ico-not-inline")).ToHaveCSSAsync("vertical-align", "baseline");
    }

    [TestMethod]
    public async Task Accessibility_ANamedIconIsAnImage_AndAFocusableOneIsNeverHidden()
    {
        await OpenAsync(Path);

        await Expect(Page.GetByRole(AriaRole.Img, new() { Name = "Succeeded" })).ToHaveIdAsync("ico-aria-label");
        await Expect(Page.GetByRole(AriaRole.Img, new() { Name = "Failed on the last run" })).ToHaveIdAsync("ico-title");
        await Expect(Page.Locator("#ico-title")).ToHaveAttributeAsync("title", "Failed on the last run");
        await Expect(Page.GetByRole(AriaRole.Img, new() { Name = "External icon name" })).ToHaveIdAsync("ico-labelledby");

        // A tab stop falls back to its glyph's name - not to the class list of a css set.
        await Expect(Page.Locator("#ico-tabstop")).ToHaveAttributeAsync("aria-label", "Info");
        await Expect(Page.Locator("#ico-tabstop")).Not.ToHaveAttributeAsync("aria-hidden", new Regex(".*"));
        await Expect(Page.Locator("#ico-tabstop-css")).Not.ToHaveAttributeAsync("aria-label", new Regex(".*"));
        await Expect(Page.Locator("#ico-tabstop-css")).Not.ToHaveAttributeAsync("aria-hidden", new Regex(".*"));

        // A negative tab index is not a tab stop, so it is hidden like any decoration.
        await Expect(Page.Locator("#ico-tabstop-negative")).ToHaveAttributeAsync("aria-hidden", "true");

        // The page's own aria-hidden and role are left as they were given.
        await Expect(Page.Locator("#ico-splat-hidden")).ToHaveAttributeAsync("aria-hidden", "false");
        await Expect(Page.Locator("#ico-splat-role")).ToHaveAttributeAsync("role", "presentation");

        await Expect(Page.Locator("#ico-decorative")).ToHaveAttributeAsync("aria-hidden", "true");

        await Page.Locator("#ico-tabstop").FocusAsync();
        await Expect(Page.Locator("#ico-tabstop")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task BitParams_CascadeTheirDefaults_AndAnIconsOwnValueWins()
    {
        await OpenAsync(Path);

        foreach (var id in new[] { "#ico-params-1", "#ico-params-2" })
        {
            await Expect(Page.Locator(id)).ToHaveClassAsync(HasClass("bit-ico-out"));
            await Expect(Page.Locator(id)).ToHaveClassAsync(HasClass("bit-ico-cir"));
        }

        await Expect(Page.Locator("#ico-params-3")).ToHaveClassAsync(HasClass("bit-ico-fil"));
        await Expect(Page.Locator("#ico-params-3")).ToHaveClassAsync(HasClass("bit-ico-cir"));
        await Expect(Page.Locator("#ico-params-3")).ToHaveCSSAsync("background-color", await ResolveAsync("color", "var(--bit-clr-pri)"));

        // The resolver cascades too.
        await Expect(Page.Locator("#ico-params-resolved")).ToHaveClassAsync(new Regex("^fa-solid fa-house "));
    }

    [TestMethod]
    [DataRow("Primary", "pri", "--bit-clr-pri", "--bit-clr-pri-text", "--bit-clr-pri-dis", "--bit-clr-pri-dis-text")]
    [DataRow("Secondary", "sec", "--bit-clr-sec", "--bit-clr-sec-text", "--bit-clr-sec-dis", "--bit-clr-sec-dis-text")]
    [DataRow("Tertiary", "ter", "--bit-clr-ter", "--bit-clr-ter-text", "--bit-clr-ter-dis", "--bit-clr-ter-dis-text")]
    [DataRow("Info", "inf", "--bit-clr-inf", "--bit-clr-inf-text", "--bit-clr-inf-dis", "--bit-clr-inf-dis-text")]
    [DataRow("Success", "suc", "--bit-clr-suc", "--bit-clr-suc-text", "--bit-clr-suc-dis", "--bit-clr-suc-dis-text")]
    [DataRow("Warning", "wrn", "--bit-clr-wrn", "--bit-clr-wrn-text", "--bit-clr-wrn-dis", "--bit-clr-wrn-dis-text")]
    [DataRow("SevereWarning", "swr", "--bit-clr-swr", "--bit-clr-swr-text", "--bit-clr-swr-dis", "--bit-clr-swr-dis-text")]
    [DataRow("Error", "err", "--bit-clr-err", "--bit-clr-err-text", "--bit-clr-err-dis", "--bit-clr-err-dis-text")]
    [DataRow("PrimaryBackground", "pbg", "--bit-clr-bg-pri", "--bit-clr-fg-pri", "--bit-clr-bg-pri-dis", "--bit-clr-bg-pri-dis-text")]
    [DataRow("SecondaryBackground", "sbg", "--bit-clr-bg-sec", "--bit-clr-fg-pri", "--bit-clr-bg-sec-dis", "--bit-clr-bg-sec-dis-text")]
    [DataRow("TertiaryBackground", "tbg", "--bit-clr-bg-ter", "--bit-clr-fg-pri", "--bit-clr-bg-ter-dis", "--bit-clr-bg-ter-dis-text")]
    [DataRow("PrimaryForeground", "pfg", "--bit-clr-fg-pri", "--bit-clr-bg-pri", "--bit-clr-fg-pri-dis", "--bit-clr-fg-pri-dis-text")]
    [DataRow("SecondaryForeground", "sfg", "--bit-clr-fg-sec", "--bit-clr-bg-pri", "--bit-clr-fg-sec-dis", "--bit-clr-fg-sec-dis-text")]
    [DataRow("TertiaryForeground", "tfg", "--bit-clr-fg-ter", "--bit-clr-bg-pri", "--bit-clr-fg-ter-dis", "--bit-clr-fg-ter-dis-text")]
    [DataRow("PrimaryBorder", "pbr", "--bit-clr-brd-pri", "--bit-clr-fg-pri", "--bit-clr-brd-pri-dis", "--bit-clr-brd-pri-dis-text")]
    [DataRow("SecondaryBorder", "sbr", "--bit-clr-brd-sec", "--bit-clr-fg-pri", "--bit-clr-brd-sec-dis", "--bit-clr-brd-sec-dis-text")]
    [DataRow("TertiaryBorder", "tbr", "--bit-clr-brd-ter", "--bit-clr-fg-pri", "--bit-clr-brd-ter-dis", "--bit-clr-brd-ter-dis-text")]
    public async Task Color_EveryValue_PaintsTheGlyphTheFillAndTheDisabledFill(string color, string suffix, string main, string on, string dis, string disText)
    {
        await OpenAsync(Path);

        var text = Page.Locator($"#ico-color-{color}");
        await Expect(text).ToHaveClassAsync(HasClass($"bit-ico-{suffix}"));
        await Expect(text).ToHaveCSSAsync("color", await ResolveAsync("color", $"var({main})"));

        var fill = Page.Locator($"#ico-color-fill-{color}");
        await Expect(fill).ToHaveCSSAsync("background-color", await ResolveAsync("color", $"var({main})"));
        await Expect(fill).ToHaveCSSAsync("color", await ResolveAsync("color", $"var({on})"));

        var disabled = Page.Locator($"#ico-color-dis-{color}");
        await Expect(disabled).ToHaveCSSAsync("background-color", await ResolveAsync("color", $"var({dis})"));
        await Expect(disabled).ToHaveCSSAsync("color", await ResolveAsync("color", $"var({disText})"));
    }

    [TestMethod]
    public async Task ExternalSets_ClassBasedSets_RenderTheirClasses_AndTheirStylesheetDrawsTheGlyph()
    {
        await OpenAsync(Path);

        var expected = new Dictionary<string, string>
        {
            ["#ico-ext-string"] = "fa-solid fa-heart",
            ["#ico-ext-css"] = "fa-solid fa-heart",
            ["#ico-ext-fa"] = "fa-brands fa-github",
            ["#ico-ext-fa-short"] = "fa-solid fa-heart",
            ["#ico-ext-bi"] = "bi bi-github",
            ["#ico-ext-bit"] = "bit-icon bit-icon--Bus",
            ["#ico-ext-icon-wins"] = "bi bi-github",
        };

        foreach (var (id, classes) in expected)
        {
            await Expect(Page.Locator(id)).ToHaveClassAsync(new Regex($@"(^|\s){Regex.Escape(classes)}\s"));

            // Waited for rather than read once: the sets' stylesheet is a link of the page's head, which the interactive
            // render of the head may put in again, and the glyph is drawn once it has been applied.
            await Page.WaitForFunctionAsync("id => document.querySelector(id).getBoundingClientRect().width > 4", id);
        }

        await Expect(Page.Locator("#ico-ext-icon-wins")).Not.ToHaveClassAsync(HasClass("bit-icon--Accept"));
        await Expect(Page.Locator("#ico-ext-css")).ToHaveCSSAsync("color", await ResolveAsync("color", "var(--bit-clr-err)"));
    }

    [TestMethod]
    public async Task ExternalSets_LigatureSets_NameTheGlyphInTheirText_AndAreNeverTranslated()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#ico-ext-mi")).ToHaveClassAsync(HasClass("material-icons"));
        await Expect(Page.Locator("#ico-ext-mi")).ToHaveTextAsync("home");
        await Expect(Page.Locator("#ico-ext-mi")).ToHaveAttributeAsync("translate", "no");
        await Expect(Page.Locator("#ico-ext-mi-outlined")).ToHaveClassAsync(HasClass("material-icons-outlined"));
        await Expect(Page.Locator("#ico-ext-ms")).ToHaveClassAsync(HasClass("material-symbols-outlined"));
        await Expect(Page.Locator("#ico-ext-ms")).ToHaveTextAsync("favorite");
        await Expect(Page.Locator("#ico-ext-ms")).ToHaveCSSAsync("color", await ResolveAsync("color", "var(--bit-clr-err)"));
        await Expect(Page.Locator("#ico-ext-ms-rounded")).ToHaveClassAsync(HasClass("material-symbols-rounded"));

        // A ligature names its glyph, so a focusable one falls back to it.
        await Expect(Page.Locator("#ico-ext-ms-rounded")).ToHaveAttributeAsync("aria-label", "settings");
    }

    [TestMethod]
    public async Task IconResolver_TurnsAPlainNameIntoAnotherSetsIcon_OrLeavesItToTheBuiltInSet()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#ico-resolver-house")).ToHaveClassAsync(new Regex("^fa-solid fa-house "));
        await Expect(Page.Locator("#ico-resolver-house")).Not.ToHaveClassAsync(HasClass("bit-icon"));

        await Expect(Page.Locator("#ico-resolver-accept")).ToHaveClassAsync(HasClass("bit-icon--Accept"));
        await Expect(Page.Locator("#ico-resolver-accept")).ToHaveCSSAsync("color", await ResolveAsync("color", "var(--bit-clr-suc)"));
    }

    [TestMethod]
    [DataRow("Small", "sm", "--bit-siz-icon-sm")]
    [DataRow("Medium", "md", "--bit-siz-icon-md")]
    [DataRow("Large", "lg", "--bit-siz-icon-lg")]
    public async Task Size_EveryValue_PicksOneOfTheThemesIconSizes(string size, string suffix, string token)
    {
        await OpenAsync(Path);

        var icon = Page.Locator($"#ico-size-{size}");
        var expected = await ResolveAsync("font-size", $"var({token})");

        await Expect(icon).ToHaveClassAsync(HasClass($"bit-ico-{suffix}"));
        await Expect(icon).ToHaveCSSAsync("font-size", expected);
        Assert.AreEqual(Px(expected), (await icon.BoundingBoxAsync())!.Width, 1, "The glyph is drawn at that size.");
    }

    [TestMethod]
    public async Task FontSize_TakesAnyLength_OrInherits_AndWinsOverSize()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#ico-fontsize")).ToHaveCSSAsync("font-size", "40px");
        Assert.AreEqual(40, (await Page.Locator("#ico-fontsize").BoundingBoxAsync())!.Width, 1);
        await Expect(Page.Locator("#ico-fontsize-wins")).ToHaveCSSAsync("font-size", "33px");
        await Expect(Page.Locator("#ico-fontsize-inherit")).ToHaveCSSAsync("font-size", "28px");
    }

    [TestMethod]
    public async Task StyleAndClass_LandOnTheIcon()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#ico-style")).ToHaveCSSAsync("color", "rgb(255, 255, 255)");
        await Expect(Page.Locator("#ico-style")).ToHaveCSSAsync("background-color", "rgb(165, 42, 42)");
        await Expect(Page.Locator("#ico-style")).ToHaveCSSAsync("border-top-left-radius", "4px");
        await Expect(Page.Locator("#ico-class")).ToHaveClassAsync(HasClass("e2e-icon-class"));
    }

    [TestMethod]
    public async Task CssVariables_SetOnAContainer_RestyleEveryIcon_AndTheParametersWin()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#ico-css-text")).ToHaveCSSAsync("color", "rgb(10, 20, 30)");
        await Expect(Page.Locator("#ico-css-text")).ToHaveCSSAsync("font-size", "31px");

        var fill = Page.Locator("#ico-css-fill");
        await Expect(fill).ToHaveCSSAsync("background-color", "rgb(10, 20, 30)");
        await Expect(fill).ToHaveCSSAsync("color", "rgb(250, 240, 230)");
        await Expect(fill).ToHaveCSSAsync("padding-top", "7px");
        await Expect(fill).ToHaveCSSAsync("border-top-left-radius", "9px");
        await Expect(Page.Locator("#ico-css-outline")).ToHaveCSSAsync("border-top-width", "3px");
        Assert.AreEqual(50, Math.Round((await Page.Locator("#ico-css-fixed").BoundingBoxAsync())!.Width));

        await Expect(Page.Locator("#ico-css-params")).ToHaveCSSAsync("color", await ResolveAsync("color", "var(--bit-clr-err)"));
        await Expect(Page.Locator("#ico-css-params")).ToHaveCSSAsync("font-size", await ResolveAsync("font-size", "var(--bit-siz-icon-sm)"));

        await Page.Locator("#before-css-click").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#ico-css-click")).ToBeFocusedAsync();
        StringAssert.Contains(await ComputedStyleAsync("#ico-css-click", "box-shadow"), "rgb(0, 150, 0)");
    }

    [TestMethod]
    public async Task CssVariables_TheHoverAndActiveColors_AndTheShadesDerivedFromTheColor()
    {
        await OpenAsync(Path);

        var states = Page.Locator("#ico-css-states");
        await states.HoverAsync();
        await Expect(states).ToHaveCSSAsync("color", "rgb(100, 0, 0)");
        await Page.Mouse.DownAsync();
        await Expect(states).ToHaveCSSAsync("color", "rgb(0, 0, 100)");
        await Page.Mouse.UpAsync();

        // With only the color set, the hover is a shade of it.
        var derived = Page.Locator("#ico-css-derived");
        await derived.HoverAsync();
        await Expect(derived).ToHaveCSSAsync("color", await ResolveAsync("color", "color-mix(in srgb, rgb(10, 20, 30) 85%, var(--bit-clr-fg-pri))"));
    }

    [TestMethod]
    public async Task Rtl_AnExplicitDirection_IsWrittenOnTheIcon_AndFlipRtlFollowsIt()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#ico-rtl")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Page.Locator("#ico-rtl")).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(Page.Locator("#ico-rtl")).ToHaveCSSAsync("scale", "none");
        await Expect(Page.Locator("#ico-rtl-flip")).ToHaveCSSAsync("scale", "-1 1");
    }

    [TestMethod]
    public async Task Runtime_ChangingTheParameters_ReRendersTheIcon()
    {
        await OpenAsync(Path);

        var icon = Page.Locator("#ico-runtime");

        await Page.Locator("#btn-set-name").ClickAsync();
        await Expect(icon).ToHaveClassAsync(HasClass("bit-icon--Bus"));
        await Expect(icon).Not.ToHaveClassAsync(HasClass("bit-icon--Accept"));

        await Page.Locator("#btn-set-color").ClickAsync();
        await Expect(icon).ToHaveCSSAsync("color", await ResolveAsync("color", "var(--bit-clr-suc)"));

        await icon.ClickAsync();
        await Expect(Page.Locator("#runtime-clicks")).ToHaveTextAsync("1");

        await Page.Locator("#btn-toggle-disabled").ClickAsync();
        await Expect(icon).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(icon).ToHaveAttributeAsync("aria-disabled", "true");
        await icon.DispatchEventAsync("click");
        await Page.Locator("#btn-toggle-disabled").ClickAsync();
        await Expect(icon).ToHaveAttributeAsync("tabindex", "0");
        await Expect(Page.Locator("#runtime-clicks")).ToHaveTextAsync("1");

        // Without a handler it is a named image again, not a button.
        await Page.Locator("#btn-toggle-clickable").ClickAsync();
        await Expect(icon).ToHaveAttributeAsync("role", "img");
        await Expect(icon).Not.ToHaveAttributeAsync("tabindex", new Regex(".*"));
        await Expect(icon).Not.ToHaveClassAsync(HasClass("bit-ico-int"));
        await icon.ClickAsync();
        await Expect(Page.Locator("#runtime-clicks")).ToHaveTextAsync("1");

        await Page.Locator("#btn-toggle-clickable").ClickAsync();
        await Expect(icon).ToHaveAttributeAsync("role", "button");
        await icon.FocusAsync();
        await Page.Keyboard.PressAsync("Space");
        await Expect(Page.Locator("#runtime-clicks")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task Unmount_RemovesAClickableIcon_WithoutAnError()
    {
        await OpenAsync(Path);

        await Page.Locator("#btn-toggle-mounted").ClickAsync();
        await Expect(Page.Locator("#mounted")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#ico-mounted")).ToHaveCountAsync(0);

        await Page.Locator("#btn-toggle-mounted").ClickAsync();
        await Expect(Page.Locator("#ico-mounted")).ToHaveAttributeAsync("role", "button");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    // The property an icon animation moves, read the same way at two moments.
    private const string AnimatedValueScript = "(() => { const s = getComputedStyle(document.querySelector(sel)); return s.transform + '|' + s.opacity; })()";

    private Task<string> AnimatedValueAsync(string selector)
    {
        return Page.EvaluateAsync<string>($"sel => {AnimatedValueScript}", selector);
    }

    private Task<string> PseudoAsync(string selector, string pseudo, string property)
    {
        return Page.Locator(selector).EvaluateAsync<string>("(el, [pseudo, property]) => getComputedStyle(el, pseudo).getPropertyValue(property)", new[] { pseudo, property });
    }

    private static double Px(string value) => double.Parse(value.Replace("px", ""), CultureInfo.InvariantCulture);

    // What a CSS value - a theme token, typically - resolves to on this page, read off a probe element.
    private Task<string> ResolveAsync(string property, string value)
    {
        return Page.EvaluateAsync<string>(
            @"([property, value]) => {
                const probe = document.createElement('div');
                probe.style.setProperty(property, value);
                document.body.appendChild(probe);
                const resolved = getComputedStyle(probe).getPropertyValue(property);
                probe.remove();
                return resolved;
            }", new[] { property, value });
    }
}

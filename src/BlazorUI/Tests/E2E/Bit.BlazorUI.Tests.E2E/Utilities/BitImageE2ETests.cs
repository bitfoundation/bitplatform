using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Utilities;

/// <summary>
/// Drives BitImage (/e2e/image and /e2e/image/errors) in a real browser: the image a prerendered page hands over to the
/// circuit, the frame's Width / Height / AspectRatio / Fluid / MaximizeFrame, every ImageFit and Cover, ImagePosition,
/// the shapes, border and shadow, the loading states (a response held back by a route: the LoadingTemplate, the alt
/// of a hidden image, OnLoadingStateChange, LoadingState, OnLoad, ReloadAsync), FadeIn, StartVisible and the
/// cross-fading PlaceholderSrc with reduced motion and ForceAnimation, a real 404 (ErrorTemplate, OnError, FallbackSrc
/// tried once), the native attributes (Loading, Decoding, FetchPriority, CrossOrigin, ReferrerPolicy, Draggable,
/// ImageAttributes), Srcset / Sizes / Sources, lazy loading in a scroller, OnClick (button role, keyboard, hover and
/// press tints, focus ring, disabled), the ChildContent overlay letting clicks through, FocusAsync, the BitParams
/// cascade, Styles / Classes of every part, the public --bit-Image-* variables, Visibility, RTL, runtime re-renders
/// and unmount.
/// </summary>
[TestClass]
public class BitImageE2ETests : E2ETestBase
{
    private const string Path = "/e2e/image";
    private const string ErrorsPath = "/e2e/image/errors";

    [TestMethod]
    public async Task Prerender_TheImageIsLeftToTheBrowser_AndTakenOverWithoutAFlash()
    {
        var html = await (await Page.APIRequest.GetAsync($"{BaseUrl}{Path}")).TextAsync();

        // Prerendered, the image is shown as the browser draws it, before any circuit exists. Its src comes last,
        // after everything that says how it is fetched (see BitImage.GetImageAttributes).
        StringAssert.Matches(html, new Regex("id=\"img-basic\" class=\"bit-img\"><img alt=\"A landscape picture\" title=\"Hover title\" class=\"[^\"]*bit-img-vis bit-img-nat[^\"]*\" src=\"samples/image-landscape.svg\""));
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");

        await OpenAsync(Path);

        var img = Page.Locator("#img-basic img");
        await Expect(img).ToBeVisibleAsync();
        await Expect(img).ToHaveClassAsync(HasClass("bit-img-vis"));
        await Expect(img).Not.ToHaveClassAsync(HasClass("bit-img-nat"));
        await Expect(Page.Locator("#basic-states")).ToHaveTextAsync(">Loaded");
    }

    [TestMethod]
    public async Task Basic_SrcAltAndTitle_LandOnTheImage_InsideItsFrame()
    {
        await OpenAsync(Path);

        var frame = Page.Locator("#img-basic");
        var img = frame.Locator("> img.bit-img-img");

        await Expect(frame).ToHaveClassAsync(HasClass("bit-img"));
        await Expect(img).ToHaveAttributeAsync("src", "samples/image-landscape.svg");
        await Expect(img).ToHaveAttributeAsync("alt", "A landscape picture");
        await Expect(img).ToHaveAttributeAsync("title", "Hover title");
        await Expect(Page.GetByRole(AriaRole.Img, new() { Name = "A landscape picture" })).ToHaveCountAsync(1);

        Assert.AreEqual(200, await img.EvaluateAsync<int>("el => el.naturalWidth"));
        Assert.AreEqual(200, Math.Round((await img.BoundingBoxAsync())!.Width), "Drawn at its natural size.");
    }

    [TestMethod]
    public async Task Disabled_DimsTheImage_AndTakesThePointerAway()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#img-disabled")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Page.Locator("#img-disabled img")).ToHaveCSSAsync("opacity", await ResolveAsync("opacity", "var(--bit-opa-dis)"));
        await Expect(Page.Locator("#img-disabled img")).ToHaveCSSAsync("pointer-events", "none");
    }

    [TestMethod]
    public async Task FrameSize_WidthHeightAndAspectRatio_SizeTheFrame_AndOneOfThemScalesTheImage()
    {
        await OpenAsync(Path);

        // Width alone in rem: the image follows it, keeping its shape.
        Assert.AreEqual((144, 72), await SizeAsync("#img-width-rem"));
        Assert.AreEqual((144, 72), await SizeAsync("#img-width-rem img"));
        await Expect(Page.Locator("#img-width-rem img")).ToHaveClassAsync(HasClass("bit-img-ihw"));

        // A bare number is pixels; Height alone scales the width.
        await Expect(Page.Locator("#img-height-number")).ToHaveAttributeAsync("style", "height:80px");
        Assert.AreEqual((160, 80), await SizeAsync("#img-height-number img"));
        await Expect(Page.Locator("#img-height-number img")).ToHaveClassAsync(HasClass("bit-img-ihh"));

        // Both: the frame is that size and the image keeps its own.
        Assert.AreEqual((256, 128), await SizeAsync("#img-width-height"));
        Assert.AreEqual((200, 100), await SizeAsync("#img-width-height img"));

        // AspectRatio reserves the frame's room.
        await Expect(Page.Locator("#img-aspect")).ToHaveCSSAsync("aspect-ratio", "1 / 1");
        Assert.AreEqual((160, 160), await SizeAsync("#img-aspect"));
    }

    [TestMethod]
    public async Task FrameSize_AspectRatio_HoldsTheRoomWhileTheImageIsStillLoading()
    {
        var gate = await HoldAsync("aspect");
        await OpenHeldAsync(Path);

        await Expect(Page.Locator("#img-aspect img")).ToBeHiddenAsync();
        Assert.AreEqual((160, 160), await SizeAsync("#img-aspect"));

        gate.SetResult();
        await Expect(Page.Locator("#img-aspect img")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Fluid_KeepsTheFrameAndTheImageWithinTheContainer()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#img-fluid")).ToHaveClassAsync(HasClass("bit-img-flu"));
        await Expect(Page.Locator("#img-fluid img")).ToHaveClassAsync(HasClass("bit-img-fli"));
        Assert.AreEqual((100, 50), await SizeAsync("#img-fluid img"), "The 200px picture is scaled down to its 100px column.");
    }

    [TestMethod]
    [DataRow("None", "bit-img-non", "none", 64, 64)]
    [DataRow("Center", "bit-img-ctr", "fill", 64, 64)]
    [DataRow("CenterContain", "bit-img-cct", "fill", 64, 64)]
    [DataRow("CenterCover", "bit-img-ccv", "fill", 64, 64)]
    [DataRow("Contain", "bit-img-cnt", "contain", 160, 96)]
    [DataRow("Cover", "bit-img-cvr", "cover", 160, 96)]
    [DataRow("Fill", "bit-img-fil", "fill", 160, 96)]
    [DataRow("ScaleDown", "bit-img-scd", "scale-down", 160, 96)]
    public async Task ImageFit_EveryValue_SettlesTheImageInItsFrame(string fit, string className, string objectFit, int width, int height)
    {
        await OpenAsync(Path);

        var frame = Page.Locator($"#img-fit-{fit}");
        var img = frame.Locator("img");

        await Expect(img).ToHaveClassAsync(HasClass(className));
        await Expect(img).ToHaveClassAsync(HasClass("bit-img-lan"));
        await Expect(img).ToHaveCSSAsync("object-fit", objectFit);
        Assert.AreEqual((width, height), await SizeAsync($"#img-fit-{fit} img"));

        // The three centered fits center the natural-size image in the frame.
        if (fit.StartsWith("Center"))
        {
            await Expect(frame).ToHaveClassAsync(HasClass("bit-img-cen"));
            var frameBox = await frame.BoundingBoxAsync();
            var imgBox = await img.BoundingBoxAsync();
            Assert.AreEqual(frameBox!.X + frameBox.Width / 2, imgBox!.X + imgBox.Width / 2, 1);
            Assert.AreEqual(frameBox.Y + frameBox.Height / 2, imgBox.Y + imgBox.Height / 2, 1);
        }
    }

    [TestMethod]
    public async Task ImageFit_CenterCoverAndCenterContain_FollowCover_AndNeverEnlarge()
    {
        await OpenAsync(Path);

        // A portrait Cover keeps the width within the frame, a landscape one the height; the image is not enlarged.
        await Expect(Page.Locator("#img-fit-ccv-portrait img")).ToHaveClassAsync(HasClass("bit-img-por"));
        await Expect(Page.Locator("#img-fit-ccv-portrait img")).ToHaveCSSAsync("max-width", "100%");
        Assert.AreEqual((200, 100), await SizeAsync("#img-fit-ccv-portrait img"));

        await Expect(Page.Locator("#img-fit-ccv-landscape img")).ToHaveCSSAsync("max-height", "100%");
        Assert.AreEqual((200, 100), await SizeAsync("#img-fit-ccv-landscape img"));

        // A bigger image is scaled down to fit inside.
        await Expect(Page.Locator("#img-fit-cct-big img")).ToHaveCSSAsync("max-width", "100%");
        Assert.AreEqual((100, 25), await SizeAsync("#img-fit-cct-big img"));
    }

    [TestMethod]
    public async Task ImagePosition_MovesTheCroppedPart()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#img-position img")).ToHaveCSSAsync("object-position", "0% 0%");
        await Expect(Page.Locator("#img-position-default img")).ToHaveCSSAsync("object-position", "50% 50%");
    }

    [TestMethod]
    public async Task MaximizeFrame_FillsTheParentAndCoversIt_AndAnImageFitStillWins()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#img-max")).ToHaveClassAsync(HasClass("bit-img-max"));
        Assert.AreEqual((300, 150), await SizeAsync("#img-max"));
        Assert.AreEqual((300, 150), await SizeAsync("#img-max img"));
        await Expect(Page.Locator("#img-max img")).ToHaveCSSAsync("object-fit", "cover");
        await Expect(Page.Locator("#img-max-contain img")).ToHaveCSSAsync("object-fit", "contain");
    }

    [TestMethod]
    public async Task Shape_RoundedCircularBorderedAndShadow()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#img-rounded")).ToHaveCSSAsync("border-top-left-radius", await ResolveAsync("border-top-left-radius", "var(--bit-shp-radius-surface)"));
        await Expect(Page.Locator("#img-rounded")).ToHaveCSSAsync("overflow", "hidden");

        await Expect(Page.Locator("#img-circular")).ToHaveClassAsync(HasClass("bit-img-cir"));
        Assert.IsTrue(Px(await ComputedStyleAsync("#img-circular", "border-top-left-radius")) >= 64);

        // Circular wins over Rounded.
        await Expect(Page.Locator("#img-circular-rounded")).Not.ToHaveClassAsync(HasClass("bit-img-rnd"));

        await Expect(Page.Locator("#img-bordered")).ToHaveCSSAsync("border-top-width", await ResolveAsync("border-top-width", "var(--bit-shp-brd-width)"));
        await Expect(Page.Locator("#img-bordered")).ToHaveCSSAsync("border-top-color", await ResolveAsync("color", "var(--bit-clr-brd-sec)"));

        await Expect(Page.Locator("#img-shadow")).ToHaveCSSAsync("box-shadow", await ResolveAsync("box-shadow", "var(--bit-shd-card)"));
    }

    [TestMethod]
    public async Task LoadingStates_TheLoadingTemplateStandsInUntilTheImageArrives_AndEveryStateIsReported()
    {
        var gate = await HoldAsync("template");
        await OpenHeldAsync(Path);

        var img = Page.Locator("#img-slow img");
        await Expect(Page.Locator("#loading-template")).ToBeVisibleAsync();
        await Expect(img).ToBeHiddenAsync();
        await Expect(img).ToHaveClassAsync(HasClass("bit-img-hid"));
        await Expect(Page.Locator("#error-template-unused")).ToHaveCountAsync(0);

        await Page.Locator("#btn-read-state").ClickAsync();
        await Expect(Page.Locator("#slow-state-property")).ToHaveTextAsync("Loading");

        gate.SetResult();

        await Expect(img).ToBeVisibleAsync();
        await Expect(Page.Locator("#loading-template")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#slow-loads")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#slow-states")).ToHaveTextAsync(">Loaded");

        await Page.Locator("#btn-read-state").ClickAsync();
        await Expect(Page.Locator("#slow-state-property")).ToHaveTextAsync("Loaded");
    }

    [TestMethod]
    public async Task ReloadAsync_FetchesTheImageAgain()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#slow-loads")).ToHaveTextAsync("1");

        var gate = await HoldAsync("template");
        await Page.Locator("#btn-reload").ClickAsync();

        await Expect(Page.Locator("#slow-states")).ToHaveTextAsync(">Loaded>Loading");
        await Expect(Page.Locator("#loading-template")).ToBeVisibleAsync();

        gate.SetResult();
        await Expect(Page.Locator("#slow-states")).ToHaveTextAsync(">Loaded>Loading>Loaded");
        await Expect(Page.Locator("#slow-loads")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task FadeIn_AnimatesTheMomentTheImageAppears()
    {
        var gate = await HoldAsync("fade");
        await OpenHeldAsync(Path);

        var img = Page.Locator("#img-fade img");
        await Expect(img).ToBeHiddenAsync();

        gate.SetResult();

        await Expect(img).ToHaveClassAsync(HasClass("bit-img-fde"));
        await Expect(img).ToHaveCSSAsync("animation-name", "bit-img-fade-in");
        await Expect(img).ToHaveCSSAsync("animation-duration", await ResolveAsync("transition-duration", "var(--bit-mot-duration-long)"));
        await Expect(img).ToHaveCSSAsync("opacity", "1");
    }

    [TestMethod]
    public async Task StartVisible_ShowsTheImageWhileItIsStillLoading()
    {
        var gate = await HoldAsync("start");
        await OpenHeldAsync(Path);

        var img = Page.Locator("#img-start-visible img");
        await Expect(img).ToHaveClassAsync(HasClass("bit-img-vis"));
        await Expect(img).ToHaveCSSAsync("display", "block");

        // The hidden-image alt is not needed: the image itself is on screen.
        await Expect(Page.Locator("#img-start-visible .bit-img-alt")).ToHaveCountAsync(0);

        gate.SetResult();
        await Page.WaitForFunctionAsync("() => document.querySelector('#img-start-visible img').complete");
        await Expect(img).ToHaveClassAsync(HasClass("bit-img-vis"));
    }

    [TestMethod]
    public async Task PlaceholderSrc_FillsTheFrameWhileLoading_AndCrossFadesWithTheImage()
    {
        var gate = await HoldAsync("placeholder");
        var noFade = await HoldAsync("nofade");
        await OpenHeldAsync(Path);

        var placeholder = Page.Locator("#img-placeholder img.bit-img-plc");
        await Expect(placeholder).ToBeVisibleAsync();
        await Expect(placeholder).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(placeholder).ToHaveAttributeAsync("alt", "");
        StringAssert.StartsWith(await placeholder.EvaluateAsync<string>("el => getComputedStyle(el).filter"), "blur(");
        // It fills the frame, scaled up a little so the blur leaves no soft edge inside it.
        Assert.AreEqual(120, await placeholder.EvaluateAsync<int>("el => el.offsetWidth"));
        Assert.AreEqual(60, await placeholder.EvaluateAsync<int>("el => el.offsetHeight"));
        await Expect(placeholder).ToHaveCSSAsync("transform", "matrix(1.1, 0, 0, 1.1, 0, 0)");

        gate.SetResult();

        // Under FadeIn the placeholder fades out as the image fades in, and goes once its fade has ended.
        await Expect(Page.Locator("#img-placeholder img.bit-img-img")).ToBeVisibleAsync();
        await Expect(Page.Locator("#img-placeholder img.bit-img-plc")).ToHaveCountAsync(0);

        // Without it, the placeholder goes as the image arrives.
        await Expect(Page.Locator("#img-placeholder-nofade img.bit-img-plc")).ToBeVisibleAsync();
        noFade.SetResult();
        await Expect(Page.Locator("#img-placeholder-nofade img.bit-img-plc")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#img-placeholder-nofade img.bit-img-img")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task PlaceholderSrc_TheLeavingPlaceholder_RunsTheFadeOut()
    {
        var gate = await HoldAsync("placeholder");
        await OpenHeldAsync(Path);

        // A long fade, so the leaving placeholder is there to be read.
        await Page.Locator("#img-placeholder").EvaluateAsync("el => el.style.setProperty('--bit-Image-fade-duration', '3s')");

        gate.SetResult();

        var leaving = Page.Locator("#img-placeholder img.bit-img-plc.bit-img-pfo");
        await Expect(leaving).ToHaveCountAsync(1);
        await Expect(leaving).ToHaveCSSAsync("animation-name", "bit-img-fade-out");
        await Expect(leaving).ToHaveCSSAsync("animation-duration", "3s");
        await Expect(Page.Locator("#img-placeholder img.bit-img-plc")).ToHaveCountAsync(0, new() { Timeout = 10000 });
    }

    [TestMethod]
    public async Task ReducedMotion_CollapsesTheFade_AndForceAnimationRestoresIt()
    {
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        var fade = await HoldAsync("fade");
        var force = await HoldAsync("force");
        await OpenHeldAsync(Path);

        fade.SetResult();
        force.SetResult();

        await Expect(Page.Locator("#img-fade img")).ToHaveClassAsync(HasClass("bit-img-fde"));
        Assert.IsTrue(Seconds(await ComputedStyleAsync("#img-fade img", "animation-duration")) < 0.001, "Under reduced motion the fade is instant.");

        await Expect(Page.Locator("#img-fade-force")).ToHaveClassAsync(HasClass("bit-fam"));
        Assert.IsTrue(Seconds(await ComputedStyleAsync("#img-fade-force img", "animation-duration")) > 0.05, "ForceAnimation restores the fade.");
    }

    [TestMethod]
    public async Task NativeAttributes_AreWrittenOnTheImg()
    {
        await OpenAsync(Path);

        var img = Page.Locator("#img-native img");
        await Expect(img).ToHaveAttributeAsync("loading", "eager");
        await Expect(img).ToHaveAttributeAsync("decoding", "async");
        await Expect(img).ToHaveAttributeAsync("fetchpriority", "high");
        await Expect(img).ToHaveAttributeAsync("crossorigin", "anonymous");
        await Expect(img).ToHaveAttributeAsync("referrerpolicy", "no-referrer");
        await Expect(img).ToHaveAttributeAsync("draggable", "false");

        // ImageAttributes reach the img, its class and style merged with the component's.
        await Expect(img).ToHaveAttributeAsync("data-e2e", "image");
        await Expect(img).ToHaveClassAsync(HasClass("splat-image-class"));
        await Expect(img).ToHaveClassAsync(HasClass("bit-img-img"));
        await Expect(img).ToHaveCSSAsync("outline-color", "rgb(1, 2, 3)");

        foreach (var (id, value) in new[] { ("Auto", "auto"), ("Sync", "sync"), ("Async", "async") })
        {
            await Expect(Page.Locator($"#img-decoding-{id} img")).ToHaveAttributeAsync("decoding", value);
        }

        foreach (var (id, value) in new[] { ("Auto", "auto"), ("High", "high"), ("Low", "low") })
        {
            await Expect(Page.Locator($"#img-priority-{id} img")).ToHaveAttributeAsync("fetchpriority", value);
        }

        await Expect(Page.Locator("#img-cors-credentials img")).ToHaveAttributeAsync("crossorigin", "use-credentials");
        await Expect(Page.Locator("#img-draggable img")).ToHaveAttributeAsync("draggable", "true");

        var policies = new[]
        {
            ("NoReferrer", "no-referrer"), ("NoReferrerWhenDowngrade", "no-referrer-when-downgrade"), ("Origin", "origin"),
            ("OriginWhenCrossOrigin", "origin-when-cross-origin"), ("SameOrigin", "same-origin"), ("StrictOrigin", "strict-origin"),
            ("StrictOriginWhenCrossOrigin", "strict-origin-when-cross-origin"), ("UnsafeUrl", "unsafe-url")
        };
        foreach (var (id, value) in policies)
        {
            await Expect(Page.Locator($"#img-referrer-{id} img")).ToHaveAttributeAsync("referrerpolicy", value);
            Assert.AreEqual(value, await Page.Locator($"#img-referrer-{id} img").EvaluateAsync<string>("el => el.referrerPolicy"));
        }

        // A default left unset writes nothing.
        await Expect(Page.Locator("#img-basic img")).Not.ToHaveAttributeAsync("loading", new Regex(".*"));
        await Expect(Page.Locator("#img-basic img")).Not.ToHaveAttributeAsync("draggable", new Regex(".*"));
    }

    [TestMethod]
    public async Task Responsive_SrcsetAndSizes_LetTheBrowserPickACandidate_AndSourcesAreWalkedInOrder()
    {
        await OpenAsync(Path);

        var srcset = Page.Locator("#img-srcset-small img");
        await Expect(srcset).ToHaveAttributeAsync("srcset", "samples/image-landscape.svg 200w, samples/image-wide.svg 400w");
        await Expect(srcset).ToHaveAttributeAsync("sizes", "100px");
        StringAssert.Matches(await srcset.EvaluateAsync<string>("el => el.currentSrc"), new Regex("image-(landscape|wide)\\.svg$"), "A srcset candidate, not the Src fallback.");
        StringAssert.EndsWith(await Page.Locator("#img-srcset-large img").EvaluateAsync<string>("el => el.currentSrc"), "image-wide.svg", "Laid out at 380px, the 400w candidate.");

        var picture = Page.Locator("#img-sources picture.bit-img-pic");
        await Expect(picture).ToHaveCSSAsync("display", "contents");
        await Expect(picture.Locator("source")).ToHaveCountAsync(3);
        await Expect(picture.Locator("source").Nth(1)).ToHaveAttributeAsync("media", "(min-width: 1px)");
        await Expect(picture.Locator("source").Nth(1)).ToHaveAttributeAsync("width", "100");
        await Expect(picture.Locator("source").Nth(0)).ToHaveAttributeAsync("type", "image/x-e2e-unsupported");

        // The unsupported type is skipped and the first matching media wins.
        StringAssert.EndsWith(await Page.Locator("#img-sources img").EvaluateAsync<string>("el => el.currentSrc"), "image-portrait.svg");

        // No source matches (and a source without a srcset is not rendered): the Src is used.
        await Expect(Page.Locator("#img-sources-nomatch source")).ToHaveCountAsync(1);
        StringAssert.EndsWith(await Page.Locator("#img-sources-nomatch img").EvaluateAsync<string>("el => el.currentSrc"), "image-square.svg");
    }

    [TestMethod]
    public async Task OnClick_MakesTheImageAButton_ThatTheMouseAndTheKeyboardActivate()
    {
        await OpenAsync(Path);

        var frame = Page.Locator("#img-click");
        var img = frame.Locator("img");

        await Expect(frame).ToHaveClassAsync(HasClass("bit-img-clk"));
        await Expect(img).ToHaveAttributeAsync("role", "button");
        await Expect(img).ToHaveAttributeAsync("tabindex", "0");
        await Expect(img).ToHaveCSSAsync("cursor", "pointer");
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Open the preview" })).ToHaveCountAsync(1);

        await img.ClickAsync();
        await Expect(Page.Locator("#click-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#click-args")).ToHaveTextAsync("detail=1;type=click");

        await img.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#click-count")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#click-args")).ToHaveTextAsync("detail=0;type=click");

        var scrollBefore = await Page.EvaluateAsync<double>("() => window.scrollY");
        await Page.Keyboard.PressAsync("Space");
        await Expect(Page.Locator("#click-count")).ToHaveTextAsync("3");
        Assert.AreEqual(scrollBefore, await Page.EvaluateAsync<double>("() => window.scrollY"), "Space did not scroll the page.");

        // AriaLabel names the action while the alt describes the picture.
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Open the gallery" })).ToHaveCountAsync(1);
        await Expect(Page.Locator("#img-click-named img")).ToHaveAttributeAsync("alt", "A landscape");
    }

    [TestMethod]
    public async Task OnClick_TheKeyboardFocusDrawsARingOnTheFrame_AndTheDisabledOneIsSkipped()
    {
        await OpenAsync(Path);

        await Page.Locator("#before-clickable").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#img-click img")).ToBeFocusedAsync();
        await Expect(Page.Locator("#img-click")).ToHaveCSSAsync("outline-style", "solid");
        await Expect(Page.Locator("#img-click")).ToHaveCSSAsync("outline-color", await ResolveAsync("color", "var(--bit-clr-pri-focus)"));

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#img-click-named img")).ToBeFocusedAsync();

        var disabled = Page.Locator("#img-click-disabled img");
        await Expect(disabled).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(disabled).Not.ToHaveAttributeAsync("tabindex", new Regex(".*"));
        await Expect(Page.Locator("#img-click-disabled")).Not.ToHaveClassAsync(HasClass("bit-img-clk"));

        await disabled.DispatchEventAsync("click");
        await disabled.DispatchEventAsync("keydown", new Dictionary<string, object> { ["key"] = "Enter" });
        await Expect(Page.Locator("#disabled-clicks")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task OnClick_ThePointerTintsTheImage()
    {
        await OpenAsync(Path);

        Assert.AreEqual("rgba(0, 0, 0, 0)", await PseudoAsync("#img-click", "::after", "background-color"));

        await Page.Locator("#img-click img").HoverAsync();
        await Page.WaitForFunctionAsync("() => getComputedStyle(document.getElementById('img-click'), '::after').backgroundColor !== 'rgba(0, 0, 0, 0)'");

        await Page.Mouse.DownAsync();
        var pressed = await PseudoAsync("#img-click", "::after", "background-color");
        await Page.Mouse.UpAsync();
        Assert.AreNotEqual("rgba(0, 0, 0, 0)", pressed);
    }

    [TestMethod]
    public async Task ChildContent_IsLaidOverTheImage_AndLetsClicksThroughToIt()
    {
        await OpenAsync(Path);

        var overlay = Page.Locator("#img-content > .bit-img-ovl");
        await Expect(overlay).ToHaveCSSAsync("position", "absolute");
        await Expect(overlay).ToHaveCSSAsync("pointer-events", "none");
        Assert.AreEqual((200, 100), await SizeAsync("#img-content > .bit-img-ovl"));

        // A click on the overlay's empty part reaches the image under it.
        await overlay.ClickAsync(new() { Position = new() { X = 150, Y = 20 }, Force = true });
        await Expect(Page.Locator("#content-image-clicks")).ToHaveTextAsync("1");

        // Its own content takes its clicks.
        await Page.Locator("#caption-button").ClickAsync();
        await Expect(Page.Locator("#caption-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#content-image-clicks")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Accessibility_AnImageWithoutAltIsDecorative_AndAHiddenOnesAltIsStillAnnounced()
    {
        var gate = await HoldAsync("alt");
        await OpenHeldAsync(Path);

        await Expect(Page.Locator("#img-decorative img")).ToHaveAttributeAsync("alt", "");
        await Expect(Page.Locator("#img-image-attr-alt img")).ToHaveAttributeAsync("alt", "Alt from attributes");

        // While loading the image is hidden, and its alt is announced in its place.
        var alt = Page.Locator("#img-hidden-alt > span.bit-img-alt");
        await Expect(alt).ToHaveAttributeAsync("role", "img");
        await Expect(alt).ToHaveAttributeAsync("aria-label", "Monthly sales chart");
        await Expect(Page.GetByRole(AriaRole.Img, new() { Name = "Monthly sales chart" })).ToHaveCountAsync(1);

        gate.SetResult();
        await Expect(alt).ToHaveCountAsync(0);
        await Expect(Page.GetByRole(AriaRole.Img, new() { Name = "Monthly sales chart" })).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task FocusAsync_FocusesTheImage()
    {
        await OpenAsync(Path);

        await Page.Locator("#btn-focus-image").ClickAsync();

        await Expect(Page.Locator("#img-focusable img")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Lazy_AnOffScreenImage_IsFetchedOnlyOnceItIsScrolledNear()
    {
        var requested = false;
        Page.Request += (_, request) =>
        {
            if (request.Url.Contains("case=lazy-late")) requested = true;
        };

        await OpenAsync(Path);
        await Page.Locator("#btn-show-late-lazy").ClickAsync();

        var img = Page.Locator("#img-lazy-late img");
        await Expect(img).ToHaveAttributeAsync("loading", "lazy");

        // Hidden while waiting, but laid out, so the browser can tell when it comes near.
        await Expect(img).ToHaveClassAsync(HasClass("bit-img-lzy"));
        await Expect(img).ToHaveCSSAsync("display", "block");
        await Expect(img).ToHaveCSSAsync("visibility", "hidden");

        for (var i = 0; i < 5; i++) await NextFramesAsync();
        Assert.IsFalse(requested, "Not fetched while far away.");
        await Expect(Page.Locator("#late-lazy-state")).ToHaveTextAsync("");

        await Page.Locator("#lazy-scroller").EvaluateAsync("el => el.scrollTop = el.scrollHeight");

        await Expect(Page.Locator("#late-lazy-state")).ToHaveTextAsync("Loaded");
        Assert.IsTrue(requested);
        await Expect(img).ToHaveCSSAsync("visibility", "visible");
    }

    [TestMethod]
    public async Task Lazy_APrerenderedOffScreenImage_StaysDeferredAfterTheCircuitTakesOver()
    {
        var requested = false;
        Page.Request += (_, request) =>
        {
            if (request.Url.Contains("case=lazy") && request.Url.Contains("case=lazy-late") is false) requested = true;
        };

        await OpenAsync(Path);

        // The scroller is brought on screen, so what keeps the image away is its offset inside the scroller alone.
        await Page.Locator("#lazy-scroller").ScrollIntoViewIfNeededAsync();
        for (var i = 0; i < 5; i++) await NextFramesAsync();

        Assert.IsFalse(requested, "Not fetched while far away.");
        await Expect(Page.Locator("#lazy-state")).ToHaveTextAsync("");

        await Page.Locator("#lazy-scroller").EvaluateAsync("el => el.scrollTop = el.scrollHeight");
        await Expect(Page.Locator("#lazy-state")).ToHaveTextAsync("Loaded");
    }

    [TestMethod]
    public async Task BitParams_CascadeTheirDefaults_AndAnImagesOwnValueWins()
    {
        await OpenAsync(Path);

        var first = Page.Locator("#img-params-1");
        await Expect(first).ToHaveClassAsync(HasClass("bit-img-rnd"));
        await Expect(first).ToHaveClassAsync(HasClass("bit-img-shd"));
        Assert.AreEqual((128, 128), await SizeAsync("#img-params-1"));
        await Expect(first.Locator("img")).ToHaveCSSAsync("object-fit", "cover");

        await Expect(Page.Locator("#img-params-2")).Not.ToHaveClassAsync(HasClass("bit-img-rnd"));
        await Expect(Page.Locator("#img-params-2")).ToHaveClassAsync(HasClass("bit-img-shd"));
    }

    [TestMethod]
    public async Task StylesAndClasses_ReachEveryPart()
    {
        var gate = await HoldAsync("styles");
        await OpenHeldAsync(Path);

        await Expect(Page.Locator("#img-style")).ToHaveCSSAsync("margin-top", "7px");
        await Expect(Page.Locator("#img-style")).ToHaveClassAsync(HasClass("e2e-image-class"));

        await Expect(Page.Locator("#img-styles")).ToHaveCSSAsync("padding-top", "3px");
        await Expect(Page.Locator("#img-styles")).ToHaveClassAsync(HasClass("e2e-root"));
        await Expect(Page.Locator("#img-styles img")).ToHaveCSSAsync("filter", "grayscale(1)");
        await Expect(Page.Locator("#img-styles img")).ToHaveClassAsync(HasClass("e2e-image"));
        await Expect(Page.Locator("#img-styles .bit-img-ovl")).ToHaveCSSAsync("color", "rgb(1, 2, 3)");
        await Expect(Page.Locator("#img-styles .bit-img-ovl")).ToHaveClassAsync(HasClass("e2e-content"));

        await Expect(Page.Locator("#img-styles-placeholder .bit-img-plc")).ToHaveCSSAsync("opacity", "0.5");
        await Expect(Page.Locator("#img-styles-placeholder .bit-img-plc")).ToHaveClassAsync(HasClass("e2e-placeholder"));
        await Expect(Page.Locator("#img-styles-placeholder .bit-img-tpl")).ToHaveCSSAsync("color", "rgb(4, 5, 6)");
        await Expect(Page.Locator("#img-styles-placeholder .bit-img-tpl")).ToHaveClassAsync(HasClass("e2e-loading"));

        gate.SetResult();
    }

    [TestMethod]
    public async Task CssVariables_RestyleTheFrame_TheStates_ThePlaceholderAndTheFade()
    {
        var blur = await HoldAsync("blur");
        var timing = await HoldAsync("timing");
        await OpenHeldAsync(Path);

        var frame = Page.Locator("#img-css");
        await Expect(frame).ToHaveCSSAsync("background-color", "rgb(10, 20, 30)");
        await Expect(frame).ToHaveCSSAsync("border-top-left-radius", "11px");
        await Expect(frame).ToHaveCSSAsync("border-top-width", "4px");
        await Expect(frame).ToHaveCSSAsync("border-top-color", "rgb(200, 0, 0)");
        await Expect(frame).ToHaveCSSAsync("box-shadow", "rgb(0, 0, 255) 2px 2px 0px 0px");

        await Expect(Page.Locator("#img-css-placeholder .bit-img-plc")).ToHaveCSSAsync("filter", "blur(3px)");

        await frame.Locator("img").HoverAsync();
        await Expect(frame).ToHaveCSSAsync("box-shadow", "rgb(0, 255, 0) 3px 3px 0px 0px");
        await Page.WaitForFunctionAsync("() => getComputedStyle(document.getElementById('img-css'), '::after').backgroundColor === 'rgba(255, 0, 0, 0.5)'");

        await Page.Mouse.DownAsync();
        await Page.WaitForFunctionAsync("() => getComputedStyle(document.getElementById('img-css'), '::after').backgroundColor === 'rgba(0, 0, 255, 0.5)'");
        await Page.Mouse.UpAsync();

        await Page.Mouse.MoveAsync(0, 0);
        await frame.Locator("img").FocusAsync();
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Page.Keyboard.PressAsync("Tab");
        await Expect(frame).ToHaveCSSAsync("outline-color", "rgb(0, 150, 0)");

        timing.SetResult();
        await Expect(Page.Locator("#img-css-fade img")).ToHaveCSSAsync("animation-duration", "0.9s");
        await Expect(Page.Locator("#img-css-fade img")).ToHaveCSSAsync("animation-timing-function", "linear");
        blur.SetResult();
    }

    [TestMethod]
    public async Task Visibility_HiddenKeepsTheRoom_CollapsedRemovesIt()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#img-hidden")).ToHaveCSSAsync("visibility", "hidden");
        Assert.IsTrue((await Page.Locator("#img-hidden").BoundingBoxAsync())!.Height > 0);
        await Expect(Page.Locator("#img-collapsed")).ToHaveCSSAsync("display", "none");
    }

    [TestMethod]
    public async Task Rtl_WritesTheDirectionOnTheFrame()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#img-rtl")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Page.Locator("#img-rtl")).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(Page.Locator("#img-rtl")).ToHaveCSSAsync("direction", "rtl");
    }

    [TestMethod]
    public async Task Runtime_ANewSrc_LoadsAgain_AndDisabledFollows()
    {
        await OpenAsync(Path);

        await Expect(Page.Locator("#runtime-states")).ToHaveTextAsync(">Loaded");

        await Page.Locator("#btn-set-src").ClickAsync();
        await Expect(Page.Locator("#runtime-states")).ToHaveTextAsync(">Loaded>Loading>Loaded");
        await Expect(Page.Locator("#img-runtime img")).ToHaveAttributeAsync("src", "samples/image-portrait.svg");
        Assert.AreEqual(100, await Page.Locator("#img-runtime img").EvaluateAsync<int>("el => el.naturalWidth"));

        await Page.Locator("#btn-toggle-disabled").ClickAsync();
        await Expect(Page.Locator("#img-runtime")).ToHaveClassAsync(HasClass("bit-dis"));
        await Page.Locator("#btn-toggle-disabled").ClickAsync();
        await Expect(Page.Locator("#img-runtime")).Not.ToHaveClassAsync(HasClass("bit-dis"));
    }

    [TestMethod]
    public async Task Unmount_RemovesAClickableImage_WithoutAnError()
    {
        await OpenAsync(Path);

        await Page.Locator("#btn-toggle-mounted").ClickAsync();
        await Expect(Page.Locator("#mounted")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#img-mounted")).ToHaveCountAsync(0);

        await Page.Locator("#btn-toggle-mounted").ClickAsync();
        await Expect(Page.Locator("#img-mounted img")).ToBeVisibleAsync();

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    [TestMethod]
    public async Task Error_A404_ShowsTheErrorTemplate_AndFiresOnErrorOnce()
    {
        AllowConsoleErrors = true; // the 404 the browser logs is the subject

        await OpenAsync(ErrorsPath);
        await Page.Locator("#btn-show").ClickAsync();

        await Expect(Page.Locator("#error-template")).ToHaveTextAsync("Could not load the picture");
        await Expect(Page.Locator("#img-error img")).ToBeHiddenAsync();
        await Expect(Page.Locator("#errors")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#states")).ToHaveTextAsync(">Error");

        Assert.IsTrue(ConsoleErrors.Any(e => e.Contains("404")), "The browser reported the 404.");
    }

    [TestMethod]
    public async Task Error_FallbackSrc_IsTriedOnceWhenTheSrcFails_AndShownAtOnceWithoutASrc()
    {
        AllowConsoleErrors = true; // the 404 the browser logs is the subject

        await OpenAsync(ErrorsPath);
        await Page.Locator("#btn-show").ClickAsync();

        var img = Page.Locator("#img-fallback img");
        await Expect(img).ToHaveAttributeAsync("src", "samples/image-square.svg");
        await Expect(img).ToBeVisibleAsync();
        await Expect(Page.Locator("#fallback-errors")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#fallback-loads")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#fallback-states")).ToHaveTextAsync(">Loaded");

        await Expect(Page.Locator("#img-fallback-only img")).ToHaveAttributeAsync("src", "samples/image-square.svg");
        await Expect(Page.Locator("#img-fallback-only img")).ToBeVisibleAsync();

        // A cascaded fallback works the same.
        await Expect(Page.Locator("#img-params-fallback img")).ToHaveAttributeAsync("src", "samples/image-square.svg");
        await Expect(Page.Locator("#img-params-fallback img")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Error_AFallbackThatFailsToo_EndsInTheErrorState_WithoutRetrying()
    {
        AllowConsoleErrors = true; // the 404s the browser logs are the subject

        await OpenAsync(ErrorsPath);
        await Page.Locator("#btn-show").ClickAsync();

        await Expect(Page.Locator("#broken-states")).ToHaveTextAsync(">Error");
        await Expect(Page.Locator("#broken-errors")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#img-fallback-broken img")).ToHaveAttributeAsync("src", "samples/no-such-fallback.png");

        for (var i = 0; i < 5; i++) await NextFramesAsync();
        await Expect(Page.Locator("#broken-errors")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task Error_AFailedImage_IsHidden_AndItsAltIsStillAnnounced()
    {
        AllowConsoleErrors = true; // the 404s the browser logs are the subject

        await OpenAsync(ErrorsPath);
        await Page.Locator("#btn-show").ClickAsync();

        await Expect(Page.Locator("#img-failed-alt img")).ToBeHiddenAsync();
        await Expect(Page.GetByRole(AriaRole.Img, new() { Name = "Monthly sales chart" })).ToHaveAttributeAsync("class", "bit-img-alt");

        // StartVisible shows an image while it loads, and hides it once it fails.
        await Expect(Page.Locator("#img-start-visible-failed img")).ToHaveClassAsync(HasClass("bit-img-hid"));
        await Expect(Page.Locator("#img-start-visible-failed img")).ToBeHiddenAsync();
    }

    // OpenAsync waits for the load event, which a held-back image holds back too, so a page with one is waited for by
    // its status alone.
    private async Task OpenHeldAsync(string path)
    {
        await Page.GotoAsync($"{BaseUrl}{path}", new() { WaitUntil = WaitUntilState.Commit });
        await WaitForStatusAsync("Ready");
    }

    // Holds back the response of one of the page's slow images until the returned gate is opened.
    private async Task<TaskCompletionSource> HoldAsync(string slowCase)
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await Page.RouteAsync(new Regex($@"image-slow\.svg\?case={slowCase}$"), async route =>
        {
            await gate.Task;
            try
            {
                await route.ContinueAsync();
            }
            catch (PlaywrightException)
            {
                // the page went away while the response was held back
            }
        });

        return gate;
    }

    private async Task<(int Width, int Height)> SizeAsync(string selector)
    {
        var box = await Page.Locator(selector).First.BoundingBoxAsync();

        return ((int)Math.Round(box!.Width), (int)Math.Round(box.Height));
    }

    private Task<string> PseudoAsync(string selector, string pseudo, string property)
    {
        return Page.Locator(selector).EvaluateAsync<string>("(el, [pseudo, property]) => getComputedStyle(el, pseudo).getPropertyValue(property)", new[] { pseudo, property });
    }

    private static double Px(string value) => double.Parse(value.Replace("px", ""), CultureInfo.InvariantCulture);

    private static double Seconds(string value) => value.EndsWith("ms")
        ? double.Parse(value[..^2], CultureInfo.InvariantCulture) / 1000
        : double.Parse(value.TrimEnd('s'), CultureInfo.InvariantCulture);

    // What a CSS value - a theme token, typically - resolves to on this page, read off a probe element.
    private Task<string> ResolveAsync(string property, string value)
    {
        return Page.EvaluateAsync<string>(
            @"([property, value]) => {
                const probe = document.createElement('div');
                probe.style.setProperty(property, value);
                probe.style.borderStyle = 'solid';
                document.body.appendChild(probe);
                const resolved = getComputedStyle(probe).getPropertyValue(property);
                probe.remove();
                return resolved;
            }", new[] { property, value });
    }
}

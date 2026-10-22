namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiImagePartView at /e2e/aiimagepartview in a real browser: an image part as a figure with its image,
/// alternative text and caption, its box sized from the declared intrinsic size before the image arrives (nothing
/// jumps), the image fetched only once it is scrolled near, a decorative image left out of the accessibility tree,
/// the image script played by the transport, right to left, the --bit-AiImagePartView-* variables, Classes/Styles and
/// print.
/// </summary>
[TestClass]
public class BitAiImagePartViewE2ETests : E2ETestBase
{
    private const string Route = "/e2e/aiimagepartview";

    // A 640 by 320 image, the intrinsic size the parts declare.
    private const string Svg = """<svg xmlns="http://www.w3.org/2000/svg" width="640" height="320"><rect width="640" height="320" fill="#2563eb"/></svg>""";

    private readonly List<string> _requests = [];
    private TaskCompletionSource? _holdChart;

    [TestInitialize]
    public async Task ServeTheImagesAsync()
    {
        await Page.RouteAsync("https://images.e2e.test/**", async route =>
        {
            lock (_requests) _requests.Add(route.Request.Url);

            if (route.Request.Url.EndsWith("/chart.svg", StringComparison.Ordinal) && _holdChart is { } hold) await hold.Task;

            await route.FulfillAsync(new() { Status = 200, ContentType = "image/svg+xml", Body = Svg });
        });
    }

    [TestMethod]
    public async Task Basic_AnImagePart_IsAFigureOfItsImageAndCaption()
    {
        await OpenAsync(Route);

        var figure = Page.Locator("#ipv-basic");
        Assert.AreEqual("FIGURE", await figure.EvaluateAsync<string>("e => e.tagName"));
        await Expect(figure).ToHaveClassAsync(HasClass("bit-aiipv"));

        var image = figure.Locator("img.bit-aiipv-img");
        await Expect(image).ToHaveAttributeAsync("src", "https://images.e2e.test/chart.svg");
        await Expect(image).ToHaveAttributeAsync("width", "640");
        await Expect(image).ToHaveAttributeAsync("height", "320");
        await Expect(image).ToHaveAttributeAsync("loading", "lazy");
        await Expect(image).ToHaveAttributeAsync("decoding", "async");

        // The figure is named by its caption, the image by its alternative text.
        await Expect(Page.GetByRole(AriaRole.Figure, new() { Name = "Visitors per day" })).ToHaveCountAsync(1);
        await Expect(figure.GetByRole(AriaRole.Img, new() { Name = "A chart of the visitors" })).ToBeVisibleAsync();
        await Expect(figure.Locator("figcaption.bit-aiipv-cap")).ToHaveTextAsync("Visitors per day");

        // Never wider than the message: the 640px image is drawn at the width of its 200px box, its height following.
        await ExpectLoadedAsync(image);
        var box = await image.BoundingBoxAsync();
        Assert.AreEqual(200, box!.Width, 0.5);
        Assert.AreEqual(100, box.Height, 0.5);

        await Expect(image).ToHaveCSSAsync("border-top-left-radius", await figure.ResolveLengthInAsync("var(--bit-shp-radius-surface)"));
        var caption = figure.Locator("figcaption");
        await Expect(caption).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-ter)"));
        await Expect(caption).ToHaveCSSAsync("font-size", await figure.ResolveLengthInAsync("var(--bit-tpg-fs-xs)"));
        await Expect(figure).ToHaveCSSAsync("margin-top", "0px");
    }

    [TestMethod]
    public async Task Basic_AnImageWithoutAlternativeText_IsLeftOutOfTheAccessibilityTree()
    {
        await OpenAsync(Route);

        var figure = Page.Locator("#ipv-noalt");
        await Expect(figure.Locator("img")).ToHaveAttributeAsync("alt", "");
        await Expect(figure.GetByRole(AriaRole.Img)).ToHaveCountAsync(0);
        await Expect(figure.Locator("figcaption")).ToHaveCountAsync(0);

        // An inline image needs no request and is drawn at its declared size.
        var data = Page.Locator("#ipv-data img");
        await ExpectLoadedAsync(data);
        var box = await data.BoundingBoxAsync();
        Assert.AreEqual(48, box!.Width, 0.5);
        Assert.AreEqual(48, box.Height, 0.5);
    }

    [TestMethod]
    public async Task IntrinsicSize_WhileTheImageLoads_ItsBoxAlreadyHasItsHeight()
    {
        _holdChart = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await OpenAsync(Route, waitForLoad: false);

        var image = Page.Locator("#ipv-basic img");
        await AiE2EHelpers.PollAsync(() => Task.FromResult(Requested("/chart.svg")), "The image was asked for.");

        // Nothing has arrived, and the box already has the height the declared size gives it: the list does not jump.
        Assert.AreEqual(0, await image.EvaluateAsync<int>("e => e.naturalWidth"));
        var before = await image.BoundingBoxAsync();
        Assert.AreEqual(200, before!.Width, 0.5);
        Assert.AreEqual(100, before.Height, 0.5);

        var captionBefore = await Page.Locator("#ipv-basic figcaption").RectAsync();

        _holdChart.SetResult();
        await ExpectLoadedAsync(image);

        var after = await image.BoundingBoxAsync();
        Assert.AreEqual(before.Height, after!.Height, 0.5);
        Assert.AreEqual(captionBefore.Top, (await Page.Locator("#ipv-basic figcaption").RectAsync()).Top, 0.5, "The caption did not move.");
    }

    [TestMethod]
    public async Task Lazy_AnImageFarBelowTheFold_IsFetchedOnlyOnceItIsScrolledNear()
    {
        await OpenAsync(Route);

        var image = Page.Locator("#ipv-lazy img");
        await ExpectLoadedAsync(Page.Locator("#ipv-basic img"));
        await NextFramesAsync();
        Assert.IsFalse(Requested("/lazy.svg"), "The image far below the fold was not fetched with the page.");

        await image.ScrollIntoViewIfNeededAsync();

        await ExpectLoadedAsync(image);
        Assert.IsTrue(Requested("/lazy.svg"));
    }

    [TestMethod]
    public async Task Transport_TheImageScript_ShowsTheImageThePartCarries()
    {
        await OpenAsync(Route);

        await Page.Locator("#ipv-send").ClickAsync();
        await Expect(Page.Locator("#ipv-live-state")).ToHaveTextAsync("Complete");

        var image = Page.Locator("#ipv-live-image img");
        await Expect(image).ToHaveAttributeAsync("alt", "A chart of the visitors");
        await Expect(image).ToHaveAttributeAsync("width", "64");
        await Expect(image).ToHaveAttributeAsync("height", "48");
        await Expect(image).ToHaveAttributeAsync("src", new System.Text.RegularExpressions.Regex("^data:image/png;base64,"));
        await ExpectLoadedAsync(image);
    }

    [TestMethod]
    public async Task Rtl_TheCaptionOfARightToLeftImage_StartsAtTheRight()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#ipv-rtl")).ToHaveAttributeAsync("dir", "rtl");

        foreach (var id in new[] { "ipv-rtl", "ipv-rtl-inherited" })
        {
            var caption = Page.Locator($"#{id} figcaption");
            await Expect(caption).ToHaveCSSAsync("direction", "rtl");
            var box = await caption.RectAsync();
            var text = await TextRectAsync(caption);
            Assert.AreEqual(box.Right, text.Right, 2, $"{id}: the caption starts at the right.");
        }

        // The same caption in a left-to-right box starts at the left.
        var ltr = Page.Locator("#ipv-ltr figcaption");
        Assert.AreEqual((await ltr.RectAsync()).Left, (await TextRectAsync(ltr)).Left, 2);
    }

    // The box of the whole text of an element, wherever the lines put it.
    private static async Task<(double Left, double Right)> TextRectAsync(ILocator locator)
    {
        var r = await locator.EvaluateAsync<double[]>(
            "e => { const range = document.createRange(); range.selectNodeContents(e); const r = range.getBoundingClientRect(); return [r.left, r.right]; }");

        return (r[0], r[1]);
    }

    [TestMethod]
    public async Task Variables_ThePublicVariables_RestyleTheImageAndItsCaption()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#ipv-vars img")).ToHaveCSSAsync("border-top-left-radius", "16px");
        await Expect(Page.Locator("#ipv-vars figcaption")).ToHaveCSSAsync("color", "rgb(1, 2, 3)");
        await Expect(Page.Locator("#ipv-vars figcaption")).ToHaveCSSAsync("font-size", "20px");
    }

    [TestMethod]
    public async Task StyleAndClass_ClassesAndStyles_ReachTheirParts()
    {
        await OpenAsync(Route);

        var figure = Page.Locator("#ipv-parts");
        await Expect(figure).ToHaveClassAsync(HasClass("e2e-ipv-class"));
        await Expect(figure).ToHaveClassAsync(HasClass("e2e-ipv-root"));
        await Expect(figure).ToHaveCSSAsync("outline-color", "rgb(4, 5, 6)");
        await Expect(figure.Locator("img")).ToHaveClassAsync(HasClass("e2e-ipv-image"));
        await Expect(figure.Locator("img")).ToHaveCSSAsync("opacity", "0.5");
        await Expect(figure.Locator("figcaption")).ToHaveClassAsync(HasClass("e2e-ipv-caption"));
        await Expect(figure.Locator("figcaption")).ToHaveCSSAsync("letter-spacing", "3px");
    }

    [TestMethod]
    public async Task Print_APrintedImage_IsNeverSplitAcrossPages()
    {
        await OpenAsync(Route);
        await Page.EmulateMediaAsync(new() { Media = Media.Print });

        await Expect(Page.Locator("#ipv-basic")).ToHaveCSSAsync("break-inside", "avoid");
    }

    private bool Requested(string fragment)
    {
        lock (_requests) return _requests.Exists(url => url.Contains(fragment, StringComparison.Ordinal));
    }

    private static async Task ExpectLoadedAsync(ILocator image)
    {
        await Expect(image).ToHaveJSPropertyAsync("complete", true);
        await AiE2EHelpers.PollAsync(async () => await image.EvaluateAsync<int>("e => e.naturalWidth") > 0, "The image was loaded and decoded.");
    }
}

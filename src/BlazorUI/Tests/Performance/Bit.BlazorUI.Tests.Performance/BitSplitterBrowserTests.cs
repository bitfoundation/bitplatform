using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser tests of what a reader does with a <see cref="BitSplitter"/>, driven with real pointer and keyboard input:
/// dragging the gutter resizes the panels within the limits of the first one, the arrow, Page, Home and End keys move
/// it, Escape abandons a drag, a row read right to left turns the horizontal direction around, and every resize is
/// reported back through the bound Percent and the resize callbacks.
/// The splitters of <c>/regression/splitter-behaviour</c> share 600px between their panels (a 10px gutter in a 610px
/// box), start the first panel at 200px and hold it between 100px and 400px.
/// </summary>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitSplitterBrowserTests
public class BitSplitterBrowserTests : PerformanceTestBase
{
    private const string PageUrl = "/regression/splitter-behaviour";

    // A pixel of slack for sub-pixel layout, and for the percentage the panel is written back as.
    private const double Tolerance = 1.5;

    [TestMethod]
    public async Task BitSplitter_DraggingTheGutter_ResizesThePanelsAndReportsThePercent()
    {
        await Open();

        Assert.AreEqual(200, await FirstSize("ltr"), Tolerance, "the first panel starts at its FirstPanelSize");

        await Drag("ltr", 100);

        await Expect(Report("ltr", "ends")).ToHaveTextAsync("1");
        await Expect(Report("ltr", "starts")).ToHaveTextAsync("1");
        await Expect(Report("ltr", "moves")).ToHaveTextAsync("some");

        var first = await FirstSize("ltr");
        Assert.AreEqual(300, first, Tolerance, "the first panel follows the pointer");
        Assert.AreEqual(600 - first, await SecondSize("ltr"), Tolerance, "the second panel takes the rest");

        // A share of the whole splitter, the gutter included: 300 of 610.
        var expected = first / 610 * 100;
        Assert.AreEqual(expected, await ReportNumber("ltr", "percent"), 0.2, "the bound Percent");
        Assert.AreEqual(expected, await ReportNumber("ltr", "last-end"), 0.2, "OnResizeEnd");
        Assert.AreEqual(expected, double.Parse(await Gutter("ltr").GetAttributeAsync("aria-valuenow") ?? "", CultureInfo.InvariantCulture), 0.2,
            "the separator announces where it stands");
    }

    [TestMethod]
    public async Task BitSplitter_DraggingPastTheLimits_StopsAtTheMinimumAndTheMaximum()
    {
        await Open();

        await Drag("ltr", 350);
        await Expect(Report("ltr", "ends")).ToHaveTextAsync("1");
        Assert.AreEqual(400, await FirstSize("ltr"), Tolerance, "held at FirstPanelMaxSize");

        // Far enough past the minimum, while staying inside the window the release has to land in.
        await Drag("ltr", -350);
        await Expect(Report("ltr", "ends")).ToHaveTextAsync("2");
        Assert.AreEqual(100, await FirstSize("ltr"), Tolerance, "held at FirstPanelMinSize");
        Assert.AreEqual(100.0 / 610 * 100, await ReportNumber("ltr", "percent"), 0.2);
    }

    [TestMethod]
    public async Task BitSplitter_EscapeDuringADrag_PutsThePanelsBack()
    {
        await Open();

        var box = await GutterCenter("ltr");

        await Page.Mouse.MoveAsync(box.X, box.Y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(box.X + 120, box.Y, new() { Steps = 8 });

        // The panels follow the pointer while it is down.
        await WaitFor("ltr", "el => el.querySelector('.bit-spl-fpn').getBoundingClientRect().width > 300");

        await Page.Keyboard.PressAsync("Escape");
        await Page.Mouse.UpAsync();

        await Expect(Report("ltr", "cancels")).ToHaveTextAsync("1");
        Assert.AreEqual(200, await FirstSize("ltr"), Tolerance, "the drag is undone");
        await Expect(Report("ltr", "ends")).ToHaveTextAsync("0");
        await Expect(Report("ltr", "percent")).ToHaveTextAsync("none");
        Assert.AreEqual(200.0 / 610 * 100, await ReportNumber("ltr", "last-cancel"), 0.2, "OnResizeCancel reports where it went back to");
    }

    [TestMethod]
    public async Task BitSplitter_Keyboard_MovesTheGutterByItsSteps()
    {
        await Open();

        await TabToGutter("ltr");

        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Report("ltr", "ends")).ToHaveTextAsync("1");
        Assert.AreEqual(210, await FirstSize("ltr"), Tolerance, "an arrow moves KeyboardStep pixels");
        Assert.AreEqual(210.0 / 610 * 100, await ReportNumber("ltr", "percent"), 0.2);

        await Page.Keyboard.PressAsync("Shift+ArrowRight");
        await Expect(Report("ltr", "ends")).ToHaveTextAsync("2");
        Assert.AreEqual(310, await FirstSize("ltr"), Tolerance, "Shift moves ten steps");

        await Page.Keyboard.PressAsync("PageDown");
        await Expect(Report("ltr", "ends")).ToHaveTextAsync("3");
        Assert.AreEqual(400, await FirstSize("ltr"), Tolerance, "a page key moves ten steps, held at the maximum");

        await Page.Keyboard.PressAsync("Home");
        await Expect(Report("ltr", "ends")).ToHaveTextAsync("4");
        Assert.AreEqual(100, await FirstSize("ltr"), Tolerance, "Home goes to the minimum");

        await Page.Keyboard.PressAsync("ArrowLeft");
        Assert.AreEqual(100, await FirstSize("ltr"), Tolerance, "nothing moves past the minimum");

        await Page.Keyboard.PressAsync("End");
        await WaitFor("ltr", "el => Math.abs(el.querySelector('.bit-spl-fpn').getBoundingClientRect().width - 400) < 1.5");
        Assert.AreEqual(400.0 / 610 * 100, double.Parse(await Gutter("ltr").GetAttributeAsync("aria-valuenow") ?? "", CultureInfo.InvariantCulture), 0.2);
        await ExpectReportNumber("ltr", "percent", 400.0 / 610 * 100);
    }

    [TestMethod]
    [DataRow("own", DisplayName = "Dir=Rtl")]
    [DataRow("inherited", DisplayName = "inside dir=rtl")]
    public async Task BitSplitter_RightToLeft_TurnsTheHorizontalDirectionAround(string probe)
    {
        await Open();

        var layout = await Probe(probe).EvaluateAsync<double[]>(@"el => [
            el.querySelector('.bit-spl-fpn').getBoundingClientRect().left,
            el.querySelector('.bit-spl-gtr').getBoundingClientRect().left]");
        Assert.IsTrue(layout[0] > layout[1], "the first panel sits on the right of the gutter");

        // Dragging the gutter to the left widens the first panel, which is on the right.
        await Drag(probe, -100);
        await Expect(Report(probe, "ends")).ToHaveTextAsync("1");
        Assert.AreEqual(300, await FirstSize(probe), Tolerance);
        Assert.AreEqual(300.0 / 610 * 100, await ReportNumber(probe, "percent"), 0.2);

        await TabToGutter(probe);

        // The arrows move the gutter the way they point: the left arrow widens the first panel there too.
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Report(probe, "ends")).ToHaveTextAsync("2");
        Assert.AreEqual(310, await FirstSize(probe), Tolerance);

        await Page.Keyboard.PressAsync("ArrowRight");
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Report(probe, "ends")).ToHaveTextAsync("4");
        Assert.AreEqual(290, await FirstSize(probe), Tolerance);
    }

    [TestMethod]
    public async Task BitSplitter_Vertical_MovesTheGutterUpAndDown()
    {
        await Open();

        Assert.AreEqual(200, await FirstSize("vertical"), Tolerance);

        var box = await GutterCenter("vertical");
        await Page.Mouse.MoveAsync(box.X, box.Y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(box.X, box.Y + 80, new() { Steps = 8 });
        await Page.Mouse.UpAsync();

        await Expect(Report("vertical", "ends")).ToHaveTextAsync("1");
        Assert.AreEqual(280, await FirstSize("vertical"), Tolerance);

        await TabToGutter("vertical");

        // The left and right arrows mean nothing to a splitter laid out in a column.
        await Page.Keyboard.PressAsync("ArrowRight");
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Report("vertical", "ends")).ToHaveTextAsync("2");
        Assert.AreEqual(290, await FirstSize("vertical"), Tolerance);

        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Report("vertical", "ends")).ToHaveTextAsync("3");
        Assert.AreEqual(280, await FirstSize("vertical"), Tolerance);
    }



    private async Task Open()
    {
        await Page.GotoAsync($"{BaseUrl}{PageUrl}");
        await WaitForStatus("Ready");

        // The setup call measures the panels and writes the position onto the separator, which is what says the
        // browser half is listening.
        foreach (var probe in new[] { "ltr", "own", "inherited", "vertical" })
        {
            await Expect(Gutter(probe)).ToHaveAttributeAsync("aria-valuenow", new System.Text.RegularExpressions.Regex(@"^\d"));
        }
    }

    private ILocator Probe(string probe) => Page.Locator($"[data-probe='{probe}']");

    private ILocator Gutter(string probe) => Probe(probe).Locator(".bit-spl-gtr");

    private ILocator Report(string probe, string name) => Probe(probe).Locator($".report .{name}");

    private async Task<double> ReportNumber(string probe, string name)
    {
        var text = await Report(probe, name).InnerTextAsync();

        return double.Parse(text, CultureInfo.InvariantCulture);
    }

    // The value the page prints, once the round trip that reports it has landed.
    private Task ExpectReportNumber(string probe, string name, double expected)
    {
        return Expect(Report(probe, name)).ToHaveTextAsync(expected.ToString("0.##", CultureInfo.InvariantCulture));
    }

    private Task<double> FirstSize(string probe) => PanelSize(probe, ".bit-spl-fpn");

    private Task<double> SecondSize(string probe) => PanelSize(probe, ".bit-spl-spn");

    private Task<double> PanelSize(string probe, string selector)
    {
        return Probe(probe).EvaluateAsync<double>(
            "(el, selector) => { const r = el.querySelector(selector).getBoundingClientRect(); return el.dataset.probe === 'vertical' ? r.height : r.width; }",
            selector);
    }

    private async Task<(float X, float Y)> GutterCenter(string probe)
    {
        // The pointer only reaches what is on the screen.
        await Probe(probe).ScrollIntoViewIfNeededAsync();

        var box = await Gutter(probe).BoundingBoxAsync();
        Assert.IsNotNull(box);

        return (box.X + box.Width / 2, box.Y + box.Height / 2);
    }

    // A press, a move in several steps (a real drag reaches the gutter's pointermove a few times) and a release.
    private async Task Drag(string probe, float dx)
    {
        var (x, y) = await GutterCenter(probe);

        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x + dx, y, new() { Steps = 10 });
        await Page.Mouse.UpAsync();
    }

    // The keyboard reaches the gutter the way a reader's does: from the control before it.
    private async Task TabToGutter(string probe)
    {
        await Probe(probe).Locator(".before").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");

        await Expect(Gutter(probe)).ToBeFocusedAsync();
    }

    private Task WaitFor(string probe, string predicate)
    {
        return Page.WaitForFunctionAsync($"(probe) => ({predicate})(document.querySelector(`[data-probe='${{probe}}']`))", probe,
            new() { Timeout = DefaultTimeout });
    }
}

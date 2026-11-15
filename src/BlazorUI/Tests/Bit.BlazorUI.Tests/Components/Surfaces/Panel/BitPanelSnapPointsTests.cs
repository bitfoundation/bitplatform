using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Panel;

/// <summary>
/// A panel at the bottom with snap points - a bottom sheet: the height it is at, written as the CSS height of its snap
/// point; the snap points it reads and the ones it leaves out; its grabber, which moves it from the keyboard and with a
/// tap; SnapPoint, two-way bound or held one way; and where a drag settles (OnSnap).
/// </summary>
[TestClass, DoNotParallelize]
public class BitPanelSnapPointsTests : BunitTestContext
{
    private sealed class GermanStrings : BitStrings
    {
        public override string PanelSizeFormat => "Größe ändern, {0} von {1}";
    }

    [TestCleanup]
    public void ResetStrings() => BitStrings.Current = null;

    private IRenderedComponent<BitPanel> RenderSheet(IReadOnlyList<double>? snapPoints, System.Action<ComponentParameterCollectionBuilder<BitPanel>>? more = null)
    {
        return RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Placement, BitPlacement.Bottom);
            parameters.Add(p => p.SnapPoints, snapPoints);
            more?.Invoke(parameters);
        });
    }

    [TestMethod]
    public void ABottomSheetShouldBeTheHeightOfTheSnapPointItIsAt()
    {
        var com = RenderSheet([0.25, 400, 0.9], p => p.Add(x => x.Size, 300));

        var container = com.Find(".bit-pnl-cnt");
        Assert.IsTrue(container.ClassList.Contains("bit-pnl-snp"));
        Assert.AreEqual("--bit-pnl-snp:25dvh", container.GetAttribute("style"), "the size gives way to the snap points");
        Assert.AreEqual("0.25 400 0.9", container.GetAttribute("data-bit-snaps"));
        Assert.AreEqual("0", container.GetAttribute("data-bit-snap"));

        com.Render(p => p.Add(x => x.SnapPoint, 1));
        Assert.AreEqual("--bit-pnl-snp:400px", com.Find(".bit-pnl-cnt").GetAttribute("style"));
        Assert.AreEqual("1", com.Find(".bit-pnl-cnt").GetAttribute("data-bit-snap"));

        com.Render(p => p.Add(x => x.SnapPoint, 9));
        Assert.AreEqual("--bit-pnl-snp:90dvh", com.Find(".bit-pnl-cnt").GetAttribute("style"), "an index past the last is the last");
    }

    [TestMethod]
    public void OnlyThePositiveSnapPointsShouldBeRead()
    {
        var com = RenderSheet([0, -1, double.NaN, 0.5]);

        var container = com.Find(".bit-pnl-cnt");
        Assert.AreEqual("0.5", container.GetAttribute("data-bit-snaps"));
        Assert.AreEqual("--bit-pnl-snp:50dvh", container.GetAttribute("style"));
    }

    [TestMethod]
    [DataRow(BitPlacement.End, false)]
    [DataRow(BitPlacement.Top, false)]
    [DataRow(BitPlacement.Bottom, true)]
    public void SnapPointsShouldOnlyBeReadAtTheBottomAndNotFullSize(BitPlacement placement, bool snaps)
    {
        var com = RenderComponent<BitPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Placement, placement);
            parameters.Add(p => p.SnapPoints, [0.3, 0.8]);
        });

        Assert.AreEqual(snaps, com.Find(".bit-pnl-cnt").ClassList.Contains("bit-pnl-snp"));
        Assert.AreEqual(snaps, com.FindAll(".bit-pnl-grb").Count == 1);

        com.Render(p => p.Add(x => x.FullSize, true));
        Assert.IsFalse(com.Find(".bit-pnl-cnt").ClassList.Contains("bit-pnl-snp"), "a full-size panel has no snap points");
        Assert.IsNull(com.Find(".bit-pnl-cnt").GetAttribute("data-bit-snaps"));
        Assert.AreEqual(0, com.FindAll(".bit-pnl-grb").Count);
    }

    [TestMethod]
    public void ASheetWithOneSnapPointShouldHaveNoGrabber()
    {
        var com = RenderSheet([0.5]);

        Assert.IsTrue(com.Find(".bit-pnl-cnt").ClassList.Contains("bit-pnl-snp"));
        Assert.AreEqual(0, com.FindAll(".bit-pnl-grb").Count, "one height has nowhere to be moved to");
    }

    [TestMethod]
    public void TheGrabberShouldBeAButtonNamedAfterTheSizeTheSheetIsAt()
    {
        var com = RenderSheet([0.3, 0.6, 0.9]);

        var grabber = com.Find(".bit-pnl-cnt > .bit-pnl-grb");
        Assert.AreEqual("BUTTON", grabber.TagName);
        Assert.AreEqual("button", grabber.GetAttribute("type"));
        Assert.AreEqual("Resize, size 1 of 3", grabber.GetAttribute("aria-label"));
        Assert.AreEqual("ArrowUp ArrowDown Home End", grabber.GetAttribute("aria-keyshortcuts"));
        Assert.IsTrue(grabber.HasAttribute("data-bit-swipe-handle"));
        Assert.AreEqual("true", grabber.QuerySelector(".bit-pnl-grh")!.GetAttribute("aria-hidden"));

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Utils.registerPreventKeys");
    }

    [TestMethod]
    public void TheGrabberShouldTakeItsNameFromItsFormatOrTheStrings()
    {
        BitStrings.Current = new GermanStrings();

        var com = RenderSheet([0.3, 0.6]);
        Assert.AreEqual("Größe ändern, 1 von 2", com.Find(".bit-pnl-grb").GetAttribute("aria-label"));

        com.Render(p => p.Add(x => x.GrabberAriaLabelFormat, "Height {0}/{1}"));
        Assert.AreEqual("Height 1/2", com.Find(".bit-pnl-grb").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void ATapOnTheGrabberShouldMoveTheSheetToTheNextHeightAndBackToTheFirst()
    {
        var changes = new List<int?>();
        var com = RenderSheet([0.3, 0.6, 0.9], p => p.Add(x => x.SnapPointChanged, (int? v) => changes.Add(v)));

        com.Find(".bit-pnl-grb").Click();
        Assert.AreEqual("--bit-pnl-snp:60dvh", com.Find(".bit-pnl-cnt").GetAttribute("style"));
        Assert.AreEqual("Resize, size 2 of 3", com.Find(".bit-pnl-grb").GetAttribute("aria-label"));

        com.Find(".bit-pnl-grb").Click();
        com.Find(".bit-pnl-grb").Click();
        Assert.AreEqual("--bit-pnl-snp:30dvh", com.Find(".bit-pnl-cnt").GetAttribute("style"), "after the last, the first");

        CollectionAssert.AreEqual(new int?[] { 1, 2, 0 }, changes);
    }

    [TestMethod]
    public void TheGrabbersKeysShouldMoveTheSheetUpDownAndToItsEnds()
    {
        var com = RenderSheet([0.3, 0.6, 0.9]);
        var grabber = () => com.Find(".bit-pnl-grb");
        var height = () => com.Find(".bit-pnl-cnt").GetAttribute("style");

        grabber().KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        Assert.AreEqual("--bit-pnl-snp:30dvh", height(), "nothing is smaller than the smallest");

        grabber().KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
        Assert.AreEqual("--bit-pnl-snp:60dvh", height());

        grabber().KeyDown(new KeyboardEventArgs { Key = "End" });
        Assert.AreEqual("--bit-pnl-snp:90dvh", height());

        grabber().KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
        Assert.AreEqual("--bit-pnl-snp:90dvh", height(), "nothing is larger than the largest");

        grabber().KeyDown(new KeyboardEventArgs { Key = "Home" });
        Assert.AreEqual("--bit-pnl-snp:30dvh", height());

        grabber().KeyDown(new KeyboardEventArgs { Key = "x" });
        Assert.AreEqual("--bit-pnl-snp:30dvh", height());
    }

    [TestMethod]
    public void ASnapPointHeldOneWayShouldStayWhereTheAppHoldsIt()
    {
        var com = RenderSheet([0.3, 0.6], p => p.Add(x => x.SnapPoint, 0));

        com.Find(".bit-pnl-grb").Click();
        com.Find(".bit-pnl-grb").KeyDown(new KeyboardEventArgs { Key = "End" });

        Assert.AreEqual("--bit-pnl-snp:30dvh", com.Find(".bit-pnl-cnt").GetAttribute("style"));
    }

    [TestMethod]
    public async Task ADragShouldSettleWhereTheComponentSays()
    {
        var bound = RenderSheet([0.3, 0.6, 0.9]);
        Assert.AreEqual(2, await bound.InvokeAsync(() => bound.Instance._OnSnap(2)));
        Assert.AreEqual("--bit-pnl-snp:90dvh", bound.Find(".bit-pnl-cnt").GetAttribute("style"));
        Assert.AreEqual(2, await bound.InvokeAsync(() => bound.Instance._OnSnap(7)), "an index the sheet has no height for is no move");

        // Held one way, the sheet goes back where the app holds it, which is what the script settles it at.
        var held = RenderSheet([0.3, 0.6, 0.9], p => p.Add(x => x.SnapPoint, 1));
        Assert.AreEqual(1, await held.InvokeAsync(() => held.Instance._OnSnap(0)));
    }

    [TestMethod]
    public void ADisabledSheetsGrabberShouldMoveNothing()
    {
        var com = RenderSheet([0.3, 0.6], p => p.Add(x => x.Disabled, true));

        var grabber = com.Find(".bit-pnl-grb");
        Assert.IsTrue(grabber.HasAttribute("disabled"));

        grabber.KeyDown(new KeyboardEventArgs { Key = "End" });
        Assert.AreEqual("--bit-pnl-snp:30dvh", com.Find(".bit-pnl-cnt").GetAttribute("style"));
    }

    [TestMethod]
    public void TheSnapPointsShouldReachThePanelThroughBitParams()
    {
        var com = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitPanelParams { SnapPoints = [0.4, 0.8], GrabberAriaLabelFormat = "{0}/{1}" } });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitPanel>(0);
                builder.AddComponentParameter(1, nameof(BitPanel.IsOpen), true);
                builder.AddComponentParameter(2, nameof(BitPanel.Placement), (BitPlacement?)BitPlacement.Bottom);
                builder.CloseComponent();
            });
        });

        Assert.AreEqual("--bit-pnl-snp:40dvh", com.Find(".bit-pnl-cnt").GetAttribute("style"));
        Assert.AreEqual("1/2", com.Find(".bit-pnl-grb").GetAttribute("aria-label"));
    }
}

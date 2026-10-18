using System.Globalization;

namespace Bit.Blow.Tests;

[TestClass]
public class ViewportTests
{
    [TestMethod]
    public void ScreenToFlow_AndBack_RoundTrips()
    {
        var viewport = new BlowViewport(120, -40, 1.75);
        var flow = new BlowXYPosition(33.5, -12.25);

        var screen = viewport.FlowToScreen(flow);
        var back = viewport.ScreenToFlow(screen.X, screen.Y);

        Assert.AreEqual(flow.X, back.X, 1e-6);
        Assert.AreEqual(flow.Y, back.Y, 1e-6);
    }

    [TestMethod]
    public void Identity_LeavesCoordinatesUnchanged()
    {
        var point = BlowViewport.Identity.ScreenToFlow(10, 20);

        Assert.AreEqual(10, point.X);
        Assert.AreEqual(20, point.Y);
    }

    [TestMethod]
    public void ToTransform_UsesInvariantFormattingUnderACommaDecimalCulture()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var transform = new BlowViewport(1.5, 2.5, 0.75).ToTransform();

            Assert.AreEqual("translate(1.5px, 2.5px) scale(0.75)", transform);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [TestMethod]
    public void Rect_ExposesConsistentEdges()
    {
        var rect = new BlowRect(10, 20, 100, 50);

        Assert.AreEqual(10, rect.Left);
        Assert.AreEqual(20, rect.Top);
        Assert.AreEqual(110, rect.Right);
        Assert.AreEqual(70, rect.Bottom);
    }

    [TestMethod]
    public void Dimensions_IsEmptyWhenEitherSideIsNonPositive()
    {
        Assert.IsTrue(BlowDimensions.Empty.IsEmpty);
        Assert.IsTrue(new BlowDimensions(10, 0).IsEmpty);
        Assert.IsTrue(new BlowDimensions(0, 10).IsEmpty);
        Assert.IsFalse(new BlowDimensions(1, 1).IsEmpty);
    }

    [TestMethod]
    public void EffectiveSize_PrefersExplicitThenMeasuredThenDefaults()
    {
        var explicitSize = new BlowNode { Id = "a", Width = 200, Height = 80 };
        var measured = new BlowNode { Id = "b", Measured = new BlowDimensions(123, 45) };
        var unknown = new BlowNode { Id = "c" };

        Assert.AreEqual(new BlowDimensions(200, 80), explicitSize.EffectiveSize);
        Assert.AreEqual(new BlowDimensions(123, 45), measured.EffectiveSize);
        Assert.AreEqual(new BlowDimensions(BlowNode.DefaultWidth, BlowNode.DefaultHeight), unknown.EffectiveSize);
    }

    [TestMethod]
    public void HandleBoundsKey_DistinguishesTypeAndHandleId()
    {
        Assert.AreNotEqual(BlowHandleBounds.Key(BlowHandleType.Source, "a"), BlowHandleBounds.Key(BlowHandleType.Target, "a"));
        Assert.AreNotEqual(BlowHandleBounds.Key(BlowHandleType.Source, "a"), BlowHandleBounds.Key(BlowHandleType.Source, "b"));
        Assert.AreEqual(BlowHandleBounds.Key(BlowHandleType.Source, null), BlowHandleBounds.Key(BlowHandleType.Source, null));
    }

    [TestMethod]
    [DataRow("top-left", BlowPanelPosition.TopLeft)]
    [DataRow("Top Right", BlowPanelPosition.TopRight)]
    [DataRow("bottom_center", BlowPanelPosition.BottomCenter)]
    [DataRow("nonsense", BlowPanelPosition.TopLeft)]
    [DataRow(null, BlowPanelPosition.TopLeft)]
    public void PanelPositionParse_IsForgiving(string? input, BlowPanelPosition expected)
    {
        Assert.AreEqual(expected, BlowPanelPositionClass.Parse(input));
    }
}

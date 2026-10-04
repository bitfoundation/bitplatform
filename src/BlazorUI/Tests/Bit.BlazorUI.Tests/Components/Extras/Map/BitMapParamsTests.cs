using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.Map;

[TestClass]
public class BitMapParamsTests : BunitTestContext
{
    [TestMethod]
    public void BitMapShouldTakeTheCascadedParamsItDoesNotSetItself()
    {
        var component = RenderComponent<BitMapCascadingParamsTest>();

        var maps = component.FindAll(".bit-map");
        Assert.AreEqual(3, maps.Count);

        // The first map takes everything from the cascade.
        var first = maps[0];
        Assert.IsTrue(first.ClassList.Contains("cascaded"));
        Assert.IsTrue(first.ClassList.Contains("cascaded-root"));
        StringAssert.Contains(first.GetAttribute("style"), "--bit-Map-radius: 1rem");
        Assert.IsNotNull(first.QuerySelector(".bit-map-gesture-hint"), "CooperativeGestures is cascaded");
        Assert.AreEqual("Cascaded keys", first.QuerySelector(".bit-map-help")!.TextContent.Trim());

        var canvas = first.QuerySelector(".bit-map-canvas")!;
        Assert.IsTrue(canvas.ClassList.Contains("cascaded-canvas"));
        StringAssert.Contains(canvas.GetAttribute("style"), "--cascaded-canvas: 1");

        // The marker list is a sibling of the map, so it is found by its own caption.
        var captions = component.FindAll(".bit-map-marker-table caption");
        Assert.AreEqual(3, captions.Count, "MarkerListMode is cascaded to every map");
        Assert.AreEqual("Cascaded caption", captions[0].TextContent.Trim());
    }

    [TestMethod]
    public void BitMapShouldKeepWhatItsOwnMarkupSets()
    {
        var component = RenderComponent<BitMapCascadingParamsTest>();

        var second = component.FindAll(".bit-map")[1];

        Assert.IsTrue(second.ClassList.Contains("second"));
        Assert.IsTrue(second.ClassList.Contains("own-root"));
        Assert.IsFalse(second.ClassList.Contains("cascaded"));
        Assert.IsFalse(second.ClassList.Contains("cascaded-root"));
        Assert.IsNull(second.QuerySelector(".bit-map-gesture-hint"), "its own CooperativeGestures=false wins");
        Assert.AreEqual("Own keys", second.QuerySelector(".bit-map-help")!.TextContent.Trim());

        // A parameter the markup leaves unset is still taken from the cascade.
        StringAssert.Contains(second.GetAttribute("style"), "--bit-Map-radius: 1rem");
    }

    [TestMethod]
    public void BitMapShouldReadTheSameParamsWhateverItsProvider()
    {
        var component = RenderComponent<BitMapCascadingParamsTest>();

        var third = component.FindAll(".bit-map")[2];

        Assert.IsTrue(third.ClassList.Contains("cascaded"));
        Assert.IsNotNull(third.QuerySelector(".bit-map-gesture-hint"));
    }

    [TestMethod]
    public void BitMapShouldGoBackToItsOwnDefaultsWhenTheParamsAreWithdrawn()
    {
        var component = RenderComponent<BitMapCascadingParamsTest>();

        component.Render(parameters => parameters.Add(p => p.WithParams, false));

        var first = component.FindAll(".bit-map")[0];

        Assert.IsFalse(first.ClassList.Contains("cascaded"));
        Assert.IsFalse(first.ClassList.Contains("cascaded-root"));
        Assert.IsNull(first.QuerySelector(".bit-map-gesture-hint"));
        Assert.AreEqual("Use the arrow keys to pan the map, plus and minus to zoom, and Escape to leave the map.",
                        first.QuerySelector(".bit-map-help")!.TextContent.Trim());
        Assert.AreEqual(0, component.FindAll(".bit-map-marker-table").Count);
    }

    [TestMethod]
    public void BitMapParamsShouldNotOverwriteAParameterTheMapSet()
    {
        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.PopupCloseLabel, "Own close");
        });

        new BitMapParams { PopupCloseLabel = "Cascaded close", PopupLabel = "Cascaded popup" }.UpdateParameters(component.Instance);

        Assert.AreEqual("Own close", component.Instance.PopupCloseLabel);
        Assert.AreEqual("Cascaded popup", component.Instance.PopupLabel);
    }
}

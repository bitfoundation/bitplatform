using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Modal;

[TestClass]
public class BitModalParametersTests
{
    [TestMethod]
    public void MergeShouldReturnNullWhenBothNull()
    {
        Assert.IsNull(BitModalParameters.Merge(null, null));
    }

    [TestMethod]
    public void MergeShouldReturnTheOtherWhenOneIsNull()
    {
        var p = new BitModalParameters();

        Assert.AreSame(p, BitModalParameters.Merge(p, null));
        Assert.AreSame(p, BitModalParameters.Merge(null, p));
    }

    [TestMethod]
    public void MergeShouldGivePrecedenceToFirstParameters()
    {
        var first = new BitModalParameters { Blocking = true, FullWidth = true };
        var second = new BitModalParameters { Blocking = false, FullHeight = true };

        var merged = BitModalParameters.Merge(first, second)!;

        Assert.AreEqual(true, merged.Blocking);   // first wins
        Assert.AreEqual(true, merged.FullWidth);  // only set on first
        Assert.AreEqual(true, merged.FullHeight); // only set on second
    }

    [TestMethod]
    public void MergeShouldFallBackToSecondForUnsetNullableValues()
    {
        var first = new BitModalParameters();
        var second = new BitModalParameters { ShowOverlay = false, AriaModal = false };

        var merged = BitModalParameters.Merge(first, second)!;

        Assert.AreEqual(false, merged.ShowOverlay);
        Assert.AreEqual(false, merged.AriaModal);
    }

    [TestMethod]
    public void MergeShouldUnionHtmlAttributesWithFirstWinningOnConflicts()
    {
        var first = new BitModalParameters
        {
            HtmlAttributes = new Dictionary<string, object> { ["data-test"] = "first", ["data-a"] = "a" }
        };
        var second = new BitModalParameters
        {
            HtmlAttributes = new Dictionary<string, object> { ["data-test"] = "second", ["data-b"] = "b" }
        };

        var merged = BitModalParameters.Merge(first, second)!;

        Assert.AreEqual("first", merged.HtmlAttributes["data-test"]);
        Assert.AreEqual("a", merged.HtmlAttributes["data-a"]);
        Assert.AreEqual("b", merged.HtmlAttributes["data-b"]);
    }

    [TestMethod]
    public void MergeShouldHandleNullHtmlAttributes()
    {
        var firstNull = new BitModalParameters { HtmlAttributes = null! };
        var second = new BitModalParameters
        {
            HtmlAttributes = new Dictionary<string, object> { ["data-b"] = "b" }
        };

        var mergedFirstNull = BitModalParameters.Merge(firstNull, second)!;
        Assert.IsNotNull(mergedFirstNull.HtmlAttributes);
        Assert.AreEqual("b", mergedFirstNull.HtmlAttributes["data-b"]);

        var first = new BitModalParameters
        {
            HtmlAttributes = new Dictionary<string, object> { ["data-a"] = "a" }
        };
        var secondNull = new BitModalParameters { HtmlAttributes = null! };

        var mergedSecondNull = BitModalParameters.Merge(first, secondNull)!;
        Assert.IsNotNull(mergedSecondNull.HtmlAttributes);
        Assert.AreEqual("a", mergedSecondNull.HtmlAttributes["data-a"]);

        var bothNull = BitModalParameters.Merge(
            new BitModalParameters { HtmlAttributes = null! },
            new BitModalParameters { HtmlAttributes = null! })!;
        Assert.IsNotNull(bothNull.HtmlAttributes);
        Assert.AreEqual(0, bothNull.HtmlAttributes.Count);
    }

    [TestMethod]
    public async System.Threading.Tasks.Task MergeShouldComposeCallbacksInvokingFirstThenSecond()
    {
        var order = new List<string>();

        var first = new BitModalParameters
        {
            OnDismiss = Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(
                new object(), () => order.Add("dismiss-first")),
            OnOverlayClick = Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(
                new object(), () => order.Add("overlay-first")),
        };
        var second = new BitModalParameters
        {
            OnDismiss = Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(
                new object(), () => order.Add("dismiss-second")),
            OnOverlayClick = Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(
                new object(), () => order.Add("overlay-second")),
        };

        var merged = BitModalParameters.Merge(first, second)!;

        await merged.OnDismiss.InvokeAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        await merged.OnOverlayClick.InvokeAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        CollectionAssert.AreEqual(
            new[] { "dismiss-first", "dismiss-second", "overlay-first", "overlay-second" },
            order);
    }
}

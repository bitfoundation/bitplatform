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
}

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.ProModal;

[TestClass]
public class BitProModalParametersTests
{
    [TestMethod]
    public void MergeShouldReturnNullWhenBothNull()
    {
        Assert.IsNull(BitProModalParameters.Merge(null, null));
    }

    [TestMethod]
    public void MergeShouldReturnTheOtherWhenOneIsNull()
    {
        var p = new BitProModalParameters();

        Assert.AreSame(p, BitProModalParameters.Merge(p, null));
        Assert.AreSame(p, BitProModalParameters.Merge(null, p));
    }

    [TestMethod]
    public void MergeShouldGivePrecedenceToFirstParameters()
    {
        var first = new BitProModalParameters
        {
            HeaderText = "first",
            Blocking = true,
            AriaLabel = "first-label",
            Visibility = BitVisibility.Hidden,
        };
        var second = new BitProModalParameters
        {
            HeaderText = "second",
            FooterText = "second-footer",
            Blocking = false,
            AriaLabel = "second-label",
            Visibility = BitVisibility.Collapsed,
        };

        var merged = BitProModalParameters.Merge(first, second)!;

        Assert.AreEqual("first", merged.HeaderText);
        Assert.AreEqual("second-footer", merged.FooterText); // only set on second
        Assert.AreEqual(true, merged.Blocking);
        Assert.AreEqual("first-label", merged.AriaLabel);
        Assert.AreEqual(BitVisibility.Hidden, merged.Visibility);
    }

    [TestMethod]
    public void MergeShouldFallBackToSecondForUnsetNullableValues()
    {
        var first = new BitProModalParameters();
        var second = new BitProModalParameters
        {
            HeaderText = "second",
            FullWidth = true,
            Position = BitPosition.TopRight,
        };

        var merged = BitProModalParameters.Merge(first, second)!;

        Assert.AreEqual("second", merged.HeaderText);
        Assert.AreEqual(true, merged.FullWidth);
        Assert.AreEqual(BitPosition.TopRight, merged.Position);
    }

    [TestMethod]
    public void MergeShouldUnionHtmlAttributesWithFirstWinningOnConflicts()
    {
        var first = new BitProModalParameters
        {
            HtmlAttributes = new Dictionary<string, object> { ["data-test"] = "first", ["data-a"] = "a" }
        };
        var second = new BitProModalParameters
        {
            HtmlAttributes = new Dictionary<string, object> { ["data-test"] = "second", ["data-b"] = "b" }
        };

        var merged = BitProModalParameters.Merge(first, second)!;

        Assert.AreEqual("first", merged.HtmlAttributes["data-test"]);
        Assert.AreEqual("a", merged.HtmlAttributes["data-a"]);
        Assert.AreEqual("b", merged.HtmlAttributes["data-b"]);
    }
}

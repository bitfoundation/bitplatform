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

    [TestMethod]
    public void MergeShouldHandleNullHtmlAttributes()
    {
        var firstNull = new BitProModalParameters { HtmlAttributes = null! };
        var second = new BitProModalParameters
        {
            HtmlAttributes = new Dictionary<string, object> { ["data-b"] = "b" }
        };

        var mergedFirstNull = BitProModalParameters.Merge(firstNull, second)!;
        Assert.IsNotNull(mergedFirstNull.HtmlAttributes);
        Assert.AreEqual("b", mergedFirstNull.HtmlAttributes["data-b"]);

        var first = new BitProModalParameters
        {
            HtmlAttributes = new Dictionary<string, object> { ["data-a"] = "a" }
        };
        var secondNull = new BitProModalParameters { HtmlAttributes = null! };

        var mergedSecondNull = BitProModalParameters.Merge(first, secondNull)!;
        Assert.IsNotNull(mergedSecondNull.HtmlAttributes);
        Assert.AreEqual("a", mergedSecondNull.HtmlAttributes["data-a"]);

        var bothNull = BitProModalParameters.Merge(
            new BitProModalParameters { HtmlAttributes = null! },
            new BitProModalParameters { HtmlAttributes = null! })!;
        Assert.IsNotNull(bothNull.HtmlAttributes);
        Assert.AreEqual(0, bothNull.HtmlAttributes.Count);
    }

    [TestMethod]
    public async System.Threading.Tasks.Task MergeShouldComposeCallbacksInvokingFirstThenSecond()
    {
        var order = new List<string>();

        var first = new BitProModalParameters
        {
            OnDismiss = Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(
                new object(), () => order.Add("dismiss-first")),
            OnOverlayClick = Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(
                new object(), () => order.Add("overlay-first")),
            OnOpen = Microsoft.AspNetCore.Components.EventCallback.Factory.Create(
                new object(), () => order.Add("open-first")),
        };
        var second = new BitProModalParameters
        {
            OnDismiss = Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(
                new object(), () => order.Add("dismiss-second")),
            OnOverlayClick = Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(
                new object(), () => order.Add("overlay-second")),
            OnOpen = Microsoft.AspNetCore.Components.EventCallback.Factory.Create(
                new object(), () => order.Add("open-second")),
        };

        var merged = BitProModalParameters.Merge(first, second)!;

        await merged.OnDismiss.InvokeAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        await merged.OnOverlayClick.InvokeAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        await merged.OnOpen.InvokeAsync();

        CollectionAssert.AreEqual(
            new[] { "dismiss-first", "dismiss-second", "overlay-first", "overlay-second", "open-first", "open-second" },
            order);
    }

    [TestMethod]
    public void MergeShouldPreserveEmptyCallbackContractWhenBothInputsAreEmpty()
    {
        var first = new BitProModalParameters();
        var second = new BitProModalParameters();

        var merged = BitProModalParameters.Merge(first, second)!;

        Assert.IsFalse(merged.OnDismiss.HasDelegate);
        Assert.IsFalse(merged.OnOverlayClick.HasDelegate);
        Assert.IsFalse(merged.OnOpen.HasDelegate);
    }
}

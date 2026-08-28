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
    public void MergeShouldCarryEverySizeAcrossWithTheFirstWinning()
    {
        var first = new BitModalParameters { Width = "20rem", MaxWidth = "28rem" };
        var second = new BitModalParameters { Width = "30rem", Height = "12rem", MaxHeight = "16rem" };

        var merged = BitModalParameters.Merge(first, second)!;

        Assert.AreEqual("20rem", merged.Width);      // first wins
        Assert.AreEqual("28rem", merged.MaxWidth);   // only set on first
        Assert.AreEqual("12rem", merged.Height);     // only set on second
        Assert.AreEqual("16rem", merged.MaxHeight);  // only set on second
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
    public void MergeShouldFallBackToSecondForTheUnsetDialogBehaviorFlags()
    {
        var first = new BitModalParameters { NoAutoFocus = true };
        var second = new BitModalParameters
        {
            NoAutoFocus = false,
            NoDismissOnEscape = true,
            NoFocusTrap = true,
            NoRestoreFocus = true,
            AriaLabel = "Cascaded label",
        };

        var merged = BitModalParameters.Merge(first, second)!;

        Assert.AreEqual(true, merged.NoAutoFocus);        // first wins
        Assert.AreEqual(true, merged.NoDismissOnEscape);  // only set on second
        Assert.AreEqual(true, merged.NoFocusTrap);        // only set on second
        Assert.AreEqual(true, merged.NoRestoreFocus);     // only set on second
        Assert.AreEqual("Cascaded label", merged.AriaLabel);
    }

    [TestMethod]
    public void MergeShouldKeepAnUnsetFlagUnset()
    {
        // null is "not set", which is what lets the BitModal default (or the value it was given on the
        // component itself) stand rather than being overridden with a false nobody asked for.
        var merged = BitModalParameters.Merge(new BitModalParameters(), new BitModalParameters())!;

        Assert.IsNull(merged.NoAutoFocus);
        Assert.IsNull(merged.NoDismissOnEscape);
        Assert.IsNull(merged.NoFocusTrap);
        Assert.IsNull(merged.NoRestoreFocus);
        Assert.IsNull(merged.AriaModal);
        Assert.IsNull(merged.ShowOverlay);
        Assert.IsNull(merged.AriaLabel);
    }

    [TestMethod]
    public void MergeShouldReportNoDelegateWhenNeitherSourceHasOne()
    {
        var merged = BitModalParameters.Merge(new BitModalParameters(), new BitModalParameters())!;

        Assert.IsFalse(merged.OnDismiss.HasDelegate);
        Assert.IsFalse(merged.OnOpen.HasDelegate);
        Assert.IsFalse(merged.OnOverlayClick.HasDelegate);
    }

    [TestMethod]
    public async System.Threading.Tasks.Task MergeShouldComposeTheOnOpenCallbacksInvokingFirstThenSecond()
    {
        var order = new List<string>();

        var first = new BitModalParameters
        {
            OnOpen = Microsoft.AspNetCore.Components.EventCallback.Factory.Create(new object(), () => order.Add("open-first")),
        };
        var second = new BitModalParameters
        {
            OnOpen = Microsoft.AspNetCore.Components.EventCallback.Factory.Create(new object(), () => order.Add("open-second")),
        };

        var merged = BitModalParameters.Merge(first, second)!;

        Assert.IsTrue(merged.OnOpen.HasDelegate);

        await merged.OnOpen.InvokeAsync();

        CollectionAssert.AreEqual(new[] { "open-first", "open-second" }, order);
    }

    [TestMethod]
    public void MergeShouldMergeTheClassesAndStylesOfEachPart()
    {
        var first = new BitModalParameters
        {
            Classes = new BitModalClassStyles { Root = "first-root" },
            Styles = new BitModalClassStyles { Overlay = "color:red" },
        };
        var second = new BitModalParameters
        {
            Classes = new BitModalClassStyles { Root = "second-root", Content = "second-content" },
            Styles = new BitModalClassStyles { Overlay = "color:green", Content = "color:blue" },
        };

        var merged = BitModalParameters.Merge(first, second)!;

        Assert.AreEqual("first-root", merged.Classes!.Root);
        Assert.AreEqual("second-content", merged.Classes!.Content);
        Assert.AreEqual("color:red", merged.Styles!.Overlay);
        Assert.AreEqual("color:blue", merged.Styles!.Content);
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

    [TestMethod]
    public void MergeShouldCarryTheChromeAcrossWithTheFirstWinning()
    {
        var first = new BitModalParameters
        {
            HeaderText = "first",
            ShowCloseButton = true,
            CloseButtonTitle = "first-close",
            Visibility = BitVisibility.Hidden,
        };
        var second = new BitModalParameters
        {
            HeaderText = "second",
            FooterText = "second-footer",
            CloseIconName = "Cancel",
            Visibility = BitVisibility.Collapsed,
        };

        var merged = BitModalParameters.Merge(first, second)!;

        Assert.AreEqual("first", merged.HeaderText);           // first wins
        Assert.AreEqual("first-close", merged.CloseButtonTitle); // only set on first
        Assert.AreEqual(true, merged.ShowCloseButton);           // only set on first
        Assert.AreEqual("second-footer", merged.FooterText);     // only set on second
        Assert.AreEqual("Cancel", merged.CloseIconName);         // only set on second
        Assert.AreEqual(BitVisibility.Hidden, merged.Visibility);
    }

    [TestMethod]
    public void MergeShouldCarryTheLayoutAndScrollHandlingAcrossWithTheFirstWinning()
    {
        var first = new BitModalParameters
        {
            Position = BitPosition.TopRight,
            AbsolutePosition = true,
            Draggable = true,
        };
        var second = new BitModalParameters
        {
            Position = BitPosition.BottomLeft,
            ModeFull = true,
            Modeless = true,
            NoBorder = true,
            FullSize = true,
            AutoToggleScroll = true,
            DragElementSelector = "#handle",
        };

        var merged = BitModalParameters.Merge(first, second)!;

        Assert.AreEqual(BitPosition.TopRight, merged.Position); // first wins
        Assert.AreEqual(true, merged.AbsolutePosition);
        Assert.AreEqual(true, merged.Draggable);
        Assert.AreEqual(true, merged.ModeFull);
        Assert.AreEqual(true, merged.Modeless);
        Assert.AreEqual(true, merged.NoBorder);
        Assert.AreEqual(true, merged.FullSize);
        Assert.AreEqual(true, merged.AutoToggleScroll);
        Assert.AreEqual("#handle", merged.DragElementSelector);
    }

    [TestMethod]
    public void MergeShouldMergeTheClassesAndStylesOfEveryChromePart()
    {
        var first = new BitModalParameters
        {
            Classes = new BitModalClassStyles { HeaderContainer = "first-hcn", Body = "first-bdy" },
            Styles = new BitModalClassStyles { Header = "color:red" },
        };
        var second = new BitModalParameters
        {
            Classes = new BitModalClassStyles { HeaderContainer = "second-hcn", Footer = "second-fcn", CloseIcon = "second-cic" },
            Styles = new BitModalClassStyles { Header = "color:green", CloseButton = "color:blue" },
        };

        var merged = BitModalParameters.Merge(first, second)!;

        Assert.AreEqual("first-hcn", merged.Classes!.HeaderContainer); // first wins
        Assert.AreEqual("first-bdy", merged.Classes.Body);             // only set on first
        Assert.AreEqual("second-fcn", merged.Classes.Footer);          // only set on second
        Assert.AreEqual("second-cic", merged.Classes.CloseIcon);       // only set on second
        Assert.AreEqual("color:red", merged.Styles!.Header);           // first wins
        Assert.AreEqual("color:blue", merged.Styles.CloseButton);      // only set on second
    }
}

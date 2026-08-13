using System;
using System.Globalization;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Layouts.Grid;

[TestClass]
public class BitGridItemTests : BunitTestContext
{
    [TestMethod]
    public void BitGridItemShouldRenderExpectedElement()
    {
        var component = RenderComponent<BitGridItem>();

        var root = component.Find(".bit-grd-itm");

        Assert.AreEqual("DIV", root.TagName);
        Assert.AreEqual("bit-grd-itm", root.ClassName);
    }

    [TestMethod]
    public void BitGridItemShouldRenderNoLayoutStyleOfItsOwnWhenNothingWasAskedFor()
    {
        // Everything an untouched item needs is inherited from the grid, so its own style stays empty and
        // the markup of a grid of plain items stays as small as it can be.
        var component = RenderComponent<BitGridItem>();

        Assert.IsNull(component.Find(".bit-grd-itm").GetAttribute("style"));
    }

    [TestMethod]
    public void BitGridItemShouldNotThrowWhenItIsRenderedOutsideAGrid()
    {
        // Reading the column counts off the parent used to throw a NullReferenceException here.
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.ColumnSpan, 3);
            parameters.AddChildContent("<span class='child'>Orphan</span>");
        });

        Assert.IsNotNull(component.Find(".bit-grd-itm"));
        Assert.AreEqual("Orphan", component.Find(".child").TextContent);
    }

    [TestMethod]
    [DataRow("li")]
    [DataRow("section")]
    [DataRow("article")]
    public void BitGridItemShouldRespectElement(string element)
    {
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.Element, element);
        });

        Assert.AreEqual(element.ToUpperInvariant(), component.Find(".bit-grd-itm").TagName);
    }

    [TestMethod]
    public void BitGridItemShouldRenderChildContent()
    {
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.AddChildContent("<div class='child'>Hello</div>");
        });

        Assert.AreEqual("Hello", component.Find(".child").TextContent);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(4)]
    [DataRow(12)]
    public void BitGridItemShouldRespectColumnSpan(int columnSpan)
    {
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.ColumnSpan, columnSpan);
        });

        StringAssert.Contains(component.Find(".bit-grd-itm").GetAttribute("style"), $"--bit-grd-span:{columnSpan}");
    }

    [TestMethod]
    public void BitGridItemShouldGuardTheColumnSpanAgainstNegativeValues()
    {
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.ColumnSpan, -2);
        });

        StringAssert.Contains(component.Find(".bit-grd-itm").GetAttribute("style"), "--bit-grd-span:0");
    }

    [TestMethod]
    public void BitGridItemShouldRenderEveryResponsiveSpan()
    {
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.Xs, 12);
            parameters.Add(p => p.Sm, 6);
            parameters.Add(p => p.Md, 4);
            parameters.Add(p => p.Lg, 3);
            parameters.Add(p => p.Xl, 2);
            parameters.Add(p => p.Xxl, 1);
        });

        var style = component.Find(".bit-grd-itm").GetAttribute("style");

        StringAssert.Contains(style, "--bit-grd-xs:12");
        StringAssert.Contains(style, "--bit-grd-sm:6");
        StringAssert.Contains(style, "--bit-grd-md:4");
        StringAssert.Contains(style, "--bit-grd-lg:3");
        StringAssert.Contains(style, "--bit-grd-xl:2");
        StringAssert.Contains(style, "--bit-grd-xxl:1");
    }

    [TestMethod]
    public void BitGridItemShouldOnlyRenderTheResponsiveSpansThatWereAskedFor()
    {
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.ColumnSpan, 12);
            parameters.Add(p => p.Md, 6);
        });

        var style = component.Find(".bit-grd-itm").GetAttribute("style");

        StringAssert.Contains(style, "--bit-grd-span:12");
        StringAssert.Contains(style, "--bit-grd-md:6");

        // The breakpoints in between are carried upwards by the stylesheet, so rendering them here would
        // only repeat what the cascade already knows.
        Assert.IsFalse(style!.Contains("--bit-grd-xs"));
        Assert.IsFalse(style.Contains("--bit-grd-sm"));
        Assert.IsFalse(style.Contains("--bit-grd-lg"));
        Assert.IsFalse(style.Contains("--bit-grd-xl"));
        Assert.IsFalse(style.Contains("--bit-grd-xxl"));
    }

    [TestMethod]
    public void BitGridItemShouldNotBeShownWhenItAsksForNoColumns()
    {
        // A span of no columns used to leave an item of no width whose content spilled out of it anyway. Asking
        // for none of the grid is asking not to be there, which is what every other grid of this kind means by it.
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.ColumnSpan, 0);
        });

        Assert.IsTrue(component.Find(".bit-grd-itm").ClassList.Contains("bit-grd-itm-non"));
    }

    [TestMethod]
    public void BitGridItemShouldNotBeShownWhenItsColumnsAreGuardedAwayToNone()
    {
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.ColumnSpan, -3);
        });

        Assert.IsTrue(component.Find(".bit-grd-itm").ClassList.Contains("bit-grd-itm-non"));
    }

    [TestMethod]
    public void BitGridItemShouldBeShownWhenItAsksForColumns()
    {
        var withSpan = RenderComponent<BitGridItem>(parameters => parameters.Add(p => p.ColumnSpan, 4));
        var withNothing = RenderComponent<BitGridItem>();

        Assert.IsFalse(withSpan.Find(".bit-grd-itm").ClassList.Contains("bit-grd-itm-non"));

        // An item that named no span at all follows the Span of its grid, which is not the same thing as
        // asking for none of it.
        Assert.IsFalse(withNothing.Find(".bit-grd-itm").ClassList.Contains("bit-grd-itm-non"));
    }

    [TestMethod]
    public void BitGridItemShouldOnlyBeHiddenAtTheBreakpointsWhoseSpanIsNone()
    {
        // Being hidden is the one thing the breakpoint above does not restate on its own, so each breakpoint the
        // item is away for has to be named, and the ones it comes back for must not be.
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.ColumnSpan, 0);
            parameters.Add(p => p.Md, 4);
        });

        var classList = component.Find(".bit-grd-itm").ClassList;

        Assert.IsTrue(classList.Contains("bit-grd-itm-non-xs"));
        Assert.IsTrue(classList.Contains("bit-grd-itm-non-sm"));
        Assert.IsFalse(classList.Contains("bit-grd-itm-non-md"));
        Assert.IsFalse(classList.Contains("bit-grd-itm-non-lg"));
        Assert.IsFalse(classList.Contains("bit-grd-itm-non-xl"));
        Assert.IsFalse(classList.Contains("bit-grd-itm-non-xxl"));

        // The all-or-nothing class of an item that never changes must not be handed out alongside them.
        Assert.IsFalse(classList.Contains("bit-grd-itm-non"));
    }

    [TestMethod]
    public void BitGridItemShouldCarryTheHidingUpwardsUntilASpanReplacesIt()
    {
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.ColumnSpan, 12);
            parameters.Add(p => p.Md, 0);
        });

        var classList = component.Find(".bit-grd-itm").ClassList;

        Assert.IsFalse(classList.Contains("bit-grd-itm-non-xs"));
        Assert.IsFalse(classList.Contains("bit-grd-itm-non-sm"));
        Assert.IsTrue(classList.Contains("bit-grd-itm-non-md"));
        Assert.IsTrue(classList.Contains("bit-grd-itm-non-lg"));
        Assert.IsTrue(classList.Contains("bit-grd-itm-non-xl"));
        Assert.IsTrue(classList.Contains("bit-grd-itm-non-xxl"));
    }

    [TestMethod]
    public void BitGridItemShouldBeHiddenForASingleBandWhenTheNextBreakpointGivesItColumnsBack()
    {
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.ColumnSpan, 6);
            parameters.Add(p => p.Sm, 0);
            parameters.Add(p => p.Md, 6);
        });

        var classList = component.Find(".bit-grd-itm").ClassList;

        Assert.IsTrue(classList.Contains("bit-grd-itm-non-sm"));
        Assert.IsFalse(classList.Contains("bit-grd-itm-non-xs"));
        Assert.IsFalse(classList.Contains("bit-grd-itm-non-md"));
    }

    [TestMethod]
    public void BitGridItemShouldNotBeHiddenWhileItIsSizedBySomethingOtherThanItsColumns()
    {
        // Being exactly as wide as its content, or taking whatever the row has left, is a width of its own and
        // has nothing to do with a count of columns, so a leftover span of none must not take the item away.
        var auto = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.Auto, true);
            parameters.Add(p => p.ColumnSpan, 0);
        });

        var grow = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.Grow, true);
            parameters.Add(p => p.ColumnSpan, 0);
        });

        Assert.IsFalse(auto.Find(".bit-grd-itm").ClassList.Contains("bit-grd-itm-non"));
        Assert.IsFalse(grow.Find(".bit-grd-itm").ClassList.Contains("bit-grd-itm-non"));
    }

    [TestMethod]
    public void BitGridItemShouldOnlyBeHiddenAtTheBreakpointsWhereItIsBackOnItsColumns()
    {
        // The item is as wide as its content up to the medium breakpoint and measured in columns from it, and it
        // was given none of them, so it is away from there and shown below it.
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.Auto, true);
            parameters.Add(p => p.AutoMd, false);
            parameters.Add(p => p.ColumnSpan, 0);
        });

        var classList = component.Find(".bit-grd-itm").ClassList;

        Assert.IsFalse(classList.Contains("bit-grd-itm-non-xs"));
        Assert.IsFalse(classList.Contains("bit-grd-itm-non-sm"));
        Assert.IsTrue(classList.Contains("bit-grd-itm-non-md"));
        Assert.IsTrue(classList.Contains("bit-grd-itm-non-xxl"));
    }

    [TestMethod]
    public void BitGridItemShouldRelayoutWhenTheSpanThatHidesItChanges()
    {
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.ColumnSpan, 0);
        });

        Assert.IsTrue(component.Find(".bit-grd-itm").ClassList.Contains("bit-grd-itm-non"));

        component.Render(parameters => parameters.Add(p => p.ColumnSpan, 4));

        Assert.IsFalse(component.Find(".bit-grd-itm").ClassList.Contains("bit-grd-itm-non"));
    }

    [TestMethod]
    public void BitGridItemShouldRelayoutWhenAResponsiveSpanStopsHidingIt()
    {
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.ColumnSpan, 6);
            parameters.Add(p => p.Md, 0);
        });

        Assert.IsTrue(component.Find(".bit-grd-itm").ClassList.Contains("bit-grd-itm-non-md"));

        component.Render(parameters => parameters.Add(p => p.Md, 3));

        Assert.IsFalse(component.Find(".bit-grd-itm").ClassList.Contains("bit-grd-itm-non-md"));
    }

    [TestMethod]
    public void BitGridItemShouldRespectOffset()
    {
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.Offset, 3);
        });

        var root = component.Find(".bit-grd-itm");

        StringAssert.Contains(root.GetAttribute("style"), "--bit-grd-off:3");
        Assert.IsTrue(root.ClassList.Contains("bit-grd-itm-off"));
    }

    [TestMethod]
    public void BitGridItemShouldRenderEveryResponsiveOffset()
    {
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.OffsetXs, 0);
            parameters.Add(p => p.OffsetSm, 1);
            parameters.Add(p => p.OffsetMd, 2);
            parameters.Add(p => p.OffsetLg, 3);
            parameters.Add(p => p.OffsetXl, 4);
            parameters.Add(p => p.OffsetXxl, 5);
        });

        var style = component.Find(".bit-grd-itm").GetAttribute("style");

        StringAssert.Contains(style, "--bit-grd-off-xs:0");
        StringAssert.Contains(style, "--bit-grd-off-sm:1");
        StringAssert.Contains(style, "--bit-grd-off-md:2");
        StringAssert.Contains(style, "--bit-grd-off-lg:3");
        StringAssert.Contains(style, "--bit-grd-off-xl:4");
        StringAssert.Contains(style, "--bit-grd-off-xxl:5");

        Assert.IsTrue(component.Find(".bit-grd-itm").ClassList.Contains("bit-grd-itm-off"));
    }

    [TestMethod]
    public void BitGridItemShouldGuardTheOffsetsAgainstNegativeValues()
    {
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.Offset, -4);
            parameters.Add(p => p.OffsetMd, -1);
        });

        var style = component.Find(".bit-grd-itm").GetAttribute("style");

        StringAssert.Contains(style, "--bit-grd-off:0");
        StringAssert.Contains(style, "--bit-grd-off-md:0");
    }

    [TestMethod]
    public void BitGridItemShouldNotClaimTheStartMarginOfAnItemThatAskedForNoOffset()
    {
        // The offset is a start margin, so it is only declared for the items that asked for one. An item
        // that did not keeps whatever margin a stylesheet of the application gives it.
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.ColumnSpan, 4);
        });

        Assert.IsFalse(component.Find(".bit-grd-itm").ClassList.Contains("bit-grd-itm-off"));
    }

    [TestMethod]
    public void BitGridItemShouldRespectAutoOffset()
    {
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.AutoOffset, true);
        });

        Assert.IsTrue(component.Find(".bit-grd-itm").ClassList.Contains("bit-grd-itm-aof"));
    }

    [TestMethod]
    public void BitGridItemShouldNotClaimTheStartMarginOfAnItemThatAskedForNoAutoOffset()
    {
        var component = RenderComponent<BitGridItem>();

        Assert.IsFalse(component.Find(".bit-grd-itm").ClassList.Contains("bit-grd-itm-aof"));
    }

    [TestMethod]
    public void BitGridItemShouldLetTheAutoOffsetWinOverACountedOne()
    {
        // Claiming the whole of the room left before the item and measuring a number of columns out of it are
        // two ways of writing the same margin, so only one of them can be handed out.
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.AutoOffset, true);
            parameters.Add(p => p.Offset, 3);
            parameters.Add(p => p.OffsetMd, 2);
        });

        var classList = component.Find(".bit-grd-itm").ClassList;

        Assert.IsTrue(classList.Contains("bit-grd-itm-aof"));
        Assert.IsFalse(classList.Contains("bit-grd-itm-off"));
    }

    [TestMethod]
    public void BitGridItemShouldOnlyClaimTheFreeRoomAtTheBreakpointsItAskedFor()
    {
        // A breakpoint can only be talked out of what the one below it was doing by naming its own, so an item
        // that only claims the room left before it at some sizes has to name each of those sizes, and carries
        // the counted margin the rest of the time.
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.AutoOffsetMd, true);
        });

        var classList = component.Find(".bit-grd-itm").ClassList;

        Assert.IsFalse(classList.Contains("bit-grd-itm-aof-xs"));
        Assert.IsFalse(classList.Contains("bit-grd-itm-aof-sm"));
        Assert.IsTrue(classList.Contains("bit-grd-itm-aof-md"));
        Assert.IsTrue(classList.Contains("bit-grd-itm-aof-lg"));
        Assert.IsTrue(classList.Contains("bit-grd-itm-aof-xl"));
        Assert.IsTrue(classList.Contains("bit-grd-itm-aof-xxl"));

        // The margin itself hangs off the counted class, which is what the breakpoints below the medium one
        // are left with, so it has to be handed out alongside them.
        Assert.IsTrue(classList.Contains("bit-grd-itm-off"));

        // The all-or-nothing class of an item that never changes must not be handed out with them.
        Assert.IsFalse(classList.Contains("bit-grd-itm-aof"));
    }

    [TestMethod]
    public void BitGridItemShouldHandTheRoomBackToTheCountedOffsetWhenItIsToldToStop()
    {
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.AutoOffset, true);
            parameters.Add(p => p.AutoOffsetMd, false);
            parameters.Add(p => p.Offset, 2);
        });

        var root = component.Find(".bit-grd-itm");
        var classList = root.ClassList;

        Assert.IsTrue(classList.Contains("bit-grd-itm-aof-xs"));
        Assert.IsTrue(classList.Contains("bit-grd-itm-aof-sm"));
        Assert.IsFalse(classList.Contains("bit-grd-itm-aof-md"));
        Assert.IsFalse(classList.Contains("bit-grd-itm-aof-xxl"));

        Assert.IsTrue(classList.Contains("bit-grd-itm-off"));
        StringAssert.Contains(root.GetAttribute("style"), "--bit-grd-off:2");
    }

    [TestMethod]
    public void BitGridItemShouldNotClaimTheStartMarginOfAnItemWhoseAutoOffsetsAllSayNo()
    {
        // Turning the push to the end edge off at a breakpoint is not asking for a margin, so an item that only
        // ever said no keeps its own margin exactly as an item that said nothing at all does.
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.AutoOffsetMd, false);
        });

        var classList = component.Find(".bit-grd-itm").ClassList;

        Assert.AreEqual(1, classList.Length);
        Assert.IsFalse(classList.Contains("bit-grd-itm-off"));
        Assert.IsFalse(classList.Contains("bit-grd-itm-aof"));
    }

    [TestMethod]
    public void BitGridItemShouldKeepTheSingleAutoOffsetClassOfAnItemThatNeverChangesItsMind()
    {
        // The item that claims the free room at every size says so once, so the markup of every grid written
        // before the responsive auto offset existed is left exactly as it was.
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.AutoOffset, true);
        });

        var classList = component.Find(".bit-grd-itm").ClassList;

        Assert.AreEqual(2, classList.Length);
        Assert.IsTrue(classList.Contains("bit-grd-itm-aof"));
    }

    [TestMethod]
    public void BitGridItemShouldRelayoutWhenAResponsiveAutoOffsetChanges()
    {
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.Offset, 2);
            parameters.Add(p => p.AutoOffsetMd, true);
        });

        var classList = component.Find(".bit-grd-itm").ClassList;

        Assert.IsTrue(classList.Contains("bit-grd-itm-aof-md"));
        Assert.IsTrue(classList.Contains("bit-grd-itm-off"));

        component.Render(parameters => parameters.Add(p => p.AutoOffsetMd, false));

        classList = component.Find(".bit-grd-itm").ClassList;

        Assert.IsFalse(classList.Contains("bit-grd-itm-aof-md"));

        // The counted offset the item still asks for is what it falls back to.
        Assert.IsTrue(classList.Contains("bit-grd-itm-off"));
    }

    [TestMethod]
    public void BitGridItemShouldRelayoutWhenTheAutoOffsetChanges()
    {
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.Offset, 2);
        });

        Assert.IsTrue(component.Find(".bit-grd-itm").ClassList.Contains("bit-grd-itm-off"));

        component.Render(parameters => parameters.Add(p => p.AutoOffset, true));

        var classList = component.Find(".bit-grd-itm").ClassList;

        Assert.IsTrue(classList.Contains("bit-grd-itm-aof"));
        Assert.IsFalse(classList.Contains("bit-grd-itm-off"));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(3)]
    [DataRow(-1)]
    public void BitGridItemShouldRespectOrder(int order)
    {
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.Order, order);
        });

        var root = component.Find(".bit-grd-itm");

        // A negative order is how a single item is moved ahead of the ones that left theirs alone, so it
        // must survive as it was given.
        StringAssert.Contains(root.GetAttribute("style"), $"--bit-grd-ord:{order}");
        Assert.IsTrue(root.ClassList.Contains("bit-grd-itm-ord"));
    }

    // The culture it swaps in is process wide for the length of the render, so it is kept away from anything
    // that could be reading a number at the same time.
    [TestMethod, DoNotParallelize]
    public void BitGridItemShouldRenderANegativeOrderTheBrowserCanRead()
    {
        // Not every culture writes a negative number with the hyphen CSS expects: several of them use the
        // real minus sign, which would make the whole declaration unreadable to the browser.
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NegativeSign = "−";

        var originalCulture = CultureInfo.CurrentCulture;
        var originalDefault = CultureInfo.DefaultThreadCurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;

            var component = RenderComponent<BitGridItem>(parameters =>
            {
                parameters.Add(p => p.Order, -1);
            });

            StringAssert.Contains(component.Find(".bit-grd-itm").GetAttribute("style"), "--bit-grd-ord:-1");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.DefaultThreadCurrentCulture = originalDefault;
        }
    }

    [TestMethod]
    public void BitGridItemShouldRenderEveryResponsiveOrder()
    {
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.OrderXs, -1);
            parameters.Add(p => p.OrderSm, 1);
            parameters.Add(p => p.OrderMd, 2);
            parameters.Add(p => p.OrderLg, 3);
            parameters.Add(p => p.OrderXl, 4);
            parameters.Add(p => p.OrderXxl, 5);
        });

        var style = component.Find(".bit-grd-itm").GetAttribute("style");

        StringAssert.Contains(style, "--bit-grd-ord-xs:-1");
        StringAssert.Contains(style, "--bit-grd-ord-sm:1");
        StringAssert.Contains(style, "--bit-grd-ord-md:2");
        StringAssert.Contains(style, "--bit-grd-ord-lg:3");
        StringAssert.Contains(style, "--bit-grd-ord-xl:4");
        StringAssert.Contains(style, "--bit-grd-ord-xxl:5");

        Assert.IsTrue(component.Find(".bit-grd-itm").ClassList.Contains("bit-grd-itm-ord"));
    }

    [TestMethod]
    public void BitGridItemShouldNotClaimTheOrderOfAnItemThatAskedForNone()
    {
        var component = RenderComponent<BitGridItem>();

        Assert.IsFalse(component.Find(".bit-grd-itm").ClassList.Contains("bit-grd-itm-ord"));
    }

    [TestMethod]
    public void BitGridItemShouldRespectAuto()
    {
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.Auto, true);
        });

        Assert.IsTrue(component.Find(".bit-grd-itm").ClassList.Contains("bit-grd-itm-atc"));
    }

    [TestMethod]
    public void BitGridItemShouldRespectGrow()
    {
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.Grow, true);
        });

        Assert.IsTrue(component.Find(".bit-grd-itm").ClassList.Contains("bit-grd-itm-grw"));
    }

    [TestMethod]
    public void BitGridItemShouldLetAutoWinOverGrow()
    {
        // An item cannot both be as wide as its content and take everything that is left, so the more
        // specific of the two wins instead of the two of them fighting in the stylesheet.
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.Auto, true);
            parameters.Add(p => p.Grow, true);
        });

        var classList = component.Find(".bit-grd-itm").ClassList;

        Assert.IsTrue(classList.Contains("bit-grd-itm-atc"));
        Assert.IsFalse(classList.Contains("bit-grd-itm-grw"));
    }

    [TestMethod]
    public void BitGridItemShouldKeepTheSingleSizingClassOfAnItemThatNeverChangesItsMind()
    {
        // The item that takes up room the same way at every size says so once, so the markup of every grid
        // written before the responsive sizing existed is left exactly as it was.
        var auto = RenderComponent<BitGridItem>(parameters => parameters.Add(p => p.Auto, true));
        var grow = RenderComponent<BitGridItem>(parameters => parameters.Add(p => p.Grow, true));

        var autoClasses = auto.Find(".bit-grd-itm").ClassList;
        var growClasses = grow.Find(".bit-grd-itm").ClassList;

        Assert.AreEqual(2, autoClasses.Length);
        Assert.IsTrue(autoClasses.Contains("bit-grd-itm-atc"));

        Assert.AreEqual(2, growClasses.Length);
        Assert.IsTrue(growClasses.Contains("bit-grd-itm-grw"));
    }

    [TestMethod]
    public void BitGridItemShouldNameTheWayItTakesUpRoomAtEveryBreakpointOnceItChanges()
    {
        // A breakpoint can only be talked out of what the one below it was doing by naming its own, so an item
        // that changes its mind has to answer for all six of them rather than only for the one that changed.
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.ColumnSpan, 12);
            parameters.Add(p => p.AutoMd, true);
        });

        var classList = component.Find(".bit-grd-itm").ClassList;

        Assert.IsTrue(classList.Contains("bit-grd-itm-spn-xs"));
        Assert.IsTrue(classList.Contains("bit-grd-itm-spn-sm"));
        Assert.IsTrue(classList.Contains("bit-grd-itm-atc-md"));
        Assert.IsTrue(classList.Contains("bit-grd-itm-atc-lg"));
        Assert.IsTrue(classList.Contains("bit-grd-itm-atc-xl"));
        Assert.IsTrue(classList.Contains("bit-grd-itm-atc-xxl"));

        // The single class of an item that never changes must not be handed out alongside them.
        Assert.IsFalse(classList.Contains("bit-grd-itm-atc"));
        Assert.IsFalse(classList.Contains("bit-grd-itm-grw"));

        // The span it falls back to below the medium breakpoint is still the one it was given.
        StringAssert.Contains(component.Find(".bit-grd-itm").GetAttribute("style"), "--bit-grd-span:12");
    }

    [TestMethod]
    public void BitGridItemShouldCarryTheWayItTakesUpRoomUpwardsUntilItIsReplaced()
    {
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.GrowXs, true);
            parameters.Add(p => p.AutoLg, true);
        });

        var classList = component.Find(".bit-grd-itm").ClassList;

        Assert.IsTrue(classList.Contains("bit-grd-itm-grw-xs"));
        Assert.IsTrue(classList.Contains("bit-grd-itm-grw-sm"));
        Assert.IsTrue(classList.Contains("bit-grd-itm-grw-md"));
        Assert.IsTrue(classList.Contains("bit-grd-itm-atc-lg"));
        Assert.IsTrue(classList.Contains("bit-grd-itm-atc-xl"));
        Assert.IsTrue(classList.Contains("bit-grd-itm-atc-xxl"));
    }

    [TestMethod]
    public void BitGridItemShouldGiveAnItemItsColumnsBackWhenItIsToldToStop()
    {
        // False is the only way back onto the column tracks, which is what an item that is sized to its content
        // on a phone and measured in columns on a desktop is written with.
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.Auto, true);
            parameters.Add(p => p.AutoMd, false);
            parameters.Add(p => p.Md, 3);
        });

        var classList = component.Find(".bit-grd-itm").ClassList;

        Assert.IsTrue(classList.Contains("bit-grd-itm-atc-xs"));
        Assert.IsTrue(classList.Contains("bit-grd-itm-atc-sm"));
        Assert.IsTrue(classList.Contains("bit-grd-itm-spn-md"));
        Assert.IsTrue(classList.Contains("bit-grd-itm-spn-lg"));
        Assert.IsTrue(classList.Contains("bit-grd-itm-spn-xxl"));

        StringAssert.Contains(component.Find(".bit-grd-itm").GetAttribute("style"), "--bit-grd-md:3");
    }

    [TestMethod]
    public void BitGridItemShouldLeaveTheOtherWayOfLeavingTheTracksAloneWhenOneOfThemIsTurnedOff()
    {
        // Turning off the growth of an item that was never sized to its content must not make it auto sized,
        // and turning off the auto sizing of an item that grows must leave it growing.
        var stoppedGrowing = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.Grow, true);
            parameters.Add(p => p.GrowMd, false);
        });

        Assert.IsTrue(stoppedGrowing.Find(".bit-grd-itm").ClassList.Contains("bit-grd-itm-spn-md"));

        var keptGrowing = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.Grow, true);
            parameters.Add(p => p.AutoMd, false);
        });

        Assert.IsTrue(keptGrowing.Find(".bit-grd-itm").ClassList.Contains("bit-grd-itm-grw-md"));
    }

    [TestMethod]
    public void BitGridItemShouldLetAutoWinOverGrowAtEveryBreakpoint()
    {
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.AutoSm, true);
            parameters.Add(p => p.GrowSm, true);
        });

        var classList = component.Find(".bit-grd-itm").ClassList;

        Assert.IsTrue(classList.Contains("bit-grd-itm-atc-sm"));
        Assert.IsFalse(classList.Contains("bit-grd-itm-grw-sm"));
    }

    [TestMethod]
    public void BitGridItemShouldRelayoutWhenAResponsiveSizingChanges()
    {
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.AutoMd, true);
        });

        Assert.IsTrue(component.Find(".bit-grd-itm").ClassList.Contains("bit-grd-itm-atc-md"));

        component.Render(parameters => parameters.Add(p => p.AutoMd, false));

        var classList = component.Find(".bit-grd-itm").ClassList;

        Assert.IsTrue(classList.Contains("bit-grd-itm-spn-md"));
        Assert.IsFalse(classList.Contains("bit-grd-itm-atc-md"));
    }

    [TestMethod]
    [DataRow(BitAlignment.Start, "flex-start")]
    [DataRow(BitAlignment.End, "flex-end")]
    [DataRow(BitAlignment.Center, "center")]
    [DataRow(BitAlignment.Baseline, "baseline")]
    [DataRow(BitAlignment.Stretch, "stretch")]
    public void BitGridItemShouldRespectAlignSelf(BitAlignment alignment, string expected)
    {
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.AlignSelf, alignment);
        });

        StringAssert.Contains(component.Find(".bit-grd-itm").GetAttribute("style"), $"align-self:{expected}");
    }

    [TestMethod]
    [DataRow(BitAlignment.SpaceBetween)]
    [DataRow(BitAlignment.SpaceAround)]
    [DataRow(BitAlignment.SpaceEvenly)]
    public void BitGridItemShouldIgnoreTheAlignSelvesThatHaveNoMeaningOnTheCrossAxis(BitAlignment alignment)
    {
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.AlignSelf, alignment);
        });

        Assert.IsFalse((component.Find(".bit-grd-itm").GetAttribute("style") ?? string.Empty).Contains("align-self"));
    }

    [TestMethod]
    public void BitGridItemShouldRelayoutWhenAResponsiveSpanChanges()
    {
        // The responsive spans used to be plain parameters with no style builder reset behind them, so an
        // item that changed one of them at runtime kept the width it was first rendered with.
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.Md, 6);
        });

        StringAssert.Contains(component.Find(".bit-grd-itm").GetAttribute("style"), "--bit-grd-md:6");

        component.Render(parameters => parameters.Add(p => p.Md, 4));

        StringAssert.Contains(component.Find(".bit-grd-itm").GetAttribute("style"), "--bit-grd-md:4");
    }

    [TestMethod]
    public void BitGridItemShouldRelayoutWhenTheColumnSpanChanges()
    {
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.ColumnSpan, 2);
        });

        StringAssert.Contains(component.Find(".bit-grd-itm").GetAttribute("style"), "--bit-grd-span:2");

        component.Render(parameters => parameters.Add(p => p.ColumnSpan, 5));

        StringAssert.Contains(component.Find(".bit-grd-itm").GetAttribute("style"), "--bit-grd-span:5");
    }

    [TestMethod]
    public void BitGridItemShouldRelayoutWhenTheOffsetChanges()
    {
        var component = RenderComponent<BitGridItem>();

        Assert.IsFalse(component.Find(".bit-grd-itm").ClassList.Contains("bit-grd-itm-off"));

        component.Render(parameters => parameters.Add(p => p.Offset, 2));

        var root = component.Find(".bit-grd-itm");

        StringAssert.Contains(root.GetAttribute("style"), "--bit-grd-off:2");
        Assert.IsTrue(root.ClassList.Contains("bit-grd-itm-off"));
    }

    [TestMethod]
    public void BitGridItemShouldRelayoutWhenTheOrderChanges()
    {
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.Order, 1);
        });

        StringAssert.Contains(component.Find(".bit-grd-itm").GetAttribute("style"), "--bit-grd-ord:1");

        component.Render(parameters => parameters.Add(p => p.Order, -1));

        StringAssert.Contains(component.Find(".bit-grd-itm").GetAttribute("style"), "--bit-grd-ord:-1");
    }

    [TestMethod]
    public void BitGridItemShouldRespectDir()
    {
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        var root = component.Find(".bit-grd-itm");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
    }

    [TestMethod]
    public void BitGridItemShouldRespectVisibility()
    {
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.Visibility, BitVisibility.Hidden);
        });

        StringAssert.Contains(component.Find(".bit-grd-itm").GetAttribute("style"), "visibility:hidden");
    }

    [TestMethod]
    public void BitGridItemShouldRespectStyleAndClass()
    {
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.Style, "background:red");
            parameters.Add(p => p.Class, "custom-item");
        });

        var root = component.Find(".bit-grd-itm");

        StringAssert.Contains(root.GetAttribute("style"), "background:red");
        Assert.IsTrue(root.ClassList.Contains("custom-item"));
    }

    [TestMethod]
    public void BitGridItemShouldRespectIsEnabled()
    {
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
        });

        Assert.IsTrue(component.Find(".bit-grd-itm").ClassList.Contains("bit-dis"));
    }

    [TestMethod]
    public void BitGridItemShouldRespectAriaLabel()
    {
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "First product");
        });

        Assert.AreEqual("First product", component.Find(".bit-grd-itm").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitGridItemShouldKeepItsSpanWhileItIsSizedToItsContent()
    {
        // Auto leaves the column tracks behind in the stylesheet rather than by throwing the span away, so an
        // item that is auto sized today can be given back its columns by a single parameter tomorrow.
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.Auto, true);
            parameters.Add(p => p.ColumnSpan, 4);
        });

        var root = component.Find(".bit-grd-itm");

        Assert.IsTrue(root.ClassList.Contains("bit-grd-itm-atc"));
        StringAssert.Contains(root.GetAttribute("style"), "--bit-grd-span:4");
    }

    [TestMethod]
    public void BitGridItemShouldRelayoutWhenItStopsBeingSizedToItsContent()
    {
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.Auto, true);
            parameters.Add(p => p.Grow, true);
        });

        Assert.IsTrue(component.Find(".bit-grd-itm").ClassList.Contains("bit-grd-itm-atc"));

        component.Render(parameters => parameters.Add(p => p.Auto, false));

        var classList = component.Find(".bit-grd-itm").ClassList;

        Assert.IsTrue(classList.Contains("bit-grd-itm-grw"));
        Assert.IsFalse(classList.Contains("bit-grd-itm-atc"));
    }

    [TestMethod]
    public void BitGridItemShouldCarryEveryLayoutParameterAtOnce()
    {
        // Nothing in the layout of an item is exclusive of anything else, so all of it has to reach the root
        // element together rather than the last one written winning.
        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.Add(p => p.ColumnSpan, 6);
            parameters.Add(p => p.Md, 4);
            parameters.Add(p => p.Offset, 1);
            parameters.Add(p => p.OffsetLg, 2);
            parameters.Add(p => p.Order, -1);
            parameters.Add(p => p.OrderMd, 2);
            parameters.Add(p => p.AlignSelf, BitAlignment.End);
        });

        var root = component.Find(".bit-grd-itm");
        var style = root.GetAttribute("style");

        StringAssert.Contains(style, "--bit-grd-span:6");
        StringAssert.Contains(style, "--bit-grd-md:4");
        StringAssert.Contains(style, "--bit-grd-off:1");
        StringAssert.Contains(style, "--bit-grd-off-lg:2");
        StringAssert.Contains(style, "--bit-grd-ord:-1");
        StringAssert.Contains(style, "--bit-grd-ord-md:2");
        StringAssert.Contains(style, "align-self:flex-end");
        Assert.IsTrue(root.ClassList.Contains("bit-grd-itm-off"));
        Assert.IsTrue(root.ClassList.Contains("bit-grd-itm-ord"));
    }

    [TestMethod]
    public void BitGridItemShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitGridHtmlAttributesTest>();

        Assert.AreEqual("bit", component.Find(".bit-grd-itm").GetAttribute("data-item-test"));
    }

    [TestMethod]
    public void BitGridItemShouldReceiveTheGridItBelongsTo()
    {
        var component = RenderComponent<BitGridItemsTest>();

        var grids = component.FindComponents<BitGrid>();
        var items = component.FindComponents<BitGridItem>();

        // The three items of the outer grid belong to it, and the item of the nested grid belongs to the
        // nested one rather than to the grid that happens to enclose them both.
        Assert.AreSame(grids[0].Instance, items[0].Instance.Parent);
        Assert.AreSame(grids[0].Instance, items[1].Instance.Parent);
        Assert.AreSame(grids[0].Instance, items[2].Instance.Parent);
        Assert.AreSame(grids[1].Instance, items[3].Instance.Parent);
    }

    [TestMethod]
    public void BitGridItemsShouldOnlyCarryTheirOwnLayoutStyle()
    {
        // The column counts of the grid used to be repeated in the inline style of every one of its items.
        // They live on the container now, so an item only carries what it asked for itself.
        var component = RenderComponent<BitGridItemsTest>();

        var items = component.FindAll(".bit-grd-itm");

        Assert.IsNull(items[0].GetAttribute("style"));
        StringAssert.Contains(items[1].GetAttribute("style"), "--bit-grd-span:2");
        Assert.IsFalse(items[1].GetAttribute("style")!.Contains("--bit-grd-cols"));
    }

    [TestMethod]
    public void BitGridItemsOfANestedGridShouldNotBeLaidOutByTheOuterOne()
    {
        var component = RenderComponent<BitGridItemsTest>();

        var grids = component.FindComponents<BitGrid>();

        Assert.AreEqual(2, grids.Count);

        // The nested grid declares its own column count, which shadows the inherited one for its items.
        StringAssert.Contains(grids[1].Find(".bit-grd").GetAttribute("style"), "--bit-grd-cols:3");
    }
}

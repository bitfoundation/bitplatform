using System.Linq;
using AngleSharp.Dom;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Navs.Pagination;

[TestClass]
public class BitPaginationTests : BunitTestContext
{
    [TestMethod]
    public void BitPaginationShouldRenderDefaultPagesAndSelected()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Add(p => p.DefaultSelectedPage, 3);
        });

        var pages = GetPageButtons(comp);

        Assert.AreEqual(5, pages.Count);
        CollectionAssert.AreEqual(new[] { "1", "2", "3", "4", "5" }, pages.Select(p => p.TextContent.Trim()).ToArray());

        var selected = comp.Find(".bit-pgn-sel");

        Assert.AreEqual("3", selected.TextContent.Trim());
        Assert.AreEqual("page", selected.GetAttribute("aria-current"));

        // Only the selected page reports aria-current.
        Assert.AreEqual(1, pages.Count(p => p.GetAttribute("aria-current") is not null));
    }

    [TestMethod]
    public void BitPaginationShouldRenderANavigationLandmarkHoldingAList()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 3);
        });

        var root = comp.Find(".bit-pgn");

        Assert.AreEqual("NAV", root.TagName);
        Assert.AreEqual("Pagination", root.GetAttribute("aria-label"));

        var list = comp.Find(".bit-pgn > ul");

        Assert.AreEqual("list", list.GetAttribute("role"));

        var items = comp.FindAll(".bit-pgn > ul > li");

        // The three pages plus the previous and next buttons.
        Assert.AreEqual(5, items.Count);
        Assert.IsTrue(items.All(i => i.GetAttribute("role") == "listitem"));
    }

    [TestMethod]
    public void BitPaginationShouldRespectAriaLabel()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 3);
            parameters.Add(p => p.AriaLabel, "Search results pages");
        });

        Assert.AreEqual("Search results pages", comp.Find(".bit-pgn").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitPaginationShouldRenderEveryButtonAsAPlainButton()
    {
        // A button with no explicit type submits the form it sits in, which a pagination must never do.
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 10);
            parameters.Add(p => p.ShowFirstButton, true);
            parameters.Add(p => p.ShowLastButton, true);
        });

        var buttons = comp.FindAll("button");

        Assert.IsTrue(buttons.Count > 0);
        Assert.IsTrue(buttons.All(b => b.GetAttribute("type") == "button"));
    }

    [TestMethod]
    public void BitPaginationShouldHideTheIconsAndTheEllipsisFromAssistiveTechnologies()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 50);
            parameters.Add(p => p.ShowFirstButton, true);
            parameters.Add(p => p.ShowLastButton, true);
            parameters.Add(p => p.DefaultSelectedPage, 25);
        });

        var icons = comp.FindAll(".bit-pgn button i");

        Assert.AreEqual(4, icons.Count);
        Assert.IsTrue(icons.All(i => i.GetAttribute("aria-hidden") == "true"));

        var ellipsis = comp.FindAll(".bit-pgn-elp");

        Assert.AreEqual(2, ellipsis.Count);
        Assert.IsTrue(ellipsis.All(e => e.GetAttribute("aria-hidden") == "true"));

        // The ellipsis is not a control, so it must not carry a disabled attribute either.
        Assert.IsTrue(ellipsis.All(e => e.HasAttribute("disabled") is false));
    }

    [TestMethod]
    public void BitPaginationShouldCallOnChangeAndBindSelectedPage()
    {
        var selected = 2;
        var changedValue = -1;

        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Bind(p => p.SelectedPage, selected, v => selected = v);
            parameters.Add(p => p.OnChange, (int p) => changedValue = p);
        });

        FindPageButton(comp, 3)!.Click();

        Assert.AreEqual(3, selected);
        Assert.AreEqual(3, changedValue);
    }

    [TestMethod]
    public void BitPaginationShouldCallOnChangeWhileSelectedPageIsBoundOneWay()
    {
        // A one way bound value belongs to the consumer, so the component does not move it, but the click
        // still has to be reported so that the consumer can apply it.
        var changedValue = -1;

        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Add(p => p.SelectedPage, 2);
            parameters.Add(p => p.OnChange, (int p) => changedValue = p);
        });

        FindPageButton(comp, 4)!.Click();

        Assert.AreEqual(4, changedValue);
        Assert.AreEqual("2", comp.Find(".bit-pgn-sel").TextContent.Trim());
    }

    [TestMethod]
    public void BitPaginationShouldNotCallOnChangeWhenTheSelectedPageIsClicked()
    {
        var changedCount = 0;

        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Add(p => p.DefaultSelectedPage, 3);
            parameters.Add(p => p.OnChange, (int _) => changedCount++);
        });

        FindPageButton(comp, 3)!.Click();

        Assert.AreEqual(0, changedCount);
    }

    [TestMethod]
    public void BitPaginationShouldNotCallOnChangeWhileDisabled()
    {
        var changedCount = 0;

        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.OnChange, (int _) => changedCount++);
        });

        Assert.IsTrue(comp.FindAll("button").All(b => b.HasAttribute("disabled")));
        Assert.IsTrue(comp.Find(".bit-pgn").ClassList.Contains("bit-dis"));

        FindPageButton(comp, 3)!.Click();

        Assert.AreEqual(0, changedCount);
    }

    [TestMethod]
    public void BitPaginationShouldRespectShowFirstLastAndNavigationButtons()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 10);
            parameters.Add(p => p.ShowFirstButton, true);
            parameters.Add(p => p.ShowLastButton, true);
            parameters.Add(p => p.ShowNextButton, true);
            parameters.Add(p => p.ShowPreviousButton, true);
        });

        Assert.IsNotNull(FindByAriaLabel(comp, "First page"));
        Assert.IsNotNull(FindByAriaLabel(comp, "Previous page"));
        Assert.IsNotNull(FindByAriaLabel(comp, "Next page"));
        Assert.IsNotNull(FindByAriaLabel(comp, "Last page"));
    }

    [TestMethod]
    public void BitPaginationShouldNotRenderTheNavigationButtonsThatAreTurnedOff()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 10);
            parameters.Add(p => p.ShowNextButton, false);
            parameters.Add(p => p.ShowPreviousButton, false);
        });

        Assert.IsNull(FindByAriaLabel(comp, "Next page"));
        Assert.IsNull(FindByAriaLabel(comp, "Previous page"));
        Assert.IsNull(FindByAriaLabel(comp, "First page"));
        Assert.IsNull(FindByAriaLabel(comp, "Last page"));
    }

    [TestMethod]
    public void BitPaginationShouldDisableTheNavigationButtonsAtTheEndsOfTheRange()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Add(p => p.ShowFirstButton, true);
            parameters.Add(p => p.ShowLastButton, true);
        });

        Assert.IsTrue(FindByAriaLabel(comp, "First page")!.HasAttribute("disabled"));
        Assert.IsTrue(FindByAriaLabel(comp, "Previous page")!.HasAttribute("disabled"));
        Assert.IsFalse(FindByAriaLabel(comp, "Next page")!.HasAttribute("disabled"));
        Assert.IsFalse(FindByAriaLabel(comp, "Last page")!.HasAttribute("disabled"));

        FindByAriaLabel(comp, "Last page")!.Click();

        Assert.AreEqual("5", comp.Find(".bit-pgn-sel").TextContent.Trim());
        Assert.IsFalse(FindByAriaLabel(comp, "First page")!.HasAttribute("disabled"));
        Assert.IsFalse(FindByAriaLabel(comp, "Previous page")!.HasAttribute("disabled"));
        Assert.IsTrue(FindByAriaLabel(comp, "Next page")!.HasAttribute("disabled"));
        Assert.IsTrue(FindByAriaLabel(comp, "Last page")!.HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitPaginationShouldWalkThePagesWithTheNavigationButtons()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Add(p => p.ShowFirstButton, true);
            parameters.Add(p => p.ShowLastButton, true);
            parameters.Add(p => p.DefaultSelectedPage, 3);
        });

        FindByAriaLabel(comp, "Next page")!.Click();
        Assert.AreEqual("4", comp.Find(".bit-pgn-sel").TextContent.Trim());

        FindByAriaLabel(comp, "Previous page")!.Click();
        Assert.AreEqual("3", comp.Find(".bit-pgn-sel").TextContent.Trim());

        FindByAriaLabel(comp, "Last page")!.Click();
        Assert.AreEqual("5", comp.Find(".bit-pgn-sel").TextContent.Trim());

        FindByAriaLabel(comp, "First page")!.Click();
        Assert.AreEqual("1", comp.Find(".bit-pgn-sel").TextContent.Trim());
    }

    [TestMethod]
    public void BitPaginationShouldWrapAroundTheRangeWhileLooping()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Add(p => p.Loop, true);
        });

        // Both buttons stay reachable at the ends of the range while looping.
        Assert.IsFalse(FindByAriaLabel(comp, "Previous page")!.HasAttribute("disabled"));
        Assert.IsFalse(FindByAriaLabel(comp, "Next page")!.HasAttribute("disabled"));

        FindByAriaLabel(comp, "Previous page")!.Click();
        Assert.AreEqual("5", comp.Find(".bit-pgn-sel").TextContent.Trim());

        FindByAriaLabel(comp, "Next page")!.Click();
        Assert.AreEqual("1", comp.Find(".bit-pgn-sel").TextContent.Trim());
    }

    [TestMethod]
    public void BitPaginationShouldNotWrapAroundTheRangeWithoutLooping()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Add(p => p.DefaultSelectedPage, 5);
        });

        Assert.IsTrue(FindByAriaLabel(comp, "Next page")!.HasAttribute("disabled"));
        Assert.AreEqual("5", comp.Find(".bit-pgn-sel").TextContent.Trim());
    }

    [TestMethod]
    public void BitPaginationShouldRenderEllipsisWhenCountLarge()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 50);
            parameters.Add(p => p.BoundaryCount, 1);
            parameters.Add(p => p.MiddleCount, 3);
            parameters.Add(p => p.DefaultSelectedPage, 25);
        });

        CollectionAssert.AreEqual(
            new[] { "1", "•••", "24", "25", "26", "•••", "50" },
            GetRenderedPages(comp));
    }

    [TestMethod]
    public void BitPaginationShouldKeepTheBoundaryPagesVisible()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 20);
            parameters.Add(p => p.BoundaryCount, 2);
            parameters.Add(p => p.MiddleCount, 3);
            parameters.Add(p => p.DefaultSelectedPage, 10);
        });

        CollectionAssert.AreEqual(
            new[] { "1", "2", "•••", "9", "10", "11", "•••", "19", "20" },
            GetRenderedPages(comp));
    }

    [TestMethod]
    public void BitPaginationShouldSpellOutAPageInsteadOfAnEllipsisHidingASingleOne()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 8);
            parameters.Add(p => p.BoundaryCount, 1);
            parameters.Add(p => p.MiddleCount, 3);
            parameters.Add(p => p.DefaultSelectedPage, 4);
        });

        // The gap between 1 and 3 holds page 2 only, which takes the same room as the ellipsis would.
        CollectionAssert.AreEqual(
            new[] { "1", "2", "3", "4", "5", "•••", "8" },
            GetRenderedPages(comp));
    }

    [TestMethod]
    public void BitPaginationShouldRenderEveryPageWhenTheyAllFit()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 7);
            parameters.Add(p => p.BoundaryCount, 1);
            parameters.Add(p => p.MiddleCount, 3);
            parameters.Add(p => p.DefaultSelectedPage, 4);
        });

        Assert.AreEqual(0, comp.FindAll(".bit-pgn-elp").Count);
        CollectionAssert.AreEqual(new[] { "1", "2", "3", "4", "5", "6", "7" }, GetRenderedPages(comp));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-5)]
    public void BitPaginationShouldFallBackToASinglePageForANonPositiveCount(int count)
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, count);
            parameters.Add(p => p.ShowFirstButton, true);
            parameters.Add(p => p.ShowLastButton, true);
        });

        CollectionAssert.AreEqual(new[] { "1" }, GetRenderedPages(comp));
        Assert.AreEqual("1", comp.Find(".bit-pgn-sel").TextContent.Trim());

        // A single page is both the first and the last one, so nothing can be navigated to.
        Assert.IsTrue(comp.FindAll("button").All(b => b.HasAttribute("disabled") || b.ClassList.Contains("bit-pgn-sel")));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-3)]
    public void BitPaginationShouldFallBackToTheDefaultBoundaryAndMiddleCounts(int value)
    {
        // A boundary or a middle range with nothing in it makes no sense, so a value that is not positive
        // falls back to the documented default, which is what an unset parameter does as well.
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 20);
            parameters.Add(p => p.BoundaryCount, value);
            parameters.Add(p => p.MiddleCount, value);
            parameters.Add(p => p.DefaultSelectedPage, 10);
        });

        CollectionAssert.AreEqual(
            new[] { "1", "2", "•••", "9", "10", "11", "•••", "19", "20" },
            GetRenderedPages(comp));
    }

    [TestMethod]
    public void BitPaginationShouldRespectAnExplicitBoundaryAndMiddleCountOfOne()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 20);
            parameters.Add(p => p.BoundaryCount, 1);
            parameters.Add(p => p.MiddleCount, 1);
            parameters.Add(p => p.DefaultSelectedPage, 10);
        });

        CollectionAssert.AreEqual(new[] { "1", "•••", "10", "•••", "20" }, GetRenderedPages(comp));
    }

    [TestMethod]
    public void BitPaginationShouldClampASelectedPageOutOfTheRangeWhileRendering()
    {
        // A one way bound value cannot be written back, so it is clamped for the rendering only, which
        // keeps a current page in the markup instead of leaving the pagination without one.
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Add(p => p.SelectedPage, 100);
        });

        Assert.AreEqual("5", comp.Find(".bit-pgn-sel").TextContent.Trim());
        Assert.IsTrue(FindByAriaLabel(comp, "Next page")!.HasAttribute("disabled"));

        comp.Render(parameters => parameters.Add(p => p.SelectedPage, -4));

        Assert.AreEqual("1", comp.Find(".bit-pgn-sel").TextContent.Trim());
        Assert.IsTrue(FindByAriaLabel(comp, "Previous page")!.HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitPaginationShouldPullTheBoundSelectedPageBackWhenTheCountShrinks()
    {
        var selected = 9;

        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 10);
            parameters.Bind(p => p.SelectedPage, selected, v => selected = v);
        });

        Assert.AreEqual("9", comp.Find(".bit-pgn-sel").TextContent.Trim());

        comp.Render(parameters => parameters.Add(p => p.Count, 4));

        Assert.AreEqual(4, selected);
        Assert.AreEqual("4", comp.Find(".bit-pgn-sel").TextContent.Trim());
    }

    [TestMethod]
    public void BitPaginationShouldSettleOnTheSelectedPageWhenTheCountShrinksAlongWithIt()
    {
        // The count and the selected page reach the component in one batch, so the write back of an out of
        // range selection must not overwrite the page the consumer asked for in the same batch.
        var selected = 9;
        var changes = new System.Collections.Generic.List<int>();

        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 10);
            parameters.Bind(p => p.SelectedPage, selected, v => { selected = v; changes.Add(v); });
        });

        changes.Clear();

        comp.Render(parameters =>
        {
            parameters.Add(p => p.Count, 4);
            parameters.Bind(p => p.SelectedPage, 2, v => { selected = v; changes.Add(v); });
        });

        // Page 2 is inside the new range, so nothing is written back and the pagination lands on it.
        Assert.AreEqual(0, changes.Count);
        Assert.AreEqual("2", comp.Find(".bit-pgn-sel").TextContent.Trim());
    }

    [TestMethod]
    public void BitPaginationShouldCorrectTheSameOutOfRangeSelectedPageOnlyOnce()
    {
        // A consumer that drops the corrected page and hands the out of range one back would otherwise be
        // answered with another correction on every render, and the two would keep re-rendering each other.
        var changes = new System.Collections.Generic.List<int>();

        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 4);
            parameters.Bind(p => p.SelectedPage, 9, changes.Add);
        });

        Assert.AreEqual(1, changes.Count);
        Assert.AreEqual(4, changes[0]);

        comp.Render(parameters =>
        {
            parameters.Add(p => p.Count, 4);
            parameters.Bind(p => p.SelectedPage, 9, changes.Add);
        });

        Assert.AreEqual(1, changes.Count);
        Assert.AreEqual("4", comp.Find(".bit-pgn-sel").TextContent.Trim());
    }

    [TestMethod]
    public void BitPaginationShouldWriteBackASelectedPageUnderTheRange()
    {
        var selected = -4;

        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Bind(p => p.SelectedPage, selected, v => selected = v);
        });

        Assert.AreEqual(1, selected);
        Assert.AreEqual("1", comp.Find(".bit-pgn-sel").TextContent.Trim());
    }

    [TestMethod]
    public void BitPaginationShouldRespectDefaultSelectedPageAndClampIt()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Add(p => p.DefaultSelectedPage, 42);
        });

        Assert.AreEqual("5", comp.Find(".bit-pgn-sel").TextContent.Trim());
    }

    [TestMethod]
    public void BitPaginationShouldSelectTheFirstPageWithoutAnySelection()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
        });

        Assert.AreEqual("1", comp.Find(".bit-pgn-sel").TextContent.Trim());
    }

    [TestMethod]
    public void BitPaginationShouldRespectHideOnSinglePage()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 1);
            parameters.Add(p => p.HideOnSinglePage, true);
        });

        Assert.AreEqual(0, comp.FindAll(".bit-pgn").Count);

        comp.Render(parameters => parameters.Add(p => p.Count, 2));

        Assert.AreEqual(1, comp.FindAll(".bit-pgn").Count);
    }

    [TestMethod]
    public void BitPaginationShouldRespectShowPageButtons()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 10);
            parameters.Add(p => p.ShowPageButtons, false);
            parameters.Add(p => p.ShowFirstButton, true);
            parameters.Add(p => p.ShowLastButton, true);
        });

        Assert.AreEqual(0, GetPageButtons(comp).Count);
        Assert.AreEqual(0, comp.FindAll(".bit-pgn-elp").Count);
        Assert.AreEqual(4, comp.FindAll(".bit-pgn button").Count);

        // The navigation still walks the range even without the page buttons.
        FindByAriaLabel(comp, "Next page")!.Click();
        Assert.IsFalse(FindByAriaLabel(comp, "Previous page")!.HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitPaginationShouldRespectShowSummary()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 12);
            parameters.Add(p => p.DefaultSelectedPage, 4);
        });

        Assert.AreEqual(0, comp.FindAll(".bit-pgn-sum").Count);

        comp.Render(parameters => parameters.Add(p => p.ShowSummary, true));

        var summary = comp.Find(".bit-pgn-sum");

        Assert.AreEqual("Page 4 of 12", summary.TextContent.Trim());

        // The summary reports the new position on its own, without moving the focus.
        Assert.AreEqual("status", summary.GetAttribute("role"));

        // It leads the pagination, ahead of the controls it describes.
        Assert.IsTrue(comp.Find(".bit-pgn > ul > li:first-child > *").ClassList.Contains("bit-pgn-sum"));

        FindPageButton(comp, 5)!.Click();

        Assert.AreEqual("Page 5 of 12", comp.Find(".bit-pgn-sum").TextContent.Trim());
    }

    [TestMethod]
    public void BitPaginationShouldRespectGetSummary()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 24);
            parameters.Add(p => p.ShowSummary, true);
            parameters.Add(p => p.DefaultSelectedPage, 3);
            parameters.Add(p => p.GetSummary, (int page, int count) => $"{page}/{count} pages");
        });

        Assert.AreEqual("3/24 pages", comp.Find(".bit-pgn-sum").TextContent.Trim());
    }

    [TestMethod]
    public void BitPaginationShouldNotRenderTheSummaryAsAControl()
    {
        // The summary is text beside the buttons, so it must not end up in the tab order.
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Add(p => p.ShowSummary, true);
        });

        Assert.AreEqual("DIV", comp.Find(".bit-pgn-sum").TagName);
        Assert.IsNull(FindByAriaLabel(comp, "Page 1 of 5"));
    }

    [TestMethod]
    public void BitPaginationShouldRespectRounded()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
        });

        Assert.IsFalse(comp.Find(".bit-pgn").ClassList.Contains("bit-pgn-rnd"));

        comp.Render(parameters => parameters.Add(p => p.Rounded, true));

        Assert.IsTrue(comp.Find(".bit-pgn").ClassList.Contains("bit-pgn-rnd"));
    }

    [TestMethod]
    [DataRow(BitVariant.Fill, "bit-pgn-fil")]
    [DataRow(BitVariant.Outline, "bit-pgn-otl")]
    [DataRow(BitVariant.Text, "bit-pgn-txt")]
    [DataRow(null, "bit-pgn-fil")]
    public void BitPaginationShouldRespectVariant(BitVariant? variant, string cssClass)
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Add(p => p.Variant, variant);
        });

        Assert.IsTrue(comp.FindAll(".bit-pgn-btn").All(b => b.ClassList.Contains(cssClass)));
    }

    [TestMethod]
    [DataRow(BitColor.Primary, "bit-pgn-pri")]
    [DataRow(BitColor.Secondary, "bit-pgn-sec")]
    [DataRow(BitColor.Tertiary, "bit-pgn-ter")]
    [DataRow(BitColor.Info, "bit-pgn-inf")]
    [DataRow(BitColor.Success, "bit-pgn-suc")]
    [DataRow(BitColor.Warning, "bit-pgn-wrn")]
    [DataRow(BitColor.SevereWarning, "bit-pgn-swr")]
    [DataRow(BitColor.Error, "bit-pgn-err")]
    [DataRow(null, "bit-pgn-pri")]
    public void BitPaginationShouldRespectColor(BitColor? color, string cssClass)
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Add(p => p.Color, color);
        });

        Assert.IsTrue(comp.Find(".bit-pgn").ClassList.Contains(cssClass));
    }

    [TestMethod]
    [DataRow(BitSize.Small, "bit-pgn-sm")]
    [DataRow(BitSize.Medium, "bit-pgn-md")]
    [DataRow(BitSize.Large, "bit-pgn-lg")]
    [DataRow(null, "bit-pgn-md")]
    public void BitPaginationShouldRespectSize(BitSize? size, string cssClass)
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Add(p => p.Size, size);
        });

        Assert.IsTrue(comp.Find(".bit-pgn").ClassList.Contains(cssClass));
    }

    [TestMethod]
    public void BitPaginationShouldRespectTheNavigationButtonAriaLabels()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Add(p => p.ShowFirstButton, true);
            parameters.Add(p => p.ShowLastButton, true);
            parameters.Add(p => p.FirstButtonAriaLabel, "aval");
            parameters.Add(p => p.PreviousButtonAriaLabel, "ghabli");
            parameters.Add(p => p.NextButtonAriaLabel, "badi");
            parameters.Add(p => p.LastButtonAriaLabel, "akhar");
        });

        foreach (var label in new[] { "aval", "ghabli", "badi", "akhar" })
        {
            var button = FindByAriaLabel(comp, label);

            Assert.IsNotNull(button);

            // The label doubles as the tooltip, since the button carries an icon and no text.
            Assert.AreEqual(label, button!.GetAttribute("title"));
        }
    }

    [TestMethod]
    public void BitPaginationShouldLabelThePageButtons()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 3);
            parameters.Add(p => p.DefaultSelectedPage, 2);
        });

        CollectionAssert.AreEqual(
            new[] { "Page 1", "Page 2", "Page 3" },
            GetPageButtons(comp).Select(b => b.GetAttribute("aria-label")).ToArray());
    }

    [TestMethod]
    public void BitPaginationShouldRespectGetPageAriaLabel()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 3);
            parameters.Add(p => p.DefaultSelectedPage, 2);
            parameters.Add(p => p.GetPageAriaLabel, (int page, bool isSelected) => isSelected ? $"current {page}" : $"go to {page}");
        });

        CollectionAssert.AreEqual(
            new[] { "go to 1", "current 2", "go to 3" },
            GetPageButtons(comp).Select(b => b.GetAttribute("aria-label")).ToArray());
    }

    [TestMethod]
    public void BitPaginationShouldRespectStyleAndClass()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Add(p => p.Style, "color: red;");
            parameters.Add(p => p.Class, "custom-class");
        });

        var root = comp.Find(".bit-pgn");

        Assert.IsTrue(root.GetAttribute("style")!.Contains("color: red;"));
        Assert.IsTrue(root.ClassList.Contains("custom-class"));
    }

    [TestMethod]
    public void BitPaginationShouldRespectClassesAndStyles()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 50);
            parameters.Add(p => p.BoundaryCount, 1);
            parameters.Add(p => p.DefaultSelectedPage, 25);
            parameters.Add(p => p.ShowFirstButton, true);
            parameters.Add(p => p.ShowLastButton, true);
            parameters.Add(p => p.ShowSummary, true);
            parameters.Add(p => p.Classes, new BitPaginationClassStyles
            {
                Root = "root-class",
                Summary = "summary-class",
                Button = "button-class",
                Ellipsis = "ellipsis-class",
                SelectedButton = "selected-class",
                FirstButton = "first-class",
                FirstButtonIcon = "first-icon-class",
                PreviousButton = "previous-class",
                PreviousButtonIcon = "previous-icon-class",
                NextButton = "next-class",
                NextButtonIcon = "next-icon-class",
                LastButton = "last-class",
                LastButtonIcon = "last-icon-class"
            });
            parameters.Add(p => p.Styles, new BitPaginationClassStyles { Root = "padding: 1rem;", SelectedButton = "color: red;", Summary = "color: blue;" });
        });

        var root = comp.Find(".bit-pgn");

        Assert.IsTrue(root.ClassList.Contains("root-class"));
        Assert.IsTrue(root.GetAttribute("style")!.Contains("padding: 1rem;"));

        var summary = comp.Find(".bit-pgn-sum");

        Assert.IsTrue(summary.ClassList.Contains("summary-class"));
        Assert.IsTrue(summary.GetAttribute("style")!.Contains("color: blue;"));

        Assert.IsTrue(comp.FindAll(".bit-pgn-btn").All(b => b.ClassList.Contains("button-class")));
        Assert.IsTrue(comp.FindAll(".bit-pgn-elp").All(e => e.ClassList.Contains("ellipsis-class")));

        var selected = comp.Find(".bit-pgn-sel");

        Assert.IsTrue(selected.ClassList.Contains("selected-class"));
        Assert.IsTrue(selected.GetAttribute("style")!.Contains("color: red;"));

        Assert.IsTrue(FindByAriaLabel(comp, "First page")!.ClassList.Contains("first-class"));
        Assert.IsTrue(FindByAriaLabel(comp, "Previous page")!.ClassList.Contains("previous-class"));
        Assert.IsTrue(FindByAriaLabel(comp, "Next page")!.ClassList.Contains("next-class"));
        Assert.IsTrue(FindByAriaLabel(comp, "Last page")!.ClassList.Contains("last-class"));

        Assert.IsTrue(FindByAriaLabel(comp, "First page")!.QuerySelector("i")!.ClassList.Contains("first-icon-class"));
        Assert.IsTrue(FindByAriaLabel(comp, "Previous page")!.QuerySelector("i")!.ClassList.Contains("previous-icon-class"));
        Assert.IsTrue(FindByAriaLabel(comp, "Next page")!.QuerySelector("i")!.ClassList.Contains("next-icon-class"));
        Assert.IsTrue(FindByAriaLabel(comp, "Last page")!.QuerySelector("i")!.ClassList.Contains("last-icon-class"));
    }

    [TestMethod]
    public void BitPaginationShouldRespectTheNavigationButtonIcons()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Add(p => p.ShowFirstButton, true);
            parameters.Add(p => p.ShowLastButton, true);
            parameters.Add(p => p.FirstButtonIconName, "DoubleChevronLeft");
            parameters.Add(p => p.PreviousButtonIconName, "Previous");
            parameters.Add(p => p.NextButtonIconName, "Next");
            parameters.Add(p => p.LastButtonIconName, "DoubleChevronRight");
        });

        Assert.IsTrue(FindByAriaLabel(comp, "First page")!.QuerySelector("i")!.ClassList.Contains("bit-icon--DoubleChevronLeft"));
        Assert.IsTrue(FindByAriaLabel(comp, "Previous page")!.QuerySelector("i")!.ClassList.Contains("bit-icon--Previous"));
        Assert.IsTrue(FindByAriaLabel(comp, "Next page")!.QuerySelector("i")!.ClassList.Contains("bit-icon--Next"));
        Assert.IsTrue(FindByAriaLabel(comp, "Last page")!.QuerySelector("i")!.ClassList.Contains("bit-icon--DoubleChevronRight"));
    }

    [TestMethod]
    public void BitPaginationShouldPreferTheExternalIconOverTheIconName()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Add(p => p.NextButtonIconName, "Next");
            parameters.Add(p => p.NextButtonIcon, BitIconInfo.Css("fa-solid fa-angle-right"));
        });

        var icon = FindByAriaLabel(comp, "Next page")!.QuerySelector("i")!;

        Assert.IsTrue(icon.ClassList.Contains("fa-angle-right"));
        Assert.IsFalse(icon.ClassList.Contains("bit-icon--Next"));
    }

    [TestMethod]
    public void BitPaginationShouldRespectRtl()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        var root = comp.Find(".bit-pgn");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
    }

    [TestMethod]
    public void BitPaginationShouldKeepTheListOfPagesInPlaceWhileTheSelectionMoves()
    {
        // Moving the selection only flips the attributes of the already rendered buttons, which is what lets
        // the browser keep the keyboard focus on the page the user just activated.
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
        });

        var before = GetRenderedPages(comp);

        FindPageButton(comp, 3)!.Click();

        CollectionAssert.AreEqual(before, GetRenderedPages(comp));
        Assert.AreEqual("3", comp.Find(".bit-pgn-sel").TextContent.Trim());
        Assert.AreEqual(1, GetPageButtons(comp).Count(b => b.GetAttribute("aria-current") is not null));
    }



    private static IElement? FindByAriaLabel(IRenderedComponent<BitPagination> comp, string label)
    {
        return comp.FindAll("button").FirstOrDefault(b => b.GetAttribute("aria-label") == label);
    }

    private static IElement? FindPageButton(IRenderedComponent<BitPagination> comp, int page)
    {
        return GetPageButtons(comp).FirstOrDefault(b => b.TextContent.Trim() == page.ToString());
    }

    private static System.Collections.Generic.List<IElement> GetPageButtons(IRenderedComponent<BitPagination> comp)
    {
        return comp.FindAll("button").Where(b => b.QuerySelector("i") is null).ToList();
    }

    private static string[] GetRenderedPages(IRenderedComponent<BitPagination> comp)
    {
        return comp.FindAll(".bit-pgn-btn, .bit-pgn-elp")
                   .Where(e => e.QuerySelector("i") is null)
                   .Select(e => e.TextContent.Trim())
                   .ToArray();
    }
}

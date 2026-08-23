using System.Linq;
using System.Threading.Tasks;
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

        // It leads the pagination, ahead of the controls it describes, and stays out of the list of pages
        // so that the items the list reports are the navigation items and nothing else.
        Assert.IsTrue(comp.Find(".bit-pgn > *:first-child").ClassList.Contains("bit-pgn-sum"));
        Assert.AreEqual(0, comp.FindAll(".bit-pgn > ul .bit-pgn-sum").Count);

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



    [TestMethod]
    public void BitPaginationShouldNameTheEllipsisAndHideItsGlyph()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 50);
            parameters.Add(p => p.DefaultSelectedPage, 25);
        });

        var items = comp.FindAll(".bit-pgn-itm").Where(i => i.QuerySelector(".bit-pgn-elp") is not null).ToList();

        Assert.AreEqual(2, items.Count);

        // The gap is reported as one named item while the dots themselves stay out of the accessibility tree.
        Assert.IsTrue(items.All(i => i.GetAttribute("aria-label") == "More pages"));
        Assert.IsTrue(items.All(i => i.GetAttribute("role") == "listitem"));
        Assert.IsTrue(comp.FindAll(".bit-pgn-elp").All(e => e.GetAttribute("aria-hidden") == "true"));
        Assert.IsTrue(comp.FindAll(".bit-pgn-elp").All(e => e.TextContent.Trim() == "•••"));
    }

    [TestMethod]
    public void BitPaginationShouldRespectEllipsisTextAndAriaLabel()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 50);
            parameters.Add(p => p.EllipsisText, "...");
            parameters.Add(p => p.EllipsisAriaLabel, "Hidden pages");
            parameters.Add(p => p.DefaultSelectedPage, 25);
        });

        Assert.IsTrue(comp.FindAll(".bit-pgn-elp").All(e => e.TextContent.Trim() == "..."));

        var items = comp.FindAll(".bit-pgn-itm").Where(i => i.QuerySelector(".bit-pgn-elp") is not null).ToList();

        Assert.AreEqual(2, items.Count);
        Assert.IsTrue(items.All(i => i.GetAttribute("aria-label") == "Hidden pages"));
    }

    [TestMethod]
    public void BitPaginationShouldRenderTheNavigationButtonTexts()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 10);
            parameters.Add(p => p.ShowFirstButton, true);
            parameters.Add(p => p.ShowLastButton, true);
            parameters.Add(p => p.FirstButtonText, "First");
            parameters.Add(p => p.PreviousButtonText, "Previous");
            parameters.Add(p => p.NextButtonText, "Next");
            parameters.Add(p => p.LastButtonText, "Last");
            parameters.Add(p => p.DefaultSelectedPage, 5);
        });

        var first = FindByAriaLabel(comp, "First page")!;
        var previous = FindByAriaLabel(comp, "Previous page")!;
        var next = FindByAriaLabel(comp, "Next page")!;
        var last = FindByAriaLabel(comp, "Last page")!;

        Assert.AreEqual("First", first.QuerySelector(".bit-pgn-btx")!.TextContent.Trim());
        Assert.AreEqual("Previous", previous.QuerySelector(".bit-pgn-btx")!.TextContent.Trim());
        Assert.AreEqual("Next", next.QuerySelector(".bit-pgn-btx")!.TextContent.Trim());
        Assert.AreEqual("Last", last.QuerySelector(".bit-pgn-btx")!.TextContent.Trim());

        // A labelled button is no longer a square, and the spoken name still comes from the aria label.
        Assert.IsTrue(new[] { first, previous, next, last }.All(b => b.ClassList.Contains("bit-pgn-lbl")));

        // The icon stays beside the text, and the text leads the buttons pointing backwards only.
        Assert.IsTrue(new[] { first, previous, next, last }.All(b => b.QuerySelector("i") is not null));
        Assert.AreEqual("I", previous.Children[0].TagName);
        Assert.AreEqual("SPAN", next.Children[0].TagName);
    }

    [TestMethod]
    public void BitPaginationShouldNotRenderATextlessNavigationButtonAsALabelledOne()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 10);
            parameters.Add(p => p.NextButtonText, "   ");
        });

        var next = FindByAriaLabel(comp, "Next page")!;

        Assert.IsNull(next.QuerySelector(".bit-pgn-btx"));
        Assert.IsFalse(next.ClassList.Contains("bit-pgn-lbl"));
    }

    [TestMethod]
    public void BitPaginationShouldKeepTheNavigationButtonsDisabledWhileLoopingASinglePage()
    {
        // A loop has nowhere to take the two buttons while there is a single page to be on.
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 1);
            parameters.Add(p => p.Loop, true);
        });

        Assert.IsTrue(FindByAriaLabel(comp, "Previous page")!.HasAttribute("disabled"));
        Assert.IsTrue(FindByAriaLabel(comp, "Next page")!.HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitPaginationShouldNotRenderTheGoToPageByDefault()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 12);
        });

        Assert.AreEqual(0, comp.FindAll(".bit-pgn-gtp").Count);
    }

    [TestMethod]
    public void BitPaginationShouldRespectShowGoToPage()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 12);
            parameters.Add(p => p.ShowGoToPage, true);
        });

        var input = comp.Find(".bit-pgn-gti");

        Assert.AreEqual("INPUT", input.TagName);
        Assert.AreEqual("number", input.GetAttribute("type"));
        Assert.AreEqual("1", input.GetAttribute("min"));
        Assert.AreEqual("12", input.GetAttribute("max"));

        var label = comp.Find(".bit-pgn-gtl");

        // The visible label names the input while it is there, so nothing is left to override it with.
        Assert.AreEqual("Go to", label.TextContent.Trim());
        Assert.AreEqual(input.Id, label.GetAttribute("for"));
        Assert.IsFalse(input.HasAttribute("aria-label"));

        // The jump closes the pagination, after the buttons it is the shortcut for, and stays out of the list.
        Assert.IsTrue(comp.Find(".bit-pgn > *:last-child").ClassList.Contains("bit-pgn-gtp"));
        Assert.AreEqual(0, comp.FindAll(".bit-pgn > ul .bit-pgn-gtp").Count);
    }

    [TestMethod]
    public void BitPaginationShouldDropTheGoToPageLabelWhenItIsEmpty()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 12);
            parameters.Add(p => p.ShowGoToPage, true);
            parameters.Add(p => p.GoToPageText, "");
        });

        Assert.AreEqual(0, comp.FindAll(".bit-pgn-gtl").Count);

        // The input is still named on its own.
        Assert.AreEqual("Go to page", comp.Find(".bit-pgn-gti").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitPaginationShouldRespectGoToPageTexts()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 12);
            parameters.Add(p => p.ShowGoToPage, true);
            parameters.Add(p => p.GoToPageText, "Jump to");
            parameters.Add(p => p.GoToPageAriaLabel, "Jump to a page");
        });

        Assert.AreEqual("Jump to", comp.Find(".bit-pgn-gtl").TextContent.Trim());

        // The visible label is the name while it is there, so the two can never say different things.
        Assert.IsFalse(comp.Find(".bit-pgn-gti").HasAttribute("aria-label"));

        comp.Render(parameters => parameters.Add(p => p.GoToPageText, ""));

        Assert.AreEqual("Jump to a page", comp.Find(".bit-pgn-gti").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitPaginationShouldJumpToThePageTypedIntoTheGoToPage()
    {
        var page = 0;

        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 12);
            parameters.Add(p => p.ShowGoToPage, true);
            parameters.Add(p => p.OnChange, p => page = p);
        });

        comp.Find(".bit-pgn-gti").Change("7");

        Assert.AreEqual(7, page);
        Assert.AreEqual("7", comp.Find(".bit-pgn-sel").TextContent.Trim());

        // The field clears itself so the next jump starts from an empty one.
        Assert.AreEqual(string.Empty, comp.Find(".bit-pgn-gti").GetAttribute("value"));
    }

    [TestMethod]
    [DataRow("99", 12)]
    [DataRow("0", 1)]
    [DataRow("-4", 1)]
    public void BitPaginationShouldLandOnTheNearestEndForAGoToPageOutOfTheRange(string text, int expected)
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 12);
            parameters.Add(p => p.ShowGoToPage, true);
            parameters.Add(p => p.DefaultSelectedPage, 6);
        });

        comp.Find(".bit-pgn-gti").Change(text);

        Assert.AreEqual(expected.ToString(), comp.Find(".bit-pgn-sel").TextContent.Trim());
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("abc")]
    public void BitPaginationShouldIgnoreAGoToPageThatIsNotANumber(string text)
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 12);
            parameters.Add(p => p.ShowGoToPage, true);
            parameters.Add(p => p.DefaultSelectedPage, 6);
        });

        comp.Find(".bit-pgn-gti").Change(text);

        Assert.AreEqual("6", comp.Find(".bit-pgn-sel").TextContent.Trim());
    }

    [TestMethod]
    public void BitPaginationShouldNotJumpWhileDisabled()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 12);
            parameters.Add(p => p.ShowGoToPage, true);
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.DefaultSelectedPage, 6);
        });

        var input = comp.Find(".bit-pgn-gti");

        Assert.IsTrue(input.HasAttribute("disabled"));

        input.Change("9");

        Assert.AreEqual("6", comp.Find(".bit-pgn-sel").TextContent.Trim());
    }

    [TestMethod]
    public void BitPaginationShouldMoveTheFocusOffANavigationButtonThatDisabledItself()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Add(p => p.ShowLastButton, true);
        });

        // Walking one page at a time leaves the next button enabled, so the focus stays where the user put it.
        FindByAriaLabel(comp, "Next page")!.Click();

        Assert.AreEqual(0, CountFocusCalls());

        // Reaching the last page disables the button that was activated, which would drop the focus.
        FindByAriaLabel(comp, "Last page")!.Click();

        Assert.AreEqual(1, CountFocusCalls());
    }

    [TestMethod]
    public void BitPaginationShouldMoveTheFocusEvenWithoutThePageButtons()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Add(p => p.ShowPageButtons, false);
            parameters.Add(p => p.DefaultSelectedPage, 4);
        });

        FindByAriaLabel(comp, "Next page")!.Click();

        Assert.AreEqual("5", comp.Instance.SelectedPage.ToString());
        Assert.AreEqual(1, CountFocusCalls());
    }

    [TestMethod]
    public void BitPaginationShouldNotMoveTheFocusOffAPageButton()
    {
        // A page button is never disabled by the selection moving onto it, so its focus is left alone.
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
        });

        FindPageButton(comp, 5)!.Click();

        Assert.AreEqual(0, CountFocusCalls());
    }

    [TestMethod]
    public void BitPaginationShouldRespectTheGoToPageClassesAndStyles()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 12);
            parameters.Add(p => p.ShowGoToPage, true);
            parameters.Add(p => p.Classes, new BitPaginationClassStyles
            {
                GoToPage = "custom-go-to-page",
                GoToPageLabel = "custom-go-to-page-label",
                GoToPageInput = "custom-go-to-page-input"
            });
            parameters.Add(p => p.Styles, new BitPaginationClassStyles
            {
                GoToPage = "color: red;",
                GoToPageLabel = "color: green;",
                GoToPageInput = "color: blue;"
            });
        });

        var container = comp.Find(".bit-pgn-gtp");
        var label = comp.Find(".bit-pgn-gtl");
        var input = comp.Find(".bit-pgn-gti");

        Assert.IsTrue(container.ClassList.Contains("custom-go-to-page"));
        Assert.IsTrue(label.ClassList.Contains("custom-go-to-page-label"));
        Assert.IsTrue(input.ClassList.Contains("custom-go-to-page-input"));

        Assert.AreEqual("color: red;", container.GetAttribute("style"));
        Assert.AreEqual("color: green;", label.GetAttribute("style"));
        Assert.AreEqual("color: blue;", input.GetAttribute("style"));
    }



    [TestMethod]
    public void BitPaginationShouldNotRenderThePageSizeSelectorByDefault()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 12);
        });

        Assert.AreEqual(0, comp.FindAll(".bit-pgn-pss").Count);
    }

    [TestMethod]
    public void BitPaginationShouldRespectShowPageSizeSelector()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 12);
            parameters.Add(p => p.ShowPageSizeSelector, true);
        });

        var select = comp.Find(".bit-pgn-pse");

        Assert.AreEqual("SELECT", select.TagName);

        var label = comp.Find(".bit-pgn-psl");

        // The visible label names the selector while it is there, so nothing is left to override it with.
        Assert.AreEqual("Items per page", label.TextContent.Trim());
        Assert.AreEqual(select.Id, label.GetAttribute("for"));
        Assert.IsFalse(select.HasAttribute("aria-label"));

        // The page size opens the pagination, since the whole range of pages follows from it, and it is not
        // one of the navigation items the list enumerates.
        Assert.IsTrue(comp.Find(".bit-pgn > *:first-child").ClassList.Contains("bit-pgn-pss"));
        Assert.AreEqual(0, comp.FindAll(".bit-pgn > ul .bit-pgn-pss").Count);

        var options = comp.FindAll(".bit-pgn-pse option");

        CollectionAssert.AreEqual(new[] { "10", "25", "50", "100" }, options.Select(o => o.TextContent.Trim()).ToArray());

        // A page size that was never picked opens on the first of the offered ones.
        Assert.AreEqual(1, options.Count(o => o.HasAttribute("selected")));
        Assert.AreEqual("10", options.Single(o => o.HasAttribute("selected")).GetAttribute("value"));
    }

    [TestMethod]
    public void BitPaginationShouldRespectPageSizeOptions()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 12);
            parameters.Add(p => p.ShowPageSizeSelector, true);
            parameters.Add(p => p.PageSizeOptions, [5, 15, 30]);
            parameters.Add(p => p.PageSize, 15);
        });

        var options = comp.FindAll(".bit-pgn-pse option");

        CollectionAssert.AreEqual(new[] { "5", "15", "30" }, options.Select(o => o.TextContent.Trim()).ToArray());
        Assert.AreEqual("15", options.Single(o => o.HasAttribute("selected")).GetAttribute("value"));
    }

    [TestMethod]
    public void BitPaginationShouldFallBackToTheDefaultPageSizeOptionsForAnEmptyList()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 12);
            parameters.Add(p => p.ShowPageSizeSelector, true);
            parameters.Add(p => p.PageSizeOptions, []);
        });

        CollectionAssert.AreEqual(new[] { "10", "25", "50", "100" },
                                  comp.FindAll(".bit-pgn-pse option").Select(o => o.TextContent.Trim()).ToArray());
    }

    [TestMethod]
    public void BitPaginationShouldDropThePageSizeLabelWhenItIsEmpty()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 12);
            parameters.Add(p => p.ShowPageSizeSelector, true);
            parameters.Add(p => p.PageSizeText, "");
        });

        Assert.AreEqual(0, comp.FindAll(".bit-pgn-psl").Count);

        // The selector is still named on its own.
        Assert.AreEqual("Items per page", comp.Find(".bit-pgn-pse").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitPaginationShouldRespectThePageSizeTexts()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 12);
            parameters.Add(p => p.ShowPageSizeSelector, true);
            parameters.Add(p => p.PageSizeText, "Rows");
            parameters.Add(p => p.PageSizeAriaLabel, "Rows per page");
        });

        Assert.AreEqual("Rows", comp.Find(".bit-pgn-psl").TextContent.Trim());

        // The visible label is the name while it is there, so the two can never say different things.
        Assert.IsFalse(comp.Find(".bit-pgn-pse").HasAttribute("aria-label"));

        comp.Render(parameters => parameters.Add(p => p.PageSizeText, ""));

        Assert.AreEqual("Rows per page", comp.Find(".bit-pgn-pse").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitPaginationShouldCallOnPageSizeChangeAndBindPageSize()
    {
        var size = 10;
        var changed = 0;

        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 12);
            parameters.Add(p => p.ShowPageSizeSelector, true);
            parameters.Bind(p => p.PageSize, size, v => size = v);
            parameters.Add(p => p.OnPageSizeChange, v => changed = v);
        });

        comp.Find(".bit-pgn-pse").Change("50");

        Assert.AreEqual(50, size);
        Assert.AreEqual(50, changed);
    }

    [TestMethod]
    public void BitPaginationShouldCallOnPageSizeChangeWhilePageSizeIsBoundOneWay()
    {
        var changed = 0;

        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 12);
            parameters.Add(p => p.ShowPageSizeSelector, true);
            parameters.Add(p => p.PageSize, 10);
            parameters.Add(p => p.OnPageSizeChange, v => changed = v);
        });

        comp.Find(".bit-pgn-pse").Change("25");

        Assert.AreEqual(25, changed);
    }

    [TestMethod]
    public void BitPaginationShouldNotCallOnPageSizeChangeWhenTheSamePageSizeIsPicked()
    {
        var changed = 0;

        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 12);
            parameters.Add(p => p.ShowPageSizeSelector, true);
            parameters.Add(p => p.PageSize, 25);
            parameters.Add(p => p.OnPageSizeChange, v => changed = v);
        });

        comp.Find(".bit-pgn-pse").Change("25");

        Assert.AreEqual(0, changed);
    }

    [TestMethod]
    public void BitPaginationShouldNotChangeThePageSizeWhileDisabled()
    {
        var changed = 0;

        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 12);
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.ShowPageSizeSelector, true);
            parameters.Add(p => p.OnPageSizeChange, v => changed = v);
        });

        var select = comp.Find(".bit-pgn-pse");

        Assert.IsTrue(select.HasAttribute("disabled"));

        select.Change("50");

        Assert.AreEqual(0, changed);
    }

    [TestMethod]
    public void BitPaginationShouldPullTheSelectedPageBackWhenAPageSizeShrinksTheCount()
    {
        // Picking a bigger page size leaves fewer pages, and the page the consumer recomputes the count from
        // is pulled back into the range that is left.
        var page = 9;

        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 12);
            parameters.Add(p => p.ShowPageSizeSelector, true);
            parameters.Bind(p => p.SelectedPage, page, v => page = v);
        });

        Assert.AreEqual("9", comp.Find(".bit-pgn-sel").TextContent.Trim());

        comp.Render(parameters => parameters.Add(p => p.Count, 3));

        Assert.AreEqual(3, page);
        Assert.AreEqual("3", comp.Find(".bit-pgn-sel").TextContent.Trim());
    }

    [TestMethod]
    public void BitPaginationShouldRespectThePageSizeClassesAndStyles()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 12);
            parameters.Add(p => p.ShowPageSizeSelector, true);
            parameters.Add(p => p.Classes, new BitPaginationClassStyles
            {
                PageSizeSelector = "custom-page-size",
                PageSizeLabel = "custom-page-size-label",
                PageSizeSelect = "custom-page-size-select"
            });
            parameters.Add(p => p.Styles, new BitPaginationClassStyles
            {
                PageSizeSelector = "color: red;",
                PageSizeLabel = "color: green;",
                PageSizeSelect = "color: blue;"
            });
        });

        var container = comp.Find(".bit-pgn-pss");
        var label = comp.Find(".bit-pgn-psl");
        var select = comp.Find(".bit-pgn-pse");

        Assert.IsTrue(container.ClassList.Contains("custom-page-size"));
        Assert.IsTrue(label.ClassList.Contains("custom-page-size-label"));
        Assert.IsTrue(select.ClassList.Contains("custom-page-size-select"));

        Assert.AreEqual("color: red;", container.GetAttribute("style"));
        Assert.AreEqual("color: green;", label.GetAttribute("style"));
        Assert.AreEqual("color: blue;", select.GetAttribute("style"));
    }

    [TestMethod]
    public async Task BitPaginationShouldFocusTheSelectedPage()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Add(p => p.DefaultSelectedPage, 3);
        });

        await comp.InvokeAsync(async () => await comp.Instance.FocusAsync());

        Assert.AreEqual(1, CountFocusCalls());
    }

    [TestMethod]
    public async Task BitPaginationShouldFocusANavigationButtonWithoutThePageButtons()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Add(p => p.ShowPageButtons, false);
            parameters.Add(p => p.ShowPreviousButton, false);
            parameters.Add(p => p.DefaultSelectedPage, 3);
        });

        await comp.InvokeAsync(async () => await comp.Instance.FocusAsync());

        Assert.AreEqual(1, CountFocusCalls());
    }

    [TestMethod]
    public async Task BitPaginationShouldNotFocusAnythingWithoutAnyControl()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Add(p => p.ShowPageButtons, false);
            parameters.Add(p => p.ShowNextButton, false);
            parameters.Add(p => p.ShowPreviousButton, false);
        });

        await comp.InvokeAsync(async () => await comp.Instance.FocusAsync());

        Assert.AreEqual(0, CountFocusCalls());
    }



    [TestMethod]
    public void BitPaginationShouldRenderEveryControlAsALinkWithGetPageHref()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Add(p => p.ShowFirstButton, true);
            parameters.Add(p => p.ShowLastButton, true);
            parameters.Add(p => p.DefaultSelectedPage, 3);
            parameters.Add(p => p.GetPageHref, (int page) => $"/results?page={page}");
        });

        // Not one control is left as a button, so none of them swaps its markup while the selection moves.
        Assert.AreEqual(0, comp.FindAll("button").Count);

        var links = comp.FindAll(".bit-pgn-btn");

        // The five pages plus the four navigation controls.
        Assert.AreEqual(9, links.Count);
        Assert.IsTrue(links.All(l => l.TagName == "A"));

        var pages = links.Where(l => l.QuerySelector("i") is null).ToList();

        CollectionAssert.AreEqual(new[] { "/results?page=1", "/results?page=2", "/results?page=3", "/results?page=4", "/results?page=5" },
                                  pages.Select(p => p.GetAttribute("href")).ToArray());

        // The selected page is still the current one, and it is still a link of its own.
        Assert.AreEqual("3", pages.Single(p => p.GetAttribute("aria-current") == "page").TextContent.Trim());

        // The navigation controls point at the page they move to.
        Assert.AreEqual("/results?page=1", FindLinkByAriaLabel(comp, "First page")!.GetAttribute("href"));
        Assert.AreEqual("/results?page=2", FindLinkByAriaLabel(comp, "Previous page")!.GetAttribute("href"));
        Assert.AreEqual("/results?page=4", FindLinkByAriaLabel(comp, "Next page")!.GetAttribute("href"));
        Assert.AreEqual("/results?page=5", FindLinkByAriaLabel(comp, "Last page")!.GetAttribute("href"));
    }

    [TestMethod]
    public void BitPaginationShouldReportAriaDisabledOnTheLinksAtTheEndsOfTheRange()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Add(p => p.ShowFirstButton, true);
            parameters.Add(p => p.ShowLastButton, true);
            parameters.Add(p => p.GetPageHref, (int page) => $"/results?page={page}");
        });

        var first = FindLinkByAriaLabel(comp, "First page")!;
        var previous = FindLinkByAriaLabel(comp, "Previous page")!;

        // A link with nowhere to go carries no address at all, which is what keeps it out of the tab order.
        Assert.IsFalse(first.HasAttribute("href"));
        Assert.IsFalse(previous.HasAttribute("href"));
        Assert.AreEqual("true", first.GetAttribute("aria-disabled"));
        Assert.AreEqual("true", previous.GetAttribute("aria-disabled"));

        Assert.IsNull(FindLinkByAriaLabel(comp, "Next page")!.GetAttribute("aria-disabled"));
        Assert.IsNull(FindLinkByAriaLabel(comp, "Last page")!.GetAttribute("aria-disabled"));
    }

    [TestMethod]
    public void BitPaginationShouldReportAriaDisabledOnEveryLinkWhileDisabled()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.DefaultSelectedPage, 3);
            parameters.Add(p => p.GetPageHref, (int page) => $"/results?page={page}");
        });

        var links = comp.FindAll(".bit-pgn-btn");

        Assert.IsTrue(links.All(l => l.HasAttribute("href") is false));
        Assert.IsTrue(links.All(l => l.GetAttribute("aria-disabled") == "true"));
    }

    [TestMethod]
    public void BitPaginationShouldLeaveAPageWithoutAnAddressOutOfReach()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Add(p => p.ShowNextButton, false);
            parameters.Add(p => p.ShowPreviousButton, false);
            parameters.Add(p => p.GetPageHref, (int page) => page == 4 ? null : $"/results?page={page}");
        });

        var pages = comp.FindAll(".bit-pgn-btn");
        var fourth = pages.Single(p => p.TextContent.Trim() == "4");

        Assert.IsFalse(fourth.HasAttribute("href"));
        Assert.AreEqual("true", fourth.GetAttribute("aria-disabled"));
        Assert.IsTrue(pages.Where(p => p != fourth).All(p => p.HasAttribute("href")));
    }

    [TestMethod]
    public void BitPaginationShouldStillReportThePageAskedForByALink()
    {
        var changed = 0;
        var selected = 1;

        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Bind(p => p.SelectedPage, selected, v => selected = v);
            parameters.Add(p => p.OnChange, v => changed = v);
            parameters.Add(p => p.GetPageHref, (int page) => $"/results?page={page}");
        });

        comp.FindAll(".bit-pgn-btn").Single(p => p.TextContent.Trim() == "4").Click();

        Assert.AreEqual(4, changed);
        Assert.AreEqual(4, selected);
        Assert.AreEqual("4", comp.Find(".bit-pgn-sel").TextContent.Trim());
    }

    [TestMethod]
    public void BitPaginationShouldWalkTheRangeWithTheNavigationLinks()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Add(p => p.ShowFirstButton, true);
            parameters.Add(p => p.ShowLastButton, true);
            parameters.Add(p => p.DefaultSelectedPage, 3);
            parameters.Add(p => p.GetPageHref, (int page) => $"/results?page={page}");
        });

        FindLinkByAriaLabel(comp, "Next page")!.Click();
        Assert.AreEqual("4", comp.Find(".bit-pgn-sel").TextContent.Trim());

        FindLinkByAriaLabel(comp, "Previous page")!.Click();
        Assert.AreEqual("3", comp.Find(".bit-pgn-sel").TextContent.Trim());

        FindLinkByAriaLabel(comp, "Last page")!.Click();
        Assert.AreEqual("5", comp.Find(".bit-pgn-sel").TextContent.Trim());

        FindLinkByAriaLabel(comp, "First page")!.Click();
        Assert.AreEqual("1", comp.Find(".bit-pgn-sel").TextContent.Trim());
    }

    [TestMethod]
    public void BitPaginationShouldNotMoveAwayFromAPageALinkAtTheEndOfTheRangePointsAt()
    {
        // A link that reports aria-disabled is still clickable with a mouse, so the page it would land on has
        // to be the one the pagination is already on.
        var changed = 0;

        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Add(p => p.OnChange, v => changed = v);
            parameters.Add(p => p.GetPageHref, (int page) => $"/results?page={page}");
        });

        FindLinkByAriaLabel(comp, "Previous page")!.Click();

        Assert.AreEqual(0, changed);
        Assert.AreEqual("1", comp.Find(".bit-pgn-sel").TextContent.Trim());
    }

    [TestMethod]
    public void BitPaginationShouldKeepTheLinksInPlaceWhileTheSelectionMoves()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Add(p => p.GetPageHref, (int page) => $"/results?page={page}");
        });

        var before = GetRenderedPages(comp);

        comp.FindAll(".bit-pgn-btn").Single(p => p.TextContent.Trim() == "3").Click();

        CollectionAssert.AreEqual(before, GetRenderedPages(comp));
        Assert.AreEqual(0, comp.FindAll("button").Count);
        Assert.AreEqual("3", comp.Find(".bit-pgn-sel").TextContent.Trim());
    }

    [TestMethod]
    public void BitPaginationShouldWrapTheNavigationLinksAroundTheRangeWhileLooping()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Add(p => p.Loop, true);
            parameters.Add(p => p.GetPageHref, (int page) => $"/results?page={page}");
        });

        // The link points at the page the wrap lands on, so its address is the page it actually reaches.
        Assert.AreEqual("/results?page=5", FindLinkByAriaLabel(comp, "Previous page")!.GetAttribute("href"));
        Assert.IsNull(FindLinkByAriaLabel(comp, "Previous page")!.GetAttribute("aria-disabled"));

        FindLinkByAriaLabel(comp, "Previous page")!.Click();

        Assert.AreEqual("5", comp.Find(".bit-pgn-sel").TextContent.Trim());
        Assert.AreEqual("/results?page=1", FindLinkByAriaLabel(comp, "Next page")!.GetAttribute("href"));
    }

    [TestMethod]
    public void BitPaginationShouldRenderTheNavigationButtonTextsInsideTheLinks()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Add(p => p.NextButtonText, "Next");
            parameters.Add(p => p.PreviousButtonText, "Previous");
            parameters.Add(p => p.DefaultSelectedPage, 3);
            parameters.Add(p => p.GetPageHref, (int page) => $"/results?page={page}");
        });

        var next = FindLinkByAriaLabel(comp, "Next page")!;
        var previous = FindLinkByAriaLabel(comp, "Previous page")!;

        Assert.AreEqual("Next", next.QuerySelector(".bit-pgn-btx")!.TextContent.Trim());
        Assert.AreEqual("Previous", previous.QuerySelector(".bit-pgn-btx")!.TextContent.Trim());
        Assert.IsTrue(next.ClassList.Contains("bit-pgn-lbl"));
        Assert.IsTrue(previous.ClassList.Contains("bit-pgn-lbl"));
    }

    [TestMethod]
    public void BitPaginationShouldOfferAPageSizeThatIsNotOneOfTheOptions()
    {
        // A selector that does not hold the size the pagination runs on would show one size while it reports
        // another, so the picked size is offered along with the options, in its place among them.
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 12);
            parameters.Add(p => p.PageSize, 30);
            parameters.Add(p => p.ShowPageSizeSelector, true);
        });

        var options = comp.FindAll(".bit-pgn-pse option");

        CollectionAssert.AreEqual(new[] { "10", "25", "30", "50", "100" }, options.Select(o => o.TextContent.Trim()).ToArray());
        Assert.AreEqual("30", options.Single(o => o.HasAttribute("selected")).GetAttribute("value"));
    }

    [TestMethod]
    public void BitPaginationShouldNotRepeatAPageSizeThatIsAlreadyOneOfTheOptions()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 12);
            parameters.Add(p => p.PageSize, 25);
            parameters.Add(p => p.ShowPageSizeSelector, true);
        });

        CollectionAssert.AreEqual(new[] { "10", "25", "50", "100" },
                                  comp.FindAll(".bit-pgn-pse option").Select(o => o.TextContent.Trim()).ToArray());
    }

    [TestMethod]
    public void BitPaginationShouldRenderTheRangeForAnEvenMiddleCount()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 20);
            parameters.Add(p => p.MiddleCount, 4);
            parameters.Add(p => p.BoundaryCount, 2);
            parameters.Add(p => p.DefaultSelectedPage, 10);
        });

        CollectionAssert.AreEqual(new[] { "1", "2", "•••", "8", "9", "10", "11", "•••", "19", "20" }, GetRenderedPages(comp));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-3)]
    public void BitPaginationShouldHideANonPositiveCountWithHideOnSinglePage(int count)
    {
        // A count that is not positive still leaves the single page there is to be on, which is the page the
        // navigation cannot go anywhere from.
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, count);
            parameters.Add(p => p.HideOnSinglePage, true);
        });

        Assert.AreEqual(0, comp.FindAll(".bit-pgn").Count);
    }

    [TestMethod]
    public void BitPaginationShouldRenderAnEmptyEllipsisTextWithoutLosingItsPlace()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 50);
            parameters.Add(p => p.EllipsisText, "");
            parameters.Add(p => p.DefaultSelectedPage, 25);
        });

        var ellipsis = comp.FindAll(".bit-pgn-elp");

        Assert.AreEqual(2, ellipsis.Count);
        Assert.IsTrue(ellipsis.All(e => e.TextContent.Trim().Length == 0));

        // The item is still named, so the gap in the range is still reported.
        Assert.IsTrue(comp.FindAll(".bit-pgn-itm")
                          .Where(i => i.QuerySelector(".bit-pgn-elp") is not null)
                          .All(i => i.GetAttribute("aria-label") == "More pages"));
    }

    [TestMethod]
    public void BitPaginationShouldWorkTheCountOutOfTheTotalItems()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            // The page count follows the number of items and the page size, so the count that is handed over
            // beside them has nothing left to say.
            parameters.Add(p => p.Count, 3);
            parameters.Add(p => p.TotalItems, 45);
            parameters.Add(p => p.PageSize, 10);
        });

        // A page that is not filled to the end is still a page, so the division rounds up.
        CollectionAssert.AreEqual(new[] { "1", "2", "3", "4", "5" }, GetRenderedPages(comp));
    }

    [TestMethod]
    [DataRow(0, 3)]
    [DataRow(-8, 3)]
    public void BitPaginationShouldFallBackToTheCountForANonPositiveTotalItems(int totalItems, int expected)
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 3);
            parameters.Add(p => p.TotalItems, totalItems);
        });

        Assert.AreEqual(expected, GetRenderedPages(comp).Length);
    }

    [TestMethod]
    public void BitPaginationShouldLeaveASinglePageForFewerItemsThanAPageHolds()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.TotalItems, 4);
            parameters.Add(p => p.PageSize, 10);
        });

        CollectionAssert.AreEqual(new[] { "1" }, GetRenderedPages(comp));
    }

    [TestMethod]
    public void BitPaginationShouldFollowThePageSizeOfTheOptionsForTheTotalItems()
    {
        // A page size that was never picked is the first of the offered ones, which is the size the range of
        // pages has to be worked out from as well.
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.TotalItems, 100);
            parameters.Add(p => p.PageSizeOptions, [25, 50]);
        });

        CollectionAssert.AreEqual(new[] { "1", "2", "3", "4" }, GetRenderedPages(comp));
    }

    [TestMethod]
    public void BitPaginationShouldBoundTheGoToPageByTheCountOfTheTotalItems()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.TotalItems, 95);
            parameters.Add(p => p.PageSize, 10);
            parameters.Add(p => p.ShowGoToPage, true);
        });

        Assert.AreEqual("10", comp.Find(".bit-pgn-gti").GetAttribute("max"));
    }

    [TestMethod]
    public void BitPaginationShouldHideASinglePageWorkedOutOfTheTotalItems()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.TotalItems, 7);
            parameters.Add(p => p.PageSize, 10);
            parameters.Add(p => p.HideOnSinglePage, true);
        });

        Assert.AreEqual(0, comp.FindAll(".bit-pgn").Count);

        comp.Render(parameters => parameters.Add(p => p.TotalItems, 17));

        Assert.AreEqual(1, comp.FindAll(".bit-pgn").Count);
    }

    [TestMethod]
    public void BitPaginationShouldPullTheSelectedPageBackWhenTheTotalItemsShrink()
    {
        var page = 9;

        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.TotalItems, 100);
            parameters.Add(p => p.PageSize, 10);
            parameters.Bind(p => p.SelectedPage, page, v => page = v);
        });

        Assert.AreEqual("9", comp.Find(".bit-pgn-sel").TextContent.Trim());

        comp.Render(parameters => parameters.Add(p => p.TotalItems, 25));

        Assert.AreEqual(3, page);
        Assert.AreEqual("3", comp.Find(".bit-pgn-sel").TextContent.Trim());
    }

    [TestMethod]
    public void BitPaginationShouldReportTheItemsInTheSummaryWithTheTotalItems()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.TotalItems, 240);
            parameters.Add(p => p.PageSize, 10);
            parameters.Add(p => p.ShowSummary, true);
        });

        Assert.AreEqual("1 - 10 of 240", comp.Find(".bit-pgn-sum").TextContent.Trim());

        FindByAriaLabel(comp, "Next page")!.Click();

        Assert.AreEqual("11 - 20 of 240", comp.Find(".bit-pgn-sum").TextContent.Trim());
    }

    [TestMethod]
    public void BitPaginationShouldStopTheSummaryRangeAtTheLastItem()
    {
        // The last page is rarely filled to the end, so the range it reports stops at the item the set does.
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.TotalItems, 245);
            parameters.Add(p => p.PageSize, 10);
            parameters.Add(p => p.ShowSummary, true);
            parameters.Add(p => p.DefaultSelectedPage, 25);
        });

        Assert.AreEqual("241 - 245 of 245", comp.Find(".bit-pgn-sum").TextContent.Trim());
    }

    [TestMethod]
    public void BitPaginationShouldLetGetSummaryReplaceTheItemRange()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.TotalItems, 240);
            parameters.Add(p => p.PageSize, 10);
            parameters.Add(p => p.ShowSummary, true);
            parameters.Add(p => p.GetSummary, (int page, int count) => $"{page}/{count}");
        });

        Assert.AreEqual("1/24", comp.Find(".bit-pgn-sum").TextContent.Trim());
    }

    [TestMethod]
    public void BitPaginationShouldKeepReportingThePagesInTheSummaryWithoutTheTotalItems()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 12);
            parameters.Add(p => p.ShowSummary, true);
            parameters.Add(p => p.DefaultSelectedPage, 4);
        });

        Assert.AreEqual("Page 4 of 12", comp.Find(".bit-pgn-sum").TextContent.Trim());
    }

    [TestMethod]
    public void BitPaginationShouldRecomputeTheRangeWhenThePageSizeIsPickedWithTheTotalItems()
    {
        var size = 10;

        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.TotalItems, 240);
            parameters.Add(p => p.ShowPageSizeSelector, true);
            parameters.Bind(p => p.PageSize, size, v => size = v);
        });

        Assert.AreEqual(24, GetLastRenderedPage(comp));

        comp.Find(".bit-pgn-pse").Change("50");

        Assert.AreEqual(50, size);
        Assert.AreEqual(5, GetLastRenderedPage(comp));
    }

    [TestMethod]
    public void BitPaginationShouldKeepTheFirstItemOfThePageInViewWhenThePageSizeIsPicked()
    {
        var size = 10;
        var page = 5;
        var changed = -1;

        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.TotalItems, 240);
            parameters.Add(p => p.ShowPageSizeSelector, true);
            parameters.Add(p => p.OnChange, (int p) => changed = p);
            parameters.Bind(p => p.PageSize, size, v => size = v);
            parameters.Bind(p => p.SelectedPage, page, v => page = v);
        });

        // Page 5 of ten items a page holds the items 41 to 50, and the first of them is on page 2 of twenty
        // five items a page, which holds the items 26 to 50.
        comp.Find(".bit-pgn-pse").Change("25");

        Assert.AreEqual(2, page);
        Assert.AreEqual(2, changed);
        Assert.AreEqual("2", comp.Find(".bit-pgn-sel").TextContent.Trim());
    }

    [TestMethod]
    public void BitPaginationShouldPullThePageIntoTheRangeAPickedPageSizeLeaves()
    {
        var size = 10;
        var page = 24;

        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.TotalItems, 240);
            parameters.Add(p => p.ShowPageSizeSelector, true);
            parameters.Bind(p => p.PageSize, size, v => size = v);
            parameters.Bind(p => p.SelectedPage, page, v => page = v);
        });

        // The last page of ten items a page starts at the item 231, which the third page of a hundred items a
        // page holds, and that is also the last page there is left.
        comp.Find(".bit-pgn-pse").Change("100");

        Assert.AreEqual(3, page);
        Assert.AreEqual("3", comp.Find(".bit-pgn-sel").TextContent.Trim());
        Assert.AreEqual(3, GetLastRenderedPage(comp));
    }

    [TestMethod]
    public void BitPaginationShouldNotReportAPageChangeWhenAPickedPageSizeLeavesItWhereItWas()
    {
        var changedCount = 0;

        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.TotalItems, 240);
            parameters.Add(p => p.ShowPageSizeSelector, true);
            parameters.Add(p => p.OnChange, (int _) => changedCount++);
        });

        // The first page is the first page whichever size it is read at.
        comp.Find(".bit-pgn-pse").Change("50");

        Assert.AreEqual(0, changedCount);
    }

    [TestMethod]
    public void BitPaginationShouldNotMoveThePageForAPageSizeItCouldNotApply()
    {
        // A page size bound one way belongs to the consumer, so the range it would work out is not the range
        // the pagination is on until the consumer hands the new size back.
        var page = 5;
        var changed = 0;

        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.PageSize, 10);
            parameters.Add(p => p.TotalItems, 240);
            parameters.Add(p => p.ShowPageSizeSelector, true);
            parameters.Add(p => p.OnPageSizeChange, (int v) => changed = v);
            parameters.Bind(p => p.SelectedPage, page, v => page = v);
        });

        comp.Find(".bit-pgn-pse").Change("100");

        Assert.AreEqual(100, changed);
        Assert.AreEqual(5, page);
        Assert.AreEqual("5", comp.Find(".bit-pgn-sel").TextContent.Trim());
    }

    [TestMethod]
    public void BitPaginationShouldLeaveTheSelectedPageAloneWhenAPageSizeIsPickedWithoutTheTotalItems()
    {
        // Without the number of items the range is the consumer's to recompute, so the pagination has nothing
        // to move the page by and reports the size only.
        var size = 10;
        var page = 5;

        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 24);
            parameters.Add(p => p.ShowPageSizeSelector, true);
            parameters.Bind(p => p.PageSize, size, v => size = v);
            parameters.Bind(p => p.SelectedPage, page, v => page = v);
        });

        comp.Find(".bit-pgn-pse").Change("50");

        Assert.AreEqual(50, size);
        Assert.AreEqual(5, page);
    }

    [TestMethod]
    public void BitPaginationShouldNotRenderTheEllipsisAsAControlByDefault()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 50);
            parameters.Add(p => p.DefaultSelectedPage, 25);
        });

        var ellipsis = comp.FindAll(".bit-pgn-elp");

        Assert.AreEqual(2, ellipsis.Count);
        Assert.IsTrue(ellipsis.All(e => e.TagName == "DIV"));
        Assert.IsTrue(ellipsis.All(e => e.ClassList.Contains("bit-pgn-elb") is false));
    }

    [TestMethod]
    public void BitPaginationShouldRespectClickableEllipsis()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 50);
            parameters.Add(p => p.BoundaryCount, 1);
            parameters.Add(p => p.MiddleCount, 3);
            parameters.Add(p => p.ClickableEllipsis, true);
            parameters.Add(p => p.DefaultSelectedPage, 25);
        });

        var ellipsis = comp.FindAll(".bit-pgn-elp");

        Assert.AreEqual(2, ellipsis.Count);
        Assert.IsTrue(ellipsis.All(e => e.TagName == "BUTTON"));
        Assert.IsTrue(ellipsis.All(e => e.GetAttribute("type") == "button"));

        // The control is named itself, so the item around it is left unnamed instead of naming it twice.
        Assert.IsTrue(ellipsis.All(e => e.GetAttribute("aria-label") == "More pages"));
        Assert.IsTrue(ellipsis.All(e => e.GetAttribute("title") == "More pages"));
        Assert.IsTrue(ellipsis.All(e => e.HasAttribute("aria-hidden") is false));
        Assert.IsTrue(comp.FindAll(".bit-pgn-itm").All(i => i.HasAttribute("aria-label") is false));
    }

    [TestMethod]
    public void BitPaginationShouldJumpIntoTheMiddleOfThePagesAClickableEllipsisHides()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 50);
            parameters.Add(p => p.BoundaryCount, 1);
            parameters.Add(p => p.MiddleCount, 3);
            parameters.Add(p => p.ClickableEllipsis, true);
            parameters.Add(p => p.DefaultSelectedPage, 25);
        });

        CollectionAssert.AreEqual(new[] { "1", "•••", "24", "25", "26", "•••", "50" }, GetRenderedPages(comp));

        // The gap between 26 and 50 is jumped into halfway along.
        comp.FindAll(".bit-pgn-elp")[1].Click();

        Assert.AreEqual("38", comp.Find(".bit-pgn-sel").TextContent.Trim());

        // The range moved along with the selection, so the gap that is jumped into next is the new one.
        CollectionAssert.AreEqual(new[] { "1", "•••", "37", "38", "39", "•••", "50" }, GetRenderedPages(comp));

        comp.FindAll(".bit-pgn-elp")[0].Click();

        Assert.AreEqual("19", comp.Find(".bit-pgn-sel").TextContent.Trim());
    }

    [TestMethod]
    public void BitPaginationShouldReportThePageAClickableEllipsisJumpedTo()
    {
        var changed = 0;
        var selected = 25;

        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 50);
            parameters.Add(p => p.BoundaryCount, 1);
            parameters.Add(p => p.ClickableEllipsis, true);
            parameters.Add(p => p.OnChange, (int v) => changed = v);
            parameters.Bind(p => p.SelectedPage, selected, v => selected = v);
        });

        comp.FindAll(".bit-pgn-elp")[0].Click();

        Assert.AreEqual(12, changed);
        Assert.AreEqual(12, selected);
    }

    [TestMethod]
    public void BitPaginationShouldRenderAClickableEllipsisAsALinkAlongWithTheRest()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 50);
            parameters.Add(p => p.BoundaryCount, 1);
            parameters.Add(p => p.MiddleCount, 3);
            parameters.Add(p => p.ClickableEllipsis, true);
            parameters.Add(p => p.DefaultSelectedPage, 25);
            parameters.Add(p => p.GetPageHref, (int page) => $"/results?page={page}");
        });

        var ellipsis = comp.FindAll(".bit-pgn-elp");

        Assert.AreEqual(2, ellipsis.Count);
        Assert.IsTrue(ellipsis.All(e => e.TagName == "A"));

        // The address is the page the jump lands on, so following the link reaches what clicking it does.
        Assert.AreEqual("/results?page=12", ellipsis[0].GetAttribute("href"));
        Assert.AreEqual("/results?page=38", ellipsis[1].GetAttribute("href"));
    }

    [TestMethod]
    public void BitPaginationShouldDisableAClickableEllipsisWhileDisabled()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 50);
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.ClickableEllipsis, true);
            parameters.Add(p => p.DefaultSelectedPage, 25);
        });

        Assert.IsTrue(comp.FindAll(".bit-pgn-elp").All(e => e.HasAttribute("disabled")));

        comp.FindAll(".bit-pgn-elp")[0].Click();

        Assert.AreEqual("25", comp.Find(".bit-pgn-sel").TextContent.Trim());
    }

    [TestMethod]
    public void BitPaginationShouldReportAriaDisabledOnAClickableEllipsisLinkWhileDisabled()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 50);
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.ClickableEllipsis, true);
            parameters.Add(p => p.DefaultSelectedPage, 25);
            parameters.Add(p => p.GetPageHref, (int page) => $"/results?page={page}");
        });

        var ellipsis = comp.FindAll(".bit-pgn-elp");

        Assert.IsTrue(ellipsis.All(e => e.HasAttribute("href") is false));
        Assert.IsTrue(ellipsis.All(e => e.GetAttribute("aria-disabled") == "true"));
    }

    [TestMethod]
    public void BitPaginationShouldRespectTheEllipsisTextAndAriaLabelOnAClickableEllipsis()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 50);
            parameters.Add(p => p.EllipsisText, "...");
            parameters.Add(p => p.EllipsisAriaLabel, "Hidden pages");
            parameters.Add(p => p.ClickableEllipsis, true);
            parameters.Add(p => p.DefaultSelectedPage, 25);
        });

        var ellipsis = comp.FindAll(".bit-pgn-elp");

        Assert.IsTrue(ellipsis.All(e => e.TextContent.Trim() == "..."));
        Assert.IsTrue(ellipsis.All(e => e.GetAttribute("aria-label") == "Hidden pages"));
    }

    [TestMethod]
    [DataRow(BitAlignment.Start, "flex-start")]
    [DataRow(BitAlignment.End, "flex-end")]
    [DataRow(BitAlignment.Center, "center")]
    [DataRow(BitAlignment.SpaceBetween, "space-between")]
    [DataRow(BitAlignment.SpaceAround, "space-around")]
    [DataRow(BitAlignment.SpaceEvenly, "space-evenly")]
    [DataRow(BitAlignment.Baseline, "baseline")]
    [DataRow(BitAlignment.Stretch, "stretch")]
    public void BitPaginationShouldRespectAlignment(BitAlignment alignment, string value)
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Add(p => p.Alignment, alignment);
        });

        var root = comp.Find(".bit-pgn");

        // The pagination is stretched across the room it is given first, since a box that is only as wide as
        // its own controls has nothing to line them up inside of.
        Assert.IsTrue(root.ClassList.Contains("bit-pgn-aln"));
        Assert.IsTrue(root.GetAttribute("style")!.Contains($"--bit-pgn-justify-content:{value}"));
    }

    [TestMethod]
    public void BitPaginationShouldNotStretchItselfWithoutAnAlignment()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
        });

        var root = comp.Find(".bit-pgn");

        Assert.IsFalse(root.ClassList.Contains("bit-pgn-aln"));
        Assert.IsFalse(root.GetAttribute("style")?.Contains("--bit-pgn-justify-content") ?? false);
    }

    [TestMethod]
    public void BitPaginationShouldRelateTheNavigationLinksToThePagesTheyReach()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Add(p => p.ShowFirstButton, true);
            parameters.Add(p => p.ShowLastButton, true);
            parameters.Add(p => p.DefaultSelectedPage, 3);
            parameters.Add(p => p.GetPageHref, (int page) => $"/results?page={page}");
        });

        Assert.AreEqual("prev", FindLinkByAriaLabel(comp, "Previous page")!.GetAttribute("rel"));
        Assert.AreEqual("next", FindLinkByAriaLabel(comp, "Next page")!.GetAttribute("rel"));

        // The two ends of the range are not a step along it, so they carry no relation of their own.
        Assert.IsNull(FindLinkByAriaLabel(comp, "First page")!.GetAttribute("rel"));
        Assert.IsNull(FindLinkByAriaLabel(comp, "Last page")!.GetAttribute("rel"));
    }

    [TestMethod]
    public void BitPaginationShouldDropTheRelationOfANavigationLinkWithNowhereToGo()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Add(p => p.GetPageHref, (int page) => $"/results?page={page}");
        });

        Assert.IsNull(FindLinkByAriaLabel(comp, "Previous page")!.GetAttribute("rel"));
        Assert.AreEqual("next", FindLinkByAriaLabel(comp, "Next page")!.GetAttribute("rel"));
    }

    [TestMethod]
    public void BitPaginationShouldDropTheTooltipOfANavigationButtonCarryingAText()
    {
        // A tooltip repeating the text that is already on screen beside it says nothing new, while the spoken
        // name stays the fuller one the aria label holds.
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 10);
            parameters.Add(p => p.NextButtonText, "Next");
            parameters.Add(p => p.DefaultSelectedPage, 5);
        });

        var next = FindByAriaLabel(comp, "Next page")!;
        var previous = FindByAriaLabel(comp, "Previous page")!;

        Assert.IsFalse(next.HasAttribute("title"));
        Assert.AreEqual("Next page", next.GetAttribute("aria-label"));

        // The one that carries an icon and nothing else keeps it.
        Assert.AreEqual("Previous page", previous.GetAttribute("title"));
    }

    [TestMethod]
    public void BitPaginationShouldRespectTheButtonTextClassesAndStyles()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 10);
            parameters.Add(p => p.NextButtonText, "Next");
            parameters.Add(p => p.Classes, new BitPaginationClassStyles { ButtonText = "custom-button-text" });
            parameters.Add(p => p.Styles, new BitPaginationClassStyles { ButtonText = "color: red;" });
        });

        var text = comp.Find(".bit-pgn-btx");

        Assert.IsTrue(text.ClassList.Contains("custom-button-text"));
        Assert.AreEqual("color: red;", text.GetAttribute("style"));
    }

    [TestMethod]
    public void BitPaginationShouldKeepTheGoToPageOutOfTheAutofill()
    {
        // The field is a jump to a page and not a detail of the person filling the form in.
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 12);
            parameters.Add(p => p.ShowGoToPage, true);
        });

        Assert.AreEqual("off", comp.Find(".bit-pgn-gti").GetAttribute("autocomplete"));
    }

    [TestMethod]
    public async Task BitPaginationShouldNotFocusAnythingWhileItRendersNothing()
    {
        // The markup the element references point at is gone along with the pagination, so a focus call on it
        // would reach an element that is no longer there.
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Add(p => p.HideOnSinglePage, true);
            parameters.Add(p => p.DefaultSelectedPage, 3);
        });

        comp.Render(parameters => parameters.Add(p => p.Count, 1));

        Assert.AreEqual(0, comp.FindAll(".bit-pgn").Count);

        await comp.InvokeAsync(async () => await comp.Instance.FocusAsync());

        Assert.AreEqual(0, CountFocusCalls());
    }

    [TestMethod]
    public void BitPaginationShouldNotRenderAClickableEllipsisAsAPageButton()
    {
        // The gap is a shortcut into the range and not one of the pages of it, so it carries neither the class
        // nor the styling hooks of a page button.
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 50);
            parameters.Add(p => p.ClickableEllipsis, true);
            parameters.Add(p => p.DefaultSelectedPage, 25);
            parameters.Add(p => p.Classes, new BitPaginationClassStyles { Button = "button-class", Ellipsis = "ellipsis-class" });
            parameters.Add(p => p.Styles, new BitPaginationClassStyles { Ellipsis = "color: red;" });
        });

        var ellipsis = comp.FindAll(".bit-pgn-elp");

        Assert.AreEqual(2, ellipsis.Count);
        Assert.IsTrue(ellipsis.All(e => e.ClassList.Contains("bit-pgn-btn") is false));
        Assert.IsTrue(ellipsis.All(e => e.ClassList.Contains("button-class") is false));

        // The ellipsis hooks reach it in both of its forms.
        Assert.IsTrue(ellipsis.All(e => e.ClassList.Contains("ellipsis-class")));
        Assert.IsTrue(ellipsis.All(e => e.GetAttribute("style") == "color: red;"));

        Assert.AreEqual(0, comp.FindAll(".bit-pgn-btn").Count(b => b.ClassList.Contains("bit-pgn-elp")));
    }

    [TestMethod]
    public void BitPaginationShouldKeepTheRootStylesBesideTheAlignment()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.Count, 5);
            parameters.Add(p => p.Alignment, BitAlignment.Center);
            parameters.Add(p => p.Styles, new BitPaginationClassStyles { Root = "padding: 1rem;" });
        });

        var style = comp.Find(".bit-pgn").GetAttribute("style")!;

        Assert.IsTrue(style.Contains("--bit-pgn-justify-content:center"));
        Assert.IsTrue(style.Contains("padding: 1rem;"));
    }

    [TestMethod]
    public void BitPaginationShouldLandTheGoToPageInsideTheRangeTheTotalItemsLeave()
    {
        var comp = RenderComponent<BitPagination>(parameters =>
        {
            parameters.Add(p => p.TotalItems, 95);
            parameters.Add(p => p.PageSize, 10);
            parameters.Add(p => p.ShowGoToPage, true);
        });

        comp.Find(".bit-pgn-gti").Change("40");

        // Ninety five items at ten a page leave ten pages, so the jump stops at the last of them.
        Assert.AreEqual("10", comp.Find(".bit-pgn-sel").TextContent.Trim());
    }

    private static int GetLastRenderedPage(IRenderedComponent<BitPagination> comp)
    {
        return int.Parse(GetRenderedPages(comp)[^1]);
    }



    private static IElement? FindLinkByAriaLabel(IRenderedComponent<BitPagination> comp, string label)
    {
        return comp.FindAll("a").FirstOrDefault(a => a.GetAttribute("aria-label") == label);
    }

    private int CountFocusCalls()
    {
        return Context.JSInterop.Invocations.Count(i => i.Identifier.Contains("focus", System.StringComparison.OrdinalIgnoreCase));
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

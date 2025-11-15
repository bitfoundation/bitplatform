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

        // should render 5 page buttons
        var buttons = comp.FindAll(".bit-pgn-btn");
        Assert.IsTrue(buttons.Count >= 5);

        // selected page should have bit-pgn-sel class and show 3
        var selected = comp.Find(".bit-pgn-sel");
        Assert.IsNotNull(selected);
        Assert.IsTrue(selected.TextContent.Contains("3"));
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

        // click next page button (page 3)
        IElement page3 = null;
        foreach (var b in comp.FindAll(".bit-pgn-btn"))
        {
            if (b.TextContent.Trim() == "3")
            {
                page3 = b;
                break;
            }
        }

        Assert.IsNotNull(page3);

        page3!.Click();

        Assert.AreEqual(3, selected);
        Assert.AreEqual(3, changedValue);
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

        // find first and last buttons by aria-label
        IElement? first = null;
        IElement? last = null;
        IElement? next = null;
        IElement? prev = null;

        foreach (var b in comp.FindAll("button"))
        {
            var aria = b.GetAttribute("aria-label");
            if (aria == "First page") first = b;
            if (aria == "Last page") last = b;
            if (aria == "Next page") next = b;
            if (aria == "Previous page") prev = b;
        }

        Assert.IsNotNull(first);
        Assert.IsNotNull(last);
        Assert.IsNotNull(next);
        Assert.IsNotNull(prev);
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

        var ellipsis = comp.FindAll(".bit-pgn-elp");
        Assert.IsTrue(ellipsis.Count >= 1);
    }
}

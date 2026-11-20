using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Lists.Carousel;

/// <summary>
/// ButtonsPlacement, which takes the next/prev buttons out of the slides, and the navigation state a page reads to draw
/// controls of its own (CanGoNext, CanGoPrev and OnNavigationStateChange).
/// </summary>
public partial class BitCarouselTests
{
    // Only a side the dots are not on asks anything of the root: there the buttons get a row of their own, which the root
    // lays out on a grid. On the side of the dots they join the dots' row, and the values ButtonsPlacement does not honour
    // keep them over the slides.
    [TestMethod,
        DataRow(null, null, null),
        DataRow(BitPlacement.Bottom, null, null),
        DataRow(BitPlacement.Top, null, "bit-csl-btop"),
        DataRow(BitPlacement.Start, null, "bit-csl-bstr"),
        DataRow(BitPlacement.End, null, "bit-csl-bend"),
        DataRow(BitPlacement.Bottom, BitPlacement.Top, "bit-csl-bbot"),
        DataRow(BitPlacement.Top, BitPlacement.Top, null),
        DataRow(BitPlacement.End, BitPlacement.End, null),
        DataRow(BitPlacement.Bottom, BitPlacement.Center, null),
        DataRow(BitPlacement.Left, null, null),
        DataRow(BitPlacement.Center, null, null),
        DataRow(BitPlacement.StartAndEnd, null, null)
    ]
    public void BitCarouselShouldRespectButtonsPlacement(BitPlacement? buttons, BitPlacement? dots, string? expectedClass)
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.ButtonsPlacement, buttons);
            parameters.Add(p => p.DotsPlacement, dots);
        });

        var classes = component.Find(".bit-csl").ClassList;

        foreach (var cls in new[] { "bit-csl-btop", "bit-csl-bbot", "bit-csl-bstr", "bit-csl-bend" })
        {
            Assert.AreEqual(cls == expectedClass, classes.Contains(cls), cls);
        }

        Assert.AreEqual(expectedClass is not null, classes.Contains("bit-csl-bsep"));
    }

    [TestMethod,
        DataRow(null),
        DataRow(BitPlacement.Left),
        DataRow(BitPlacement.Center),
        DataRow(BitPlacement.TopAndBottom)
    ]
    public void BitCarouselShouldKeepTheButtonsOverTheSlidesWithoutASideToPutThemOn(BitPlacement? buttons)
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.ButtonsPlacement, buttons);
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.FindAll(".bit-csl-dot").Count));

        var container = component.Find(".bit-csl-cnt");

        Assert.IsTrue(container.Children[0].ClassList.Contains("bit-csl-pbt"));
        Assert.IsTrue(container.Children[1].ClassList.Contains("bit-csl-nbt"));
        Assert.AreEqual(0, component.Find(".bit-csl-dcn").QuerySelectorAll(".bit-csl-pbt, .bit-csl-nbt").Length);
        Assert.AreEqual(0, component.FindAll(".bit-csl-bcn").Count);
    }

    // The controls are read in the order they are laid out in: the play/pause button first, then the previous button,
    // the dots and the next button.
    [TestMethod]
    public void BitCarouselShouldPutTheButtonsAroundTheDotsOnTheirSide()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.ButtonsPlacement, BitPlacement.Bottom);
            parameters.Add(p => p.AutoPlay, true);
            parameters.Add(p => p.ShowPlayPause, true);
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.FindAll(".bit-csl-dot").Count));

        var row = component.Find(".bit-csl-dcn").Children;

        Assert.AreEqual(4, row.Length);
        Assert.IsTrue(row[0].ClassList.Contains("bit-csl-ppb"));
        Assert.IsTrue(row[1].ClassList.Contains("bit-csl-pbt"));
        Assert.IsTrue(row[2].ClassList.Contains("bit-csl-dts"));
        Assert.IsTrue(row[3].ClassList.Contains("bit-csl-nbt"));

        Assert.AreEqual(0, component.Find(".bit-csl-cnt").QuerySelectorAll(".bit-csl-pbt, .bit-csl-nbt").Length);
        Assert.AreEqual(0, component.FindAll(".bit-csl-bcn").Count);
    }

    [TestMethod]
    public void BitCarouselShouldPutTheButtonsAroundTheDotsInAStartOrEndColumn()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.Vertical, true);
            parameters.Add(p => p.DotsPlacement, BitPlacement.End);
            parameters.Add(p => p.ButtonsPlacement, BitPlacement.End);
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.FindAll(".bit-csl-dot").Count));

        var row = component.Find(".bit-csl-dcn").Children;

        Assert.AreEqual(3, row.Length);
        Assert.IsTrue(row[0].ClassList.Contains("bit-csl-pbt"));
        Assert.IsTrue(row[1].ClassList.Contains("bit-csl-dts"));
        Assert.IsTrue(row[2].ClassList.Contains("bit-csl-nbt"));

        // A vertical carousel's buttons point up and down wherever they are placed.
        Assert.IsTrue(row[0].QuerySelector("i")!.ClassList.Contains("bit-icon--ChevronUp"));
        Assert.IsTrue(row[2].QuerySelector("i")!.ClassList.Contains("bit-icon--ChevronDown"));
    }

    [TestMethod]
    public void BitCarouselShouldLetTheButtonsHoldTheRowOfHiddenDots()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.HideDots, true);
            parameters.Add(p => p.ButtonsPlacement, BitPlacement.Bottom);
        });

        component.WaitForAssertion(() => Assert.AreEqual(2, component.Find(".bit-csl-dcn").Children.Length));

        var row = component.Find(".bit-csl-dcn").Children;

        Assert.IsTrue(row[0].ClassList.Contains("bit-csl-pbt"));
        Assert.IsTrue(row[1].ClassList.Contains("bit-csl-nbt"));
        Assert.AreEqual(0, component.FindAll(".bit-csl-dts").Count);
    }

    // The previous button sits at the start of the row in either direction, and the default chevrons point to the
    // sides the direction puts the buttons on, as they do over the slides.
    [TestMethod]
    public void BitCarouselShouldKeepTheButtonsInReadingOrderInRtl()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
            parameters.Add(p => p.ButtonsPlacement, BitPlacement.Bottom);
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.FindAll(".bit-csl-dot").Count));

        var row = component.Find(".bit-csl-dcn").Children;

        Assert.IsTrue(row[0].ClassList.Contains("bit-csl-pbt"));
        Assert.IsTrue(row[2].ClassList.Contains("bit-csl-nbt"));

        AssertDefaultIconsPointTo(component.Find(".bit-csl-nbt i"), component.Find(".bit-csl-pbt i"), rtl: true);
    }

    [TestMethod]
    public void BitCarouselShouldGiveTheButtonsARowOfTheirOwnOnAnotherSide()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.ButtonsPlacement, BitPlacement.Top);
            parameters.Add(p => p.Classes, new BitCarouselClassStyles { ButtonsContainer = "buttons-class" });
            parameters.Add(p => p.Styles, new BitCarouselClassStyles { ButtonsContainer = "color: red;" });
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll(".bit-csl-bcn").Count));

        var root = component.Find(".bit-csl").Children;
        var buttonsRow = component.Find(".bit-csl-bcn");

        // The row is rendered before the slides, after the row of the dots, as the controls come before what they control.
        Assert.IsTrue(root[0].ClassList.Contains("bit-csl-dcn"));
        Assert.IsTrue(root[1].ClassList.Contains("bit-csl-bcn"));
        Assert.IsTrue(root[2].ClassList.Contains("bit-csl-cnt"));

        Assert.AreEqual(2, buttonsRow.Children.Length);
        Assert.IsTrue(buttonsRow.Children[0].ClassList.Contains("bit-csl-pbt"));
        Assert.IsTrue(buttonsRow.Children[1].ClassList.Contains("bit-csl-nbt"));
        Assert.IsTrue(buttonsRow.ClassList.Contains("buttons-class"));
        Assert.Contains("color: red;", buttonsRow.GetAttribute("style") ?? "");

        Assert.AreEqual(0, component.Find(".bit-csl-dcn").QuerySelectorAll(".bit-csl-pbt, .bit-csl-nbt").Length);
        Assert.AreEqual(0, component.Find(".bit-csl-cnt").QuerySelectorAll(".bit-csl-pbt, .bit-csl-nbt").Length);
    }

    [TestMethod]
    public void BitCarouselShouldWriteTheExplicitDirectionOnTheRowOfTheButtons()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
            parameters.Add(p => p.ButtonsPlacement, BitPlacement.Top);
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll(".bit-csl-bcn").Count));

        Assert.Contains("direction:rtl", component.Find(".bit-csl-bcn").GetAttribute("style") ?? "");
    }

    [TestMethod,
        DataRow(BitPlacement.Bottom),
        DataRow(BitPlacement.Top)
    ]
    public void BitCarouselShouldLeaveTheButtonsOutWithHideNextPrev(BitPlacement buttons)
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.HideNextPrev, true);
            parameters.Add(p => p.ButtonsPlacement, buttons);
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.FindAll(".bit-csl-dot").Count));

        Assert.AreEqual(0, component.FindAll(".bit-csl-pbt, .bit-csl-nbt").Count);
        Assert.AreEqual(0, component.FindAll(".bit-csl-bcn").Count);
    }

    // With a single page there is nowhere to go, so the buttons are left out of the rows like the dots are, rather than
    // holding an empty row open.
    [TestMethod,
        DataRow(BitPlacement.Bottom),
        DataRow(BitPlacement.Top)
    ]
    public void BitCarouselShouldLeaveTheButtonsOutOfTheRowsWithASinglePage(BitPlacement buttons)
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.ItemsCount, 3);
            parameters.Add(p => p.VisibleItemsCount, 3);
            parameters.Add(p => p.ButtonsPlacement, buttons);
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.Carousel.PagesCount));

        Assert.AreEqual(0, component.FindAll(".bit-csl-pbt, .bit-csl-nbt").Count);
        Assert.AreEqual(0, component.FindAll(".bit-csl-dcn").Count);
        Assert.AreEqual(0, component.FindAll(".bit-csl-bcn").Count);
    }

    // Over the slides a button at the end it cannot pass is taken out of the layout; in a row it keeps its room, so the
    // dots beside it stay where they are.
    [TestMethod,
        DataRow(BitPlacement.Bottom),
        DataRow(BitPlacement.Top)
    ]
    public async Task BitCarouselShouldKeepTheRoomOfAButtonHiddenInARow(BitPlacement buttons)
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.ButtonsPlacement, buttons);
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.FindAll(".bit-csl-dot").Count));

        var prevStyle = component.Find(".bit-csl-pbt").GetAttribute("style") ?? "";
        Assert.Contains("visibility:hidden", prevStyle);
        Assert.DoesNotContain("display:none", prevStyle);
        Assert.DoesNotContain("visibility:hidden", component.Find(".bit-csl-nbt").GetAttribute("style") ?? "");

        await component.InvokeAsync(() => component.Instance.Carousel.GoTo(3));

        component.WaitForAssertion(() =>
        {
            Assert.Contains("visibility:hidden", component.Find(".bit-csl-nbt").GetAttribute("style") ?? "");
            Assert.DoesNotContain("visibility:hidden", component.Find(".bit-csl-pbt").GetAttribute("style") ?? "");
        });
    }

    [TestMethod]
    public async Task BitCarouselShouldHandTheFocusOverWhenAKeyHidesTheFocusedButtonInARow()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.ButtonsPlacement, BitPlacement.Bottom);
        });

        component.WaitForAssertion(() => Assert.AreEqual(3, component.FindAll(".bit-csl-dot").Count));

        await PressKey(component, "End", "next");

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(2, component.Instance.Carousel.CurrentPage);
            Assert.Contains("visibility:hidden", component.Find(".bit-csl-nbt").GetAttribute("style") ?? "");
        });

        Assert.IsTrue(System.Threading.SpinWait.SpinUntil(() => FocusCalls() == 1, 1000));
        Assert.AreEqual(ButtonReferenceId(component, "_prevButtonRef"), FocusedReferenceId(0));
    }

    [TestMethod]
    public void BitCarouselShouldTakeButtonsPlacementFromBitParams()
    {
        var component = RenderCarouselInBitParams(new BitCarouselParams { ButtonsPlacement = BitPlacement.Top });

        Assert.IsTrue(component.Find(".bit-csl").ClassList.Contains("bit-csl-btop"));
    }

    [TestMethod]
    public async Task BitCarouselShouldTellWhetherItCanGoNextOrPrev()
    {
        var component = RenderComponent<BitCarouselTest>();

        var carousel = component.Instance.Carousel;

        component.WaitForAssertion(() => Assert.AreEqual(3, carousel.PagesCount));

        Assert.IsFalse(carousel.CanGoPrev);
        Assert.IsTrue(carousel.CanGoNext);

        await component.InvokeAsync(carousel.GoNext);

        component.WaitForAssertion(() => Assert.AreEqual(1, carousel.CurrentPage));
        Assert.IsTrue(carousel.CanGoPrev);
        Assert.IsTrue(carousel.CanGoNext);

        await component.InvokeAsync(() => carousel.GoTo(3));

        component.WaitForAssertion(() => Assert.AreEqual(2, carousel.CurrentPage));
        Assert.IsTrue(carousel.CanGoPrev);
        Assert.IsFalse(carousel.CanGoNext);
    }

    // A carousel showing several slides is at its end once its last slide is on screen, wherever its first page started.
    [TestMethod]
    public async Task BitCarouselShouldTellWhetherItCanGoNextWithSeveralVisibleItems()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.ItemsCount, 5);
            parameters.Add(p => p.VisibleItemsCount, 2);
            parameters.Add(p => p.ScrollItemsCount, 2);
        });

        var carousel = component.Instance.Carousel;

        component.WaitForAssertion(() => Assert.AreEqual(3, carousel.PagesCount));

        await component.InvokeAsync(() => carousel.GoTo(3));

        component.WaitForAssertion(() => Assert.AreEqual(2, carousel.CurrentPage));
        Assert.IsTrue(carousel.CanGoPrev);
        Assert.IsFalse(carousel.CanGoNext);
    }

    // With InfiniteScrolling the first page follows the last one, which CurrentPage and PagesCount cannot tell a page.
    [TestMethod]
    public async Task BitCarouselShouldAlwaysGoNextAndPrevWithInfiniteScrolling()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.InfiniteScrolling, true);
        });

        var carousel = component.Instance.Carousel;

        component.WaitForAssertion(() => Assert.AreEqual(3, carousel.PagesCount));

        Assert.IsTrue(carousel.CanGoPrev);
        Assert.IsTrue(carousel.CanGoNext);

        await component.InvokeAsync(() => carousel.GoTo(3));

        component.WaitForAssertion(() => Assert.AreEqual(2, carousel.CurrentPage));
        Assert.IsTrue(carousel.CanGoPrev);
        Assert.IsTrue(carousel.CanGoNext);
    }

    // An infinite carousel has a page per step of its scroll, so it only has a single one when it holds a single step.
    [TestMethod,
        DataRow(false, 3, 3),
        DataRow(false, 1, 1),
        DataRow(true, 1, 1)
    ]
    public void BitCarouselShouldGoNowhereWithASinglePage(bool infinite, int itemsCount, int visibleItemsCount)
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.ItemsCount, itemsCount);
            parameters.Add(p => p.VisibleItemsCount, visibleItemsCount);
            parameters.Add(p => p.InfiniteScrolling, infinite);
        });

        var carousel = component.Instance.Carousel;

        component.WaitForAssertion(() => Assert.AreEqual(1, carousel.PagesCount));

        Assert.IsFalse(carousel.CanGoPrev);
        Assert.IsFalse(carousel.CanGoNext);
    }

    [TestMethod]
    public void BitCarouselShouldGoNowhereWithoutItems()
    {
        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.ItemsCount, 0);
            parameters.Add(p => p.InfiniteScrolling, true);
        });

        Assert.IsFalse(component.Instance.Carousel.CanGoPrev);
        Assert.IsFalse(component.Instance.Carousel.CanGoNext);
    }

    // The buttons of the carousel are shown exactly while it can go their way, so a page's own controls disabled through
    // CanGoNext and CanGoPrev agree with them.
    [TestMethod]
    public async Task BitCarouselShouldShowItsButtonsExactlyWhileItCanGoTheirWay()
    {
        var component = RenderComponent<BitCarouselTest>();

        var carousel = component.Instance.Carousel;

        component.WaitForAssertion(() => Assert.AreEqual(3, carousel.PagesCount));

        for (var page = 1; page <= 3; page++)
        {
            await component.InvokeAsync(() => carousel.GoTo(page));

            component.WaitForAssertion(() =>
            {
                Assert.AreEqual(carousel.CanGoPrev, (component.Find(".bit-csl-pbt").GetAttribute("style") ?? "").Contains("display:none") is false);
                Assert.AreEqual(carousel.CanGoNext, (component.Find(".bit-csl-nbt").GetAttribute("style") ?? "").Contains("display:none") is false);
            });
        }
    }

    // OnChange reports the moves alone, so the state a page reads off the carousel is reported on its own: once the
    // carousel is first laid out, after a move, and after a re-layout that changed it without a move.
    [TestMethod]
    public async Task BitCarouselShouldReportEveryChangeOfItsNavigationState()
    {
        var stateChanges = 0;
        var changes = 0;

        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.OnNavigationStateChange, EventCallback.Factory.Create(this, () => stateChanges++));
            parameters.Add(p => p.OnChange, EventCallback.Factory.Create<int>(this, _ => changes++));
        });

        var carousel = component.Instance.Carousel;

        component.WaitForAssertion(() => Assert.AreEqual(1, stateChanges));
        Assert.AreEqual(0, changes);
        Assert.IsTrue(carousel.CanGoNext);

        await component.InvokeAsync(carousel.GoNext);

        component.WaitForAssertion(() => Assert.AreEqual(2, stateChanges));
        Assert.AreEqual(1, changes);

        // A render that changes nothing of the state reports nothing.
        component.Render();

        Assert.AreEqual(2, stateChanges);

        // Showing every slide at once leaves a single page and nowhere to go, which is no move of the carousel.
        component.Render(parameters => parameters.Add(p => p.VisibleItemsCount, 3));

        component.WaitForAssertion(() => Assert.AreEqual(3, stateChanges));
        Assert.AreEqual(1, changes);
        Assert.AreEqual(1, carousel.PagesCount);
        Assert.IsFalse(carousel.CanGoNext);
        Assert.IsFalse(carousel.CanGoPrev);
    }

    [TestMethod]
    public void BitCarouselShouldNotReportTheNavigationStateOfACarouselWithoutPages()
    {
        var stateChanges = 0;

        var component = RenderComponent<BitCarouselTest>(parameters =>
        {
            parameters.Add(p => p.ItemsCount, 0);
            parameters.Add(p => p.OnNavigationStateChange, EventCallback.Factory.Create(this, () => stateChanges++));
        });

        component.Render();

        Assert.AreEqual(0, stateChanges);
    }
}

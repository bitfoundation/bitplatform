using Bit.Bdnd.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bdnd.Tests.E2E;

/// <summary>
/// The chessboard tutorial: every square is a drop target whose <c>CanDropPredicate</c> allows only a knight's move,
/// so a knight in the air lights up exactly the squares it may go to, and a drop moves it only there. A legal drop
/// re-renders the board and removes the dragged knight from the DOM before the browser ends the drag, which is the
/// case the browser module's end-of-drag handling exists for.
/// </summary>
[TestClass]
public sealed class TutorialTests : BdndTest
{
    private const string Green = "rgb(0, 128, 0)";
    private const string Yellow = "rgb(255, 255, 0)";
    private const string Red = "rgb(255, 0, 0)";

    [TestMethod]
    public async Task Dragging_the_knight_highlights_exactly_the_squares_it_may_move_to()
    {
        var page = await OpenAsync("/tutorial");

        await StartDragAsync(page, page.Locator(".knight"));

        // From column 1, row 7 a knight reaches three squares, each lit green. The pointer is still over the
        // knight's own square, which the knight cannot move to: hovered but refusing, it is red.
        await Expect(page.Locator(".chess-overlay")).ToHaveCountAsync(4);
        CollectionAssert.AreEquivalent(new[] { "0,5", "2,5", "3,6" }, await SquaresLitAsync(page, Green), "the legal squares");
        CollectionAssert.AreEquivalent(new[] { "1,7" }, await SquaresLitAsync(page, Red), "the hovered square");

        // Over a legal square the light turns yellow; the origin square, no longer hovered, goes dark.
        await MoveAsync(page, Square(page, "2,5"));
        await Expect(Overlay(page, "2,5")).ToHaveCSSAsync("background-color", Yellow);
        await Expect(Overlay(page, "1,7")).ToHaveCountAsync(0);

        // Over an illegal square: red, and the legal one it left is green again.
        await DragToAsync(page, (await BoxAsync(Square(page, "4,4"))).Centre, async () =>
        {
            await Expect(Overlay(page, "4,4")).ToHaveCSSAsync("background-color", Red);
            await Expect(Overlay(page, "2,5")).ToHaveCSSAsync("background-color", Green);
        });

        // The drop on the illegal square did not move the knight, and the drag ended: no square is lit any more.
        await Expect(page.Locator(".chess-overlay")).ToHaveCountAsync(0);
        await Expect(Square(page, "1,7").Locator(".knight")).ToBeVisibleAsync();
        await Expect(Status(page)).ToContainTextAsync("Knight at column 1, row 7.");
        Assert.AreEqual(0, (await DropsAsync(page.MainFrame)).Count, "the browser raised a drop on a square the knight cannot go to");
    }

    [TestMethod]
    public async Task Dropping_the_knight_on_a_legal_square_moves_it_and_ends_the_drag_of_the_removed_source()
    {
        var page = await OpenAsync("/tutorial");

        await DragAsync(page, page.Locator(".knight"), Square(page, "2,5"),
            () => Expect(Overlay(page, "2,5")).ToHaveCSSAsync("background-color", Yellow));

        await Expect(Square(page, "2,5").Locator(".knight")).ToBeVisibleAsync();
        await Expect(page.Locator(".knight")).ToHaveCountAsync(1);
        await Expect(Status(page)).ToContainTextAsync("Knight at column 2, row 5.");

        // The drop moved the knight while the drag was still running, so the element the drag started from was gone
        // from the DOM when the browser ended the drag. The drag still ended: no square stays lit.
        await Expect(page.Locator(".chess-overlay")).ToHaveCountAsync(0);

        // That is the case under test: a dragend raised on an element still in the document bubbles up to the
        // window, and none did. The browser raised it on the detached element, where only the module's own
        // element-level listener could hear it.
        Assert.AreEqual(0, await page.EvaluateAsync<int>("() => window.__bdndE2E.dragends"),
            "the knight's element was still in the document when the drag ended");

        // The knight on its new square is a new draggable, and the next move starts from there.
        await StartDragAsync(page, page.Locator(".knight"));
        await Expect(page.Locator(".chess-overlay")).ToHaveCountAsync(9);
        CollectionAssert.AreEquivalent(new[] { "0,4", "0,6", "1,3", "1,7", "3,3", "3,7", "4,4", "4,6" }, await SquaresLitAsync(page, Green),
            "the legal squares from column 2, row 5");
        CollectionAssert.AreEquivalent(new[] { "2,5" }, await SquaresLitAsync(page, Red), "the hovered square");

        await DragToAsync(page, (await BoxAsync(Square(page, "4,4"))).Centre,
            () => Expect(Overlay(page, "4,4")).ToHaveCSSAsync("background-color", Yellow));

        await Expect(Square(page, "4,4").Locator(".knight")).ToBeVisibleAsync();
        await Expect(Status(page)).ToContainTextAsync("Knight at column 4, row 4.");
        await Expect(page.Locator(".chess-overlay")).ToHaveCountAsync(0);
    }

    private static ILocator Square(IPage page, string square) => page.Locator($".chess-square[data-square=\"{square}\"]");

    private static ILocator Overlay(IPage page, string square) => Square(page, square).Locator(".chess-overlay");

    private static ILocator Status(IPage page) => page.GetByText("Knight at column");

    /// <summary>The squares whose overlay the browser paints in <paramref name="colour"/>.</summary>
    private static Task<string[]> SquaresLitAsync(IPage page, string colour) => page.EvaluateAsync<string[]>(
        """
        (colour) => [...document.querySelectorAll('.chess-square')]
          .filter(s => { const o = s.querySelector('.chess-overlay'); return o && getComputedStyle(o).backgroundColor === colour; })
          .map(s => s.dataset.square)
        """, colour);
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.Sortable;

/// <summary>
/// Lists inside the items of another list. When a drag starts, the lists answer which of them take it, and the
/// browser layer routes the pointer to the innermost of those under it: a nested list that refuses the drag
/// (another group, another item type, <c>Put="false"</c>) leaves it to the list around it, as SortableJS does.
/// </summary>
[TestClass]
public class BitSortableNestedListTests : BitSortableTestContext
{
    /// <summary>
    /// A board: the columns sort in one list, and each column holds a list of cards. With <paramref name="cardGroup"/>
    /// the same as <paramref name="boardGroup"/> a card can also land among the columns, as in a tree.
    /// </summary>
    private IRenderedComponent<BitSortable<string>> RenderBoard(
        List<string> columns, Dictionary<string, List<string>> cards, string? boardGroup, string cardGroup)
        => Render<BitSortable<string>>(p => p
            .Add(c => c.Id, "board")
            .Add(c => c.Group, boardGroup)
            .Add(c => c.Items, columns)
            .Add(c => c.ItemTemplate, column => builder =>
            {
                builder.OpenComponent<BitSortable<string>>(0);
                builder.AddComponentParameter(1, nameof(BitSortable<string>.Id), $"cards-{column}");
                builder.AddComponentParameter(2, nameof(BitSortable<string>.Group), cardGroup);
                builder.AddComponentParameter(3, nameof(BitSortable<string>.Items), cards[column]);
                builder.AddComponentParameter(4, nameof(BitSortable<string>.ItemTemplate), (RenderFragment<string>)(card => b => b.AddContent(0, card)));
                builder.CloseComponent();
            }));

    [TestMethod]
    public async Task ANestedListThatRefusesTheItemIsNotRoutedTo()
    {
        List<string> columns = ["A", "B", "C"];
        var board = RenderBoard(columns, columns.ToDictionary(c => c, c => new List<string> { $"{c}1" }),
            boardGroup: null, cardGroup: "cards");

        // Column A: the card lists take cards only, so the pointer over them belongs to the board around them.
        var started = await StartByKeyAsync(board, board.Find("#board > [data-bit-srt-item]").GetAttribute("data-bit-srt-key")!);
        Assert.AreSequenceEqual(["board"], started!.Lists.ToList());

        await DragOverAsync(board, 3);
        await DropAsync(board);

        Assert.AreSequenceEqual(["B", "C", "A"], columns);
        Assert.IsEmpty(board.FindAll("[data-bit-srt-placeholder]"));
        Assert.IsNull(Coordinator.Context);
    }

    [TestMethod]
    public async Task ANestedListThatRefusesTheItemCannotBeMadeToTakeIt()
    {
        List<string> columns = ["A", "B"];
        var board = RenderBoard(columns, columns.ToDictionary(c => c, c => new List<string> { $"{c}1" }),
            boardGroup: null, cardGroup: "cards");
        var cardsB = ListById<string>(board, "cards-B");

        await StartByKeyAsync(board, board.Find("#board > [data-bit-srt-item]").GetAttribute("data-bit-srt-key")!);
        // A drop position for a list that does not take the drag is ignored, whoever sends it.
        Assert.IsFalse(await DragOverAsync(cardsB, 0));

        Assert.IsEmpty(board.FindAll("[data-bit-srt-placeholder]"));
    }

    [TestMethod]
    public async Task EveryListThatCanTakeTheItemIsNamedAtTheStart()
    {
        List<string> columns = ["A", "B"];
        var cards = columns.ToDictionary(c => c, c => new List<string> { $"{c}1" });
        var board = RenderBoard(columns, cards, boardGroup: "g", cardGroup: "g");
        var cardsA = ListById<string>(board, "cards-A");
        var cardsB = ListById<string>(board, "cards-B");

        // Card A1: its own list, the other card list and the board (the same group) all take it.
        var started = await DragStartAsync(cardsA, "A1");
        CollectionAssert.AreEquivalent(new[] { "board", "cards-A", "cards-B" }, started!.Lists);

        // The browser layer routes the pointer over B's card list to that list, the innermost one under it.
        await DragOverAsync(cardsB, 1);
        Assert.ContainsSingle(board.FindAll("#cards-B > [data-bit-srt-placeholder]"));
        Assert.ContainsSingle(board.FindAll("[data-bit-srt-placeholder]"));

        await DropAsync(cardsB);
        Assert.AreSequenceEqual(["A", "B"], columns);
        Assert.IsEmpty(cards["A"]);
        Assert.AreSequenceEqual(["B1", "A1"], cards["B"]);
    }
}

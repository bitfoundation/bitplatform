using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bortable.Tests;

/// <summary>
/// Lists inside the items of another list, and the app around a list. A <c>dragover</c> or <c>drop</c> stops at
/// the innermost list only when that list takes the drag in progress: a nested list that refuses it (another
/// group, another item type, <c>Put="false"</c>) leaves it to the list around it, as SortableJS does, and a drag
/// that is not a list's at all (a file, selected text) reaches the app's own handlers.
/// </summary>
/// <remarks>
/// bUnit bubbles an event only through the markup of the component the element was found in, so every element
/// here is found through the outermost component.
/// </remarks>
[TestClass]
public class NestedListTests : BortableTestContext
{
    /// <summary>
    /// A board: the columns sort in one list, and each column holds a list of cards. With <paramref name="cardGroup"/>
    /// the same as <paramref name="boardGroup"/> a card can also land among the columns, as in a tree.
    /// </summary>
    private IRenderedComponent<Bortable<string>> RenderBoard(
        List<string> columns, Dictionary<string, List<string>> cards, string? boardGroup, string cardGroup)
        => Render<Bortable<string>>(p => p
            .Add(c => c.Id, "board")
            .Add(c => c.Group, boardGroup)
            .Add(c => c.Items, columns)
            .Add(c => c.ItemTemplate, column => builder =>
            {
                builder.OpenComponent<Bortable<string>>(0);
                builder.AddComponentParameter(1, nameof(Bortable<string>.Id), $"cards-{column}");
                builder.AddComponentParameter(2, nameof(Bortable<string>.Group), cardGroup);
                builder.AddComponentParameter(3, nameof(Bortable<string>.Items), cards[column]);
                builder.AddComponentParameter(4, nameof(Bortable<string>.ItemTemplate), (RenderFragment<string>)(card => b => b.AddContent(0, card)));
                builder.CloseComponent();
            }));

    [TestMethod]
    public async Task ANestedListThatRefusesTheItemLeavesTheDragoverToTheListAroundIt()
    {
        List<string> columns = ["A", "B", "C"];
        var board = RenderBoard(columns, columns.ToDictionary(c => c, c => new List<string> { $"{c}1" }),
            boardGroup: null, cardGroup: "cards");

        // Column A over the card list of column C, which does not take columns: the board sorts the columns.
        await board.FindAll("#board > [data-bortable-item]")[0].DragStartAsync(ListDragStart());
        await DragOverAsync(board.Find("#cards-C"), 3);

        Assert.AreSequenceEqual(["B", "C", "A"], columns);
        Assert.IsEmpty(board.FindAll("[data-bortable-placeholder]"));

        await board.Find("#cards-C").DropAsync(new DragEventArgs());
        Assert.AreSequenceEqual(["B", "C", "A"], columns);
        Assert.IsNull(Coordinator.Context);
    }

    [TestMethod]
    public async Task ANestedListThatTakesTheItemKeepsTheDragoverToItself()
    {
        List<string> columns = ["A", "B"];
        var cards = columns.ToDictionary(c => c, c => new List<string> { $"{c}1" });
        var board = RenderBoard(columns, cards, boardGroup: "g", cardGroup: "g");

        // Card A1 over the card list of column B: that list previews it, and the board around it, which could
        // take the card as well, is not asked.
        await board.Find("#cards-A > [data-bortable-item]").DragStartAsync(ListDragStart());
        await DragOverAsync(board.Find("#cards-B"), 1);

        Assert.ContainsSingle(board.FindAll("#cards-B > [data-bortable-placeholder]"));
        Assert.ContainsSingle(board.FindAll("[data-bortable-placeholder]"));

        await board.Find("#cards-B").DropAsync(new DragEventArgs());
        Assert.AreSequenceEqual(["A", "B"], columns);
        Assert.IsEmpty(cards["A"]);
        Assert.AreSequenceEqual(["B1", "A1"], cards["B"]);
    }

    [TestMethod]
    public async Task ADropThatIsNotAListsReachesTheAppAroundTheList()
    {
        int dropped = 0;
        var page = Render(builder =>
        {
            // The app's own drop zone (for files, say), with a list inside it.
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "ondrop", EventCallback.Factory.Create<DragEventArgs>(this, () => dropped++));
            builder.OpenComponent<Bortable<string>>(2);
            builder.AddComponentParameter(3, nameof(Bortable<string>.Items), new List<string> { "A", "B" });
            builder.AddComponentParameter(4, nameof(Bortable<string>.ItemTemplate), (RenderFragment<string>)(item => b => b.AddContent(0, item)));
            builder.CloseComponent();
            builder.CloseElement();
        });

        await page.Find("[data-bortable-list]").DropAsync(new DragEventArgs());
        Assert.AreEqual(1, dropped);

        // A drag of the list's own ends in the list.
        await page.Find("[data-bortable-item]").DragStartAsync(ListDragStart());
        await page.Find("[data-bortable-list]").DropAsync(new DragEventArgs());
        Assert.AreEqual(1, dropped);
        Assert.IsNull(Coordinator.Context);
    }
}

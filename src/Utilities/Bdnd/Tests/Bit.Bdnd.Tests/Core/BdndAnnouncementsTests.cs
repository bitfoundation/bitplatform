namespace Bit.Bdnd.Tests.Core;

/// <summary>The English texts, for one item and for a drag that carries several.</summary>
[TestClass]
public class BdndAnnouncementsTests
{
    private static BdndAnnouncement Of(int count, string? target = "Bin", bool? before = null, int targets = 2) => new()
    {
        SourceLabel = "Crate",
        ItemType = "box",
        ItemCount = count,
        TargetLabel = target,
        Before = before,
        TargetCount = targets,
    };

    private static readonly BdndAnnouncements Texts = BdndAnnouncements.Default;

    [TestMethod]
    [DataRow(1, "Picked up Crate. 2 drop targets are available.")]
    [DataRow(2, "Picked up Crate and 1 other item. 2 drop targets are available.")]
    [DataRow(4, "Picked up Crate and 3 other items. 2 drop targets are available.")]
    public void ThePickUpNamesTheGrabbedSourceAndCountsTheRest(int count, string expected)
        => Assert.AreEqual(expected, Texts.DragStarted(Of(count)));

    [TestMethod]
    public void NoTargetForSeveralItemsSaysThem()
        => Assert.AreEqual("Picked up Crate and 1 other item. There is no drop target for them; press Escape to cancel.",
            Texts.DragStarted(Of(2, targets: 0)));

    [TestMethod]
    [DataRow(1, null, "Crate is over Bin.")]
    [DataRow(3, null, "3 items are over Bin.")]
    [DataRow(3, true, "3 items are before Bin.")]
    [DataRow(3, false, "3 items are after Bin.")]
    public void AMoveSaysWhereTheItemsAre(int count, bool? before, string expected)
        => Assert.AreEqual(expected, Texts.DraggedOver(Of(count, before: before)));

    [TestMethod]
    public void TheEndOfTheWayAndTheDropAgreeInNumber()
    {
        Assert.AreEqual("3 items are not over a drop target.", Texts.DraggedOver(Of(3, target: null)));
        Assert.AreEqual("There is nothing further in that direction: 3 items stay after Bin.", Texts.NoFurtherTarget(Of(3, before: false)));
        Assert.AreEqual("There is nothing further in that direction: Crate stays after Bin.", Texts.NoFurtherTarget(Of(1, before: false)));
        Assert.AreEqual("3 items were dropped before Bin.", Texts.Dropped(Of(3, before: true)));
        Assert.AreEqual("Crate was dropped on Bin.", Texts.Dropped(Of(1)));
        Assert.AreEqual("Dragging 3 items was cancelled.", Texts.Cancelled(Of(3)));
    }

    [TestMethod]
    public void AnItemCountsOneByDefaultEvenACollection()
    {
        Assert.AreEqual(1, Texts.CountItems(new[] { "a", "b", "c" }));
        Assert.AreEqual(1, Texts.CountItems(new List<string> { "a", "b" }));
        Assert.AreEqual(1, Texts.CountItems(null));
    }

    [TestMethod]
    public void CountElementsCountsTheElementsOfACollectionAndAnythingElseAsOne()
    {
        Assert.AreEqual(3, BdndAnnouncements.CountElements(new[] { "a", "b", "c" }));
        Assert.AreEqual(2, BdndAnnouncements.CountElements(new List<int> { 1, 2 }));
        Assert.AreEqual(3, BdndAnnouncements.CountElements(new HashSet<string> { "a", "b", "c" }));
        Assert.AreEqual(2, BdndAnnouncements.CountElements((IReadOnlySet<string>)new SortedSet<string> { "a", "b" }));
        Assert.AreEqual(1, BdndAnnouncements.CountElements("a string is one item"));
        Assert.AreEqual(1, BdndAnnouncements.CountElements(null));
        Assert.AreEqual(1, BdndAnnouncements.CountElements(new object()));
    }

    [TestMethod]
    public void TheDefaultInstructionsNameTheDefaultKeys()
        => Assert.AreEqual(
            "To pick up, press Space or Enter. While dragging, use the arrow keys to move between drop targets, " +
            "Home and End to go to the first or last one, Space or Enter to drop, and Escape to cancel.",
            Texts.InstructionsFor(BdndKeyBindings.Default));

    [TestMethod]
    public void TheInstructionsNameTheKeysBound()
    {
        var keys = new BdndKeyBindings
        {
            PickUp = ["p"],
            Drop = ["d", "Enter"],
            Cancel = ["Esc", "Escape", "q"],
            Up = ["ArrowUp", "k"],
            Down = ["ArrowDown", "j"],
            Left = [],
            Right = ["l"],
            First = ["PageUp"],
        };

        Assert.AreEqual(
            "To pick up, press P. While dragging, use Up Arrow or K for up, Down Arrow or J for down and L for right to move " +
            "between drop targets, Page Up and End to go to the first or last one, D or Enter to drop, and Escape or Q to cancel.",
            Texts.InstructionsFor(keys));
    }

    [TestMethod]
    public void TranslatedInstructionsNameTheKeysTheirOwnWay()
    {
        var texts = new BdndAnnouncements
        {
            Instructions = "{PickUp} zum Aufnehmen, {Cancel} zum Abbrechen. {Unknown} bleibt.",
            NameKeys = (keys, placeholder) => placeholder == "PickUp" ? string.Join(" oder ", keys.PickUp) : "Escape",
        };

        Assert.AreEqual("Enter oder x zum Aufnehmen, Escape zum Abbrechen. {Unknown} bleibt.",
            texts.InstructionsFor(new BdndKeyBindings { PickUp = ["Enter", "x"] }));
        Assert.AreEqual("Ohne Platzhalter.", new BdndAnnouncements { Instructions = "Ohne Platzhalter." }.InstructionsFor(BdndKeyBindings.Default));
    }
}

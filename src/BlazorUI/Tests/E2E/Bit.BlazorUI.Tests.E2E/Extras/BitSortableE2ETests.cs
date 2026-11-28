namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// BitSortable's basics on /e2e/sortable: the markup a list renders for the script and for assistive technology,
/// sorting within one list with real drags (the live reorder while the pointer moves, the order the bound list ends
/// with, the slides), rows keyed by their id (ItemKey), what an empty list shows, a Disabled list, and the axe scan.
/// </summary>
[TestClass]
[E2ERetry]
[DoNotParallelize]
public class BitSortableE2ETests : BitSortableE2ETestBase
{
    private const string Url = "/e2e/sortable";
    private static readonly string[] Simple = ["Item 1", "Item 2", "Item 3", "Item 4", "Item 5", "Item 6"];

    [TestMethod]
    public async Task Markup_TheListAndItsItems_NameThemselvesToTheScriptAndToAssistiveTechnology()
    {
        await OpenAsync(Url);

        var list = Page.Locator("#simple-list");
        await Expect(list).ToHaveClassAsync(HasClass("bit-srt"));
        await Expect(list).ToHaveClassAsync(HasClass("demo-list"));
        await Expect(list).ToHaveAttributeAsync("role", "list");
        await Expect(list).ToHaveAttributeAsync("data-bit-srt-list", "simple-list");
        await Expect(list).ToHaveCSSAsync("position", "relative");

        var items = Page.Locator("#simple-list > [data-bit-srt-item]");
        await Expect(items).ToHaveCountAsync(6);
        var first = items.First;
        await Expect(first).ToHaveAttributeAsync("role", "listitem");
        await Expect(first).ToHaveClassAsync(HasClass("bit-srt-itm"));
        await Expect(first).ToHaveClassAsync(HasClass("demo-item"));
        await Expect(first).ToHaveAttributeAsync("draggable", "true");
        await Expect(first).ToHaveAttributeAsync("data-bit-srt-key", new System.Text.RegularExpressions.Regex(".+"));
        await Expect(first).ToHaveAttributeAsync("aria-roledescription", "sortable item");
        await Expect(first).ToHaveAttributeAsync("aria-describedby", "simple-list-ins");

        // One item is the list's tab stop; the instructions every item refers to are read, never shown.
        await Expect(Page.Locator("#simple-list > [tabindex='0']")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#simple-list > [tabindex='-1']")).ToHaveCountAsync(5);
        var instructions = Page.Locator("#simple-list-ins");
        await Expect(instructions).ToHaveClassAsync(HasClass("bit-srt-ins"));
        await Expect(instructions).ToBeHiddenAsync();
        await Expect(instructions).ToContainTextAsync("To pick up the item, press Space or Enter.");

        // A MultiDrag list's instructions say how to select, too.
        var keysItem = Page.Locator("#keys-list > [data-bit-srt-item]").First;
        await Expect(keysItem).ToHaveAttributeAsync("aria-describedby", "keys-list-ins");
        await Expect(Page.Locator("#keys-list-ins")).ToContainTextAsync("Shift and Space select or deselect the item");
    }

    [TestMethod]
    public async Task Mouse_DraggingAnItemDown_ReordersTheListLiveAndBindsTheOrderOnDrop()
    {
        await OpenAsync(Url);
        await AssertOrderAsync("simple-list", "simple-order", Simple);

        await PressAsync(await CentreOfAsync("simple-list", "Item 1"));
        await MoveAsync(await CentreOfAsync("simple-list", "Item 4"));

        // Mid-drag: the list has been reordered under the pointer, and the dragged item wears the ghost and chosen
        // classes - the built-in ones and the page's own Classes.Ghost. The readout waits for the drop.
        await AssertLabelsAsync("simple-list", ["Item 2", "Item 3", "Item 4", "Item 1", "Item 5", "Item 6"]);
        var dragged = Item("simple-list", "Item 1");
        await Expect(dragged).ToHaveClassAsync(HasClass("bit-srt-gst"));
        await Expect(dragged).ToHaveClassAsync(HasClass("demo-ghost"));
        await Expect(dragged).ToHaveClassAsync(HasClass("bit-srt-chs"));
        await Expect(Page.Locator("#simple-order")).ToHaveTextAsync(string.Join(", ", Simple));

        await ReleaseAsync();

        await AssertOrderAsync("simple-list", "simple-order", "Item 2", "Item 3", "Item 4", "Item 1", "Item 5", "Item 6");
        await Expect(Page.Locator("#simple-list .bit-srt-gst, #simple-list .demo-ghost, #simple-list .bit-srt-chs")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Mouse_DraggingAnItemUp_MovesItAboveTheItemItIsDroppedOn()
    {
        await OpenAsync(Url);

        await DragAsync(await CentreOfAsync("simple-list", "Item 5"), await CentreOfAsync("simple-list", "Item 2"));

        await AssertOrderAsync("simple-list", "simple-order", "Item 1", "Item 5", "Item 2", "Item 3", "Item 4", "Item 6");
    }

    [TestMethod]
    public async Task Mouse_SuccessiveDrags_KeepSortingTheAlreadySortedList()
    {
        await OpenAsync(Url);

        await DragAsync(await CentreOfAsync("simple-list", "Item 6"), await CentreOfAsync("simple-list", "Item 1"));
        await AssertOrderAsync("simple-list", "simple-order", "Item 6", "Item 1", "Item 2", "Item 3", "Item 4", "Item 5");

        await DragAsync(await CentreOfAsync("simple-list", "Item 1"), await CentreOfAsync("simple-list", "Item 5"));
        await AssertOrderAsync("simple-list", "simple-order", "Item 6", "Item 2", "Item 3", "Item 4", "Item 5", "Item 1");
    }

    [TestMethod]
    public async Task Animation_ItemsSlideIntoPlace_WithoutDisturbingTheFinalOrder()
    {
        await OpenAsync(Url);

        // Counts the slides the browser runs: a slide inverts a moved item with a transform and transitions it back to
        // none, so the transition itself is the trace it leaves.
        await Page.EvaluateAsync(
            """
            () => {
              window.__flips = 0;
              document.getElementById('simple-list').addEventListener('transitionrun', e => {
                if (e.propertyName === 'transform') window.__flips++;
              });
            }
            """);

        // Down past every item and straight back up to the third slot, without waiting in between: every reorder starts
        // while the slides of the previous ones are still running, and has to take over from them.
        var slots = new List<Box>();
        foreach (var label in Simple) slots.Add(await BoxAsync(Item("simple-list", label)));
        await PressAsync(slots[0].Centre);
        await Page.Mouse.MoveAsync((float)slots[5].Centre.X, (float)slots[5].Centre.Y, new() { Steps = 15 });
        await MoveAsync(slots[2].Centre, steps: 10);
        await ReleaseAsync();

        await AssertOrderAsync("simple-list", "simple-order", "Item 2", "Item 3", "Item 1", "Item 4", "Item 5", "Item 6");
        Assert.IsGreaterThan(0, await Page.EvaluateAsync<int>("() => window.__flips"), "no item was animated into its new place");

        // Once the slides are over, no item is left offset or mid-transition, and each is painted in its slot.
        var leftovers = await Page.EvaluateAsync<string[]>(
            "() => [...document.querySelectorAll('#simple-list > [data-bit-srt-item]')].filter(i => i.style.transform || i.style.transition).map(i => i.textContent.trim())");
        Assert.IsEmpty(leftovers, $"items still carry an animation style: {string.Join(", ", leftovers)}");

        string[] expected = ["Item 2", "Item 3", "Item 1", "Item 4", "Item 5", "Item 6"];
        for (int i = 0; i < expected.Length; i++)
        {
            var box = await BoxAsync(Item("simple-list", expected[i]));
            Assert.AreEqual(slots[i].Y, box.Y, 0.5, $"{expected[i]} is not painted in slot {i}");
        }
    }

    [TestMethod]
    public async Task ItemKey_RowsLoadedAgain_KeepTheirSelectionWhenKeyedById()
    {
        await OpenAsync(Url);

        await Item("keys-list", "Write the spec").ClickAsync();
        await Item("keys-list", "Build the API").ClickAsync();
        await Expect(Page.Locator("#keys-list > .bit-srt-sel")).ToHaveTextAsync(["Write the spec", "Build the API"]);

        await Page.Locator("#keys-reload").ClickAsync();

        await Expect(Page.Locator("#keys-loads")).ToHaveTextAsync("2 times");
        await Expect(Page.Locator("#keys-list > .bit-srt-sel")).ToHaveTextAsync(["Write the spec", "Build the API"]);
    }

    [TestMethod]
    public async Task ItemKey_WithoutIt_RowsLoadedAgainAreNewItems()
    {
        await OpenAsync(Url);

        await Page.Locator("#keys-by-id").UncheckAsync();
        // Turning the key off renders every row anew (its element is keyed by the row now): let that land first.
        await WaitForQuietAsync();
        await Item("keys-list", "Review the design").ClickAsync();
        await Expect(Page.Locator("#keys-list > .bit-srt-sel")).ToHaveCountAsync(1);

        await Page.Locator("#keys-reload").ClickAsync();

        await Expect(Page.Locator("#keys-loads")).ToHaveTextAsync("2 times");
        await Expect(Page.Locator("#keys-list > .bit-srt-sel")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task ItemKey_ARowLoadedAgain_IsDraggedAsTheRowTheListHoldsNow()
    {
        await OpenAsync(Url);

        await Page.Locator("#keys-reload").ClickAsync();
        await Expect(Page.Locator("#keys-loads")).ToHaveTextAsync("2 times");

        await DragAsync(await CentreOfAsync("keys-list", "Write the spec"), (await BoxAsync(Item("keys-list", "Build the API"))).At(0.5, 0.8));

        await AssertOrderAsync("keys-list", "keys-order", "Review the design", "Build the API", "Write the spec", "Ship it");
    }

    [TestMethod]
    public async Task Empty_AListWithNoItems_ShowsItsTemplateItsTextOrTheDefault()
    {
        await OpenAsync(Url);

        var text = Page.Locator("#empty-text > .bit-srt-emp");
        await Expect(text).ToHaveTextAsync("Drop something here");
        await Expect(text).ToHaveClassAsync(HasClass("custom-empty"));
        await Expect(Page.Locator("#empty-template > .bit-srt-emp > b.custom-empty-template")).ToHaveTextAsync("Nothing to sort");
        await Expect(Page.Locator("#empty-default > .bit-srt-emp")).ToHaveTextAsync("No items found");
        await Expect(Page.Locator("#empty-params > .bit-srt-emp")).ToHaveTextAsync("Empty, from BitParams");

        // A list that has items shows none of it.
        await Expect(Page.Locator("#simple-list > .bit-srt-emp")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Disabled_AList_TakesPartInNoDrag()
    {
        await OpenAsync(Url);

        var archive = Page.Locator("#disabled-archive");
        await Expect(archive).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(archive.Locator("> [data-bit-srt-item]").First).ToHaveAttributeAsync("draggable", "false");
        await Expect(archive.Locator("> [data-bit-srt-item]").First).ToHaveAttributeAsync("data-bit-srt-disabled", "true");
        await Expect(archive.Locator("> [tabindex]")).ToHaveCountAsync(0);

        // Out of it: nothing is picked up.
        await DragAsync(await CentreOfAsync("disabled-archive", "2023 report"), await CentreOfAsync("disabled-desk", "Notes"));
        await AssertOrderAsync("disabled-archive", "disabled-archive-order", "2023 report", "2024 report");
        await AssertOrderAsync("disabled-desk", "disabled-desk-order", "Draft", "Notes");

        // Into it: the disabled list previews nothing and takes nothing.
        await PressAsync(await CentreOfAsync("disabled-desk", "Draft"));
        await MoveAsync(await CentreOfAsync("disabled-archive", "2024 report"));
        await Expect(Placeholder("disabled-archive")).ToHaveCountAsync(0);
        await ReleaseAsync();
        await AssertOrderAsync("disabled-archive", "disabled-archive-order", "2023 report", "2024 report");
        await AssertOrderAsync("disabled-desk", "disabled-desk-order", "Draft", "Notes");
    }

    [TestMethod]
    public async Task Axe_EachSection_HasNoWcagAOrAAViolation()
    {
        await OpenAsync(Url);

        await AssertNoAxeViolationsAsync("#simple");
        await AssertNoAxeViolationsAsync("#keys");
        await AssertNoAxeViolationsAsync("#disabled");
    }

    [TestMethod]
    public async Task Axe_EmptyLists_HaveNoWcagAOrAAViolation()
    {
        await OpenAsync(Url);

        await AssertNoAxeViolationsAsync("#empty");
    }
}

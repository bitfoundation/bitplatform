using System.Text.RegularExpressions;
using Bit.Bcrdt.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Bcrdt.Tests.E2E;

/// <summary>
/// The shared to-do list, a sequence of collaborative maps: items added, ticked, renamed, reordered and deleted on
/// one replica reach the other, and a rename and a tick of the same item made while the replicas cannot see each
/// other both survive, because they touch different keys of a real map.
/// </summary>
[TestClass]
public sealed class CollaborativeListTests : BcrdtTest
{
    [TestMethod]
    public async Task Items_added_ticked_reordered_and_deleted_on_one_replica_reach_the_other()
    {
        var page = await OpenReplicasAsync("/list", 2);
        ILocator a = Replica(page, "Replica A"), b = Replica(page, "Replica B");

        await AddAsync(a, "Buy milk");
        await AddAsync(b, "Walk the dog");

        await ExpectTitlesAsync(a, "Buy milk", "Walk the dog");
        await ExpectTitlesAsync(b, "Buy milk", "Walk the dog");

        // A tick on B is a write of the item's done key, and A strikes the item through.
        await Items(b).Nth(0).Locator("input[type=checkbox]").CheckAsync();
        await Assertions.Expect(Items(a).Nth(0).Locator("input[type=checkbox]")).ToBeCheckedAsync();
        await Assertions.Expect(Items(a).Nth(0)).ToHaveClassAsync(new Regex("\\btodo-done\\b"));
        await Assertions.Expect(a.Locator(".panel-foot")).ToContainTextAsync("1 done");

        // A move on A reorders B's list too, and the tick travels with its item.
        await Items(a).Nth(0).GetByTitle("Move down").ClickAsync();
        await ExpectTitlesAsync(b, "Walk the dog", "Buy milk");
        await Assertions.Expect(Items(b).Nth(1).Locator("input[type=checkbox]")).ToBeCheckedAsync();

        await Items(b).Nth(0).GetByTitle("Delete").ClickAsync();
        await ExpectTitlesAsync(a, "Buy milk");
        await Assertions.Expect(a.Locator(".panel-foot")).ToContainTextAsync("1 items");

        await ExpectSameProjectionsAsync(page);
    }

    [TestMethod]
    public async Task A_rename_and_a_tick_made_offline_on_the_same_item_both_survive_the_merge()
    {
        var page = await OpenReplicasAsync("/list", 2);
        ILocator a = Replica(page, "Replica A"), b = Replica(page, "Replica B");

        await AddAsync(a, "Draft the report");
        await ExpectTitlesAsync(b, "Draft the report");

        await GoOfflineAsync(b);

        // A renames the item; B, unaware, ticks the same item and adds one of its own.
        var title = Items(a).Nth(0).Locator(".todo-title");
        await title.FillAsync("Finish the report");
        await title.PressAsync("Tab");
        await Items(b).Nth(0).Locator("input[type=checkbox]").CheckAsync();
        await AddAsync(b, "Send it to Ada");

        await ExpectTitlesAsync(a, "Finish the report");
        await ExpectTitlesAsync(b, "Draft the report", "Send it to Ada");

        await GoOnlineAsync(b);

        // The rename touched the title key and the tick the done key, so neither overwrote the other, and B's new
        // item is now in A as well.
        foreach (var panel in new[] { a, b })
        {
            await ExpectTitlesAsync(panel, "Finish the report", "Send it to Ada");
            await Assertions.Expect(Items(panel).Nth(0).Locator("input[type=checkbox]")).ToBeCheckedAsync();
            await Assertions.Expect(Items(panel).Nth(1).Locator("input[type=checkbox]")).Not.ToBeCheckedAsync();
        }

        await ExpectSameProjectionsAsync(page);
    }

    private static ILocator Items(ILocator panel) => panel.Locator("li.todo-item");

    private static async Task AddAsync(ILocator panel, string title)
    {
        var input = panel.Locator(".list-add input");
        await input.FillAsync(title);
        await panel.Locator(".list-add").GetByRole(AriaRole.Button, new() { Name = "Add" }).ClickAsync();
        await Assertions.Expect(input).ToHaveValueAsync("");
    }

    /// <summary>The titles of the panel's items, in order. A title is an input, so it is compared by value.</summary>
    private static async Task ExpectTitlesAsync(ILocator panel, params string[] titles)
    {
        await Assertions.Expect(Items(panel)).ToHaveCountAsync(titles.Length);
        for (var i = 0; i < titles.Length; i++)
            await Assertions.Expect(Items(panel).Nth(i).Locator(".todo-title")).ToHaveValueAsync(titles[i]);
    }
}

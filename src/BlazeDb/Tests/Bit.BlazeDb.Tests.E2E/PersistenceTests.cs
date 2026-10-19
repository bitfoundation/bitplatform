using Bit.BlazeDb.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.BlazeDb.Tests.E2E;

/// <summary>
/// The demo database the way a visitor uses it: on OPFS, behind the tab's Web Lock, surviving a reload -
/// and the second tab, which the lock keeps from writing the same files and the demo moves to memory.
/// </summary>
[TestClass]
public class PersistenceTests : BlazeDbTest
{
    [TestMethod]
    public async Task A_todo_added_on_opfs_survives_a_reload()
    {
        var page = await OpenAsync("/todos");

        Assert.AreEqual("Opfs", await WaitForDatabaseAsync(page), "A fresh context should win the Web Lock and open the database on OPFS.");

        var title = $"e2e {Guid.NewGuid():N}";

        await page.Locator("#new-title").FillAsync(title);
        await page.Locator("button.btn-primary").Filter(new() { HasText = "Insert" }).ClickAsync();
        await Assertions.Expect(page.Locator(".row-title", new() { HasText = title })).ToBeVisibleAsync();

        // The demo flushes every 100 ms; the write-ahead log has reached OPFS well within this.
        await page.WaitForTimeoutAsync(1_500);

        // A reload releases this document's lock and takes it again in the next one; the row comes back
        // out of OPFS, replayed from the log, rather than out of anything the old page kept.
        await page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 90_000 });

        Assert.AreEqual("Opfs", await WaitForDatabaseAsync(page));

        await page.Locator("input[placeholder='Filter titles…']").FillAsync(title);
        await Assertions.Expect(page.Locator(".row-title", new() { HasText = title })).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task A_second_tab_falls_back_to_memory_while_the_first_holds_the_writer_lock()
    {
        var context = await NewContextAsync();

        var first = await context.NewPageAsync();
        await GotoAsync(first, "/");
        Assert.AreEqual("Opfs", await WaitForDatabaseAsync(first));

        var second = await context.NewPageAsync();
        await GotoAsync(second, "/");

        Assert.AreEqual("LockedFallback", await WaitForDatabaseAsync(second),
            "Two tabs of one origin must not both write the same OPFS files: the second one has to lose the Web Lock election.");
        await Assertions.Expect(second.Locator(".storage-pill")).ToContainTextAsync("another tab owns OPFS");
    }

    [TestMethod]
    public async Task The_writer_lock_is_released_when_its_tab_closes()
    {
        var context = await NewContextAsync();

        var first = await context.NewPageAsync();
        await GotoAsync(first, "/");
        Assert.AreEqual("Opfs", await WaitForDatabaseAsync(first));

        await first.CloseAsync();

        var next = await context.NewPageAsync();
        await GotoAsync(next, "/");

        Assert.AreEqual("Opfs", await WaitForDatabaseAsync(next), "A closed tab should leave the lock for the next one to take.");
    }
}

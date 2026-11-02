using Bit.BlazeDb.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.BlazeDb.Tests.E2E;

/// <summary>
/// The demos that show one feature each - writes, backups, encryption, replica tabs, the storage quota,
/// streaming and change handlers - reach the outcome their page describes, in the browser. A scenario that
/// sees the engine do something its page does not describe logs a step saying "unexpected"; none may.
/// </summary>
[TestClass]
public class FeatureDemoTests : BlazeDbTest
{
    [TestMethod]
    public async Task Every_scenario_reports_what_it_describes()
    {
        var misses = new List<string>();

        foreach (var path in new[] { "/writes", "/transactions", "/efcore", "/backups" })
        {
            var (page, errors) = await OpenWatchedAsync(path);
            var scenarios = Panel(page, "Scenarios").Locator("button");
            var trace = Panel(page, "Trace");

            for (var i = 0; i < await scenarios.CountAsync(); i++)
            {
                var button = scenarios.Nth(i);
                await Assertions.Expect(button).ToBeEnabledAsync(new() { Timeout = 30_000 });
                var name = (await button.InnerTextAsync()).Trim();

                await button.ClickAsync();
                // The scenario's sample appears once it has started, and its buttons come back once it has ended.
                await Assertions.Expect(page.Locator(".bb-source-file", new() { HasText = $"Scenario: {name}" })).ToBeVisibleAsync();
                await Assertions.Expect(button).ToBeEnabledAsync(new() { Timeout = 30_000 });

                var text = await trace.InnerTextAsync();
                if (text.Contains("unexpected", StringComparison.OrdinalIgnoreCase))
                {
                    misses.Add($"{path} - {name}:{Environment.NewLine}{text}");
                }
            }

            misses.AddRange(errors.Select(e => $"{path}: {e}"));
        }

        Assert.IsEmpty(misses, string.Join(Environment.NewLine + Environment.NewLine, misses));
    }

    [TestMethod]
    public async Task An_encrypted_database_opens_with_its_key_and_with_nothing_else()
    {
        var (page, errors) = await OpenWatchedAsync("/encryption");
        var trace = Panel(page, "Trace");

        await Press(page, "BlazeDbEncryptedStorage.DeriveKey(passphrase, salt)");
        await Assertions.Expect(trace).ToContainTextAsync("Key derived", new() { Timeout = 60_000 });

        await Press(page, "Write six people through the encrypted storage");
        await Assertions.Expect(trace).ToContainTextAsync("Wrote, flushed and checkpointed");
        await Assertions.Expect(page.Locator(".bb-badge", new() { HasText = "\"Lovelace\" is nowhere in it" })).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".bb-badge", new() { HasText = "\"Lovelace\" is readable" })).ToBeVisibleAsync();

        await Press(page, "Reopen with this key");
        await Assertions.Expect(trace).ToContainTextAsync("people.Count → 6");

        await Press(page, "Reopen with a different passphrase");
        await Assertions.Expect(trace).ToContainTextAsync("A different passphrase cannot open it", new() { Timeout = 60_000 });

        await Press(page, "Flip one stored byte, then reopen");
        await Assertions.Expect(trace).ToContainTextAsync("The frame failed authentication");

        await Press(page, "Append half a frame to the log, then reopen");
        await Assertions.Expect(trace).ToContainTextAsync("Opened past the torn frame");

        await Press(page, "Rotate to a new key");
        await Assertions.Expect(trace).ToContainTextAsync("Opened with the new key", new() { Timeout = 60_000 });

        await Assertions.Expect(trace).Not.ToContainTextAsync("unexpected");
        Assert.IsEmpty(errors, string.Join(Environment.NewLine, errors));
    }

    [TestMethod]
    public async Task A_replica_follows_the_writer_s_checkpoints_and_refuses_writes()
    {
        var (page, errors) = await OpenWatchedAsync("/tabs");
        var trace = Panel(page, "Trace");

        await Press(page, "Become the writer");
        await Assertions.Expect(trace).ToContainTextAsync("This tab is the writer");
        await Press(page, "Insert a person");
        await Press(page, "CheckpointAsync - announced to replicas");

        await Press(page, "Open as a replica");
        await Assertions.Expect(trace).ToContainTextAsync("This tab follows the writer");
        var replica = Panel(page, "Replica");
        await Assertions.Expect(replica).ToContainTextAsync("people.Count → 1");

        // The replica sees nothing of a commit until the writer checkpoints and announces it.
        await Press(page, "Insert a person");
        await Assertions.Expect(replica).ToContainTextAsync("people.Count → 1");
        await Press(page, "CheckpointAsync - announced to replicas");
        await Assertions.Expect(trace).ToContainTextAsync("Replica reloaded");
        await Assertions.Expect(replica).ToContainTextAsync("people.Count → 2");

        await Press(page, "Try to write");
        await Assertions.Expect(trace).ToContainTextAsync("The replica refused the write");
        await Press(page, "ReloadAsync on the writer");
        await Assertions.Expect(trace).ToContainTextAsync("ReloadAsync is for replicas");

        await Press(page, "Close", exact: true);
        await Press(page, "Close - release the lock");
        await Press(page, "BlazeDbDatabase.DeleteAsync(storage)");
        await Assertions.Expect(trace).ToContainTextAsync("Deleted");

        Assert.IsEmpty(errors, string.Join(Environment.NewLine, errors));
    }

    [TestMethod]
    public async Task A_flush_past_the_quota_is_refused_and_retried_once_space_is_freed()
    {
        var (page, errors) = await OpenWatchedAsync("/storage");
        var quotaTrace = Panel(page, "Quota trace");

        // Each press commits 150 more rows and adds a step: the flush, or its refusal.
        var fill = page.GetByRole(AriaRole.Button, new() { Name = "Insert 150 rows, then FlushAsync" });
        var steps = quotaTrace.Locator(".bd-step");
        for (var press = 0; press < 6; press++)
        {
            var before = await steps.CountAsync();
            await Press(page, "Insert 150 rows, then FlushAsync");
            await Assertions.Expect(steps.Nth(before)).ToBeVisibleAsync();
            await Assertions.Expect(fill).ToBeEnabledAsync();
            if ((await quotaTrace.InnerTextAsync()).Contains("FlushAsync refused")) break;
        }

        await Assertions.Expect(quotaTrace).ToContainTextAsync("FlushAsync refused");
        await Assertions.Expect(quotaTrace).ToContainTextAsync("OnQuotaPressure");

        await Press(page, "Let the background flusher try");
        await Assertions.Expect(quotaTrace).ToContainTextAsync("The flusher keeps retrying");

        await Press(page, "Free space - raise the allowance - and flush");
        await Assertions.Expect(quotaTrace).ToContainTextAsync("Space freed, flush retried");
        await Assertions.Expect(quotaTrace).ToContainTextAsync("db.LastBackgroundError → null");

        Assert.IsEmpty(errors, string.Join(Environment.NewLine, errors));
    }

    [TestMethod]
    public async Task A_stream_yields_between_batches_and_refuses_a_write_made_meanwhile()
    {
        var (page, errors) = await OpenWatchedAsync("/queries");
        var stream = Panel(page, "Stream it");

        await Press(page, "await foreach … ExecuteAsync(batchSize)");
        await Assertions.Expect(stream.Locator(".bd-out")).ToContainTextAsync("yields", new() { Timeout = 60_000 });

        await Press(page, "Stream while another handler writes");
        await Assertions.Expect(stream.Locator(".bd-out")).ToContainTextAsync("the stream threw InvalidOperationException", new() { Timeout = 60_000 });

        await Assertions.Expect(Panel(page, "Plan")).ToContainTextAsync("source: ordered index 'Effort'");
        Assert.IsEmpty(errors, string.Join(Environment.NewLine, errors));
    }

    [TestMethod]
    public async Task A_failing_change_handler_surfaces_after_the_commit()
    {
        var (page, errors) = await OpenWatchedAsync("/changes");

        await Press(page, "A handler that throws");
        await Assertions.Expect(page.Locator(".bd-foot")).ToContainTextAsync("BlazeDbChangeHandlerException");
        await Assertions.Expect(Panel(page, "Change feed").Locator("tbody tr")).ToHaveCountAsync(1);

        await Press(page, "A handler that writes");
        await Assertions.Expect(Panel(page, "Change feed").Locator("tbody tr")).ToHaveCountAsync(3);
        await Assertions.Expect(Panel(page, "Change feed").Locator("tbody tr").First).ToContainTextAsync("settings: inserted");

        Assert.IsEmpty(errors, string.Join(Environment.NewLine, errors));
    }

    private static async Task Press(IPage page, string name, bool exact = false)
    {
        var button = page.GetByRole(AriaRole.Button, new() { Name = name, Exact = exact });
        await Assertions.Expect(button).ToBeEnabledAsync(new() { Timeout = 60_000 });
        await button.ClickAsync();
    }

    private async Task<(IPage Page, List<string> Errors)> OpenWatchedAsync(string path)
    {
        var page = await (await NewContextAsync()).NewPageAsync();
        var errors = CollectErrors(page);
        await GotoAsync(page, path);
        return (page, errors);
    }

    private static ILocator Panel(IPage page, string title) =>
        page.Locator(".bd-panel", new() { Has = page.Locator(".bd-panel-title", new() { HasText = title }) });
}

using System.Text.RegularExpressions;
using Bit.BlazeDb.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.BlazeDb.Tests.E2E;

/// <summary>
/// The interactive half of the site: every button a visitor can press, pressed, with the page asked
/// afterwards whether anything went wrong - and, for the scenarios whose outcome is the lesson, that the
/// outcome is the one the page describes. Against the published demo (see the README) this is also what
/// catches a scenario that only the trimmed client breaks.
/// </summary>
[TestClass]
public class DemoScenarioTests : BlazeDbTest
{
    /// <summary>Every page with something to press. The benchmark page is left out: one run takes minutes on a build server.</summary>
    private static readonly string[] _interactivePages =
        ["/todos", "/schema", "/writes", "/indexes", "/queries", "/efcore", "/transactions", "/changes", "/durability", "/backups", "/storage", "/tabs",
         "/encryption", "/serialization", "/errors"];

    /// <summary>
    /// The buttons of a page's content: not the site's chrome, not the copy buttons on code samples, and not a
    /// row's own edit and delete actions on the live app, which would press their way through the table.
    /// </summary>
    private const string ContentButtons = ".bb-docs-content button:not(.bb-copy-btn):not(.bd-rows button)";

    [TestMethod]
    public async Task Every_button_of_every_page_runs_without_an_error()
    {
        var misses = new List<string>();

        foreach (var path in _interactivePages)
        {
            var (page, errors) = await OpenWatchedAsync(path);
            var buttons = page.Locator(ContentButtons);
            var pressed = 0;

            // Indexes rather than a snapshot of the buttons: a scenario re-renders the page, and a button pressed
            // as a stale handle would be pressed on a node that is gone.
            for (var i = 0; i < await buttons.CountAsync(); i++)
            {
                var button = buttons.Nth(i);
                if (await button.IsVisibleAsync() is false) continue;

                // A scenario still running disables its page's buttons until it ends; a button with nothing to act
                // on (an empty form's Insert, the first page's Previous) stays disabled and is passed over.
                if (await button.IsEnabledAsync() is false)
                {
                    try { await Assertions.Expect(button).ToBeEnabledAsync(new() { Timeout = 8_000 }); }
                    catch (PlaywrightException) { continue; }
                }

                var name = (await button.InnerTextAsync()).Trim();
                await button.ClickAsync();
                pressed++;
                await page.WaitForTimeoutAsync(200);

                if (await page.Locator("#blazor-error-ui").IsVisibleAsync())
                {
                    misses.Add($"{path}: pressing '{name}' brought up the error UI.");
                    break;
                }
            }

            // Whatever the last press started has to finish cleanly too.
            await page.WaitForTimeoutAsync(1_000);
            if (await page.Locator("#blazor-error-ui").IsVisibleAsync() && misses.LastOrDefault()?.StartsWith(path + ":") is not true)
            {
                misses.Add($"{path}: the error UI came up after the last press.");
            }

            if (pressed == 0) misses.Add($"{path}: no button could be pressed.");
            misses.AddRange(errors.Select(e => $"{path}: {e}"));
        }

        Assert.IsEmpty(misses, string.Join(Environment.NewLine, misses));
    }

    [TestMethod]
    public async Task The_ef_core_scenarios_report_what_they_describe()
    {
        var (page, errors) = await OpenWatchedAsync("/efcore");
        var trace = Panel(page, "Trace");

        // Each scenario starts from a context that tracks nothing, as one created over an existing database does.
        await page.GetByRole(AriaRole.Button, new() { Name = "AsNoTracking still returns the live row" }).ClickAsync();
        await Assertions.Expect(trace).ToContainTextAsync("ChangeTracker.Entries().Count() → 0");
        await Assertions.Expect(trace).ToContainTextAsync("ReferenceEquals(entity, todos.Get(id)) → True");

        // The duplicate reaches the engine, which refuses it; the provider reports it as EF Core's own
        // exception, and the first insert of the same save is undone with it.
        await page.GetByRole(AriaRole.Button, new() { Name = "A failing save rolls everything back" }).ClickAsync();
        await Assertions.Expect(trace).ToContainTextAsync("DbUpdateException ← BlazeDbDuplicateKeyException");
        await Assertions.Expect(trace).ToContainTextAsync(new Regex(@"todos\.Count → (\d+) \(was \1\)"));

        // AsNoTracking (behind Find) and Include both run EntityFrameworkQueryableExtensions' static constructor,
        // which finds EF Core's own methods by reflection - by parameter name, for some of them.
        await page.GetByRole(AriaRole.Button, new() { Name = "Find", Exact = true }).ClickAsync();
        await Assertions.Expect(Panel(page, "Result")).ToContainTextAsync("1 row");
        await page.GetByRole(AriaRole.Button, new() { Name = "Run Include" }).ClickAsync();
        await Assertions.Expect(page.Locator(".bd-out")).ToContainTextAsync("NotSupportedException");

        Assert.IsFalse(await page.Locator("#blazor-error-ui").IsVisibleAsync(), "The error UI is showing.");
        Assert.IsEmpty(errors, string.Join(Environment.NewLine, errors));
    }

    [TestMethod]
    public async Task Every_error_case_behaves_as_documented()
    {
        var page = await OpenAsync("/errors");
        var cases = page.Locator(".bd-case");
        var count = await cases.CountAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Run every case" }).ClickAsync();
        await Assertions.Expect(page.Locator(".bd-case-result")).ToHaveCountAsync(count);

        var failed = await page.Locator(".bd-case", new() { Has = page.Locator(".bd-case-err") }).AllInnerTextsAsync();
        Assert.IsEmpty(failed, string.Join(Environment.NewLine + Environment.NewLine, failed));
        await Assertions.Expect(page.Locator(".bb-badge", new() { HasText = "behaved as documented" })).ToHaveTextAsync($"{count}/{count} behaved as documented");

        // The messages are the point of the page, so they have to be the runtime's sentences rather than the
        // resource keys a trimmed WebAssembly app shows by default ("Arg_ParamName_Name").
        var results = await page.Locator(".bd-case-result").AllInnerTextsAsync();
        var keys = results.Where(r => Regex.IsMatch(r, @"\b[A-Z][A-Za-z]*(?:_[A-Za-z]+)+\b")).ToList();
        Assert.IsEmpty(keys, string.Join(Environment.NewLine, keys));
    }

    [TestMethod]
    public async Task The_durability_scenarios_reach_the_outcome_they_describe()
    {
        var (page, errors) = await OpenWatchedAsync("/durability");
        var trace = Panel(page, "Trace");

        // Straight after the page opens the log holds no record yet; the scenario writes one rather than
        // finding nothing to damage.
        await page.GetByRole(AriaRole.Button, new() { Name = "Tear the log tail" }).ClickAsync();
        await Assertions.Expect(trace).ToContainTextAsync("Recovery ignored the partial record");
        await Assertions.Expect(trace).ToContainTextAsync("The database is fully usable");

        await page.GetByRole(AriaRole.Button, new() { Name = "Corrupt the last record" }).ClickAsync();
        await Assertions.Expect(trace).ToContainTextAsync("Replay stopped at the damage");

        // A checkpoint replaces the snapshot before it, so the page has to notice a new one by name, not by count.
        await page.GetByRole(AriaRole.Button, new() { Name = "CheckpointAsync" }).ClickAsync();
        await Assertions.Expect(trace).ToContainTextAsync("Snapshot is");
        await page.GetByRole(AriaRole.Button, new() { Name = "Fill until it self-checkpoints" }).ClickAsync();
        await Assertions.Expect(trace).ToContainTextAsync("The engine checkpointed itself", new() { Timeout = 30_000 });
        await Assertions.Expect(trace).Not.ToContainTextAsync("No snapshot yet");

        Assert.IsEmpty(errors, string.Join(Environment.NewLine, errors));
    }

    [TestMethod]
    public async Task The_date_ranges_open_on_rows_of_the_tabs_own_database()
    {
        // The demo's rows are as old as the tab's database - weeks, on OPFS - so a range that opened on a fixed
        // "last N days" would show an empty table to most visitors.
        var page = await OpenAsync("/queries");
        // Both pages open on the Effort index, which has rows of its own: wait for the render that switched to
        // CreatedAt - the date fields exist only for it - before reading what the range returned.
        await page.GetByRole(AriaRole.Button, new() { Name = "UseIndex(CreatedAt, range)" }).ClickAsync();
        await Assertions.Expect(page.Locator("#q-days")).ToBeVisibleAsync();
        await Assertions.Expect(Panel(page, "Plan")).ToContainTextAsync("ordered - CreatedAt");
        await Assertions.Expect(Panel(page, "Result").Locator("tbody tr").First).ToBeVisibleAsync();

        await GotoAsync(page, "/indexes");
        await page.Locator("#ord-index").SelectOptionAsync("CreatedAt");
        await Assertions.Expect(page.Locator("#date-from")).ToBeVisibleAsync();
        await Assertions.Expect(Panel(page, "Compose a range").Locator(".bd-out")).ToContainTextAsync("TodoEntry.Indexes.CreatedAt");
        await Assertions.Expect(Panel(page, "Compose a range").Locator("tbody tr").First).ToBeVisibleAsync();
    }

    /// <summary>Opens a page with its error listeners attached before it navigates, so an error raised while it boots counts too.</summary>
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

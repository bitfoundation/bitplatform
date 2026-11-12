using System.Text.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools.Tests;

/// <summary>
/// Going back to an earlier state through the provider's own contract: BevTools only asks a provider that can restore
/// itself, with the token it gave, and only where changing what the app does is allowed.
/// </summary>
[TestClass]
public class StateRestoreTests : BunitContext
{
    public StateRestoreTests()
    {
        // A circuit in Development, where restoring is allowed (the refusal elsewhere has a test of its own).
        Services.AddSingleton<Microsoft.Extensions.Hosting.IHostEnvironment>(new DevelopmentEnvironment());
        Services.AddBitBevToolsServices(o =>
        {
            o.Enabled = true;
            o.Ui.OpenByDefault = true;
        });
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.UseRenderer("Server");
    }

    private BevToolsSession Session => Services.GetRequiredService<BevToolsSession>();

    private sealed class DevelopmentEnvironment : Microsoft.Extensions.Hosting.IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";

        public string ApplicationName { get; set; } = "Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    /// <summary>A counter that keeps its versions and can go back to one.</summary>
    private sealed class VersionedCounter : IBevStateProvider, IBevStateRestorer
    {
        private readonly Dictionary<string, int> _versions = [];
        private int _version;

        public int Value { get; private set; }

        public string Name => "Counter";

        public event Action<BevStateChangeInfo>? Changed;

        public object? GetSnapshot() => this;

        public void Add(int amount)
        {
            Value += amount;
            Notify("Add", amount.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        public ValueTask RestoreAsync(string token, CancellationToken cancellationToken = default)
        {
            Value = _versions.TryGetValue(token, out var value) ? value : throw new InvalidOperationException("No version " + token);
            Notify("Restore", "to " + token);
            return ValueTask.CompletedTask;
        }

        private void Notify(string action, string detail)
        {
            var token = (++_version).ToString(System.Globalization.CultureInfo.InvariantCulture);
            _versions[token] = Value;
            Changed?.Invoke(new BevStateChangeInfo(action, detail) { RestoreToken = token });
        }
    }

    /// <summary>A provider that gives tokens but cannot restore: BevTools must not keep or offer them.</summary>
    private sealed class TokensOnly : IBevStateProvider
    {
        public string Name => "TokensOnly";

        public event Action<BevStateChangeInfo>? Changed;

        public object? GetSnapshot() => new { Value = 1 };

        public void Raise() => Changed?.Invoke(new BevStateChangeInfo("Set") { RestoreToken = "1" });
    }

    [TestMethod]
    public async Task A_restorable_provider_goes_back_to_the_state_after_an_earlier_change()
    {
        var counter = new VersionedCounter();
        Session.State.Register(counter);
        counter.Add(5);
        counter.Add(10);
        counter.Add(100);
        var afterFirst = Session.State.Entries.Single().Changes[0];
        Assert.AreEqual("1", afterFirst.RestoreToken);

        Assert.AreEqual("Restored Counter to after Add.", await Session.State.RestoreAsync(afterFirst.Id));
        Assert.AreEqual(5, counter.Value);
        var changes = Session.State.Entries.Single().Changes;
        Assert.AreEqual("Restore", changes[^1].Action, "the restore is a change of its own");
        Assert.AreEqual("Value: 115 → 5", changes[^1].Changes.Single() is var d ? d.Path + ": " + d.Before + " → " + d.After : null);
        var restored = Assert.ContainsSingle(e => e.Category == "state-restore", Session.Timeline.Snapshot());
        Assert.AreEqual(afterFirst.TimelineEventId, restored.ParentEventId);

        Assert.AreEqual("That change is no longer in the history.", await Session.State.RestoreAsync(9999));
    }

    [TestMethod]
    public async Task Tokens_are_kept_only_for_a_provider_that_can_restore_and_a_failing_restore_is_reported()
    {
        var tokens = new TokensOnly();
        Session.State.Register(tokens);
        tokens.Raise();
        var change = Session.State.Entries.Single().Changes.Single();
        Assert.IsNull(change.RestoreToken);
        Assert.Contains("cannot restore this change", await Session.State.RestoreAsync(change.Id));

        var counter = new VersionedCounter();
        Session.State.Register(counter);
        counter.Add(1);
        var counted = Session.State.Entries.Single(e => e.Name == "Counter").Changes.Single();
        // A token the provider no longer knows: its exception is an answer, and an error of BevTools' own.
        typeof(BevStateChangeRecord).GetProperty(nameof(BevStateChangeRecord.RestoreToken))!.SetValue(counted, "404");
        Assert.StartsWith("Counter could not restore it: InvalidOperationException: No version 404", await Session.State.RestoreAsync(counted.Id));
        Assert.Contains(e => e.Source == "bevtools", Session.Errors.Snapshot());
    }

    [TestMethod]
    public async Task Restoring_is_refused_outside_development_in_a_multi_user_process()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            session.Platform = "Server";
            var counter = new VersionedCounter();
            session.State.Register(counter);
            counter.Add(1);
            Assert.IsFalse(session.AllowsStateRestore);
            Assert.StartsWith("Restoring state is not allowed", await session.State.RestoreAsync(session.State.Entries.Single().Changes.Single().Id));
            Assert.AreEqual(1, counter.Value);
            Assert.AreEqual(BevCapabilityStatus.NotAvailable, session.Capabilities.Single(c => c.Name.StartsWith("Restoring an earlier state", StringComparison.Ordinal)).Status);
        }
    }

    [TestMethod]
    public void The_protocol_restores_by_change_id_and_exports_say_what_is_restorable()
    {
        var counter = new VersionedCounter();
        Session.State.Register(counter);
        counter.Add(2);
        counter.Add(3);
        var first = Session.State.Entries.Single().Changes[0];

        var answer = JsonDocument.Parse(BevToolsProtocol.Handle(Session, "command", "{\"name\":\"restore\",\"argument\":\"" + first.Id + "\"}")).RootElement;
        Assert.AreEqual("Restored Counter to after Add.", answer.GetProperty("done").GetString());
        Assert.AreEqual(2, counter.Value);

        using var json = JsonDocument.Parse(BevSessionExporter.ToJson(Session, new BevSessionExportOptions { Indented = false, Sections = new HashSet<string> { "state" } }));
        var text = json.RootElement.GetProperty("state").GetRawText();
        Assert.Contains("\"restorable\":true", text);
        Assert.DoesNotContain("restoreToken", text, StringComparison.Ordinal, "the token means something only to the live app");
    }

    [TestMethod]
    public void The_state_panel_shows_details_filters_changes_and_restores_from_the_history()
    {
        var counter = new VersionedCounter();
        Session.State.Register(counter);
        counter.Add(7);
        counter.Add(40);
        var panel = Render<BevToolsPanel>();
        panel.FindAll(".bev-tab").First(t => t.TextContent.Trim().StartsWith("State", StringComparison.Ordinal)).Click();
        Assert.Contains("Add · 40", panel.Find(".bev-right").TextContent);

        panel.Find("input[aria-label='Filter state providers and changes']").Input("40");
        var rows = panel.FindAll(".bev-right .bev-ev-row");
        Assert.ContainsSingle(rows, "only the change whose detail matches");

        panel.Find("input[aria-label='Filter state providers and changes']").Input("");
        // Newest first: the last row's button restores the first change.
        panel.FindAll(".bev-right .bev-ev-row button[aria-label^='Restore Counter to after Add']").Last().Click();
        Assert.AreEqual(7, counter.Value);
        Assert.Contains("Restored Counter to after Add.", panel.Markup);
    }

    [TestMethod]
    public void A_second_provider_of_a_name_already_shown_is_refused_and_said_to_be()
    {
        // The panel, the protocol and exports name a provider by its name: two adapters named alike cannot both be told apart.
        var first = new VersionedCounter();
        var second = new VersionedCounter();
        Session.State.Register(first);
        Session.State.Register(second).Dispose();
        first.Add(7);
        second.Add(40);

        var entry = Assert.ContainsSingle(Session.State.Entries);
        Assert.AreSame(first, entry.Provider);
        Assert.AreEqual("7", Assert.ContainsSingle(entry.Changes).Detail);
        Assert.Contains("A state provider named 'Counter' is already registered", Assert.ContainsSingle(e => e.Source == "bevtools", Session.Errors.Snapshot()).Message);
        Assert.AreSame(first, Assert.ContainsSingle(Session.State.Entries).Provider, "disposing the refused registration leaves the first one");
    }

    [TestMethod]
    public void The_detail_pane_shows_only_a_provider_the_filter_shows()
    {
        var counter = new VersionedCounter();
        Session.State.Register(counter);
        counter.Add(7);
        var panel = Render<BevToolsPanel>();
        panel.FindAll(".bev-tab").First(t => t.TextContent.Trim().StartsWith("State", StringComparison.Ordinal)).Click();
        Assert.Contains("Current snapshot", panel.Find(".bev-right").TextContent);

        panel.Find("input[aria-label='Filter state providers and changes']").Input("nothing like it");
        Assert.IsEmpty(panel.FindAll(".bev-left .bev-ev-row"));
        Assert.DoesNotContain("Current snapshot", panel.Find(".bev-right").TextContent, StringComparison.Ordinal);
    }
}

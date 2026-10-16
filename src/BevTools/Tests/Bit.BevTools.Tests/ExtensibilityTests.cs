using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools.Tests;

/// <summary>
/// Everything a library author is promised by the README, exercised the way they would use it: one extension class
/// that contributes a panel, a rule, a command, a state provider, a value formatter and a hidden namespace, plus
/// timeline events emitted from application code. If this test needs privileged access, the API is wrong.
/// </summary>
[TestClass]
public class ExtensibilityTests : BunitContext
{
    public ExtensibilityTests()
    {
        Services.AddSingleton<MoneyStore>();
        Services.AddBitBevToolsServices(o =>
        {
            o.Enabled = true;
            o.Extensions.Add(new AcmeBevToolsExtension());
        });
        this.UseRenderer("Server");
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private BevToolsSession Session => Services.GetRequiredService<BevToolsSession>();

    [TestMethod]
    public void An_extension_contributes_a_panel_a_rule_a_command_and_a_formatter()
    {
        var registry = Services.GetRequiredService<BevToolsRegistry>();

        Assert.Contains("Acme", registry.ExtensionNames);
        Assert.IsEmpty(registry.ExtensionErrors);
        Assert.Contains(p => p.Id == "acme" && p.ComponentType == typeof(AcmePanel), registry.Panels);
        Assert.Contains(r => r.Id == "acme.too-much-money", registry.Rules);
        Assert.Contains(c => c.Id == "acme.show", registry.Commands);
        Assert.Contains(typeof(Money), registry.Formatters.Keys);
        Assert.Contains("Acme.Internal", registry.HiddenNamespaces);
    }

    [TestMethod]
    public void The_extension_panel_is_shown_and_can_be_opened_like_a_built_in_one()
    {
        var cut = Render<BevToolsPanel>();
        cut.Find(".bev-badge").Click();

        var tab = cut.FindAll(".bev-tab").First(t => t.TextContent.Contains("Acme", StringComparison.Ordinal));
        tab.Click();

        Assert.Contains("Acme panel says hello", cut.Markup, StringComparison.Ordinal);
    }

    [TestMethod]
    public void A_state_provider_from_an_extension_is_attached_and_its_changes_are_diffed()
    {
        Session.EnsureActivated();
        var store = Services.GetRequiredService<MoneyStore>();

        store.Set(42);

        var entry = Assert.ContainsSingle(e => e.Name == "Acme money", Session.State.Entries);
        Assert.AreEqual("extension", entry.Origin);
        var change = Assert.ContainsSingle(entry.Changes);
        Assert.AreEqual("set", change.Action);
        Assert.Contains(c => c.Path == "Amount" && c.After == "42", change.Changes);
        Assert.Contains(e => e.Kind == BevToolsEventKind.StateChange && e.Title.Contains("Acme money", StringComparison.Ordinal), Session.Timeline.Snapshot());
    }

    [TestMethod]
    public void A_value_formatter_from_an_extension_is_used_by_the_inspector()
    {
        Assert.AreEqual("$5.00", Session.Inspector.Describe("price", new Money(5)).Display);
    }

    [TestMethod]
    public void Application_code_can_emit_its_own_timeline_events_through_the_public_api()
    {
        var timeline = Services.GetRequiredService<IBevToolsTimeline>();

        using (timeline.BeginActivity("acme", "Importing catalogue", "3 files"))
        {
            timeline.Record(new BevToolsEvent { Category = "acme", Title = "File 1 imported" });
        }

        var events = Session.Timeline.Snapshot();
        var activity = Assert.ContainsSingle(e => e.Title == "Importing catalogue", events);
        Assert.IsNotNull(activity.DurationMs);
        Assert.Contains(e => e.Title == "File 1 imported", events);
    }

    [TestMethod]
    public void An_extension_rule_runs_alongside_the_built_in_ones()
    {
        Session.EnsureActivated();
        Services.GetRequiredService<MoneyStore>().Set(1_000_000);
        Session.Diagnostics.Evaluate(force: true);

        Assert.Contains(f => f.Diagnostic.RuleId == "acme.too-much-money", Session.Diagnostics.Findings);
    }

    [TestMethod]
    public void An_extension_that_throws_while_configuring_is_reported_not_fatal()
    {
        var services = new ServiceCollection();
        services.AddBitBevToolsServices(o =>
        {
            o.Enabled = true;
            o.Extensions.Add(new BrokenExtension());
        });

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<BevToolsRegistry>();

        Assert.Contains(e => e.Contains("Broken", StringComparison.Ordinal), registry.ExtensionErrors);
        Assert.IsTrue(registry.IsEnabled);
    }
}

public readonly record struct Money(decimal Amount);

public sealed class MoneyStore
{
    public decimal Amount { get; private set; }

    public event Action<string>? Changed;

    public void Set(decimal amount)
    {
        Amount = amount;
        Changed?.Invoke("set");
    }
}

public sealed class MoneyStateProvider : IBevStateProvider
{
    private readonly MoneyStore _store;

    public MoneyStateProvider(MoneyStore store)
    {
        _store = store;
        _store.Changed += action => Changed?.Invoke(new BevStateChangeInfo(action));
    }

    public string Name => "Acme money";

    public string? Description => "The Acme sample store";

    public object? GetSnapshot() => new { _store.Amount };

    public event Action<BevStateChangeInfo>? Changed;
}

public sealed class AcmeBevToolsExtension : IBevToolsExtension
{
    public string Name => "Acme";

    public void Configure(IBevToolsExtensionBuilder builder) => builder
        .AddPanel(new BevToolsPanelDescriptor("acme", "Acme", typeof(AcmePanel)))
        .AddStateProvider(sp => new MoneyStateProvider(sp.GetRequiredService<MoneyStore>()))
        .AddDiagnosticRule(new TooMuchMoneyRule())
        .AddCommand(new ShowAcmeCommand())
        .AddValueFormatter(typeof(Money), v => ((Money)v).Amount.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("en-US")))
        .HideComponentsInNamespace("Acme.Internal");

    private sealed class TooMuchMoneyRule : IBevDiagnosticRule
    {
        public string Id => "acme.too-much-money";

        public string Title => "Suspiciously large amount";

        public string Description => "The Acme store holds an implausible amount.";

        public IEnumerable<BevDiagnostic> Evaluate(IBevDiagnosticContext context)
        {
            foreach (var change in context.Events.Where(e => e.Kind == BevToolsEventKind.StateChange && e.Category == "state:Acme money"))
            {
                if (change.Detail?.Contains("1000000", StringComparison.Ordinal) == true)
                {
                    yield return new BevDiagnostic(Id, BevDiagnosticSeverity.Warning, "Acme store holds 1,000,000", change.Detail, "Check the import.", Fingerprint: Id);
                }
            }
        }
    }

    private sealed class ShowAcmeCommand : IBevToolsCommand
    {
        public string Id => "acme.show";

        public string Title => "Show Acme";

        public string Category => "Acme";

        public IReadOnlyList<string> Keywords => ["acme"];

        public Task ExecuteAsync(IBevToolsCommandContext context, string? argument)
        {
            context.ShowPanel("acme");
            return Task.CompletedTask;
        }
    }
}

public sealed class BrokenExtension : IBevToolsExtension
{
    public string Name => "Broken";

    public void Configure(IBevToolsExtensionBuilder builder) => throw new InvalidOperationException("nope");
}

public class AcmePanel : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddContent(1, "Acme panel says hello");
        builder.CloseElement();
    }
}

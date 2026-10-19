using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools.Tests;

[TestClass]
public class CircuitRegistryTests
{
    [TestMethod]
    public void Registry_tracks_open_close_and_truncates_ids()
    {
        var registry = new BevCircuitRegistry();
        var info = registry.Open("abcdefghijklmnop", 1);
        Assert.AreEqual("abcdefgh…", info.ShortId);
        Assert.AreEqual(1, registry.ActiveCount);
        Assert.AreEqual(0, registry.ConnectedCount);

        info.IsConnected = true;
        Assert.AreEqual(1, registry.ConnectedCount);

        registry.Close("abcdefghijklmnop");
        Assert.AreEqual(0, registry.ActiveCount);
        Assert.AreEqual(1, registry.TotalClosed);
        Assert.IsTrue(registry.Circuits.Single().IsClosed);
    }

    [TestMethod]
    public void AddBitBevToolsServerServices_registers_handler_extension_and_panel()
    {
        var services = new ServiceCollection();
        services.AddBitBevToolsServerServices(o => o.Enabled = true);
        Assert.Contains(d => d.ServiceType == typeof(CircuitHandler) && d.ImplementationType == typeof(BevToolsCircuitHandler), services);
        Assert.Contains(d => d.ServiceType == typeof(BevCircuitRegistry), services);

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<BevToolsRegistry>();
        Assert.Contains("Blazor Server circuits", registry.ExtensionNames);
        var panel = Assert.ContainsSingle(registry.Panels);
        Assert.AreEqual("circuit", panel.Id);
        Assert.AreEqual("Server", panel.RequiredPlatform);
        Assert.Contains(c => c.Id == "circuit.show", registry.Commands);
        Assert.Contains(r => r.Id == "circuit.unstable", registry.Rules);
    }

    [TestMethod]
    public void Disabled_server_bevtools_registers_no_circuit_handler()
    {
        var services = new ServiceCollection();
        services.AddBitBevToolsServerServices(o => o.Enabled = false);
        Assert.DoesNotContain(d => d.ServiceType == typeof(CircuitHandler), services);
    }

    [TestMethod]
    public void A_later_core_registration_can_enable_server_circuit_instrumentation()
    {
        var services = new ServiceCollection();
        services.AddBitBevToolsServerServices(options => options.Enabled = false);
        services.AddBitBevToolsServices(options => options.Enabled = true);

        Assert.Contains(descriptor => descriptor.ServiceType == typeof(CircuitHandler)
            && descriptor.ImplementationType == typeof(BevToolsCircuitHandler), services);
    }

    [TestMethod]
    public void Circuit_instability_rule_fires_on_repeated_disconnects()
    {
        var (session, scope) = TestHelpers.CreateSession(o => o.Extensions.Add(new BevToolsServerExtension()));
        using (scope)
        {
            session.Timeline.Record(new BevToolsEvent { Kind = BevToolsEventKind.Circuit, Category = "circuit", Title = "Connection down" });
            session.Timeline.Record(new BevToolsEvent { Kind = BevToolsEventKind.Circuit, Category = "circuit", Title = "Connection up" });
            session.Timeline.Record(new BevToolsEvent { Kind = BevToolsEventKind.Circuit, Category = "circuit", Title = "Connection down" });
            session.Timeline.Record(new BevToolsEvent { Kind = BevToolsEventKind.Circuit, Category = "reconnect", Title = "Reconnect show" });
            session.Diagnostics.Evaluate(force: true);

            var finding = Assert.ContainsSingle(f => f.Diagnostic.RuleId == "circuit.unstable", session.Diagnostics.Findings);
            Assert.Contains("dropped 2 times", finding.Diagnostic.Title);
            Assert.Contains("Reconnect show", finding.Diagnostic.Evidence);
        }
    }

    [TestMethod]
    public void A_connection_that_keeps_failing_stays_one_finding()
    {
        // The fingerprint used to be the newest event's id, so every further drop or slow message opened a new
        // finding with its own timeline row instead of updating the one already shown.
        var (session, scope) = TestHelpers.CreateSession(o => o.Extensions.Add(new BevToolsServerExtension()));
        using (scope)
        {
            for (var i = 0; i < 5; i++)
            {
                session.Timeline.Record(new BevToolsEvent { Kind = BevToolsEventKind.Circuit, Category = "circuit", Title = "Connection down" });
                session.Timeline.Record(new BevToolsEvent { Kind = BevToolsEventKind.Circuit, Category = "circuit-inbound", Title = "Slow inbound message", DurationMs = 250 });
                session.Diagnostics.Evaluate(force: true);
            }

            var unstable = Assert.ContainsSingle(f => f.Diagnostic.RuleId == "circuit.unstable", session.Diagnostics.Findings);
            Assert.Contains("dropped 5 times", unstable.Diagnostic.Title);
            var blocked = Assert.ContainsSingle(f => f.Diagnostic.RuleId == "circuit.blocked", session.Diagnostics.Findings);
            Assert.StartsWith("5 inbound messages", blocked.Diagnostic.Title, StringComparison.Ordinal);
            Assert.AreEqual(2, session.Timeline.Snapshot().Count(e => e.Kind == BevToolsEventKind.Diagnostic && e.Category.StartsWith("diagnostic:circuit.", StringComparison.Ordinal)));
        }
    }
}

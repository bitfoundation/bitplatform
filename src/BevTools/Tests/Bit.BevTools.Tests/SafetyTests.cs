using System.Collections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.BevTools.Tests;

/// <summary>
/// Guards the properties that make BevTools safe to leave running in a real application: it must not leak secrets,
/// must not execute application logic while inspecting, and must not corrupt the values it passes through.
/// </summary>
[TestClass]
public class SafetyTests
{
    private sealed class Credentials
    {
        public string User { get; set; } = "ada";

        public byte[] ApiKeyBytes { get; set; } = [1, 2, 3];

        public int PasswordAttempts { get; set; } = 3;
    }

    private sealed record Login(string Email, string Password);

    private sealed class LazySequence : IEnumerable<int>
    {
        public int Enumerations { get; private set; }

        public IEnumerator<int> GetEnumerator()
        {
            Enumerations++;
            yield return 1;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class Node
    {
        public string Name { get; set; } = "root";

        public Node? Self { get; set; }
    }

    private static BevObjectInspector NewInspector() => new(new BitBevToolsOptions(), BevRedactor.Default);

    [TestMethod]
    public void Sensitive_names_are_redacted_whatever_the_value_type()
    {
        var inspector = NewInspector();
        var children = inspector.Children(new Credentials(), "");

        Assert.AreEqual("ada", children.Single(c => c.Name == "User").Display.Trim('"'));
        // A key does not stop being a secret because it is stored as bytes rather than as a string.
        Assert.AreEqual(BevRedactor.RedactedValue, children.Single(c => c.Name == "ApiKeyBytes").Display);
        Assert.AreEqual(BevRedactor.RedactedValue, children.Single(c => c.Name == "PasswordAttempts").Display);
    }

    [TestMethod]
    public void Records_do_not_leak_sensitive_members_through_their_generated_ToString()
    {
        var inspector = NewInspector();
        var node = inspector.Describe("login", new Login("ada@example.com", "hunter2"));

        Assert.DoesNotContain("hunter2", node.Display, StringComparison.Ordinal);
        Assert.AreEqual("Login", node.Display);
    }

    [TestMethod]
    public void Flatten_does_not_enumerate_lazy_sequences()
    {
        var sequence = new LazySequence();
        var flat = NewInspector().Flatten(new { Items = sequence });

        Assert.AreEqual(0, sequence.Enumerations);
        Assert.Contains(k => k == "Items", flat.Keys);
    }

    [TestMethod]
    public void Flatten_handles_self_referencing_objects()
    {
        var node = new Node();
        node.Self = node;

        var flat = NewInspector().Flatten(node);

        Assert.AreEqual("\"root\"", flat["Name"]);
        Assert.Contains("circular", flat["Self"], StringComparison.OrdinalIgnoreCase);
    }

    [TestMethod]
    public void Urls_hide_embedded_credentials_and_sensitive_query_values()
    {
        var redactor = BevRedactor.Default;

        Assert.AreEqual("https://«redacted»@api.example.com/v1", redactor.RedactUrl("https://ada:hunter2@api.example.com/v1"));
        Assert.AreEqual("https://api.example.com/v1?page=2&token=«redacted»", redactor.RedactUrl("https://api.example.com/v1?page=2&token=abc123"));
        Assert.AreEqual("https://api.example.com/v1?page=2", redactor.RedactUrl("https://api.example.com/v1?page=2"));
    }

    [TestMethod]
    public void Diff_keeps_only_the_most_specific_changed_paths()
    {
        var before = new Dictionary<string, string> { ["Items"] = "List (1 items)", ["Items[0].Name"] = "a", ["Other"] = "1" };
        var after = new Dictionary<string, string> { ["Items"] = "List (2 items)", ["Items[0].Name"] = "b", ["Other"] = "1" };

        var changes = BevObjectInspector.Diff(before, after);

        var change = Assert.ContainsSingle(changes);
        Assert.AreEqual("Items[0].Name", change.Path);
    }

    [TestMethod]
    public async Task Module_references_are_unwrapped_before_they_reach_the_framework()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            var inner = new FakeJSRuntime();
            var module = new FakeJSObjectReference();
            inner.Handler = (_, _) => module;
            var tracked = inner.WithBevToolsTracking(session);

            var wrapped = await tracked.InvokeAsync<IJSObjectReference>("import", ["./x.js"]);
            Assert.IsExactInstanceOfType<BevTrackingJSObjectReference>(wrapped);

            await tracked.InvokeVoidAsync("use", wrapped);

            // The framework serializes an IJSObjectReference by its concrete type; handing it a wrapper silently
            // produces a plain JSON object and the JS side receives garbage.
            Assert.AreSame(module, Assert.ContainsSingle(inner.LastArgs!));
        }
    }

    [TestMethod]
    public async Task Module_reference_wrapping_can_be_turned_off()
    {
        var (session, scope) = TestHelpers.CreateSession(o => o.TrackJsModuleReferences = false);
        using (scope)
        {
            var inner = new FakeJSRuntime();
            var module = new FakeJSObjectReference();
            inner.Handler = (_, _) => module;

            var result = await inner.WithBevToolsTracking(session).InvokeAsync<IJSObjectReference>("import", ["./x.js"]);

            Assert.AreSame(module, result);
        }
    }

    [TestMethod]
    public async Task Js_interop_duration_survives_the_timeline_buffer_wrapping_around()
    {
        var (session, scope) = TestHelpers.CreateSession(o => o.MaxEvents = 2);
        using (scope)
        {
            var inner = new FakeJSRuntime { Handler = (_, _) => null };
            var tracked = inner.WithBevToolsTracking(session);

            await tracked.InvokeVoidAsync("a");
            await tracked.InvokeVoidAsync("b");
            await tracked.InvokeVoidAsync("c");

            foreach (var call in session.Interop.Snapshot())
            {
                Assert.IsNotNull(call.DurationMs);
            }
        }
    }

    [TestMethod]
    public void Server_sessions_are_never_resolved_by_being_the_only_one()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            session.Platform = "WebAssembly";
            Assert.AreSame(session, SessionResolver.Resolve());

            // On a server, "the only circuit right now" is not evidence that ambient work belongs to that user.
            session.Platform = "Server";
            Assert.IsNull(SessionResolver.Resolve());
        }
    }

    [TestMethod]
    public void Component_tracking_stops_at_the_configured_budget()
    {
        var (session, scope) = TestHelpers.CreateSession(o => o.MaxTrackedComponents = 2);
        using (scope)
        {
            var tracked = new List<BevComponentRecord>();
            for (var i = 0; i < 5; i++)
            {
                var record = session.Components.Register(new ChildComponent(), typeof(ChildComponent), isBevTools: false, isHidden: false);
                if (record is not null)
                {
                    tracked.Add(record);
                }
            }

            Assert.AreEqual(2, session.Components.TrackedCount);
            Assert.IsTrue(session.Components.IsTruncated);
            tracked[0].IsDisposed = true;
            session.Components.ClearDisposed();
            Assert.IsTrue(session.Components.IsTruncated);
            Assert.AreEqual(1, session.Components.TrackedCount);
        }
    }

    [TestMethod]
    public void Second_registration_merges_configuration_instead_of_being_ignored()
    {
        var services = new ServiceCollection();
        services.AddBitBevToolsServices(o => o.Enabled = true);
        services.AddBitBevToolsServices(o => o.MaxEvents = 17);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<BitBevToolsOptions>();
        Assert.AreEqual(17, options.MaxEvents);
        Assert.IsTrue(provider.GetRequiredService<BevToolsRegistry>().IsEnabled);
    }

    [TestMethod]
    public void Second_registration_can_enable_a_previously_disabled_registration()
    {
        var services = new ServiceCollection();
        services.AddBitBevToolsServices(o => o.Enabled = false);
        services.AddBitBevToolsServices(o => o.Enabled = true);

        using var provider = services.BuildServiceProvider();
        Assert.IsTrue(provider.GetRequiredService<BevToolsRegistry>().IsEnabled);
        Assert.IsTrue(provider.GetRequiredService<BitBevToolsOptions>().ResolvedEnabled);
        Assert.Contains(d => d.ServiceType == typeof(Microsoft.AspNetCore.Components.IComponentActivator)
            && d.ImplementationType == typeof(BevToolsComponentActivator), services);
        Assert.ContainsSingle(d => d.ServiceType == typeof(Microsoft.Extensions.Http.IHttpMessageHandlerBuilderFilter)
            && d.ImplementationType == typeof(BevToolsHttpFilter), services);
    }

    [TestMethod]
    public void Server_package_contributes_its_panel_even_when_core_bevtools_was_added_first()
    {
        var services = new ServiceCollection();
        services.AddBitBevToolsServices(o => o.Enabled = true);
        services.AddBitBevToolsServerServices();

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<BevToolsRegistry>();
        Assert.Contains(p => p.Id == "circuit", registry.Panels);
        Assert.Contains(d => d.ServiceType == typeof(Microsoft.AspNetCore.Components.Server.Circuits.CircuitHandler), services);
    }
}

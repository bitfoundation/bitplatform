using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.BevTools.Tests;

/// <summary>
/// Deliberate attempts to break BevTools from the application side. The rule these enforce is simple: an application
/// that misbehaves must still run, and BevTools failing must never be the reason an application fails.
/// </summary>
[TestClass]
public class AdversarialTests : BunitContext
{
    public AdversarialTests()
    {
        Services.AddBitBevToolsServices(o =>
        {
            o.Enabled = true;
            o.CaptureComponentStateOnRender = true;
        });
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private BevToolsSession Session => Services.GetRequiredService<BevToolsSession>();

    [TestMethod]
    public void A_component_whose_every_member_throws_is_still_rendered_and_inspected()
    {
        var cut = Render<HostileComponent>();

        Assert.Contains("still here", cut.Markup, StringComparison.Ordinal);

        var record = Session.Components.All.Single(r => r.Type == typeof(HostileComponent));
        var children = Session.Inspector.Children(record.Component, "");

        Assert.Contains(c => c.Name == "Exploding" && c.Kind == BevNodeKind.Error, children);
        Assert.Contains(c => c.Name == "Enormous" && c.Count == 1_000_000, children);

        // The cycle is one level down: SelfReferencing.Next points back at SelfReferencing.
        var cycle = Session.Inspector.Children(record.Component, "SelfReferencing");
        Assert.Contains(c => c.Name == "Next" && c.Kind == BevNodeKind.Circular, cycle);
        Assert.AreEqual(1, record.RenderCount);
    }

    [TestMethod]
    public void An_endless_sequence_is_summarised_rather_than_enumerated_to_death()
    {
        var inspector = Session.Inspector;

        var node = inspector.Describe("endless", Endless());
        var children = inspector.Children(new { Endless = Endless() }, "Endless");

        Assert.AreEqual(BevNodeKind.Collection, node.Kind);
        Assert.IsTrue(children.Count <= Session.Options.MaxCollectionItems + 1);

        static IEnumerable<int> Endless()
        {
            var i = 0;
            while (true)
            {
                yield return i++;
            }
        }
    }

    [TestMethod]
    public void A_state_provider_that_throws_is_reported_and_does_not_break_the_session()
    {
        Session.EnsureActivated();
        var provider = new ExplodingStateProvider();

        using var registration = Session.StateProviders.Register(provider);
        provider.Raise();

        var entry = Assert.ContainsSingle(e => e.Name == "Exploding", Session.State.Entries);
        Assert.IsNotNull(entry.Error);
        Assert.Contains("InvalidOperationException", entry.Error!, StringComparison.Ordinal);
        Assert.IsTrue(Session.IsEnabled);
    }

    [TestMethod]
    public void A_component_disposed_during_its_own_event_does_not_break_tracking()
    {
        var cut = Render<SelfRemoving>();
        var childRecord = Session.Components.All.Single(r => r.Type == typeof(ChildComponent));

        cut.InvokeAsync(() => cut.Instance.RemoveChild());
        Session.Components.Refresh(force: true);

        Assert.IsTrue(childRecord.IsDisposed);
        Assert.IsNull(childRecord.Component);
        Assert.IsNotEmpty(Session.Components.BuildTree(includeHidden: false));
    }

    [TestMethod]
    public void An_exception_thrown_by_a_diagnostic_rule_is_contained()
    {
        Session.Diagnostics.AddRule(new ExplodingRule());

        Session.Diagnostics.Evaluate(force: true);

        Assert.Contains(f => f.Diagnostic.RuleId == "bevtools.rule-failed", Session.Diagnostics.Findings);
        Assert.IsTrue(Session.IsEnabled);
    }

    [TestMethod]
    public void Clearing_everything_mid_flight_leaves_a_usable_session()
    {
        var cut = Render<ParentComponent>();
        for (var i = 0; i < 50; i++)
        {
            cut.InvokeAsync(() => cut.Instance.Refresh());
        }

        Session.ClearAll();
        cut.InvokeAsync(() => cut.Instance.Refresh());
        Session.Components.Refresh(force: true);

        Assert.IsNotEmpty(Session.Timeline.Snapshot());
        Assert.IsNotEmpty(Session.Components.BuildTree(includeHidden: false));
    }

    [TestMethod]
    public void Inspecting_a_component_that_has_already_been_released_returns_nothing_instead_of_throwing()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            var record = session.Components.Register(new ChildComponent(), typeof(ChildComponent), isBevTools: false, isHidden: false)!;
            record.IsDisposed = true;
            record.DisposedAt = DateTimeOffset.UtcNow;
            record.ReleaseReferences();

            Assert.IsNull(record.Component);
            Assert.IsEmpty(session.Inspector.Children(record.Component, ""));
            Assert.AreEqual("null", session.Inspector.Describe("component", record.Component).Display);
        }
    }

    private sealed class ExplodingStateProvider : IBevStateProvider
    {
        public string Name => "Exploding";

        public object? GetSnapshot() => throw new InvalidOperationException("no snapshot for you");

        public void Raise() => Changed?.Invoke(new BevStateChangeInfo("boom"));

        public event Action<BevStateChangeInfo>? Changed;
    }

    private sealed class ExplodingRule : IBevDiagnosticRule
    {
        public string Id => "acme.exploding";

        public string Title => "Exploding";

        public string Description => "Throws every time.";

        public IEnumerable<BevDiagnostic> Evaluate(IBevDiagnosticContext context)
            => throw new InvalidOperationException("rule failure");
    }
}

public class HostileComponent : ComponentBase
{
    private readonly Node _self = new();

    public HostileComponent() => _self.Next = _self;

    public string Exploding => throw new InvalidOperationException("no reading this");

    public Node SelfReferencing => _self;

    public IEnumerable<int> Enormous => Enumerable.Range(0, 1_000_000);

    protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, "still here");

    public sealed class Node
    {
        public Node? Next { get; set; }
    }
}

public class SelfRemoving : ComponentBase
{
    private bool _showChild = true;

    public void RemoveChild()
    {
        _showChild = false;
        StateHasChanged();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        if (_showChild)
        {
            builder.OpenComponent<ChildComponent>(1);
            builder.AddComponentParameter(2, nameof(ChildComponent.Text), "bye");
            builder.CloseComponent();
        }

        builder.CloseElement();
    }
}

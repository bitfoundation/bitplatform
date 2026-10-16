using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools.Tests;

/// <summary>
/// "Why did this render?" has a follow-up question: "did it need to?". A parent that recreates a list, a lambda or an
/// anonymous object per render forces every child to re-render even when nothing observable changed, and Blazor cannot
/// tell (it only short-circuits primitive parameters). The inspector compares parameter values between captured
/// renders so the developer can.
/// </summary>
[TestClass]
public class WastedRenderTests : BunitContext
{
    public WastedRenderTests() => Services.AddBitBevToolsServices(o =>
    {
        o.Enabled = true;
        o.CaptureComponentStateOnRender = true;
    });

    private BevToolsSession Session => Services.GetRequiredService<BevToolsSession>();

    [TestMethod]
    public void A_parent_driven_render_with_identical_parameter_values_is_marked_as_forced()
    {
        var cut = Render<ListParent>();
        cut.InvokeAsync(() => cut.Instance.Bump());
        cut.InvokeAsync(() => cut.Instance.Bump());

        var child = Session.Components.All.Single(r => r.Type == typeof(ListChild));
        Assert.AreEqual(3, child.RenderCount);
        var last = child.LastRender!;
        Assert.AreEqual(BevRenderCause.ParentRender, last.Cause);
        Assert.IsTrue(last.ParametersCompared);
        Assert.IsEmpty(last.ChangedParameters!);
        Assert.IsTrue(last.IsWastedRender);
        // The first render has nothing to compare against and is never counted.
        Assert.AreEqual(2, child.ParentRendersWithUnchangedParameters);

        var evt = Session.Timeline.Find(last.EventId)!;
        Assert.IsTrue(evt.TryGetData("parametersUnchanged", out var unchanged) && unchanged is true);
    }

    [TestMethod]
    public void A_parameter_whose_contents_changed_is_reported_with_the_path_and_values()
    {
        var cut = Render<ListParent>();
        cut.InvokeAsync(() => cut.Instance.Add("c"));

        var child = Session.Components.All.Single(r => r.Type == typeof(ListChild));
        var last = child.LastRender!;
        Assert.AreEqual(BevRenderCause.ParentRender, last.Cause);
        Assert.IsFalse(last.IsWastedRender);
        // The most specific path wins (the parent "Items" entry collapses into it), same as state-provider diffs.
        Assert.AreEqual("Items[2]: ∅ → \"c\"", Assert.ContainsSingle(last.ChangedParameters!));
        Assert.AreEqual(0, child.ParentRendersWithUnchangedParameters);
    }

    /// <summary>
    /// Names here deliberately match none of the sensitive name patterns, so only the attribute stands between the
    /// value and the diff. Before the member was passed to the inspector, field diffs honoured the patterns only.
    /// </summary>
    [TestMethod]
    public void Sensitive_fields_and_parameters_never_appear_in_diffs_in_plain_text()
    {
        var cut = Render<SecretHost>();
        cut.InvokeAsync(() => cut.Instance.Rotate());

        var host = Session.Components.All.Single(r => r.Type == typeof(SecretHost));
        var child = Session.Components.All.Single(r => r.Type == typeof(SecretChild));

        // The unmarked sibling proves the field diff ran; the marked field must be invisible in it.
        var changed = host.LastRender!.ChangedState!;
        Assert.Contains(c => c.StartsWith("_note: \"n1\" → \"n2\"", StringComparison.Ordinal), changed);
        Assert.DoesNotContain(c => c.StartsWith("_apiPhrase", StringComparison.Ordinal), changed);
        Assert.DoesNotContain(c => c.Contains("plain-", StringComparison.Ordinal), changed);

        // The child's [BevToolsSensitive] parameter changed too; redacted values compare equal, which is the safe
        // direction: over-redaction never leaks, and the "forced" verdict is documented as observable-only.
        Assert.IsTrue(child.LastRender!.ParametersCompared);
        Assert.DoesNotContain(c => c.Contains("plain-", StringComparison.Ordinal), child.LastRender.ChangedParameters ?? []);
        Assert.AreEqual(BevRedactor.RedactedValue, child.LastParameterSnapshot!["Passphrase"]);
        Assert.DoesNotContain(k => k.StartsWith("Passphrase.", StringComparison.Ordinal), child.LastParameterSnapshot.Keys);
        Assert.DoesNotContain(e => e.Detail?.Contains("plain-", StringComparison.Ordinal) == true, Session.Timeline.Snapshot());
    }

    [TestMethod]
    public void The_parent_driven_rule_escalates_when_every_compared_render_was_forced()
    {
        var cut = Render<ListParent>();
        for (var i = 0; i < 12; i++)
        {
            cut.InvokeAsync(() => cut.Instance.Bump());
        }

        Session.Diagnostics.Evaluate(force: true);

        var finding = Assert.ContainsSingle(f => f.Diagnostic.RuleId == "render.parent-driven" && f.Diagnostic.Title.StartsWith("ListChild", StringComparison.Ordinal), Session.Diagnostics.Findings);
        Assert.AreEqual(BevDiagnosticSeverity.Warning, finding.Diagnostic.Severity);
        Assert.Contains("identical to the previous render", finding.Diagnostic.Evidence, StringComparison.Ordinal);
    }

    [TestMethod]
    public void Without_capture_the_rule_stays_informational_and_says_how_to_get_evidence()
    {
        using var context = new BunitContext();
        context.Services.AddBitBevToolsServices(o => o.Enabled = true);
        var cut = context.Render<ListParent>();
        for (var i = 0; i < 12; i++)
        {
            cut.InvokeAsync(() => cut.Instance.Bump());
        }

        var session = context.Services.GetRequiredService<BevToolsSession>();
        session.Diagnostics.Evaluate(force: true);

        var finding = Assert.ContainsSingle(f => f.Diagnostic.RuleId == "render.parent-driven" && f.Diagnostic.Title.StartsWith("ListChild", StringComparison.Ordinal), session.Diagnostics.Findings);
        Assert.AreEqual(BevDiagnosticSeverity.Info, finding.Diagnostic.Severity);
        Assert.Contains("Select the component in the inspector", finding.Diagnostic.Evidence, StringComparison.Ordinal);
    }

    /// <summary>
    /// <c>&lt;Child&gt;@count&lt;/Child&gt;</c>: every fragment displays as "RenderFragment", so comparing displays
    /// called new content identical, counted the render as forced and escalated the rule to a warning whose
    /// ShouldRender advice would freeze the count on screen.
    /// </summary>
    [TestMethod]
    public void New_child_content_is_a_changed_parameter_and_keeps_the_rule_informational()
    {
        var cut = Render<ContentParent>();
        for (var i = 0; i < 12; i++)
        {
            cut.InvokeAsync(() => cut.Instance.Bump());
        }

        var child = Session.Components.All.Single(r => r.Type == typeof(ContentChild));
        var last = child.LastRender!;
        Assert.AreEqual(BevRenderCause.ParentRender, last.Cause);
        Assert.IsFalse(last.IsWastedRender);
        Assert.AreEqual("ChildContent: new RenderFragment instance (its output cannot be compared)", Assert.ContainsSingle(last.ChangedParameters!));
        Assert.AreEqual(0, child.ParentRendersWithUnchangedParameters);
        Assert.AreEqual(12, child.RendersWithComparedParameters);
        Assert.AreEqual("12", cut.Markup.Trim());

        Session.Diagnostics.Evaluate(force: true);
        var finding = Assert.ContainsSingle(f => f.Diagnostic.RuleId == "render.parent-driven" && f.Diagnostic.Title.StartsWith("ContentChild", StringComparison.Ordinal), Session.Diagnostics.Findings);
        Assert.AreEqual(BevDiagnosticSeverity.Info, finding.Diagnostic.Severity);
        Assert.Contains("0 of 12 compared renders received identical parameter values", finding.Diagnostic.Evidence, StringComparison.Ordinal);
    }

    /// <summary>Non-capturing child content compiles to one cached delegate: the same instance renders the same output.</summary>
    [TestMethod]
    public void Static_child_content_is_the_same_instance_and_can_be_a_forced_render()
    {
        var cut = Render<ContentParent>(ps => ps.Add(p => p.Mode, "static"));
        cut.InvokeAsync(() => cut.Instance.Bump());
        cut.InvokeAsync(() => cut.Instance.Bump());

        var child = Session.Components.All.Single(r => r.Type == typeof(ContentChild));
        Assert.IsTrue(child.LastRender!.IsWastedRender);
        Assert.AreEqual(2, child.ParentRendersWithUnchangedParameters);
    }

    /// <summary>A fragment cached in a field is the same instance every time, yet renders the owner's current state.</summary>
    [TestMethod]
    public void A_cached_fragment_that_reads_component_state_is_never_judged_unchanged()
    {
        var cut = Render<ContentParent>(ps => ps.Add(p => p.Mode, "cached"));
        cut.InvokeAsync(() => cut.Instance.Bump());
        cut.InvokeAsync(() => cut.Instance.Bump());

        var child = Session.Components.All.Single(r => r.Type == typeof(ContentChild));
        var last = child.LastRender!;
        Assert.IsFalse(last.IsWastedRender);
        Assert.AreEqual("ChildContent: RenderFragment reads component state when it runs (its output cannot be compared)", Assert.ContainsSingle(last.ChangedParameters!));
        Assert.AreEqual(0, child.ParentRendersWithUnchangedParameters);
        Assert.AreEqual("2", cut.Markup.Trim());
    }

    /// <summary>
    /// The forced count used to be shown as a fraction of the samples still in the ring buffer, so it could read
    /// "250 of 100 compared renders". Both sides are now session-long counters, as is the per-cause breakdown.
    /// </summary>
    [TestMethod]
    public void Forced_compared_and_per_cause_counts_are_not_capped_by_the_sample_ring()
    {
        using var context = new BunitContext();
        context.Services.AddBitBevToolsServices(o =>
        {
            o.Enabled = true;
            o.CaptureComponentStateOnRender = true;
            o.MaxRendersPerComponent = 5;
        });
        var cut = context.Render<ListParent>();
        for (var i = 0; i < 12; i++)
        {
            cut.InvokeAsync(() => cut.Instance.Bump());
        }

        var session = context.Services.GetRequiredService<BevToolsSession>();
        var child = session.Components.All.Single(r => r.Type == typeof(ListChild));
        Assert.AreEqual(5, child.Renders.Length);
        Assert.AreEqual(13, child.RenderCount);
        Assert.AreEqual(12, child.RendersWithComparedParameters);
        Assert.AreEqual(12, child.ParentRendersWithUnchangedParameters);
        Assert.AreEqual(12, child.RendersByCause(BevRenderCause.ParentRender));
        Assert.AreEqual(1, child.RendersByCause(BevRenderCause.Initial));
        Assert.Contains("| 12 of 12 compared |", BevSessionExporter.ToMarkdownReport(session), StringComparison.Ordinal);

        session.Components.MarkBaseline();
        cut.InvokeAsync(() => cut.Instance.Bump());
        Assert.AreEqual(1, child.RendersByCause(BevRenderCause.ParentRender, sinceBaseline: true));
        Assert.AreEqual(0, child.RendersByCause(BevRenderCause.Initial, sinceBaseline: true));
    }

    /// <summary>Passes child content in three shapes: rebuilt per render, static, or cached in a field.</summary>
    public sealed class ContentParent : ComponentBase
    {
        private RenderFragment? _cached;
        private int _count;

        [Parameter] public string Mode { get; set; } = "rebuilt";

        public void Bump()
        {
            _count++;
            StateHasChanged();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            RenderFragment content = Mode switch
            {
                "static" => b => b.AddContent(0, "static"),
                "cached" => _cached ??= b => b.AddContent(0, _count),
                _ => b => b.AddContent(0, _count),
            };
            builder.OpenComponent<ContentChild>(0);
            builder.AddComponentParameter(1, nameof(ContentChild.ChildContent), content);
            builder.CloseComponent();
        }
    }

    public sealed class ContentChild : ComponentBase
    {
        [Parameter] public RenderFragment? ChildContent { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, ChildContent);
    }

    /// <summary>Recreates the list on every render, which is exactly what forces <see cref="ListChild"/> to re-render.</summary>
    public sealed class ListParent : ComponentBase
    {
        private readonly List<string> _items = ["a", "b"];
        private int _counter;

        public void Bump()
        {
            _counter++;
            StateHasChanged();
        }

        public void Add(string item)
        {
            _items.Add(item);
            StateHasChanged();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<ListChild>(0);
            builder.AddComponentParameter(1, nameof(ListChild.Items), new List<string>(_items));
            builder.CloseComponent();
            builder.AddContent(2, _counter);
        }
    }

    public sealed class ListChild : ComponentBase
    {
        [Parameter] public List<string> Items { get; set; } = [];

        protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, string.Join(",", Items));
    }

    public sealed class SecretHost : ComponentBase
    {
        [BevToolsSensitive] private string _apiPhrase = "plain-1";
        private string _note = "n1";

        public void Rotate()
        {
            _apiPhrase = "plain-2";
            _note = "n2";
            StateHasChanged();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<SecretChild>(0);
            builder.AddComponentParameter(1, nameof(SecretChild.Passphrase), new Phrase(_apiPhrase));
            builder.CloseComponent();
            builder.AddContent(2, _note);
        }
    }

    public sealed record Phrase(string Value);

    public sealed class SecretChild : ComponentBase
    {
        [Parameter, BevToolsSensitive] public Phrase? Passphrase { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, Passphrase?.Value.Length ?? 0);
    }
}

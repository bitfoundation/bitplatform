
namespace Bit.BevTools.Tests;

[TestClass]
public class DiagnosticsRulesTests
{
    private static BevToolsEvent Render(long componentId, string name, string cause, double ms = 1) => new()
    {
        Kind = BevToolsEventKind.Render,
        Category = "render",
        Title = name + " re-rendered",
        Detail = cause == "ParentRender" ? "Parent Dashboard re-rendered (parameters re-applied)" : "Event: onclick → Filter.Set",
        DurationMs = ms,
        ComponentInstanceId = componentId,
        ComponentName = name,
        Data = new Dictionary<string, object?> { ["cause"] = cause },
    };

    [TestMethod]
    public void Render_storm_reports_cause_breakdown_and_component()
    {
        var (session, scope) = TestHelpers.CreateSession(o => o.Diagnostics.RenderStormCount = 10);
        using (scope)
        {
            for (var i = 0; i < 9; i++)
            {
                session.Timeline.Record(Render(7, "ProductList", "ParentRender"));
            }

            for (var i = 0; i < 3; i++)
            {
                session.Timeline.Record(Render(7, "ProductList", "UiEvent"));
            }

            session.Diagnostics.Evaluate(force: true);
            var finding = Assert.ContainsSingle(f => f.Diagnostic.RuleId == "render.storm", session.Diagnostics.Findings);
            Assert.AreEqual(7, finding.Diagnostic.ComponentInstanceId);
            Assert.Contains("ProductList rendered 12 times", finding.Diagnostic.Title);
            Assert.Contains("9 × parent re-render", finding.Diagnostic.Evidence);
            Assert.Contains("3 × UI event", finding.Diagnostic.Evidence);
            Assert.Contains("ShouldRender", finding.Diagnostic.Suggestion!);
        }
    }

    [TestMethod]
    public void Below_threshold_produces_no_storm_finding()
    {
        var (session, scope) = TestHelpers.CreateSession(o => o.Diagnostics.RenderStormCount = 10);
        using (scope)
        {
            for (var i = 0; i < 5; i++)
            {
                session.Timeline.Record(Render(1, "Calm", "UiEvent"));
            }

            session.Diagnostics.Evaluate(force: true);
            Assert.DoesNotContain(f => f.Diagnostic.RuleId == "render.storm", session.Diagnostics.Findings);
        }
    }

    [TestMethod]
    public void Duplicate_http_requests_are_detected_with_trigger_context()
    {
        var (session, scope) = TestHelpers.CreateSession(o => o.Diagnostics.DuplicateHttpCount = 3);
        using (scope)
        {
            var trigger = session.Timeline.Record(new BevToolsEvent { Kind = BevToolsEventKind.UiEvent, Category = "ui-event", Title = "oninput → FilterBar.OnQuery" });
            for (var i = 0; i < 4; i++)
            {
                session.Timeline.Record(new BevToolsEvent { Kind = BevToolsEventKind.Http, Category = "http", Title = "GET /api/search", ParentEventId = trigger, DurationMs = 5 });
            }

            session.Timeline.Record(new BevToolsEvent { Kind = BevToolsEventKind.Http, Category = "http", Title = "GET /api/other", DurationMs = 5 });
            session.Diagnostics.Evaluate(force: true);
            var finding = Assert.ContainsSingle(f => f.Diagnostic.RuleId == "http.duplicate", session.Diagnostics.Findings);
            Assert.Contains("GET /api/search", finding.Diagnostic.Title);
            Assert.Contains("oninput → FilterBar.OnQuery", finding.Diagnostic.Evidence);
        }
    }

    [TestMethod]
    public void Slow_and_excessive_js_interop_are_detected()
    {
        var (session, scope) = TestHelpers.CreateSession(o =>
        {
            o.Diagnostics.SlowJsInteropMs = 100;
            o.Diagnostics.ExcessiveJsInteropCount = 5;
        });
        using (scope)
        {
            session.Timeline.Record(new BevToolsEvent { Kind = BevToolsEventKind.JsInterop, Category = "js-interop", Title = "→ JS module.slowCall", DurationMs = 400, ComponentName = "Interop" });
            for (var i = 0; i < 6; i++)
            {
                session.Timeline.Record(new BevToolsEvent { Kind = BevToolsEventKind.JsInterop, Category = "js-interop", Title = "→ JS module.ping", DurationMs = 1 });
            }

            session.Diagnostics.Evaluate(force: true);
            var findings = session.Diagnostics.Findings;
            Assert.Contains(f => f.Diagnostic.RuleId == "jsinterop.slow" && f.Diagnostic.Evidence.Contains("Interop"), findings);
            Assert.Contains(f => f.Diagnostic.RuleId == "jsinterop.excessive" && f.Diagnostic.Title.Contains("module.ping"), findings);
        }
    }

    [TestMethod]
    public void Repeated_errors_and_findings_are_deduplicated_across_evaluations()
    {
        var (session, scope) = TestHelpers.CreateSession(o => o.Diagnostics.RepeatedErrorCount = 2);
        using (scope)
        {
            session.Errors.Record(new InvalidOperationException("boom"), "render");
            session.Errors.Record(new InvalidOperationException("boom"), "render");
            session.Diagnostics.Evaluate(force: true);
            var first = Assert.ContainsSingle(f => f.Diagnostic.RuleId == "error.repeated", session.Diagnostics.Findings);
            session.Errors.Record(new InvalidOperationException("boom"), "render");
            session.Diagnostics.Evaluate(force: true);
            var again = Assert.ContainsSingle(f => f.Diagnostic.RuleId == "error.repeated", session.Diagnostics.Findings);
            Assert.AreSame(first, again);
            Assert.IsTrue(again.LastSeen >= again.FirstSeen);
            Assert.Contains("occurred 3 times", again.Diagnostic.Title);
            Assert.ContainsSingle(e => e.Kind == BevToolsEventKind.Diagnostic && e.Category == "diagnostic:error.repeated", session.Timeline.Snapshot());
        }
    }

    [TestMethod]
    public void Disabled_rules_do_not_run_and_custom_rules_do()
    {
        var custom = new CountingRule();
        var (session, scope) = TestHelpers.CreateSession(o =>
        {
            o.Diagnostics.DisabledRules.Add("render.storm");
            o.Extensions.Add(new RuleExtension(custom));
        });
        using (scope)
        {
            for (var i = 0; i < 100; i++)
            {
                session.Timeline.Record(Render(1, "Storm", "UiEvent"));
            }

            session.Diagnostics.Evaluate(force: true);
            Assert.DoesNotContain(f => f.Diagnostic.RuleId == "render.storm", session.Diagnostics.Findings);
            Assert.Contains(f => f.Diagnostic.RuleId == "test.custom", session.Diagnostics.Findings);
            Assert.AreEqual(1, custom.Evaluations);
        }
    }

    /// <summary>
    /// The parent-driven rule turns from information into a warning once parameter comparison proves the renders
    /// wasted. That escalation used to overwrite the finding silently: the timeline kept only the Info row and the
    /// warning badge changed with no record of when or why.
    /// </summary>
    [TestMethod]
    public void An_escalation_is_recorded_once_and_brings_a_dismissed_finding_back()
    {
        var rule = new AdjustableRule();
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            session.Diagnostics.AddRule(rule);
            session.Diagnostics.Evaluate(force: true);
            var finding = Assert.ContainsSingle(f => f.Diagnostic.RuleId == rule.Id, session.Diagnostics.Findings);
            var firstRow = finding.TimelineEventId;
            session.Diagnostics.Dismiss(finding.Diagnostic.EffectiveFingerprint);

            rule.Severity = BevDiagnosticSeverity.Warning;
            session.Diagnostics.Evaluate(force: true);

            Assert.IsFalse(finding.Dismissed);
            Assert.AreEqual(1, session.Diagnostics.ActiveWarningsOrErrors);
            Assert.AreNotEqual(firstRow, finding.TimelineEventId);
            var escalation = session.Timeline.Find(finding.TimelineEventId)!;
            Assert.AreEqual(BevToolsSeverity.Warning, escalation.Severity);
            Assert.AreEqual("adjustable finding (escalated from Info)", escalation.Title);
            Assert.IsTrue(escalation.TryGetData("escalatedFrom", out var from) && (string?)from == "Info");

            // Flapping between two severities reports nothing new: only a new high is news.
            rule.Severity = BevDiagnosticSeverity.Info;
            session.Diagnostics.Evaluate(force: true);
            rule.Severity = BevDiagnosticSeverity.Warning;
            session.Diagnostics.Evaluate(force: true);
            Assert.AreEqual(2, session.Timeline.Snapshot().Count(e => e.Category == "diagnostic:" + rule.Id));
        }
    }

    private sealed class AdjustableRule : IBevDiagnosticRule
    {
        public BevDiagnosticSeverity Severity { get; set; } = BevDiagnosticSeverity.Info;

        public string Id => "test.adjustable";

        public string Title => "Adjustable";

        public string Description => "";

        public IEnumerable<BevDiagnostic> Evaluate(IBevDiagnosticContext context)
        {
            yield return new BevDiagnostic(Id, Severity, "adjustable finding", "evidence", Fingerprint: Id);
        }
    }

    private sealed class RuleExtension(IBevDiagnosticRule rule) : IBevToolsExtension
    {
        public string Name => "test";

        public void Configure(IBevToolsExtensionBuilder builder) => builder.AddDiagnosticRule(rule);
    }

    private sealed class CountingRule : IBevDiagnosticRule
    {
        public int Evaluations;

        public string Id => "test.custom";

        public string Title => "Custom";

        public string Description => "";

        public IEnumerable<BevDiagnostic> Evaluate(IBevDiagnosticContext context)
        {
            Evaluations++;
            yield return new BevDiagnostic(Id, BevDiagnosticSeverity.Info, "custom finding", context.Events.Count + " events");
        }
    }
}

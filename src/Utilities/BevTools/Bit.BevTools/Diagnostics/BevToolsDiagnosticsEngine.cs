using System.Diagnostics;

namespace Bit.BevTools;

/// <summary>Runs rules on the UI cadence (at most once per second), deduplicates findings and records new ones in the timeline.</summary>
internal sealed class BevToolsDiagnosticsEngine
{
    private readonly BevToolsSession _session;
    private readonly BevToolsOptions _options;
    private readonly List<IBevToolsDiagnosticRule> _rules = [];
    private readonly Dictionary<string, BevToolsDiagnosticRecord> _findings = new(StringComparer.Ordinal);
    private readonly object _lock = new();

    // The panel, a frontend's snapshot and an export can ask from different threads: one run at a time keeps the run
    // bookkeeping consistent and never runs the rules (an extension's included) concurrently with themselves.
    private readonly object _runLock = new();
    private long _lastRunTicks;
    private long _lastRunVersion = -1;

    /// <summary>Something was captured since the last evaluation: the findings may be behind the data.</summary>
    public bool IsStale => _session.IsEnabled && _lastRunVersion != _session.Version;
    // The service graph is analysed once (it does not change) and its findings are re-reported on every evaluation, so
    // they stay as recently seen as the rules' and are never the first evicted when per-occurrence findings pile up.
    private List<BevToolsDiagnostic>? _serviceGraphFindings;

    public BevToolsDiagnosticsEngine(BevToolsSession session, BevToolsOptions options, BevToolsRegistry registry)
    {
        _session = session;
        _options = options;
        _rules.AddRange(BevToolsBuiltInRules.Create());
        _rules.AddRange(registry.Rules);
    }

    public IReadOnlyList<IBevToolsDiagnosticRule> Rules => _rules;

    public void AddRule(IBevToolsDiagnosticRule rule)
    {
        lock (_lock)
        {
            if (_rules.All(r => r.Id != rule.Id))
            {
                _rules.Add(rule);
            }
        }
    }

    public BevToolsDiagnosticRecord[] Findings
    {
        get { lock (_lock) { return _findings.Values.OrderByDescending(f => f.Diagnostic.Severity).ThenByDescending(f => f.LastSeen).ToArray(); } }
    }

    public int ActiveCount
    {
        get { lock (_lock) { return _findings.Values.Count(f => !f.Dismissed); } }
    }

    public int ActiveWarningsOrErrors
    {
        get { lock (_lock) { return _findings.Values.Count(f => !f.Dismissed && f.Diagnostic.Severity >= BevToolsDiagnosticSeverity.Warning); } }
    }

    public void Evaluate(bool force = false)
    {
        lock (_runLock)
        {
            EvaluateCore(force);
        }
    }

    private void EvaluateCore(bool force)
    {
        if (!_session.IsEnabled)
        {
            return;
        }

        var now = Stopwatch.GetTimestamp();
        if (!force && (Stopwatch.GetElapsedTime(_lastRunTicks).TotalMilliseconds < 1000 || _lastRunVersion == _session.Version))
        {
            return;
        }

        _lastRunTicks = now;
        _lastRunVersion = _session.Version;

        var components = _session.Components.All.Where(c => !c.IsBevTools).Select(c => c.ToSnapshot()).ToList();
        var events = _session.Timeline.Snapshot();
        var context = new BevToolsDiagnosticContext(_session, components, events);
        var produced = new List<BevToolsDiagnostic>();

        IBevToolsDiagnosticRule[] rules;
        lock (_lock)
        {
            rules = _rules.ToArray();
        }

        foreach (var rule in rules)
        {
            if (_options.Diagnostics.DisabledRules.Contains(rule.Id))
            {
                continue;
            }

            try
            {
                produced.AddRange(rule.Evaluate(context));
            }
            catch (Exception ex)
            {
                produced.Add(new BevToolsDiagnostic("bevtools.rule-failed", BevToolsDiagnosticSeverity.Info, $"Rule {rule.Id} threw", ex.GetType().Name + ": " + BevToolsRedactor.RedactText(ex.Message), Fingerprint: "rule-failed:" + rule.Id));
            }
        }

        if (_serviceGraphFindings is null && _session.Registry.ServiceGraph is { } graph)
        {
            _serviceGraphFindings = graph.Analyze().ToList();
        }

        if (_serviceGraphFindings is not null)
        {
            produced.AddRange(_serviceGraphFindings);
        }

        Merge(produced);
        _session.Overhead.AddDiagnostics(Stopwatch.GetTimestamp() - now);
    }

    private void Merge(List<BevToolsDiagnostic> produced)
    {
        var now = DateTimeOffset.UtcNow;
        var changed = false;
        lock (_lock)
        {
            foreach (var diagnostic in produced)
            {
                var key = diagnostic.EffectiveFingerprint;
                if (_findings.TryGetValue(key, out var existing))
                {
                    existing.Diagnostic = diagnostic;
                    existing.LastSeen = now;
                    if (diagnostic.Severity > existing.HighestSeverity)
                    {
                        // A stronger verdict on a known finding is news (the parent-driven rule becomes a warning once
                        // parameter comparison proves the renders wasted): it gets its own timeline row, and dismissing
                        // the weaker version does not hide it. Only a new high is reported, so a finding that moves
                        // back and forth between two severities does not flood the timeline.
                        var previous = existing.HighestSeverity;
                        existing.HighestSeverity = diagnostic.Severity;
                        existing.Dismissed = false;
                        RecordTimeline(existing, key, escalatedFrom: previous);
                        changed = true;
                    }

                    continue;
                }

                var record = new BevToolsDiagnosticRecord { Diagnostic = diagnostic, FirstSeen = now, LastSeen = now, HighestSeverity = diagnostic.Severity };
                _findings[key] = record;
                changed = true;
                RecordTimeline(record, key, escalatedFrom: null);
            }

            changed |= EvictLeastRecentlySeen();
        }

        if (changed)
        {
            _session.Touch();
        }
    }

    /// <summary>
    /// Keeps at most <see cref="BevToolsOptions.MaxFindings"/> findings, dropping the ones no rule has reported for
    /// the longest time. Several rules fingerprint per occurrence (a slow request, a cascading batch), so a long session
    /// would otherwise keep every one of them for good. A finding that is still being reported is seen again on every
    /// evaluation, so it is never among the oldest. Called under the lock.
    /// </summary>
    private bool EvictLeastRecentlySeen()
    {
        var excess = _findings.Count - Math.Max(1, _options.MaxFindings);
        if (excess <= 0)
        {
            return false;
        }

        foreach (var key in _findings.OrderBy(f => f.Value.LastSeen).ThenBy(f => f.Value.Diagnostic.Severity).Take(excess).Select(f => f.Key).ToList())
        {
            _findings.Remove(key);
        }

        return true;
    }

    private void RecordTimeline(BevToolsDiagnosticRecord record, string fingerprint, BevToolsDiagnosticSeverity? escalatedFrom)
    {
        var diagnostic = record.Diagnostic;
        var data = new Dictionary<string, object?> { ["fingerprint"] = fingerprint };
        if (escalatedFrom is { } previous)
        {
            data["escalatedFrom"] = previous.ToString();
        }

        var id = _session.Timeline.Record(new BevToolsEvent
        {
            Kind = BevToolsEventKind.Diagnostic,
            Category = "diagnostic:" + diagnostic.RuleId,
            Title = escalatedFrom is { } from ? $"{diagnostic.Title} (escalated from {from})" : diagnostic.Title,
            Detail = diagnostic.Evidence,
            Severity = diagnostic.Severity switch
            {
                BevToolsDiagnosticSeverity.Error => BevToolsSeverity.Error,
                BevToolsDiagnosticSeverity.Warning => BevToolsSeverity.Warning,
                _ => BevToolsSeverity.Info,
            },
            ComponentInstanceId = diagnostic.ComponentInstanceId,
            Data = data,
        });

        // Paused: no row was written, so there is nothing to link to.
        if (id > 0)
        {
            record.TimelineEventId = id;
        }
    }

    public void Dismiss(string fingerprint)
    {
        lock (_lock)
        {
            if (_findings.TryGetValue(fingerprint, out var record))
            {
                record.Dismissed = true;
            }
        }

        _session.Touch();
    }

    public void Clear()
    {
        lock (_lock)
        {
            _findings.Clear();
            _serviceGraphFindings = null;
        }

        _session.Touch();
    }
}

using System.Diagnostics;

namespace Bit.BevTools;

internal sealed class BevToolsDiagnosticContext(BevToolsSession session, IReadOnlyList<BevToolsComponentSnapshot> components, BevToolsEvent[] events) : IBevToolsDiagnosticContext
{
    public DateTimeOffset Now { get; } = DateTimeOffset.UtcNow;

    /// <summary>For the built-in rules that need more than the contract offers (the prerendering summary, the HTTP log).</summary>
    internal BevToolsSession Session => session;

    public IReadOnlyList<BevToolsComponentSnapshot> Components => components;

    public IReadOnlyList<BevToolsEvent> Events { get; } = events;

    public string? Platform => session.Platform;

    public IEnumerable<BevToolsEvent> EventsInWindow(BevToolsEventKind kind, TimeSpan window)
    {
        var threshold = Stopwatch.GetTimestamp() - (long)(window.TotalSeconds * Stopwatch.Frequency);
        for (var i = events.Length - 1; i >= 0; i--)
        {
            var evt = events[i];
            if (evt.StartTicks < threshold)
            {
                yield break;
            }

            if (evt.Kind == kind)
            {
                yield return evt;
            }
        }
    }

    private HashSet<long>? _frameworkInternals;

    public bool IsFrameworkInternal(long instanceId)
    {
        _frameworkInternals ??= components.Where(c => c.IsFrameworkInternal).Select(c => c.InstanceId).ToHashSet();
        return _frameworkInternals.Contains(instanceId);
    }

    public T? GetOptions<T>() where T : class => typeof(T) == typeof(BevToolsDiagnosticsOptions) ? session.Options.Diagnostics as T
        : typeof(T) == typeof(BevToolsOptions) ? session.Options as T
        : null;
}

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;

namespace Bit.Blex;

/// <summary>
/// Blex's tracing and metrics, through the standard <see cref="System.Diagnostics.ActivitySource"/> and
/// <see cref="System.Diagnostics.Metrics.Meter"/> APIs: subscribe with OpenTelemetry
/// (<c>.AddSource(BlexDiagnostics.SourceName)</c>, <c>.AddMeter(BlexDiagnostics.SourceName)</c>), dotnet-counters
/// or any <see cref="ActivityListener"/>/<see cref="MeterListener"/>. With nothing listening, an action pays two
/// flag reads.
/// </summary>
/// <remarks>
/// <para>
/// Every action, effect and query fetch is a span (<c>"{store}/{action}"</c>, <c>"query {resource}"</c>);
/// one that runs inside another -- an effect calling an action -- is its child. A failed one is marked
/// <see cref="ActivityStatusCode.Error"/> with the exception's type; a cancelled one is tagged
/// <c>blex.cancelled</c>, not marked failed. An effect's retries are events on its span.
/// </para>
/// <para>
/// Metrics: <c>blex.actions</c>, <c>blex.action.errors</c> and <c>blex.action.duration</c> (seconds), tagged with
/// <c>blex.store</c> and <c>blex.action</c>; <c>blex.query.fetches</c> and <c>blex.query.duration</c>, tagged with
/// <c>blex.query.resource</c> and <c>blex.query.outcome</c>.
/// </para>
/// <para>
/// What is recorded is chosen to be safe to export: store and action names, never action arguments or state.
/// A store's metric tag is its declared name, without a scoped instance's key, so it stays low in
/// cardinality; spans add the key as <c>blex.store.instance</c>. A query is named by its key's first part
/// (<c>["todos", 42]</c> is <c>"todos"</c>) -- keep that part a fixed word, not an id or an address.
/// </para>
/// </remarks>
public static class BlexDiagnostics
{
    /// <summary>The name of Blex's <see cref="System.Diagnostics.ActivitySource"/> and <see cref="System.Diagnostics.Metrics.Meter"/>.</summary>
    public const string SourceName = "Bit.Blex";

    private static readonly string? Version = typeof(BlexDiagnostics).Assembly.GetName().Version?.ToString();

    internal static readonly ActivitySource ActivitySource = new(SourceName, Version);

    private static readonly Meter Meter = new(SourceName, Version);

    private static readonly Counter<long> Actions =
        Meter.CreateCounter<long>("blex.actions", "{action}", "Actions and effects that ran, by store and action.");

    private static readonly Counter<long> ActionErrors =
        Meter.CreateCounter<long>("blex.action.errors", "{action}", "Actions and effects whose body threw, by store and action.");

    private static readonly Histogram<double> ActionDuration = CreateDuration(
        "blex.action.duration", "How long an action's body ran - an async one's included its awaits.");

    private static readonly Counter<long> QueryFetches =
        Meter.CreateCounter<long>("blex.query.fetches", "{fetch}", "Query fetches that settled, by resource and outcome.");

    private static readonly Histogram<double> QueryDuration = CreateDuration(
        "blex.query.duration", "How long a query fetch took, its retries included.");

    private static Histogram<double> CreateDuration(string name, string description)
    {
#if NET9_0_OR_GREATER
        // UI actions run in microseconds; effects and fetches in hundreds of milliseconds.
        return Meter.CreateHistogram<double>(name, "s", description, tags: null, advice: new InstrumentAdvice<double>
        {
            HistogramBucketBoundaries = [0.0001, 0.0005, 0.001, 0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10, 30],
        });
#else
        return Meter.CreateHistogram<double>(name, "s", description);
#endif
    }

    /// <summary>Whether anything listens to actions -- spans or metrics. The inlined check on every action's path.</summary>
    internal static bool ActionsObserved
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ActivitySource.HasListeners() || Actions.Enabled || ActionDuration.Enabled || ActionErrors.Enabled;
    }

    /// <summary>Starts measuring an action of <paramref name="store"/>; <c>default</c> when nothing listens.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ActionScope StartAction(BlexStoreBase store, string actionName)
        => ActionsObserved ? StartObservedAction(store, actionName) : default;

    // Every call below that reaches a listener -- ActivityListener sampling and stop callbacks, MeterListener
    // measurement callbacks -- reaches user code. Telemetry must never break an action or wedge a fetch, so what
    // a listener throws is swallowed here: the measurement is lost, the app goes on.

    private static ActionScope StartObservedAction(BlexStoreBase store, string actionName)
    {
        Activity? activity = null;
        try
        {
            activity = ActivitySource.HasListeners()
                ? ActivitySource.StartActivity(store.DeclaredName + "/" + actionName, ActivityKind.Internal)
                : null;

            if (activity is null && !Actions.Enabled && !ActionDuration.Enabled && !ActionErrors.Enabled)
                return default;

            if (activity is { IsAllDataRequested: true })
            {
                activity.SetTag("blex.store", store.DeclaredName);
                activity.SetTag("blex.action", actionName);
                if (store.InstanceKey is { } key)
                    activity.SetTag("blex.store.instance", key);
            }

            return new ActionScope(activity, store.DeclaredName, actionName, Stopwatch.GetTimestamp());
        }
        catch
        {
            Dispose(activity);
            return default;
        }
    }

    private static void Dispose(Activity? activity)
    {
        try
        {
            activity?.Dispose();
        }
        catch
        {
            // A throwing ActivityStopped listener; nothing left to measure.
        }
    }

    /// <summary>One action being measured. A <c>default</c> scope measures nothing.</summary>
    internal readonly struct ActionScope(Activity? activity, string? store, string? action, long started)
    {
        private bool Active => store is not null;

        /// <summary>Marks the action failed, or cancelled when the exception is a cancellation.</summary>
        public void Fail(Exception exception)
        {
            if (Active)
                FailCore(exception);
        }

        private void FailCore(Exception exception)
        {
            if (exception is OperationCanceledException)
            {
                activity?.SetTag("blex.cancelled", true);
                return;
            }

            if (activity is not null)
            {
                activity.SetStatus(ActivityStatusCode.Error, exception.Message);
                activity.SetTag("error.type", exception.GetType().FullName);
            }

            try
            {
                ActionErrors.Add(1, Tags());
            }
            catch
            {
                // A throwing MeterListener; see the note above.
            }
        }

        /// <summary>Ends the measurement: counts the action, records its duration and stops its span.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Stop()
        {
            if (Active)
                StopCore();
        }

        private void StopCore()
        {
            try
            {
                var tags = Tags();
                Actions.Add(1, tags);
                if (ActionDuration.Enabled)
                    ActionDuration.Record(Stopwatch.GetElapsedTime(started).TotalSeconds, tags);
            }
            catch
            {
                // A throwing MeterListener; see the note above.
            }
            finally
            {
                Dispose(activity);
            }
        }

        private TagList Tags() => new() { { "blex.store", store }, { "blex.action", action } };
    }

    /// <summary>
    /// Counts a direct assignment made while nothing observes actions -- a <c>"Set X"</c> that runs no pipeline
    /// and has no body to time -- so <c>blex.actions</c> counts the same changes whether or not middleware is
    /// attached.
    /// </summary>
    internal static void CountUnrecordedSet(BlexStoreBase store, string? propertyName)
    {
        if (!Actions.Enabled)
            return;

        try
        {
            Actions.Add(1, new TagList { { "blex.store", store.DeclaredName }, { "blex.action", "Set " + propertyName } });
        }
        catch
        {
            // A throwing MeterListener; see the note above.
        }
    }

    /// <summary>Records a retry of the running effect as an event on its span.</summary>
    internal static void EffectRetry(int attempt, Exception failure, TimeSpan delay)
    {
        if (Activity.Current is not { IsAllDataRequested: true } activity || activity.Source != ActivitySource)
            return;

        activity.AddEvent(new ActivityEvent("blex.effect.retry", tags: new ActivityTagsCollection
        {
            { "blex.attempt", attempt },
            { "error.type", failure.GetType().FullName },
            { "blex.retry.delay_ms", delay.TotalMilliseconds },
        }));
    }

    /// <summary>Starts measuring a query fetch; <c>default</c> when nothing listens.</summary>
    internal static QueryScope StartQuery(Query.BlexQueryKey key)
    {
        if (!ActivitySource.HasListeners() && !QueryFetches.Enabled && !QueryDuration.Enabled)
            return default;

        Activity? activity = null;
        try
        {
            activity = ActivitySource.HasListeners()
                ? ActivitySource.StartActivity("query " + ResourceOf(key), ActivityKind.Client)
                : null;

            activity?.SetTag("blex.query.resource", ResourceOf(key));
            return new QueryScope(activity, ResourceOf(key), Stopwatch.GetTimestamp());
        }
        catch
        {
            Dispose(activity);
            return default;
        }
    }

    /// <summary>A query's first key part as text: the resource it names, without the ids after it.</summary>
    private static string ResourceOf(Query.BlexQueryKey key)
        => key[0] switch
        {
            string text => text,
            null => "null",
            var other => other.GetType().Name,
        };

    /// <summary>One query fetch being measured. A <c>default</c> scope measures nothing.</summary>
    internal readonly struct QueryScope(Activity? activity, string? resource, long started)
    {
        /// <summary>Ends the measurement with its outcome: <c>success</c>, <c>error</c> or <c>cancelled</c>.</summary>
        public void Stop(string outcome, Exception? error = null)
        {
            if (resource is null)
                return;

            try
            {
                var tags = new TagList { { "blex.query.resource", resource }, { "blex.query.outcome", outcome } };
                QueryFetches.Add(1, tags);
                if (QueryDuration.Enabled)
                    QueryDuration.Record(Stopwatch.GetElapsedTime(started).TotalSeconds, tags);
            }
            catch
            {
                // A throwing MeterListener; see the note above.
            }

            if (activity is not null)
            {
                activity.SetTag("blex.query.outcome", outcome);
                if (error is not null)
                {
                    activity.SetStatus(ActivityStatusCode.Error, error.Message);
                    activity.SetTag("error.type", error.GetType().FullName);
                }

                Dispose(activity);
            }
        }
    }
}

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;

namespace Bit.Balette;

/// <summary>
/// The names under which the palette reports metrics and traces through <c>System.Diagnostics</c>, for an OpenTelemetry
/// or Bit.Belemetry setup to listen to: <c>options.Metrics.Meters.Add(BaletteDiagnostics.MeterName)</c>. Nothing is
/// recorded unless something listens.
/// </summary>
/// <remarks>
/// Instruments - every one tagged with low-cardinality values only:
/// <list type="bullet">
/// <item><c>balette.command.executions</c> (counter): <c>balette.command.kind</c>, <c>balette.trigger</c> (<c>palette</c>,
/// <c>shortcut</c>, <c>programmatic</c>) and <c>balette.outcome</c> (<c>succeeded</c>, <c>failed</c>, <c>cancelled</c>, <c>refused</c>).</item>
/// <item><c>balette.command.duration</c> (histogram, seconds): how long handlers took, tagged as above (refusals aside).</item>
/// <item><c>balette.source.duration</c> (histogram, seconds): how long async sources took, with <c>balette.source.name</c>
/// and <c>balette.outcome</c> (<c>succeeded</c>, <c>failed</c>, <c>timed_out</c>).</item>
/// <item><c>balette.search.duration</c> (histogram, seconds): how long ranking the in-memory commands took for a query.</item>
/// </list>
/// Traces: a <c>balette.command</c> activity around every handler, and a <c>balette.source</c> activity around every source
/// search; a failure records the exception's type, never its message. Queries, prompt values, titles and selected
/// commands are never recorded; command ids are recorded on activities unless
/// <see cref="BaletteOptions.RecordCommandIdsInTraces"/> is off. The palette's own rows (pin, retry, "Show more") are not
/// counted.
/// </remarks>
public static class BaletteDiagnostics
{
    /// <summary>The name of the palette's <see cref="System.Diagnostics.Metrics.Meter"/>.</summary>
    public const string MeterName = "Bit.Balette";

    /// <summary>The name of the palette's <see cref="System.Diagnostics.ActivitySource"/>.</summary>
    public const string ActivitySourceName = "Bit.Balette";

    private static readonly string? Version = typeof(BaletteDiagnostics).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

    internal static readonly ActivitySource Source = new(ActivitySourceName, Version);

    private static readonly Meter Meter = new(MeterName, Version);

    private static readonly Counter<long> Executions = Meter.CreateCounter<long>(
        "balette.command.executions", "{execution}", "Commands the palette ran, refused, or saw fail or be cancelled.");

    private static readonly Histogram<double> ExecutionDuration = Meter.CreateHistogram<double>(
        "balette.command.duration", "s", "How long command handlers took.");

    private static readonly Histogram<double> SourceDuration = Meter.CreateHistogram<double>(
        "balette.source.duration", "s", "How long async sources took to answer.");

    private static readonly Histogram<double> SearchDuration = Meter.CreateHistogram<double>(
        "balette.search.duration", "s", "How long matching and ranking the in-memory commands took for a query.");

    internal const string Succeeded = "succeeded";
    internal const string Failed = "failed";
    internal const string Cancelled = "cancelled";
    internal const string Refused = "refused";
    internal const string TimedOut = "timed_out";

    internal static string TriggerName(BaletteExecutionTrigger trigger) => trigger switch
    {
        BaletteExecutionTrigger.Shortcut => "shortcut",
        BaletteExecutionTrigger.Programmatic => "programmatic",
        _ => "palette",
    };

    internal static string KindName(BaletteCommandKind kind) => kind switch
    {
        BaletteCommandKind.Action => "action",
        BaletteCommandKind.Navigation => "navigation",
        BaletteCommandKind.Toggle => "toggle",
        BaletteCommandKind.Setting => "setting",
        BaletteCommandKind.Search => "search",
        BaletteCommandKind.Page => "page",
        BaletteCommandKind.Help => "help",
        BaletteCommandKind.Recent => "recent",
        _ => "other",
    };

    /// <summary>Starts the activity around a handler, or returns null when nothing listens.</summary>
    internal static Activity? StartCommand(BaletteCommand command, BaletteExecutionTrigger trigger, bool recordId)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("balette.command");
        if (activity is { IsAllDataRequested: true })
        {
            if (recordId)
            {
                activity.SetTag("balette.command.id", command.Id);
            }

            activity.SetTag("balette.command.kind", KindName(command.Kind));
            activity.SetTag("balette.trigger", TriggerName(trigger));
        }

        return activity;
    }

    /// <summary>Records how a command ended - on the instruments, and on its activity, which it stops.</summary>
    internal static void CommandEnded(Activity? activity, BaletteCommand command, BaletteExecutionTrigger trigger, string outcome, TimeSpan? duration, Exception? exception = null)
    {
        if (activity is not null)
        {
            activity.SetTag("balette.outcome", outcome);
            if (exception is not null)
            {
                // The type only: an exception's message - and its stack's arguments - can carry what the user typed.
                var type = exception.GetType().FullName;
                activity.SetStatus(ActivityStatusCode.Error, type);
                activity.SetTag("exception.type", type);
            }

            activity.Dispose();
        }

        if (!Executions.Enabled && !ExecutionDuration.Enabled)
        {
            return;
        }

        var tags = new TagList
        {
            { "balette.command.kind", KindName(command.Kind) },
            { "balette.trigger", TriggerName(trigger) },
            { "balette.outcome", outcome },
        };
        Executions.Add(1, tags);
        if (duration is { } d)
        {
            ExecutionDuration.Record(d.TotalSeconds, tags);
        }
    }

    /// <summary>Starts the activity around a source search, or returns null when nothing listens.</summary>
    internal static Activity? StartSource(IBaletteSource source)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("balette.source");
        activity?.SetTag("balette.source.name", source.Name);
        return activity;
    }

    /// <summary>Records how a source search ended; one the palette abandoned (overtaken, closed) only ends its activity.</summary>
    internal static void SourceEnded(Activity? activity, IBaletteSource source, string outcome, TimeSpan duration, Exception? exception = null)
    {
        if (activity is not null)
        {
            activity.SetTag("balette.outcome", outcome);
            if (outcome is not Succeeded and not Cancelled)
            {
                var type = exception?.GetType().FullName;
                activity.SetStatus(ActivityStatusCode.Error, type ?? outcome);
                if (type is not null)
                {
                    activity.SetTag("exception.type", type);
                }
            }

            activity.Dispose();
        }

        // Abandoned - overtaken by a newer query, or the palette closed - it says nothing about how fast the source is.
        if (SourceDuration.Enabled && outcome != Cancelled)
        {
            SourceDuration.Record(duration.TotalSeconds, new TagList { { "balette.source.name", source.Name }, { "balette.outcome", outcome } });
        }
    }

    internal static void SearchEnded(TimeSpan duration)
    {
        if (SearchDuration.Enabled)
        {
            SearchDuration.Record(duration.TotalSeconds);
        }
    }
}

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;

namespace Bit.BlazorUI;

/// <summary>
/// The names under which the palette reports metrics and traces through <c>System.Diagnostics</c>, for an OpenTelemetry
/// or Bit.Belemetry setup to listen to: <c>options.Metrics.Meters.Add(BitCommandPaletteDiagnostics.MeterName)</c>. Nothing is
/// recorded unless something listens.
/// </summary>
/// <remarks>
/// Instruments - every one tagged with low-cardinality values only:
/// <list type="bullet">
/// <item><c>bit.command_palette.command.executions</c> (counter): <c>bit.command_palette.command.kind</c>, <c>bit.command_palette.trigger</c> (<c>palette</c>,
/// <c>shortcut</c>, <c>programmatic</c>) and <c>bit.command_palette.outcome</c> (<c>succeeded</c>, <c>failed</c>, <c>cancelled</c>, <c>refused</c>).</item>
/// <item><c>bit.command_palette.command.duration</c> (histogram, seconds): how long handlers took, tagged as above (refusals aside).</item>
/// <item><c>bit.command_palette.source.duration</c> (histogram, seconds): how long async sources took, with <c>bit.command_palette.source.name</c>
/// and <c>bit.command_palette.outcome</c> (<c>succeeded</c>, <c>failed</c>, <c>timed_out</c>).</item>
/// <item><c>bit.command_palette.search.duration</c> (histogram, seconds): how long ranking the in-memory commands took for a query.</item>
/// </list>
/// Traces: a <c>bit.command_palette.command</c> activity around every handler, and a <c>bit.command_palette.source</c> activity around every source
/// search; a failure records the exception's type, never its message. Queries, prompt values, titles and selected
/// commands are never recorded; command ids are recorded on activities unless
/// <see cref="BitCommandPaletteOptions.RecordCommandIdsInTraces"/> is off. The palette's own rows (pin, retry, "Show more") are not
/// counted.
/// </remarks>
public static class BitCommandPaletteDiagnostics
{
    /// <summary>The name of the palette's <see cref="System.Diagnostics.Metrics.Meter"/>.</summary>
    public const string MeterName = "Bit.BlazorUI.CommandPalette";

    /// <summary>The name of the palette's <see cref="System.Diagnostics.ActivitySource"/>.</summary>
    public const string ActivitySourceName = "Bit.BlazorUI.CommandPalette";

    private static readonly string? Version = typeof(BitCommandPaletteDiagnostics).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

    internal static readonly ActivitySource Source = new(ActivitySourceName, Version);

    private static readonly Meter Meter = new(MeterName, Version);

    private static readonly Counter<long> Executions = Meter.CreateCounter<long>(
        "bit.command_palette.command.executions", "{execution}", "Commands the palette ran, refused, or saw fail or be cancelled.");

    private static readonly Histogram<double> ExecutionDuration = Meter.CreateHistogram<double>(
        "bit.command_palette.command.duration", "s", "How long command handlers took.");

    private static readonly Histogram<double> SourceDuration = Meter.CreateHistogram<double>(
        "bit.command_palette.source.duration", "s", "How long async sources took to answer.");

    private static readonly Histogram<double> SearchDuration = Meter.CreateHistogram<double>(
        "bit.command_palette.search.duration", "s", "How long matching and ranking the in-memory commands took for a query.");

    internal const string Succeeded = "succeeded";
    internal const string Failed = "failed";
    internal const string Cancelled = "cancelled";
    internal const string Refused = "refused";
    internal const string TimedOut = "timed_out";

    internal static string TriggerName(BitCommandPaletteExecutionTrigger trigger) => trigger switch
    {
        BitCommandPaletteExecutionTrigger.Shortcut => "shortcut",
        BitCommandPaletteExecutionTrigger.Programmatic => "programmatic",
        _ => "palette",
    };

    internal static string KindName(BitCommandPaletteCommandKind kind) => kind switch
    {
        BitCommandPaletteCommandKind.Action => "action",
        BitCommandPaletteCommandKind.Navigation => "navigation",
        BitCommandPaletteCommandKind.Toggle => "toggle",
        BitCommandPaletteCommandKind.Setting => "setting",
        BitCommandPaletteCommandKind.Search => "search",
        BitCommandPaletteCommandKind.Page => "page",
        BitCommandPaletteCommandKind.Help => "help",
        BitCommandPaletteCommandKind.Recent => "recent",
        _ => "other",
    };

    /// <summary>Starts the activity around a handler, or returns null when nothing listens.</summary>
    internal static Activity? StartCommand(BitCommandPaletteCommand command, BitCommandPaletteExecutionTrigger trigger, bool recordId)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("bit.command_palette.command");
        if (activity is { IsAllDataRequested: true })
        {
            if (recordId)
            {
                activity.SetTag("bit.command_palette.command.id", command.Id);
            }

            activity.SetTag("bit.command_palette.command.kind", KindName(command.Kind));
            activity.SetTag("bit.command_palette.trigger", TriggerName(trigger));
        }

        return activity;
    }

    /// <summary>Records how a command ended - on the instruments, and on its activity, which it stops.</summary>
    internal static void CommandEnded(Activity? activity, BitCommandPaletteCommand command, BitCommandPaletteExecutionTrigger trigger, string outcome, TimeSpan? duration, Exception? exception = null)
    {
        if (activity is not null)
        {
            activity.SetTag("bit.command_palette.outcome", outcome);
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
            { "bit.command_palette.command.kind", KindName(command.Kind) },
            { "bit.command_palette.trigger", TriggerName(trigger) },
            { "bit.command_palette.outcome", outcome },
        };
        Executions.Add(1, tags);
        if (duration is { } d)
        {
            ExecutionDuration.Record(d.TotalSeconds, tags);
        }
    }

    /// <summary>Starts the activity around a source search, or returns null when nothing listens.</summary>
    internal static Activity? StartSource(IBitCommandPaletteSource source)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("bit.command_palette.source");
        activity?.SetTag("bit.command_palette.source.name", source.Name);
        return activity;
    }

    /// <summary>Records how a source search ended; one the palette abandoned (overtaken, closed) only ends its activity.</summary>
    internal static void SourceEnded(Activity? activity, IBitCommandPaletteSource source, string outcome, TimeSpan duration, Exception? exception = null)
    {
        if (activity is not null)
        {
            activity.SetTag("bit.command_palette.outcome", outcome);
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
            SourceDuration.Record(duration.TotalSeconds, new TagList { { "bit.command_palette.source.name", source.Name }, { "bit.command_palette.outcome", outcome } });
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

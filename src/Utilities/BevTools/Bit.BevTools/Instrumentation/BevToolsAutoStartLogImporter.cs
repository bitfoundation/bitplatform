using System.Text;
using Microsoft.Extensions.Logging;

namespace Bit.BevTools;

/// <summary>
/// Takes over, into a session attached at runtime, the log entries Bit.BevTools.Loader captured before it existed
/// (<c>BevToolsLoaderOptions.AutoStart</c>). Each entry becomes the record <see cref="BevToolsLoggerProvider"/> would have
/// made of it, read now through this session's inspector - its redaction and value access, which did not exist when it
/// was logged - from what the loader kept: values as themselves when immutable, otherwise as their type and text
/// (<see cref="BevToolsObjectInspector.DescribeDetached"/>). What only a session can know at the moment of logging - the
/// component on the stack, the UI event running, a timeline row, an entry in Errors - is left out, never guessed.
/// <para>
/// Two steps, so that the loader stops keeping entries and the session's own provider starts recording them as close
/// together as possible: <see cref="Take"/> takes the buffer and reserves the records' ids, the caller then lets the
/// session's provider record, and <see cref="Complete"/> does the slow part - reading every entry - and puts the
/// records ahead of whatever the session recorded meanwhile, in the order of their ids.
/// </para>
/// </summary>
internal static class BevToolsAutoStartLogImporter
{
    /// <summary>
    /// Takes what the loader kept for the session's scope, which stops it keeping more. Null when the loader kept
    /// nothing because the app did not ask it to; otherwise a handover that <see cref="Complete"/> finishes, with nothing
    /// in it when there was nothing to take.
    /// </summary>
    public static Handover? Take(BevToolsSession session, BevToolsLoaderContext? loader)
    {
        if (loader is null)
        {
            return null;
        }

        if (loader.AutoStartLogs is not { } logs)
        {
            return loader.RegisteredAtStartup || (loader.Options.AutoStart & BevToolsAutoStart.Logs) == 0
                ? null
                : new Handover("Bit.BevTools.Loader did not start capturing them.");
        }

        try
        {
            var entries = logs.Take(session.Services, out var buffer, out var counts, out var whyNone);
            if (entries is null)
            {
                return new Handover("Bit.BevTools.Loader kept none for this session: " + whyNone + ".");
            }

            var options = session.Options;
            if (!options.TrackLogs)
            {
                return new Handover("Bit.BevTools.Loader kept " + entries.Length + " " + buffer!.Owner + ", and this session took none over: BevToolsOptions.TrackLogs is off.");
            }

            // The session may record from a stricter level than the loader captured at (an attach's own configure).
            entries = Array.FindAll(entries, e => e.Level >= options.MinimumLogLevel);
            for (var level = 0; level < counts.Length; level++)
            {
                if ((LogLevel)level < options.MinimumLogLevel)
                {
                    counts[level] = 0;
                }
            }

            return new Handover(entries, counts, buffer!, session.Logs.ReserveIds(entries.Length));
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return new Handover("Taking them over failed: " + ex.GetType().Name + ": " + BevToolsRedactor.RedactText(ex.Message));
        }
    }

    /// <summary>
    /// Reads the entries taken into records and adds them to the session ahead of what it recorded since
    /// <see cref="Take"/>, and returns what the capability report says about them.
    /// </summary>
    public static string? Complete(BevToolsSession session, Handover? handover, out bool handedOver)
    {
        handedOver = false;
        if (handover is null)
        {
            return null;
        }

        if (handover.Note is { } note)
        {
            return note;
        }

        var (entries, counts, buffer) = (handover.Entries!, handover.Counts!, handover.Buffer!);
        try
        {
            List<BevToolsLogRecord> records = new(entries.Length);
            for (var i = 0; i < entries.Length; i++)
            {
                records.Add(Read(session, entries[i], handover.FirstId + i));
            }

            session.Logs.Import(records, counts);
            handedOver = true;
            var total = counts.Sum();
            var dropped = Math.Max(0, total - records.Count);
            if (total > 0)
            {
                session.Timeline.Record(new BevToolsEvent
                {
                    Kind = BevToolsEventKind.Log,
                    Category = "log",
                    Title = Plural(records.Count, "log entry", "log entries") + " from before BevTools was attached" + (dropped > 0 ? " (" + dropped + " older not kept)" : ""),
                    Detail = "Captured by Bit.BevTools.Loader " + buffer.Owner + (buffer.Since is { } since ? " from " + since.ToLocalTime().ToString("HH:mm:ss") : "") + " (BevToolsLoaderOptions.AutoStart); in Logs.",
                    Severity = BevToolsSeverity.Info,
                    Data = new Dictionary<string, object?>
                    {
                        ["entries"] = records.Count,
                        ["notKept"] = dropped,
                    },
                });
            }

            return "Bit.BevTools.Loader captured them " + buffer.Owner + (buffer.Since is { } from ? " from " + from.ToString("u") : "") + " (BevToolsLoaderOptions.AutoStart) and handed "
                + Plural(records.Count, "entry", "entries") + " to this session" + (dropped > 0 ? "; " + dropped + " older ones were counted, not kept (MaxLogs)" : "")
                + ". They are read at this session's value access and redacted like any entry, without the component that logged them, the UI event running, a timeline row of their own or an Errors entry: knowing those takes the session that did not exist yet.";
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Run from the panel's render on a circuit: a failure costs the entries taken, never the panel.
            return "Bit.BevTools.Loader kept " + entries.Length + " " + buffer.Owner + ", and reading them failed: " + ex.GetType().Name + ": " + BevToolsRedactor.RedactText(ex.Message);
        }
    }

    private static BevToolsLogRecord Read(BevToolsSession session, BevToolsAutoStartLogEntry entry, long id)
    {
        var inspector = session.Inspector;
        // An entry whose values the loader did not read is shown as under None, whatever the session reads: there is
        // nothing more to show, and an empty message is not what was logged.
        var access = entry.ValuesRead ? inspector.ValueAccess : BevToolsValueAccess.None;
        List<KeyValuePair<string, string>> properties = [];
        List<(string Name, object? Value, BevToolsInspectedNode Node)>? values = null;
        // The rules of BevToolsLoggerProvider.Read: below Full, or with values past the ones read, the logger's own text
        // holds more than may be shown, so the message is rebuilt from what the properties show.
        var rebuild = access != BevToolsValueAccess.Full || entry.ValuesTruncated;
        foreach (var value in entry.Values)
        {
            var node = Describe(inspector, value, entry.ValuesRead);
            if (properties.Count < BevToolsLoggerProvider.MaxProperties)
            {
                properties.Add(new(value.Name, node.Display));
            }

            (values ??= []).Add((value.Name, value.Value, node));
            rebuild |= node.Kind == BevToolsNodeKind.Redacted;
        }

        var template = entry.Template is null || (access == BevToolsValueAccess.None && values is null) ? null : BevToolsRedactor.RedactText(entry.Template);
        string? message = access == BevToolsValueAccess.None
            ? null
            : rebuild
                ? template is not null ? BevToolsLoggerProvider.Render(template, values) : BevToolsLoggerProvider.RenderWithoutTemplate(values)
                : BevToolsRedactor.RedactText(entry.Formatted ?? "");

        return new BevToolsLogRecord
        {
            Id = id,
            At = entry.At,
            Level = entry.Level,
            Category = BevToolsRedactor.RedactText(entry.Category),
            EventId = entry.EventId,
            EventName = entry.EventName is { } eventName ? BevToolsRedactor.RedactText(eventName) : null,
            Message = message is null ? null : BevToolsLoggerProvider.Shorten(message, BevToolsLoggerProvider.MaxMessageLength),
            MessageTemplate = template is null ? null : BevToolsLoggerProvider.Shorten(template, BevToolsLoggerProvider.MaxMessageLength),
            Properties = properties,
            Scopes = ReadScopes(inspector, entry.Scopes, access, entry.ValuesRead),
            ExceptionType = entry.ExceptionType,
            ExceptionMessage = entry.ExceptionMessages is { } exception ? DescribeException(exception) : null,
            TraceId = entry.TraceId,
            SpanId = entry.SpanId,
            // EF Core writes the SQL and the elapsed time as strings, which the loader kept as themselves when it read values.
            DbCommand = BevToolsDbCommands.IsCommand(entry.Category, entry.EventId)
                ? BevToolsDbCommands.Read(entry.EventId, entry.Values.Select(v => new KeyValuePair<string, object?>(v.Name, v.Value)).ToList(), access)
                : null,
            BeforeAttach = true,
        };
    }

    private static BevToolsInspectedNode Describe(BevToolsObjectInspector inspector, BevToolsAutoStartLogValue value, bool valuesRead) =>
        !valuesRead ? inspector.DescribeUnread(value.Name, value.ObjectType)
        : value.ObjectType is { } type ? inspector.DescribeDetached(value.Name, type, value.Text, value.Count)
        : inspector.Describe(value.Name, value.Value);

    /// <summary>Each scope as one line, by the rules of the provider's <c>DescribeScope</c>.</summary>
    private static List<string> ReadScopes(BevToolsObjectInspector inspector, IReadOnlyList<BevToolsAutoStartLogScope> scopes, BevToolsValueAccess access, bool valuesRead)
    {
        List<string> lines = [];
        foreach (var scope in scopes.Take(BevToolsLoggerProvider.MaxScopes))
        {
            string line;
            if (scope.Pairs is { } pairs)
            {
                var sb = new StringBuilder();
                foreach (var pair in pairs)
                {
                    if (pair.Name == "{OriginalFormat}")
                    {
                        continue;
                    }

                    if (sb.Length > 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(pair.Name);
                    if (access != BevToolsValueAccess.None)
                    {
                        sb.Append('=').Append(BevToolsLoggerProvider.Unquoted(Describe(inspector, pair, valuesRead)));
                    }
                }

                line = sb.ToString();
            }
            else if (access == BevToolsValueAccess.None)
            {
                line = scope.IsString ? "string" : scope.Type is { } type ? BevToolsTypeNames.Short(type) : "";
            }
            else if (scope.IsString || (access == BevToolsValueAccess.Full && (scope.Type is null || inspector.MayShowToString(scope.Type))))
            {
                line = BevToolsRedactor.RedactText(scope.Text ?? "");
            }
            else
            {
                line = scope.Type is { } type ? BevToolsTypeNames.Short(type) : "";
            }

            lines.Add(BevToolsLoggerProvider.Shorten(line, BevToolsLoggerProvider.MaxScopeLength));
        }

        return lines;
    }

    private static string DescribeException(IReadOnlyList<(string? Type, string? Message)> messages)
    {
        var sb = new StringBuilder();
        foreach (var (type, message) in messages)
        {
            if (sb.Length > 0 || type is not null)
            {
                sb.Append('\n').Append(type).Append(": ");
            }

            // A message the loader did not read (the app's code, kept nowhere) is hidden, as an unread value is.
            sb.Append(message is null ? BevToolsRedactor.HiddenValue : BevToolsRedactor.RedactText(message));
        }

        return BevToolsLoggerProvider.Shorten(sb.ToString(), BevToolsLoggerProvider.MaxMessageLength);
    }

    private static string Plural(int count, string one, string many) => count + " " + (count == 1 ? one : many);

    /// <summary>What <see cref="Take"/> took: the entries and the ids reserved for them, or only what to say about them.</summary>
    internal sealed class Handover
    {
        public Handover(string note) => Note = note;

        public Handover(BevToolsAutoStartLogEntry[] entries, int[] counts, BevToolsAutoStartLogBuffer buffer, long firstId)
        {
            Entries = entries;
            Counts = counts;
            Buffer = buffer;
            FirstId = firstId;
        }

        public string? Note { get; }

        public BevToolsAutoStartLogEntry[]? Entries { get; }

        public int[]? Counts { get; }

        public BevToolsAutoStartLogBuffer? Buffer { get; }

        public long FirstId { get; }
    }
}

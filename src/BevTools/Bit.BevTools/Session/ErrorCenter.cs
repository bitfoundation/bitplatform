using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Bit.BevTools;

/// <summary>Aggregates errors from every source. Repeats within a minute collapse into one record with a count.</summary>
internal sealed partial class ErrorCenter
{
    private readonly BevToolsSession _session;
    private readonly RingBuffer<BevErrorRecord> _errors;
    private readonly ConditionalWeakTable<Exception, BevErrorRecord> _byException = new();
    private readonly object _lock = new();
    private long _nextId;
    private int _total;

    public ErrorCenter(BevToolsSession session, BitBevToolsOptions options)
    {
        _session = session;
        _errors = new RingBuffer<BevErrorRecord>(options.MaxErrors);
    }

    public int Count => _errors.Count;

    /// <summary>Total occurrences including collapsed repeats.</summary>
    public int TotalOccurrences => Volatile.Read(ref _total);

    public BevErrorRecord Record(
        Exception? exception,
        string source,
        string? message = null,
        BevComponentRecord? component = null,
        long? precedingEventId = null,
        string? exceptionType = null,
        string? stackTrace = null)
    {
        // One exception can reach BevTools through two capture paths: the render wrapper records it, then the
        // ErrorBoundary or renderer that handles it logs it. That is one error, not two.
        if (exception is not null && _byException.TryGetValue(exception, out var known))
        {
            return known;
        }

        exceptionType ??= exception?.GetType().FullName ?? "Error";
        message ??= exception?.Message ?? "Unknown error";
        stackTrace ??= exception?.StackTrace;
        var location = FindSourceLocation(stackTrace);
        var fingerprint = string.Concat(source, "|", exceptionType, "|", message, "|", location ?? FirstLine(stackTrace), "|", component?.InstanceId);
        var now = DateTimeOffset.UtcNow;
        component?.AddError();

        lock (_lock)
        {
            _total++;
            var existing = _errors.FindLast(e => e.Fingerprint == fingerprint);
            if (existing is not null && now - existing.LastSeen < TimeSpan.FromMinutes(1))
            {
                existing.Count++;
                existing.LastSeen = now;
                Remember(exception, existing);
                RecordTimeline(existing, repeat: true);
                _session.Touch();
                return existing;
            }

            var record = new BevErrorRecord
            {
                Id = Interlocked.Increment(ref _nextId),
                At = now,
                LastSeen = now,
                Source = source,
                ExceptionType = exceptionType,
                Message = message,
                StackTrace = stackTrace,
                SourceLocation = location,
                ComponentInstanceId = component?.InstanceId,
                ComponentName = component?.DisplayName,
                PrecedingEventId = precedingEventId ?? _session.CurrentTriggerEventId ?? (exception is null ? null : _session.TakeFailedTrigger(exceptionType)),
                Fingerprint = fingerprint,
                InnerError = Describe(exception?.InnerException),
            };
            if (_errors.Add(record, out var evicted) && evicted is not null)
            {
                _total -= evicted.Count;
            }

            Remember(exception, record);
            RecordTimeline(record, repeat: false);
            _session.Touch();
            return record;
        }
    }

    private void Remember(Exception? exception, BevErrorRecord record)
    {
        if (exception is not null)
        {
            _byException.AddOrUpdate(exception, record);
        }
    }

    private void RecordTimeline(BevErrorRecord record, bool repeat)
    {
        var id = _session.Timeline.Record(new BevToolsEvent
        {
            Kind = BevToolsEventKind.Error,
            Category = "error:" + record.Source,
            Title = (repeat ? "(repeat) " : "") + ShortTypeName(record.ExceptionType) + ": " + record.Message,
            Detail = record.SourceLocation is null ? record.Source : record.Source + " · " + record.SourceLocation,
            Severity = BevToolsSeverity.Error,
            ComponentInstanceId = record.ComponentInstanceId,
            ComponentName = record.ComponentName,
            ParentEventId = record.PrecedingEventId,
            Data = new Dictionary<string, object?> { ["errorId"] = record.Id },
        });
        if (!repeat)
        {
            record.TimelineEventId = id;
        }
    }

    public BevErrorRecord[] Snapshot() => _errors.ToArray();

    public BevErrorRecord? Find(long id) => _errors.FindByKey(static e => e.Id, id);

    public void Clear()
    {
        lock (_lock)
        {
            _errors.Clear();
            _byException.Clear();
            _total = 0;
        }

        _session.Touch();
    }

    internal static string? FindSourceLocation(string? stackTrace)
    {
        if (string.IsNullOrEmpty(stackTrace))
        {
            return null;
        }

        var match = LocationRegex().Match(stackTrace);
        if (!match.Success)
        {
            return null;
        }

        var path = match.Groups["file"].Value;
        var slash = Math.Max(path.LastIndexOf('\\'), path.LastIndexOf('/'));
        var file = slash >= 0 ? path[(slash + 1)..] : path;
        return file + ":" + match.Groups["line"].Value;
    }

    private static string? Describe(Exception? exception)
    {
        if (exception is null)
        {
            return null;
        }

        var innermost = exception;
        var hops = 0;
        while (innermost.InnerException is { } inner && hops++ < 16)
        {
            innermost = inner;
        }

        return innermost.GetType().Name + ": " + innermost.Message;
    }

    private static string? FirstLine(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        var nl = text.IndexOf('\n');
        return (nl < 0 ? text : text[..nl]).Trim();
    }

    internal static string ShortTypeName(string fullName)
    {
        var dot = fullName.LastIndexOf('.');
        return dot < 0 ? fullName : fullName[(dot + 1)..];
    }

    [GeneratedRegex(@" in (?<file>[^\r\n]+?):line (?<line>\d+)", RegexOptions.CultureInvariant)]
    private static partial Regex LocationRegex();
}

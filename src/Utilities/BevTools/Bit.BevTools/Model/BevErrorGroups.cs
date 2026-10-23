using System.Text.RegularExpressions;

namespace Bit.BevTools;

/// <summary>
/// Errors that are one problem although their messages differ in values: "Order 1042 not found" and "Order 1043 not
/// found" thrown from the same place are one group, "Order {n} not found". The values a message template replaces are
/// the ones that identify an instance rather than a kind of failure: GUIDs, timestamps, long hexadecimal strings
/// (hashes, tokens), quoted values, the ids in addresses (absolute URLs and "GET /path" alike, as the Network endpoints
/// show them; ports and hosts stay), and numbers of four digits or more. Short numbers stay - "404" and "500" are
/// different failures.
/// </summary>
public static partial class BevErrorGroups
{
    /// <summary>
    /// The message with its values replaced: <c>{guid}</c>, <c>{time}</c>, <c>{hex}</c>, <c>'…'</c>, <c>{n}</c>, addresses
    /// as their endpoint template.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="keepQuoted">
    /// Keep quoted values: when nothing in the stack tells one throw site from another, a quoted name ("service of type
    /// 'IOrderApi'") is what tells one failure from another.
    /// </param>
    public static string Template(string message, bool keepQuoted = false)
    {
        if (string.IsNullOrEmpty(message))
        {
            return message;
        }

        // Addresses are templated by the endpoint rules and set aside, so the passes below cannot touch their ports.
        var addresses = new List<string>();
        string SetAside(string templated)
        {
            addresses.Add(templated);
            return "\u0001" + (addresses.Count - 1).ToString(System.Globalization.CultureInfo.InvariantCulture) + "\u0002";
        }

        var template = UrlRegex().Replace(message, m =>
        {
            var url = m.Value.TrimEnd('.', ',', ';', ':', '!', '?');
            return SetAside(TemplateUrl(url)) + m.Value[url.Length..];
        });
        template = MethodPathRegex().Replace(template, m => SetAside(BevHttpEndpoints.Template(m.Groups["method"].Value, m.Groups["path"].Value.TrimEnd('.', ',', ';', ':', '!', '?')))
            + m.Groups["path"].Value[m.Groups["path"].Value.TrimEnd('.', ',', ';', ':', '!', '?').Length..]);
        // A host and port without a scheme ("localhost:5001", "10.0.0.7:6379") names a server, not an instance.
        template = HostPortRegex().Replace(template, m => SetAside(m.Value));
        template = GuidRegex().Replace(template, "{guid}");
        template = TimestampRegex().Replace(template, "{time}");
        template = HexRegex().Replace(template, "{hex}");
        if (!keepQuoted)
        {
            template = QuotedRegex().Replace(template, m => m.Value[0] + "…" + m.Value[0]);
        }

        template = NumberRegex().Replace(template, "{n}");
        return addresses.Count == 0 ? template : PlaceholderRegex().Replace(template, m => addresses[int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)]);
    }

    /// <summary>
    /// What makes two errors one group: where they come from, their type, their message template, and where they were
    /// thrown - the source location, else the first application frame of the stack (<see cref="StackAnchor"/>). Not the
    /// component instance: the same failure in 120 rows is one problem.
    /// </summary>
    internal static string KeyOf(string source, string exceptionType, string template, string? anchor) =>
        string.Concat(source, "|", exceptionType, "|", template, "|", anchor);

    /// <summary>
    /// The frame that says where an error was thrown when no source location does: the first frame of application code,
    /// else the first frame at all, and whether it was application code. A JavaScript stack starts with its message
    /// ("TypeError: …"), which is not a frame; .NET frames of System and Microsoft code and the framework's scripts are
    /// shared by every caller, so they are passed over while an application frame follows.
    /// </summary>
    internal static (string? Frame, bool InApplication) StackAnchor(string? stackTrace)
    {
        if (string.IsNullOrEmpty(stackTrace))
        {
            return (null, false);
        }

        string? first = null;
        foreach (var raw in stackTrace.AsSpan().EnumerateLines())
        {
            var line = raw.Trim();
            var isFrame = line.StartsWith("at ") || (line.Contains('@') && line.Contains(':'));
            if (!isFrame)
            {
                continue;
            }

            var frame = line.ToString();
            first ??= frame;
            if (!IsFrameworkFrame(frame))
            {
                return (frame, true);
            }
        }

        return (first, false);
    }

    private static bool IsFrameworkFrame(string frame) =>
        frame.StartsWith("at System.", StringComparison.Ordinal)
        || frame.StartsWith("at Microsoft.", StringComparison.Ordinal)
        || frame.Contains("/_framework/", StringComparison.Ordinal);

    /// <summary>Errors grouped by <see cref="BevErrorRecord.Group"/>, with their counts, components and time span.</summary>
    internal static List<BevErrorGroupSummary> Summarize(IEnumerable<BevErrorRecord> errors) =>
        errors.GroupBy(e => e.Group, StringComparer.Ordinal).Select(g =>
        {
            var latest = g.MaxBy(e => e.LastSeen)!;
            return new BevErrorGroupSummary(
                g.Key,
                latest.Source,
                latest.ExceptionType,
                latest.MessageTemplate,
                g.Sum(e => e.Count),
                g.Count(),
                g.Select(e => e.Message).Distinct(StringComparer.Ordinal).Count(),
                g.Where(e => e.ComponentName is not null).GroupBy(e => e.ComponentName!).OrderByDescending(c => c.Sum(e => e.Count)).Select(c => c.Key).ToList(),
                g.Min(e => e.At),
                latest.LastSeen,
                g.Select(e => e.SourceLocation).FirstOrDefault(l => l is not null),
                latest.Id);
        }).ToList();

    // The URL's path and query names as the Network panel's endpoints show them: "/api/orders/{id}?page".
    private static string TemplateUrl(string url) => BevHttpEndpoints.Template("X", url)[2..];

    [GeneratedRegex(@"https?://[^\s'""<>()\[\]]+", RegexOptions.CultureInvariant)]
    private static partial Regex UrlRegex();

    // "GET /api/orders/42": how a request is named in messages, BevTools' own included.
    [GeneratedRegex(@"\b(?<method>GET|POST|PUT|PATCH|DELETE|HEAD|OPTIONS) (?<path>/[^\s'""<>()\[\]\u0001\u0002]*)", RegexOptions.CultureInvariant)]
    private static partial Regex MethodPathRegex();

    [GeneratedRegex(@"\b(?:localhost|\d{1,3}(?:\.\d{1,3}){3}|[a-zA-Z][\w-]*(?:\.[\w-]+)+):\d{2,5}\b", RegexOptions.CultureInvariant)]
    private static partial Regex HostPortRegex();

    [GeneratedRegex(@"\u0001(\d+)\u0002", RegexOptions.CultureInvariant)]
    private static partial Regex PlaceholderRegex();

    [GeneratedRegex(@"\b[0-9a-fA-F]{8}-(?:[0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}\b", RegexOptions.CultureInvariant)]
    private static partial Regex GuidRegex();

    // ISO 8601: a date, with a time and an offset when there are.
    [GeneratedRegex(@"\b\d{4}-\d{2}-\d{2}(?:[T ]\d{2}:\d{2}(?::\d{2}(?:\.\d+)?)?(?:Z|[+-]\d{2}:?\d{2})?)?(?![\w])", RegexOptions.CultureInvariant)]
    private static partial Regex TimestampRegex();

    [GeneratedRegex(@"\b(?=[0-9a-fA-F]*[0-9])(?=[0-9a-fA-F]*[a-fA-F])[0-9a-fA-F]{16,}\b", RegexOptions.CultureInvariant)]
    private static partial Regex HexRegex();

    // Between quotes that are not apostrophes inside a word ("can't", "it's").
    [GeneratedRegex(@"(?<!\w)'[^'\r\n]{1,200}'(?!\w)|(?<!\w)""[^""\r\n]{1,200}""(?!\w)", RegexOptions.CultureInvariant)]
    private static partial Regex QuotedRegex();

    [GeneratedRegex(@"(?<![\w.{\u0001])\d{4,}(?![\w}\u0002])", RegexOptions.CultureInvariant)]
    private static partial Regex NumberRegex();
}

/// <summary>One group of errors in the buffer: every record of it, however many occurrences each counted.</summary>
internal sealed record BevErrorGroupSummary(
    string Group,
    string Source,
    string ExceptionType,
    string MessageTemplate,
    int Occurrences,
    int Records,
    int DistinctMessages,
    IReadOnlyList<string> Components,
    DateTimeOffset FirstAt,
    DateTimeOffset LastSeen,
    string? SourceLocation,
    long LatestId);

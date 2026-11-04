using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Bit.Belemetry;

/// <summary>
/// Deterministic exception grouping, independent of any backend: a fingerprint of the exception type, its message
/// template (the message with the values that identify an instance replaced) and its meaningful stack frames (the
/// first application frames, without line numbers, file paths, arguments or compiler-generated names).
/// </summary>
/// <remarks>
/// <para>
/// "Order 1042 not found" and "Order 1043 not found" thrown from the same method are one group; an
/// <see cref="InvalidOperationException"/> thrown from two different methods is two. Timestamps, GUIDs, long hexadecimal
/// strings, quoted values, numbers of four digits or more and URL path ids are replaced; short numbers stay (404 and 500
/// are different failures).
/// </para>
/// <para>
/// The algorithm is versioned (<see cref="Version"/>): a later version may group differently, and every exported group
/// says which version made its fingerprint, so a backend or a support tool never compares fingerprints across versions.
/// </para>
/// </remarks>
public static partial class BelemetryExceptionFingerprint
{
    /// <summary>The algorithm version, part of every fingerprint's input and exported with it.</summary>
    public const string Version = "1";

    /// <summary>How many meaningful frames take part.</summary>
    public const int FrameCount = 3;

    /// <summary>The fingerprint: 24 lowercase hexadecimal characters (the first 96 bits of a SHA-256).</summary>
    /// <param name="type">The exception type.</param>
    /// <param name="message">The message (already sanitized or not: only its template takes part).</param>
    /// <param name="stackTrace">The stack trace (.NET or JavaScript).</param>
    /// <param name="classification">An application classification that splits groups (optional).</param>
    /// <param name="release">The release, when groups must not span releases (<see cref="BelemetryDiagnosticsOptions.GroupAcrossReleases"/>).</param>
    public static string Compute(string? type, string? message, string? stackTrace, string? classification = null, string? release = null)
    {
        var frames = MeaningfulFrames(stackTrace);
        return FromParts(type, TemplateFor(message, frames), frames, classification, release);
    }

    /// <summary>
    /// The template that takes part for a message: without a stack nothing tells one throw site from another, so quoted
    /// names ("service of type 'IOrderApi'") are what tells one failure from another, and they stay.
    /// </summary>
    internal static string TemplateFor(string? message, IReadOnlyList<string> frames) => Template(message, keepQuoted: frames.Count == 0);

    /// <summary><see cref="Compute"/> from its parts, for a caller that has the frames and the template already.</summary>
    internal static string FromParts(string? type, string template, IReadOnlyList<string> frames, string? classification, string? release)
    {
        var key = new StringBuilder(256)
            .Append('v').Append(Version).Append('\n')
            .Append(NormalizeType(type)).Append('\n')
            .Append(template).Append('\n')
            .AppendJoin('\n', frames).Append('\n')
            .Append(classification).Append('\n')
            .Append(release)
            .ToString();

        return Hash(key);
    }

    /// <summary>The fingerprint of an application-defined grouping key (<see cref="BelemetryExceptionClassification.Fingerprint"/>).</summary>
    public static string FromKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return Hash("custom\n" + key);
    }

    /// <summary>
    /// The message with its values replaced: <c>{guid}</c>, <c>{time}</c>, <c>{hex}</c>, <c>'…'</c>, <c>{n}</c>, and URLs
    /// as their scheme, host and templated path.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="keepQuoted">Keep quoted values.</param>
    public static string Template(string? message, bool keepQuoted = false)
    {
        if (string.IsNullOrEmpty(message)) return string.Empty;

        // Long messages are cut first: the template only has to tell kinds of failures apart.
        var template = message.Length > 1024 ? message[..1024] : message;
        template = UrlRegex().Replace(template, m => BelemetryUrlTemplate.TemplateUrl(m.Value, allowedQueryParameters: null) ?? "{url}");
        template = GuidRegex().Replace(template, "{guid}");
        template = TimestampRegex().Replace(template, "{time}");
        template = HexRegex().Replace(template, "{hex}");
        if (keepQuoted is false) template = QuotedRegex().Replace(template, m => m.Value[0] + "…" + m.Value[0]);
        template = NumberRegex().Replace(template, "{n}");
        return template;
    }

    /// <summary>
    /// The frames that take part in the fingerprint: up to <see cref="FrameCount"/> application frames, normalized; the
    /// first frames of any code when there is no application frame.
    /// </summary>
    public static IReadOnlyList<string> MeaningfulFrames(string? stackTrace)
    {
        if (string.IsNullOrEmpty(stackTrace)) return [];

        var application = new List<string>(FrameCount);
        var any = new List<string>(FrameCount);
        foreach (var line in stackTrace.AsSpan().EnumerateLines())
        {
            if (NormalizeFrame(line.Trim().ToString()) is not { } frame) continue;

            if (any.Count < FrameCount) any.Add(frame);
            if (IsFrameworkFrame(frame) is false)
            {
                application.Add(frame);
                if (application.Count == FrameCount) break;
            }
        }

        return application.Count > 0 ? application : any;
    }

    /// <summary>The first meaningful frame: the first application frame, else the first frame.</summary>
    public static string? Anchor(string? stackTrace)
    {
        var frames = MeaningfulFrames(stackTrace);
        return frames.Count > 0 ? frames[0] : null;
    }

    /// <summary>
    /// A stack frame without what changes between builds or machines: .NET frames become <c>Namespace.Type.Method</c>
    /// (async state machines, lambdas and local functions named after their method; no arguments, file or line);
    /// JavaScript frames become <c>function@file.js</c> (no host, query, line or column). Null for a line that is not a frame.
    /// </summary>
    public static string? NormalizeFrame(string line)
    {
        if (line.StartsWith("at ", StringComparison.Ordinal))
        {
            var body = line[3..].Trim();

            // JavaScript (V8): "at fn (https://host/app.js?v=1:12:34)", "at https://host/app.js:12:34", "at async fn (...)".
            var paren = body.LastIndexOf(" (", StringComparison.Ordinal);
            if (body.EndsWith(')') && paren > 0 && LooksLikeJsLocation(body[(paren + 2)..^1]))
            {
                return JsFrame(body[..paren].Replace("async ", string.Empty, StringComparison.Ordinal), body[(paren + 2)..^1]);
            }
            if (LooksLikeJsLocation(body)) return JsFrame(null, body);

            return DotnetFrame(body);
        }

        // JavaScript (SpiderMonkey, JavaScriptCore): "fn@https://host/app.js:12:34", "@https://host/app.js:12:34".
        var at = line.IndexOf('@', StringComparison.Ordinal);
        if (at >= 0 && LooksLikeJsLocation(line[(at + 1)..])) return JsFrame(line[..at], line[(at + 1)..]);

        return null;
    }

    /// <summary>
    /// The stack trace with the file paths of .NET frames reduced to the file name (a path can hold a user name or a
    /// build machine's layout), and at most <paramref name="maxLength"/> characters.
    /// </summary>
    public static string StripPaths(string stackTrace, int maxLength = 8192)
    {
        ArgumentNullException.ThrowIfNull(stackTrace);
        var stripped = PathRegex().Replace(stackTrace, m => " in " + Path.GetFileName(m.Groups["path"].Value.Replace('\\', '/')) + ":line");
        return stripped.Length > maxLength ? BelemetryValueLimit.Truncate(stripped, maxLength) : stripped;
    }

    /// <summary>Whether a normalized frame is framework or runtime code, shared by every caller.</summary>
    internal static bool IsFrameworkFrame(string frame) =>
        frame.StartsWith("System.", StringComparison.Ordinal)
        || frame.StartsWith("Microsoft.", StringComparison.Ordinal)
        || IsOwnFrame(frame)
        || frame.StartsWith("Interop+", StringComparison.Ordinal)
        || frame.Contains("@blazor.", StringComparison.Ordinal)
        || frame.Contains("@dotnet.", StringComparison.Ordinal)
        || frame.Contains("@bit-belemetry.", StringComparison.Ordinal)
        || frame.EndsWith("@native", StringComparison.Ordinal);

    /// <summary>
    /// Belemetry's own frames: every type of the library sits directly in the <c>Bit.Belemetry</c> namespace and is named
    /// <c>Belemetry*</c> or <c>IBelemetry*</c>. A namespace prefix alone would also pass over an app that lives under
    /// <c>Bit.Belemetry.*</c> (the demo and the samples do), as if its code were the library's.
    /// </summary>
    private static bool IsOwnFrame(string frame)
    {
        const string prefix = "Bit.Belemetry.";
        if (frame.StartsWith(prefix, StringComparison.Ordinal) is false) return false;
        var rest = frame.AsSpan(prefix.Length);
        return rest.StartsWith("Belemetry", StringComparison.Ordinal) || rest.StartsWith("IBelemetry", StringComparison.Ordinal);
    }

    internal static string NormalizeType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type)) return "Exception";
        var trimmed = type.Trim();
        return trimmed.Length > 256 ? trimmed[..256] : trimmed;
    }

    private static string? DotnetFrame(string body)
    {
        // "Namespace.Type.Method(Args) in C:\path\File.cs:line 42" - the method is what is before the first '('.
        var open = body.IndexOf('(');
        var method = open > 0 ? body[..open] : body;
        if (method.Length == 0 || method.Contains(' ')) return null;

        // Generic arity and instantiation: "Method[T]", "Type`1".
        method = GenericRegex().Replace(method, string.Empty);

        // Compiler-generated names: "<Method>d__3.MoveNext" (async), "<>c__DisplayClass5_0.<Method>b__0" (lambda),
        // "<Method>g__Local|5_0" (local function), "<>c.<Method>b__1_0".
        method = method.Replace(".MoveNext", string.Empty, StringComparison.Ordinal);
        method = LocalFunctionRegex().Replace(method, "$1.$2");
        method = CompilerGeneratedRegex().Replace(method, "$1");
        method = DisplayClassRegex().Replace(method, string.Empty);
        return method.Replace("..", ".", StringComparison.Ordinal).Trim('.');
    }

    private static string JsFrame(string? function, string location)
    {
        // The file name without query, hash, line or column; a content hash in the name ("main.3f2a1b9c.js") is dropped.
        var file = location;
        var end = file.IndexOfAny(['?', '#']);
        if (end >= 0) file = file[..end];
        file = LineColumnRegex().Replace(file, string.Empty);
        var slash = file.LastIndexOf('/');
        if (slash >= 0) file = file[(slash + 1)..];
        file = ContentHashRegex().Replace(file, string.Empty);
        if (file.Length == 0) file = "native";

        var name = string.IsNullOrWhiteSpace(function) ? "<anonymous>" : function.Trim();
        return name + "@" + file;
    }

    private static bool LooksLikeJsLocation(string location) =>
        location.Contains("://", StringComparison.Ordinal) || location.StartsWith("native", StringComparison.Ordinal) || location.StartsWith("<anonymous>", StringComparison.Ordinal);

    private static string Hash(string key) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(key)).AsSpan(0, 12));

    [GeneratedRegex(@"https?://[^\s'""<>()\[\]]+", RegexOptions.CultureInvariant)]
    private static partial Regex UrlRegex();

    [GeneratedRegex(@"\b[0-9a-fA-F]{8}-(?:[0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}\b", RegexOptions.CultureInvariant)]
    private static partial Regex GuidRegex();

    [GeneratedRegex(@"\b\d{4}-\d{2}-\d{2}(?:[T ]\d{2}:\d{2}(?::\d{2}(?:\.\d+)?)?(?:Z|[+-]\d{2}:?\d{2})?)?(?![\w])", RegexOptions.CultureInvariant)]
    private static partial Regex TimestampRegex();

    [GeneratedRegex(@"\b(?=[0-9a-fA-F]*[0-9])(?=[0-9a-fA-F]*[a-fA-F])[0-9a-fA-F]{16,}\b", RegexOptions.CultureInvariant)]
    private static partial Regex HexRegex();

    [GeneratedRegex(@"(?<!\w)'[^'\r\n]{1,200}'(?!\w)|(?<!\w)""[^""\r\n]{1,200}""(?!\w)", RegexOptions.CultureInvariant)]
    private static partial Regex QuotedRegex();

    [GeneratedRegex(@"(?<![\w.{])\d{4,}(?![\w}])", RegexOptions.CultureInvariant)]
    private static partial Regex NumberRegex();

    [GeneratedRegex(@"\[[^\]]*\]|`\d+", RegexOptions.CultureInvariant)]
    private static partial Regex GenericRegex();

    [GeneratedRegex(@"<([^<>]+)>g__([^|]+)\|[\w]+", RegexOptions.CultureInvariant)]
    private static partial Regex LocalFunctionRegex();

    [GeneratedRegex(@"<([^<>]+)>[a-z]__[\w]+", RegexOptions.CultureInvariant)]
    private static partial Regex CompilerGeneratedRegex();

    [GeneratedRegex(@"<>c(?:__DisplayClass[\w]+)?\.?", RegexOptions.CultureInvariant)]
    private static partial Regex DisplayClassRegex();

    [GeneratedRegex(@":\d+(?::\d+)?$", RegexOptions.CultureInvariant)]
    private static partial Regex LineColumnRegex();

    [GeneratedRegex(@"[.-][0-9a-fA-F]{6,}(?=\.\w+$)", RegexOptions.CultureInvariant)]
    private static partial Regex ContentHashRegex();

    [GeneratedRegex(@" in (?<path>[^\r\n]+?):line", RegexOptions.CultureInvariant)]
    private static partial Regex PathRegex();
}

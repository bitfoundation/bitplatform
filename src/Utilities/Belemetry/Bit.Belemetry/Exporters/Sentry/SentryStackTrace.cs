using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Bit.Belemetry;

/// <summary>
/// The stack frames of a Sentry exception, oldest first as Sentry orders them: from a .NET exception object (with the
/// method token, the IL offset and the debug image, for the symbolicator), from a .NET stack trace's text, or from a
/// JavaScript stack (V8, SpiderMonkey, JavaScriptCore) - neither Relay nor Sentry parses a stack string. The text
/// parsers are mirrored by <c>Scripts/exporters/sentry.ts</c>.
/// </summary>
internal static partial class SentryStackTrace
{
    /// <summary>The most frames an exception keeps: the newest, where it was thrown.</summary>
    public const int MaxFrames = 100;

    /// <summary>The most exceptions of a chain an event keeps, outermost first.</summary>
    public const int MaxExceptions = 16;

    private const string EndOfInnerException = "--- End of inner exception stack trace ---";

    /// <summary>A Sentry stack frame.</summary>
    internal sealed class Frame
    {
        public string? Function { get; init; }
        public string? Module { get; init; }
        public string? FileName { get; init; }
        public string? AbsolutePath { get; init; }
        public int? Line { get; init; }
        public int? Column { get; init; }
        public bool InApp { get; init; }

        /// <summary>The IL offset (<c>0x...</c>), with a debug image only.</summary>
        public string? InstructionAddress { get; init; }

        /// <summary>The method's MethodDef row (<c>0x...</c>, its metadata token without the table byte), with a debug image only.</summary>
        public string? FunctionId { get; init; }

        /// <summary>The index of its debug image in the event's <c>debug_meta.images</c>.</summary>
        public int? Image { get; init; }
    }

    /// <summary>An exception of a .NET stack trace's text: its type and message (null when unknown) and frames, oldest first.</summary>
    internal sealed record TextException(string? Type, string? Message, List<Frame> Frames);

    /// <summary>
    /// The frames of <paramref name="exception"/>, oldest first, the newest <see cref="MaxFrames"/> of those
    /// <c>Exception.ToString()</c> shows; each frame whose assembly is a file gets the method token, the IL offset and
    /// its debug image (added to <paramref name="images"/>). Empty when the runtime has no method for a frame (NativeAOT).
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The methods on a stack were not trimmed away; a frame without metadata (NativeAOT) has no method, and the exception falls back to its stack trace's text.")]
    public static List<Frame> FromException(Exception exception, List<SentryDebugImage> images)
    {
        var result = new List<Frame>();
        StackFrame[] frames;
        try
        {
            frames = new StackTrace(exception, fNeedFileInfo: true).GetFrames();
        }
        catch (Exception)
        {
            return result;
        }

        foreach (var stackFrame in frames)
        {
            if (result.Count == MaxFrames) break;

            MethodBase? method;
            try
            {
                method = stackFrame.GetMethod();
            }
            catch (Exception)
            {
                continue;
            }

            if (method is null || IsShown(method) is false) continue;

            var (module, function) = Normalize(method.DeclaringType is { } type ? TypeName(type) : null, method.Name,
                                               method.DeclaringType?.DeclaringType is { } owner ? TypeName(owner) : null);

            string? path = null;
            int? line = null, column = null;
            try
            {
                path = stackFrame.GetFileName();
                line = stackFrame.GetFileLineNumber() is var l and > 0 ? l : null;
                column = stackFrame.GetFileColumnNumber() is var c and > 0 ? c : null;
            }
            catch (Exception)
            {
                // No file information: the symbolicator adds it from the PDB.
            }

            string? instruction = null, functionId = null;
            int? image = null;
            if (DebugImageOf(method) is { } debugImage && stackFrame.GetILOffset() is var offset and >= 0)
            {
                try
                {
                    // The symbolicator looks the method up by its MethodDef row: the token without its table byte, as
                    // Sentry's .NET SDK sends it. A method of another table (none on a stack, in practice) has no row.
                    var token = method.MetadataToken;
                    if ((token & 0xFF000000) == 0x06000000)
                    {
                        functionId = Hex(token & 0x00FFFFFF);
                        instruction = Hex(offset);
                        image = IndexOf(images, debugImage);
                    }
                }
                catch (InvalidOperationException)
                {
                    // A dynamic method has no token.
                    functionId = null;
                }
            }

            result.Add(new Frame
            {
                Function = function,
                Module = module,
                FileName = FileNameOf(path),
                AbsolutePath = string.IsNullOrEmpty(path) ? null : path,
                Line = line,
                Column = column,
                InApp = IsInApp(module, function),
                InstructionAddress = functionId is null ? null : instruction,
                FunctionId = functionId,
                Image = functionId is null ? null : image,
            });
        }

        result.Reverse();
        return result;
    }

    private static SentryDebugImage? DebugImageOf(MethodBase method)
    {
        try
        {
            return method.Module.Assembly.IsDynamic ? null : SentryDebugImage.Of(method.Module);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static int IndexOf(List<SentryDebugImage> images, SentryDebugImage image)
    {
        var index = images.IndexOf(image);
        if (index >= 0) return index;
        images.Add(image);
        return images.Count - 1;
    }

    /// <summary>As <c>StackTrace.ToString()</c> decides: aggressively inlined methods and <see cref="StackTraceHiddenAttribute"/> are not shown.</summary>
    private static bool IsShown(MethodBase method)
    {
        if ((method.MethodImplementationFlags & MethodImplAttributes.AggressiveInlining) != 0) return false;
        try
        {
            return method.IsDefined(typeof(StackTraceHiddenAttribute), inherit: false) is false
                   && method.DeclaringType?.IsDefined(typeof(StackTraceHiddenAttribute), inherit: false) is not true;
        }
        catch (Exception)
        {
            return true;
        }
    }

    /// <summary>A type's name as a stack trace prints it: no generic arguments, <c>.</c> for a nested type.</summary>
    private static string TypeName(Type type)
    {
        var name = type.FullName ?? type.Name;
        var bracket = name.IndexOf('[', StringComparison.Ordinal);
        return (bracket > 0 ? name[..bracket] : name).Replace('+', '.');
    }

    /// <summary>
    /// The module and function of a frame, the same from an exception object and from its text, so both group alike: an
    /// async method's or iterator's <c>MoveNext</c> is the method it was written as, on the type that declares it, as
    /// <c>Exception.ToString()</c> shows it.
    /// </summary>
    /// <param name="module">The declaring type.</param>
    /// <param name="function">The method name.</param>
    /// <param name="owner">The type that declares <paramref name="module"/>; from the text, the module without its last segment.</param>
    internal static (string? Module, string Function) Normalize(string? module, string function, string? owner)
    {
        if (function == "MoveNext" && module is not null && owner is not null)
        {
            var dot = module.LastIndexOf('.');
            if (StateMachineRegex().Match(dot >= 0 ? module[(dot + 1)..] : module) is { Success: true } match)
            {
                return (owner, match.Groups[1].Value);
            }
        }
        return (module, function);
    }

    private static bool IsInApp(string? module, string function) =>
        BelemetryExceptionFingerprint.IsFrameworkFrame(module is null ? function : module + "." + function) is false;

    private static string? FileNameOf(string? path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        var slash = path.LastIndexOfAny(['/', '\\']);
        return slash >= 0 ? path[(slash + 1)..] : path;
    }

    private static string Hex(int value) => "0x" + value.ToString("x", CultureInfo.InvariantCulture);

    /// <summary>
    /// The exceptions of a .NET stack trace's text (<c>Exception.ToString()</c>), outermost first: the header names them
    /// (<c>Outer: message ---&gt; Inner: message</c>), and their frames follow innermost first, each set closed by
    /// "End of inner exception stack trace". When the two do not line up - a message that spans lines and holds the arrow,
    /// an <c>AggregateException</c>'s other inner exceptions - only the outermost is told, with its own frames.
    /// </summary>
    public static List<TextException> ParseDotnet(string? text)
    {
        if (string.IsNullOrEmpty(text)) return [];

        var header = new List<string>();
        var segments = new List<List<Frame>> { new() };
        var inFrames = false;
        foreach (var raw in text.AsSpan().EnumerateLines())
        {
            var line = raw.Trim().ToString();
            if (inFrames is false)
            {
                if (line.StartsWith("at ", StringComparison.Ordinal) is false && line != EndOfInnerException)
                {
                    header.Add(raw.ToString());
                    continue;
                }
                inFrames = true;
            }

            if (line == EndOfInnerException)
            {
                segments.Add([]);
                continue;
            }

            // An AggregateException's other inner exceptions follow its own frames: they are not the chain's.
            if (line.StartsWith("--->", StringComparison.Ordinal)) break;

            if (DotnetFrame(line) is { } frame) segments[^1].Add(frame);
        }

        foreach (var segment in segments)
        {
            if (segment.Count > MaxFrames) segment.RemoveRange(MaxFrames, segment.Count - MaxFrames);
            segment.Reverse();
        }

        var parts = string.Join('\n', header).Split(" ---> ");
        var result = new List<TextException>();
        if (parts.Length == segments.Count)
        {
            for (var i = 0; i < parts.Length && i < MaxExceptions; i++)
            {
                var (type, message) = SplitHeader(parts[i]);
                result.Add(new TextException(type, message, segments[segments.Count - 1 - i]));
            }
        }
        else
        {
            var (type, message) = SplitHeader(parts[0]);
            result.Add(new TextException(type, message, segments[^1]));
        }

        return result;
    }

    private static (string? Type, string? Message) SplitHeader(string part)
    {
        var colon = part.IndexOf(": ", StringComparison.Ordinal);
        var type = (colon >= 0 ? part[..colon] : part).Trim();
        var message = colon >= 0 ? part[(colon + 2)..].TrimEnd() : null;
        return (type.Length == 0 || type.Contains(' ') ? null : type, string.IsNullOrEmpty(message) ? null : message);
    }

    /// <summary>A frame of a .NET stack trace's text (<c>at Namespace.Type.Method(Args) in path:line 42</c>); null for another line.</summary>
    internal static Frame? DotnetFrame(string line)
    {
        if (DotnetFrameRegex().Match(line) is not { Success: true } match) return null;

        var method = match.Groups["method"].Value.Trim();

        // A generic method's arguments ("Method[T]") are not part of its name.
        if (method.EndsWith(']') && method.IndexOf('[', StringComparison.Ordinal) is var bracket and > 0) method = method[..bracket];
        if (method.Length == 0 || method.Contains(' ')) return null;

        string? module;
        string function;
        if (method.EndsWith("..ctor", StringComparison.Ordinal) || method.EndsWith("..cctor", StringComparison.Ordinal))
        {
            var cut = method.LastIndexOf("..", StringComparison.Ordinal);
            (module, function) = (method[..cut], method[(cut + 1)..]);
        }
        else
        {
            var dot = method.LastIndexOf('.');
            (module, function) = dot > 0 ? (method[..dot], method[(dot + 1)..]) : (null, method);
        }

        var ownerDot = module?.LastIndexOf('.') ?? -1;
        (module, function) = Normalize(module, function, ownerDot > 0 ? module![..ownerDot] : null);

        var path = match.Groups["file"].Success ? match.Groups["file"].Value : null;
        return new Frame
        {
            Function = function,
            Module = module,
            FileName = FileNameOf(path),
            AbsolutePath = path,
            Line = match.Groups["line"].Success && int.TryParse(match.Groups["line"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var l) ? l : null,
            InApp = IsInApp(module, function),
        };
    }

    /// <summary>The frames of a JavaScript stack (V8, SpiderMonkey, JavaScriptCore), oldest first, at most <see cref="MaxFrames"/> of the newest.</summary>
    public static List<Frame> ParseJavaScript(string? text)
    {
        var result = new List<Frame>();
        if (string.IsNullOrEmpty(text)) return result;

        foreach (var raw in text.AsSpan().EnumerateLines())
        {
            if (result.Count == MaxFrames) break;
            if (JavaScriptFrame(raw.Trim().ToString()) is { } frame) result.Add(frame);
        }

        result.Reverse();
        return result;
    }

    /// <summary>A frame of a JavaScript stack; null for a line that is not one (the message V8 starts with).</summary>
    internal static Frame? JavaScriptFrame(string line)
    {
        string? function;
        string location;
        if (line.StartsWith("at ", StringComparison.Ordinal))
        {
            // V8: "at fn (url:1:2)", "at url:1:2", "at async fn (url:1:2)", "at new Foo (url:1:2)", "at fn (<anonymous>)".
            var body = line[3..].Trim();
            if (body.StartsWith("async ", StringComparison.Ordinal)) body = body[6..];
            var open = body.IndexOf(" (", StringComparison.Ordinal);
            if (open > 0 && body.EndsWith(')'))
            {
                function = body[..open];
                location = body[(open + 2)..^1];
            }
            else
            {
                function = null;
                location = body;
            }

            // "eval at fn (url:1:2), <anonymous>:3:4": the script that called eval is where the code came from.
            if (location.StartsWith("eval at ", StringComparison.Ordinal))
            {
                if (EvalLocationRegex().Match(location) is not { Success: true } eval) return null;
                location = eval.Groups[1].Value;
            }
        }
        else
        {
            // SpiderMonkey and JavaScriptCore: "fn@url:1:2", "@url:1:2", "async*fn@url:1:2", "global code@url:1:2",
            // "map@[native code]". The first '@': a URL may hold one (a CDN's "pkg@1.0.0"), a function name may not.
            var at = line.IndexOf('@', StringComparison.Ordinal);
            if (at < 0) return null;
            function = line[..at];
            if (function.StartsWith("async*", StringComparison.Ordinal)) function = function[6..];
            location = line[(at + 1)..];

            // SpiderMonkey's eval: "url line 2 > eval:1:1" - the script that called eval, at its line.
            var evalLine = location.IndexOf(" line ", StringComparison.Ordinal);
            if (evalLine > 0 && location.Contains(" > ", StringComparison.Ordinal))
            {
                var digits = location[(evalLine + 6)..];
                var end = 0;
                while (end < digits.Length && char.IsAsciiDigit(digits[end])) end++;
                location = location[..evalLine] + (end > 0 ? ":" + digits[..end] : string.Empty);
            }
        }

        string url;
        int? lineNumber = null, column = null;
        if (LocationRegex().Match(location) is { Success: true } parts)
        {
            url = parts.Groups["url"].Value;
            lineNumber = int.Parse(parts.Groups["line"].ValueSpan, NumberStyles.None, CultureInfo.InvariantCulture);
            if (parts.Groups["column"].Success) column = int.Parse(parts.Groups["column"].ValueSpan, NumberStyles.None, CultureInfo.InvariantCulture);
        }
        else
        {
            url = location;
        }

        // An '@' line of something else (a message holding an e-mail address) has no script location.
        if (line.StartsWith("at ", StringComparison.Ordinal) is false && lineNumber is null && url.Contains("://", StringComparison.Ordinal) is false && url != "[native code]") return null;
        if (url.Length == 0) return null;

        return new Frame
        {
            Function = string.IsNullOrWhiteSpace(function) ? "?" : function.Trim(),
            FileName = url,
            AbsolutePath = url,
            Line = lineNumber,
            Column = column,
            InApp = IsJavaScriptInApp(url),
        };
    }

    /// <summary>
    /// Whether a script is the app's: one loaded from a URL (any scheme - a Hybrid app's page is <c>app://</c> on iOS)
    /// that is not a browser extension's, nor Blazor's runtime (<c>/_framework/</c>) or Belemetry's script.
    /// </summary>
    private static bool IsJavaScriptInApp(string url) =>
        url.Contains("://", StringComparison.Ordinal)
        && ExtensionSchemeRegex().IsMatch(url) is false
        && url.Contains("/_framework/", StringComparison.Ordinal) is false
        && url.Contains("/_content/Bit.Belemetry/", StringComparison.Ordinal) is false;

    [GeneratedRegex(@"^<([^<>]+)>d(?:__\d+)?$", RegexOptions.CultureInvariant)]
    private static partial Regex StateMachineRegex();

    /// <summary>
    /// Whether <paramref name="text"/> is a .NET stack trace: a line is a .NET frame. A JavaScript frame never is - V8
    /// puts a space before the location's parenthesis, the others have no "at " - so a .NET exception the browser script
    /// recorded (a Blazor Server component's <c>TrackException</c>) is told by its stack, not by who recorded it.
    /// </summary>
    public static bool IsDotnet(string? text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        foreach (var raw in text.AsSpan().EnumerateLines())
        {
            if (DotnetFrame(raw.Trim().ToString()) is not null) return true;
        }
        return false;
    }

    [GeneratedRegex(@"^at (?<method>[^\s(]+)\((?<args>.*?)\)(?: \+ 0x[0-9a-fA-F]+)?(?: in (?<file>.+):line (?<line>\d+))?$", RegexOptions.CultureInvariant)]
    private static partial Regex DotnetFrameRegex();

    [GeneratedRegex(@"\(([^()]+:\d+:\d+)\)", RegexOptions.CultureInvariant)]
    private static partial Regex EvalLocationRegex();

    [GeneratedRegex(@"^(?<url>.*?):(?<line>\d+)(?::(?<column>\d+))?$", RegexOptions.CultureInvariant)]
    private static partial Regex LocationRegex();

    [GeneratedRegex(@"^(?:chrome|moz|safari|safari-web|ms-browser)-extension:", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex ExtensionSchemeRegex();
}

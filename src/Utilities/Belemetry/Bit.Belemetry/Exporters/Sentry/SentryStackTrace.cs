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
    internal sealed record Frame
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

        /// <summary>The PE file of its method, which the event lists in <c>debug_meta.images</c>; with a <see cref="FunctionId"/> only.</summary>
        public SentryDebugImage? DebugImage { get; init; }
    }

    /// <summary>An exception of a .NET stack trace's text: its type and message (null when unknown) and frames, oldest first.</summary>
    internal sealed record TextException(string? Type, string? Message, List<Frame> Frames);

    /// <summary>
    /// The frames of <paramref name="exception"/>, oldest first, the newest <see cref="MaxFrames"/> of those
    /// <c>Exception.ToString()</c> shows; each frame whose assembly is a file gets the method token, the IL offset and
    /// its debug image. <c>Complete</c> is false when the runtime had no method for a frame (NativeAOT, wholly or in
    /// part): those frames are missing, and the stack trace's text tells them better.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <param name="fileInfo">
    /// Whether the frames get their file and line from the PDB. The stack trace's text holds them already, resolved once
    /// as the record was made: <see cref="TryAddFileInfo"/> copies them from there instead.
    /// </param>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The methods on a stack were not trimmed away; a frame without metadata (NativeAOT) has no method, and the exception falls back to its stack trace's text.")]
    public static (List<Frame> Frames, bool Complete) FromException(Exception exception, bool fileInfo = false)
    {
        var result = new List<Frame>();
        StackFrame[] frames;
        try
        {
            frames = new StackTrace(exception, fileInfo).GetFrames();
        }
        catch (Exception)
        {
            return (result, false);
        }

        var complete = true;
        for (var i = 0; i < frames.Length && result.Count < MaxFrames; i++)
        {
            var stackFrame = frames[i];
            MethodBase? method;
            try
            {
                method = stackFrame.GetMethod();
            }
            catch (Exception)
            {
                method = null;
            }

            if (method is null)
            {
                complete = false;
                continue;
            }

            // StackTrace.ToString() never hides the oldest frame.
            if (IsShown(method) is false && i < frames.Length - 1) continue;

            var (module, function) = Normalize(method.DeclaringType is { } type ? TypeName(type) : null, method.Name,
                                               method.DeclaringType?.DeclaringType is { } owner ? TypeName(owner) : null);

            string? path = null;
            int? line = null, column = null;
            if (fileInfo)
            {
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
            }

            string? instruction = null, functionId = null;
            var debugImage = DebugImageOf(method);
            if (debugImage is not null && stackFrame.GetILOffset() is var offset and >= 0)
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
                DebugImage = functionId is null ? null : debugImage,
            });
        }

        result.Reverse();
        return (result, complete);
    }

    /// <summary>
    /// Copies the file and line of the frames of an exception's text (<see cref="ParseDotnet"/>) onto the same frames
    /// from its object (<see cref="FromException"/>), which both show alike; false, and nothing copied, when the two
    /// do not line up.
    /// </summary>
    internal static bool TryAddFileInfo(List<Frame> frames, List<Frame> text)
    {
        if (frames.Count != text.Count) return false;
        for (var i = 0; i < frames.Count; i++)
        {
            var told = text[i];
            if (told.AbsolutePath is null && told.Line is null) continue;
            if (told.Function != frames[i].Function || told.Module != frames[i].Module) continue;
            frames[i] = frames[i] with { FileName = told.FileName, AbsolutePath = told.AbsolutePath, Line = told.Line };
        }
        return true;
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
    /// <c>Exception.ToString()</c> shows it - an async lambda's or local function's too (<c>&lt;&lt;Main&gt;b__0&gt;d</c>
    /// is <c>&lt;Main&gt;b__0</c>).
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
    /// an <c>AggregateException</c>'s other inner exceptions - only the outermost is told, with its own frames. The
    /// frames start at the first frame line: a message's line may start with "at" too ("at least one item is required").
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
            if (line == EndOfInnerException)
            {
                inFrames = true;
                segments.Add([]);
                continue;
            }

            // An AggregateException's other inner exceptions follow its own frames: they are not the chain's.
            if (inFrames && line.StartsWith("--->", StringComparison.Ordinal)) break;

            if (DotnetFrame(line) is { } frame)
            {
                inFrames = true;
                segments[^1].Add(frame);
            }
            else if (inFrames is false)
            {
                // A frame without the method's metadata ("at App!<BaseAddress>+0x1a2b", NativeAOT) is one word.
                if (line.StartsWith("at ", StringComparison.Ordinal) && IsOneWord(line.AsSpan(3))) inFrames = true;
                else header.Add(raw.ToString());
            }
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

    private static bool IsOneWord(ReadOnlySpan<char> text)
    {
        foreach (var c in text)
        {
            if (char.IsWhiteSpace(c)) return false;
        }
        return text.Length > 0;
    }

    private static (string? Type, string? Message) SplitHeader(string part)
    {
        var colon = part.IndexOf(": ", StringComparison.Ordinal);
        var type = (colon >= 0 ? part[..colon] : part).Trim();
        var message = colon >= 0 ? part[(colon + 2)..].TrimEnd() : null;
        return (type.Length == 0 || type.Contains(' ') ? null : type, string.IsNullOrEmpty(message) ? null : message);
    }

    /// <summary>
    /// A frame of a .NET stack trace's text (<c>at Namespace.Type.Method(Args)[ + 0x2a][ in path:line 42]</c>); null for
    /// another line. The method runs to the first <c>(</c>, with no whitespace before it - which no JavaScript frame
    /// has - and the arguments close at the first <c>)</c> the rest of the line can follow. Read by hand, in one pass:
    /// a relayed stack is anyone's text, and a backtracking pattern would cost the square of a long line.
    /// </summary>
    internal static Frame? DotnetFrame(string line)
    {
        if (line.StartsWith("at ", StringComparison.Ordinal) is false) return null;

        var open = 3;
        while (open < line.Length && line[open] != '(' && char.IsWhiteSpace(line[open]) is false) open++;
        if (open == 3 || open == line.Length || line[open] != '(') return null;

        // Where the file ends, when the line ends with ":line {n}".
        var digits = line.Length;
        while (digits > 0 && char.IsAsciiDigit(line[digits - 1])) digits--;
        var fileEnd = digits < line.Length && line.AsSpan(0, digits).EndsWith(":line ", StringComparison.Ordinal) ? digits - 6 : -1;

        string? path = null;
        var closed = false;
        for (var close = line.IndexOf(')', open + 1); close >= 0; close = line.IndexOf(')', close + 1))
        {
            var rest = close + 1;
            if (line.AsSpan(rest).StartsWith(" + 0x", StringComparison.Ordinal))
            {
                var hex = rest + 5;
                while (hex < line.Length && char.IsAsciiHexDigit(line[hex])) hex++;
                if (hex == rest + 5) continue;
                rest = hex;
            }

            if (rest == line.Length)
            {
                closed = true;
                break;
            }

            if (fileEnd > rest + 4 && line.AsSpan(rest).StartsWith(" in ", StringComparison.Ordinal))
            {
                path = line[(rest + 4)..fileEnd];
                closed = true;
                break;
            }
        }
        if (closed is false) return null;

        var method = line[3..open];

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

        return new Frame
        {
            Function = function,
            Module = module,
            FileName = FileNameOf(path),
            AbsolutePath = path,
            Line = path is not null ? ToInt(line.AsSpan(digits)) : null,
            InApp = IsInApp(module, function),
        };
    }

    /// <summary>The number of ASCII <paramref name="digits"/>; null past <see cref="int.MaxValue"/>, as the script's <c>toInt</c>.</summary>
    private static int? ToInt(ReadOnlySpan<char> digits) =>
        int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : null;

    /// <summary>
    /// The frames of a JavaScript stack (V8, SpiderMonkey, JavaScriptCore), oldest first, at most <see cref="MaxFrames"/>
    /// of the newest. A .NET frame's line is the message's (a JS interop call's error carries the .NET exception's text).
    /// </summary>
    public static List<Frame> ParseJavaScript(string? text)
    {
        var result = new List<Frame>();
        if (string.IsNullOrEmpty(text)) return result;

        foreach (var raw in text.AsSpan().EnumerateLines())
        {
            if (result.Count == MaxFrames) break;
            var line = raw.Trim().ToString();
            if (DotnetFrame(line) is null && JavaScriptFrame(line) is { } frame) result.Add(frame);
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
            lineNumber = ToInt(parts.Groups["line"].ValueSpan);
            if (parts.Groups["column"].Success) column = ToInt(parts.Groups["column"].ValueSpan);
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
    /// that is not a browser extension's, nor Blazor's runtime (<c>/_framework/</c>, and the .NET runtime's WebAssembly
    /// module, <c>wasm://</c>) or Belemetry's script.
    /// </summary>
    private static bool IsJavaScriptInApp(string url) =>
        url.Contains("://", StringComparison.Ordinal)
        && url.StartsWith("wasm:", StringComparison.OrdinalIgnoreCase) is false
        && ExtensionSchemeRegex().IsMatch(url) is false
        && url.Contains("/_framework/", StringComparison.Ordinal) is false
        && url.Contains("/_content/Bit.Belemetry/", StringComparison.Ordinal) is false;

    // Not anchored inside: an async lambda's or local function's state machine is named after a generated name itself.
    [GeneratedRegex(@"^<(.+)>d(?:__[0-9]+)?$", RegexOptions.CultureInvariant)]
    private static partial Regex StateMachineRegex();

    /// <summary>
    /// Whether <paramref name="text"/> is a .NET stack trace: a line is a .NET frame, and no JavaScript frame with a line
    /// number follows it. A JavaScript frame is never a .NET frame - V8 puts a space before the location's parenthesis,
    /// the others have no "at " - so a .NET exception the browser script recorded (a Blazor Server component's
    /// <c>TrackException</c>) is told by its stack, not by who recorded it; and a JS error whose message carries a .NET
    /// exception's text (a JS interop call's) is still a JavaScript stack, its frames after that text.
    /// </summary>
    public static bool IsDotnet(string? text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        var dotnet = false;
        foreach (var raw in text.AsSpan().EnumerateLines())
        {
            var line = raw.Trim().ToString();
            if (DotnetFrame(line) is not null) dotnet = true;
            else if (dotnet && line.StartsWith("---", StringComparison.Ordinal) is false && JavaScriptFrame(line) is { Line: not null }) return false;
        }
        return dotnet;
    }

    [GeneratedRegex(@"\(([^()]+:[0-9]+:[0-9]+)\)", RegexOptions.CultureInvariant)]
    private static partial Regex EvalLocationRegex();

    // ASCII digits, as the script reads them: \d would take any script's digits.
    [GeneratedRegex(@"^(?<url>.*?):(?<line>[0-9]+)(?::(?<column>[0-9]+))?$", RegexOptions.CultureInvariant)]
    private static partial Regex LocationRegex();

    [GeneratedRegex(@"^(?:chrome|moz|safari|safari-web|ms-browser)-extension:", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex ExtensionSchemeRegex();
}

using System.Text;
using System.Drawing;
using System.Reflection;
using System.Globalization;
using Bit.Bmage.Analysis;
using Bit.Bmage.Geometry;
using Bit.Bmage.Pipeline;

namespace Bit.Bmage.Demo.Server.Services;

/// <summary>
/// Reads a pipeline written the way an agent writes it - the fluent C# chain, e.g.
/// <c>ImagePipeline.Create().AutoOrient().MaxSize(1920, 1920).Sharpen(0.3f)</c> - and builds the
/// real <see cref="ImagePipeline"/> from it.
/// <para>
/// The builder methods are found by reflection rather than listed here, so every method
/// <see cref="ImagePipeline"/> has is accepted with exactly the parameters it has, defaults and
/// named arguments included: a pipeline copied out of an agent's code works verbatim, and one the
/// library would reject is rejected with the library's own message rather than a second opinion of
/// it. What a text spec cannot express - a custom <c>IImageOperation</c>, a <c>ColorMatrix</c> - is
/// refused by name.
/// </para>
/// </summary>
public static class BmagePipelineSpec
{
    /// <summary>A parsed pipeline, or the reason it could not be read.</summary>
    public sealed record Result(ImagePipeline? Pipeline, string Canonical, string[] Warnings, string? Error);

    /// <summary>The longest spec read; a pipeline is a dozen calls, not a file.</summary>
    public const int MaxLength = 4_000;

    /// <summary>The most builder calls a spec may chain; every one of them runs on a shared public server.</summary>
    public const int MaxOperations = 32;

    /// <summary>
    /// Builder methods that take something a spec cannot spell: an operation object, another
    /// pipeline, a matrix. They are named in the error rather than silently missing.
    /// </summary>
    private static readonly HashSet<string> _unsupported = new(StringComparer.OrdinalIgnoreCase)
    {
        "Add", "AddRange", "Insert", "RemoveAt", "Replace", "Concat", "Apply", "ColorMatrix", "From",
    };

    /// <summary>Calls that run or rewrite the pipeline rather than add to it. The tool does both itself.</summary>
    private static readonly HashSet<string> _terminal = new(StringComparer.OrdinalIgnoreCase)
    {
        "ExecuteAsync", "Execute", "Optimize", "ToString", "GetOutputSize", "TryGetOutputSize", "ConfigureAwait",
    };

    private static readonly Lazy<ILookup<string, MethodInfo>> _builders = new(() =>
        typeof(ImagePipeline).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(method => method.ReturnType == typeof(ImagePipeline) && method.IsGenericMethod is false)
            .ToLookup(method => method.Name, StringComparer.OrdinalIgnoreCase));

    /// <summary>Every builder method a spec can call, for the error that lists them.</summary>
    public static string[] BuilderNames => [.. _builders.Value.Select(group => group.Key)
        .Where(name => _unsupported.Contains(name) is false)
        .Order(StringComparer.Ordinal)];

    /// <summary>Parses a spec. Never throws: a spec that cannot be read comes back with <see cref="Result.Error"/> set.</summary>
    public static Result Parse(string? spec)
    {
        var text = StripComments(spec ?? string.Empty).Trim();

        if (text.Length == 0)
        {
            return new Result(null, string.Empty, [],
                "The pipeline is empty. Write it the way you would in C#, e.g. " +
                "'ImagePipeline.Create().AutoOrient().MaxSize(1920, 1920).Sharpen(0.3f)' - the 'ImagePipeline.Create()' " +
                "prefix is optional.");
        }

        if (text.Length > MaxLength)
        {
            return new Result(null, string.Empty, [], $"The pipeline is {text.Length} characters long; at most {MaxLength} are read.");
        }

        try
        {
            var calls = new Parser(text).ParseChain();

            if (calls.Count > MaxOperations)
            {
                return new Result(null, string.Empty, [], $"The pipeline chains {calls.Count} calls; at most {MaxOperations} are run.");
            }
            var warnings = new List<string>();
            var pipeline = ImagePipeline.Create();
            var canonical = new StringBuilder("ImagePipeline.Create()");

            foreach (var call in calls)
            {
                if (_terminal.Contains(call.Name))
                {
                    warnings.Add($"'{call.Name}(...)' was ignored: this tool runs the pipeline itself, with the optimiser on, " +
                                 "and reports the run without it alongside.");
                    continue;
                }

                if (_unsupported.Contains(call.Name))
                {
                    return new Result(null, string.Empty, [.. warnings],
                        $"'{call.Name}' takes an operation object, a pipeline or a matrix, which a text spec cannot construct. " +
                        $"The builder methods this tool can run are: {string.Join(", ", BuilderNames)}.");
                }

                var candidates = _builders.Value[call.Name].ToArray();

                if (candidates.Length == 0)
                {
                    return new Result(null, string.Empty, [.. warnings],
                        $"ImagePipeline has no method called '{call.Name}'. Its builder methods are: {string.Join(", ", BuilderNames)}.");
                }

                var failures = new List<string>();
                (MethodInfo Method, object?[] Arguments)? bound = null;

                // Fewest parameters first, so Crop(4 ints) is not tried against Crop(Rectangle) last.
                foreach (var candidate in candidates.OrderBy(method => method.GetParameters().Length))
                {
                    if (TryBind(candidate, call.Arguments, out var arguments, out var failure))
                    {
                        bound = (candidate, arguments);
                        break;
                    }

                    failures.Add($"{candidate.Name}{Describe(candidate)}: {failure}");
                }

                if (bound is not { } match)
                {
                    return new Result(null, string.Empty, [.. warnings],
                        $"'{call.Name}({string.Join(", ", call.Arguments.Select(argument => argument.Source))})' does not match " +
                        $"ImagePipeline.{candidates[0].Name}: {string.Join(" / ", failures)}");
                }

                try
                {
                    pipeline = (ImagePipeline)match.Method.Invoke(pipeline, match.Arguments)!;
                }
                catch (TargetInvocationException ex) when (ex.InnerException is not null)
                {
                    // The library's own validation: AspectRatio(0, 9), Posterize(1). Its message is the answer.
                    return new Result(null, string.Empty, [.. warnings],
                        $"ImagePipeline.{match.Method.Name} rejected its arguments: {ex.InnerException.Message}");
                }

                canonical.Append("\n    .").Append(match.Method.Name).Append('(')
                         .Append(string.Join(", ", call.Arguments.Select(Canonical)))
                         .Append(')');
            }

            if (pipeline.IsEmpty)
            {
                return new Result(null, string.Empty, [.. warnings],
                    "The pipeline has no operations. Add at least one, e.g. '.MaxSize(1920, 1920)'.");
            }

            return new Result(pipeline, canonical.ToString(), [.. warnings], null);
        }
        catch (FormatException ex)
        {
            return new Result(null, string.Empty, [],
                $"{ex.Message} Write the pipeline as a C# method chain, e.g. " +
                "'ImagePipeline.Create().AutoOrient().Cover(512, 512, Anchor.Top).Sharpen(0.3f)'.");
        }
    }

    private static string Describe(MethodInfo method)
        => $"({string.Join(", ", method.GetParameters().Select(parameter => $"{BmageApiCatalog.FriendlyName(parameter.ParameterType)} {parameter.Name}"))})";

    /// <summary>Line comments are allowed - a pipeline pasted out of code keeps its explanations.</summary>
    private static string StripComments(string text)
    {
        var builder = new StringBuilder(text.Length);
        var inString = false;

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];

            if (c == '"') inString = inString is false;

            if (inString is false && c == '/' && i + 1 < text.Length && text[i + 1] == '/')
            {
                while (i < text.Length && text[i] != '\n') i++;
                builder.Append('\n');
                continue;
            }

            builder.Append(c);
        }

        return builder.ToString();
    }

    // -- Binding ---------------------------------------------------------------

    private static bool TryBind(MethodInfo method, IReadOnlyList<Argument> supplied, out object?[] arguments, out string failure)
    {
        var parameters = method.GetParameters();
        arguments = new object?[parameters.Length];
        var assigned = new bool[parameters.Length];
        failure = string.Empty;

        var position = 0;

        var positional = supplied.Count(argument => argument.Name is null);

        if (positional > parameters.Length)
        {
            failure = $"it takes at most {parameters.Length} argument(s), and {positional} were given";
            return false;
        }

        foreach (var argument in supplied)
        {
            int index;

            if (argument.Name is { } name)
            {
                index = Array.FindIndex(parameters, parameter => string.Equals(parameter.Name, name, StringComparison.OrdinalIgnoreCase));

                if (index < 0)
                {
                    failure = $"it has no parameter called '{name}'";
                    return false;
                }
            }
            else
            {
                index = position++;

                if (index >= parameters.Length)
                {
                    failure = $"it takes at most {parameters.Length} argument(s), and {supplied.Count} were given";
                    return false;
                }
            }

            if (assigned[index])
            {
                failure = $"'{parameters[index].Name}' was given twice";
                return false;
            }

            if (TryConvert(argument.Value, parameters[index].ParameterType, out var value, out var reason) is false)
            {
                failure = $"'{parameters[index].Name}' is a {BmageApiCatalog.FriendlyName(parameters[index].ParameterType)}, and {reason}";
                return false;
            }

            arguments[index] = value;
            assigned[index] = true;
        }

        for (var i = 0; i < parameters.Length; i++)
        {
            if (assigned[i]) continue;

            if (parameters[i].HasDefaultValue is false)
            {
                failure = $"'{parameters[i].Name}' is required";
                return false;
            }

            arguments[i] = DefaultOf(parameters[i]);
        }

        return true;
    }

    /// <summary>
    /// A parameter's declared default as a value Invoke accepts. A defaulted struct
    /// (<c>Rgba32 background = default</c>) reports null, and an enum default its underlying number.
    /// </summary>
    private static object? DefaultOf(ParameterInfo parameter)
    {
        var type = parameter.ParameterType;
        var value = parameter.DefaultValue;

        if (value is null || value is DBNull) return type.IsValueType && Nullable.GetUnderlyingType(type) is null ? Activator.CreateInstance(type) : null;

        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        return underlying.IsEnum && value is not Enum ? Enum.ToObject(underlying, value) : value;
    }

    private static bool TryConvert(Value value, Type type, out object? result, out string reason)
    {
        result = null;
        reason = string.Empty;

        var nullable = Nullable.GetUnderlyingType(type);

        if (value.Kind == ValueKind.Null)
        {
            if (type.IsValueType && nullable is null)
            {
                reason = "null is not a value of it";
                return false;
            }

            return true;
        }

        var target = nullable ?? type;

        switch (value.Kind)
        {
            case ValueKind.Number when target == typeof(int):
                if (value.Number % 1 != 0 || value.Number is < int.MinValue or > int.MaxValue)
                {
                    reason = $"{value.Source} is not a whole number";
                    return false;
                }

                result = (int)value.Number;
                return true;

            case ValueKind.Number when target == typeof(float):
                result = (float)value.Number;
                return true;

            case ValueKind.Number when target == typeof(double):
                result = value.Number;
                return true;

            case ValueKind.Bool when target == typeof(bool):
                result = value.Bool;
                return true;

            case ValueKind.Object when value.Object is not null && target.IsInstanceOfType(value.Object):
                result = value.Object;
                return true;

            case ValueKind.Identifier when target.IsEnum:
                var member = value.Identifier!;
                var dot = member.LastIndexOf('.');
                var owner = dot > 0 ? member[..dot] : null;
                var name = dot > 0 ? member[(dot + 1)..] : member;

                if (owner is not null && owner.Split('.')[^1].Equals(target.Name, StringComparison.Ordinal) is false)
                {
                    reason = $"{member} is not a {target.Name}";
                    return false;
                }

                if (Enum.TryParse(target, name, ignoreCase: true, out var parsed) && Enum.IsDefined(target, parsed!))
                {
                    result = parsed;
                    return true;
                }

                reason = $"{target.Name} has no member '{name}' (it has {string.Join(", ", Enum.GetNames(target))})";
                return false;

            case ValueKind.Identifier when target == typeof(AspectRatio):
                if (TryStatic(value.Identifier!, typeof(AspectRatio), out var ratio))
                {
                    result = ratio;
                    return true;
                }

                reason = $"'{value.Identifier}' is not one of AspectRatio's presets ({string.Join(", ", StaticNames(typeof(AspectRatio)))}) - " +
                         "write new AspectRatio(4, 5) or \"4:5\" for any other";
                return false;

            case ValueKind.Identifier when target == typeof(Rgba32):
                if (TryStatic(value.Identifier!, typeof(Rgba32), out var color))
                {
                    result = color;
                    return true;
                }

                reason = $"'{value.Identifier}' is not a colour - write Rgba32.White, new Rgba32(24, 26, 31) or \"#18181f\"";
                return false;

            case ValueKind.String when target == typeof(Rgba32):
                if (Rgba32.TryParseHex(value.Text, out var hex))
                {
                    result = hex;
                    return true;
                }

                reason = $"\"{value.Text}\" is not a hex colour";
                return false;

            case ValueKind.String when target == typeof(AspectRatio):
                var parts = value.Text!.Split([':', 'x', '/'], StringSplitOptions.TrimEntries);

                if (parts.Length == 2
                    && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var width)
                    && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var height)
                    && width > 0 && height > 0)
                {
                    result = new AspectRatio(width, height);
                    return true;
                }

                reason = $"\"{value.Text}\" is not a ratio such as \"16:9\"";
                return false;

            case ValueKind.Object when target == typeof(SmartCropOptions) && value.Object is null:
                return true;
        }

        reason = value.Kind == ValueKind.Unsupported
            ? $"'{value.Source}' is not a value this tool can build - it understands new Rectangle(x, y, width, height), " +
              "new Rgba32(r, g, b[, a]) and new AspectRatio(width, height)"
            : $"{value.Source} is not one";

        return false;
    }

    /// <summary>A public static field or property of a type, by its name with or without the type in front.</summary>
    private static bool TryStatic(string identifier, Type type, out object? value)
    {
        value = null;

        var parts = identifier.Split('.');
        var name = parts[^1];

        if (parts.Length > 1 && parts[^2].Equals(type.Name, StringComparison.Ordinal) is false) return false;

        const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase;

        if (type.GetProperty(name, flags) is { } property && property.PropertyType == type)
        {
            value = property.GetValue(null);
            return true;
        }

        if (type.GetField(name, flags) is { } field && field.FieldType == type)
        {
            value = field.GetValue(null);
            return true;
        }

        return false;
    }

    private static IEnumerable<string> StaticNames(Type type)
        => type.GetProperties(BindingFlags.Public | BindingFlags.Static).Where(property => property.PropertyType == type).Select(property => property.Name);

    /// <summary>One argument, written back the way it was given, for the pipeline as understood.</summary>
    private static string Canonical(Argument argument)
        => argument.Name is null ? argument.Value.Source : $"{argument.Name}: {argument.Value.Source}";

    // -- Parsing ---------------------------------------------------------------

    private enum ValueKind { Number, Bool, Null, String, Identifier, Object, Unsupported }

    private sealed record Value(ValueKind Kind, string Source)
    {
        public double Number { get; init; }
        public bool Bool { get; init; }
        public string? Text { get; init; }
        public string? Identifier { get; init; }
        public object? Object { get; init; }
    }

    private sealed record Argument(string? Name, Value Value)
    {
        public string Source => Name is null ? Value.Source : $"{Name}: {Value.Source}";
    }

    private sealed record Call(string Name, IReadOnlyList<Argument> Arguments);

    /// <summary>A recursive-descent reader for the small part of C# a pipeline chain is written in.</summary>
    private sealed class Parser(string text)
    {
        /// <summary>Marks a construction this reader cannot evaluate.</summary>
        private static readonly object Unsupported = new();

        private int _position;

        public List<Call> ParseChain()
        {
            SkipPrefix();

            var calls = new List<Call>();

            while (true)
            {
                SkipWhitespace();

                if (AtEnd || Peek == ';') break;

                if (Peek == '.') _position++;

                SkipWhitespace();

                var name = ReadIdentifier() ?? throw Error("Expected a method name");

                SkipWhitespace();

                if (Peek != '(') throw Error($"Expected '(' after '{name}'");

                calls.Add(new Call(name, ReadArguments()));
            }

            SkipWhitespace();

            if (AtEnd is false && Peek == ';') _position++;

            SkipWhitespace();

            if (AtEnd is false) throw Error("Unexpected text after the pipeline");

            return calls;
        }

        /// <summary>
        /// Everything up to the first builder call: "var pipeline = ", "await ",
        /// "ImagePipeline.Create()" and "ImagePipeline.Empty" are all accepted and dropped.
        /// </summary>
        private void SkipPrefix()
        {
            SkipWhitespace();

            var assignment = text.IndexOf('=', _position);
            var parenthesis = text.IndexOf('(', _position);

            if (assignment > 0 && (parenthesis < 0 || assignment < parenthesis) && text[assignment..].StartsWith("=>", StringComparison.Ordinal) is false)
            {
                _position = assignment + 1;
            }

            SkipWhitespace();

            if (Match("await")) SkipWhitespace();

            if (Match("ImagePipeline"))
            {
                SkipWhitespace();

                if (Peek != '.') throw Error("Expected '.Create()' after 'ImagePipeline'");

                _position++;
                SkipWhitespace();

                var start = ReadIdentifier();

                if (start is "Create")
                {
                    SkipWhitespace();

                    if (ReadArguments().Count > 0) throw Error("ImagePipeline.Create() takes no arguments");
                }
                else if (start is not "Empty")
                {
                    throw Error($"Expected 'ImagePipeline.Create()', found 'ImagePipeline.{start}'");
                }
            }
        }

        private List<Argument> ReadArguments()
        {
            Expect('(');

            var arguments = new List<Argument>();

            SkipWhitespace();

            if (Peek == ')')
            {
                _position++;
                return arguments;
            }

            while (true)
            {
                SkipWhitespace();

                string? name = null;
                var mark = _position;

                // "name: value" - a named argument - or the start of a value.
                if (ReadIdentifier() is { } identifier)
                {
                    SkipWhitespace();

                    if (AtEnd is false && Peek == ':')
                    {
                        _position++;
                        name = identifier;
                    }
                    else
                    {
                        _position = mark;
                    }
                }

                SkipWhitespace();

                arguments.Add(new Argument(name, ReadValue()));

                SkipWhitespace();

                if (AtEnd) throw Error("The argument list is not closed");

                if (Peek == ',')
                {
                    _position++;
                    continue;
                }

                Expect(')');

                return arguments;
            }
        }

        private Value ReadValue()
        {
            SkipWhitespace();

            if (AtEnd) throw Error("Expected a value");

            var start = _position;

            if (Peek == '"')
            {
                var close = text.IndexOf('"', _position + 1);
                if (close < 0) throw Error("A string is not closed");

                var content = text[(_position + 1)..close];
                _position = close + 1;

                return new Value(ValueKind.String, text[start.._position]) { Text = content };
            }

            if (char.IsDigit(Peek) || Peek is '-' or '+' or '.')
            {
                return ReadNumber();
            }

            if (Match("new"))
            {
                return ReadConstruction(start);
            }

            var identifier = ReadQualifiedIdentifier() ?? throw Error("Expected a value");

            switch (identifier)
            {
                case "true": return new Value(ValueKind.Bool, "true") { Bool = true };
                case "false": return new Value(ValueKind.Bool, "false") { Bool = false };
                case "null" or "default": return new Value(ValueKind.Null, identifier);
            }

            SkipWhitespace();

            // Rgba32.ParseHex("#ff0000") / Rgba32.FromScaled(...) are the two calls a colour is
            // commonly written as.
            if (AtEnd is false && Peek == '(')
            {
                var arguments = ReadArguments();
                var source = text[start.._position];

                if (identifier.EndsWith("ParseHex", StringComparison.Ordinal) && arguments is [{ Value.Kind: ValueKind.String } hex]
                    && Rgba32.TryParseHex(hex.Value.Text, out var parsed))
                {
                    return new Value(ValueKind.Object, source) { Object = parsed };
                }

                throw Error($"'{identifier}(...)' cannot be evaluated here - write the value itself");
            }

            return new Value(ValueKind.Identifier, identifier) { Identifier = identifier };
        }

        private Value ReadNumber()
        {
            var start = _position;

            if (Peek is '-' or '+') _position++;

            while (AtEnd is false && (char.IsDigit(Peek) || Peek is '.' or '_' or 'e' or 'E'
                                      || (Peek is '-' or '+' && text[_position - 1] is 'e' or 'E')))
            {
                _position++;
            }

            var digits = text[start.._position].Replace("_", string.Empty, StringComparison.Ordinal);

            // A C# literal suffix: 0.3f, 0.5d, 1m. It says which overload was meant, not what the number is.
            if (AtEnd is false && Peek is 'f' or 'F' or 'd' or 'D' or 'm' or 'M') _position++;

            if (double.TryParse(digits, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) is false)
            {
                throw Error($"'{text[start.._position]}' is not a number");
            }

            return new Value(ValueKind.Number, text[start.._position]) { Number = number };
        }

        /// <summary>new Rectangle(...), new Rgba32(...), new AspectRatio(...) - and new SmartCropOptions() for the default.</summary>
        private Value ReadConstruction(int start)
        {
            SkipWhitespace();

            var type = ReadQualifiedIdentifier() ?? throw Error("Expected a type after 'new'");
            var simple = type.Split('.')[^1];

            SkipWhitespace();

            var arguments = ReadArguments();
            var numbers = arguments.Select(argument => argument.Value.Kind == ValueKind.Number ? argument.Value.Number : double.NaN).ToArray();
            var source = text[start.._position];

            object? instance = simple switch
            {
                "Rectangle" when numbers.Length == 4 && numbers.All(double.IsFinite)
                    => new Rectangle((int)numbers[0], (int)numbers[1], (int)numbers[2], (int)numbers[3]),
                "Rgba32" when numbers.Length is 3 or 4 && numbers.All(n => n is >= 0 and <= 255)
                    => new Rgba32((byte)numbers[0], (byte)numbers[1], (byte)numbers[2], numbers.Length == 4 ? (byte)numbers[3] : (byte)255),
                "AspectRatio" when numbers.Length == 2 && numbers.All(n => n > 0)
                    => new AspectRatio(numbers[0], numbers[1]),
                "SmartCropOptions" when arguments.Count == 0 => null,
                // Not thrown here: the call it is an argument of may be one the spec cannot run at all,
                // and that is the more useful thing to be told.
                _ => Unsupported
            };

            return ReferenceEquals(instance, Unsupported)
                ? new Value(ValueKind.Unsupported, source)
                : new Value(ValueKind.Object, source) { Object = instance };
        }

        private string? ReadQualifiedIdentifier()
        {
            var first = ReadIdentifier();
            if (first is null) return null;

            var builder = new StringBuilder(first);

            while (AtEnd is false && Peek == '.' && _position + 1 < text.Length && (char.IsLetter(text[_position + 1]) || text[_position + 1] == '_'))
            {
                _position++;
                builder.Append('.').Append(ReadIdentifier());
            }

            return builder.ToString();
        }

        private string? ReadIdentifier()
        {
            if (AtEnd || (char.IsLetter(Peek) is false && Peek != '_')) return null;

            var start = _position;

            while (AtEnd is false && (char.IsLetterOrDigit(Peek) || Peek == '_')) _position++;

            return text[start.._position];
        }

        /// <summary>Consumes a keyword when it stands alone, not when it starts a longer identifier.</summary>
        private bool Match(string keyword)
        {
            if (string.CompareOrdinal(text, _position, keyword, 0, keyword.Length) != 0) return false;

            var end = _position + keyword.Length;

            if (end < text.Length && (char.IsLetterOrDigit(text[end]) || text[end] == '_')) return false;

            _position = end;
            return true;
        }

        private void Expect(char c)
        {
            SkipWhitespace();

            if (AtEnd || Peek != c) throw Error($"Expected '{c}'");

            _position++;
        }

        private void SkipWhitespace()
        {
            while (AtEnd is false && char.IsWhiteSpace(Peek)) _position++;
        }

        private bool AtEnd => _position >= text.Length;

        /// <summary>The next character, or NUL at the end - so a spec that stops mid-call is an error message, not an exception.</summary>
        private char Peek => _position < text.Length ? text[_position] : '\0';

        private FormatException Error(string message)
        {
            var near = text[Math.Min(_position, text.Length)..].Trim();

            return new FormatException(near.Length == 0
                ? $"{message} at the end of the pipeline."
                : $"{message} near '{(near.Length > 24 ? near[..24] + "..." : near)}'.");
        }
    }
}

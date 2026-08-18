using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.Bmotion.Demo.Server.Services;

/// <summary>
/// Turns the way a person (or an agent) writes a transition into the <see cref="BmTransition"/> the
/// engine runs.
/// <para>
/// An MCP client has one string to hand over, and no two of them phrase a spring the same way:
/// <c>Bm.Spring(stiffness: 260, damping: 12)</c> copied out of a Razor file, <c>spring stiffness=260
/// damping=12</c> typed from memory, <c>spring(0.4 bounce)</c> half-remembered. Rejecting all but one
/// spelling would mean an agent spends its turns guessing at syntax instead of at motion, so this
/// parser accepts the family and reports what it understood - including the canonical C# call, which
/// is what the agent should actually write into the code.
/// </para>
/// <para>
/// It is deliberately not a C# parser. It recognises the three transition kinds and their named
/// arguments; anything it does not recognise comes back as a warning rather than being silently
/// dropped, so a misspelled argument is visible instead of quietly becoming a default.
/// </para>
/// </summary>
public static partial class BmotionTransitionSpec
{
    /// <summary>What a spec string parsed into.</summary>
    /// <param name="Transition">The transition to run, or null when the spec could not be read.</param>
    /// <param name="Canonical">The C# call that produces it - what to write in the code.</param>
    /// <param name="Warnings">Arguments that were not recognised, and assumptions that were made.</param>
    /// <param name="Error">Why the spec could not be read, when it could not.</param>
    public sealed record Result(BmTransition? Transition, string Canonical, string[] Warnings, string? Error);

    /// <summary>The transition kinds a spec can name, for error messages and tool descriptions.</summary>
    public static readonly string[] Kinds = ["spring", "tween", "inertia"];

    /// <summary>
    /// Reads a transition spec. Accepts <c>spring</c>, <c>tween</c> and <c>inertia</c>, with or
    /// without a <c>Bm.</c> prefix, parentheses, named arguments (<c>name: value</c> or
    /// <c>name=value</c>) and positional arguments in the order the <c>Bm</c> factory declares them.
    /// An empty spec is a default tween, which is what the library itself falls back to.
    /// </summary>
    public static Result Parse(string? spec)
    {
        var text = (spec ?? string.Empty).Trim();

        if (text.Length == 0) return Build("tween", [], []);

        // "Bm.Spring(...)", "Motion.Spring(...)" and a bare "Spring(...)" are the same request.
        text = QualifierRegex().Replace(text, string.Empty);

        var open = text.IndexOf('(');
        var kindText = (open < 0 ? FirstWord(text) : text[..open]).Trim();
        var kind = kindText.ToLowerInvariant();

        var warnings = new List<string>();

        // A spec that opens with a number is a duration: "0.4" and "0.4, InOut" are tweens.
        if (kind.Length == 0 || double.TryParse(kindText, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
        {
            kind = "tween";
            warnings.Add("No transition kind was named, so this was read as a tween. Write 'spring', 'tween' or 'inertia' to be explicit.");
            return Build(kind, SplitArguments(text), warnings);
        }

        if (Kinds.Contains(kind) is false)
        {
            return new Result(null, string.Empty, [],
                $"'{kindText}' is not a Bit.Bmotion transition kind. Use one of: {string.Join(", ", Kinds)} - " +
                "for example 'spring(stiffness: 260, damping: 12)', 'tween(0.4, InOut)' or 'inertia(velocity: 500)'.");
        }

        // Arguments are whatever sits inside the parentheses, or everything after the kind when the
        // spec was written without them.
        var close = text.LastIndexOf(')');
        var arguments = open >= 0 && close > open
            ? text[(open + 1)..close]
            : text[kindText.Length..];

        return Build(kind, SplitArguments(arguments), warnings);
    }

    private static Result Build(string kind, string[] arguments, List<string> warnings)
    {
        // Positional arguments follow the Bm factory signatures, so a spec copied out of Razor
        // means here what it means there.
        string[] positional = kind switch
        {
            "spring" => ["stiffness", "damping", "mass"],
            "inertia" => ["velocity", "timeconstant", "power"],
            _ => ["duration", "ease"]
        };

        var named = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var index = 0;

        foreach (var argument in arguments)
        {
            var separator = argument.IndexOfAny([':', '=']);

            if (separator > 0)
            {
                var name = argument[..separator].Trim();
                var value = argument[(separator + 1)..].Trim();

                if (name.Length > 0) named[name] = value;

                continue;
            }

            // A positional argument only has meaning while the signature still has a slot for it.
            if (index < positional.Length)
            {
                named[positional[index++]] = argument.Trim();
                continue;
            }

            warnings.Add($"'{argument.Trim()}' was ignored: it is past the last positional argument of a {kind}.");
        }

        return kind switch
        {
            "spring" => BuildSpring(named, warnings),
            "inertia" => BuildInertia(named, warnings),
            _ => BuildTween(named, warnings)
        };
    }

    private static Result BuildSpring(Dictionary<string, string> named, List<string> warnings)
    {
        var spring = new BmSpring();
        var written = new List<string>();

        // Bounce and duration derive stiffness and damping, so a spec that sets both forms would
        // silently have half of it ignored by the engine. Say so rather than letting it look applied.
        var hasPhysics = named.ContainsKey("stiffness") || named.ContainsKey("damping");
        var hasFeel = named.ContainsKey("bounce");

        if (hasPhysics && hasFeel)
        {
            warnings.Add("Both 'bounce' and 'stiffness'/'damping' were given. Bounce wins: the engine derives " +
                         "stiffness and damping from bounce and duration, so the explicit values are unused.");
        }

        foreach (var (name, value) in named)
        {
            switch (name.ToLowerInvariant())
            {
                case "stiffness" when TryNumber(name, value, warnings, out var stiffness):
                    spring.Stiffness = stiffness;
                    written.Add($"stiffness: {Number(stiffness)}");
                    break;

                case "damping" when TryNumber(name, value, warnings, out var damping):
                    spring.Damping = damping;
                    written.Add($"damping: {Number(damping)}");
                    break;

                case "mass" when TryNumber(name, value, warnings, out var mass):
                    spring.Mass = mass;
                    written.Add($"mass: {Number(mass)}");
                    break;

                case "bounce" when TryNumber(name, value, warnings, out var bounce):
                    spring.Bounce = bounce;
                    written.Add($"bounce: {Number(bounce)}");
                    break;

                case "duration" or "visualduration" when TryNumber(name, value, warnings, out var duration):
                    spring.Duration = duration;
                    written.Add($"duration: {Number(duration)}");
                    break;

                case "velocity" when TryNumber(name, value, warnings, out var velocity):
                    spring.Velocity = velocity;
                    written.Add($"velocity: {Number(velocity)}");
                    break;

                case "restspeed" when TryNumber(name, value, warnings, out var restSpeed):
                    spring.RestSpeed = restSpeed;
                    break;

                case "restdelta" when TryNumber(name, value, warnings, out var restDelta):
                    spring.RestDelta = restDelta;
                    break;

                case "delay" when TryNumber(name, value, warnings, out var delay):
                    spring.Delay = delay;
                    written.Add($"delay: {Number(delay)}");
                    break;

                default:
                    Unknown(name, "spring", "stiffness, damping, mass, bounce, duration, velocity, restSpeed, restDelta, delay", warnings);
                    break;
            }
        }

        // A bounce spring with no duration is the library's own default visual duration; stating it
        // keeps the canonical call runnable as written.
        if (spring.Bounce.HasValue && spring.Duration.HasValue is false)
        {
            warnings.Add("'bounce' was given without a 'duration'; the spring uses its Duration default. " +
                         "Pass both for the motion.dev-style pairing, e.g. spring(bounce: 0.4, duration: 0.6).");
        }

        return new Result(spring, $"Bm.Spring({string.Join(", ", written)})", [.. warnings], null);
    }

    private static Result BuildTween(Dictionary<string, string> named, List<string> warnings)
    {
        var tween = new BmTween();
        var written = new List<string>();

        foreach (var (name, value) in named)
        {
            switch (name.ToLowerInvariant())
            {
                case "duration" when TryNumber(name, value, warnings, out var duration):
                    tween.Duration = duration;
                    written.Insert(0, Number(duration));
                    break;

                case "ease" or "easing":
                    if (TryEase(value, out var ease))
                    {
                        tween.Ease = ease;
                        written.Add($"BmEase.{ease}");
                    }
                    else
                    {
                        warnings.Add($"'{value}' is not a BmEase value; the tween kept BmEase.{tween.Ease}. " +
                                     "Call GetBmotionEasings for the full list.");
                    }
                    break;

                case "delay" when TryNumber(name, value, warnings, out var delay):
                    tween.Delay = delay;
                    written.Add($"delay: {Number(delay)}");
                    break;

                case "steps" when TryNumber(name, value, warnings, out var steps):
                    tween.Steps = (int)steps;
                    written.Add($"steps: {(int)steps}");
                    break;

                case "bezier":
                    var bezier = Numbers(value);
                    if (bezier.Length == 4)
                    {
                        tween.Bezier = bezier;
                        written.Add($"bezier: [{string.Join(", ", bezier.Select(Number))}]");
                    }
                    else
                    {
                        warnings.Add("'bezier' needs exactly four numbers, e.g. bezier: [0.42, 0, 0.58, 1].");
                    }
                    break;

                default:
                    Unknown(name, "tween", "duration, ease, delay, steps, bezier", warnings);
                    break;
            }
        }

        return new Result(tween, $"Bm.Tween({string.Join(", ", written)})", [.. warnings], null);
    }

    private static Result BuildInertia(Dictionary<string, string> named, List<string> warnings)
    {
        var inertia = new BmInertia();
        var written = new List<string>();

        foreach (var (name, value) in named)
        {
            switch (name.ToLowerInvariant())
            {
                case "velocity" when TryNumber(name, value, warnings, out var velocity):
                    inertia.Velocity = velocity;
                    written.Add($"velocity: {Number(velocity)}");
                    break;

                case "timeconstant" when TryNumber(name, value, warnings, out var timeConstant):
                    inertia.TimeConstant = timeConstant;
                    written.Add($"timeConstant: {Number(timeConstant)}");
                    break;

                case "power" when TryNumber(name, value, warnings, out var power):
                    inertia.Power = power;
                    written.Add($"power: {Number(power)}");
                    break;

                case "min" when TryNumber(name, value, warnings, out var min):
                    inertia.Min = min;
                    written.Add($"min: {Number(min)}");
                    break;

                case "max" when TryNumber(name, value, warnings, out var max):
                    inertia.Max = max;
                    written.Add($"max: {Number(max)}");
                    break;

                case "restdelta" when TryNumber(name, value, warnings, out var restDelta):
                    inertia.RestDelta = restDelta;
                    break;

                case "delay" when TryNumber(name, value, warnings, out var delay):
                    inertia.Delay = delay;
                    written.Add($"delay: {Number(delay)}");
                    break;

                default:
                    Unknown(name, "inertia", "velocity, timeConstant, power, min, max, restDelta, delay", warnings);
                    break;
            }
        }

        return new Result(inertia, $"Bm.Inertia({string.Join(", ", written)})", [.. warnings], null);
    }

    /// <summary>Resolves a <see cref="BmEase"/> by name, with or without its enum qualifier.</summary>
    public static bool TryEase(string? text, out BmEase ease)
    {
        ease = BmEase.Out;

        var name = (text ?? string.Empty).Trim();
        if (name.Length == 0) return false;

        // "BmEase.InOut", "ease.InOut" and "easeInOut" all name the same member.
        var dot = name.LastIndexOf('.');
        if (dot >= 0) name = name[(dot + 1)..];
        if (name.StartsWith("ease", StringComparison.OrdinalIgnoreCase) && name.Length > 4) name = name[4..];

        return Enum.TryParse(name, ignoreCase: true, out ease);
    }

    private static void Unknown(string name, string kind, string known, List<string> warnings)
    {
        warnings.Add($"'{name}' is not an argument of a {kind} and was ignored. A {kind} takes: {known}.");
    }

    private static bool TryNumber(string name, string value, List<string> warnings, out double number)
    {
        // Units are how a person writes seconds; the library takes plain numbers.
        var text = value.Trim().TrimEnd('s', 'S').Trim();

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out number)) return true;

        warnings.Add($"'{value}' is not a number, so '{name}' was ignored.");

        return false;
    }

    private static double[] Numbers(string value)
    {
        return [.. value.Split(['[', ']', ',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
                ? number
                : double.NaN)
            .Where(double.IsFinite)];
    }

    /// <summary>
    /// Splits an argument list on commas, falling back to whitespace when it was written without
    /// any - which is how a spec typed by hand usually arrives.
    /// </summary>
    private static string[] SplitArguments(string arguments)
    {
        var text = arguments.Trim().Trim('(', ')');
        if (text.Length == 0) return [];

        // A bezier argument carries its own commas inside its brackets, so the comma split has to
        // respect nesting; a spec written without commas at all is split on whitespace instead.
        var parts = text.Contains(',')
            ? SplitOutsideBrackets(text)
            : text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return [.. parts.Where(part => part.Length > 0)];
    }

    private static string[] SplitOutsideBrackets(string text)
    {
        var parts = new List<string>();
        var depth = 0;
        var start = 0;

        for (int i = 0; i < text.Length; i++)
        {
            var c = text[i];

            if (c is '[' or '(') depth++;
            else if (c is ']' or ')') depth--;
            else if (c == ',' && depth == 0)
            {
                parts.Add(text[start..i].Trim());
                start = i + 1;
            }
        }

        parts.Add(text[start..].Trim());

        return [.. parts];
    }

    /// <summary>
    /// The leading token of a spec written without parentheses. A comma ends it as surely as a
    /// space does: "0.4, InOut" names a duration followed by an easing, and reading "0.4," as the
    /// kind would leave a plain tween spec unparseable.
    /// </summary>
    private static string FirstWord(string text)
    {
        var end = text.IndexOfAny([' ', '\t', ',']);

        return end < 0 ? text : text[..end];
    }

    private static string Number(double value) => value.ToString("0.####", CultureInfo.InvariantCulture);

    [GeneratedRegex(@"^\s*(Bm|Motion|BmTransition)\s*\.\s*", RegexOptions.IgnoreCase)]
    private static partial Regex QualifierRegex();
}

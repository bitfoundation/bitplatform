using System.Text;
using System.Text.RegularExpressions;

namespace Bit.Bmotion;

/// <summary>
/// Interpolates between two arbitrary CSS strings whose "shapes" match - motion.dev's complex
/// string mixer. Both strings are tokenised into literals, numbers and colors; when the literal
/// skeletons and token kinds line up, numbers lerp numerically (extrapolating for spring
/// overshoot) and colors lerp via <see cref="BmotionColorInterpolator"/>. This is what makes
/// <c>filter: "blur(10px) brightness(1)"</c>, multi-part <c>box-shadow</c>s and matching
/// gradients animate instead of snapping.
/// </summary>
internal static partial class BmotionStringMixer
{
    // Colors first so the numbers inside rgb()/oklch()/#hex are consumed as part of the color
    // token instead of being tokenised individually. Words are matched whole so a named color
    // ("0 0 8px red") becomes a color token while every other word (px, blur, inset) stays literal.
    [GeneratedRegex(@"(?<color>#[0-9a-fA-F]{3,8}\b|\b(?:rgba?|hsla?|hwb|lab|lch|oklab|oklch|color)\([^)]*\))|(?<word>[a-zA-Z][a-zA-Z-]*)|(?<num>-?\d*\.?\d+(?:[eE][-+]?\d+)?)")]
    private static partial Regex TokenRegex();

    private abstract record Token;
    private sealed record NumberToken(double Value) : Token;
    private sealed record ColorToken(double[] Channels) : Token;

    /// <summary><see cref="TryCreateMix(string, string, string?)"/> between the two <paramref name="ends"/> (see <see cref="MixEnds"/>).</summary>
    public static Func<double, string>? TryCreateMix((string From, string To) ends, string? key = null) => TryCreateMix(ends.From, ends.To, key);

    /// <summary>
    /// Builds a mix function from <paramref name="from"/> to <paramref name="to"/>, or
    /// <c>null</c> when the strings don't share an interpolatable shape (different literal
    /// skeletons, token counts or token kinds - or no tokens at all). <paramref name="key"/> is the
    /// property being mixed: it tells which numbers CSS doesn't allow below zero (see <see cref="Floored"/>).
    /// </summary>
    public static Func<double, string>? TryCreateMix(string from, string to, string? key = null)
    {
        if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to)) return null;
        if (!TryParse(from, out var fromLiterals, out var fromTokens)) return null;
        if (!TryParse(to, out var toLiterals, out var toTokens)) return null;

        if (fromTokens.Count == 0 || fromTokens.Count != toTokens.Count) return null;
        if (!fromLiterals.SequenceEqual(toLiterals, StringComparer.Ordinal)) return null;
        for (int i = 0; i < fromTokens.Count; i++)
            if (fromTokens[i].GetType() != toTokens[i].GetType()) return null;
        var floored = Floored(key, fromLiterals, fromTokens);

        return p =>
        {
            var sb = new StringBuilder(to.Length + 16);
            for (int i = 0; i < fromTokens.Count; i++)
            {
                sb.Append(fromLiterals[i]);
                switch (fromTokens[i])
                {
                    case NumberToken a when toTokens[i] is NumberToken b:
                        // Numbers extrapolate freely so spring overshoot looks natural - down to zero for the
                        // ones CSS has no negative of: below it the value is invalid, and the browser drops the
                        // frame (an inline style) or the whole keyframe (a recording).
                        double value = a.Value + (b.Value - a.Value) * p;
                        sb.Append(BmotionCssFormat.Num(floored[i] && value < 0 ? 0 : value));
                        break;
                    case ColorToken a when toTokens[i] is ColorToken b:
                        // Colors clamp: channel overshoot would wrap/saturate unpredictably.
                        sb.Append(BmotionColorInterpolator.Lerp(a.Channels, b.Channels, Math.Clamp(p, 0, 1)));
                        break;
                }
            }
            sb.Append(fromLiterals[^1]);
            return sb.ToString();
        };
    }

    // Filter functions whose argument can't be negative (hue-rotate's can).
    private static readonly string[] _nonNegativeFilters =
        ["blur(", "brightness(", "contrast(", "grayscale(", "invert(", "opacity(", "saturate(", "sepia("];

    /// <summary>
    /// Which of <paramref name="tokens"/> CSS doesn't allow below zero, so the mix stops them there when a spring
    /// undershoots: the blur radius of every shadow - the third length of each in a <c>box-shadow</c> or
    /// <c>text-shadow</c> list, and of a <c>drop-shadow()</c> - and the amount of a filter like <c>blur()</c> or
    /// <c>brightness()</c>. Offsets, spreads and angles keep their sign.
    /// </summary>
    private static bool[] Floored(string? key, List<string> literals, List<Token> tokens)
    {
        var floored = new bool[tokens.Count];
        bool shadows = string.Equals(key, "boxShadow", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(key, "textShadow", StringComparison.OrdinalIgnoreCase);
        bool filters = string.Equals(key, "filter", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(key, "backdropFilter", StringComparison.OrdinalIgnoreCase);
        if (!shadows && !filters) return floored;

        // The lengths seen so far in the current shadow. Colours are tokens, so a comma left in the skeleton is one
        // between shadows; in a filter, drop-shadow( starts a shadow and the next closing parenthesis ends it.
        int lengths = 0;
        bool inShadow = shadows;
        for (int i = 0; i < tokens.Count; i++)
        {
            var before = literals[i];
            if (filters)
            {
                if (before.Contains(')')) inShadow = false;
                if (before.Contains("drop-shadow(", StringComparison.OrdinalIgnoreCase)) { inShadow = true; lengths = 0; }
            }
            else if (before.Contains(',')) lengths = 0;

            if (tokens[i] is not NumberToken) continue;
            if (inShadow)
                floored[i] = ++lengths == 3;
            else
            {
                var name = before.TrimEnd();
                floored[i] = Array.Exists(_nonNegativeFilters, f => name.EndsWith(f, StringComparison.OrdinalIgnoreCase));
            }
        }
        return floored;
    }

    // Properties whose "none" is the identity of a value list: no filter, no shadow. CSS interpolates "none" there
    // as the other end with every function or shadow at its neutral value, and so does the mixer.
    private static readonly HashSet<string> _noneIsNeutral = new(StringComparer.OrdinalIgnoreCase)
    {
        "filter", "backdropFilter", "boxShadow", "textShadow",
    };

    // Filter functions whose neutral amount is 1 (100%) rather than 0.
    private static readonly string[] _unitAmountFilters = ["brightness(", "contrast(", "saturate(", "opacity("];

    /// <summary>
    /// The two ends <paramref name="key"/> mixes between. Where one end is <c>none</c> on a property whose
    /// <c>none</c> is a neutral value list (filters, shadows), the other end at its neutral values instead -
    /// <c>none</c> → <c>blur(8px) brightness(1.5)</c> plays from <c>blur(0px) brightness(1)</c>, and a shadow grows
    /// out of a transparent zero-size one, as CSS interpolates them. Shadows are also put in one form - the page
    /// reports <c>rgba(0, 0, 0, 0.3) 0px 4px 12px 0px</c> for an authored <c>0 4px 12px rgba(0,0,0,.3)</c> - and a
    /// shorter shadow list is padded with transparent zero-size shadows. Otherwise the ends as given.
    /// </summary>
    public static (string From, string To) MixEnds(string key, string from, string to)
    {
        if (!_noneIsNeutral.Contains(key)) return (from, to);
        bool text = string.Equals(key, "textShadow", StringComparison.OrdinalIgnoreCase);
        if ((text || string.Equals(key, "boxShadow", StringComparison.OrdinalIgnoreCase))
            && AlignShadows(from, to, text) is { } aligned)
            return aligned;
        bool fromNone = IsNone(from), toNone = IsNone(to);
        if (fromNone == toNone) return (from, to);
        return fromNone
            ? (Neutral(to) ?? from, to)
            : (from, Neutral(from) ?? to);

        static bool IsNone(string value) => string.Equals(value.Trim(), "none", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record Shadow(bool Inset, string[] Lengths, string Color);

    // Both shadow lists in the one form the mixer lines up, the shorter padded; null when either isn't a list of
    // shadows it can parse (a shadow without a colour is currentcolor, which it can't mix), or two shadows at the
    // same place differ in inset-ness (CSS doesn't interpolate those either).
    private static (string From, string To)? AlignShadows(string from, string to, bool text)
    {
        if (ParseShadows(from, text) is not { } a || ParseShadows(to, text) is not { } b) return null;
        if (a.Count == 0 && b.Count == 0) return null;
        for (int i = 0; i < Math.Min(a.Count, b.Count); i++)
            if (a[i].Inset != b[i].Inset) return null;
        while (a.Count < b.Count) a.Add(NeutralShadow(b[a.Count]));
        while (b.Count < a.Count) b.Add(NeutralShadow(a[b.Count]));
        return (Format(a), Format(b));

        static Shadow NeutralShadow(Shadow like)
            => new(like.Inset, Array.ConvertAll(like.Lengths, l => "0" + l.TrimStart('-', '+', '.', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9')), "rgba(0,0,0,0)");

        static string Format(List<Shadow> shadows)
            => string.Join(", ", shadows.Select(s => (s.Inset ? "inset " : "") + string.Join(" ", s.Lengths) + " " + s.Color));
    }

    // "none" is no shadows; otherwise each comma-separated shadow's inset keyword, offsets, blur and spread (missing
    // ones zero) and colour, in any order.
    private static List<Shadow>? ParseShadows(string value, bool text)
    {
        value = value.Trim();
        if (string.Equals(value, "none", StringComparison.OrdinalIgnoreCase)) return [];
        var shadows = new List<Shadow>();
        foreach (var part in SplitTopLevel(value, ','))
        {
            bool inset = false;
            string? color = null;
            var lengths = new List<string>(4);
            foreach (var token in SplitTopLevel(part, ' '))
            {
                if (token.Length == 0) continue;
                if (string.Equals(token, "inset", StringComparison.OrdinalIgnoreCase) && !text && !inset) { inset = true; continue; }
                if (BmotionElementAnimationState.TryParseCssDimension(token, out double number, out string unit)
                    && (unit.Length > 0 && unit.All(char.IsAsciiLetter) || number == 0 && unit.Length == 0))
                {
                    lengths.Add(unit.Length == 0 ? "0px" : token);
                    continue;
                }
                if (color is null && BmotionColorInterpolator.Parse(token) is not null) { color = token; continue; }
                return null;
            }
            int max = text ? 3 : 4;
            if (color is null || lengths.Count < 2 || lengths.Count > max) return null;
            while (lengths.Count < max) lengths.Add("0px");
            shadows.Add(new Shadow(inset, [.. lengths], color));
        }
        return shadows;
    }

    // Splits on separator outside parentheses (commas between shadows, spaces between a shadow's parts).
    private static List<string> SplitTopLevel(string value, char separator)
    {
        var parts = new List<string>();
        int depth = 0, start = 0;
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (c == '(') depth++;
            else if (c == ')') depth--;
            else if (depth == 0 && (c == separator || separator == ' ' && char.IsWhiteSpace(c)))
            {
                parts.Add(value[start..i].Trim());
                start = i + 1;
            }
        }
        parts.Add(value[start..].Trim());
        return parts;
    }

    /// <summary>
    /// <paramref name="value"/> with every number at its neutral value (0, or 1 / 100% for an amount filter such as
    /// <c>brightness()</c>) and every colour transparent; <c>null</c> when it has nothing to neutralise.
    /// </summary>
    internal static string? Neutral(string value)
    {
        if (!TryParse(value, out var literals, out var tokens) || tokens.Count == 0) return null;
        var sb = new StringBuilder(value.Length);
        for (int i = 0; i < tokens.Count; i++)
        {
            sb.Append(literals[i]);
            if (tokens[i] is ColorToken)
            {
                sb.Append("rgba(0,0,0,0)");
                continue;
            }
            var before = literals[i].TrimEnd();
            bool unitAmount = Array.Exists(_unitAmountFilters, f => before.EndsWith(f, StringComparison.OrdinalIgnoreCase));
            sb.Append(!unitAmount ? "0" : literals[i + 1].StartsWith('%') ? "100" : "1");
        }
        sb.Append(literals[^1]);
        return sb.ToString();
    }

    /// <summary>
    /// Splits <paramref name="value"/> into <c>literals[0] token[0] literals[1] token[1] …
    /// literals[n]</c> (always one more literal than tokens; literals may be empty strings).
    /// Returns <c>false</c> when a color token fails to parse.
    /// </summary>
    private static bool TryParse(string value, out List<string> literals, out List<Token> tokens)
    {
        literals = new List<string>();
        tokens = new List<Token>();

        int pos = 0;
        foreach (Match m in TokenRegex().Matches(value))
        {
            // A word that isn't a color name is part of the literal skeleton around the tokens.
            if (m.Groups["word"].Success && !BmotionColorInterpolator.IsNamedColor(m.Value)) continue;
            literals.Add(value[pos..m.Index]);
            if (m.Groups["color"].Success || m.Groups["word"].Success)
            {
                var channels = BmotionColorInterpolator.Parse(m.Value);
                if (channels is null) return false; // malformed color ⇒ not mixable
                tokens.Add(new ColorToken(channels));
            }
            else
            {
                if (!double.TryParse(m.Value, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out double num))
                    return false;
                tokens.Add(new NumberToken(num));
            }
            pos = m.Index + m.Length;
        }
        literals.Add(value[pos..]);
        return true;
    }
}

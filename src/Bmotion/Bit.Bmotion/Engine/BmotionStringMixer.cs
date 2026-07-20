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
    // Colors first so the numbers inside rgb()/hsl()/#hex are consumed as part of the color
    // token instead of being tokenised individually.
    [GeneratedRegex(@"(?<color>#[0-9a-fA-F]{3,8}\b|(?:rgba?|hsla?)\([^)]*\))|(?<num>-?\d*\.?\d+)")]
    private static partial Regex TokenRegex();

    private abstract record Token;
    private sealed record NumberToken(double Value) : Token;
    private sealed record ColorToken(double[] Channels) : Token;

    /// <summary>
    /// Builds a mix function from <paramref name="from"/> to <paramref name="to"/>, or
    /// <c>null</c> when the strings don't share an interpolatable shape (different literal
    /// skeletons, token counts or token kinds - or no tokens at all).
    /// </summary>
    public static Func<double, string>? TryCreateMix(string from, string to)
    {
        if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to)) return null;
        if (!TryParse(from, out var fromLiterals, out var fromTokens)) return null;
        if (!TryParse(to, out var toLiterals, out var toTokens)) return null;

        if (fromTokens.Count == 0 || fromTokens.Count != toTokens.Count) return null;
        if (!fromLiterals.SequenceEqual(toLiterals, StringComparer.Ordinal)) return null;
        for (int i = 0; i < fromTokens.Count; i++)
            if (fromTokens[i].GetType() != toTokens[i].GetType()) return null;

        return p =>
        {
            var sb = new StringBuilder(to.Length + 16);
            for (int i = 0; i < fromTokens.Count; i++)
            {
                sb.Append(fromLiterals[i]);
                switch (fromTokens[i])
                {
                    case NumberToken a when toTokens[i] is NumberToken b:
                        // Numbers extrapolate freely so spring overshoot looks natural.
                        sb.Append(BmotionCssFormat.Num(a.Value + (b.Value - a.Value) * p));
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
            literals.Add(value[pos..m.Index]);
            if (m.Groups["color"].Success)
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

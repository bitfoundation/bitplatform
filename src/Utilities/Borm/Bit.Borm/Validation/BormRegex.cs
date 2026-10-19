using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Bit.Borm;

/// <summary>
/// Creates and caches regular expressions used by schemas. Patterns frequently arrive from untrusted
/// JSON Schema documents, so every expression is built with a match timeout (guarding against
/// catastrophic backtracking) and invalid patterns degrade to "no constraint" instead of throwing
/// while a schema is being imported.
/// </summary>
internal static class BormRegex
{
    /// <summary>How long a single match may run before it is abandoned.</summary>
    public static readonly TimeSpan MatchTimeout = TimeSpan.FromMilliseconds(250);

    private static readonly ConcurrentDictionary<string, Regex?> Cache = new(StringComparer.Ordinal);

    /// <summary>Returns a cached regex for <paramref name="pattern"/>, or null if the pattern does not compile.</summary>
    public static Regex? Create(string? pattern)
    {
        if (string.IsNullOrEmpty(pattern)) return null;
        return Cache.GetOrAdd(pattern, static p =>
        {
            try
            {
                return new Regex(p, RegexOptions.CultureInvariant, MatchTimeout);
            }
            catch (ArgumentException)
            {
                return null;
            }
        });
    }

    /// <summary>
    /// Runs <paramref name="regex"/> against <paramref name="input"/>. Returns null when the match
    /// times out, letting callers distinguish "did not match" from "could not be checked".
    /// </summary>
    public static bool? IsMatch(Regex? regex, string input)
    {
        if (regex is null) return true;
        try
        {
            return regex.IsMatch(input);
        }
        catch (RegexMatchTimeoutException)
        {
            return null;
        }
    }
}

using System.Text.RegularExpressions;

namespace Bit.Brouter;

/// <summary>
/// A validation-only built-in constraint: it accepts or rejects the raw segment text and leaves
/// the bound value untouched (framework parity - only type constraints such as int/guid convert).
/// </summary>
internal sealed class BrouterPredicateRouteConstraint : BrouterRouteConstraint
{
    private readonly Func<string, bool> _predicate;
    private readonly bool _matchesMissingValue;

    internal override bool ConvertsValue => false;
    internal override bool MatchesMissingValue => _matchesMissingValue;

    public BrouterPredicateRouteConstraint(Func<string, bool> predicate, bool matchesMissingValue = false)
    {
        _predicate = predicate;
        _matchesMissingValue = matchesMissingValue;
    }

    public override bool TryMatch(string pathSegment, out object? convertedValue)
    {
        convertedValue = pathSegment;
        return _predicate(pathSegment);
    }

    /// <summary>
    /// Framework-parity file-name test (see ASP.NET Core's FileNameRouteConstraint): the text after
    /// the last '/' is a file name when it contains a '.' followed by at least one non-'.' character
    /// ("a.txt" and ".gitignore" are file names; "a", "a." and "a..." are not).
    /// </summary>
    internal static bool IsFileName(ReadOnlySpan<char> value)
    {
        var lastSlashIndex = value.LastIndexOf('/');
        if (lastSlashIndex >= 0) value = value[(lastSlashIndex + 1)..];

        var dotIndex = value.IndexOf('.');
        if (dotIndex < 0) return false;

        for (var i = dotIndex + 1; i < value.Length; i++)
        {
            if (value[i] != '.') return true;
        }

        return false;
    }
}

/// <summary>Validation-only regex constraint backing the built-in <c>regex(...)</c> token.</summary>
internal sealed class BrouterRegexRouteConstraint : BrouterRouteConstraint
{
    private readonly Regex _regex;

    internal override bool ConvertsValue => false;

    public BrouterRegexRouteConstraint(string pattern) =>
        // Framework parity: case-insensitive, culture-invariant, bounded execution time. The
        // pattern is used verbatim (unanchored), exactly like the built-in router's inline regex.
        _regex = new Regex(pattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10));

    public override bool TryMatch(string pathSegment, out object? convertedValue)
    {
        convertedValue = pathSegment;
        return _regex.IsMatch(pathSegment);
    }
}

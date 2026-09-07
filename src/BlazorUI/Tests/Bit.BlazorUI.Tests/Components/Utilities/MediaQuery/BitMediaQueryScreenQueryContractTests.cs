using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.MediaQuery;

/// <summary>
/// The media query a <see cref="BitScreenQuery"/> stands for is written down three times: as the enum
/// member, as the case that builds it in BitMediaQuery.ts, and as the "[@media ...]" line in the XML
/// documentation the site and the MCP server answer with. Nothing but a name ties the three together,
/// and a member with no case on the JS side resolves to no query at all - the component then silently
/// never listens. These tests read the TypeScript source and the generated documentation to fail on
/// any drift between them.
/// </summary>
[TestClass]
public sealed class BitMediaQueryScreenQueryContractTests
{
    // case 'Md': return `${min(bp.md)} and ${max(bp.lg)}`;   /   case 'Xxl': return min(bp.xxl);
    private static readonly Regex CaseLine = new(@"case '(?<name>\w+)':\s*return\s+(?<body>[^;]+);", RegexOptions.Compiled);

    // The entries of the _defaultBreakpoints table: "xs: '0'," through "xxl: '2560px',".
    private static readonly Regex DefaultBreakpoint = new(@"^\s*(?<key>xs|sm|md|lg|xl|xxl):\s*'(?<value>[^']*)',", RegexOptions.Compiled | RegexOptions.Multiline);

    // The documented query of an enum member: "... query: [@media screen and (min-width: 960px)]".
    private static readonly Regex DocumentedQuery = new(@"\[@media (?<query>[^\]]+)\]", RegexOptions.Compiled);

    private static string ReadTypeScript()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "ts-sources", "BitMediaQuery.ts");
        Assert.IsTrue(File.Exists(path), $"Missing {path}; ensure BitMediaQuery.ts is copied to output by the test csproj.");

        return File.ReadAllText(path);
    }

    private static string ReadBuildScreenQueryBody()
    {
        var source = ReadTypeScript();

        var start = source.IndexOf("private static buildScreenQuery", StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, "buildScreenQuery is gone from BitMediaQuery.ts; this contract needs to follow it.");

        var end = source.IndexOf("private static resolveBreakpoints", StringComparison.Ordinal);
        Assert.IsTrue(end > start, "resolveBreakpoints no longer follows buildScreenQuery; the slice below would read the wrong code.");

        return source[start..end];
    }

    private static Dictionary<string, string> ReadDefaultBreakpoints()
    {
        var source = ReadTypeScript();

        var start = source.IndexOf("_defaultBreakpoints", StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, "_defaultBreakpoints is gone from BitMediaQuery.ts; this contract needs to follow it.");

        var end = source.IndexOf("};", start, StringComparison.Ordinal);
        Assert.IsTrue(end > start, "The _defaultBreakpoints table is not an object literal any more.");

        return DefaultBreakpoint.Matches(source[start..end])
                                .ToDictionary(m => m.Groups["key"].Value, m => m.Groups["value"].Value, StringComparer.Ordinal);
    }

    private static double ReadRangeEpsilon()
    {
        var source = ReadTypeScript();

        var match = Regex.Match(source, @"_rangeEpsilon\s*=\s*(?<value>[\d.]+)\s*;");
        Assert.IsTrue(match.Success, "_rangeEpsilon is gone from BitMediaQuery.ts; this contract needs to follow it.");

        return double.Parse(match.Groups["value"].Value, CultureInfo.InvariantCulture);
    }



    [TestMethod]
    public void EveryScreenQueryMemberIsBuiltByTheJsSide()
    {
        var cases = CaseLine.Matches(ReadBuildScreenQueryBody())
                            .Select(m => m.Groups["name"].Value)
                            .ToHashSet(StringComparer.Ordinal);

        var missing = Enum.GetNames<BitScreenQuery>().Where(n => cases.Contains(n) is false).ToArray();

        CollectionAssert.AreEqual(Array.Empty<string>(), missing,
            $"BitScreenQuery members with no case in BitMediaQuery.ts, which resolve to no query and never listen: {string.Join(", ", missing)}");
    }

    [TestMethod]
    public void EveryJsCaseIsAScreenQueryMember()
    {
        var names = Enum.GetNames<BitScreenQuery>().ToHashSet(StringComparer.Ordinal);

        var extra = CaseLine.Matches(ReadBuildScreenQueryBody())
                            .Select(m => m.Groups["name"].Value)
                            .Where(n => names.Contains(n) is false)
                            .ToArray();

        CollectionAssert.AreEqual(Array.Empty<string>(), extra,
            $"Cases in BitMediaQuery.ts naming no BitScreenQuery member (a renamed or removed one): {string.Join(", ", extra)}");
    }

    [TestMethod]
    public void JsFallbackBreakpointsMatchTheThemeDefaults()
    {
        // The JS table only answers for a --bit-bp-* variable that resolves to nothing, so it has to
        // be the same scale the theme publishes - otherwise a stylesheet that failed to load moves
        // every predefined query instead of leaving it where it was.
        var fallbacks = ReadDefaultBreakpoints();

        var expected = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["xs"] = BitThemeBreakpointDefaults.Xs,
            ["sm"] = BitThemeBreakpointDefaults.Sm,
            ["md"] = BitThemeBreakpointDefaults.Md,
            ["lg"] = BitThemeBreakpointDefaults.Lg,
            ["xl"] = BitThemeBreakpointDefaults.Xl,
            ["xxl"] = BitThemeBreakpointDefaults.Xxl,
        };

        Assert.AreEqual(expected.Count, fallbacks.Count, "The JS fallback table no longer carries one entry per breakpoint.");

        foreach (var (key, value) in expected)
        {
            Assert.IsTrue(fallbacks.TryGetValue(key, out var fallback), $"The JS fallback table has no '{key}' entry.");
            Assert.AreEqual(ToPixels(value), ToPixels(fallback!), $"The JS fallback for '{key}' is not the theme default.");
        }
    }

    [TestMethod]
    public void RangeBoundsAreCloseEnoughToLeaveNoWidthUnmatched()
    {
        var epsilon = ReadRangeEpsilon();

        // A whole pixel would leave a gap no side of the scale matches, since a viewport is not always
        // a whole number of CSS pixels; too small a one is rounded away by the browsers. Bootstrap's
        // hundredths are the value the ecosystem settled on for exactly these two reasons.
        Assert.IsTrue(epsilon > 0 && epsilon <= 0.02,
            $"The exclusive upper bound is kept {epsilon}px below the next breakpoint, which is outside the range that both closes the gap and stays below the edge.");
    }

    [TestMethod]
    public void DocumentedQueriesAreWhatTheJsSideBuilds()
    {
        var breakpoints = ReadDefaultBreakpoints();
        var epsilon = ReadRangeEpsilon();
        var body = ReadBuildScreenQueryBody();
        var documentation = ReadEnumDocumentation();

        foreach (var name in Enum.GetNames<BitScreenQuery>())
        {
            var match = CaseLine.Matches(body).SingleOrDefault(m => m.Groups["name"].Value == name);
            Assert.IsNotNull(match, $"BitScreenQuery.{name} has no case in BitMediaQuery.ts.");

            var built = $"screen and {EvaluateCaseBody(match!.Groups["body"].Value, breakpoints, epsilon)}";

            Assert.IsTrue(documentation.TryGetValue(name, out var documented),
                $"BitScreenQuery.{name} carries no XML documentation, so nothing says what it matches.");

            var documentedQuery = DocumentedQuery.Match(documented!);
            Assert.IsTrue(documentedQuery.Success,
                $"The documentation of BitScreenQuery.{name} names no [@media ...] query.");

            Assert.AreEqual(built, documentedQuery.Groups["query"].Value.Trim(),
                $"The documented query of BitScreenQuery.{name} is not the one BitMediaQuery.ts builds from the default breakpoints.");
        }
    }



    // Reads the summary of every BitScreenQuery member out of the XML documentation the library
    // ships - the same file the docs site and the MCP server answer from.
    private static Dictionary<string, string> ReadEnumDocumentation()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Bit.BlazorUI.xml");
        Assert.IsTrue(File.Exists(path), $"Missing {path}; ensure the library's XML documentation is copied to output.");

        var prefix = $"F:{typeof(BitScreenQuery).FullName}.";

        return XDocument.Load(path)
                        .Descendants("member")
                        .Where(m => m.Attribute("name")?.Value.StartsWith(prefix, StringComparison.Ordinal) == true)
                        .ToDictionary(m => m.Attribute("name")!.Value[prefix.Length..],
                                      m => string.Concat(m.Element("summary")?.Nodes().Select(n => n.ToString()) ?? []).Trim(),
                                      StringComparer.Ordinal);
    }

    // Evaluates the template literal a case returns, which is built from nothing but the three
    // helpers of buildScreenQuery over the breakpoint table: `min(bp.x)`, `max(bp.x)` and the
    // `from(bp.x)` that drops a lower bound sitting at zero.
    private static string EvaluateCaseBody(string body, Dictionary<string, string> breakpoints, double epsilon)
    {
        var evaluated = body.Trim().Trim('`');

        evaluated = Regex.Replace(evaluated, @"\$?\{?\s*min\(bp\.(?<key>\w+)\)\s*\}?", m => $"(min-width: {Breakpoint(m)})");
        evaluated = Regex.Replace(evaluated, @"\$?\{?\s*max\(bp\.(?<key>\w+)\)\s*\}?", m => $"(max-width: {Below(Breakpoint(m))})");
        evaluated = Regex.Replace(evaluated, @"\$?\{?\s*from\(bp\.(?<key>\w+)\)\s*\}?", m =>
        {
            var value = Breakpoint(m);

            return ToPixels(value) == 0 ? string.Empty : $"(min-width: {value}) and ";
        });

        Assert.IsFalse(evaluated.Contains("${", StringComparison.Ordinal),
            $"The case body '{body.Trim()}' uses something other than the min/max/from helpers, which this contract cannot evaluate.");

        return evaluated.Trim();

        string Breakpoint(Match match)
        {
            var key = match.Groups["key"].Value;
            Assert.IsTrue(breakpoints.ContainsKey(key), $"The case body names the breakpoint '{key}', which the fallback table does not carry.");

            return breakpoints[key];
        }

        string Below(string value) => $"{(ToPixels(value) - epsilon).ToString("0.##", CultureInfo.InvariantCulture)}px";
    }

    private static double ToPixels(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.EndsWith("px", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[..^2];
        }

        return double.Parse(trimmed, CultureInfo.InvariantCulture);
    }
}

using System.Text.RegularExpressions;

namespace Bit.Blow.Tests;

/// <summary>
/// Tests over the stylesheet itself. A colour hard-coded into an add-on cannot be caught by
/// rendering - it looks perfectly fine in the light theme, and only turns into a white control
/// bar sitting on a dark canvas for the people using the dark one. The only place to catch it
/// is where it is written.
/// </summary>
[TestClass]
public class ThemeTests
{
    private static readonly string Css = ReadStylesheet();

    private static string ReadStylesheet()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var path = Path.Combine(dir.FullName, "Bit.Blow", "Styles", "bit-blow.css");
            if (File.Exists(path)) return File.ReadAllText(path);
            dir = dir.Parent;
        }
        throw new FileNotFoundException("Could not find bit-blow.css above " + AppContext.BaseDirectory);
    }

    /// <summary>The stylesheet with its custom-property declarations removed.</summary>
    private static string RulesOnly() => string.Join(
        '\n',
        Css.Split('\n').Where(line => !line.TrimStart().StartsWith("--bit-blow-", StringComparison.Ordinal)));

    [TestMethod]
    public void NoRuleHardCodesAColour()
    {
        // Every colour belongs in a --bit-blow-* property, or a colour mode cannot reach it.
        var literals = Regex.Matches(RulesOnly(), @":[^;{}]*(#[0-9a-fA-F]{3,8}\b|\brgba?\()")
            .Select(m => m.Value.Trim())
            .ToList();

        Assert.IsTrue(literals.Count == 0, "colours must go through a --bit-blow-* property, but these rules hard-code one:\n  "
            + string.Join("\n  ", literals));
    }

    [TestMethod]
    // The chrome drawn over the canvas: if the dark theme misses any of these, that piece stays
    // light while everything around it goes dark.
    [DataRow("--bit-blow-surface")]
    [DataRow("--bit-blow-surface-hover")]
    [DataRow("--bit-blow-surface-muted")]
    [DataRow("--bit-blow-surface-muted-hover")]
    [DataRow("--bit-blow-surface-translucent")]
    [DataRow("--bit-blow-border")]
    [DataRow("--bit-blow-border-subtle")]
    [DataRow("--bit-blow-text")]
    [DataRow("--bit-blow-text-muted")]
    [DataRow("--bit-blow-shadow")]
    [DataRow("--bit-blow-group-bg")]
    [DataRow("--bit-blow-group-border")]
    [DataRow("--bit-blow-bg")]
    [DataRow("--bit-blow-node-bg")]
    [DataRow("--bit-blow-node-border")]
    [DataRow("--bit-blow-node-color")]
    [DataRow("--bit-blow-node-selected")]
    [DataRow("--bit-blow-node-input-border")]
    [DataRow("--bit-blow-node-output-border")]
    [DataRow("--bit-blow-edge-stroke")]
    [DataRow("--bit-blow-edge-selected")]
    [DataRow("--bit-blow-connection-stroke")]
    [DataRow("--bit-blow-handle-bg")]
    public void TheDarkThemeOverridesEveryThemeableProperty(string property)
    {
        Assert.Contains(property, DarkBlock(), StringComparison.Ordinal);
        Assert.Contains(property, SystemBlock(), StringComparison.Ordinal);
    }

    [TestMethod]
    public void TheExplicitAndSystemDarkThemesStayInStep()
    {
        // Two copies of a palette are two things to keep in step, and one of them will be
        // forgotten. Both blocks must therefore assign the same properties from the same source.
        var dark = Assignments(DarkBlock());
        var system = Assignments(SystemBlock());

        Assert.AreSequenceEqual(dark, system);
    }

    [TestMethod]
    public void EveryPropertyTheThemeOverridesHasALightDefault()
    {
        // A property assigned only in the dark block resolves to nothing in the light one, and
        // whatever uses it silently loses its colour.
        var root = Block(@"\.bit-blow \{");
        foreach (var (property, _) in Assignments(DarkBlock()))
        {
            Assert.Contains(property + ":", root, StringComparison.Ordinal);
        }
    }

    [TestMethod]
    public void TheDarkPaletteIsWrittenDownOnlyOnce()
    {
        // The two theme blocks copy a palette defined on the root, rather than each spelling the
        // colours out; otherwise "in step" above is something a human has to maintain by hand.
        foreach (var a in Assignments(DarkBlock())) Assert.StartsWith("var(--bit-blow-dark-", a.Value);
    }

    [TestMethod]
    public void TheHandleStatesEachHaveTheirOwnColour()
    {
        // Valid, rejected, and "this is where the drag came from" are three different things to
        // tell the user, so they cannot share one colour.
        Assert.Contains("--bit-blow-handle-valid:", Css, StringComparison.Ordinal);
        Assert.Contains("--bit-blow-handle-invalid:", Css, StringComparison.Ordinal);
        Assert.Contains(".bit-blow-handle-connectingfrom", Css, StringComparison.Ordinal);
    }

    // ---- helpers ----

    private static string DarkBlock() => Block(@"\.bit-blow-dark \{");

    private static string SystemBlock() => Block(@"\.bit-blow-system \{");

    /// <summary>The body of the first rule whose selector matches, up to its closing brace.</summary>
    private static string Block(string selectorPattern)
    {
        var start = Regex.Match(Css, selectorPattern);
        Assert.IsTrue(start.Success, $"no rule matching {selectorPattern} in the stylesheet");

        var from = start.Index + start.Length;
        var end = Css.IndexOf('}', from);
        Assert.IsTrue(end > from, $"rule {selectorPattern} is not closed");

        return Css[from..end];
    }

    private static List<(string Property, string Value)> Assignments(string block) =>
        Regex.Matches(block, @"(--bit-blow-[a-z-]+)\s*:\s*([^;]+);")
            .Select(m => (m.Groups[1].Value, m.Groups[2].Value.Trim()))
            .OrderBy(a => a.Item1, StringComparer.Ordinal)
            .ToList();
}

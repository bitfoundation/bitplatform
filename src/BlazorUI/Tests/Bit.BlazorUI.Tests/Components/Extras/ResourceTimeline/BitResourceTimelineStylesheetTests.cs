using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.ResourceTimeline;

/// <summary>
/// Pins the public --bit-ResourceTimeline-* variables and the rules of the stylesheet a bUnit render cannot see: every
/// variable the header documents is read with a fallback, none of them is ever declared (so they keep inheriting from
/// :root), nothing is read that the header does not document, the theme colors the script reads off the root are the
/// ones the stylesheet resolves there, and every part the markup and the scripts render is styled.
/// </summary>
[TestClass]
public partial class BitResourceTimelineStylesheetTests
{
    [TestMethod]
    public void BitResourceTimelineShouldReadEveryPublicVariableItDocuments()
    {
        var stylesheet = ReadStylesheet();

        var documented = DocumentedVariables(stylesheet);

        Assert.HasCount(28, documented, "The stylesheet does not document the 28 public variables.");

        foreach (var name in documented)
        {
            Assert.Contains($"var({name}, ", SourceFiles.StripScssComments(stylesheet), $"{name} is documented but never read with a fallback.");
        }
    }

    [TestMethod]
    public void BitResourceTimelineShouldNotReadAPublicVariableItDoesNotDocument()
    {
        var stylesheet = ReadStylesheet();

        var documented = DocumentedVariables(stylesheet);
        var read = ReadVariable().Matches(SourceFiles.StripScssComments(stylesheet)).Select(m => m.Groups[1].Value).Distinct().ToList();

        Assert.IsNotEmpty(read);

        foreach (var name in read)
        {
            Assert.Contains(name, documented, $"{name} is read but not documented in the header.");
        }
    }

    [TestMethod]
    public void BitResourceTimelineShouldNeverDeclareAPublicVariable()
    {
        var body = SourceFiles.StripScssComments(ReadStylesheet());

        Assert.IsFalse(DeclaredVariable().IsMatch(body), "A public --bit-ResourceTimeline-* variable is declared, which stops it inheriting.");
    }

    [TestMethod]
    public void BitResourceTimelineShouldResolveEveryThemeColorTheScriptReadsOnItsRoot()
    {
        var facade = ReadScript("BitResourceTimeline.ts");
        var themeColors = ThemeColor().Matches(facade).ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);

        // The script reads each color it hands the engine off a probe inside the root, by the key of the colors option.
        Assert.AreSequenceEqual(typeof(BitResourceTimelineColors).GetProperties().Select(p => JsonNamingPolicy.CamelCase.ConvertName(p.Name)).ToArray(), themeColors.Keys.ToArray(), global::Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder, "The theme colors of the script are not the colors of BitResourceTimelineColors.");

        // So each private variable it reads is resolved on the root from a public one (or the theme token behind it), and
        // the root resolves nothing the script does not read.
        var stylesheet = SourceFiles.StripScssComments(ReadStylesheet());
        var root = SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-rtm {");
        var declared = Regex.Matches(root, @"(--bit-rtm-thm-[a-z-]+):\s*([^;]+);").ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);

        Assert.AreSequenceEqual(themeColors.Values.ToArray(), declared.Keys.ToArray(), global::Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder);
        Assert.AreSequenceEqual(ThemePublicVariables.Keys.ToArray(), declared.Keys.ToArray(), global::Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder, "A theme color has no public variable pinned below.");

        foreach (var (variable, value) in declared)
        {
            // A Sass variable of the stylesheet ($rtm-focus) stands for its own value.
            var resolved = Regex.Replace(value, @"#\{(\$rtm-[a-z-]+)\}", m => Regex.Match(stylesheet, $@"\n{Regex.Escape(m.Groups[1].Value)}:\s*([^;]+);").Groups[1].Value);

            var expected = $"var({ThemePublicVariables[variable]}, ";

            Assert.IsTrue(resolved.StartsWith(expected, StringComparison.Ordinal), $"{variable} is not resolved from {ThemePublicVariables[variable]} first: {value}");
        }
    }

    // The public variable each theme color of the script is resolved from first.
    private static readonly Dictionary<string, string> ThemePublicVariables = new()
    {
        ["--bit-rtm-thm-content-bg"] = "--bit-ResourceTimeline-content-background",
        ["--bit-rtm-thm-axis-bg"] = "--bit-ResourceTimeline-axis-background",
        ["--bit-rtm-thm-axis-border"] = "--bit-ResourceTimeline-axis-border-color",
        ["--bit-rtm-thm-tick"] = "--bit-ResourceTimeline-tick-color",
        ["--bit-rtm-thm-label"] = "--bit-ResourceTimeline-label-color",
        ["--bit-rtm-thm-date-label"] = "--bit-ResourceTimeline-date-label-color",
        ["--bit-rtm-thm-grid"] = "--bit-ResourceTimeline-grid-color",
        ["--bit-rtm-thm-bar"] = "--bit-ResourceTimeline-bar-color",
        ["--bit-rtm-thm-bar-selected"] = "--bit-ResourceTimeline-bar-selected-color",
        ["--bit-rtm-thm-bar-selected-border"] = "--bit-ResourceTimeline-bar-selected-border-color",
        ["--bit-rtm-thm-bar-label"] = "--bit-ResourceTimeline-bar-label-color",
        ["--bit-rtm-thm-now"] = "--bit-ResourceTimeline-now-color",
        ["--bit-rtm-thm-selection-fill"] = "--bit-ResourceTimeline-selection-background",
        ["--bit-rtm-thm-selection-border"] = "--bit-ResourceTimeline-selection-border-color",
        ["--bit-rtm-thm-focus"] = "--bit-ResourceTimeline-focus-color",
        ["--bit-rtm-thm-tooltip-bg"] = "--bit-ResourceTimeline-tooltip-background",
        ["--bit-rtm-thm-tooltip-text"] = "--bit-ResourceTimeline-tooltip-color",
        ["--bit-rtm-thm-non-working"] = "--bit-ResourceTimeline-non-working-background",
    };

    [TestMethod]
    public void BitResourceTimelineShouldStyleEveryPartItsMarkupAndScriptsRender()
    {
        var folder = SourceFiles.GetDirectory("Bit.BlazorUI.Extras", "Components", "ResourceTimeline");

        var sources = new[] { "BitResourceTimeline.razor", "BitResourceTimeline.razor.cs" }.Select(f => SourceFiles.ReadFullPath(Path.Combine(folder, f)))
                                                                                           .Concat(Directory.EnumerateFiles(folder, "*.ts").Select(SourceFiles.ReadFullPath));

        var rendered = sources.SelectMany(source => ClassName().Matches(source).Select(m => m.Value)).ToHashSet(StringComparer.Ordinal);
        var styled = StyledClass().Matches(SourceFiles.StripScssComments(ReadStylesheet())).Select(m => m.Groups[1].Value).ToHashSet(StringComparer.Ordinal);

        Assert.IsTrue(rendered.Contains("bit-rtm") && rendered.Contains("bit-rtm-srf"), "The classes of the markup and the scripts were not found.");

        // A part drawn unstyled, or a rule for a part nothing renders any more.
        Assert.AreSequenceEqual(rendered.Order(StringComparer.Ordinal).ToArray(), styled.Order(StringComparer.Ordinal).ToArray(), global::Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder, $"Rendered only: {string.Join(", ", rendered.Except(styled))}; styled only: {string.Join(", ", styled.Except(rendered))}");
    }

    [TestMethod]
    public void BitResourceTimelineShouldBeImportedByTheExtrasStylesheet()
    {
        var components = SourceFiles.StripScssComments(SourceFiles.Read("Bit.BlazorUI.Extras", "Styles", "extra-components.scss"));

        Assert.Contains("Components/ResourceTimeline/BitResourceTimeline.scss", components, "The Extras stylesheet does not import the stylesheet of the timeline.");
    }

    [TestMethod]
    public void BitResourceTimelineShouldDimTheContentOfADisabledTimeline()
    {
        var disabled = SourceFiles.GetScssBlock(SourceFiles.StripScssComments(ReadStylesheet()), "\n.bit-rtm.bit-dis {");

        Assert.Contains("opacity: $opa-dis;", disabled);
    }

    private static string[] DocumentedVariables(string stylesheet)
    {
        return DocumentedVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct().ToArray();
    }

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI.Extras", "Components", "ResourceTimeline", "BitResourceTimeline.scss");

    private static string ReadScript(string file) => SourceFiles.Read("Bit.BlazorUI.Extras", "Components", "ResourceTimeline", file);

    [GeneratedRegex(@"^//\s+(--bit-ResourceTimeline-[a-z-]+)\s", RegexOptions.Multiline)]
    private static partial Regex DocumentedVariable();

    [GeneratedRegex(@"var\((--bit-ResourceTimeline-[a-z-]+)[,)]")]
    private static partial Regex ReadVariable();

    [GeneratedRegex(@"(^|[;{\s])--bit-ResourceTimeline-[a-z-]+\s*:", RegexOptions.Multiline)]
    private static partial Regex DeclaredVariable();

    [GeneratedRegex(@"\['(\w+)',\s*'(--bit-rtm-thm-[a-z-]+)'\]")]
    private static partial Regex ThemeColor();

    // A class of the timeline, never a variable (--bit-rtm-...) nor the prefix of an id put together at run time.
    [GeneratedRegex(@"(?<![-\w])bit-rtm(?:-[a-z]+)*(?![-\w])")]
    private static partial Regex ClassName();

    [GeneratedRegex(@"\.(bit-rtm(?:-[a-z]+)*)(?![-\w])")]
    private static partial Regex StyledClass();
}

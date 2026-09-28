using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Progress;

/// <summary>
/// Pins what a bUnit render cannot see of the progress: its public --bit-Progress-* variables, which the stylesheet
/// reads and the header comment of that stylesheet documents, and the theme tokens the parts fall back to.
/// </summary>
[TestClass]
public class BitProgressStylesheetTests
{
    private static readonly Regex PublicVariableRead = new(@"var\(\s*(--bit-Progress-[a-zA-Z0-9-]+)", RegexOptions.Compiled);
    private static readonly Regex PublicVariableDeclaration = new(@"^\s*(--bit-Progress-[a-zA-Z0-9-]+)\s*:", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex DocumentedVariable = new(@"^//\s+(--bit-Progress-[a-zA-Z0-9-]+)\s", RegexOptions.Compiled | RegexOptions.Multiline);

    [TestMethod]
    public void BitProgressShouldDocumentEveryPublicVariableItReads()
    {
        var stylesheet = ReadStylesheet();

        var read = PublicVariableRead.Matches(stylesheet).Select(m => m.Groups[1].Value).ToHashSet(StringComparer.Ordinal);
        var documented = DocumentedVariable.Matches(stylesheet).Select(m => m.Groups[1].Value).ToHashSet(StringComparer.Ordinal);

        CollectionAssert.AreEquivalent(documented.Order().ToArray(), read.Order().ToArray(),
            "The --bit-Progress-* variables the stylesheet reads and the ones its header comment documents have drifted apart.");
    }

    [TestMethod]
    public void BitProgressShouldNeverDeclareItsPublicVariables()
    {
        // Read with a fallback and never declared, so a value set on :root or on an ancestor reaches every progress
        // below it: a declaration on the progress would shadow them all.
        var declared = PublicVariableDeclaration.Matches(ReadStylesheet()).Select(m => m.Groups[1].Value).ToArray();

        CollectionAssert.AreEqual(Array.Empty<string>(), declared);
    }

    [TestMethod]
    public void BitProgressRoleClassesShouldLetTheBarColorVariableWin()
    {
        var stylesheet = ReadStylesheet();

        // The role classes are generated, so it is their template that is pinned: a role class that declared its
        // color bare would beat a --bit-Progress-bar-color set on an ancestor.
        StringAssert.Contains(stylesheet, "--bit-prb-bar-color: var(--bit-Progress-bar-color, #{role($tokens, main)});");
        StringAssert.Contains(stylesheet, "--bit-prb-bar-on-color: var(--bit-Progress-bar-text-color, #{role($tokens, on)});");
    }

    [TestMethod]
    public void BitProgressRoundedShouldWinOverTheRadiusVariable()
    {
        var rounded = GetBlock(ReadStylesheet(), "\n.bit-prb-rnd {");

        // Rounded is a parameter, so it says the last word on the corners of the bar whatever the variable says.
        StringAssert.Contains(rounded, "--bit-prb-radius: #{$shp-radius-full};");
        Assert.IsFalse(rounded.Contains("--bit-Progress-radius"), "Rounded reads the public radius variable.");
    }

    [TestMethod]
    public void BitProgressBarShouldTakeItsThicknessFromTheTrackToken()
    {
        var stylesheet = ReadStylesheet();

        foreach (var size in new[] { "sm", "md", "lg" })
        {
            StringAssert.Contains(GetBlock(stylesheet, $"\n.bit-prb-{size} {{"), $"--bit-prb-thickness: var(--bit-Progress-thickness, #{{$siz-track-{size}}});");
        }
    }

    [TestMethod]
    public void BitProgressHorizontalSweepShouldMoveAlongTheLogicalAxisOnly()
    {
        var keyframes = GetBlock(ReadStylesheet(), "@keyframes bit-prb-animation {", "\n    }");

        // A translateX is physical: in a right-to-left bar it would push the sweep off the edge it enters from.
        StringAssert.Contains(keyframes, "inset-inline-start");
        Assert.IsFalse(keyframes.Contains("translate"), "The horizontal sweep is moved by a physical transform.");
    }

    [TestMethod]
    public void BitProgressDescriptionShouldFollowTheSize()
    {
        var stylesheet = ReadStylesheet();

        StringAssert.Contains(GetBlock(stylesheet, "\n.bit-prb-des {"), "font-size: var(--bit-Progress-description-font-size, var(--bit-prb-des-fs));");
        StringAssert.Contains(GetBlock(stylesheet, "\n.bit-prb-lg {"), "--bit-prb-des-fs: #{$tg-fs-xs};");
    }

    [TestMethod]
    public void BitProgressLabelShouldWrapRatherThanBeCutOff()
    {
        // A truncated name is lost to the reader: nothing else on the page repeats it.
        var label = GetBlock(ReadStylesheet(), "\n.bit-prb-lbl {");

        Assert.IsFalse(label.Contains("text-overflow"), "The label is truncated with an ellipsis.");
        Assert.IsFalse(label.Contains("nowrap"), "The label is kept to one line.");
    }

    private static string GetBlock(string stylesheet, string selector, string terminator = "\n}")
    {
        var start = stylesheet.IndexOf(selector, StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, $"{selector.Trim()} was not found in the stylesheet.");

        var end = stylesheet.IndexOf(terminator, start, StringComparison.Ordinal);

        return stylesheet[start..end];
    }

    private static string ReadStylesheet([CallerFilePath] string thisFile = "")
    {
        var path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "..", "..", "..",
                                                 "Bit.BlazorUI", "Components", "Progress", "Progress", "BitProgress.scss"));

        Assert.IsTrue(File.Exists(path), $"Missing {path}.");

        return File.ReadAllText(path).Replace("\r\n", "\n");
    }
}

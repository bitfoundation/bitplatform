using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.TextShimmer;

/// <summary>
/// Pins the contract between the text shimmer and its stylesheet: the shimmer is painted by CSS alone, so what the
/// component writes and what the stylesheet reads have to agree, and a bUnit render cannot see the stylesheet.
/// </summary>
[TestClass]
public class BitTextShimmerStylesheetTests : BunitTestContext
{
    private static readonly Regex VariableDeclaration = new(@"^\s*(--bit-tsh-[a-z-]+)\s*:", RegexOptions.Multiline);
    private static readonly Regex VariableReference = new(@"var\((--bit-tsh-[a-z-]+)");

    // Every custom property the component writes has a default declared on the root of the shimmer itself, rather
    // than left to a fallback - which is what keeps a shimmer nested in the content of another one from inheriting
    // the values the outer one was given.
    [TestMethod]
    public void EveryVariableTheComponentWritesHasADefaultOnTheRoot()
    {
        var component = RenderComponent<BitTextShimmer>(parameters =>
        {
            parameters.Add(p => p.Text, "every variable");
            parameters.Add(p => p.Duration, 1000);
            parameters.Add(p => p.Delay, 100);
            parameters.Add(p => p.RepeatDelay, 500);
            parameters.Add(p => p.Iterations, 2);
            parameters.Add(p => p.Angle, 20);
            parameters.Add(p => p.BaseColor, "gray");
            parameters.Add(p => p.GradientColor, "white");
        });

        var written = component.Find(".bit-tsh").GetAttribute("style")!
                               .Split(';', StringSplitOptions.RemoveEmptyEntries)
                               .Select(declaration => declaration.Split(':')[0].Trim())
                               .ToArray();

        var declared = GetRootDeclarations();

        Assert.AreEqual(8, written.Length, string.Join(", ", written));
        foreach (var variable in written)
        {
            CollectionAssert.Contains(declared, variable, $"{variable} is written by the component but has no default on .bit-tsh.");
        }
    }

    // A variable read without a default would be invalid at computed-value time, and a gradient with an invalid stop
    // is no gradient at all.
    [TestMethod]
    public void EveryVariableTheStylesheetReadsHasADefaultOnTheRoot()
    {
        var declared = GetRootDeclarations();

        var read = VariableReference.Matches(ReadStylesheet()).Select(m => m.Groups[1].Value).Distinct().ToArray();

        Assert.IsTrue(read.Length > 0);
        foreach (var variable in read)
        {
            CollectionAssert.Contains(declared, variable, $"{variable} is read by the stylesheet but has no default on .bit-tsh.");
        }
    }

    // The glyphs are transparent for the band to show through, so every place the band is not painted has to give
    // them a fill back - otherwise the text is left invisible.
    [TestMethod,
        DataRow("@media (prefers-reduced-motion: reduce)"),
        DataRow("@media (forced-colors: active)"),
        DataRow("@media print"),
        DataRow("@supports not ((background-clip: text) or (-webkit-background-clip: text))"),
        DataRow(".bit-tsh.bit-tsh-sta"),
        DataRow(".bit-tsh.bit-dis")]
    public void EveryStateWithoutTheBandDrawsTheTextInAFlatColor(string rule)
    {
        var block = GetBlock(ReadStylesheet(), rule);

        StringAssert.Contains(block, "@include bit-tsh-static");
    }

    // The resting place of the band is outside the text at both ends of a sweep whatever its spread, so a wide band
    // never hangs over the first or the last letters of a shimmer that has not started, is paused or has finished.
    [TestMethod]
    public void TheBandRestsOutsideTheTextByItsSpread()
    {
        var keyframes = GetBlock(ReadStylesheet(), "@keyframes bit-tsh-anim");

        StringAssert.Contains(keyframes, "calc(150% - var(--bit-tsh-spread) * var(--bit-tsh-cycle))");
        StringAssert.Contains(keyframes, "calc(-50% + var(--bit-tsh-spread) * var(--bit-tsh-cycle))");
    }



    private static string[] GetRootDeclarations()
    {
        return VariableDeclaration.Matches(GetBlock(ReadStylesheet(), "\n.bit-tsh {")).Select(m => m.Groups[1].Value).ToArray();
    }

    private static string GetBlock(string stylesheet, string start)
    {
        var index = stylesheet.IndexOf(start, StringComparison.Ordinal);
        Assert.IsTrue(index >= 0, $"The stylesheet has no \"{start.Trim()}\" block.");

        var open = stylesheet.IndexOf('{', index + start.TrimEnd('{', ' ').Length);
        var depth = 0;
        for (var i = open; i < stylesheet.Length; i++)
        {
            if (stylesheet[i] == '{') depth++;
            else if (stylesheet[i] == '}' && --depth == 0) return stylesheet[open..(i + 1)];
        }

        Assert.Fail($"The \"{start.Trim()}\" block is not closed.");
        return string.Empty;
    }

    private static string ReadStylesheet([CallerFilePath] string thisFile = "")
    {
        var path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "..", "..", "..",
                                                 "Bit.BlazorUI.Extras", "Components", "TextShimmer", "BitTextShimmer.scss"));

        Assert.IsTrue(File.Exists(path), $"The stylesheet ({path}) is not available; this test must run from a source checkout.");

        return File.ReadAllText(path).Replace("\r\n", "\n");
    }
}

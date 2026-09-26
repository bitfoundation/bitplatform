using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Navs.Pagination;

/// <summary>
/// Pins the contract of the public --bit-Pagination-* CSS variables, which a bUnit render cannot see: they are read
/// with a fallback and never declared (so they inherit from :root, an ancestor or the Style of an instance), and the
/// parameters the instance asks for itself win over a value inherited from an ancestor.
/// </summary>
[TestClass]
public class BitPaginationStylesheetTests
{
    private static readonly string[] PublicVariables =
    [
        "--bit-Pagination-gap",
        "--bit-Pagination-color",
        "--bit-Pagination-font-size",
        "--bit-Pagination-button-size",
        "--bit-Pagination-button-radius",
        "--bit-Pagination-button-border-width",
        "--bit-Pagination-button-color",
        "--bit-Pagination-button-background",
        "--bit-Pagination-button-border-color",
        "--bit-Pagination-button-hover-color",
        "--bit-Pagination-button-hover-background",
        "--bit-Pagination-selected-color",
        "--bit-Pagination-selected-background",
        "--bit-Pagination-selected-font-weight",
        "--bit-Pagination-focus-color",
        "--bit-Pagination-input-background",
        "--bit-Pagination-input-border-color",
    ];

    [TestMethod]
    public void BitPaginationShouldReadEveryPublicVariableWithAFallbackAndNeverDeclareIt()
    {
        var stylesheet = StripComments(ReadStylesheet());

        foreach (var variable in PublicVariables)
        {
            Assert.IsTrue(stylesheet.Contains($"var({variable}, "), $"{variable} is never read with a fallback.");
            Assert.IsFalse(Regex.IsMatch(stylesheet, $@"(^|[\s;{{]){Regex.Escape(variable)}\s*:", RegexOptions.Multiline), $"{variable} is declared, which stops it from inheriting.");
        }

        // Every public variable the stylesheet reads is one the list above (and the demo page) documents.
        var read = Regex.Matches(stylesheet, @"var\((--bit-Pagination-[a-z-]+)").Select(m => m.Groups[1].Value).Distinct();

        CollectionAssert.IsSubsetOf(read.ToArray(), PublicVariables);
    }

    [TestMethod]
    public void BitPaginationShouldLetRoundedWinOverAnInheritedRadius()
    {
        var stylesheet = ReadStylesheet();

        // The radius is resolved into a private variable on the root, which the Rounded class declared after it
        // replaces on the same element, so a radius inherited from an ancestor does not undo the parameter.
        StringAssert.Contains(GetBlock(stylesheet, "\n.bit-pgn {"), "--bit-pgn-radius: var(--bit-Pagination-button-radius, #{$shp-radius-control});");
        StringAssert.Contains(GetBlock(stylesheet, "\n.bit-pgn-rnd {"), "--bit-pgn-radius: #{$shp-radius-full};");

        Assert.IsTrue(stylesheet.IndexOf("\n.bit-pgn {", System.StringComparison.Ordinal) < stylesheet.IndexOf("\n.bit-pgn-rnd {", System.StringComparison.Ordinal));

        StringAssert.Contains(GetBlock(stylesheet, "\n.bit-pgn-btn {"), "border-radius: var(--bit-pgn-radius);");
        StringAssert.Contains(GetBlock(stylesheet, "\n.bit-pgn-elp {"), "border-radius: var(--bit-pgn-radius);");
    }

    [TestMethod]
    public void BitPaginationShouldSizeEveryControlFromOneResolvedVariable()
    {
        var stylesheet = ReadStylesheet();

        StringAssert.Contains(GetBlock(stylesheet, "\n.bit-pgn {"), "--bit-pgn-btn-size: var(--bit-Pagination-button-size, var(--bit-pgn-size-btn));");

        foreach (var size in new[] { "sm", "md", "lg" })
        {
            var block = GetBlock(stylesheet, $"\n.bit-pgn-{size} {{");

            StringAssert.Contains(block, $"--bit-pgn-size-btn: #{{$siz-ctrl-{size}}};");
            StringAssert.Contains(block, $"--bit-pgn-pad-x: #{{$siz-ctrl-pad-x-{size}}};");
        }
    }

    private static string StripComments(string stylesheet)
    {
        return Regex.Replace(stylesheet, @"//[^\n]*", string.Empty);
    }

    private static string GetBlock(string stylesheet, string selector)
    {
        var start = stylesheet.IndexOf(selector, System.StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, $"{selector.Trim()} was not found in the stylesheet.");

        var end = stylesheet.IndexOf("\n}", start, System.StringComparison.Ordinal);

        return stylesheet[start..end];
    }

    private static string ReadStylesheet([CallerFilePath] string thisFile = "")
    {
        var path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "..", "..", "..",
                                                 "Bit.BlazorUI", "Components", "Navs", "Pagination", "BitPagination.scss"));

        Assert.IsTrue(File.Exists(path), $"Missing {path}.");

        return File.ReadAllText(path).Replace("\r\n", "\n");
    }
}

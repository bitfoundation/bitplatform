using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// Pins that the stacking of the library's surfaces goes through its layer scale - the --bit-zin-* tokens of the theme
/// (base 1000, modal 1100, overlay 1200, callout 1300, snackbar 1400), which an app moves with BitThemeZIndices. A
/// literal at or above the bottom of that scale bypasses it: an app that moves the scale leaves it where it was, above or
/// below the layers it was meant to sit among. A z-index under the scale stays local to the component (the order of its
/// own parts), which is what a literal is for.
/// </summary>
[TestClass]
public partial class BitZIndexLayerContractTests
{
    // The package folders, the AI one holding every AI package.
    private static readonly string[] Packages = ["Bit.BlazorUI", "Bit.BlazorUI.Extras", "Bit.BlazorUI.Legacy", "Bit.BlazorUI.AI"];

    private static readonly string[] SkippedFolders = ["obj", "bin", "node_modules", "wwwroot"];

    // The bottom of the layer scale, the base layer (--bit-zin-base).
    private const int ScaleFloor = 1000;

    [TestMethod]
    public void NoStylesheetShouldHardCodeAZIndexInTheRangeOfTheLayerScale()
    {
        var problems = new List<string>();

        foreach (var file in Stylesheets())
        {
            var body = SourceFiles.StripScssComments(SourceFiles.ReadFullPath(file));

            foreach (Match match in LiteralZIndex().Matches(body))
            {
                var value = long.Parse(match.Groups["value"].Value);

                if (Math.Abs(value) >= ScaleFloor)
                {
                    problems.Add($"{Path.GetRelativePath(SourceFiles.Root, file)}: {match.Value.Trim()}");
                }
            }
        }

        Assert.AreEqual(0, problems.Count, $"These z-indexes are literals in the range of the layer scale; use a --bit-zin-* token ($zindex-*) instead:{Environment.NewLine}{string.Join(Environment.NewLine, problems)}");
    }

    [TestMethod]
    [DataRow("Bit.BlazorUI.Extras/Components/DataGrid/BitDataGrid.scss", ".bit-dtg-resize-overlay")]
    [DataRow("Bit.BlazorUI.Extras/Components/AppShell/BitAppShell.scss", "$bit-ash-inset-zindex")]
    public void TheLayersAboveEverythingShouldSitRightAboveTheTopOfTheScale(string stylesheet, string start)
    {
        // The catcher of a column resize and the bars over the device's insets are above everything the library
        // renders: one above the snackbar's layer, wherever an app moves it.
        var body = SourceFiles.StripScssComments(SourceFiles.Read(stylesheet.Split('/')));
        string declaration;

        if (start.StartsWith('$'))
        {
            var at = body.IndexOf(start + ":", StringComparison.Ordinal);
            Assert.IsTrue(at >= 0, $"{start} is not declared in {stylesheet}.");
            declaration = body[at..(body.IndexOf(';', at) + 1)];
        }
        else
        {
            declaration = SourceFiles.GetScssBlock(body, start, $"{start} is not in {stylesheet}.");
        }

        StringAssert.Contains(declaration, "calc(#{$zindex-snackbar} + 1)", $"{start} does not sit right above the snackbar's layer.");
    }

    private static IEnumerable<string> Stylesheets()
    {
        return Packages.SelectMany(package => Directory.EnumerateFiles(Path.Combine(SourceFiles.Root, package), "*.scss", SearchOption.AllDirectories))
                       .Where(file => file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(SkippedFolders.Contains) is false);
    }

    // A z-index declared as a number, or a Sass variable for one given a number (`$x-zindex: 999999;`).
    [GeneratedRegex(@"(?:z-index|zindex[\w-]*)\s*:\s*(?<value>-?\d+)\s*[;}!]", RegexOptions.IgnoreCase)]
    private static partial Regex LiteralZIndex();
}

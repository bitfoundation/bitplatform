using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// Pins the one way a part hides, under forced colors, the border it reserves with a transparent one: the
/// forced-hidden-border mixin of functions.scss. The mode forces every border color to a system one and keeps no
/// transparency for it, so a border meant for one row or tab (the selection) or for none shows on all of them; each
/// component includes the mixin in its forced-colors block rather than writing the Canvas border out again.
/// </summary>
[TestClass]
public class BitForcedHiddenBorderStylesheetTests
{
    private const string Include = "@include forced-hidden-border;";

    [TestMethod]
    public void ForcedHiddenBorderShouldDrawTheBorderInCanvasAndLeaveTheQueryToItsCaller()
    {
        var mixin = SourceFiles.GetScssBlock(ReadFunctions(), "@mixin forced-hidden-border {");

        // Canvas is what the mode paints the part's background in, so the border vanishes and keeps its width. The
        // mixin is included inside a forced-colors block, where its caller orders it ahead of the edge that stays.
        StringAssert.Contains(mixin, "border-color: Canvas;");
        Assert.IsFalse(mixin.Contains("@media"), "The mixin carries a media query, which would nest in its caller's forced-colors block.");
        Assert.IsFalse(mixin.Contains("width"), "The mixin changes the border's width, which would move the content when the border comes back.");
    }

    [TestMethod,
        DataRow("Navs/Nav/BitNav.scss", ".bit-nav-ict"),
        DataRow("Navs/Breadcrumb/BitBreadcrumb.scss", ".bit-brc-ofi, .bit-brc-ofn"),
        DataRow("Navs/Pivot/BitPivot.scss", "#{$itm}")]
    public void APartReservingABorderShouldHideItInForcedColors(string path, string part)
    {
        var stylesheet = SourceFiles.Read(["Bit.BlazorUI", "Components", .. path.Split('/')]);

        // The rule of that part inside a forced-colors block (the Pivot's is in its outline-edges mixin) that includes it,
        // ahead of anything else it declares, so an edge it keeps is set after the mixin rather than undone by it.
        var hiding = SourceFiles.GetScssRules(stylesheet)
            .Where(rule => rule.Ancestors.Contains("@media (forced-colors: active)"))
            .Where(rule => Regex.Replace(rule.Header, @"\s+", " ") == part)
            .Where(rule => rule.Declarations.Contains(Include.TrimEnd(';')))
            .ToArray();

        Assert.AreEqual(1, hiding.Length, $"{path} has no single forced-colors rule for {part} that includes the mixin.");
        Assert.AreEqual(Include.TrimEnd(';'), hiding[0].Declarations[0], $"{path} hides the border of {part} after another declaration.");
    }

    [TestMethod]
    public void NoComponentShouldWriteOutAHiddenBorderOfItsOwn()
    {
        var offenders = new[] { "Bit.BlazorUI", "Bit.BlazorUI.Extras", "Bit.BlazorUI.Legacy" }
            .Select(project => SourceFiles.GetPath(project, "Components"))
            .Where(Directory.Exists)
            .SelectMany(dir => Directory.EnumerateFiles(dir, "*.scss", SearchOption.AllDirectories))
            .Where(file => SourceFiles.StripScssComments(SourceFiles.ReadFullPath(file)).Contains("border-color: Canvas;"))
            .Select(file => Path.GetRelativePath(SourceFiles.Root, file))
            .ToArray();

        Assert.AreEqual(0, offenders.Length, $"These stylesheets paint a border Canvas by hand instead of including forced-hidden-border: {string.Join(", ", offenders)}");
    }

    private static string ReadFunctions() => SourceFiles.Read("Bit.BlazorUI", "Styles", "functions.scss");
}

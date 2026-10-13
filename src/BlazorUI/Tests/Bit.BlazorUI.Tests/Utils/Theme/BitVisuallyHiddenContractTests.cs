using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// Pins the one way a component hides what only a screen reader meets - a description pointed at through
/// aria-describedby, a live region, the name of a part that shows none: the visually-hidden mixin of functions.scss,
/// and visually-hidden-control for a native control kept for the browser's behavior. A component includes them rather
/// than writing the block out again, so a change to the technique is made once and the copies cannot drift (#13448).
/// </summary>
[TestClass]
public partial class BitVisuallyHiddenContractTests
{
    [TestMethod]
    public void VisuallyHiddenShouldClipOnePixelOutOfTheFlow()
    {
        var mixin = SourceFiles.GetScssDeclarations(ReadFunctions(), "@mixin visually-hidden {");

        // A box sized to nothing is dropped from the accessibility tree by some screen readers, so it keeps a pixel
        // and is clipped away from it; display:none and visibility:hidden would hide it from them as well.
        StringAssert.Contains(mixin, "width: 1px;");
        StringAssert.Contains(mixin, "height: 1px;");
        StringAssert.Contains(mixin, "margin: -1px;");
        StringAssert.Contains(mixin, "overflow: hidden;");
        StringAssert.Contains(mixin, "position: absolute;");
        StringAssert.Contains(mixin, "white-space: nowrap;");
        StringAssert.Contains(mixin, "clip-path: inset(50%);");
        Assert.IsFalse(mixin.Contains("display: none"));
        Assert.IsFalse(mixin.Contains("visibility: hidden"));
    }

    [TestMethod]
    public void VisuallyHiddenControlShouldBeHiddenTheSameWayAndNeverPaintedOrPointedAt()
    {
        var mixin = SourceFiles.GetScssDeclarations(ReadFunctions(), "@mixin visually-hidden-control {");

        StringAssert.Contains(mixin, "@include visually-hidden;");
        StringAssert.Contains(mixin, "opacity: 0;");
        StringAssert.Contains(mixin, "pointer-events: none;");
    }

    [TestMethod,
        DataRow("Inputs/Checkbox/BitCheckbox.scss", ".bit-chb-inp {"),
        DataRow("Inputs/ChoiceGroup/BitChoiceGroup.scss", ".bit-chg-inp {")]
    public void ANativeControlKeptForTheBrowsersBehaviorShouldBeHiddenAsAControl(string path, string rule)
    {
        var declarations = SourceFiles.GetScssDeclarations(SourceFiles.Read(["Bit.BlazorUI", "Components", .. path.Split('/')]), rule);

        StringAssert.Contains(declarations, "@include visually-hidden-control;");
    }

    [TestMethod]
    public void ASkipLinkShownOnFocusShouldTakeBackWhatTheMixinSet()
    {
        // BitHeader's skip link is hidden at rest and drawn while focused, so its focus rule resets the size, the
        // overflow, the clip and the margin of the mixin - the margin would otherwise pull the shown link a pixel out
        // of its corner.
        var stylesheet = SourceFiles.Read("Bit.BlazorUI", "Components", "Layouts", "Header", "BitHeader.scss");
        var link = SourceFiles.GetScssBlock(stylesheet, ".bit-hdr-skp {");
        var focus = SourceFiles.GetScssDeclarations(link, "&:focus {");

        StringAssert.Contains(link, "@include visually-hidden;");
        StringAssert.Contains(focus, "margin: 0;");
        StringAssert.Contains(focus, "width: auto;");
        StringAssert.Contains(focus, "height: auto;");
        StringAssert.Contains(focus, "overflow: visible;");
        StringAssert.Contains(focus, "clip-path: none;");
    }

    [TestMethod]
    public void NoComponentShouldWriteOutAVisuallyHiddenBlockOfItsOwn()
    {
        var offenders = new[] { "Bit.BlazorUI", "Bit.BlazorUI.Extras", "Bit.BlazorUI.Legacy" }
            .Select(project => SourceFiles.GetPath(project, "Components"))
            .Where(Directory.Exists)
            .SelectMany(dir => Directory.EnumerateFiles(dir, "*.scss", SearchOption.AllDirectories))
            .Where(file => ClipToNothing().IsMatch(SourceFiles.StripScssComments(SourceFiles.ReadFullPath(file))))
            .Select(file => Path.GetRelativePath(SourceFiles.Root, file))
            .ToArray();

        Assert.AreEqual(0, offenders.Length, $"These stylesheets clip an element out of sight themselves instead of including visually-hidden: {string.Join(", ", offenders)}");
    }

    private static string ReadFunctions() => SourceFiles.Read("Bit.BlazorUI", "Styles", "functions.scss");

    // The clip every copy of the block was written with: the inset that leaves nothing of the box, or the legacy clip
    // rectangle of no size.
    [GeneratedRegex(@"clip-path\s*:\s*inset\(\s*50%\s*\)|(?<![\w-])clip\s*:\s*rect\(\s*0")]
    private static partial Regex ClipToNothing();
}

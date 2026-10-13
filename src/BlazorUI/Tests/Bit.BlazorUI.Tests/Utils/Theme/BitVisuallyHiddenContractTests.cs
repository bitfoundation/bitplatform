using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
    private static readonly string[] Projects = ["Bit.BlazorUI", "Bit.BlazorUI.Extras", "Bit.BlazorUI.Legacy"];

    [TestMethod]
    public void VisuallyHiddenShouldClipOnePixelOutOfTheFlow()
    {
        var mixin = SourceFiles.GetScssDeclarations(ReadFunctions(), "@mixin visually-hidden {");

        // A box sized to nothing is dropped from the accessibility tree by some screen readers, so it keeps a pixel
        // and is clipped away from it; display:none and visibility:hidden would hide it from them as well. clip stays
        // beside clip-path for the engines that predate the unprefixed clip-path.
        StringAssert.Contains(mixin, "border: 0;");
        StringAssert.Contains(mixin, "padding: 0;");
        StringAssert.Contains(mixin, "width: 1px;");
        StringAssert.Contains(mixin, "height: 1px;");
        StringAssert.Contains(mixin, "margin: -1px;");
        StringAssert.Contains(mixin, "overflow: hidden;");
        StringAssert.Contains(mixin, "position: absolute;");
        StringAssert.Contains(mixin, "white-space: nowrap;");
        StringAssert.Contains(mixin, "clip: rect(0 0 0 0);");
        StringAssert.Contains(mixin, "clip-path: inset(50%);");
        Assert.IsFalse(mixin.Contains("display: none"));
        Assert.IsFalse(mixin.Contains("visibility: hidden"));

        // Parked where it would have been laid out, never pushed off-screen: an offset of -10000px scrolls an RTL page
        // (or any container scrolling to the left) by that much.
        Assert.IsFalse(OffsetDeclaration().IsMatch(mixin), mixin);
    }

    [TestMethod]
    public void VisuallyHiddenControlShouldBeHiddenTheSameWayAndNeverPaintedOrPointedAt()
    {
        var mixin = SourceFiles.GetScssDeclarations(ReadFunctions(), "@mixin visually-hidden-control {");

        StringAssert.Contains(mixin, "@include visually-hidden;");
        StringAssert.Contains(mixin, "opacity: 0;");
        StringAssert.Contains(mixin, "pointer-events: none;");

        // The control stays on the part drawn for it rather than a pixel past its edge, so its zeroed margin has to
        // come after the include that sets the negative one.
        Assert.IsTrue(mixin.IndexOf("margin: 0;") > mixin.IndexOf("@include visually-hidden;"), mixin);
    }

    [TestMethod,
        DataRow("Inputs/Checkbox/BitCheckbox.scss", ".bit-chb-inp {"),
        DataRow("Inputs/ChoiceGroup/BitChoiceGroup.scss", ".bit-chg-inp {")]
    public void ANativeControlKeptForTheBrowsersBehaviorShouldBeHiddenAsAControl(string path, string rule)
    {
        var declarations = SourceFiles.GetScssDeclarations(SourceFiles.Read(["Bit.BlazorUI", "Components", .. path.Split('/')]), rule);

        StringAssert.Contains(declarations, "@include visually-hidden-control;");
        Assert.IsFalse(declarations.Contains("margin"), $"The control's margin is the mixin's: {declarations}");
    }

    [TestMethod]
    public void ASkipLinkShownOnFocusShouldBeHiddenOnlyWhileItIsNotFocused()
    {
        // BitHeader's skip link is hidden at rest and drawn while focused. It is hidden under a selector that stops
        // matching on focus, so the focused link takes back nothing the mixin set one declaration at a time - a reset
        // left out (the nowrap, the margin) would keep a part of the hiding on the shown link.
        var stylesheet = SourceFiles.Read("Bit.BlazorUI", "Components", "Layouts", "Header", "BitHeader.scss");
        var link = SourceFiles.GetScssBlock(stylesheet, ".bit-hdr-skp {");
        var atRest = SourceFiles.GetScssDeclarations(link, "&:where(:not(:focus)) {");
        var focus = SourceFiles.GetScssDeclarations(link, "&:focus {");

        StringAssert.Contains(atRest, "@include visually-hidden;");
        Assert.AreEqual(1, Regex.Matches(link, "@include visually-hidden;").Count, link);
        Assert.IsFalse(Regex.IsMatch(focus, @"(?<![\w-])(clip|clip-path|width|height|overflow|margin|white-space)\s*:"), focus);
    }

    [TestMethod]
    public void NoComponentStylesheetShouldWriteOutAVisuallyHiddenBlockOfItsOwn()
    {
        var stylesheets = Projects
            .Select(project => SourceFiles.GetDirectory(project, "Components"))
            .SelectMany(dir => Directory.EnumerateFiles(dir, "*.scss", SearchOption.AllDirectories))
            .ToArray();

        // An empty scan would pass whatever the stylesheets say.
        Assert.IsTrue(stylesheets.Length > 100, $"Only {stylesheets.Length} component stylesheets were found under {SourceFiles.Root}.");

        var offenders = stylesheets
            .SelectMany(file => SourceFiles.GetScssRules(SourceFiles.ReadFullPath(file))
                // A keyframe animating a clip (a reveal) hides nothing; it is the steps of a transition.
                .Where(rule => rule.Ancestors.Any(a => a.StartsWith("@keyframes", StringComparison.Ordinal)) is false)
                .Where(rule => IsAVisuallyHiddenBlock(rule.Declarations))
                .Select(rule => $"{Path.GetRelativePath(SourceFiles.Root, file)}: {rule.Header}"))
            .ToArray();

        Assert.AreEqual(0, offenders.Length, $"These rules hide an element out of sight themselves instead of including visually-hidden: {string.Join(", ", offenders)}");
    }

    [TestMethod]
    public void NoComponentCodeShouldWriteOutAVisuallyHiddenStyleOfItsOwn()
    {
        // Markup built in C#, Razor or TypeScript with an inline style hides through a copy of the mixin, which no
        // include can reach. BitMarkdownRenderer's is the one there is, for a page without the stylesheet, and is pinned
        // to the mixin below; any other goes through a class of the component's stylesheet.
        var allowed = SourceFiles.GetPath("Bit.BlazorUI.Extras", "Services", "Markdown", "Rendering", "BitMarkdownRenderer.cs");

        var offenders = Projects
            .Select(project => SourceFiles.GetDirectory(project))
            .SelectMany(dir => Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories))
            .Where(file => Path.GetExtension(file) is ".cs" or ".razor" or ".ts")
            .Where(file => file.Split(Path.DirectorySeparatorChar).Any(segment => segment is "bin" or "obj" or "node_modules" or "wwwroot") is false)
            .Where(file => string.Equals(Path.GetFullPath(file), allowed, StringComparison.OrdinalIgnoreCase) is false)
            .Where(file => InlineClipToNothing().IsMatch(SourceFiles.ReadFullPath(file)))
            .Select(file => Path.GetRelativePath(SourceFiles.Root, file))
            .ToArray();

        Assert.AreEqual(0, offenders.Length, $"These files hide an element with an inline style of their own instead of a class including visually-hidden: {string.Join(", ", offenders)}");
    }

    [TestMethod]
    public void TheMarkdownRenderersInlineStyleShouldBeWhatItsStylesheetRuleCompilesTo()
    {
        // A standalone BitMarkdownViewer renders without its stylesheet, so its screen-reader-only text carries the
        // .bit-mdv-sr-only rule inline: the mixin's declarations, then the rule's own, in order.
        var mixin = SourceFiles.GetScssRules(ReadFunctions()).Single(rule => rule.Header == "@mixin visually-hidden");
        var rule = SourceFiles.GetScssRules(SourceFiles.Read("Bit.BlazorUI.Extras", "Components", "MarkdownViewer", "BitMarkdownViewer.scss"))
                              .Single(rule => rule.Header.Contains(".bit-mdv-sr-only", StringComparison.Ordinal));

        Assert.AreEqual("@include visually-hidden", rule.Declarations[0]);

        var expected = mixin.Declarations.Concat(rule.Declarations.Skip(1)).Select(Normalize).ToArray();

        var field = typeof(BitMarkdownRenderer).GetField("ScreenReaderOnlyStyle", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(field, "BitMarkdownRenderer.ScreenReaderOnlyStyle is gone; update this test to wherever the inline style now lives.");

        var actual = ((string)field.GetRawConstantValue()!).Split(';', StringSplitOptions.RemoveEmptyEntries).Select(Normalize).ToArray();

        CollectionAssert.AreEqual(expected, actual, $"Expected \"{string.Join(';', expected)}\" but was \"{string.Join(';', actual)}\".");

        static string Normalize(string declaration)
        {
            var colon = declaration.IndexOf(':');

            return $"{declaration[..colon].Trim()}:{Regex.Replace(declaration[(colon + 1)..].Trim(), @"\s+", " ")}";
        }
    }

    private static string ReadFunctions() => SourceFiles.Read("Bit.BlazorUI", "Styles", "functions.scss");

    // A rule written as the block: a box of one pixel with a clip that leaves nothing of it, or one pixel square with its
    // overflow hidden out of the flow (the clip left out, which hides it no less from the eye).
    private static bool IsAVisuallyHiddenBlock(IReadOnlyList<string> declarations)
    {
        bool Has(Regex pattern) => declarations.Any(pattern.IsMatch);

        var onePixel = Has(OnePixelSize());

        return (onePixel && Has(ClipToNothing())) ||
               (Has(OnePixelWidth()) && Has(OnePixelHeight()) && Has(OverflowHidden()) && Has(PositionAbsolute()));
    }

    // Any clip rectangle (a box of one pixel is only ever clipped to hide it), or a clip-path inset that meets in the
    // middle - half of the box from each side or more - prefixed or not.
    [GeneratedRegex(@"^(?:clip\s*:\s*rect\(|(?:-webkit-)?clip-path\s*:\s*inset\(\s*(?:[5-9]\d|\d{3,})(?:\.\d+)?%\s*\)$)", RegexOptions.IgnoreCase)]
    private static partial Regex ClipToNothing();

    [GeneratedRegex(@"^(?:width|height|inline-size|block-size|max-width|max-height)\s*:\s*1px$", RegexOptions.IgnoreCase)]
    private static partial Regex OnePixelSize();

    [GeneratedRegex(@"^(?:width|inline-size)\s*:\s*1px$", RegexOptions.IgnoreCase)]
    private static partial Regex OnePixelWidth();

    [GeneratedRegex(@"^(?:height|block-size)\s*:\s*1px$", RegexOptions.IgnoreCase)]
    private static partial Regex OnePixelHeight();

    [GeneratedRegex(@"^overflow\s*:\s*hidden$", RegexOptions.IgnoreCase)]
    private static partial Regex OverflowHidden();

    [GeneratedRegex(@"^position\s*:\s*absolute$", RegexOptions.IgnoreCase)]
    private static partial Regex PositionAbsolute();

    // The same in an inline style: a clip rectangle of the block, or the inset that leaves nothing of the box.
    [GeneratedRegex(@"(?<![\w-])clip\s*:\s*rect\(|clip-path\s*:\s*inset\(\s*(?:[5-9]\d|\d{3,})(?:\.\d+)?%\s*\)", RegexOptions.IgnoreCase)]
    private static partial Regex InlineClipToNothing();

    [GeneratedRegex(@"(?<![\w-])(?:top|left|right|bottom|inset(?:-inline|-block)?(?:-start|-end)?)\s*:")]
    private static partial Regex OffsetDeclaration();
}

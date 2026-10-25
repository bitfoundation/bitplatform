using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// Pins the one inline busy spinner every component draws for itself: the spinner-ring mixin of functions.scss. A
/// component includes it rather than writing the ring out again, so a fix to the ring - its forced-colors pair, its
/// tokens - is made once and reaches every spinner of the library.
/// </summary>
[TestClass]
public partial class BitSpinnerRingStylesheetTests
{
    [TestMethod]
    public void SpinnerRingShouldTurnOnTheSharedKeyframesAndTheSpinnerTokens()
    {
        var mixin = SourceFiles.GetScssBlock(ReadFunctions(), "@mixin spinner-ring(");

        StringAssert.Contains(mixin, "$stroke: $siz-spinner-stroke");
        StringAssert.Contains(mixin, "border-width: $stroke;");
        StringAssert.Contains(mixin, "animation: bit-spin $mot-duration-spinner $mot-easing-spinner infinite;");

        StringAssert.Contains(SourceFiles.Read("Bit.BlazorUI", "Styles", "general.scss"), "@keyframes bit-spin {");
    }

    [TestMethod]
    public void SpinnerRingShouldKeepItsArcApartFromItsTrackInForcedColors()
    {
        var forced = SourceFiles.GetScssBlock(SourceFiles.GetScssBlock(ReadFunctions(), "@mixin spinner-ring("), "@media (forced-colors: active) {");

        // Every side would otherwise come back in the one system color, a ring that turns without showing it.
        StringAssert.Contains(forced, "border-color: GrayText;");
        StringAssert.Contains(forced, "border-block-start-color: CanvasText;");
    }

    [TestMethod]
    public void SpinnerRingForcedDisabledShouldGoGreyWithoutStandingStill()
    {
        var mixin = SourceFiles.GetScssBlock(ReadFunctions(), "@mixin spinner-ring-forced-disabled {");

        StringAssert.Contains(mixin, "border-block-start-color: GrayText;");
        StringAssert.Contains(mixin, "border-color: Canvas;");
    }

    [TestMethod,
        DataRow("Inputs/TextField/BitTextField.scss", ".bit-tfl.bit-dis {"),
        DataRow("Inputs/NumberField/BitNumberField.scss", "&.bit-dis {"),
        DataRow("Inputs/SearchBox/BitSearchBox.scss", ".bit-srb.bit-dis {")]
    public void ADisabledFieldShouldGreyItsSpinnerInForcedColors(string path, string disabledBlock)
    {
        var stylesheet = SourceFiles.Read(["Bit.BlazorUI", "Components", .. path.Split('/')]);

        var forcedDisabled = SourceFiles.GetScssBlock(stylesheet, disabledBlock);

        // Everything else in the frame goes GrayText there, so an arc left in CanvasText would read as a live field.
        StringAssert.Matches(forcedDisabled, SpinnerForcedDisabled());
    }

    [TestMethod]
    public void NoComponentShouldWriteOutASpinnerRingOfItsOwn()
    {
        // The Bit*Loading family and BitProgress draw indicators that are the component itself, and BitIcon's spin
        // animations turn glyphs; everything else that turns a ring includes the mixin and its shared keyframes.
        string[] exempt = [Path.Combine("Progress", "Loading"), Path.Combine("Progress", "Progress"), Path.Combine("Utilities", "Icon")];

        var offenders = new[] { "Bit.BlazorUI", "Bit.BlazorUI.Extras", "Bit.BlazorUI.Legacy" }
            .Select(project => SourceFiles.GetPath(project, "Components"))
            .Where(Directory.Exists)
            .SelectMany(dir => Directory.EnumerateFiles(dir, "*.scss", SearchOption.AllDirectories))
            .Where(file => exempt.Any(e => file.Contains(Path.DirectorySeparatorChar + e + Path.DirectorySeparatorChar)) is false)
            .Where(file => FullTurnKeyframes().IsMatch(SourceFiles.StripScssComments(SourceFiles.ReadFullPath(file))))
            .Select(file => Path.GetRelativePath(SourceFiles.Root, file))
            .ToArray();

        Assert.AreEqual(0, offenders.Length, $"These stylesheets declare their own spinning keyframes instead of including spinner-ring: {string.Join(", ", offenders)}");
    }

    private static string ReadFunctions() => SourceFiles.Read("Bit.BlazorUI", "Styles", "functions.scss");

    [GeneratedRegex(@"-spn \{\s*@include spinner-ring-forced-disabled;")]
    private static partial Regex SpinnerForcedDisabled();

    // A keyframes block that turns something a full circle, however its steps are written.
    [GeneratedRegex(@"@keyframes\s+[\w-]+\s*\{(?:[^{}]*\{[^{}]*\})*?[^{}]*?\{[^{}]*rotate[:(]\s*360deg")]
    private static partial Regex FullTurnKeyframes();
}

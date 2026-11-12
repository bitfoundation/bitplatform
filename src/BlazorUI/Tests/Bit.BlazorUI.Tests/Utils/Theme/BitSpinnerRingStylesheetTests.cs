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

        Assert.Contains("$stroke: $siz-spinner-stroke", mixin);
        Assert.Contains("border-width: $stroke;", mixin);
        Assert.Contains("animation: bit-spin $mot-duration-spinner $mot-easing-spinner infinite;", mixin);

        Assert.Contains("@keyframes bit-spin {", SourceFiles.Read("Bit.BlazorUI", "Styles", "general.scss"));
    }

    [TestMethod]
    public void SpinnerRingShouldKeepItsArcApartFromItsTrackInForcedColors()
    {
        var forced = SourceFiles.GetScssBlock(SourceFiles.GetScssBlock(ReadFunctions(), "@mixin spinner-ring("), "@media (forced-colors: active) {");

        // Every side would otherwise come back in the one system color, a ring that turns without showing it.
        Assert.Contains("border-color: GrayText;", forced);
        Assert.Contains("border-block-start-color: CanvasText;", forced);
    }

    [TestMethod]
    public void SpinnerRingForcedDisabledShouldGoGreyWithoutStandingStill()
    {
        var mixin = SourceFiles.GetScssBlock(ReadFunctions(), "@mixin spinner-ring-forced-disabled {");

        Assert.Contains("border-block-start-color: GrayText;", mixin);
        Assert.Contains("border-color: Canvas;", mixin);
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
        Assert.MatchesRegex(SpinnerForcedDisabled(), forcedDisabled);
    }

    [TestMethod]
    public void NoComponentShouldWriteOutASpinnerRingOfItsOwn()
    {
        // The Bit*Loading family and BitProgress draw indicators that are the component itself, and BitIcon's spin
        // animations turn glyphs; everything else that turns a ring includes the mixin and its shared keyframes.
        string[] exempt = [Path.Combine("Progress", "Loading"), Path.Combine("Progress", "Progress"), Path.Combine("Utilities", "Icon")];

        var offenders = new[] { "Bit.BlazorUI", "Bit.BlazorUI.Extras", "Bit.BlazorUI.Legacy", "Bit.BlazorUI.AI" }
            .Select(project => SourceFiles.GetPath(project, "Components"))
            .Where(Directory.Exists)
            .SelectMany(dir => Directory.EnumerateFiles(dir, "*.scss", SearchOption.AllDirectories))
            .Where(file => exempt.Any(e => file.Contains(Path.DirectorySeparatorChar + e + Path.DirectorySeparatorChar)) is false)
            .Where(file => FullTurnKeyframes().IsMatch(SourceFiles.StripScssComments(SourceFiles.ReadFullPath(file))))
            .Select(file => Path.GetRelativePath(SourceFiles.Root, file))
            .ToArray();

        Assert.IsEmpty(offenders, $"These stylesheets declare their own spinning keyframes instead of including spinner-ring: {string.Join(", ", offenders)}");
    }

    private static string ReadFunctions() => SourceFiles.Read("Bit.BlazorUI", "Styles", "functions.scss");

    [GeneratedRegex(@"-spn \{\s*@include spinner-ring-forced-disabled;")]
    private static partial Regex SpinnerForcedDisabled();

    // A keyframes block that turns something a full circle, however its steps are written.
    [GeneratedRegex(@"@keyframes\s+[\w-]+\s*\{(?:[^{}]*\{[^{}]*\})*?[^{}]*?\{[^{}]*rotate[:(]\s*360deg")]
    private static partial Regex FullTurnKeyframes();
}

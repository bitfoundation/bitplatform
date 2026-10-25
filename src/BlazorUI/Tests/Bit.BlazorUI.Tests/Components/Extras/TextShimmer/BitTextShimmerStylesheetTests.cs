using System;
using System.Linq;
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

        Assert.AreEqual(9, written.Length, string.Join(", ", written));
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
        var block = SourceFiles.GetScssBlock(ReadStylesheet(), rule);

        StringAssert.Contains(block, "@include bit-tsh-static");
    }

    // The resting place of the band is outside the text at both ends of a sweep whatever its spread, so a wide band
    // never hangs over the first or the last letters of a shimmer that has not started, is paused or has finished.
    [TestMethod]
    public void TheBandRestsOutsideTheTextByItsSpread()
    {
        var keyframes = SourceFiles.GetScssBlock(ReadStylesheet(), "@keyframes bit-tsh-anim");

        StringAssert.Contains(keyframes, "calc(150% - var(--bit-tsh-spread) * var(--bit-tsh-cycle))");
        StringAssert.Contains(keyframes, "calc(-50% + var(--bit-tsh-spread) * var(--bit-tsh-cycle))");
    }

    // The component writes the pause as a time and the cycle is derived here, from the sweep the element ends up with
    // before the loop factor - so a duration a class sets gets the rest that was asked for, and the loop factor
    // stretches the default sweep and its rest alike, whether the rest came from RepeatDelay or from the variable.
    [TestMethod]
    public void TheCycleIsTakenOfTheSweepTheElementEndsUpWithBeforeTheLoopFactor()
    {
        var stylesheet = ReadStylesheet();
        var block = SourceFiles.GetScssBlock(stylesheet, "@supports (animation-duration: calc(1s * tan(atan2(1s, 2s))))");

        StringAssert.Contains(block, "--bit-tsh-cycle: calc(1 + tan(clamp(0deg, atan2(var(--bit-tsh-repeat-delay), var(--bit-tsh-sweep)), 89.9deg)));");

        var root = SourceFiles.GetScssBlock(stylesheet, "\n.bit-tsh {");

        StringAssert.Contains(root, "--bit-tsh-sweep: var(--bit-TextShimmer-duration, 2000ms);");
        StringAssert.Contains(root, "--bit-tsh-duration: var(--bit-TextShimmer-duration, calc(2000ms * #{$mot-loop-factor}));");
        StringAssert.Contains(root, "--bit-tsh-repeat-delay: var(--bit-TextShimmer-repeat-delay, 0ms);");
    }



    // The public variables inherit, so a value set on :root, an ancestor or a class reaches every shimmer under it:
    // each one is read with a fallback and never declared, and the header of the stylesheet names it.
    [TestMethod,
        DataRow("base-color", "--bit-tsh-base-clr"),
        DataRow("gradient-color", "--bit-tsh-gradient-clr"),
        DataRow("spread", "--bit-tsh-spread"),
        DataRow("angle", "--bit-tsh-angle"),
        DataRow("duration", "--bit-tsh-duration"),
        DataRow("delay", "--bit-tsh-delay"),
        DataRow("repeat-delay", "--bit-tsh-repeat-delay"),
        DataRow("iterations", "--bit-tsh-iterations")]
    public void EveryPublicVariableIsReadWithAFallbackAndNeverDeclared(string name, string privateVariable)
    {
        var stylesheet = ReadStylesheet();
        var variable = $"--bit-TextShimmer-{name}";

        Assert.IsFalse(Regex.IsMatch(stylesheet, $@"^\s*{variable}\s*:", RegexOptions.Multiline), $"{variable} is declared.");
        StringAssert.Contains(stylesheet, $"//   {variable} ");
        StringAssert.Matches(SourceFiles.GetScssBlock(stylesheet, "\n.bit-tsh {"), new Regex($@"{privateVariable}: var\({variable}, "));
    }

    // A Spread left at its default publishes the spread it computes under a name of its own, which only backs the
    // public variable up - so --bit-TextShimmer-spread restyles every shimmer that was not given a spread.
    [TestMethod]
    public void TheComputedDefaultSpreadIsOnlyTheFallbackOfThePublicVariable()
    {
        var root = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-tsh {");

        StringAssert.Contains(root, "--bit-tsh-spread: var(--bit-TextShimmer-spread, var(--bit-tsh-auto-spread));");
    }

    // A Color is a choice the shimmer was given, so its class sets the private value again after the root reads the
    // public variable into it.
    [TestMethod]
    public void TheColorRoleWinsOverThePublicVariable()
    {
        var stylesheet = ReadStylesheet();
        var roles = SourceFiles.GetScssBlock(stylesheet, ".bit-tsh-#{$role} {");

        StringAssert.Contains(roles, "--bit-tsh-gradient-clr:");
        Assert.IsTrue(stylesheet.IndexOf(".bit-tsh-#{$role}", StringComparison.Ordinal) > stylesheet.IndexOf("\n.bit-tsh {", StringComparison.Ordinal));
    }



    // A disabled shimmer is dimmed by a public variable, the same as a disabled BitText, and a forced palette - which
    // takes the dimming away - paints it in the system color for disabled text instead.
    [TestMethod]
    public void ADisabledShimmerIsDimmedByAPublicVariableAndGrayedOutInForcedColors()
    {
        var stylesheet = ReadStylesheet();

        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-tsh.bit-dis {"), "opacity: var(--bit-TextShimmer-disabled-opacity, #{$opa-dis});");
        StringAssert.Contains(stylesheet, "//   --bit-TextShimmer-disabled-opacity ");
        StringAssert.Contains(SourceFiles.GetScssBlock(SourceFiles.GetScssBlock(stylesheet, "@media (forced-colors: active)"), ".bit-tsh.bit-dis {"), "color: GrayText;");
    }

    // Reduced motion is often on together with forced colors, so its rule must weigh no more than .bit-tsh: the
    // forced-colors and print rules - and the GrayText of a disabled shimmer - would otherwise lose to it.
    [TestMethod]
    public void TheReducedMotionRuleDoesNotOutweighTheForcedColorsOnes()
    {
        var reducedMotion = SourceFiles.GetScssBlock(ReadStylesheet(), "@media (prefers-reduced-motion: reduce)");

        StringAssert.Contains(reducedMotion, ".bit-tsh:where(:not(.bit-fam):not(.bit-fam *)) {");
    }

    // A shimmer a page made focusable is drawn with the focus ring of the library rather than the browser's own.
    [TestMethod]
    public void AFocusableShimmerIsDrawnWithTheFocusRing()
    {
        var root = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-tsh {");

        StringAssert.Contains(SourceFiles.GetScssBlock(root, "&:focus-visible:not([tabindex=\"-1\"]) {"), "@include focus-ring;");
    }



    private static string[] GetRootDeclarations()
    {
        return VariableDeclaration.Matches(SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-tsh {")).Select(m => m.Groups[1].Value).ToArray();
    }

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI.Extras", "Components", "TextShimmer", "BitTextShimmer.scss");
}

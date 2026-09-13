using System;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

[TestClass]
public sealed class BitThemeApcaContrastTests
{
    // ── Algorithm correctness vs published Myndex anchors ─────────────────────

    [TestMethod]
    public void GetApcaContrastMatchesReferenceAnchors()
    {
        // The canonical APCA-W3 (0.98G-4g) reference values. Getting these right proves the
        // constants and the polarity branches are implemented correctly.
        Assert.AreEqual(106.04, BitThemeColorContrast.GetApcaContrast("#000000", "#FFFFFF"), 0.05, "black on white");
        Assert.AreEqual(-107.88, BitThemeColorContrast.GetApcaContrast("#FFFFFF", "#000000"), 0.05, "white on black");
        Assert.AreEqual(63.056, BitThemeColorContrast.GetApcaContrast("#888888", "#FFFFFF"), 0.05, "grey on white");
    }

    // ── Polarity and symmetry semantics ───────────────────────────────────────

    [TestMethod]
    public void DarkTextOnLightBackgroundIsPositive()
    {
        Assert.IsTrue(BitThemeColorContrast.GetApcaContrast("#000000", "#FFFFFF") > 0);
    }

    [TestMethod]
    public void LightTextOnDarkBackgroundIsNegative()
    {
        Assert.IsTrue(BitThemeColorContrast.GetApcaContrast("#FFFFFF", "#000000") < 0);
    }

    [TestMethod]
    public void ApcaIsDirectionalUnlikeWcag()
    {
        // Swapping text and background flips APCA's sign (and generally its magnitude), because APCA
        // is polarity-aware - the key property WCAG's symmetric ratio lacks.
        var forward = BitThemeColorContrast.GetApcaContrast("#212121", "#569FFF");
        var reversed = BitThemeColorContrast.GetApcaContrast("#569FFF", "#212121");

        Assert.IsTrue(forward > 0, "dark-on-light is positive");
        Assert.IsTrue(reversed < 0, "light-on-dark is negative");
        Assert.AreNotEqual(Math.Abs(forward), Math.Abs(reversed), 0.5, "magnitudes differ by polarity");

        // WCAG, by contrast, is identical either way.
        Assert.AreEqual(
            BitThemeColorContrast.GetContrastRatio("#212121", "#569FFF"),
            BitThemeColorContrast.GetContrastRatio("#569FFF", "#212121"),
            1e-9);
    }

    [TestMethod]
    public void EqualColorsHaveZeroContrast()
    {
        Assert.AreEqual(0.0, BitThemeColorContrast.GetApcaContrast("#3060A0", "#3060A0"), 1e-9);
    }

    [TestMethod]
    public void ShorthandHexIsExpandedLikeSixDigit()
    {
        Assert.AreEqual(
            BitThemeColorContrast.GetApcaContrast("#000000", "#FFFFFF"),
            BitThemeColorContrast.GetApcaContrast("#000", "#FFF"),
            1e-9);
    }

    // ── Threshold helpers use magnitude (polarity-agnostic) ───────────────────

    [TestMethod]
    public void MeetsHelpersEvaluateMagnitudeAcrossBothPolarities()
    {
        var darkOnLight = BitThemeColorContrast.GetApcaContrast("#000000", "#FFFFFF");  // +106
        var lightOnDark = BitThemeColorContrast.GetApcaContrast("#FFFFFF", "#000000");  // -108

        // A strongly-negative Lc is just as readable as a strongly-positive one.
        Assert.IsTrue(BitThemeColorContrast.MeetsApcaBodyText(darkOnLight));
        Assert.IsTrue(BitThemeColorContrast.MeetsApcaBodyText(lightOnDark));

        // Ordering of the advisory thresholds.
        Assert.IsTrue(BitThemeColorContrast.ApcaBodyTextLc > BitThemeColorContrast.ApcaLargeTextLc);
        Assert.IsTrue(BitThemeColorContrast.ApcaLargeTextLc > BitThemeColorContrast.ApcaNonTextLc);

        // A mid-grey pair clears the large-text advisory floor but not body text.
        var mid = BitThemeColorContrast.GetApcaContrast("#767676", "#FFFFFF"); // ~ Lc 70
        Assert.IsTrue(BitThemeColorContrast.MeetsApcaLargeText(mid));
        Assert.IsFalse(BitThemeColorContrast.MeetsApcaBodyText(mid));
    }

    // ── Input validation mirrors the WCAG helper ──────────────────────────────

    [TestMethod]
    public void InvalidInputThrows()
    {
        Assert.ThrowsExactly<ArgumentException>(() => BitThemeColorContrast.GetApcaContrast("nope", "#FFFFFF"));
        Assert.ThrowsExactly<ArgumentException>(() => BitThemeColorContrast.GetApcaContrast("#FFFFFF", ""));
        // Null funnels through ArgumentException.ThrowIfNullOrWhiteSpace, which throws the
        // ArgumentNullException subtype - matching GetContrastRatio's contract.
        Assert.ThrowsExactly<ArgumentNullException>(() => BitThemeColorContrast.GetApcaContrast(null!, "#FFFFFF"));
    }

    // ── Packaged palette smoke test (advisory, non-gating) ────────────────────

    [TestMethod]
    public void PackagedAccentFillsAreAboveTheApcaInvisibilityFloor()
    {
        // A deliberately loose safety net: APCA is offered as ADVISORY (WCAG 2.x stays the
        // conformance bar, and APCA can legitimately disagree with the WCAG-tuned on-color
        // choices), so this does NOT assert a body-text or even a UI floor - it only guards against
        // a future palette change that drops a resting accent fill below APCA's ~Lc 15
        // "invisibility" threshold, which no usable palette should ever hit.
        const double invisibilityFloor = 15.0;
        var roles = new[] { "pri", "sec", "ter", "inf", "suc", "wrn", "swr", "err" };

        foreach (var file in Directory.EnumerateFiles(
                     Path.Combine(AppContext.BaseDirectory, "theme-styles"),
                     "colors.*-*.scss",
                     SearchOption.AllDirectories))
        {
            var css = File.ReadAllText(file);
            var name = Path.GetFileName(file);

            foreach (var role in roles)
            {
                var main = ReadToken(css, $"--bit-clr-{role}");
                var text = ReadToken(css, $"--bit-clr-{role}-text");
                if (main is null || text is null) continue;

                var lc = BitThemeColorContrast.GetApcaContrast(text, main);

                Assert.IsTrue(double.IsFinite(lc) && Math.Abs(lc) <= 110,
                    $"{name} {role}: APCA produced an out-of-range value {lc}.");
                Assert.IsTrue(Math.Abs(lc) >= invisibilityFloor,
                    $"{name} {role}: on-text {text} on fill {main} is APCA Lc {lc:F1}, below the {invisibilityFloor} invisibility floor.");
            }
        }
    }

    private static string? ReadToken(string css, string tokenName)
    {
        var match = Regex.Match(css, Regex.Escape(tokenName) + @":[ \t]*(#[0-9A-Fa-f]{6})");
        return match.Success ? match.Groups[1].Value : null;
    }
}

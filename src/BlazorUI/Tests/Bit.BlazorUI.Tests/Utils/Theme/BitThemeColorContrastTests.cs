using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

[TestClass]
public sealed class BitThemeColorContrastTests
{
    [TestMethod]
    public void BlackOnWhite_IsHighContrast()
    {
        var ratio = BitThemeColorContrast.GetContrastRatio("#000000", "#FFFFFF");
        Assert.AreEqual(21.0, ratio, 0.001);
        Assert.IsTrue(BitThemeColorContrast.MeetsWcagAaNormalText(ratio));
    }

    [TestMethod]
    public void SameColor_IsMinimumRatio()
    {
        var ratio = BitThemeColorContrast.GetContrastRatio("#FFFFFF", "#FFFFFF");
        Assert.AreEqual(1.0, ratio, 0.001);
        Assert.IsFalse(BitThemeColorContrast.MeetsWcagAaNormalText(ratio));
    }
}

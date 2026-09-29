using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Progress.Loading;

/// <summary>
/// Pins the protected members a loader derived outside this library may still use, which the family itself no
/// longer does: its drawings are laid out in CSS units now, but removing them would break such a loader's build.
/// </summary>
[TestClass]
public class BitLoadingBaseCompatibilityTests
{
#pragma warning disable CS0618 // The members under test are obsolete on purpose.
    private sealed class LegacyLoading : BitLoadingBase
    {
        protected override string RootElementClass => "legacy-ldn";

        public string ConvertOffset(double value) => Convert(value);

        public int AuthoredSize => OriginalSize;
    }
#pragma warning restore CS0618

    [TestMethod]
    [DataRow(null, null, 8, "6.4")]
    [DataRow(BitSize.Small, null, 8, "4")]
    [DataRow(BitSize.Large, null, 8, "8.8")]
    [DataRow(null, 160, 8, "16")]
    [DataRow(BitSize.Small, 160, 8, "4")]
    public void ConvertShouldStillRescaleAnAuthoredOffsetToThePixelSize(BitSize? size, int? customSize, double value, string expected)
    {
        var loading = new LegacyLoading { Size = size, CustomSize = customSize };

        Assert.AreEqual(80, loading.AuthoredSize);
        Assert.AreEqual(expected, loading.ConvertOffset(value));
    }
}

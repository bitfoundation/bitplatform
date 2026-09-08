namespace Boilerplate.Tests.Features.Versioning;

/// <summary>
/// Nothing in the build can compare <c>AppVersionCodes</c> with the <c>ApplicationVersion</c> arithmetic in
/// Boilerplate.Client.Maui.csproj, so their parity is pinned here as literal expectations.
/// </summary>
[TestClass]
public class AppVersionCodesTests
{
    [TestMethod]
    [DataRow("1.0", 1_000_000L, DisplayName = "two components, patch implied zero - the csproj's '1.0' example")]
    [DataRow("1.0.1", 1_000_001L, DisplayName = "the csproj's '1.0.1' example")]
    [DataRow("1.2.3", 1_002_003L)]
    [DataRow("1.0.0.0", 1_000_000L, DisplayName = "Assembly.GetName().Version always renders four parts")]
    [DataRow("0.0.1", 1L, DisplayName = "the lowest code Google Play accepts")]
    [DataRow("1.999.999", 1_999_999L, DisplayName = "the widest each component may go")]
    [DataRow("2100.0.0", 2_100_000_000L, DisplayName = "the highest version Google Play's 2100000000 cap allows")]
    public void TryEncode_Should_MatchTheMauiFormula(string version, long expected)
    {
        Assert.AreEqual(expected, AppVersionCodes.TryEncode(version));
    }

    /// <summary>The reason the column exists: as text, 1.10.0 sorts below 1.9.0.</summary>
    [TestMethod]
    public void TryEncode_Should_OrderVersionsNumerically_WhereTextDoesNot()
    {
        Assert.IsLessThan(0, string.CompareOrdinal("1.10.0", "1.9.0"), "This is the broken ordering the code replaces; if it ever stops holding, this test is testing nothing.");

        Assert.IsTrue(AppVersionCodes.TryEncode("1.10.0") > AppVersionCodes.TryEncode("1.9.0"));
        Assert.IsTrue(AppVersionCodes.TryEncode("2.0.0") > AppVersionCodes.TryEncode("1.999.999"));
        Assert.IsTrue(AppVersionCodes.TryEncode("1.0.1") > AppVersionCodes.TryEncode("1.0.0"));
    }

    /// <summary>
    /// Anything that does not fit must become null rather than collide with a real build (1.0.1000 and 1.1.0 would
    /// otherwise be one number).
    /// </summary>
    [TestMethod]
    [DataRow(null, DisplayName = "no header at all")]
    [DataRow("", DisplayName = "empty header")]
    [DataRow("not-a-version")]
    [DataRow("1.0.0-rc1", DisplayName = "a pre-release suffix is not a number and is not silently truncated")]
    [DataRow("1.0.1000", DisplayName = "patch past 999 would collide with 1.1.0")]
    [DataRow("1.1000.0", DisplayName = "minor past 999 would collide with 2.0.0")]
    [DataRow("-1.0.0")]
    [DataRow("0.0.0", DisplayName = "code 0 is below the 1 Google Play accepts")]
    [DataRow("2100.0.1", DisplayName = "the first code past the 2100000000 cap")]
    [DataRow("9999.0.0", DisplayName = "a major well past the cap")]
    public void TryEncode_Should_ReturnNull_ForAnythingItCannotRepresent(string? version)
    {
        Assert.IsNull(AppVersionCodes.TryEncode(version));
    }

    [TestMethod]
    [DataRow(1_000_000L, "1.0.0")]
    [DataRow(1_000_001L, "1.0.1")]
    [DataRow(1_002_003L, "1.2.3")]
    [DataRow(0L, "0.0.0")]
    [DataRow(2_099_999_999L, "2099.999.999")]
    public void Decode_Should_RenderTheVersionBack(long code, string expected)
    {
        Assert.AreEqual(expected, AppVersionCodes.Decode(code));
    }

    [TestMethod]
    public void Decode_Should_ReturnNull_ForNothingToShow()
    {
        Assert.IsNull(AppVersionCodes.Decode(null), "A session or device that never reported a usable version has no version to display.");
        Assert.IsNull(AppVersionCodes.Decode(-1));
    }

    /// <summary>
    /// Three components round-trip exactly; two come back canonicalised ('1.0' -&gt; '1.0.0') and four lose the revision.
    /// </summary>
    [TestMethod]
    [DataRow("1.2.3", "1.2.3")]
    [DataRow("1.0", "1.0.0")]
    [DataRow("1.0.0.7", "1.0.0")]
    public void EncodeThenDecode_Should_RoundTrip(string version, string expected)
    {
        Assert.AreEqual(expected, AppVersionCodes.Decode(AppVersionCodes.TryEncode(version)));
    }
}

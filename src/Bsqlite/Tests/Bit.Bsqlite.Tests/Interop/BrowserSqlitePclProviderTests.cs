using Bit.Bsqlite.EntityFrameworkCore;

namespace Bit.Bsqlite.Interop.Tests;

/// <summary>
/// The version this stub reports is what EF Core gates its SQL translations on in the browser, so
/// it has to describe the engine that is actually there. The browser suite checks it against the
/// running engine; this checks the two forms of it against each other, which is the half that can
/// be got wrong on desktop.
/// </summary>
[TestClass]
public sealed class BrowserSqlitePclProviderTests
{
    [TestMethod]
    public void TheReportedVersionAndVersionNumberAgree()
    {
        var provider = new BsqlitePclProvider();
        var version = new Version(provider.sqlite3_libversion().utf8_to_string());

        var expected = (version.Major * 1_000_000)
            + (version.Minor * 1_000)
            + Math.Max(version.Build, 0);

        Assert.AreEqual(expected, provider.sqlite3_libversion_number());
    }

    /// <summary>
    /// Pinned so that bumping the vendored engine without updating the stub is a failing test here
    /// as well as in the browser, where the engine can actually be asked.
    /// </summary>
    [TestMethod]
    public void TheReportedVersionIsTheVendoredEngines()
    {
        // Two constants on purpose: the pin is the point, so the analyzer's "always true" is the test.
#pragma warning disable MSTEST0032
        Assert.AreEqual("3.53.0", BsqlitePclProvider.EngineVersion);
#pragma warning restore MSTEST0032
        Assert.AreEqual(
            BsqlitePclProvider.EngineVersion,
            new BsqlitePclProvider().sqlite3_libversion().utf8_to_string());
    }
}

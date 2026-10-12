using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.TimeZonePicker;

/// <summary>
/// Covers the table of time zones the picker lists and the lookups that resolve the ids a time zone is written down
/// with - the canonical IANA id, a renamed one, a Windows id, a spelling of UTC - back to one entry of it.
/// </summary>
[TestClass]
public class BitTimeZonesTests
{
    [TestMethod]
    public void BitTimeZonesShouldListEveryTimeZoneOnceUnderItsCanonicalId()
    {
        Assert.IsTrue(BitTimeZones.All.Length > 400);

        var ids = BitTimeZones.All.Select(z => z.Id).ToArray();

        Assert.AreEqual(ids.Length, ids.Distinct(StringComparer.OrdinalIgnoreCase).Count());

        // Every entry is found by its own id, as itself.
        foreach (var zone in BitTimeZones.All)
        {
            Assert.AreSame(zone, BitTimeZones.Find(zone.Id), zone.Id);
        }
    }

    [TestMethod]
    public void BitTimeZonesShouldStartWithUtc()
    {
        Assert.AreSame(BitTimeZones.All[0], BitTimeZones.Utc);
        Assert.AreEqual("Etc/UTC", BitTimeZones.Utc.Id);
        Assert.IsNull(BitTimeZones.Utc.CountryCode);
        Assert.AreSame(TimeZoneInfo.Utc, BitTimeZones.Utc.TimeZoneInfo);
    }

    [TestMethod,
        DataRow("Asia/Calcutta", "Asia/Kolkata"),
        DataRow("Europe/Kiev", "Europe/Kyiv"),
        DataRow("US/Pacific", "America/Los_Angeles"),
        DataRow("America/Buenos_Aires", "America/Argentina/Buenos_Aires"),
        DataRow("Africa/Asmera", "Africa/Asmara"),
        DataRow("Pacific/Truk", "Pacific/Chuuk"),
        DataRow("UTC", "Etc/UTC"),
        DataRow("GMT", "Etc/UTC"),
        DataRow("Etc/Zulu", "Etc/UTC"),
        DataRow("asia/tehran", "Asia/Tehran"),
        DataRow("  Asia/Tehran ", "Asia/Tehran")]
    public void BitTimeZonesShouldFindATimeZoneByAnyIanaIdOfIt(string id, string expected)
    {
        Assert.AreEqual(expected, BitTimeZones.Find(id)?.Id);
    }

    [TestMethod,
        DataRow("Pacific Standard Time", "America/Los_Angeles"),
        DataRow("Iran Standard Time", "Asia/Tehran"),
        DataRow("India Standard Time", "Asia/Kolkata"),
        DataRow("FLE Standard Time", "Europe/Kyiv"),
        DataRow("W. Europe Standard Time", "Europe/Berlin"),
        DataRow("UTC", "Etc/UTC")]
    public void BitTimeZonesShouldFindATimeZoneByItsWindowsId(string windowsId, string expected)
    {
        Assert.AreEqual(expected, BitTimeZones.Find(windowsId)?.Id);
    }

    [TestMethod,
        DataRow(null),
        DataRow(""),
        DataRow("   "),
        DataRow("Not/A_Zone"),
        DataRow("Etc/GMT+5")]
    public void BitTimeZonesShouldFindNothingForAnIdOfNoPlace(string? id)
    {
        Assert.IsNull(BitTimeZones.Find(id));
    }

    [TestMethod]
    public void BitTimeZonesShouldFindTheTimeZoneOfATimeZoneInfo()
    {
        Assert.AreSame(BitTimeZones.Utc, BitTimeZones.Find(TimeZoneInfo.Utc));
        Assert.IsNull(BitTimeZones.Find((TimeZoneInfo?)null));

        var tehran = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran");

        Assert.AreEqual("Asia/Tehran", BitTimeZones.Find(tehran)?.Id);
    }

    [TestMethod]
    public void BitTimeZonesShouldCarryTheWindowsIdAndTheCountryOfATimeZone()
    {
        var losAngeles = BitTimeZones.Find("America/Los_Angeles")!;

        Assert.AreEqual("Los Angeles", losAngeles.Name);
        Assert.AreEqual("US", losAngeles.CountryCode);
        Assert.AreEqual("United States", losAngeles.CountryName);
        Assert.AreEqual("Pacific Standard Time", losAngeles.WindowsId);

        // A name of several words is read with its underscores as spaces, and the place of a three-part id is its last.
        Assert.AreEqual("Buenos Aires", BitTimeZones.Find("America/Argentina/Buenos_Aires")!.Name);

        // A country BitCountries has no entry for still has its name.
        Assert.AreEqual("French Guiana", BitTimeZones.Find("America/Cayenne")!.CountryName);
    }

    [TestMethod]
    public void BitTimeZoneShouldResolveItsTimeZoneInfoOnTheRunningRuntime()
    {
        var unresolved = BitTimeZones.All.Where(z => z.TimeZoneInfo is null).Select(z => z.Id).ToArray();

        // A zone newer than the data of the runtime falls back to an older id or to its Windows id, so at most a zone
        // with neither (Antarctica/Troll, which Windows has no zone for) can be missing.
        Assert.IsTrue(unresolved.Length <= 1, string.Join(", ", unresolved));

        var tehran = BitTimeZones.Find("Asia/Tehran")!;

        Assert.AreEqual(TimeSpan.FromHours(3.5), tehran.GetUtcOffset(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)));
    }

    [TestMethod]
    public void BitTimeZoneShouldTakeATranslatedCountryName()
    {
        var berlin = new BitTimeZone("Europe/Berlin", "Berlin", "DE", "W. Europe Standard Time", "Deutschland");

        Assert.AreEqual("Deutschland", berlin.CountryName);

        berlin.CountryName = null;

        Assert.IsNull(berlin.CountryName);
        Assert.AreEqual("Germany", new BitTimeZone("Europe/Berlin", "Berlin", "DE").CountryName);
    }

    [TestMethod,
        DataRow(3.5, "UTC+03:30"),
        DataRow(-8, "UTC-08:00"),
        DataRow(0, "UTC+00:00"),
        DataRow(5.75, "UTC+05:45"),
        DataRow(-9.5, "UTC-09:30"),
        DataRow(14, "UTC+14:00")]
    public void BitTimeZonesShouldFormatAnOffsetFromUtc(double hours, string expected)
    {
        Assert.AreEqual(expected, BitTimeZones.FormatUtcOffset(TimeSpan.FromHours(hours)));
    }
}

using System.Text.Json;

namespace Bit.BbScanner.Tests;

/// <summary>
/// Replays element strings that BWIPP's GS1 syntax checker judged - a valid value and its mutations for every AI the
/// parser knows - against <see cref="BbGs1ElementString"/>. See <c>generate-gs1-cases.mjs</c> next to the data.
/// </summary>
/// <remarks>
/// The parser's table and checks were written from the GS1 General Specifications by the same hand as its other
/// tests; BWIPP checks against the GS1 Barcode Syntax Dictionary independently, so agreement with it is what
/// establishes that both read the specification the same way.
/// </remarks>
[TestClass]
public class Gs1ReferenceTests
{
    /// <summary>
    /// BWIPP's rejections the parser deliberately does not make, by BWIPP's error code: membership of the ISO country,
    /// currency, package type and media type lists, and the internal structure of North American coupon codes. The
    /// parser accepts what these reject; everything else BWIPP rejects, it must reject too.
    /// </summary>
    private static readonly HashSet<string> NotChecked =
    [
        "GS1UnknownCountry",
        "GS1UnknownCountryAlpha",
        "GS1UnknownCountryOr999",
        "GS1UnknownCurrency",
        "GS1unknownPackageType",
        "GS1UnknownMediaType",
        "GS1couponNotNumeric",
        "GS1couponMissingFormatCode",
        "GS1couponBadFormatCode",
        "GS1couponTooShortGCPVLI",
        "GS1couponTooShortGCP",
        "GS1couponTooShortOfferCode",
        "GS1couponBadVLI",
    ];

    private static readonly Lazy<Gs1CaseFile> File = new(() =>
    {
        using var stream = typeof(Gs1ReferenceTests).Assembly.GetManifestResourceStream("Gs1Cases.json")
            ?? throw new InvalidOperationException("Gs1Cases.json is not embedded in the test assembly.");
        return JsonSerializer.Deserialize<Gs1CaseFile>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Gs1Cases.json is empty.");
    });

    [TestMethod]
    public void TheCasesComeFromTheIndependentChecker()
    {
        StringAssert.StartsWith(File.Value.Generator, "BWIPP");
        Assert.IsGreaterThan(2000, File.Value.Cases.Length);
        Assert.IsGreaterThan(500, File.Value.Cases.Select(c => c.Ai).Distinct().Count(), "Every AI of the table has cases.");
    }

    [TestMethod]
    public void EveryElementStringBwippAcceptsParses()
    {
        var failures = new List<string>();
        foreach (var c in File.Value.Cases.Where(c => c.Valid))
        {
            var text = $"({c.Ai}){c.Value}";
            if (!BbGs1ElementString.TryParse(text, out var parsed))
            {
                failures.Add($"{text}: {Error(text)}");
            }
            else if (parsed.Elements.Count != 1 || parsed.Elements[0].Value != c.Value)
            {
                failures.Add($"{text}: parsed as {parsed}");
            }
        }

        Assert.IsEmpty(failures, string.Join(Environment.NewLine, failures.Take(40)));
    }

    [TestMethod]
    public void EveryElementStringBwippRejectsIsRejectedUnlessTheCheckIsOneTheParserLeavesOut()
    {
        var failures = new List<string>();
        var leftOut = 0;
        foreach (var c in File.Value.Cases.Where(c => !c.Valid))
        {
            if (NotChecked.Contains(c.Error!))
            {
                leftOut++;
                continue;
            }

            var text = $"({c.Ai}){c.Value}";
            if (BbGs1ElementString.TryParse(text, out _))
            {
                failures.Add($"{text}: BWIPP says {c.Error}");
            }
        }

        Assert.IsEmpty(failures, string.Join(Environment.NewLine, failures.Take(40)));
        Assert.IsLessThan(File.Value.Cases.Length / 20, leftOut, "The checks left out should stay few.");
    }

    private static string Error(string text)
    {
        try
        {
            BbGs1ElementString.Parse(text);
            return "parsed";
        }
        catch (FormatException exception)
        {
            return exception.Message;
        }
    }

    private sealed record Gs1CaseFile(string Generator, Gs1Case[] Cases);

    private sealed record Gs1Case(string Ai, string Value, bool Valid, string? Error);
}

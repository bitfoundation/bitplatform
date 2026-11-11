namespace Bit.BbScanner.Tests;

/// <summary>
/// The GS1 element string parser: splitting a GS1 symbol's content into Application Identifiers and values, and
/// rejecting whatever the GS1 General Specifications do not allow rather than reporting part of it.
/// </summary>
[TestClass]
public class Gs1ElementStringTests
{
    private const char GS = '\u001D';

    private static string WithCheckDigit(string digits) => digits + LinearEncoders.EanCheckDigit(digits);

    [TestMethod]
    public void SplitsAnElementStringIntoItsElements()
    {
        var elements = BbGs1ElementString.Parse($"010952123454321310ABC123{GS}15261231");

        Assert.HasCount(3, elements.Elements);
        Assert.AreEqual("01", elements.Elements[0].ApplicationIdentifier);
        Assert.AreEqual("09521234543213", elements.Elements[0].Value);
        Assert.AreEqual("GTIN", elements.Elements[0].Title);
        Assert.AreEqual("ABC123", elements["10"]);
        Assert.AreEqual("261231", elements["15"]);
        Assert.IsNull(elements["17"]);
        Assert.AreEqual("(01)09521234543213(10)ABC123(15)261231", elements.ToBracketedString());
    }

    [TestMethod]
    public void APredefinedLengthElementNeedsNoSeparatorButToleratesOne()
    {
        var without = BbGs1ElementString.Parse("0109521234543213" + "17261231" + "10LOT");
        var with = BbGs1ElementString.Parse($"0109521234543213{GS}17261231{GS}10LOT");

        Assert.AreEqual(without.ToBracketedString(), with.ToBracketedString());
        Assert.AreEqual("0109521234543213" + "17261231" + "10LOT", with.ToElementString());
    }

    [TestMethod]
    public void AFixedLengthElementOutsideThePredefinedListMustBeEndedBySeparator()
    {
        // AI 422, the country of origin, is three digits but not among the predefined lengths: an encoder must end it
        // with FNC1, and a reader may not guess where it ends.
        Assert.IsFalse(BbGs1ElementString.TryParse("422276" + "10ABC", out _));

        var elements = BbGs1ElementString.Parse($"422276{GS}10ABC");
        Assert.AreEqual("276", elements["422"]);
        Assert.AreEqual($"422276{GS}10ABC", elements.ToElementString());
    }

    [TestMethod]
    [DataRow("]C1")]
    [DataRow("]d2")]
    [DataRow("]Q3")]
    [DataRow("]e0")]
    [DataRow("\u001D")]
    public void ALeadingSymbologyIdentifierOrFnc1IsSkipped(string prefix)
    {
        var elements = BbGs1ElementString.Parse(prefix + "0109521234543213");

        Assert.AreEqual("09521234543213", elements["01"]);
    }

    [TestMethod]
    public void TheBracketedFormParsesToTheSameElements()
    {
        var encoded = BbGs1ElementString.Parse($"0109521234543213{GS}3103001250{GS}21A(1)B");
        var bracketed = BbGs1ElementString.Parse(encoded.ToBracketedString());

        Assert.AreEqual(@"(01)09521234543213(3103)001250(21)A\(1)B", encoded.ToBracketedString());
        Assert.AreEqual("A(1)B", bracketed["21"]);
        Assert.AreEqual(encoded.ToElementString(), bracketed.ToElementString());
    }

    [TestMethod]
    [DataRow("0109521234543214", DisplayName = "A wrong GTIN check digit")]
    [DataRow("00123456789012345670", DisplayName = "A wrong SSCC check digit")]
    [DataRow("05123", DisplayName = "An AI that does not exist")]
    [DataRow("010952123454321", DisplayName = "A GTIN a digit short")]
    [DataRow("01095212345\u001D43213", DisplayName = "A fixed length value cut short by a separator")]
    [DataRow("10ABCDEFGHIJKLMNOPQRSTU", DisplayName = "A batch longer than twenty characters")]
    [DataRow("10AB CD", DisplayName = "A space, which character set 82 leaves out")]
    [DataRow("10AB#CD", DisplayName = "A hash, which character set 82 leaves out")]
    [DataRow("10", DisplayName = "An AI with no value")]
    [DataRow("10\u001D21X", DisplayName = "An empty variable length value")]
    [DataRow("011234567890123A", DisplayName = "A letter in a numeric field")]
    [DataRow("17261331", DisplayName = "Month 13")]
    [DataRow("17260230", DisplayName = "30 February")]
    [DataRow("17250229", DisplayName = "29 February in a year not a multiple of four")]
    [DataRow("4326261200", DisplayName = "Day 00 where the AI does not allow it")]
    [DataRow("70032612312460", DisplayName = "A time of 24:60")]
    [DataRow("4321\u001D10A", DisplayName = "A yes or no flag with no value")]
    [DataRow("43212", DisplayName = "A yes or no flag of 2")]
    [DataRow("8010abc", DisplayName = "Lower case letters in character set 39")]
    [DataRow("10A\u001D10B", DisplayName = "One AI twice, with different values")]
    [DataRow("", DisplayName = "Nothing")]
    [DataRow("]C1", DisplayName = "Only a symbology identifier")]
    [DataRow("(01)", DisplayName = "A bracketed AI with no value")]
    [DataRow("(01)09521234543213(10", DisplayName = "An unclosed bracketed AI")]
    [DataRow("(1)ABC", DisplayName = "A bracketed AI that does not exist")]
    [DataRow("(010)9521234543213", DisplayName = "A bracketed AI with a digit of its value inside")]
    [DataRow("72523", DisplayName = "A sex code ISO/IEC 5218 does not have")]
    [DataRow("80110123", DisplayName = "A coupon serial with a leading zero")]
    [DataRow("80131987654Ad4X4bL5ttr2310c2L", DisplayName = "A GMN with a wrong check character")]
    [DataRow("8013AB87654Ad4X4bL5ttr2310c2K", DisplayName = "A GMN that does not start with a GS1 Company Prefix")]
    [DataRow("8007GB83WEST12345698765432", DisplayName = "An IBAN with wrong check digits")]
    [DataRow("4301John%2", DisplayName = "A percent sign without two hexadecimal digits")]
    [DataRow("43091800000001" + "0000000000", DisplayName = "A latitude beyond 90 degrees")]
    [DataRow("800100001234500101", DisplayName = "A roll of no width")]
    [DataRow("72582/1", DisplayName = "The second of a single birth")]
    [DataRow("]C00109521234543213", DisplayName = "A symbology identifier that says the data is not GS1")]
    [DataRow("]Q10109521234543213", DisplayName = "A plain QR code's symbology identifier")]
    public void RejectsWhatTheSpecificationDoesNotAllow(string text)
    {
        Assert.IsFalse(BbGs1ElementString.TryParse(text, out var result), text);
        Assert.IsNull(result);
        Assert.ThrowsExactly<FormatException>(() => BbGs1ElementString.Parse(text));
    }

    [TestMethod]
    [DataRow("43210")]
    [DataRow("43211")]
    [DataRow("0109521234543213" + "17240229", DisplayName = "29 February in a year that is a multiple of four")]
    [DataRow("0109521234543213" + "17261200", DisplayName = "Day 00 where the AI allows it")]
    [DataRow("801012AB-/#", DisplayName = "Character set 39")]
    [DataRow("8030AbC-_=", DisplayName = "Character set 64")]
    [DataRow("10!\"%&'()*+,-./:;<=>?_", DisplayName = "Every punctuation mark of character set 82")]
    [DataRow("2539521234543213", DisplayName = "A GDTI without its serial component")]
    [DataRow("2559521234543213", DisplayName = "A GCN without its serial component")]
    [DataRow("800309521234543213", DisplayName = "A GRAI without its serial component")]
    [DataRow("43242612001430", DisplayName = "A delivery date with day 00")]
    [DataRow("717ABC123", DisplayName = "AI 717, NHRN SRN")]
    [DataRow("8040490154203237518", DisplayName = "AI 8040, an IMEI")]
    [DataRow("72529", DisplayName = "Sex not applicable")]
    [DataRow("80131987654Ad4X4bL5ttr2310c2K", DisplayName = "The GMN of the GS1 General Specifications' example")]
    [DataRow("8007GB82WEST12345698765432", DisplayName = "An IBAN")]
    [DataRow("4301John%20Smith", DisplayName = "A percent-encoded name")]
    [DataRow("43090262718281" + "2061415926", DisplayName = "A latitude and a longitude")]
    [DataRow("80110", DisplayName = "A coupon serial of zero")]
    public void AcceptsWhatTheSpecificationAllows(string text)
    {
        Assert.IsTrue(BbGs1ElementString.TryParse(text, out var result), text);
        Assert.AreEqual(text, result.ToElementString());
    }

    [TestMethod]
    public void TheFormatExceptionSaysWhichAiFailedAndWhy()
    {
        var exception = Assert.ThrowsExactly<FormatException>(() => BbGs1ElementString.Parse($"0109521234543213{GS}17261331"));

        StringAssert.Contains(exception.Message, "(17)");
        StringAssert.Contains(exception.Message, "date");
    }

    [TestMethod]
    public void NullIsNotAnElementString()
    {
        Assert.IsFalse(BbGs1ElementString.TryParse(null, out _));
        Assert.ThrowsExactly<ArgumentNullException>(() => BbGs1ElementString.Parse(null!));
    }

    [TestMethod]
    public void OneAiTwiceWithTheSameValueIsAccepted()
    {
        var elements = BbGs1ElementString.Parse($"10A{GS}10A");

        Assert.HasCount(2, elements.Elements);
        Assert.AreEqual("A", elements["10"]);
    }

    [TestMethod]
    public void ReadsMeasuresAmountsAndPricesWithTheirDecimalPoint()
    {
        var elements = BbGs1ElementString.Parse($"3103001250{GS}3922795{GS}39329781999{GS}30120");

        Assert.IsTrue(elements.Find("3103")!.TryGetDecimal(out var weight));
        Assert.AreEqual(1.25m, weight);
        Assert.AreEqual(3, elements.Find("3103")!.DecimalPlaces);
        Assert.AreEqual("NET WEIGHT (kg)", elements.Find("3103")!.Title);

        Assert.IsTrue(elements.Find("3922")!.TryGetDecimal(out var price));
        Assert.AreEqual(7.95m, price);
        Assert.IsNull(elements.Find("3922")!.CurrencyCode);

        Assert.IsTrue(elements.Find("3932")!.TryGetDecimal(out var euros));
        Assert.AreEqual(19.99m, euros);
        Assert.AreEqual("978", elements.Find("3932")!.CurrencyCode);

        Assert.IsTrue(elements.Find("30")!.TryGetDecimal(out var count));
        Assert.AreEqual(120m, count);
        Assert.IsNull(elements.Find("30")!.DecimalPlaces);
    }

    [TestMethod]
    public void AValueThatIsNotANumberHasNoDecimal()
    {
        var elements = BbGs1ElementString.Parse("10123");

        Assert.IsFalse(elements.Find("10")!.TryGetDecimal(out _));
        Assert.IsFalse(elements.Find("10")!.TryGetDate(2026, out _));
    }

    [TestMethod]
    [DataRow("17261231", 2026, 2026, 12, 31)]
    [DataRow("17240229", 2026, 2024, 2, 29)]
    [DataRow("17260200", 2026, 2026, 2, 28, DisplayName = "Day 00 is the month's last day")]
    [DataRow("17240200", 2026, 2024, 2, 29, DisplayName = "Day 00 in a leap year's February")]
    [DataRow("17750101", 2026, 2075, 1, 1, DisplayName = "49 years ahead is this century")]
    [DataRow("17760101", 2026, 2076, 1, 1, DisplayName = "50 years ahead is this century")]
    [DataRow("17770101", 2026, 1977, 1, 1, DisplayName = "51 years ahead is last century")]
    [DataRow("17010101", 2099, 2101, 1, 1, DisplayName = "98 years behind is next century")]
    [DataRow("7003261231" + "2359", 2026, 2026, 12, 31, DisplayName = "A date and time")]
    [DataRow("725019991231", 2026, 1999, 12, 31, DisplayName = "A four digit year")]
    public void ReadsDatesInTheCenturyTheGs1RulesPlaceThem(string text, int referenceYear, int year, int month, int day)
    {
        var element = BbGs1ElementString.Parse(text).Elements[0];

        Assert.IsTrue(element.TryGetDate(referenceYear, out var date));
        Assert.AreEqual(new DateOnly(year, month, day), date);
    }

    [TestMethod]
    public void TwoDigitYearsAreReadRelativeToTheCurrentYearByDefault()
    {
        var element = BbGs1ElementString.Parse("17" + (DateTime.UtcNow.Year % 100).ToString("D2", System.Globalization.CultureInfo.InvariantCulture) + "0101").Elements[0];

        Assert.IsTrue(element.TryGetDate(out var date));
        Assert.AreEqual(DateTime.UtcNow.Year, date.Year);
    }

    [TestMethod]
    public void ChecksTheCheckDigitsOfEveryKeyThatHasOne()
    {
        var gln = WithCheckDigit("952123456789");
        var sscc = WithCheckDigit("09521234" + "500000001");
        var gsin = WithCheckDigit("9521234" + "500000000");
        var grai = "0" + WithCheckDigit("952123456789") + "SERIAL1";
        var gdti = WithCheckDigit("952123456789") + "ABC";

        var elements = BbGs1ElementString.Parse($"00{sscc}{GS}402{gsin}{GS}414{gln}{GS}8003{grai}{GS}253{gdti}");

        Assert.AreEqual(sscc, elements["00"]);
        Assert.AreEqual(gsin, elements["402"]);
        Assert.AreEqual(gln, elements["414"]);
        Assert.AreEqual(grai, elements["8003"]);
        Assert.AreEqual(gdti, elements["253"]);

        // Each key with its check digit changed.
        static string Wrong(string digits, int at) => digits[..at] + (char)('0' + ((digits[at] - '0' + 1) % 10)) + digits[(at + 1)..];
        Assert.IsFalse(BbGs1ElementString.TryParse("00" + Wrong(sscc, 17), out _));
        Assert.IsFalse(BbGs1ElementString.TryParse("402" + Wrong(gsin, 16), out _));
        Assert.IsFalse(BbGs1ElementString.TryParse("414" + Wrong(gln, 12), out _));
        Assert.IsFalse(BbGs1ElementString.TryParse("8003" + Wrong(grai, 13), out _));
        Assert.IsFalse(BbGs1ElementString.TryParse("253" + Wrong(gdti, 12), out _));

        // A GRAI is fourteen digits, the first of them zero.
        Assert.IsFalse(BbGs1ElementString.TryParse("8003" + "1" + grai[1..], out _));
    }

    [TestMethod]
    public void ReadsOptionalTrailingComponents()
    {
        // AI 7007, a harvest date or a range of two; AI 4330, a temperature with an optional minus sign after it.
        Assert.AreEqual("260101", BbGs1ElementString.Parse("7007260101")["7007"]);
        Assert.AreEqual("260101260115", BbGs1ElementString.Parse("7007260101260115")["7007"]);
        Assert.IsFalse(BbGs1ElementString.TryParse("7007260101260", out _));
        Assert.IsFalse(BbGs1ElementString.TryParse("7007260101261315", out _));

        Assert.AreEqual("001250-", BbGs1ElementString.Parse("4330001250-")["4330"]);
        Assert.IsFalse(BbGs1ElementString.TryParse("4330001250+", out _));
    }

    [TestMethod]
    public void AResultOfAnotherSymbologyHasNoElements()
    {
        using var image = SyntheticImage.FromMatrix(QrEncoder.Encode("0109521234543213"), scale: 4);
        using var decoder = new BbBarcodeDecoder(BbScannerOptions.ForStillImages());

        var result = decoder.Decode(image.View);

        Assert.IsNotNull(result);
        Assert.IsFalse(result.TryGetGs1(out var elements), "A QR code that does not open with FNC1 is not a GS1 symbol, whatever it holds.");
        Assert.IsNull(elements);
    }

    [TestMethod]
    public void AGs1DataBarResultParsesIntoItsElements()
    {
        using var image = SyntheticImage.FromLinear(
            LinearEncoders.DataBarExpanded($"0109521234543213" + "3103001250" + $"10ABC{GS}15261231"),
            scale: 3,
            height: 34 * 3,
            quietZoneModules: 6);
        using var decoder = new BbBarcodeDecoder(new BbScannerOptions { Formats = BbBarcodeFormat.DataBarExpanded });

        var result = decoder.Decode(image.View);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.TryGetGs1(out var elements));
        Assert.AreEqual("(01)09521234543213(3103)001250(10)ABC(15)261231", elements.ToBracketedString());
        Assert.IsTrue(result.TryGetGs1(out var again));
        Assert.AreSame(elements, again, "The elements are parsed once.");
    }
}

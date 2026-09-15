using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.Flag;

[TestClass]
public class BitCountriesTests
{
    [TestMethod]
    public void BitCountriesShouldCarryEveryCountryOfTheTableInAll()
    {
        Assert.IsTrue(BitCountries.All.Length > 200);

        var fields = typeof(BitCountries).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                                         .Where(f => f.FieldType == typeof(BitCountry))
                                         .Select(f => (BitCountry)f.GetValue(null)!)
                                         .ToArray();

        CollectionAssert.AreEquivalent(fields, BitCountries.All);
    }

    [TestMethod]
    public void BitCountriesShouldCarryNoDuplicateIsoCodes()
    {
        var iso2 = BitCountries.All.Select(c => c.Iso2).ToArray();
        var iso3 = BitCountries.All.Select(c => c.Iso3).ToArray();
        var names = BitCountries.All.Select(c => c.Name).ToArray();

        Assert.AreEqual(iso2.Length, iso2.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.AreEqual(iso3.Length, iso3.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.AreEqual(names.Length, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [TestMethod]
    public void BitCountriesShouldCarryWellFormedCodes()
    {
        foreach (var country in BitCountries.All)
        {
            Assert.AreEqual(2, country.Iso2.Length, country.Name);
            Assert.AreEqual(3, country.Iso3.Length, country.Name);
            Assert.IsTrue(country.Iso2.All(char.IsAsciiLetterUpper), country.Name);
            Assert.IsTrue(country.Iso3.All(char.IsAsciiLetterUpper), country.Name);
            Assert.IsFalse(string.IsNullOrWhiteSpace(country.Name));
            Assert.IsFalse(string.IsNullOrWhiteSpace(country.Code));
        }
    }

    [TestMethod,
        DataRow("NL"),
        DataRow("nl"),
        DataRow(" Nl ")]
    public void BitCountriesShouldFindByIso2(string iso2)
    {
        Assert.AreSame(BitCountries.Netherlands, BitCountries.FindByIso2(iso2));
    }

    [TestMethod,
        DataRow("NLD"),
        DataRow("nld"),
        DataRow(" NLd ")]
    public void BitCountriesShouldFindByIso3(string iso3)
    {
        Assert.AreSame(BitCountries.Netherlands, BitCountries.FindByIso3(iso3));
    }

    [TestMethod,
        DataRow("Netherlands"),
        DataRow("netherlands"),
        DataRow(" NETHERLANDS ")]
    public void BitCountriesShouldFindByName(string name)
    {
        Assert.AreSame(BitCountries.Netherlands, BitCountries.FindByName(name));
    }

    [TestMethod,
        DataRow("31"),
        DataRow("+31"),
        DataRow("0031"),
        DataRow(" + 31 "),
        DataRow("(31)")]
    public void BitCountriesShouldFindByCode(string code)
    {
        Assert.AreSame(BitCountries.Netherlands, BitCountries.FindByCode(code));
    }

    [TestMethod,
        DataRow("1-242"),
        DataRow("+1-242"),
        DataRow("+1 (242)"),
        DataRow("1242"),
        DataRow("001 242")]
    public void BitCountriesShouldFindANorthAmericanCodeHoweverItIsPunctuated(string code)
    {
        Assert.AreSame(BitCountries.Bahamas, BitCountries.FindByCode(code));
    }

    [TestMethod]
    public void BitCountriesShouldAnswerASharedDialingCodeWithTheFirstCountryCarryingIt()
    {
        // Alphabetical, so Canada comes before the United States and Kazakhstan before Russia.
        Assert.AreSame(BitCountries.Canada, BitCountries.FindByCode("1"));
        Assert.AreSame(BitCountries.Kazakhstan, BitCountries.FindByCode("7"));
    }

    [TestMethod,
        DataRow("NL"),
        DataRow("NLD"),
        DataRow("Netherlands"),
        DataRow("+31")]
    public void BitCountriesShouldFindWhicheverWayTheCountryIsWritten(string value)
    {
        Assert.AreSame(BitCountries.Netherlands, BitCountries.Find(value));
    }

    [TestMethod]
    public void BitCountriesShouldTellATwoLetterCodeApartFromAThreeLetterOne()
    {
        Assert.AreSame(BitCountries.Austria, BitCountries.Find("AT"));
        Assert.AreSame(BitCountries.Australia, BitCountries.Find("AUS"));

        // "AU" is Australia's alpha-2 and "AUT" is Austria's alpha-3: the length decides which table
        // is asked, so neither answers with the other's country.
        Assert.AreSame(BitCountries.Australia, BitCountries.Find("AU"));
        Assert.AreSame(BitCountries.Austria, BitCountries.Find("AUT"));
    }

    [TestMethod]
    public void BitCountriesShouldReadAThreeDigitValueAsADialingCode()
    {
        Assert.AreSame(BitCountries.Morocco, BitCountries.Find("212"));
    }

    [TestMethod,
        DataRow(null),
        DataRow(""),
        DataRow("   "),
        DataRow("ZZ"),
        DataRow("ZZZ"),
        DataRow("Atlantis"),
        DataRow("00")]
    public void BitCountriesShouldFindNothingForAValueNoCountryCarries(string value)
    {
        Assert.IsNull(BitCountries.Find(value));
        Assert.IsNull(BitCountries.FindByIso2(value));
        Assert.IsNull(BitCountries.FindByIso3(value));
        Assert.IsNull(BitCountries.FindByName(value));
        Assert.IsNull(BitCountries.FindByCode(value));
    }

    [TestMethod,
        DataRow("UK"),
        DataRow("uk"),
        DataRow(" Uk ")]
    public void BitCountriesShouldAnswerTheReservedUkCodeWithTheUnitedKingdom(string iso2)
    {
        // "UK" is no country's alpha-2 code, but ISO 3166-1 reserves it for exactly this country.
        Assert.AreSame(BitCountries.UnitedKingdom, BitCountries.FindByIso2(iso2));
        Assert.AreSame(BitCountries.UnitedKingdom, BitCountries.Find(iso2));
    }

    [TestMethod]
    public void BitCountriesShouldNotReadALongerAliasAsAnAlpha2Code()
    {
        // The alias table carries "USA" and "UAE" as names, and a lookup promising an alpha-2 code
        // does not answer with them.
        Assert.IsNull(BitCountries.FindByIso2("USA"));
        Assert.IsNull(BitCountries.FindByIso2("UAE"));
    }

    [TestMethod,
        DataRow("Czechia", "Czech Republic"),
        DataRow("Türkiye", "Turkey"),
        DataRow("Holland", "Netherlands"),
        DataRow("USA", "United States"),
        DataRow("America", "United States"),
        DataRow("UK", "United Kingdom"),
        DataRow("UAE", "United Arab Emirates"),
        DataRow("Burma", "Myanmar"),
        DataRow("Eswatini", "Swaziland"),
        DataRow("North Macedonia", "Macedonia"),
        DataRow("Cabo Verde", "Cape Verde"),
        DataRow("Timor-Leste", "East Timor"),
        DataRow("Holy See", "Vatican"),
        DataRow("Russian Federation", "Russia"),
        DataRow("Korea, South", "South Korea"),
        DataRow("DRC", "Democratic Republic of the Congo"),
        DataRow("Zaire", "Democratic Republic of the Congo"),
        DataRow("Congo", "Republic of the Congo"),
        DataRow(" czechia ", "Czech Republic")]
    public void BitCountriesShouldFindByAnAlternativeName(string alias, string expected)
    {
        Assert.AreEqual(expected, BitCountries.FindByName(alias)?.Name);
        Assert.AreEqual(expected, BitCountries.Find(alias)?.Name);
    }

    [TestMethod,
        // The accents, the punctuation and the spacing come off both sides of the comparison, so the
        // spelling a second source uses reaches the same country.
        DataRow("Aland Islands", "Åland Islands"),
        DataRow("Curaçao", "Curacao"),
        DataRow("Guinea-Bissau", "Guinea Bissau"),
        DataRow("US Virgin Islands", "U.S. Virgin Islands"),
        DataRow("Côte d'Ivoire", "Ivory Coast"),
        DataRow("Cote dIvoire", "Ivory Coast"),
        DataRow("SaintKittsAndNevis", "Saint Kitts and Nevis")]
    public void BitCountriesShouldIgnoreTheSpellingOfAName(string written, string expected)
    {
        Assert.AreEqual(expected, BitCountries.FindByName(written)?.Name);
    }

    [TestMethod]
    public void BitCountriesShouldNeverLetAnAliasTakeANameFromTheCountryCarryingIt()
    {
        foreach (var country in BitCountries.All)
        {
            Assert.AreSame(country, BitCountries.FindByName(country.Name), country.Name);
            Assert.AreSame(country, BitCountries.FindByIso2(country.Iso2), country.Name);
            Assert.AreSame(country, BitCountries.FindByIso3(country.Iso3), country.Name);
        }
    }

    [TestMethod]
    public void BitCountriesShouldCarryNoTwoNamesThatSpellTheSameWayOnceFolded()
    {
        // Two countries whose names differ only in their accents or their punctuation would make the
        // spelling-insensitive lookup answer one of them for the other.
        var folded = BitCountries.All
                                 .Select(c => new string([.. c.Name.Normalize(System.Text.NormalizationForm.FormD)
                                                                   .Where(char.IsLetterOrDigit)
                                                                   .Select(char.ToUpperInvariant)]))
                                 .ToArray();

        Assert.AreEqual(folded.Length, folded.Distinct(StringComparer.Ordinal).Count());
    }

    [TestMethod]
    public void BitCountriesShouldStillReadATwoDigitValueAsADialingCode()
    {
        // A two character value that is no alpha-2 code falls through to the dialing codes rather than
        // ending the lookup there.
        Assert.AreSame(BitCountries.Netherlands, BitCountries.Find("31"));
    }

    [TestMethod]
    public void BitCountriesShouldKnowWhichFlagsItShips()
    {
        Assert.IsTrue(BitCountries.HasFlag("nl"));
        Assert.IsTrue(BitCountries.HasFlag("NL"));
        Assert.IsFalse(BitCountries.HasFlag("XK"));
        Assert.IsFalse(BitCountries.HasFlag(null));

        // The reserved code reaches the flag the United Kingdom ships under "GB".
        Assert.IsTrue(BitCountries.HasFlag("UK"));
    }

    [TestMethod,
        DataRow("NL", "\U0001F1F3\U0001F1F1"),
        DataRow("nl", "\U0001F1F3\U0001F1F1"),
        DataRow(" us ", "\U0001F1FA\U0001F1F8"),
        // Built rather than looked up, so a code the packaged images do not cover still has one.
        DataRow("XK", "\U0001F1FD\U0001F1F0")]
    public void BitCountriesShouldBuildTheEmojiFlag(string iso2, string expected)
    {
        Assert.AreEqual(expected, BitCountries.GetEmoji(iso2));
    }

    [TestMethod,
        DataRow(null),
        DataRow(""),
        DataRow("N"),
        DataRow("NLD"),
        DataRow("N1"),
        DataRow("12")]
    public void BitCountriesShouldBuildNoEmojiFlagOutOfSomethingThatIsNotTwoLetters(string iso2)
    {
        Assert.IsNull(BitCountries.GetEmoji(iso2));
    }

    [TestMethod,
        DataRow("GB-ENG", "\U0001F3F4\U000E0067\U000E0062\U000E0065\U000E006E\U000E0067\U000E007F"),
        DataRow("gb-sct", "\U0001F3F4\U000E0067\U000E0062\U000E0073\U000E0063\U000E0074\U000E007F"),
        DataRow(" GB-WLS ", "\U0001F3F4\U000E0067\U000E0062\U000E0077\U000E006C\U000E0073\U000E007F"),
        // The hyphen is not part of the sequence, so the code reads the same without it.
        DataRow("GBENG", "\U0001F3F4\U000E0067\U000E0062\U000E0065\U000E006E\U000E0067\U000E007F")]
    public void BitCountriesShouldBuildTheSubdivisionEmojiFlagsUnicodeRecommends(string code, string expected)
    {
        Assert.AreEqual(expected, BitCountries.GetEmoji(code));
    }

    [TestMethod,
        // Every other subdivision draws a plain black flag rather than the flag it names, so none of
        // them is built at all.
        DataRow("US-CA"),
        DataRow("GB-NIR"),
        DataRow("ES-CT"),
        DataRow("GB-")]
    public void BitCountriesShouldBuildNoEmojiFlagForASubdivisionNoFontDraws(string code)
    {
        Assert.IsNull(BitCountries.GetEmoji(code));
    }

    [TestMethod]
    public void BitCountryShouldCarryItsOwnEmojiFlag()
    {
        Assert.AreEqual("\U0001F1EF\U0001F1F5", BitCountries.Japan.Emoji);
    }

    [TestMethod]
    public void EveryCountryOfTheTableShouldHaveAnEmojiFlag()
    {
        var missing = new List<string>();

        foreach (var country in BitCountries.All)
        {
            if (country.Emoji is null) missing.Add(country.Name);
        }

        CollectionAssert.AreEqual(Array.Empty<string>(), missing);
    }

    [TestMethod]
    public void BitCountryShouldPrintItsName()
    {
        Assert.AreEqual("Netherlands", BitCountries.Netherlands.ToString());
    }
}

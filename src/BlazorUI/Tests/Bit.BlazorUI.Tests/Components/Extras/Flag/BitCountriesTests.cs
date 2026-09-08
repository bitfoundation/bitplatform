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

    [TestMethod]
    public void BitCountriesShouldKeepTheHyphenOfANorthAmericanCode()
    {
        Assert.AreSame(BitCountries.Bahamas, BitCountries.FindByCode("1-242"));
        Assert.AreSame(BitCountries.Bahamas, BitCountries.FindByCode("+1-242"));
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

    [TestMethod]
    public void BitCountriesShouldKnowWhichFlagsItShips()
    {
        Assert.IsTrue(BitCountries.HasFlag("nl"));
        Assert.IsTrue(BitCountries.HasFlag("NL"));
        Assert.IsFalse(BitCountries.HasFlag("XK"));
        Assert.IsFalse(BitCountries.HasFlag(null));
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

using System;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils;

[TestClass]
public sealed class BitTypeaheadTests
{
    private CultureInfo? _culture;

    private long _now = 1_000_000;

    [TestInitialize]
    public void UseEnglish()
    {
        _culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
    }

    [TestCleanup]
    public void RestoreCulture()
    {
        if (_culture is not null) CultureInfo.CurrentCulture = _culture;
    }

    private BitTypeahead NewTypeahead() => new(() => _now);

    private static int Find(BitTypeahead typeahead, string key, string?[] items, int current, Func<int, bool>? skip = null) =>
        typeahead.Find(key, items.Length, current, i => items[i], skip);

    [TestMethod]
    public void TheSameCharacterTypedAgainShouldMoveOnToTheNextItemStartingWithIt()
    {
        string[] items = ["Apple", "Avocado", "Banana"];
        var typeahead = NewTypeahead();

        // From Apple, "a" moves on to the next item starting with it, then around the end and on again.
        Assert.AreEqual(1, Find(typeahead, "a", items, 0));
        _now += 100;
        Assert.AreEqual(0, Find(typeahead, "a", items, 1));
        _now += 100;
        Assert.AreEqual(1, Find(typeahead, "a", items, 0));
    }

    [TestMethod]
    public void TheSameCharacterInTheOtherCaseShouldStillBeARepeat()
    {
        string[] items = ["Apple", "Avocado", "Banana"];
        var typeahead = NewTypeahead();

        Assert.AreEqual(1, Find(typeahead, "a", items, 0));
        _now += 100;
        Assert.AreEqual(0, Find(typeahead, "A", items, 1));
    }

    [TestMethod]
    public void KeysTypedTogetherShouldMatchAsOneTermFromTheCurrentItem()
    {
        string[] items = ["Cherry", "Apple", "Apricot"];
        var typeahead = NewTypeahead();

        Assert.AreEqual(1, Find(typeahead, "a", items, -1));
        _now += 100;
        // "ap" still matches Apple, which the keyboard is on, so it stays there rather than moving on.
        Assert.AreEqual(1, Find(typeahead, "p", items, 1));
        _now += 100;
        Assert.AreEqual(2, Find(typeahead, "r", items, 1));
    }

    [TestMethod]
    public void ResetShouldStartANewTermWithTheNextKeyWithoutAPause()
    {
        var typeahead = NewTypeahead();

        Assert.AreEqual(("a", true), typeahead.Type("a"));
        _now += 100;
        Assert.AreEqual(("ap", false), typeahead.Type("p"));

        typeahead.Reset();
        _now += 100;

        // Well within the timeout, yet the key after a reset is a term of its own, and a repeat of it is a repeat.
        Assert.AreEqual(("p", true), typeahead.Type("p"));
        _now += 100;
        Assert.AreEqual(("p", true), typeahead.Type("p"));
    }

    [TestMethod]
    public void IsTypingShouldLastUntilThePauseThatStartsANewTerm()
    {
        var typeahead = NewTypeahead();

        Assert.IsFalse(typeahead.IsTyping);

        typeahead.Type("u");
        Assert.IsTrue(typeahead.IsTyping);

        _now += (long)BitTypeahead.Timeout.TotalMilliseconds;
        Assert.IsTrue(typeahead.IsTyping);

        _now += 1;
        Assert.IsFalse(typeahead.IsTyping);

        typeahead.Type("u");
        typeahead.Reset();
        Assert.IsFalse(typeahead.IsTyping);
    }

    [TestMethod]
    public void APauseLongerThanTheTimeoutShouldStartANewTerm()
    {
        var typeahead = NewTypeahead();

        Assert.AreEqual(("a", true), typeahead.Type("a"));
        _now += (long)BitTypeahead.Timeout.TotalMilliseconds;
        // A pause of the timeout itself still carries the term on.
        Assert.AreEqual(("ap", false), typeahead.Type("p"));
        _now += (long)BitTypeahead.Timeout.TotalMilliseconds + 1;
        Assert.AreEqual(("r", true), typeahead.Type("r"));
    }

    [TestMethod]
    public void TypeShouldReturnTheFirstCharacterOfARepeatAndTheWholeTermOtherwise()
    {
        var typeahead = NewTypeahead();

        Assert.AreEqual(("b", true), typeahead.Type("b"));
        _now += 10;
        Assert.AreEqual(("b", true), typeahead.Type("B"));
        _now += 10;
        Assert.AreEqual(("bBa", false), typeahead.Type("a"));
        _now += 10;
        // Once the term is a real one, typing its first character again does not make it a repeat.
        Assert.AreEqual(("bBab", false), typeahead.Type("b"));
    }

    [TestMethod]
    public void TheItemsNamedBySkipShouldBePassedOver()
    {
        string[] items = ["Apple", "Avocado", "Apricot"];
        var typeahead = NewTypeahead();

        Assert.AreEqual(2, Find(typeahead, "a", items, 0, i => i == 1));
    }

    [TestMethod]
    public void NoMatchOrNoItemsShouldFindNothing()
    {
        string?[] items = ["Apple", null, "Banana"];

        Assert.AreEqual(-1, Find(NewTypeahead(), "z", items, 0));
        Assert.AreEqual(-1, Find(NewTypeahead(), "a", [], -1));
    }

    [TestMethod]
    public void TheSearchShouldStartAtTheFirstItemWhenTheKeyboardIsOnNone()
    {
        string[] items = ["Apple", "Avocado"];

        Assert.AreEqual(0, Find(NewTypeahead(), "a", items, -1));
    }

    [TestMethod,
        DataRow(" Apple", "a", true),
        DataRow("\tApple", "ap", true),
        DataRow("Apple", " a", false),
        DataRow("", "a", false),
        DataRow(null, "a", false),
        DataRow("Apple", "", false)
    ]
    public void MatchesShouldLeaveOutTheWhiteSpaceATextStartsWith(string? text, string term, bool expected)
    {
        Assert.AreEqual(expected, BitTypeahead.Matches(text, term));
    }

    [TestMethod,
        DataRow("en-US", "Église", "e", true),
        DataRow("en-US", "Åland", "a", true),
        DataRow("en-US", "apple", "å", true),
        DataRow("de-DE", "Österreich", "o", true),
        // å is a letter of its own in Swedish, after z: neither of the two stands for the other.
        DataRow("sv-SE", "Åland", "a", false),
        DataRow("sv-SE", "Åland", "å", true),
        DataRow("sv-SE", "apple", "å", false),
        // Turkish pairs the dotted capital with the dotted small i, and the dotless ones with each other.
        DataRow("tr-TR", "İstanbul", "i", true),
        DataRow("tr-TR", "Istanbul", "ı", true),
        DataRow("en-US", "Istanbul", "ı", false)
    ]
    public void MatchesShouldFollowTheRulesOfTheCurrentCulture(string culture, string text, string term, bool expected)
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);

        Assert.AreEqual(expected, BitTypeahead.Matches(text, term));
    }

    [TestMethod]
    public void AnAccentedCharacterShouldRepeatThePlainOneWhereTheCultureFoldsIt()
    {
        string[] items = ["Eagle", "Église", "Fig"];
        var typeahead = NewTypeahead();

        Assert.AreEqual(1, Find(typeahead, "e", items, 0));
        _now += 100;
        // "é" after "e" is the same letter again in English, so it cycles on rather than looking for "eé".
        Assert.AreEqual(0, Find(typeahead, "é", items, 1));
    }
}

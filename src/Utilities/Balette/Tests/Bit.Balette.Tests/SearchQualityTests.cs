namespace Bit.Balette.Tests;

/// <summary>Matching text the way people type it: diacritics, keyboard variants of Arabic-script letters and digits, quoted phrases and typos.</summary>
[TestClass]
public class NormalizationTests
{
    private static readonly BaletteFuzzyMatcher Fuzzy = BaletteFuzzyMatcher.Default;

    [TestMethod]
    [DataRow("cafe", "Café")]
    [DataRow("resume", "Résumé")]
    [DataRow("lodz", "Łódź")]
    [DataRow("sao paulo", "São Paulo")]
    [DataRow("noel", "Noël")]
    [DataRow("dong", "Đồng")]
    [DataRow("ostergotland", "Östergötland")]
    public void A_plain_query_finds_letters_with_diacritics(string query, string title)
        => Assert.IsNotNull(Fuzzy.Match(BaletteCmd.New(title).Build(), query), $"{query} -> {title}");

    [TestMethod]
    public void Positions_point_at_the_original_characters()
    {
        var m = Fuzzy.Match("resume", "Résumé");

        Assert.IsTrue(m.Matched);
        AssertSequence.AreEqual([0, 1, 2, 3, 4, 5], m.Positions);
    }

    [TestMethod]
    public void A_query_that_types_a_diacritic_means_that_letter()
    {
        Assert.IsTrue(Fuzzy.Match("café", "Café").Matched);
        Assert.IsFalse(Fuzzy.Match("café", "Cafe").Matched);
    }

    [TestMethod]
    public void Literal_matching_can_be_asked_for()
    {
        var literal = new BaletteFuzzyMatcher { Normalize = false };

        Assert.IsFalse(literal.Match("cafe", "Café").Matched);
        Assert.IsFalse(literal.Match("علی", "علي").Matched);
        Assert.IsTrue(literal.Match("cafe", "Cafe").Matched);
    }

    [TestMethod]
    [DataRow("علی", "علي")]   // Persian yeh typed, Arabic yeh stored
    [DataRow("علي", "علی")]   // and the other way round
    [DataRow("کتاب", "كتاب")] // keheh / kaf
    [DataRow("اسم", "إسم")]  // alef with hamza below
    [DataRow("مدرسه", "مدرسة")] // teh marbuta
    [DataRow("۱۲", "Order 12")]
    [DataRow("12", "سفارش ۱۲")]
    [DataRow("٣", "Step 3")]
    [DataRow("ab", "ＡＢ")]
    public void Keyboard_variants_of_a_character_are_the_same_character(string query, string text)
        => Assert.IsTrue(Fuzzy.Match(query, text).Matched, $"{query} -> {text}");

    [TestMethod]
    public void Operators_match_normalized_text_too()
    {
        var cafe = BaletteCmd.New("Café au lait").Build();

        Assert.IsNotNull(Fuzzy.Match(cafe, "'cafe"));
        Assert.IsNotNull(Fuzzy.Match(cafe, "^cafe"));
        Assert.IsNotNull(Fuzzy.Match(cafe, "lait$"));
        Assert.IsNull(Fuzzy.Match(cafe, "!cafe"));
        AssertSequence.AreEqual([0, 1, 2, 3], Fuzzy.Match(cafe, "'cafe")!.TitlePositions);
        Assert.IsNotNull(new BaletteSubstringMatcher().Match(cafe, "cafe"));
        Assert.IsNull(new BaletteSubstringMatcher { Normalize = false }.Match(cafe, "cafe"));
    }
}

[TestClass]
public class QuotedPhraseTests
{
    private static readonly BaletteFuzzyMatcher Fuzzy = BaletteFuzzyMatcher.Default;

    [TestMethod]
    public void A_quoted_phrase_is_one_exact_term_spaces_included()
    {
        var match = Fuzzy.Match(BaletteCmd.New("Go to Settings").Build(), "\"go to\"");

        Assert.IsNotNull(match);
        AssertSequence.AreEqual([0, 1, 2, 3, 4], match.TitlePositions);
        Assert.IsNull(Fuzzy.Match(BaletteCmd.New("Go home to bed").Build(), "\"go to\""));
    }

    [TestMethod]
    public void Phrases_take_operators_and_mix_with_plain_terms()
    {
        var settings = BaletteCmd.New("Go to Settings").Build();
        var home = BaletteCmd.New("Go to Home").Build();

        Assert.IsNull(Fuzzy.Match(settings, "!\"to settings\""));
        Assert.IsNotNull(Fuzzy.Match(home, "!\"to settings\""));
        Assert.IsNotNull(Fuzzy.Match(settings, "^\"go to\" sett"));
        Assert.IsNotNull(Fuzzy.Match(settings, "\"to settings\"$"));
        Assert.IsNull(Fuzzy.Match(home, "\"to settings\"$"));
    }

    [TestMethod]
    public void Curly_quotes_and_a_quote_left_open_work_as_people_type_them()
    {
        var settings = BaletteCmd.New("Go to Settings").Build();

        Assert.IsNotNull(Fuzzy.Match(settings, "“go to”"));
        // Still typing: the open phrase is matched as far as it goes.
        Assert.IsNotNull(Fuzzy.Match(settings, "\"go to se"));
        Assert.IsNull(Fuzzy.Match(settings, "\"go to x"));
        // A lone quote is nothing to match yet.
        Assert.IsNotNull(Fuzzy.Match(settings, "\""));
    }
}

[TestClass]
public class TypoToleranceTests
{
    [TestMethod]
    [DataRow("settigns", "Settings")] // transposition
    [DataRow("setings", "Settings")]  // missing letter
    [DataRow("settinngs", "Settings")] // extra letter
    [DataRow("settimgs", "Settings")] // wrong letter
    [DataRow("prefrences", "Open preferences")]
    [DataRow("dashbaord", "Dashboard")]
    public void One_typo_is_forgiven(string query, string text)
        => Assert.IsTrue(new BaletteTypoTolerantMatcher(BaletteFuzzyMatcher.Default).Match(query, text).Matched, $"{query} -> {text}");

    [TestMethod]
    public void Short_terms_and_too_many_edits_are_not()
    {
        var typo = new BaletteTypoTolerantMatcher(BaletteFuzzyMatcher.Default);

        Assert.IsFalse(typo.Match("stx", "Settings").Matched);     // too short to guess at
        Assert.IsFalse(typo.Match("sxtxing", "Settings").Matched);  // two typos in seven letters
        Assert.IsTrue(typo.Match("sxttxings", "Settings").Matched); // two typos in nine letters
        Assert.IsFalse(typo.Match("zzzz", "Settings").Matched);
    }

    [TestMethod]
    public void A_typo_match_scores_below_an_exact_one_and_highlights_what_matched()
    {
        var typo = new BaletteTypoTolerantMatcher(BaletteFuzzyMatcher.Default);
        var approximate = typo.Match("settigns", "Settings");
        var exact = BaletteFuzzyMatcher.Default.Match("settings", "Settings");

        Assert.IsTrue(approximate.Score < exact.Score);
        AssertSequence.AreEqual([0, 1, 2, 3, 4, 5, 6, 7], approximate.Positions);
        Assert.IsTrue(typo.Match("settigns", "Open the settings").Positions.All(p => p >= 9));
    }
}

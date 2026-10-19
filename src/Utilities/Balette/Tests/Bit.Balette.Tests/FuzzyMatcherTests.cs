
namespace Bit.Balette.Tests;

[TestClass]
public class FuzzyMatcherTests
{
    private readonly BaletteFuzzyMatcher _m = new();

    [TestMethod]
    [DataRow("gs", "Go to Settings")]
    [DataRow("settings", "Go to Settings")]
    [DataRow("gts", "Go to Settings")]
    [DataRow("tgl", "Toggle dark mode")]
    public void Matches_subsequences(string query, string text)
    {
        var r = _m.Match(query, text);
        Assert.IsTrue(r.Matched);
        Assert.AreEqual(query.Length, r.Positions.Length);
    }

    [TestMethod]
    [DataRow("xyz", "Go to Settings")]
    [DataRow("settingsx", "Settings")]
    [DataRow("longer than text", "short")]
    public void Rejects_non_matches(string query, string text)
    {
        Assert.IsFalse(_m.Match(query, text).Matched);
    }

    [TestMethod]
    public void Empty_query_matches_everything_with_zero_score()
    {
        var r = _m.Match(string.Empty, "Anything");
        Assert.IsTrue(r.Matched);
        Assert.AreEqual(0, r.Score);
    }

    [TestMethod]
    public void Prefers_word_boundaries_over_mid_word()
    {
        var boundary = _m.Match("op", "Open Project");   // O...P at word starts
        var mid = _m.Match("op", "Scoop it");             // o,p inside a word
        Assert.IsTrue(boundary.Score > mid.Score);
    }

    [TestMethod]
    public void Prefers_consecutive_runs_over_gaps()
    {
        var consecutive = _m.Match("set", "Settings");
        var gapped = _m.Match("set", "Sort elements today");
        Assert.IsTrue(consecutive.Score > gapped.Score);
    }

    [TestMethod]
    public void Rewards_camelCase_humps()
    {
        var camel = _m.Match("fb", "fooBar");
        var plain = _m.Match("fb", "foobar");
        Assert.IsTrue(camel.Score > plain.Score);
    }

    [TestMethod]
    public void Positions_point_at_matched_characters()
    {
        var r = _m.Match("gs", "Go to Settings");
        Assert.AreEqual('G', "Go to Settings"[r.Positions[0]]);
        Assert.AreEqual('S', "Go to Settings"[r.Positions[1]]);
        AssertSequence.AreEqual(new[] { 0, 6 }, r.Positions);
    }

    [TestMethod]
    public void Positions_are_strictly_increasing()
    {
        var text = "Toggle the sidebar visibility";
        var r = _m.Match("tsv", text);
        Assert.IsTrue(r.Matched);
        for (var i = 1; i < r.Positions.Length; i++)
        {
            Assert.IsTrue(r.Positions[i] > r.Positions[i - 1]);
        }
    }

    [TestMethod]
    public void Smart_case_lowercase_query_is_case_insensitive()
    {
        Assert.IsTrue(_m.Match("open", "OPEN FILE").Matched);
    }

    [TestMethod]
    public void Smart_case_uppercase_query_is_case_sensitive()
    {
        Assert.IsFalse(_m.Match("OPEN", "open file").Matched);
        Assert.IsTrue(_m.Match("OPEN", "OPEN file").Matched);
    }

    [TestMethod]
    public void Exact_prefix_beats_scattered_match_in_ranking()
    {
        var cmds = new[]
        {
            BaletteCmd.New("Preferences: Open Settings").Build(),
            BaletteCmd.New("Settings").Build(),
            BaletteCmd.New("Set Timer").Build(),
        };
        var ranked = cmds.Select(c => _m.Match(c, "settings")).Where(m => m is not null).OrderByDescending(m => m!.Score).ToList();
        Assert.AreEqual("Settings", ranked[0]!.Command.Title);
    }

    [TestMethod]
    public void Substring_matcher_matches_contiguously_only()
    {
        var s = BaletteSubstringMatcher.Default;
        Assert.IsTrue(s.Match("set", "Settings").Matched);
        Assert.IsFalse(s.Match("sts", "Settings").Matched);
    }
}

[TestClass]
public class CommandMatcherExtensionsTests
{
    private readonly BaletteFuzzyMatcher _m = new();

    [TestMethod]
    public void Matches_keywords_and_reports_which_one()
    {
        var cmd = BaletteCmd.New("Toggle theme").Keywords("dark", "light", "appearance").Build();
        var r = _m.Match(cmd, "appear");
        Assert.IsNotNull(r);
        Assert.AreEqual("appearance", r!.MatchedKeyword);
    }

    [TestMethod]
    public void Title_outranks_keyword_match()
    {
        var titleHit = BaletteCmd.New("Dark mode").Build();
        var keywordHit = BaletteCmd.New("Appearance").Keywords("dark mode").Build();
        var a = _m.Match(titleHit, "dark")!;
        var b = _m.Match(keywordHit, "dark")!;
        Assert.IsTrue(a.Score > b.Score);
        Assert.IsNotEmpty(a.TitlePositions);
        Assert.IsEmpty(b.TitlePositions);
    }

    [TestMethod]
    public void Multi_term_query_requires_all_terms()
    {
        var cmd = BaletteCmd.New("Open recent project").Build();
        Assert.IsNotNull(_m.Match(cmd, "open proj"));
        Assert.IsNull(_m.Match(cmd, "open banana"));
    }

    [TestMethod]
    public void Negation_operator_excludes()
    {
        var cmd = BaletteCmd.New("Delete workspace").Build();
        Assert.IsNull(_m.Match(cmd, "work !delete"));
        Assert.IsNotNull(_m.Match(cmd, "work !rename"));
    }

    [TestMethod]
    public void Exact_prefix_suffix_operators()
    {
        var cmd = BaletteCmd.New("Go to Settings").Build();
        Assert.IsNotNull(_m.Match(cmd, "'to set"));
        Assert.IsNull(_m.Match(cmd, "'tst"));
        Assert.IsNotNull(_m.Match(cmd, "^go"));
        Assert.IsNull(_m.Match(cmd, "^set"));
        Assert.IsNotNull(_m.Match(cmd, "ings$"));
        Assert.IsNull(_m.Match(cmd, "go$"));
    }

    [TestMethod]
    public void Subtitle_positions_are_reported()
    {
        var cmd = BaletteCmd.New("Invoice 42").Subtitle("Acme Corporation").Build();
        var r = _m.Match(cmd, "acme")!;
        AssertSequence.AreEqual(new[] { 0, 1, 2, 3 }, r.SubtitlePositions);
    }
}

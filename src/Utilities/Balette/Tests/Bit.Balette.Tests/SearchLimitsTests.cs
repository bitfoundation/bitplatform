namespace Bit.Balette.Tests;

[TestClass]
public class SearchLimitsTests
{
    private static readonly BaletteCommand Settings = BaletteCmd.New("Go to Settings")
        .Subtitle("Preferences: open the settings editor")
        .Keywords("preferences", "options")
        .Group("Navigation")
        .Description("Opens the settings page of the app.")
        .Build();

    private static readonly BaletteCommand[] Commands =
    [
        Settings,
        BaletteCmd.New("Toggle dark mode").Subtitle("Switch the theme").Keywords("theme", "appearance").Group("View").Build(),
        BaletteCmd.New("Open recent project").Subtitle("Recently opened folders").Tags("file").Build(),
        BaletteCmd.New("Delete workspace").Description("Removes the workspace and its settings.").Build(),
    ];

    private static readonly string[] Queries =
    [
        "set", "go set", "set set", "set go set", "pref set", "edit sett", "s s s", "op re", "open proj", "^go set",
        "'set edit", "ings$ go", "theme dark", "togg !delete", "work !delete", "dark dark mode", "e e e e e", "o o rec",
    ];

    private static readonly string Prose = string.Join(" ", Enumerable.Repeat("the quick brown fox jumps over the lazy dog", 50));

    [TestMethod]
    [DataRow("set", 79.66, new[] { 6, 7, 8 }, new int[] { }, null)]
    [DataRow("go set", 135.38, new[] { 0, 1, 6, 7, 8 }, new int[] { }, null)]
    [DataRow("set set set", 238.98, new[] { 6, 7, 8 }, new int[] { }, null)]
    [DataRow("'set ^go", 116d, new[] { 0, 1, 6, 7, 8 }, new int[] { }, null)]
    [DataRow("ings$ go", 135.72, new[] { 0, 1, 10, 11, 12, 13 }, new int[] { }, null)]
    [DataRow("pref set", 173.062, new[] { 6, 7, 8 }, new int[] { }, "preferences")]
    [DataRow("s s s s", 126.64, new[] { 6 }, new int[] { }, null)]
    [DataRow("opt go", 127.594, new[] { 0, 1 }, new int[] { }, "options")]
    [DataRow("set | xyz go", 135.38, new[] { 0, 1, 6, 7, 8 }, new int[] { }, null)]
    [DataRow("go !delete", 55.72, new[] { 0, 1 }, new int[] { }, null)]
    [DataRow("edit sett", 175.725, new[] { 6, 7, 8, 9 }, new[] { 31, 32, 33, 34 }, null)]
    [DataRow("nav go", 95.62, new[] { 0, 1 }, new int[] { }, null)]
    public void Scores_and_highlights_stay_as_they_were(string query, double score, int[] title, int[] subtitle, string? keyword)
    {
        var match = BaletteFuzzyMatcher.Default.Match(Settings, query);

        Assert.IsNotNull(match);
        Assert.AreEqual(score, match.Score, 1e-9);
        AssertSequence.AreEqual(title, match.TitlePositions);
        AssertSequence.AreEqual(subtitle, match.SubtitlePositions);
        Assert.AreEqual(keyword, match.MatchedKeyword);
    }

    [TestMethod]
    [DataRow("quickfox", 146.98, new[] { 4, 5, 6, 7, 8, 16, 17, 18 })]
    [DataRow("lazydogs", 117.67, new[] { 35, 36, 37, 38, 40, 41, 42, 68 })]
    [DataRow("brownjumps", 196.92, new[] { 10, 11, 12, 13, 14, 20, 21, 22, 23, 24 })]
    public void A_field_past_the_retained_buffers_scores_as_it_did(string query, double score, int[] positions)
    {
        var match = BaletteFuzzyMatcher.Default.Match(query, Prose);

        Assert.IsTrue(match.Matched);
        Assert.AreEqual(score, match.Score, 1e-9);
        AssertSequence.AreEqual(positions, match.Positions);
    }

    [TestMethod]
    public void A_long_subtitle_highlights_as_it_did()
    {
        var match = BaletteFuzzyMatcher.Default.Match(BaletteCmd.New("Read the article").Subtitle(Prose).Build(), "quickfox lazy");

        Assert.IsNotNull(match);
        Assert.AreEqual(144.655, match.Score, 1e-9);
        AssertSequence.AreEqual([4, 5, 6, 7, 8, 16, 17, 18, 35, 36, 37, 38], match.SubtitlePositions);
    }

    [TestMethod]
    public void Terms_together_score_and_highlight_as_each_term_alone()
    {
        foreach (var command in Commands)
        {
            foreach (var query in Queries)
            {
                var together = BaletteFuzzyMatcher.Default.Match(command, query);
                var alone = query.Split(' ').Select(term => BaletteFuzzyMatcher.Default.Match(command, term)).ToArray();
                var label = $"{command.Title}: {query}";

                if (alone.Any(match => match is null))
                {
                    Assert.IsNull(together, label);
                    continue;
                }

                Assert.IsNotNull(together, label);
                Assert.AreEqual(alone.Sum(match => match!.Score), together.Score, 1e-9, label);
                AssertSequence.AreEqual(alone.SelectMany(match => match!.TitlePositions).Distinct().Order(), together.TitlePositions);
                AssertSequence.AreEqual(alone.SelectMany(match => match!.SubtitlePositions).Distinct().Order(), together.SubtitlePositions);
                Assert.AreEqual(alone.Select(match => match!.MatchedKeyword).FirstOrDefault(keyword => keyword is not null), together.MatchedKeyword, label);
            }
        }
    }

    [TestMethod]
    public void Many_terms_collect_their_highlights_in_linear_time()
    {
        var matcher = new EveryCharacterMatcher();
        var command = BaletteCmd.New(new string('a', EveryCharacterMatcher.Length)).Build();
        var query = string.Join(' ', Enumerable.Repeat("a", 128));

        matcher.Match(command, "a");
        var before = GC.GetAllocatedBytesForCurrentThread();
        var match = matcher.Match(command, query);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.IsNotNull(match);
        AssertSequence.AreEqual(Enumerable.Range(0, EveryCharacterMatcher.Length), match.TitlePositions);
        Assert.IsLessThan(8_000_000L, allocated);
    }

    [TestMethod]
    public void A_query_longer_than_the_limit_matches_nothing()
    {
        var atLimit = string.Join(' ', Enumerable.Repeat("s", 128)) + " ";
        var pastLimit = atLimit + "s";

        Assert.AreEqual(BaletteQuery.MaxLength, atLimit.Length);
        Assert.IsNotNull(BaletteFuzzyMatcher.Default.Match(Settings, atLimit));
        Assert.IsNull(BaletteFuzzyMatcher.Default.Match(Settings, pastLimit));
        Assert.IsNull(BaletteFuzzyMatcher.Default.Match(Settings, BaletteQuery.Parse(pastLimit)));
        Assert.IsEmpty(BaletteQuery.Parse(pastLimit).Terms);
    }

    [TestMethod]
    public void A_comparison_past_the_cell_budget_does_not_match()
    {
        var text = new string('a', 2_000);

        Assert.IsTrue(BaletteFuzzyMatcher.Default.Match(new string('a', 500), text).Matched);
        Assert.IsFalse(BaletteFuzzyMatcher.Default.Match(new string('a', 600), text).Matched);
        Assert.IsFalse(BaletteFuzzyMatcher.Default.Match(new string('a', 50_000), new string('a', 50_000)).Matched);
    }

    [TestMethod]
    public void A_long_miss_allocates_no_score_tables()
    {
        var query = new string('b', 100);
        var text = new string('a', 5_000);

        BaletteFuzzyMatcher.Default.Match(query, text);
        var before = GC.GetAllocatedBytesForCurrentThread();
        var match = BaletteFuzzyMatcher.Default.Match(query, text);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.IsFalse(match.Matched);
        Assert.IsLessThan(1_000_000L, allocated);
    }

    [TestMethod]
    public void Typo_tolerance_keeps_to_the_same_cell_budget()
    {
        var text = new string('a', 2_000);

        Assert.IsTrue(BaletteTypoTolerantMatcher.Approximate(new string('a', 500), text, 2, true).Matched);
        Assert.IsFalse(BaletteTypoTolerantMatcher.Approximate(new string('a', 600), text, 2, true).Matched);
    }

    private sealed class EveryCharacterMatcher : IBaletteMatcher
    {
        public const int Length = 1_000;

        private static readonly int[] All = [.. Enumerable.Range(0, Length)];

        public BaletteFieldMatch Match(string query, string text) => new(true, 1, All);
    }
}

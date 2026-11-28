using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Bit.BlazorUI.Tests.Components.Extras.CommandPalette;

[TestClass]
public class BitCommandPaletteSearchLimitsTests
{
    private static readonly BitCommandPaletteCommand Settings = BitCommandPaletteCmd.New("Go to Settings")
        .Subtitle("Preferences: open the settings editor")
        .Keywords("preferences", "options")
        .Group("Navigation")
        .Description("Opens the settings page of the app.")
        .Build();

    private static readonly BitCommandPaletteCommand[] Commands =
    [
        Settings,
        BitCommandPaletteCmd.New("Toggle dark mode").Subtitle("Switch the theme").Keywords("theme", "appearance").Group("View").Build(),
        BitCommandPaletteCmd.New("Open recent project").Subtitle("Recently opened folders").Tags("file").Build(),
        BitCommandPaletteCmd.New("Delete workspace").Description("Removes the workspace and its settings.").Build(),
    ];

    private static readonly string[] Queries =
    [
        "set", "go set", "set set", "set go set", "pref set", "edit sett", "s s s", "op re", "open proj", "^go set",
        "'set edit", "ings$ go", "theme dark", "togg !delete", "work !delete", "dark dark mode", "e e e e e", "o o rec",
    ];

    private static readonly string Prose = string.Join(" ", Enumerable.Repeat("the quick brown fox jumps over the lazy dog", 50));

    private static readonly string Article = string.Join(" ", Enumerable.Repeat("lorem ipsum dolor sit amet", 7_000)) + " kubernetes";

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
        var match = BitCommandPaletteFuzzyMatcher.Default.Match(Settings, query);

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
        var match = BitCommandPaletteFuzzyMatcher.Default.Match(query, Prose);

        Assert.IsTrue(match.Matched);
        Assert.AreEqual(score, match.Score, 1e-9);
        AssertSequence.AreEqual(positions, match.Positions);
    }

    [TestMethod]
    public void A_long_subtitle_highlights_as_it_did()
    {
        var match = BitCommandPaletteFuzzyMatcher.Default.Match(BitCommandPaletteCmd.New("Read the article").Subtitle(Prose).Build(), "quickfox lazy");

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
                var together = BitCommandPaletteFuzzyMatcher.Default.Match(command, query);
                var alone = query.Split(' ').Select(term => BitCommandPaletteFuzzyMatcher.Default.Match(command, term)).ToArray();
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
        var command = BitCommandPaletteCmd.New(new string('a', EveryCharacterMatcher.Length)).Build();
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
        // As many one-letter terms as fit, so the limit is reached by the query as Parse reads it: the spaces between terms
        // count, the spaces around the query do not.
        var atLimit = "s  " + string.Join(' ', Enumerable.Repeat("s", 127));
        var pastLimit = atLimit + "s";

        Assert.AreEqual(BitCommandPaletteQuery.MaxLength, atLimit.Length);
        Assert.AreEqual(BitCommandPaletteQuery.MaxLength + 1, pastLimit.Trim().Length);
        Assert.IsFalse(BitCommandPaletteQuery.Parse($" {atLimit} ").IsTooLong);

        var match = BitCommandPaletteFuzzyMatcher.Default.Match(Settings, atLimit);

        Assert.IsNotNull(match);
        Assert.IsGreaterThan(0d, match.Score);
        AssertSequence.AreEqual([6], match.TitlePositions);
        Assert.IsNull(BitCommandPaletteFuzzyMatcher.Default.Match(Settings, pastLimit));
        Assert.IsNull(BitCommandPaletteFuzzyMatcher.Default.Match(Settings, BitCommandPaletteQuery.Parse(pastLimit)));
        Assert.IsEmpty(BitCommandPaletteQuery.Parse(pastLimit).Terms);
        Assert.IsTrue(BitCommandPaletteQuery.Parse(pastLimit).IsTooLong);
        Assert.IsFalse(BitCommandPaletteQuery.Parse(atLimit).IsTooLong);
        Assert.IsFalse(BitCommandPaletteQuery.Empty.IsTooLong);
    }

    [TestMethod]
    public void Spaces_around_a_query_do_not_count_toward_the_limit()
    {
        var padded = new string(' ', BitCommandPaletteQuery.MaxLength) + "set" + new string(' ', BitCommandPaletteQuery.MaxLength);

        Assert.IsFalse(BitCommandPaletteQuery.Parse(padded).IsTooLong);
        AssertSequence.AreEqual([6, 7, 8], BitCommandPaletteFuzzyMatcher.Default.Match(Settings, padded)!.TitlePositions);
    }

    [TestMethod]
    public void A_comparison_past_the_cell_budget_aligns_greedily()
    {
        var text = new string('a', 2_000);

        // A query longer than any term the palette parses: only a direct call goes past the table's budget.
        Assert.IsGreaterThan(BitCommandPaletteQuery.MaxLength, 600);
        Assert.IsGreaterThan((long)BitCommandPaletteFuzzyMatcher.MaxCells, 601L * 2_001);
        AssertSequence.AreEqual(Enumerable.Range(0, 600),BitCommandPaletteFuzzyMatcher.Default.Match(new string('a', 600), text).Positions);
        Assert.IsTrue(BitCommandPaletteFuzzyMatcher.Default.Match(new string('a', 50_000), new string('a', 50_000)).Matched);
        Assert.IsFalse(BitCommandPaletteFuzzyMatcher.Default.Match(new string('b', 600), text).Matched);
    }

    [TestMethod]
    public void A_term_in_a_very_long_field_is_still_found_and_highlighted()
    {
        var match = BitCommandPaletteFuzzyMatcher.Default.Match("kubernetes", Article);

        Assert.IsGreaterThan(BitCommandPaletteFuzzyMatcher.MaxOptimalLength, Article.Length);
        Assert.IsTrue(match.Matched);
        AssertSequence.AreEqual(Enumerable.Range(Article.Length - 10, 10), match.Positions);
        Assert.IsNotNull(BitCommandPaletteFuzzyMatcher.Default.Match(BitCommandPaletteCmd.New("Article").Description(Article).Build(), "kube nets"));
    }

    [TestMethod]
    public void A_field_keeps_its_alignment_whatever_the_length_of_the_query()
    {
        // Every term the palette parses fits the table against every field aligned optimally, so the field alone decides.
        Assert.IsLessThanOrEqualTo(BitCommandPaletteFuzzyMatcher.MaxCells, (BitCommandPaletteQuery.MaxLength + 1) * (BitCommandPaletteFuzzyMatcher.MaxOptimalLength + 1));

        // The longest term spelled out at the start, where the greedy alignment takes it, and as a word at the end, where the
        // optimal one does - for the term and for its first three letters alike.
        var term = string.Concat(Enumerable.Repeat("set", 86))[..BitCommandPaletteQuery.MaxLength];
        var spelled = string.Join(' ', term.ToCharArray()) + " ";
        var optimal = spelled + new string('x', BitCommandPaletteFuzzyMatcher.MaxOptimalLength - spelled.Length - term.Length - 1) + " " + term;
        var greedy = optimal + "x";

        Assert.AreEqual(BitCommandPaletteFuzzyMatcher.MaxOptimalLength, optimal.Length);
        foreach (var query in new[] { "set", term })
        {
            AssertSequence.AreEqual(Enumerable.Range(optimal.Length - term.Length, query.Length), BitCommandPaletteFuzzyMatcher.Default.Match(query, optimal).Positions);
            AssertSequence.AreEqual(Enumerable.Range(0, query.Length).Select(i => 2 * i), BitCommandPaletteFuzzyMatcher.Default.Match(query, greedy).Positions);
        }
    }

    [TestMethod]
    [DataRow("set", "Go to Settings")]
    [DataRow("gs", "Go to Settings")]
    [DataRow("GS", "Go to Settings")]
    [DataRow("fm", "FuzzyMatcher")]
    [DataRow("edit", "Preferences: open the settings editor")]
    [DataRow("prefs", "Preferences: open the settings editor")]
    public void The_greedy_alignment_scores_as_the_table_does_when_they_agree(string query, string text)
    {
        var optimal = BitCommandPaletteFuzzyMatcher.Default.Match(query, text);
        var greedy = BitCommandPaletteFuzzyMatcher.Default.Match(query, text, greedy: true);

        AssertSequence.AreEqual(optimal.Positions, greedy.Positions);
        Assert.AreEqual(optimal.Score, greedy.Score, 1e-9);
    }

    [TestMethod]
    public void Wherever_the_two_alignments_agree_they_score_the_same()
    {
        // Both score by the same rules; this holds them to it over texts nobody picked by hand.
        var random = new Random(14505);
        const string Letters = "abcAB _-1";
        var agreed = 0;
        for (var n = 0; n < 5_000; n++)
        {
            var text = new string([.. Enumerable.Range(0, random.Next(1, 40)).Select(_ => Letters[random.Next(Letters.Length)])]);
            var query = new string([.. text.Where(_ => random.Next(4) == 0).Take(6)]);
            if (query.Length == 0)
            {
                continue;
            }

            var optimal = BitCommandPaletteFuzzyMatcher.Default.Match(query, text);
            var greedy = BitCommandPaletteFuzzyMatcher.Default.Match(query, text, greedy: true);
            var label = $"'{query}' in '{text}'";

            Assert.IsTrue(optimal.Matched, label);
            Assert.IsTrue(greedy.Matched, label);
            if (optimal.Positions.SequenceEqual(greedy.Positions))
            {
                Assert.AreEqual(optimal.Score, greedy.Score, 1e-9, label);
                agreed++;
            }
        }

        Assert.IsGreaterThan(1_000, agreed);
    }

    [TestMethod]
    [DataRow("quickfox", 146.98, new[] { 4, 5, 6, 7, 8, 16, 17, 18 })]
    [DataRow("lazydogs", 117.67, new[] { 35, 36, 37, 38, 40, 41, 42, 68 })]
    [DataRow("brownjumps", 196.92, new[] { 10, 11, 12, 13, 14, 20, 21, 22, 23, 24 })]
    public void A_long_field_aligned_greedily_scores_as_the_table_did(string query, double score, int[] positions)
    {
        var match = BitCommandPaletteFuzzyMatcher.Default.Match(query, Prose, greedy: true);

        Assert.AreEqual(score, match.Score, 1e-9);
        AssertSequence.AreEqual(positions, match.Positions);
    }

    [TestMethod]
    public void A_field_allocates_nothing_but_its_positions()
    {
        // The longest field aligned optimally against a long term - the thread's own buffers at their largest, which the
        // first comparison grows and no collection takes back - and a field past it, which needs no buffers at all.
        var description = string.Concat(Enumerable.Repeat("lorem ipsum ", BitCommandPaletteFuzzyMatcher.MaxOptimalLength / 12));
        var term = string.Concat(Enumerable.Repeat("lorem", BitCommandPaletteQuery.MaxLength / 5));

        BitCommandPaletteFuzzyMatcher.Default.Match(term, description);
        BitCommandPaletteFuzzyMatcher.Default.Match("kubernetes", Article);
        var before = GC.GetAllocatedBytesForCurrentThread();
        var scored = BitCommandPaletteFuzzyMatcher.Default.Match(term, description);
        var aligned = BitCommandPaletteFuzzyMatcher.Default.Match("kubernetes", Article);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.IsTrue(scored.Matched);
        Assert.IsTrue(aligned.Matched);
        Assert.IsLessThan(2_000L, allocated);
    }

    [TestMethod]
    public void A_long_miss_allocates_no_score_tables()
    {
        var query = new string('b', 100);
        var text = new string('a', 5_000);

        BitCommandPaletteFuzzyMatcher.Default.Match(query, text);
        var before = GC.GetAllocatedBytesForCurrentThread();
        var match = BitCommandPaletteFuzzyMatcher.Default.Match(query, text);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.IsFalse(match.Matched);
        Assert.IsLessThan(1_000L, allocated);
    }

    [TestMethod]
    public void A_typo_in_a_very_long_field_is_still_found()
    {
        var command = BitCommandPaletteCmd.New("Article").Description(Article).Build();
        var match = BitCommandPaletteTypoTolerantMatcher.Approximate("kubrenetes", Article, 2, true);
        var word = WholeTable("kubrenetes", " kubernetes", 2, true);

        Assert.IsTrue(match.Matched);
        AssertSequence.AreEqual(word.Positions.Select(position => position + Article.Length - 11), match.Positions);
        Assert.AreEqual(word.Score - (0.02 * (Article.Length - 11)), match.Score, 1e-9);
        Assert.IsNull(BitCommandPaletteFuzzyMatcher.Default.Match(command, "kubrenetes"));
        Assert.IsNotNull(new BitCommandPaletteTypoTolerantMatcher(BitCommandPaletteFuzzyMatcher.Default).Match(command, "kubrenetes"));
    }

    [TestMethod]
    public void Typo_tolerance_reads_a_long_field_without_a_table()
    {
        var query = new string('b', 20);
        var text = new string('a', 100_000);

        BitCommandPaletteTypoTolerantMatcher.Approximate(query, text, 2, true);
        BitCommandPaletteTypoTolerantMatcher.Approximate("kubrenetes", Article, 2, true);
        var before = GC.GetAllocatedBytesForCurrentThread();
        var missed = BitCommandPaletteTypoTolerantMatcher.Approximate(query, text, 2, true);
        var found = BitCommandPaletteTypoTolerantMatcher.Approximate("kubrenetes", Article, 2, true);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.IsFalse(missed.Matched);
        Assert.IsTrue(found.Matched);
        Assert.IsLessThan(2_000L, allocated);
    }

    [TestMethod]
    public void Typo_tolerance_stops_at_terms_longer_than_a_word_of_its_columns()
    {
        var text = new string('a', 200);

        Assert.AreEqual(2, BitCommandPaletteTypoTolerantMatcher.MaxEdits(BitCommandPaletteTypoTolerantMatcher.MaxTermLength));
        Assert.AreEqual(0, BitCommandPaletteTypoTolerantMatcher.MaxEdits(BitCommandPaletteTypoTolerantMatcher.MaxTermLength + 1));
        AssertSequence.AreEqual(Enumerable.Range(0, 63), BitCommandPaletteTypoTolerantMatcher.Approximate(new string('a', 63) + "b", text, 2, true).Positions);
        Assert.IsFalse(BitCommandPaletteTypoTolerantMatcher.Approximate(new string('a', 64) + "b", text, 2, true).Matched);
    }

    [TestMethod]
    public void Typo_tolerance_finds_what_the_whole_table_finds()
    {
        // The columns computed a machine word at a time, and the few walked back, against the whole table they stand for.
        var random = new Random(14505);
        const string Letters = "abcdAB é-";
        var found = 0;
        for (var n = 0; n < 20_000; n++)
        {
            var text = new string([.. Enumerable.Range(0, random.Next(0, 30)).Select(_ => Letters[random.Next(Letters.Length)])]);
            var query = new string([.. Enumerable.Range(0, random.Next(1, 12)).Select(_ => Letters[random.Next(Letters.Length)])]);
            var maxEdits = random.Next(1, 3);
            var normalize = n % 2 == 0;
            var expected = WholeTable(query, text, maxEdits, normalize);
            var actual = BitCommandPaletteTypoTolerantMatcher.Approximate(query, text, maxEdits, normalize);
            var label = $"'{query}' in '{text}', {maxEdits} edits";

            Assert.AreEqual(expected.Matched, actual.Matched, label);
            if (expected.Matched)
            {
                Assert.AreEqual(expected.Score, actual.Score, 1e-9, label);
                AssertSequence.AreEqual(expected.Positions, actual.Positions);
                found++;
            }
        }

        Assert.IsGreaterThan(2_000, found);

        // Row 0 is the free start: the start of " set" costs nothing.
        var exact = BitCommandPaletteTypoTolerantMatcher.Approximate("set", new string('x', 26) + " set", 1, true);

        Assert.AreEqual((3 * 16) + 16 - (0.02 * 30), exact.Score, 1e-9);
        AssertSequence.AreEqual([27, 28, 29], exact.Positions);
    }

    /// <summary>Optimal string alignment over the whole table, stored - what <see cref="BitCommandPaletteTypoTolerantMatcher.Approximate"/> computes without it.</summary>
    private static BitCommandPaletteFieldMatch WholeTable(string query, string text, int maxEdits, bool normalize)
    {
        int p = query.Length, t = text.Length;
        if (p - maxEdits > t)
        {
            return BitCommandPaletteFieldMatch.None;
        }

        var strip = normalize && !BitCommandPaletteTextFold.HasDiacritic(query);
        var q = query.Select(c => BitCommandPaletteTextFold.Fold(c, true, normalize, strip)).ToArray();
        var x = text.Select(c => BitCommandPaletteTextFold.Fold(c, true, normalize, strip)).ToArray();
        var w = t + 1;
        var d = new int[(p + 1) * w];
        for (var i = 1; i <= p; i++)
        {
            d[i * w] = i;
            for (var j = 1; j <= t; j++)
            {
                var v = Math.Min(d[((i - 1) * w) + j] + 1, d[(i * w) + j - 1] + 1);
                v = Math.Min(v, d[((i - 1) * w) + j - 1] + (q[i - 1] == x[j - 1] ? 0 : 1));
                if (i > 1 && j > 1 && q[i - 1] == x[j - 2] && q[i - 2] == x[j - 1])
                {
                    v = Math.Min(v, d[((i - 2) * w) + j - 2] + 1);
                }

                d[(i * w) + j] = v;
            }
        }

        var edits = int.MaxValue;
        var end = 0;
        for (var j = 1; j <= t; j++)
        {
            if (d[(p * w) + j] < edits)
            {
                edits = d[(p * w) + j];
                end = j;
            }
        }

        if (edits > maxEdits)
        {
            return BitCommandPaletteFieldMatch.None;
        }

        var positions = new List<int>(p);
        int qi = p, tj = end;
        while (qi > 0 && tj > 0)
        {
            var cur = d[(qi * w) + tj];
            if (q[qi - 1] == x[tj - 1] && cur == d[((qi - 1) * w) + tj - 1])
            {
                positions.Add(tj - 1);
                qi--;
                tj--;
            }
            else if (qi > 1 && tj > 1 && q[qi - 1] == x[tj - 2] && q[qi - 2] == x[tj - 1] && cur == d[((qi - 2) * w) + tj - 2] + 1)
            {
                positions.Add(tj - 1);
                positions.Add(tj - 2);
                qi -= 2;
                tj -= 2;
            }
            else if (cur == d[((qi - 1) * w) + tj - 1] + 1)
            {
                qi--;
                tj--;
            }
            else if (cur == d[((qi - 1) * w) + tj] + 1)
            {
                qi--;
            }
            else
            {
                tj--;
            }
        }

        positions.Sort();
        var atWordStart = tj == 0 || !char.IsLetterOrDigit(text[tj - 1]);
        var score = ((p - edits) * BitCommandPaletteFuzzyMatcher.ScoreMatch) - (edits * BitCommandPaletteTypoTolerantMatcher.EditPenalty)
            + (atWordStart ? BitCommandPaletteFuzzyMatcher.BonusBoundary * 2 : 0) - (0.02 * t);
        return new BitCommandPaletteFieldMatch(true, score, [.. positions]);
    }

    private sealed class EveryCharacterMatcher : IBitCommandPaletteMatcher
    {
        public const int Length = 1_000;

        private static readonly int[] All = [.. Enumerable.Range(0, Length)];

        public BitCommandPaletteFieldMatch Match(string query, string text) => new(true, 1, All);
    }
}

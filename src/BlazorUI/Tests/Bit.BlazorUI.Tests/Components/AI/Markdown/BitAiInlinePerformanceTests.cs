using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Markdown;

/// <summary>
/// Guards the inline parser's linear-time claim on the inputs the CommonMark delimiter algorithm
/// exists to cope with. Each of these took seconds, some of them minutes, while a scan was quadratic.
/// </summary>
/// <remarks>
/// Every timing is the best of several runs, and every limit is an order of magnitude above what a
/// linear parser needs, so a slow or busy CI machine does not make the tests flaky.
/// </remarks>
[TestClass, DoNotParallelize]
public class BitAiInlinePerformanceTests
{
    public static IEnumerable<object[]> PathologicalPatterns =>
    [
        ["*a"], ["_a "], ["**a"], ["*a **b "], ["**a*"], ["a*"], ["~~a "], ["a~~ "], ["[a"], ["![a"], ["[a]("], ["[a]( ("],
        ["[a](b \"x "], ["[a](b 'x "], ["[a](<b "], ["<a"], ["<a "], ["$a "], ["``a`"], ["*a [b "], ["_a [b](c) "],
    ];

    [TestMethod]
    [DynamicData(nameof(PathologicalPatterns))]
    public void FiftyThousandRepetitionsParseWellUnderASecond(string pattern)
    {
        var text = Repeat(pattern, 50_000);

        foreach (var complete in new[] { true, false })
        {
            var elapsed = BestOf(2, () => BitAiInlineParser.Parse(text, isComplete: complete));
            Assert.IsTrue(elapsed < TimeSpan.FromSeconds(1), $"'{pattern}' x 50,000 (complete: {complete}) took {elapsed.TotalMilliseconds:F0}ms.");
        }

        if (!pattern.Contains('<')) return;

        var options = new BitAiMarkdownOptions { AllowRawHtml = true };
        var html = BestOf(2, () => BitAiInlineParser.Parse(text, options));
        Assert.IsTrue(html < TimeSpan.FromSeconds(1), $"'{pattern}' x 50,000 with raw HTML took {html.TotalMilliseconds:F0}ms.");
    }

    [TestMethod]
    [DataRow("*a")]
    [DataRow("_a ")]
    [DataRow("**a")]
    [DataRow("*a **b ")]
    [DataRow("[a")]
    [DataRow("[a](")]
    [DataRow("<a")]
    public void ParsingTimeGrowsLinearlyWithTheInput(string pattern)
    {
        var small = Repeat(pattern, 6_250);
        var large = Repeat(pattern, 100_000);

        var smallTime = BestOf(3, () => BitAiInlineParser.Parse(small, isComplete: false), withoutCollections: true);
        var largeTime = BestOf(3, () => BitAiInlineParser.Parse(large, isComplete: false), withoutCollections: true);

        // Sixteen times the input costs sixteen times as much when linear, and in practice up to
        // twice that as the working set outgrows the caches; when quadratic it costs 256 times as
        // much. The limit sits between the two on a log scale, with a factor of two and a half of
        // slack on the linear side and three on the quadratic one, and the fixed allowance absorbs
        // timer noise on very short runs.
        var limit = smallTime * 80 + TimeSpan.FromMilliseconds(20);
        Assert.IsTrue(
            largeTime < limit,
            $"'{pattern}': {smallTime.TotalMilliseconds:F1}ms for 6,250 repetitions but {largeTime.TotalMilliseconds:F1}ms for 100,000.");
    }

    // With raw HTML on, an opening tag takes its whole element when the text closes it. Looked for from every opening tag
    // in turn, the closing tag would make a line of tags that never close quadratic; the tags are paired in one pass.
    [TestMethod]
    [DataRow("<b>a ")]
    [DataRow("<span x>")]
    [DataRow("<b><i>")]
    public void UnclosedTagsParseInLinearTimeWithRawHtml(string pattern)
    {
        var options = new BitAiMarkdownOptions { AllowRawHtml = true };
        var small = Repeat(pattern, 6_250) + "</b></span>";
        var large = Repeat(pattern, 100_000) + "</b></span>";

        var smallTime = BestOf(3, () => BitAiInlineParser.Parse(small, options), withoutCollections: true);
        var largeTime = BestOf(3, () => BitAiInlineParser.Parse(large, options), withoutCollections: true);

        // As in ParsingTimeGrowsLinearlyWithTheInput: sixteen times the input, sixteen times the cost when linear.
        var limit = smallTime * 80 + TimeSpan.FromMilliseconds(20);
        Assert.IsTrue(
            largeTime < limit,
            $"'{pattern}': {smallTime.TotalMilliseconds:F1}ms for 6,250 repetitions but {largeTime.TotalMilliseconds:F1}ms for 100,000.");
    }

    [TestMethod]
    public void DeeplyNestedEmphasisStaysShallowAndFast()
    {
        string[] sources =
        [
            new string('*', 50_000) + "a" + new string('*', 50_000),
            Repeat("*a ", 20_000) + "b" + Repeat(" c*", 20_000),
            Repeat("~~a ", 20_000) + "b" + Repeat(" c~~", 20_000),
        ];

        foreach (var source in sources)
        {
            List<BitAiMarkdownInline> inlines = [];
            var elapsed = BestOf(1, () => inlines = BitAiInlineParser.Parse(source));

            Assert.IsTrue(elapsed < TimeSpan.FromSeconds(1), $"Took {elapsed.TotalMilliseconds:F0}ms.");
            Assert.IsInRange(0, 33, Depth(inlines));
        }
    }

    private static string Repeat(string pattern, int count) => string.Concat(Enumerable.Repeat(pattern, count));

    // The first run also pays for compilation, and each run starts from a collected heap so that
    // one run's garbage is not charged to the next. Comparing growth, the runs can also be kept
    // free of collections: a parse keeps everything it allocates alive until it returns, so the
    // collector's cost depends on its own heuristics as much as on the input, and that noise can
    // swamp the difference between linear and quadratic.
    private static TimeSpan BestOf(int runs, Action action, bool withoutCollections = false)
    {
        var budget = 0L;
        if (withoutCollections)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            action();
            budget = (GC.GetAllocatedBytesForCurrentThread() - before) * 2 + (16 << 20);
        }

        var best = TimeSpan.MaxValue;
        for (var run = 0; run < runs; run++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            var noCollections = budget > 0 && TryStartNoGCRegion(budget);

            var stopwatch = Stopwatch.StartNew();
            action();
            stopwatch.Stop();

            if (noCollections && GCSettings.LatencyMode == GCLatencyMode.NoGCRegion) GC.EndNoGCRegion();
            if (stopwatch.Elapsed < best) best = stopwatch.Elapsed;
        }

        return best;
    }

    // Falls back to an ordinary run where the runtime cannot reserve that much.
    private static bool TryStartNoGCRegion(long budget)
    {
        try
        {
            return GC.TryStartNoGCRegion(budget);
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private static int Depth(IEnumerable<BitAiMarkdownInline> inlines) =>
        inlines.Select(i => i switch
        {
            BitAiEmphasisInline e => 1 + Depth(e.Inlines),
            BitAiStrikethroughInline s => 1 + Depth(s.Inlines),
            BitAiLinkInline l => 1 + Depth(l.Inlines),
            _ => 0,
        }).DefaultIfEmpty(0).Max();
}

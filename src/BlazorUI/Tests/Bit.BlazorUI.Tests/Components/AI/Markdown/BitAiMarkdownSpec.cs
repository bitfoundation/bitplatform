using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Markdown;

/// <summary>Shared helpers for the Markdown specification tests.</summary>
internal static class BitAiMarkdownSpec
{
    public static BitAiMarkdownDocument Parse(string source, BitAiMarkdownOptions? options = null, bool complete = true) =>
        BitAiMarkdownParser.Parse(source, options, complete);

    public static string Dump(string source, BitAiMarkdownOptions? options = null, bool complete = true) =>
        Dump(Parse(source, options, complete));

    /// <summary>The tree and the footnotes, so two parses can be compared exactly.</summary>
    public static string Dump(BitAiMarkdownDocument document)
    {
        var builder = new StringBuilder(BitAiMarkdownTreeDump.Of(document));
        foreach (var footnote in document.Footnotes)
        {
            var body = new BitAiMarkdownDocument();
            body.Blocks.AddRange(footnote.Blocks);
            builder.Append("|fn ").Append(footnote.Label).Append(':').Append(footnote.Number).Append('x').Append(footnote.ReferenceCount).Append(BitAiMarkdownTreeDump.Of(body));
        }

        return builder.ToString();
    }

    public static string InlineDump(string source, BitAiMarkdownOptions? options = null, bool complete = true)
    {
        var document = new BitAiMarkdownDocument();
        var paragraph = new BitAiParagraphBlock();
        paragraph.Inlines.AddRange(BitAiInlineParser.Parse(source, options, complete));
        document.Blocks.Add(paragraph);
        return BitAiMarkdownTreeDump.Of(document);
    }

    public static string Text(IEnumerable<BitAiMarkdownInline> inlines) =>
        string.Concat(inlines.Select(i => i switch
        {
            BitAiTextInline t => t.Text,
            BitAiEmphasisInline e => Text(e.Inlines),
            BitAiStrikethroughInline s => Text(s.Inlines),
            BitAiLinkInline l => Text(l.Inlines),
            BitAiCodeInline c => c.Code,
            BitAiLineBreakInline => "\n",
            BitAiFootnoteReferenceInline f => $"[^{f.Label}]",
            BitAiCitationMarkerInline c => $"[{c.Number}]",
            _ => string.Empty,
        }));

    public static string Text(BitAiMarkdownBlock block) => block switch
    {
        BitAiParagraphBlock p => Text(p.Inlines),
        BitAiHeadingBlock h => Text(h.Inlines),
        _ => string.Empty,
    };

    public static int BlockDepth(IEnumerable<BitAiMarkdownBlock> blocks) =>
        blocks.Select(b => b switch
        {
            BitAiQuoteBlock q => 1 + BlockDepth(q.Blocks),
            BitAiListBlock l => 1 + l.Items.Select(i => BlockDepth(i.Blocks)).DefaultIfEmpty(0).Max(),
            _ => 0,
        }).DefaultIfEmpty(0).Max();

    public static int InlineDepth(IEnumerable<BitAiMarkdownInline> inlines) =>
        inlines.Select(i => i switch
        {
            BitAiEmphasisInline e => 1 + InlineDepth(e.Inlines),
            BitAiStrikethroughInline s => 1 + InlineDepth(s.Inlines),
            BitAiLinkInline l => 1 + InlineDepth(l.Inlines),
            _ => 0,
        }).DefaultIfEmpty(0).Max();

    public static string Repeat(string pattern, int count) => string.Concat(Enumerable.Repeat(pattern, count));

    public static TimeSpan Time(Action action, int runs = 3)
    {
        var best = TimeSpan.MaxValue;
        for (var run = 0; run < runs; run++)
        {
            GC.Collect();
            var stopwatch = Stopwatch.StartNew();
            action();
            stopwatch.Stop();
            if (stopwatch.Elapsed < best) best = stopwatch.Elapsed;
        }

        return best;
    }

    /// <summary>Asserts the incremental parser agrees with a one-shot parse at every prefix and at the end.</summary>
    public static void AssertIncrementalMatches(string source, BitAiMarkdownOptions? options = null)
    {
        var parser = new BitAiIncrementalMarkdownParser(options);

        for (var length = 1; length <= source.Length; length++)
        {
            var prefix = source[..length];
            var expected = Dump(BitAiMarkdownParser.Parse(prefix, options, isComplete: false));
            var actual = Dump(parser.Parse(prefix, isComplete: false));
            Assert.IsTrue(expected == actual, $"Mismatch at prefix {length}: {prefix}\nexpected: {expected}\nactual:   {actual}");
        }

        Assert.AreEqual(Dump(BitAiMarkdownParser.Parse(source, options)), Dump(parser.Parse(source, isComplete: true)));
    }
}

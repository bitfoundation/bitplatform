using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Components.Extras.MarkdownViewer;

/// <summary>
/// Covers the Markdown pipeline itself - the part a caller uses without a component, and the
/// part an extension author builds against.
/// </summary>
[TestClass]
public class BitMarkdownPipelineTests
{
    [TestMethod]
    public void BitMarkdownParserShouldParseWithTheBasicPipelineByDefault()
    {
        var document = BitMarkdownParser.Parse("# hello");

        var heading = document.Children.Single() as BitMarkdownHeadingNode;

        Assert.IsNotNull(heading);
        Assert.AreEqual(1, heading.Level);
        Assert.AreEqual("hello", BitMarkdownInlineHelpers.PlainText(heading.Inlines));
    }

    [TestMethod]
    public void BitMarkdownParserShouldReturnAnEmptyDocumentForNullOrEmptySource()
    {
        Assert.AreEqual(0, BitMarkdownParser.Parse(null).Children.Count);
        Assert.AreEqual(0, BitMarkdownParser.Parse(string.Empty).Children.Count);
    }

    [TestMethod]
    public void BitMarkdownPipelineShouldSplitLinesOnEveryNewlineConvention()
    {
        var lf = BitMarkdownParser.Parse("a\n\nb");
        var crlf = BitMarkdownParser.Parse("a\r\n\r\nb");
        var cr = BitMarkdownParser.Parse("a\r\rb");

        Assert.AreEqual(2, lf.Children.Count);
        Assert.AreEqual(2, crlf.Children.Count);
        Assert.AreEqual(2, cr.Children.Count);
    }

    [TestMethod]
    public void BitMarkdownPipelineBuilderShouldApplyTheSameExtensionOnlyOnce()
    {
        var builder = new BitMarkdownPipelineBuilder()
            .UseStrikethrough()
            .UseStrikethrough();

        // A second registration of the same delimiter character would throw at Build time.
        var pipeline = builder.Build();

        Assert.IsNotNull(pipeline);
    }

    [TestMethod]
    public void BitMarkdownPipelineBuilderShouldRollBackAFailedExtensionSetup()
    {
        var builder = new BitMarkdownPipelineBuilder();
        int blockParsers = builder.BlockParsers.Count;
        int renderers = builder.Renderers.Count;

        Assert.ThrowsExactly<InvalidOperationException>(() => builder.Use(new ThrowingExtension()));

        Assert.AreEqual(blockParsers, builder.BlockParsers.Count);
        Assert.AreEqual(renderers, builder.Renderers.Count);

        // The builder is still usable, and the failed extension can be retried.
        Assert.ThrowsExactly<InvalidOperationException>(() => builder.Use(new ThrowingExtension()));
        Assert.IsNotNull(builder.UseStrikethrough().Build());
    }

    [TestMethod]
    public void BitMarkdownPipelineShouldRejectTwoProcessorsClaimingTheSameDelimiter()
    {
        var builder = new BitMarkdownPipelineBuilder();
        builder.DelimiterProcessors.Add(new BitMarkdownStrikethroughDelimiterProcessor());
        builder.DelimiterProcessors.Add(new BitMarkdownStrikethroughDelimiterProcessor());

        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => builder.Build());

        Assert.Contains("'~'", exception.Message);
    }

    [TestMethod]
    public void BitMarkdownRendererShouldFailLoudlyForAnUnrenderableNode()
    {
        var renderer = BitMarkdownPipelines.Basic.CreateRenderer();

        var exception = Assert.ThrowsExactly<InvalidOperationException>(
            () => renderer.WriteNode(null!, new BitMarkdownStrikethroughNode()));

        Assert.Contains(nameof(BitMarkdownStrikethroughNode), exception.Message);
    }

    [TestMethod]
    public void BitMarkdownHeadingNodeShouldRejectALevelOutsideOneToSix()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BitMarkdownHeadingNode { Level = 0 });
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new BitMarkdownHeadingNode { Level = 7 });
    }

    [TestMethod]
    public async Task BitMarkdownPipelineShouldBeSafeToShareAcrossConcurrentParses()
    {
        const string source = """
            # Shared

            A [reference][ref] and a note[^n] with ~~strikethrough~~ and :rocket:.

            | a | b |
            |---|---|
            | 1 | 2 |

            [ref]: /r
            [^n]: The note.
            """;

        var expected = Describe(BitMarkdownPipelines.Advanced.Parse(source));

        var results = await Task.WhenAll(Enumerable.Range(0, 32).Select(_ =>
            Task.Run(() => Describe(BitMarkdownPipelines.Advanced.Parse(source)))));

        foreach (var result in results)
        {
            Assert.AreEqual(expected, result);
        }
    }

    [TestMethod]
    public void BitMarkdownAstHelperShouldWalkDeeplyNestedTreesWithoutOverflowing()
    {
        // Deeper than the parser's own ceiling, to prove the traversal itself is iterative.
        var document = new BitMarkdownDocumentNode();
        document.Children.Add(Nest(50_000));

        int count = BitMarkdownAstHelper.Descendants(document).Count();

        Assert.AreEqual(50_000, count);

        int lists = 0;
        BitMarkdownAstHelper.VisitChildLists(document, _ => lists++);

        // The document's own children, each block quote's, and the innermost paragraph's inlines.
        Assert.AreEqual(50_001, lists);
    }

    [TestMethod]
    public void BitMarkdownAstHelperShouldEnumerateInDocumentOrder()
    {
        var document = BitMarkdownParser.Parse("# one\n\n## two\n\n### three");

        var headings = BitMarkdownAstHelper.Descendants(document)
                                           .OfType<BitMarkdownHeadingNode>()
                                           .Select(h => h.Level)
                                           .ToArray();

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, headings);
    }

    [TestMethod]
    public void BitMarkdownListNodeShouldExposeItsItemsAsALiveView()
    {
        var list = new BitMarkdownListNode();
        list.Items.Add(new BitMarkdownListItemNode());

        var view = list.ChildLists.Single();

        Assert.AreEqual(1, view.Count);

        view.Add(new BitMarkdownListItemNode());

        // The view writes through to the strongly typed collection.
        Assert.AreEqual(2, list.Items.Count);
        Assert.ThrowsExactly<ArgumentException>(() => view.Add(new BitMarkdownTextNode("x")));
    }

    [TestMethod]
    [DataRow("Foo   Bar", "foo bar")]
    [DataRow("  spaced  ", "spaced")]
    [DataRow("MiXeD", "mixed")]
    [DataRow("a\nb", "a b")]
    public void BitMarkdownLinkHelpersShouldNormalizeLabelsTheWayCommonMarkMatchesThem(string label, string expected)
    {
        Assert.AreEqual(expected, BitMarkdownLinkHelpers.NormalizeLabel(label));
    }

    [TestMethod]
    [DataRow("&amp;", "&")]
    [DataRow("&#65;", "A")]
    [DataRow("&#x41;", "A")]
    [DataRow("a &notreal; b", "a &notreal; b")]
    [DataRow("&#x1F680;", "\U0001F680")]
    [DataRow("&#99999999;", "&#99999999;")]
    public void BitMarkdownEntitiesShouldDecodeWhatItRecognizesAndLeaveTheRest(string input, string expected)
    {
        Assert.AreEqual(expected, BitMarkdownEntities.Decode(input));
    }

    [TestMethod]
    public void BitMarkdownPipelineShouldConsultLowerOrderedInlineParsersFirst()
    {
        // The footnote parser (Order 10) shares '[' with the link parser (Order 100) and has
        // to be offered the position first, whichever order they were registered in.
        var pipeline = new BitMarkdownPipelineBuilder().UseFootnotes().Build();

        var document = pipeline.Parse("a[^1]\n\n[^1]: note");

        Assert.AreEqual(1, BitMarkdownAstHelper.Descendants(document).OfType<BitMarkdownFootnoteReferenceNode>().Count());
        Assert.AreEqual(0, BitMarkdownAstHelper.Descendants(document).OfType<BitMarkdownLinkNode>().Count());
    }

    [TestMethod]
    public void BitMarkdownPipelineShouldCapNestingDepthOnPathologicalInput()
    {
        var document = BitMarkdownPipelines.Basic.Parse(new string('>', 5_000) + " deep");

        // Parsing completes instead of overflowing the stack; the surplus is plain text.
        Assert.IsGreaterThan(0, document.Children.Count);
    }

    [TestMethod]
    public void BitMarkdownPipelineShouldShareOneRendererAcrossRenders()
    {
        var pipeline = BitMarkdownPipelines.GitHub;

        // A renderer holds nothing but the pipeline's immutable renderer list, so the shared one
        // is what every render uses instead of allocating a new instance each time.
        Assert.AreSame(pipeline.Renderer, pipeline.Renderer);
        Assert.AreNotSame(pipeline.Renderer, pipeline.CreateRenderer());
    }

    [TestMethod]
    public void BitMarkdownPipelineShouldLetTheEmphasisExtrasAndStrikethroughShareTheTildeCharacter()
    {
        // Two processors claiming '~' would throw at Build time; the extras flavor replaces the
        // strikethrough one instead, and strikethrough steps aside when it is already claimed.
        Assert.IsNotNull(new BitMarkdownPipelineBuilder().UseStrikethrough().UseEmphasisExtras().Build());
        Assert.IsNotNull(new BitMarkdownPipelineBuilder().UseEmphasisExtras().UseStrikethrough().Build());
        Assert.IsNotNull(new BitMarkdownPipelineBuilder().UseGitHubFlavored().UseEmphasisExtras().Build());
        Assert.IsNotNull(new BitMarkdownPipelineBuilder().UseEmphasisExtras().UseGitHubFlavored().Build());
    }

    [TestMethod]
    public void BitMarkdownPipelineShouldKeepFrontMatterOutOfTheRenderedDocument()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseFrontMatter().Build();

        var document = pipeline.Parse("---\na: 1\n---\n\n# t");

        Assert.AreEqual(2, document.Children.Count);
        Assert.IsInstanceOfType<BitMarkdownFrontMatterNode>(document.Children[0]);
        Assert.IsInstanceOfType<BitMarkdownHeadingNode>(document.Children[1]);
    }

    [TestMethod]
    public void BitMarkdownSmartyPantsShouldEducateOnlyWhatItIsGiven()
    {
        Assert.AreEqual("“a”", BitMarkdownSmartyPantsAstProcessor.Educate("\"a\""));
        Assert.AreEqual("a–b", BitMarkdownSmartyPantsAstProcessor.Educate("a--b"));
        Assert.AreEqual("a—b", BitMarkdownSmartyPantsAstProcessor.Educate("a---b"));
        Assert.AreEqual("a…", BitMarkdownSmartyPantsAstProcessor.Educate("a..."));
        Assert.AreEqual("don’t", BitMarkdownSmartyPantsAstProcessor.Educate("don't"));
        // Nothing to educate is returned untouched, not rebuilt.
        Assert.AreEqual("plain text", BitMarkdownSmartyPantsAstProcessor.Educate("plain text"));
    }

    private static string Describe(BitMarkdownDocumentNode document)
        => string.Join("|", BitMarkdownAstHelper.Descendants(document).Select(n => n switch
        {
            BitMarkdownTextNode t => "t:" + t.Text,
            BitMarkdownHeadingNode h => "h" + h.Level,
            BitMarkdownLinkNode l => "a:" + l.Url,
            BitMarkdownFootnoteReferenceNode f => "fn:" + f.Number,
            _ => n.GetType().Name
        }));

    private static BitMarkdownNode Nest(int depth)
    {
        // Built bottom-up so the fixture itself cannot recurse.
        BitMarkdownNode node = new BitMarkdownParagraphNode();
        for (int i = 1; i < depth; i++)
        {
            var quote = new BitMarkdownBlockquoteNode();
            quote.Children.Add(node);
            node = quote;
        }
        return node;
    }

    private sealed class ThrowingExtension : IBitMarkdownExtension
    {
        public void Setup(BitMarkdownPipelineBuilder builder)
        {
            builder.BlockParsers.Add(new BitMarkdownThematicBreakParser());
            builder.Renderers.Add(new BitMarkdownStrikethroughRenderer());
            throw new InvalidOperationException("setup failed");
        }
    }
}

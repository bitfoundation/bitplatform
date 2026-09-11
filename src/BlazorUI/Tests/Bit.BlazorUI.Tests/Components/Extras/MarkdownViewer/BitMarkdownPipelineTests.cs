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

    [TestMethod]
    public void BitMarkdownTaskListShouldCountTheSameBoxesTheRendererDraws()
    {
        // A marker with no space after the bracket is not a task, and a scanner that thought it was
        // shifted every index after it: the reader ticked one box and another one changed.
        AssertBoxesAndCountAgree("- [ ]a\n- [ ] b\n");
        Assert.AreEqual("- [ ]a\n- [x] b\n", BitMarkdownTaskList.Toggle("- [ ]a\n- [ ] b\n", 0, true));

        // A quoted task is drawn, so it is counted and it can be ticked.
        AssertBoxesAndCountAgree("> - [ ] a\n\n- [ ] b\n");
        Assert.AreEqual("> - [x] a\n\n- [ ] b\n", BitMarkdownTaskList.Toggle("> - [ ] a\n\n- [ ] b\n", 0, true));
        Assert.AreEqual("> - [ ] a\n\n- [x] b\n", BitMarkdownTaskList.Toggle("> - [ ] a\n\n- [ ] b\n", 1, true));

        // A fence indented under a nested list item is still a fence, so the task inside it is
        // still not one.
        const string fenced = "- a\n  - b\n    ```\n    - [ ] x\n    ```\n- [ ] y\n";
        AssertBoxesAndCountAgree(fenced);
        Assert.AreEqual(1, BitMarkdownTaskList.Count(fenced));
        Assert.AreEqual("- a\n  - b\n    ```\n    - [ ] x\n    ```\n- [x] y\n",
            BitMarkdownTaskList.Toggle(fenced, 0, true));
    }

    [TestMethod]
    public void BitMarkdownTaskListShouldToggleTheMarkerACheckboxWasParsedFrom()
    {
        const string markdown = "> - [ ] quoted\n\n- [ ] top\n";
        var boxes = BitMarkdownAstHelper.Descendants(BitMarkdownPipelines.GitHub.Parse(markdown))
            .OfType<BitMarkdownTaskCheckboxNode>().ToList();

        Assert.AreEqual(2, boxes.Count);
        // Each box knows the line it came from, so nothing has to be searched for again.
        Assert.AreEqual(0, boxes[0].SourceLine);
        Assert.AreEqual(2, boxes[1].SourceLine);
        Assert.AreEqual("> - [x] quoted\n\n- [ ] top\n", BitMarkdownTaskList.Toggle(markdown, boxes[0], true));
        Assert.AreEqual("> - [ ] quoted\n\n- [x] top\n", BitMarkdownTaskList.Toggle(markdown, boxes[1], true));
    }

    [TestMethod]
    public void BitMarkdownPipelineShouldGiveEachBuilderTextsOfItsOwn()
    {
        var builder = new BitMarkdownPipelineBuilder();
        builder.Texts.AlertNote = "Hinweis";

        // Overriding one word in place must not translate every other pipeline in the process.
        Assert.AreEqual("Hinweis", builder.Build().Texts.AlertNote);
        Assert.AreEqual("Note", BitMarkdownTexts.Default.AlertNote);
        Assert.AreEqual("Note", new BitMarkdownPipelineBuilder().Build().Texts.AlertNote);
        Assert.AreEqual("Note", BitMarkdownPipelines.GitHub.Texts.AlertNote);
    }

    [TestMethod]
    public void BitMarkdownPipelineShouldSanitizeAReferenceDestinationForWhatUsesIt()
    {
        // An embedded raster image is a valid image source and never a valid link, so a definition
        // resolving as an image is sanitized as one - not twice, once as each.
        var image = BitMarkdownAstHelper
            .Descendants(BitMarkdownParser.Parse("![dot][d]\n\n[d]: data:image/png;base64,iVBORw0KGgo="))
            .OfType<BitMarkdownImageNode>().Single();
        Assert.AreEqual("data:image/png;base64,iVBORw0KGgo=", image.Url);

        // The schemes an image may not have are still refused.
        foreach (var destination in new[] { "javascript:alert(1)", "mailto:a@b.c", "data:text/html,<x>" })
        {
            var blocked = BitMarkdownAstHelper
                .Descendants(BitMarkdownParser.Parse($"![x][d]\n\n[d]: {destination}"))
                .OfType<BitMarkdownImageNode>().Single();
            Assert.AreEqual(string.Empty, blocked.Url, destination);
        }
    }

    [TestMethod]
    public void BitMarkdownPipelineShouldParseAParagraphOfDollarSignsInLinearTime()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseMathematics().Build();
        pipeline.Parse("warm $up ");

        // Every "$" used to rescan the remainder of the paragraph looking for a closing delimiter
        // that was never there, which made a page of prices quadratic in its own length.
        var watch = System.Diagnostics.Stopwatch.StartNew();
        pipeline.Parse(string.Concat(Enumerable.Repeat("a $b ", 16_000)));
        long elapsed = watch.ElapsedMilliseconds;

        Assert.IsTrue(elapsed < 500, $"80KB of spaced dollar signs took {elapsed}ms");

        // A closing delimiter that is there is still found, including past one that was rejected.
        Assert.AreEqual("x+1", BitMarkdownAstHelper.Descendants(pipeline.Parse("a $x+1$ b"))
            .OfType<BitMarkdownMathNode>().Single().Content);
        Assert.AreEqual("a $b", BitMarkdownAstHelper.Descendants(pipeline.Parse("$a $b$"))
            .OfType<BitMarkdownMathNode>().Single().Content);
    }

    [TestMethod]
    public void BitMarkdownMathBlockShouldNotBreakAParagraphItDoesNotOpen()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseMathematics().Build();

        // "$$5 for two" is prose, so the line above it stays part of the same paragraph.
        Assert.AreEqual(1, pipeline.Parse("The cost is\n$$5 for two\n").Children.Count);

        // A block that does open still interrupts the paragraph above it.
        var document = pipeline.Parse("text\n$$\nx=1\n$$\n");
        Assert.AreEqual(2, document.Children.Count);
        Assert.IsInstanceOfType<BitMarkdownMathNode>(document.Children[1]);
        // And so does a complete one-line block.
        Assert.AreEqual(2, pipeline.Parse("text\n$$ x=1 $$\n").Children.Count);
    }

    [TestMethod]
    public void BitMarkdownSmartyPantsShouldLeaveAnAutoLinkReadingAsItsDestination()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseAutoLinks().UseSmartyPants().Build();

        var link = BitMarkdownAstHelper.Descendants(pipeline.Parse("see https://a.com/x--y...z"))
            .OfType<BitMarkdownLinkNode>().Single();

        // The text of an autolink is its destination: educating one and not the other would leave
        // the reader copying a URL the link does not go to.
        Assert.AreEqual(link.Url, BitMarkdownInlineHelpers.PlainText(link.Children));

        // A label the author wrote is still educated.
        var written = BitMarkdownAstHelper
            .Descendants(new BitMarkdownPipelineBuilder().UseSmartyPants().Build().Parse("[a--b](/x)"))
            .OfType<BitMarkdownLinkNode>().Single();
        Assert.AreEqual("a–b", BitMarkdownInlineHelpers.PlainText(written.Children));
    }

    [TestMethod]
    public void BitMarkdownBaseUrlShouldLeaveARootRelativeDestinationAlone()
    {
        var relative = new BitMarkdownPipelineBuilder().UseBaseUrl("/docs/").Build();
        var absolute = new BitMarkdownPipelineBuilder().UseBaseUrl("https://b.com/r/").Build();

        // A root-relative destination is already resolved against the site root, which is what the
        // absolute base does with one too; prefixing it as well pointed at nothing.
        Assert.AreEqual("/img/logo.png", Destination(relative, "[a](/img/logo.png)"));
        Assert.AreEqual("https://b.com/img/logo.png", Destination(absolute, "[a](/img/logo.png)"));

        // A genuinely relative one is still resolved against the base.
        Assert.AreEqual("/docs/img/logo.png", Destination(relative, "[a](img/logo.png)"));
        Assert.AreEqual("/docs/img/logo.png", Destination(relative, "[a](./img/logo.png)"));
        Assert.AreEqual("https://b.com/r/img/logo.png", Destination(absolute, "[a](img/logo.png)"));
    }

    [TestMethod]
    public void BitMarkdownPipelineShouldReportAFlavorConfiguredTwiceInsteadOfDroppingOne()
    {
        // Silently keeping the first registration meant the options in the later call simply did
        // not apply, with nothing said about it.
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            new BitMarkdownPipelineBuilder().UseAutoIdentifiers().UseAutoIdentifiers(anchorLinks: true).Build());
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            new BitMarkdownPipelineBuilder().UseAdvanced()
                .UseEmojis(new Dictionary<string, string> { ["x"] = "y" }).Build());

        // Adding the same flavor with the same configuration asks for nothing new, and is a no-op.
        new BitMarkdownPipelineBuilder().UseGitHubFlavored().UsePipeTables().Build();
        new BitMarkdownPipelineBuilder().UseAutoIdentifiers().UseAutoIdentifiers().Build();
        new BitMarkdownPipelineBuilder().UseLinkOptions().UseLinkOptions().Build();
    }

    [TestMethod]
    public void BitMarkdownAdvancedShouldDeferToAFlavorAlreadyConfigured()
    {
        // The bundle asks for the flavor, not for particular options, so configuring one before it
        // keeps that configuration.
        var configured = new BitMarkdownPipelineBuilder().UseAutoIdentifiers(anchorLinks: true).UseAdvanced().Build();
        Assert.IsTrue(BitMarkdownAstHelper.Descendants(configured.Parse("# A"))
            .OfType<BitMarkdownHeadingAnchorNode>().Any());

        // With nothing configured, the bundle's own default still applies.
        var plain = new BitMarkdownPipelineBuilder().UseAdvanced().Build();
        Assert.IsFalse(BitMarkdownAstHelper.Descendants(plain.Parse("# A"))
            .OfType<BitMarkdownHeadingAnchorNode>().Any());
        Assert.AreEqual("a", BitMarkdownAstHelper.Descendants(plain.Parse("# A"))
            .OfType<BitMarkdownHeadingNode>().Single().Id);
    }

    [TestMethod]
    public void BitMarkdownUrlRewritesShouldComposeInTheOrderTheyWereAdded()
    {
        // A base URL and a rewriter of one's own is the pair this is most often written as, and one
        // of them being the only one that applied was not something the caller was told about.
        var pipeline = new BitMarkdownPipelineBuilder()
            .UseBaseUrl("https://b.com/r/")
            .UseUrlRewriter(context => context.Url.Replace("https://b.com/", "https://cdn.z.com/"))
            .Build();

        Assert.AreEqual("https://cdn.z.com/r/x.png", Destination(pipeline, "[a](x.png)"));
    }

    [TestMethod]
    public void BitMarkdownFootnoteDefinitionShouldInterruptTheParagraphAboveIt()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseFootnotes().Build();

        // Absorbed into the paragraph, the definition printed as prose and the citation - with its
        // label now undefined - degraded to literal text.
        var document = pipeline.Parse("a[^1]\n[^1]: note");

        Assert.IsTrue(BitMarkdownAstHelper.Descendants(document).OfType<BitMarkdownFootnoteReferenceNode>().Any());
        Assert.IsTrue(document.Children.OfType<BitMarkdownFootnotesNode>().Any());
        StringAssert.DoesNotMatch(BitMarkdownAstHelper.ToPlainText(document),
            new System.Text.RegularExpressions.Regex(@"\[\^1\]:"));
    }

    [TestMethod]
    public void BitMarkdownFootnoteReferenceShouldSurviveAColonAfterIt()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseFootnotes().Build();

        // "[^1]:" is the definition only at the start of a line. Read as one mid-sentence, the
        // citation stayed literal text and the note it pointed at was dropped as uncited.
        var document = pipeline.Parse("note[^1]: colon\n\n[^1]: def");

        Assert.IsTrue(BitMarkdownAstHelper.Descendants(document).OfType<BitMarkdownFootnoteReferenceNode>().Any());
        StringAssert.Contains(BitMarkdownAstHelper.ToPlainText(document), "def");
    }

    [TestMethod]
    public void BitMarkdownAbbreviationDefinitionShouldInterruptTheParagraphAboveIt()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseAbbreviations().Build();

        // The line right after a sentence is the natural place to explain a term just used.
        var document = pipeline.Parse("Uses HTML here\n*[HTML]: HyperText");

        Assert.AreEqual(1, BitMarkdownAstHelper.Descendants(document).OfType<BitMarkdownAbbreviationNode>().Count());
        StringAssert.DoesNotMatch(BitMarkdownAstHelper.ToPlainText(document),
            new System.Text.RegularExpressions.Regex(@"\*\[HTML\]"));
    }

    [TestMethod]
    public void BitMarkdownContainerShouldOnlyBeOpenedByAFenceCarryingAName()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseContainers().Build();

        // An info-less ":::" is a closer. Read as an opening fence, a stray one claimed every line
        // that followed it - the rest of a block quote, or the rest of the document.
        var stray = pipeline.Parse("text\n:::\nmore");
        Assert.IsFalse(BitMarkdownAstHelper.Descendants(stray).OfType<BitMarkdownContainerNode>().Any());
        StringAssert.Contains(BitMarkdownAstHelper.ToPlainText(stray), "more");

        // A named fence still opens one, and still closes on an info-less fence.
        var container = pipeline.Parse(":::note Heads up\nbody\n:::\nafter")
            .Children.OfType<BitMarkdownContainerNode>().Single();
        Assert.AreEqual("note", container.Name);
        Assert.AreEqual("Heads up", container.Title);
        StringAssert.Contains(BitMarkdownAstHelper.ToPlainText(container), "body");
    }

    [TestMethod]
    public void BitMarkdownContainerShouldNotBeClosedByAFenceInsideACodeBlock()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseContainers().Build();

        // A ":::" written inside a code block is sample text, not this container's closer.
        var document = pipeline.Parse(":::note\n```\n:::\n```\n:::");

        Assert.AreEqual(1, document.Children.Count);
        var container = document.Children.OfType<BitMarkdownContainerNode>().Single();
        Assert.AreEqual(":::", container.Children.OfType<BitMarkdownCodeBlockNode>().Single().Content);
    }

    [TestMethod]
    public void BitMarkdownAutoIdentifiersShouldNotLetAnExplicitIdCollide()
    {
        var pipeline = new BitMarkdownPipelineBuilder().UseAutoIdentifiers().Build();

        // Two headings carrying one id is invalid markup, and every link to it - the permalinks
        // this flavor adds included - landed on whichever the browser found first.
        var collided = BitMarkdownAstHelper.Descendants(pipeline.Parse("# A\n\n# B {#a}"))
            .OfType<BitMarkdownHeadingNode>().Select(h => h.Id).ToList();
        CollectionAssert.AllItemsAreUnique(collided);
        Assert.AreEqual("a", collided[0]);

        // An id nothing else took is kept exactly, which is the whole point of naming one.
        var named = BitMarkdownAstHelper.Descendants(pipeline.Parse("# A\n\n# B {#stable}"))
            .OfType<BitMarkdownHeadingNode>().Select(h => h.Id).ToList();
        CollectionAssert.AreEqual(new[] { "a", "stable" }, named);
    }

    private static void AssertBoxesAndCountAgree(string markdown)
    {
        int drawn = BitMarkdownAstHelper.Descendants(BitMarkdownPipelines.Advanced.Parse(markdown))
            .OfType<BitMarkdownTaskCheckboxNode>().Count();

        Assert.AreEqual(drawn, BitMarkdownTaskList.Count(markdown), markdown);
    }

    private static string Destination(BitMarkdownPipeline pipeline, string markdown)
        => BitMarkdownAstHelper.Descendants(pipeline.Parse(markdown))
            .OfType<BitMarkdownLinkNode>().Single().Url;

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

using System.Text.RegularExpressions;

namespace Bit.Bmotion.Tests.Mcp.Services;

/// <summary>
/// The unified search - the tool the other tools' descriptions send an agent to first.
/// <para>
/// Its contract is not "returns something relevant"; it is that every hit carries the exact
/// follow-up call that returns the hit in full. A search that names a section, a type or a recipe
/// the fetching tools cannot resolve leaves an agent in a loop it has no way out of, and that is
/// the failure this file is mostly about. The relevance tests below are deliberately about
/// questions phrased as symptoms rather than as names, because those are the ones a keyword index
/// gets wrong.
/// </para>
/// </summary>
[TestClass]
public class SearchIndexTests
{
    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow(null)]
    public async Task Search_NothingToSearchFor_IsNoHits(string? query)
    {
        Assert.AreEqual(0, (await BmotionSearchIndex.SearchAsync(query!, 12)).Length);
    }

    /// <summary>
    /// Every entry in this index is about animating something in Bmotion, so a query saying so
    /// matches everything and ranks nothing. Those words are dropped, and a query made only of them
    /// is no query at all.
    /// </summary>
    [TestMethod]
    [DataRow("how does this work")]
    [DataRow("animation")]
    [DataRow("the and for with")]
    [DataRow("a an in of")]
    public async Task Search_AQueryMadeOnlyOfNoiseWords_ReturnsNothingRatherThanEverything(string query)
    {
        Assert.AreEqual(0, (await BmotionSearchIndex.SearchAsync(query, 12)).Length,
                        $"'{query}' matched something, which means it would match nearly everything.");
    }

    [TestMethod]
    [DataRow("stagger a list")]
    [DataRow("animate something out before it is removed")]
    [DataRow("drag within bounds")]
    [DataRow("spring stiffness damping")]
    [DataRow("scroll reveal")]
    [DataRow("shared element transition")]
    [DataRow("reduced motion accessibility")]
    [DataRow("keyframes")]
    public async Task Search_TheQuestionsThisServerExistsFor_FindSomething(string query)
    {
        var hits = await BmotionSearchIndex.SearchAsync(query, 12);

        Assert.AreNotEqual(0, hits.Length, $"'{query}' found nothing.");
    }

    /// <summary>
    /// The property that makes one search enough: an agent can act on any hit without guessing at
    /// what to call next. A Tool string naming a call that does not resolve is the worst outcome
    /// this index has, because it looks like an answer.
    /// </summary>
    [TestMethod]
    public async Task Search_EveryHit_CarriesAFollowUpCallThatActuallyResolves()
    {
        var queries = new[]
        {
            "stagger", "exit", "drag", "spring", "scroll", "layout", "variants", "opacity",
            "colour", "keyframes", "presence", "easing", "server", "gesture", "split text",
        };

        var toolNames = new McpController().GetMcpCatalog().Tools.Select(tool => tool.Name).ToHashSet(StringComparer.Ordinal);

        var checkedCalls = 0;

        foreach (var query in queries)
        {
            foreach (var hit in await BmotionSearchIndex.SearchAsync(query, 20))
            {
                var call = Regex.Match(hit.Tool, @"^(?<tool>\w+)\((?<args>.*)\)$");

                Assert.IsTrue(call.Success, $"'{hit.Title}' offers '{hit.Tool}', which is not a tool call.");

                var tool = call.Groups["tool"].Value;

                CollectionAssert.Contains(toolNames.ToArray(), tool,
                                          $"'{hit.Title}' points at '{tool}', which this server does not expose.");

                // The single-argument fetches are the ones that can name something that does not
                // exist; the multi-argument tools take free text and are checked by name only.
                var value = Regex.Match(call.Groups["args"].Value, @"^\w+:\s*""(?<value>[^""]*)""$").Groups["value"].Value;

                // And the argument it names has to be one the tool answers for.
                switch (tool)
                {
                    case "GetBmotionGuideSection":
                        Assert.IsNotNull(BmotionSourceCatalog.GetGuideSection(value), $"No guide section '{value}'.");
                        break;

                    case "GetBmotionSourceFile":
                        Assert.IsNotNull(BmotionSourceCatalog.GetSourceFile(value), $"No source file '{value}'.");
                        break;

                    case "GetBmotionRecipe":
                        Assert.IsNotNull(BmotionRecipeCatalog.Find(value), $"No recipe '{value}'.");
                        break;

                    case "GetBmotionApiDetails":
                        Assert.IsNotNull(BmotionApiCatalog.GetTypeDetails(value), $"No public type '{value}'.");
                        break;

                    case "GetBmotionSetupGuide":
                        Assert.IsNotNull(BmotionSetupGuide.Get(value), $"No setup guide for '{value}'.");
                        break;
                }

                checkedCalls++;
            }
        }

        Assert.IsTrue(checkedCalls > 50, $"Only {checkedCalls} hits were checked, which is too few to mean anything.");
    }

    [TestMethod]
    public async Task Search_EveryHit_IsLabelledAndCarriesTheTextThatMatched()
    {
        foreach (var hit in await BmotionSearchIndex.SearchAsync("spring bounce overshoot", 20))
        {
            Assert.AreNotEqual(string.Empty, hit.Kind.Trim());
            Assert.AreNotEqual(string.Empty, hit.Title.Trim());
            Assert.AreNotEqual(string.Empty, hit.Tool.Trim());

            // A snippet is a window on the body, not the body: the point is to spend a client's
            // context on the shortlist rather than on the answer it has not chosen yet.
            Assert.IsTrue(hit.Snippet.Length <= 250, $"'{hit.Title}' returned a {hit.Snippet.Length}-character snippet.");
        }
    }

    /// <summary>
    /// A name is what someone asking for it means; prose that merely mentions it is not. The match
    /// is on the member's own name rather than on the whole title, because a member inherited by
    /// four transition types is the same member however it is qualified.
    /// </summary>
    [TestMethod]
    [DataRow("BmSpring", "BmSpring")]
    [DataRow("BmotionAnimatePresence", "BmotionAnimatePresence")]
    [DataRow("staggerChildren", "StaggerChildren")]
    [DataRow("LayoutId", "LayoutId")]
    public async Task Search_ANameQuery_PutsTheThingItselfFirst(string query, string expected)
    {
        var hits = await BmotionSearchIndex.SearchAsync(query, 5);

        Assert.AreNotEqual(0, hits.Length, $"'{query}' found nothing.");
        Assert.IsTrue(hits[0].Title.EndsWith(expected, StringComparison.Ordinal),
                      $"'{query}' ranked '{hits[0].Title}' first. The rest: " +
                      $"{string.Join(", ", hits.Skip(1).Select(hit => hit.Title))}.");
    }

    /// <summary>
    /// The plural in a heading is an accident of writing, not of meaning: nobody phrases a question
    /// in the number the guide happens to use.
    /// </summary>
    [TestMethod]
    public async Task Search_FindsASectionWhoseHeadingIsThePluralOfTheQuery()
    {
        var hits = await BmotionSearchIndex.SearchAsync("variant", 10);

        Assert.IsTrue(hits.Any(hit => hit.Title.Equals("Variants", StringComparison.OrdinalIgnoreCase)),
                      $"Found: {string.Join(", ", hits.Select(hit => hit.Title))}.");
    }

    /// <summary>
    /// A question phrased as a symptom names no section, type or recipe. Without the two entries for
    /// the engine-running tools it would match whatever else happens to contain the word "server" -
    /// and this is the single most common thing to go wrong with a Bmotion app.
    /// </summary>
    [TestMethod]
    public async Task Search_ASymptomWithNoNameInIt_StillFindsTheToolThatDiagnosesIt()
    {
        var hits = await BmotionSearchIndex.SearchAsync("the animation snaps instantly in production on the server", 12);

        Assert.IsTrue(hits.Any(hit => hit.Tool.StartsWith("AnalyzeBmotionAnimation", StringComparison.Ordinal)),
                      $"Found: {string.Join(", ", hits.Select(hit => $"{hit.Title} -> {hit.Tool}"))}.");
    }

    [TestMethod]
    public async Task Search_AskingHowLongASpringTakes_FindsTheToolThatMeasuresIt()
    {
        var hits = await BmotionSearchIndex.SearchAsync("my spring feels too slow to settle", 12);

        Assert.IsTrue(hits.Any(hit => hit.Tool.StartsWith("SimulateBmotionTransition", StringComparison.Ordinal)),
                      $"Found: {string.Join(", ", hits.Select(hit => $"{hit.Title} -> {hit.Tool}"))}.");
    }

    [TestMethod]
    public async Task Search_HonoursTheLimit_AndClampsItToSomethingAClientCanRead()
    {
        Assert.AreEqual(1, (await BmotionSearchIndex.SearchAsync("spring", 1)).Length);
        Assert.AreEqual(5, (await BmotionSearchIndex.SearchAsync("spring", 5)).Length);

        // Below one is still one hit, not none and not an exception.
        Assert.AreEqual(1, (await BmotionSearchIndex.SearchAsync("spring", 0)).Length);
        Assert.AreEqual(1, (await BmotionSearchIndex.SearchAsync("spring", -10)).Length);

        Assert.IsTrue((await BmotionSearchIndex.SearchAsync("spring", 5_000)).Length <= 50);
    }

    [TestMethod]
    public async Task Search_TheSameQueryTwice_RanksTheSameWay()
    {
        var first = await BmotionSearchIndex.SearchAsync("layout shared element", 12);
        var second = await BmotionSearchIndex.SearchAsync("layout shared element", 12);

        CollectionAssert.AreEqual(first.Select(hit => hit.Title).ToArray(), second.Select(hit => hit.Title).ToArray());
    }

    /// <summary>
    /// Every term is counted against every entry, so the work is terms x corpus. A pasted file as a
    /// query has to be bounded rather than scanned in full.
    /// </summary>
    [TestMethod]
    public async Task Search_APastedFileAsAQuery_IsBoundedRatherThanScannedInFull()
    {
        var pasted = string.Join(' ', Enumerable.Range(0, 4_000).Select(i => $"token{i}"));

        var started = System.Diagnostics.Stopwatch.StartNew();

        await BmotionSearchIndex.SearchAsync(pasted, 12);

        Assert.IsTrue(started.Elapsed < TimeSpan.FromSeconds(5), $"It took {started.Elapsed.TotalSeconds:0.0}s.");
    }

    [TestMethod]
    public async Task Search_APunctuatedQuery_IsSplitTheSameWayAsAPlainOne()
    {
        var plain = await BmotionSearchIndex.SearchAsync("exit animation presence", 8);
        var punctuated = await BmotionSearchIndex.SearchAsync("exit-animation (presence)?", 8);

        CollectionAssert.AreEqual(plain.Select(hit => hit.Title).ToArray(), punctuated.Select(hit => hit.Title).ToArray());
    }

    /// <summary>
    /// The index is built once, lazily, from six catalogs that each run the engine or walk the
    /// assembly. Several searches arriving at once must not each build their own.
    /// </summary>
    [TestMethod]
    public async Task Search_IsSafeToCallConcurrentlyWhileTheIndexIsStillBeingBuilt()
    {
        var results = await Task.WhenAll(Enumerable.Range(0, 12)
            .Select(_ => BmotionSearchIndex.SearchAsync("spring damping", 6)));

        foreach (var result in results)
        {
            CollectionAssert.AreEqual(results[0].Select(hit => hit.Title).ToArray(),
                                      result.Select(hit => hit.Title).ToArray());
        }
    }

    [TestMethod]
    public async Task Search_CoversEveryCorpusTheDescriptionPromises()
    {
        var kinds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var query in new[]
        {
            "variants", "spring", "stagger", "opacity", "BackOut", "drag", "springs page",
            "server render mode", "settle time", "Program.cs",
        })
        {
            foreach (var hit in await BmotionSearchIndex.SearchAsync(query, 30))
            {
                kinds.Add(hit.Kind);
            }
        }

        foreach (var promised in new[]
        {
            "Guide section", "Recipe", "Animatable property", "Easing preset", "Demo page", "Source file", "Setup guide", "Tool",
        })
        {
            CollectionAssert.Contains(kinds.ToArray(), promised, $"Nothing of kind '{promised}' was ever found.");
        }

        // The API entries are labelled by what they are - "API component", "API parameter", ...
        Assert.IsTrue(kinds.Any(kind => kind.StartsWith("API ", StringComparison.OrdinalIgnoreCase)),
                      $"The public API was never reached. Kinds seen: {string.Join(", ", kinds)}.");
    }
}

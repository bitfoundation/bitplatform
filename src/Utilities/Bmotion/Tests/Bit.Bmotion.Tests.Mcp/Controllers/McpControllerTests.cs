using Bit.Bmotion.Demo.Client;

namespace Bit.Bmotion.Tests.Mcp.Controllers;

/// <summary>
/// The tool methods themselves.
/// <para>
/// The theme here is what happens when an agent asks for something that is not there. Over MCP a
/// thrown exception is reduced to "an error occurred invoking X", which teaches the caller nothing
/// and costs it a turn; every miss in this controller therefore has to come back as text that names
/// what does exist. These tests hold each of them to that, and check the answers a caller acts on
/// without reading - the overview an agent starts from, and the catalog the demo page renders.
/// </para>
/// </summary>
[TestClass]
public class McpControllerTests
{
    private readonly McpController _controller = new();

    [TestMethod]
    public void GetBmotionOverview_IsTheBriefingAnAgentCanStartFrom()
    {
        var overview = _controller.GetBmotionOverview();

        Assert.IsGreaterThan(2_000, overview.Length, $"The overview is only {overview.Length} characters.");

        // The three things it promises: what the library is, how to wire it, and how to work.
        Assert.Contains("## Installation", overview);
        Assert.Contains("## Quick Start", overview);
        Assert.Contains("## How to work", overview);
        Assert.Contains("## Rules of thumb when writing Bmotion code", overview);

        // The rule that decides whether an animation works at all in production: the browser plays it,
        // as a timeline or a recording, unless only the frame loop can.
        Assert.Contains("Blazor Server", overview);
        Assert.Contains("compositor", overview);
        Assert.Contains("*timeline*", overview);
        Assert.Contains("*records*", overview);
        Assert.Contains("Only the C# frame loop can play", overview);

        // A section the guide no longer has must be reported, not left as a silent gap.
        Assert.IsFalse(overview.Contains("was not found in this build", StringComparison.Ordinal),
                       "The overview is quoting a guide section that no longer exists.");
    }

    /// <summary>
    /// Which build the answers come from. An agent holding a remembered version of the library
    /// otherwise has no way to tell that this server is answering about a different one.
    /// </summary>
    [TestMethod]
    public void GetBmotionOverview_NamesTheVersionItIsAnsweringFor()
    {
        var overview = _controller.GetBmotionOverview();

        Assert.Contains("These tools answer from Bit.Bmotion", overview);
        Assert.IsFalse(overview.Contains("Bit.Bmotion unknown", StringComparison.Ordinal),
                       "The assembly version could not be read.");
    }

    [TestMethod]
    public void GetBmotionSetupGuide_AnUnknownRenderMode_NamesTheOnesThatExist()
    {
        var answer = _controller.GetBmotionSetupGuide("maui");

        foreach (var mode in BmotionSetupGuide.RenderModes)
        {
            Assert.Contains(mode, answer, $"The answer does not offer '{mode}'.");
        }
    }

    [TestMethod]
    public void GetBmotionGuideSection_AnUnknownHeading_NamesTheOnesThatExist()
    {
        var answer = _controller.GetBmotionGuideSection("Teleportation");

        Assert.Contains("no section called 'Teleportation'", answer);

        foreach (var section in BmotionSourceCatalog.GuideSections.Take(3))
        {
            Assert.Contains(section.Heading, answer);
        }
    }

    [TestMethod]
    public void GetBmotionRecipe_AKnownId_ReturnsTheRecipeWithItsCode()
    {
        var recipe = _controller.GetBmotionRecipe("staggered-list").Recipe;

        Assert.IsNotNull(recipe, "A known id did not come back as a recipe.");
        Assert.AreEqual("staggered-list", recipe.Id);
        Assert.IsFalse(string.IsNullOrWhiteSpace(recipe.Code));
        Assert.IsFalse(string.IsNullOrWhiteSpace(recipe.Notes));
    }

    [TestMethod]
    public void GetBmotionRecipe_AnUnknownId_NamesEveryIdThatExists()
    {
        var answer = _controller.GetBmotionRecipe("teleport").Message;

        Assert.IsNotNull(answer, "An unknown id did not come back as an explanation.");

        foreach (var recipe in BmotionRecipeCatalog.All)
        {
            Assert.Contains(recipe.Id, answer);
        }

        Assert.Contains(nameof(McpController.SearchBmotion), answer);
    }

    [TestMethod]
    public void GetBmotionApiDetails_AKnownType_ReturnsDetailsAndNoMessage()
    {
        var result = _controller.GetBmotionApiDetails("BmSpring");

        Assert.IsNotNull(result.Details);
        Assert.IsNull(result.Message);
        Assert.AreEqual("BmSpring", result.Details.Name);
    }

    /// <summary>A near miss is the common case, and the near misses are the useful part of the answer.</summary>
    [TestMethod]
    public void GetBmotionApiDetails_AnAlmostRightName_SuggestsTheTypesItCouldMean()
    {
        var result = _controller.GetBmotionApiDetails("Spring");

        Assert.IsNull(result.Details);
        Assert.IsNotNull(result.Message);
        Assert.Contains("BmSpring", result.Message);
    }

    [TestMethod]
    public void GetBmotionApiDetails_ANameLikeNothingAtAll_PointsAtTheListing()
    {
        var result = _controller.GetBmotionApiDetails("Teleporter");

        Assert.IsNull(result.Details);
        Assert.Contains(nameof(McpController.GetBmotionApiList), result.Message!);
    }

    [TestMethod]
    public void GetBmotionSourceFile_AKnownPath_ReturnsItVerbatim()
    {
        var content = _controller.GetBmotionSourceFile("Demo/Server/Program.cs");

        Assert.Contains("MapMcp", content);
        Assert.AreEqual(BmotionSourceCatalog.GetSourceFile("Demo/Server/Program.cs"), content);
    }

    [TestMethod]
    public void GetBmotionSourceFile_AnAlmostRightPath_SuggestsTheFilesItCouldMean()
    {
        var answer = _controller.GetBmotionSourceFile("Springs.razor");

        Assert.Contains("Did you mean", answer);
        Assert.Contains("Springs.razor", answer);
    }

    [TestMethod]
    public void GetBmotionSourceFile_APathLikeNothingAtAll_PointsAtTheListing()
    {
        Assert.Contains(nameof(McpController.GetBmotionSourceFiles),
_controller.GetBmotionSourceFile("nowhere/at/all.txt"));
    }

    /// <summary>
    /// A couple of tool calls must not be able to crowd out a client's context window, so the long
    /// documents are cut - and say that they were, rather than ending mid-sentence.
    /// </summary>
    [TestMethod]
    public void GetBmotionSourceFile_AVeryLongFile_IsCutAndSaysSo()
    {
        var longest = BmotionSourceCatalog.SourceFiles
            .OrderByDescending(file => BmotionSourceCatalog.GetSourceFile(file.Path)!.Length)
            .First();

        var content = BmotionSourceCatalog.GetSourceFile(longest.Path)!;
        var answer = _controller.GetBmotionSourceFile(longest.Path);

        if (content.Length <= McpController.MaxDocumentLength)
        {
            Assert.AreEqual(content, answer, "A file inside the limit was altered.");

            return;
        }

        Assert.Contains("[truncated", answer);
        Assert.IsLessThan(content.Length, answer.Length);

        // A cut that says only that it happened leaves the caller to guess how to reach the rest.
        Assert.Contains(nameof(McpController.GetBmotionSourceFile), answer);
        Assert.Contains("fromLine:", answer);
    }

    [TestMethod]
    public async Task SimulateBmotionTransition_MeasuresEachCandidateSeparately()
    {
        var results = await _controller.SimulateBmotionTransition(
            "spring(stiffness: 260, damping: 12); spring(stiffness: 260, damping: 30); tween(0.3, BackOut)");

        Assert.HasCount(3, results);
        Assert.IsTrue(results.All(result => result.Error is null));

        // The comparison is only useful if the numbers actually differ between candidates.
        Assert.HasCount(3, results.Select(result => result.Transition).Distinct());
        Assert.IsGreaterThan(results[1].OvershootPercent, results[0].OvershootPercent,
                      "The lighter-damped spring did not measure as bouncier.");
    }

    [TestMethod]
    public async Task SimulateBmotionTransition_AcceptsNewlinesAsWellAsSemicolons()
    {
        var results = await _controller.SimulateBmotionTransition("spring(stiffness: 200, damping: 20)\ntween(0.4)\r\ninertia(velocity: 300)");

        Assert.HasCount(3, results);
        Assert.AreSequenceEqual(new[] { "Spring", "Tween", "Inertia" }, results.Select(result => result.Kind).ToArray());
    }

    /// <summary>
    /// Naming every unmeasured spec is an answer per spec, so a long enough list would make a response as long. Past
    /// <see cref="McpController.MaxListedTransitions"/> the rest of the list is one answer that does not echo it.
    /// </summary>
    [TestMethod]
    public async Task SimulateBmotionTransition_AListPastTheListedCap_EndsInOneAnswerForTheRest()
    {
        // Every spec distinct, so an entry answered for the wrong spec, or out of order, shows.
        var specs = Enumerable.Range(0, 100_000).Select(i => $"tween(0.1{i:D6})").ToArray();

        var results = await _controller.SimulateBmotionTransition(string.Join(";", specs));

        Assert.HasCount(McpController.MaxListedTransitions + 1, results);
        Assert.AreEqual(McpController.MaxSimulatedTransitions, results.Count(result => result.Error is null));
        Assert.AreSequenceEqual(specs[McpController.MaxSimulatedTransitions..McpController.MaxListedTransitions], results[McpController.MaxSimulatedTransitions..^1].Select(result => result.Transition).ToArray());

        var rest = results[^1];
        Assert.AreEqual(string.Empty, rest.Transition, "the rest of the list is not echoed back");
        Assert.Contains($"at most {McpController.MaxListedTransitions}", rest.Error!);
        Assert.AreEqual(rest.Error, rest.Reading);
    }

    [TestMethod]
    public async Task SimulateBmotionTransition_ExactlyTheListedCap_IsAllNamed()
    {
        var atCap = string.Join(";\n", Enumerable.Repeat("tween(0.1)", McpController.MaxListedTransitions)) + ";";

        var results = await _controller.SimulateBmotionTransition(atCap);

        // The empty entries the separators leave are not specs, so the list is exactly at the cap: every spec is
        // answered for by name, and nothing is left over to report as unread.
        Assert.HasCount(McpController.MaxListedTransitions, results);
        Assert.IsTrue(results.Skip(McpController.MaxSimulatedTransitions).All(result => result.Transition == "tween(0.1)"));
        var actualValue = results.Any(result => result.Error?.StartsWith("Not read") ?? false);
        Assert.IsFalse(actualValue);
    }

    /// <summary>
    /// Simulating dozens at once would spend more of a client's context than any comparison is read
    /// with, and the interesting comparisons are between two and four candidates. What is over the
    /// cap is still answered: fewer results than transitions asked about is a silent cut, and the
    /// caller cannot tell which candidates were never run - or that any were left out at all.
    /// </summary>
    [TestMethod]
    public async Task SimulateBmotionTransition_IsCappedRatherThanRunningWhateverArrives()
    {
        var many = string.Join("; ", Enumerable.Range(1, 40).Select(i => $"tween({i / 100.0})"));

        var results = await _controller.SimulateBmotionTransition(many);

        Assert.HasCount(40, results);
        Assert.AreEqual(McpController.MaxSimulatedTransitions, results.Count(result => result.Error is null));

        // The measured ones come first, in the order asked, and every one after them says why it has
        // no numbers - in Error and in Reading both, as an unreadable spec does.
        Assert.IsTrue(results.Take(McpController.MaxSimulatedTransitions).All(result => result.Error is null));

        foreach (var unmeasured in results.Skip(McpController.MaxSimulatedTransitions))
        {
            Assert.Contains("Not measured", unmeasured.Error!);
            Assert.Contains(McpController.MaxSimulatedTransitions.ToString(), unmeasured.Error);
            Assert.AreEqual(unmeasured.Error, unmeasured.Reading);
        }

        // The unmeasured ones are the specs asked about, not blanks: the caller reads them to know
        // which candidates to send in the next call.
        Assert.AreEqual($"tween({9 / 100.0})", results[8].Transition);
    }

    /// <summary>
    /// A blank spec is a question the lab has an answer for - the library's own default tween - and
    /// answering with nothing at all would read as a server that failed rather than a spec that was
    /// empty. This is also what the demo page's lab sends before anyone types into it.
    /// </summary>
    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow(";;;")]
    [DataRow(null)]
    public async Task SimulateBmotionTransition_NothingToSimulate_MeasuresTheDefaultTween(string? transitions)
    {
        var results = await _controller.SimulateBmotionTransition(transitions!);

        Assert.ContainsSingle(results);
        Assert.IsNull(results[0].Error);
        Assert.AreEqual("Tween", results[0].Kind);
    }

    [TestMethod]
    public async Task SimulateBmotionTransition_OneUnreadableCandidate_DoesNotCostTheOthers()
    {
        var results = await _controller.SimulateBmotionTransition("spring(stiffness: 200, damping: 20); swoosh(1); tween(0.4)");

        Assert.HasCount(3, results);
        Assert.IsNull(results[0].Error);
        Assert.IsNotNull(results[1].Error);
        Assert.IsNull(results[2].Error);
    }

    /// <summary>
    /// The samples are the largest part of a simulation and the part a caller reading settle time
    /// and overshoot never looks at, so they are asked for rather than sent. Everything that
    /// describes the motion in words - the sparkline, the reading - is there either way.
    /// </summary>
    [TestMethod]
    public async Task SimulateBmotionTransition_SamplesAreOptIn()
    {
        var lean = (await _controller.SimulateBmotionTransition("spring(stiffness: 260, damping: 12)")).Single();
        var full = (await _controller.SimulateBmotionTransition("spring(stiffness: 260, damping: 12)", includeSamples: true)).Single();

        Assert.IsEmpty(lean.Samples);
        Assert.IsGreaterThan(1, full.Samples.Length);

        Assert.AreEqual(full.SettleSeconds, lean.SettleSeconds);
        Assert.AreEqual(full.Sparkline, lean.Sparkline);
        Assert.AreEqual(full.Reading, lean.Reading);
    }

    [TestMethod]
    public async Task AnalyzeBmotionAnimation_ReadsThePropertyListHoweverItIsWritten()
    {
        foreach (var written in new[] { "x, opacity", "x;opacity", "x opacity", " x ,  opacity " })
        {
            var result = await _controller.AnalyzeBmotionAnimation(written, "tween(0.4)");

            Assert.AreSequenceEqual(new[] { "x", "opacity" }, result.Properties, $"'{written}' was read as something else.");
            Assert.IsTrue(result.WorksOnBlazorServer);
        }
    }

    /// <summary>
    /// The refusal is an answer, not an exception: an MCP client is told only that a throwing tool failed, so the
    /// limit - and how to stay under it - has to arrive as data to reach the agent at all.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(10_000)]
    public async Task AnalyzeBmotionAnimation_MoreThanTheCapOfProperties_IsRefusedAsAnAnswer(int over)
    {
        var many = string.Join(", ", Enumerable.Range(0, McpController.MaxAnalyzedProperties + over).Select(i => $"unknown{i}"));

        var result = await _controller.AnalyzeBmotionAnimation(many, "tween(0.4)");

        Assert.AreEqual("Not analysed", result.Path);
        Assert.IsFalse(result.WorksOnBlazorServer);
        Assert.IsEmpty(result.Properties, "a refused list is not echoed back");
        Assert.Contains($"at most {McpController.MaxAnalyzedProperties}", result.Error!);
    }

    [TestMethod]
    [DataRow(", ")]
    [DataRow(" ,, ;; ")]
    public async Task AnalyzeBmotionAnimation_ExactlyTheCapOfProperties_IsAnswered(string separator)
    {
        // Empty entries and a trailing separator are not names, so they do not count toward the limit.
        var atCap = string.Join(separator, Enumerable.Range(0, McpController.MaxAnalyzedProperties).Select(i => $"unknown{i}")) + separator;

        var result = await _controller.AnalyzeBmotionAnimation(atCap, "tween(0.4)");

        Assert.HasCount(McpController.MaxAnalyzedProperties, result.Properties);
        var actualValue = result.Error?.Contains("at most") ?? false;
        Assert.IsFalse(actualValue, result.Error);
    }

    /// <summary>
    /// The filters partition the measured catalog the way the verdict does: what the browser plays -
    /// as a timeline or as a recording, nothing else - and what only the frame loop plays.
    /// </summary>
    [TestMethod]
    public async Task GetBmotionAnimatableProperties_TheFiltersPartitionTheMeasuredVerdict()
    {
        var all = await _controller.GetBmotionAnimatableProperties();
        var browser = await _controller.GetBmotionAnimatableProperties("browser");
        var timeline = await _controller.GetBmotionAnimatableProperties("timeline");
        var recorded = await _controller.GetBmotionAnimatableProperties("recorded");
        var frameLoop = await _controller.GetBmotionAnimatableProperties("frame-loop");
        var compositor = await _controller.GetBmotionAnimatableProperties("compositor");

        Assert.AreEqual(all.Length, browser.Length + frameLoop.Length);
        Assert.AreEqual(browser.Length, timeline.Length + recorded.Length);
        Assert.AreSequenceEqual(new[] { "d" }, frameLoop.Select(property => property.Name).ToArray());

        // Width is the property everyone remembers as Server-hostile; it is a browser timeline now.
        Assert.Contains(property => property.Name == "width", timeline);

        // The compositor is the cheap end of what the browser plays, not a list of what survives Server.
        Assert.IsTrue(compositor.All(property => property.PlayedByBrowser));
        Assert.DoesNotContain(property => property.Name == "width", compositor);
    }

    [TestMethod]
    public async Task AnalyzeBmotionAnimation_NoTransitionGiven_FallsBackToAPlainTween()
    {
        var result = await _controller.AnalyzeBmotionAnimation("x", transition: null);

        Assert.IsNull(result.Error);
        Assert.Contains("Bm.Tween", result.Transition);
    }

    /// <summary>
    /// The demo pages used to be a listing of their own, overlapping this one entry for entry. What
    /// only they carried - the title, the route and the keywords, and a description written to say
    /// what the page demonstrates - has to survive the merge, or the merge lost the better half.
    /// </summary>
    [TestMethod]
    public void GetBmotionSourceFiles_CarriesEveryDemoPage_Described()
    {
        var files = _controller.GetBmotionSourceFiles();

        foreach (var page in DocsCatalog.AllPages)
        {
            var path = BmotionSourceCatalog.SourcePathOf(page);
            var file = files.SingleOrDefault(entry => entry.Path == path);

            Assert.IsNotNull(file, $"'{page.Title}' points at '{path}', which is not embedded.");
            Assert.AreEqual("Demo page", file.Kind);
            Assert.AreEqual(page.Title, file.Title);
            Assert.AreEqual(page.Url.Trim('/'), file.Slug);
            Assert.AreEqual(page.Keywords, file.Keywords);

            // Not merely non-empty: the description has to be the page's own sentence rather than
            // whatever comment came first in the file, which is what it used to be.
            Assert.AreEqual(page.Description, file.Description);
        }
    }

    [TestMethod]
    public void GetBmotionSourceFiles_Filter_NarrowsWithoutLosingTheKind()
    {
        var pages = _controller.GetBmotionSourceFiles("Demo page");

        Assert.IsTrue(pages.All(page => page.Kind == "Demo page"));

        // Every docs page, plus the handful under Pages/ that have no catalog entry - the landing,
        // error and not-found pages - which is why this is not an equality against DocsCatalog.AllPages.
        Assert.IsGreaterThanOrEqualTo(DocsCatalog.AllPages.Length, pages.Length);
        Assert.IsTrue(DocsCatalog.AllPages.All(page => pages.Any(file => file.Path == BmotionSourceCatalog.SourcePathOf(page))));

        // A keyword only a catalog entry knows, so a hit proves the merged fields are searched.
        var dragging = _controller.GetBmotionSourceFiles("constraints");

        Assert.Contains(file => file.Path.EndsWith("DragPage.razor", StringComparison.Ordinal), dragging);
    }

    [TestMethod]
    public void GetBmotionSourceFile_ALineRange_ReturnsThatSliceAndSaysWhereItIs()
    {
        var answer = _controller.GetBmotionSourceFile("Demo/Client/Pages/Springs.razor", fromLine: 10, toLine: 14);

        Assert.Contains("lines 10-14 of ", answer);

        // The header, a blank line, and the five lines asked for.
        Assert.HasCount(7, answer.Split('\n'));
    }

    /// <summary>
    /// An off-by-something is not a request worth refusing: the range is clamped, and the header
    /// says what came back so the caller can correct itself.
    /// </summary>
    [TestMethod]
    public void GetBmotionSourceFile_ARangePastTheEnd_IsClampedRatherThanRefused()
    {
        var answer = _controller.GetBmotionSourceFile("Demo/Client/Pages/Springs.razor", fromLine: 90_000, toLine: 99_000);

        Assert.Contains(" of ", answer);
        Assert.IsFalse(answer.Contains("No source file", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task SearchBmotion_DefaultsToAReadableNumberOfHits()
    {
        Assert.IsLessThanOrEqualTo(12, (await _controller.SearchBmotion("spring")).Length);
    }

    /// <summary>
    /// The catalog is reflected off the attributes rather than restated, so the demo page that
    /// documents this server cannot describe a tool that no longer exists or miss one just added.
    /// </summary>
    [TestMethod]
    public void GetMcpCatalog_ReportsTheServersRealSurface()
    {
        var catalog = _controller.GetMcpCatalog();

        Assert.IsGreaterThanOrEqualTo(15, catalog.Tools.Length, $"Only {catalog.Tools.Length} tools were found.");
        Assert.IsNotEmpty(catalog.Prompts);
        Assert.IsNotEmpty(catalog.Resources);

        foreach (var member in catalog.Tools.Concat(catalog.Prompts).Concat(catalog.Resources))
        {
            Assert.AreNotEqual(string.Empty, member.Name.Trim());
            Assert.AreNotEqual(string.Empty, member.Description.Trim(),
                               $"'{member.Name}' has no description, which is all a client has to choose it by.");
            Assert.IsNotNull(member.Parameters);
        }

        // GetMcpCatalog is deliberately not a tool: an agent gets this from tools/list.
        Assert.DoesNotContain(tool => tool.Name == nameof(McpController.GetMcpCatalog), catalog.Tools);
    }

    /// <summary>
    /// The reviewed body is bounded like every other document this server hands out, and what was cut
    /// is said out loud: a review that silently covered the first half of a file would read as a clean
    /// bill of health for the second.
    /// </summary>
    [TestMethod]
    public void ReviewBmotionCode_CodeLongerThanTheLimit_ReviewsWhatFitsAndNamesWhatDidNot()
    {
        const string Markup =
            "<Bmotion Animate=\"Bm.To(opacity: 1)\" Transition=\"Bm.Spring(stiffness: 300, duration: 0.5)\"><div /></Bmotion>";

        var review = _controller.ReviewBmotionCode(Markup + new string('\n', 60_000) + Markup);

        Assert.IsFalse(review.Passed);

        Assert.Contains(finding => finding.Rule == "code-too-long", review.Findings,
                      "Nothing said the code was cut short.");

        // And the rules still ran over the part that fit.
        Assert.Contains(finding => finding.Rule == "spring-physics-overridden-by-duration", review.Findings,
                      "The head of the code was not reviewed.");
    }

    [TestMethod]
    public void GetMcpCatalog_IsSortedSoThePageDoesNotReshuffleBetweenBuilds()
    {
        var catalog = _controller.GetMcpCatalog();

        foreach (var names in new[]
        {
            catalog.Tools.Select(tool => tool.Name).ToArray(),
            catalog.Prompts.Select(prompt => prompt.Name).ToArray(),
            catalog.Resources.Select(resource => resource.Name).ToArray(),
        })
        {
            Assert.AreSequenceEqual(names.OrderBy(name => name, StringComparer.Ordinal).ToArray(), names);
        }
    }

    [TestMethod]
    public void GetMcpCatalog_MarksTheOptionalParameters()
    {
        var catalog = _controller.GetMcpCatalog();

        var search = catalog.Tools.Single(tool => tool.Name == nameof(McpController.SearchBmotion));

        Assert.Contains("query: string", search.Parameters);
        Assert.Contains("limit?: int", search.Parameters);

        var simulate = catalog.Tools.Single(tool => tool.Name == nameof(McpController.SimulateBmotionTransition));

        Assert.Contains("transition: string", simulate.Parameters);
        Assert.Contains("from?: double", simulate.Parameters);
    }
}

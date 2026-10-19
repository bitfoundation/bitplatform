using Bit.Bmotion.Demo.Client.Shared;

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

        Assert.IsTrue(overview.Length > 2_000, $"The overview is only {overview.Length} characters.");

        // The three things it promises: what the library is, how to wire it, and how to work.
        StringAssert.Contains(overview, "## Installation");
        StringAssert.Contains(overview, "## Quick Start");
        StringAssert.Contains(overview, "## How to work");
        StringAssert.Contains(overview, "## Rules of thumb when writing Bmotion code");

        // The rule that decides whether an animation works at all in production.
        StringAssert.Contains(overview, "Blazor Server");
        StringAssert.Contains(overview, "compositor");

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

        StringAssert.Contains(overview, "These tools answer from Bit.Bmotion");
        Assert.IsFalse(overview.Contains("Bit.Bmotion unknown", StringComparison.Ordinal),
                       "The assembly version could not be read.");
    }

    [TestMethod]
    public void GetBmotionSetupGuide_AnUnknownRenderMode_NamesTheOnesThatExist()
    {
        var answer = _controller.GetBmotionSetupGuide("maui");

        foreach (var mode in BmotionSetupGuide.RenderModes)
        {
            StringAssert.Contains(answer, mode, $"The answer does not offer '{mode}'.");
        }
    }

    [TestMethod]
    public void GetBmotionGuideSection_AnUnknownHeading_NamesTheOnesThatExist()
    {
        var answer = _controller.GetBmotionGuideSection("Teleportation");

        StringAssert.Contains(answer, "no section called 'Teleportation'");

        foreach (var section in BmotionSourceCatalog.GuideSections.Take(3))
        {
            StringAssert.Contains(answer, section.Heading);
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
            StringAssert.Contains(answer, recipe.Id);
        }

        StringAssert.Contains(answer, nameof(McpController.SearchBmotion));
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
        StringAssert.Contains(result.Message, "BmSpring");
    }

    [TestMethod]
    public void GetBmotionApiDetails_ANameLikeNothingAtAll_PointsAtTheListing()
    {
        var result = _controller.GetBmotionApiDetails("Teleporter");

        Assert.IsNull(result.Details);
        StringAssert.Contains(result.Message!, nameof(McpController.GetBmotionApiList));
    }

    [TestMethod]
    public void GetBmotionSourceFile_AKnownPath_ReturnsItVerbatim()
    {
        var content = _controller.GetBmotionSourceFile("Demo/Server/Program.cs");

        StringAssert.Contains(content, "MapMcp");
        Assert.AreEqual(BmotionSourceCatalog.GetSourceFile("Demo/Server/Program.cs"), content);
    }

    [TestMethod]
    public void GetBmotionSourceFile_AnAlmostRightPath_SuggestsTheFilesItCouldMean()
    {
        var answer = _controller.GetBmotionSourceFile("Springs.razor");

        StringAssert.Contains(answer, "Did you mean");
        StringAssert.Contains(answer, "Springs.razor");
    }

    [TestMethod]
    public void GetBmotionSourceFile_APathLikeNothingAtAll_PointsAtTheListing()
    {
        StringAssert.Contains(_controller.GetBmotionSourceFile("nowhere/at/all.txt"),
                              nameof(McpController.GetBmotionSourceFiles));
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

        StringAssert.Contains(answer, "[truncated");
        Assert.IsTrue(answer.Length < content.Length);

        // A cut that says only that it happened leaves the caller to guess how to reach the rest.
        StringAssert.Contains(answer, nameof(McpController.GetBmotionSourceFile));
        StringAssert.Contains(answer, "fromLine:");
    }

    [TestMethod]
    public async Task SimulateBmotionTransition_MeasuresEachCandidateSeparately()
    {
        var results = await _controller.SimulateBmotionTransition(
            "spring(stiffness: 260, damping: 12); spring(stiffness: 260, damping: 30); tween(0.3, BackOut)");

        Assert.AreEqual(3, results.Length);
        Assert.IsTrue(results.All(result => result.Error is null));

        // The comparison is only useful if the numbers actually differ between candidates.
        Assert.AreEqual(3, results.Select(result => result.Transition).Distinct().Count());
        Assert.IsTrue(results[0].OvershootPercent > results[1].OvershootPercent,
                      "The lighter-damped spring did not measure as bouncier.");
    }

    [TestMethod]
    public async Task SimulateBmotionTransition_AcceptsNewlinesAsWellAsSemicolons()
    {
        var results = await _controller.SimulateBmotionTransition("spring(stiffness: 200, damping: 20)\ntween(0.4)\r\ninertia(velocity: 300)");

        Assert.AreEqual(3, results.Length);
        CollectionAssert.AreEqual(new[] { "Spring", "Tween", "Inertia" }, results.Select(result => result.Kind).ToArray());
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

        Assert.AreEqual(40, results.Length);
        Assert.AreEqual(McpController.MaxSimulatedTransitions, results.Count(result => result.Error is null));

        // The measured ones come first, in the order asked, and every one after them says why it has
        // no numbers - in Error and in Reading both, as an unreadable spec does.
        Assert.IsTrue(results.Take(McpController.MaxSimulatedTransitions).All(result => result.Error is null));

        foreach (var unmeasured in results.Skip(McpController.MaxSimulatedTransitions))
        {
            StringAssert.Contains(unmeasured.Error, "Not measured");
            StringAssert.Contains(unmeasured.Error, McpController.MaxSimulatedTransitions.ToString());
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

        Assert.AreEqual(1, results.Length);
        Assert.IsNull(results[0].Error);
        Assert.AreEqual("Tween", results[0].Kind);
    }

    [TestMethod]
    public async Task SimulateBmotionTransition_OneUnreadableCandidate_DoesNotCostTheOthers()
    {
        var results = await _controller.SimulateBmotionTransition("spring(stiffness: 200, damping: 20); swoosh(1); tween(0.4)");

        Assert.AreEqual(3, results.Length);
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

        Assert.AreEqual(0, lean.Samples.Length);
        Assert.IsTrue(full.Samples.Length > 1);

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

            CollectionAssert.AreEqual(new[] { "x", "opacity" }, result.Properties, $"'{written}' was read as something else.");
            Assert.IsTrue(result.WorksOnBlazorServer);
        }
    }

    [TestMethod]
    public async Task AnalyzeBmotionAnimation_NoTransitionGiven_FallsBackToAPlainTween()
    {
        var result = await _controller.AnalyzeBmotionAnimation("x", transition: null);

        Assert.IsNull(result.Error);
        StringAssert.Contains(result.Transition, "Bm.Tween");
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

        foreach (var page in NavItem.All)
        {
            var file = files.SingleOrDefault(entry => entry.Path == page.SourcePath);

            Assert.IsNotNull(file, $"'{page.Title}' points at '{page.SourcePath}', which is not embedded.");
            Assert.AreEqual("Demo page", file.Kind);
            Assert.AreEqual(page.Title, file.Title);
            Assert.AreEqual(page.Href, file.Slug);
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

        // Every navigable page, plus the handful under Pages/ that have no nav entry - the error and
        // not-found pages - which is why this is not an equality against NavItem.All.
        Assert.IsTrue(pages.Length >= NavItem.All.Length);
        Assert.IsTrue(NavItem.All.All(page => pages.Any(file => file.Path == page.SourcePath)));

        // A keyword only a nav entry knows, so a hit proves the merged fields are searched.
        var dragging = _controller.GetBmotionSourceFiles("constraints");

        Assert.IsTrue(dragging.Any(file => file.Path.EndsWith("DragPage.razor", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void GetBmotionSourceFile_ALineRange_ReturnsThatSliceAndSaysWhereItIs()
    {
        var answer = _controller.GetBmotionSourceFile("Demo/Client/Pages/Springs.razor", fromLine: 10, toLine: 14);

        StringAssert.Contains(answer, "lines 10-14 of ");

        // The header, a blank line, and the five lines asked for.
        Assert.AreEqual(7, answer.Split('\n').Length);
    }

    /// <summary>
    /// An off-by-something is not a request worth refusing: the range is clamped, and the header
    /// says what came back so the caller can correct itself.
    /// </summary>
    [TestMethod]
    public void GetBmotionSourceFile_ARangePastTheEnd_IsClampedRatherThanRefused()
    {
        var answer = _controller.GetBmotionSourceFile("Demo/Client/Pages/Springs.razor", fromLine: 90_000, toLine: 99_000);

        StringAssert.Contains(answer, " of ");
        Assert.IsFalse(answer.Contains("No source file", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task SearchBmotion_DefaultsToAReadableNumberOfHits()
    {
        Assert.IsTrue((await _controller.SearchBmotion("spring")).Length <= 12);
    }

    /// <summary>
    /// The catalog is reflected off the attributes rather than restated, so the demo page that
    /// documents this server cannot describe a tool that no longer exists or miss one just added.
    /// </summary>
    [TestMethod]
    public void GetMcpCatalog_ReportsTheServersRealSurface()
    {
        var catalog = _controller.GetMcpCatalog();

        Assert.IsTrue(catalog.Tools.Length >= 15, $"Only {catalog.Tools.Length} tools were found.");
        Assert.AreNotEqual(0, catalog.Prompts.Length);
        Assert.AreNotEqual(0, catalog.Resources.Length);

        foreach (var member in catalog.Tools.Concat(catalog.Prompts).Concat(catalog.Resources))
        {
            Assert.AreNotEqual(string.Empty, member.Name.Trim());
            Assert.AreNotEqual(string.Empty, member.Description.Trim(),
                               $"'{member.Name}' has no description, which is all a client has to choose it by.");
            Assert.IsNotNull(member.Parameters);
        }

        // GetMcpCatalog is deliberately not a tool: an agent gets this from tools/list.
        Assert.IsFalse(catalog.Tools.Any(tool => tool.Name == nameof(McpController.GetMcpCatalog)));
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

        Assert.IsTrue(review.Findings.Any(finding => finding.Rule == "code-too-long"),
                      "Nothing said the code was cut short.");

        // And the rules still ran over the part that fit.
        Assert.IsTrue(review.Findings.Any(finding => finding.Rule == "spring-physics-overridden-by-duration"),
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
            CollectionAssert.AreEqual(names.OrderBy(name => name, StringComparer.Ordinal).ToArray(), names);
        }
    }

    [TestMethod]
    public void GetMcpCatalog_MarksTheOptionalParameters()
    {
        var catalog = _controller.GetMcpCatalog();

        var search = catalog.Tools.Single(tool => tool.Name == nameof(McpController.SearchBmotion));

        CollectionAssert.Contains(search.Parameters, "query: string");
        CollectionAssert.Contains(search.Parameters, "limit?: int");

        var simulate = catalog.Tools.Single(tool => tool.Name == nameof(McpController.SimulateBmotionTransition));

        CollectionAssert.Contains(simulate.Parameters, "transition: string");
        CollectionAssert.Contains(simulate.Parameters, "from?: double");
    }
}

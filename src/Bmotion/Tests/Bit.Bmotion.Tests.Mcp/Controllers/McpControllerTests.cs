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

        // The three things it promises: what the library is, how to wire it, and what to call next.
        StringAssert.Contains(overview, "## Installation");
        StringAssert.Contains(overview, "## Quick Start");
        StringAssert.Contains(overview, "## Which tool to call");
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
        var recipe = _controller.GetBmotionRecipe("staggered-list") as BmotionRecipeDto;

        Assert.IsNotNull(recipe, "A known id did not come back as a recipe.");
        Assert.AreEqual("staggered-list", recipe.Id);
        Assert.IsFalse(string.IsNullOrWhiteSpace(recipe.Code));
        Assert.IsFalse(string.IsNullOrWhiteSpace(recipe.Notes));
    }

    [TestMethod]
    public void GetBmotionRecipe_AnUnknownId_NamesEveryIdThatExists()
    {
        var answer = _controller.GetBmotionRecipe("teleport") as string;

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

        if (content.Length <= 40_000)
        {
            Assert.AreEqual(content, answer, "A file inside the limit was altered.");

            return;
        }

        StringAssert.Contains(answer, "[truncated");
        Assert.IsTrue(answer.Length < content.Length);
    }

    [TestMethod]
    public async Task CompareBmotionTransitions_MeasuresEachCandidateSeparately()
    {
        var results = await _controller.CompareBmotionTransitions(
            "spring(stiffness: 260, damping: 12); spring(stiffness: 260, damping: 30); tween(0.3, BackOut)");

        Assert.AreEqual(3, results.Length);
        Assert.IsTrue(results.All(result => result.Error is null));

        // The comparison is only useful if the numbers actually differ between candidates.
        Assert.AreEqual(3, results.Select(result => result.Transition).Distinct().Count());
        Assert.IsTrue(results[0].OvershootPercent > results[1].OvershootPercent,
                      "The lighter-damped spring did not measure as bouncier.");
    }

    [TestMethod]
    public async Task CompareBmotionTransitions_AcceptsNewlinesAsWellAsSemicolons()
    {
        var results = await _controller.CompareBmotionTransitions("spring(stiffness: 200, damping: 20)\ntween(0.4)\r\ninertia(velocity: 300)");

        Assert.AreEqual(3, results.Length);
        CollectionAssert.AreEqual(new[] { "Spring", "Tween", "Inertia" }, results.Select(result => result.Kind).ToArray());
    }

    /// <summary>
    /// Simulating dozens at once would spend more of a client's context than any comparison is read
    /// with, and the interesting comparisons are between two and four candidates.
    /// </summary>
    [TestMethod]
    public async Task CompareBmotionTransitions_IsCappedRatherThanRunningWhateverArrives()
    {
        var many = string.Join("; ", Enumerable.Range(1, 40).Select(i => $"tween({i / 100.0})"));

        var results = await _controller.CompareBmotionTransitions(many);

        Assert.AreEqual(8, results.Length);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow(";;;")]
    [DataRow(null)]
    public async Task CompareBmotionTransitions_NothingToCompare_IsAnEmptyAnswerRatherThanAThrow(string? transitions)
    {
        Assert.AreEqual(0, (await _controller.CompareBmotionTransitions(transitions!)).Length);
    }

    [TestMethod]
    public async Task CompareBmotionTransitions_OneUnreadableCandidate_DoesNotCostTheOthers()
    {
        var results = await _controller.CompareBmotionTransitions("spring(stiffness: 200, damping: 20); swoosh(1); tween(0.4)");

        Assert.AreEqual(3, results.Length);
        Assert.IsNull(results[0].Error);
        Assert.IsNotNull(results[1].Error);
        Assert.IsNull(results[2].Error);
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

    [TestMethod]
    public void GetBmotionDemoPages_ListsEveryPage_WithASourceFileThatResolves()
    {
        var pages = _controller.GetBmotionDemoPages();

        Assert.AreEqual(NavItem.All.Length, pages.Length);

        foreach (var page in pages)
        {
            Assert.AreNotEqual(string.Empty, page.Title.Trim());
            Assert.AreNotEqual(string.Empty, page.Description.Trim());
            Assert.AreNotEqual(string.Empty, page.Keywords.Trim());
            Assert.IsNotNull(BmotionSourceCatalog.GetSourceFile(page.SourcePath),
                             $"'{page.Title}' points at '{page.SourcePath}', which is not embedded.");
        }
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

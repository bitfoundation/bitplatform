namespace Bit.Bmage.Tests.Mcp.Controllers;

/// <summary>
/// The tools as C# methods: what each one answers for the questions it was built for, and - just
/// as much - what it answers when it is asked for something that does not exist. An agent's next
/// call is decided by that second answer, so it has to name what does exist.
/// </summary>
[TestClass]
public class McpControllerTests
{
    private readonly McpController _controller = new();

    [TestMethod]
    public void Overview_CarriesTheReadmeIntro_TheVersion_AndTheRulesNoSignatureShows()
    {
        var overview = _controller.GetBmageOverview();

        StringAssert.StartsWith(overview, "# bit Bmage");
        StringAssert.Contains(overview, BmageServerInstructions.BmageVersion);
        StringAssert.Contains(overview, "## Packages");
        StringAssert.Contains(overview, "## Installation");
        StringAssert.Contains(overview, "## Where it runs");
        StringAssert.Contains(overview, "AddBitBmageHeadlessServices");
        StringAssert.Contains(overview, "ImageProcessorFactory.GetAsync()");
        StringAssert.Contains(overview, "OnAfterRenderAsync");
        StringAssert.Contains(overview, "bit-bmage-editor.css");
        Assert.IsFalse(overview.Contains("was not found in this build", StringComparison.Ordinal), "A README section the overview quotes has been renamed.");
        Assert.IsFalse(overview.Contains("## Table of Contents", StringComparison.Ordinal), "The overview quotes the README's table of contents.");
    }

    [TestMethod]
    [DataRow("wasm", "BOTH containers")]
    [DataRow("server", "MaxInteractiveDimension = 1200")]
    [DataRow("auto", "InteractiveAuto")]
    [DataRow("standalone-wasm", "wwwroot/index.html")]
    [DataRow("headless", "AddBitBmageHeadlessServices")]
    [DataRow("InteractiveServer", "InteractiveServer")]
    [DataRow("console", "new ImageProcessor()")]
    public void SetupGuide_AnswersEveryHostingModel_AndItsAliases(string mode, string expected)
    {
        StringAssert.Contains(_controller.GetBmageSetupGuide(mode), expected);
    }

    [TestMethod]
    public void SetupGuide_AnUnknownMode_ListsTheKnownOnes()
    {
        var answer = _controller.GetBmageSetupGuide("maui");

        StringAssert.Contains(answer, "not a known hosting model");

        foreach (var mode in BmageSetupGuide.RenderModes) StringAssert.Contains(answer, mode);
    }

    [TestMethod]
    public void Recipes_TheListing_LeavesOutTheCode_AndTheRecipeCarriesIt()
    {
        var listing = _controller.GetBmageRecipes();

        Assert.AreEqual(BmageRecipeCatalog.All.Length, listing.Length);
        Assert.IsTrue(listing.All(recipe => recipe.Code is null && recipe.Notes is null), "The listing carries whole recipes.");

        var recipe = _controller.GetBmageRecipe("AVATAR-CROP").Recipe;

        Assert.IsNotNull(recipe);
        StringAssert.Contains(recipe.Code, "SmartCrop(AspectRatio.Square)");
        StringAssert.Contains(recipe.Notes, "ImageEncodeException");
    }

    [TestMethod]
    public void Documents_ListsTheReadmeAndEveryGuide_WithTheirSections()
    {
        var documents = _controller.GetBmageDocuments();

        Assert.AreEqual("readme", documents[0].Name, "The README is not first.");
        CollectionAssert.IsSubsetOf(new[] { "getting-started", "pipelines", "formats", "metadata-and-privacy", "headless", "troubleshooting", "editor", "security" },
                                    documents.Select(document => document.Name).ToArray());

        var pipelines = documents.Single(document => document.Name == "pipelines");

        Assert.AreEqual("Pipelines", pipelines.Title);
        CollectionAssert.Contains(pipelines.Sections, "The optimiser");
        Assert.IsFalse(string.IsNullOrWhiteSpace(pipelines.Summary));
    }

    [TestMethod]
    public void Document_WholeOrOneSection_AndNamesAreForgiving()
    {
        StringAssert.StartsWith(_controller.GetBmageDocument("docs/pipelines.md"), "# Pipelines");
        StringAssert.StartsWith(_controller.GetBmageDocument("Pipelines"), "# Pipelines");
        StringAssert.StartsWith(_controller.GetBmageDocument("overview"), "# bit Bmage");

        var section = _controller.GetBmageDocument("pipelines", "the OPTIMISER");

        StringAssert.StartsWith(section, "## The optimiser");
        Assert.IsFalse(section.Contains("## Previewing at another resolution", StringComparison.Ordinal), "The section ran on into the next one.");

        // A sub-section is a section too, and a parenthesised aside in a heading is optional.
        StringAssert.StartsWith(_controller.GetBmageDocument("pipelines", "The six resize modes"), "### The six resize modes");
        StringAssert.StartsWith(_controller.GetBmageDocument("readme", "MCP server"), "## MCP server (for AI agents)");
    }

    [TestMethod]
    public void Document_WhatIsNotThere_NamesWhatIs()
    {
        var missing = _controller.GetBmageDocument("teleportation");

        StringAssert.Contains(missing, "no Bit.Bmage guide called");
        StringAssert.Contains(missing, "'troubleshooting'");

        var section = _controller.GetBmageDocument("formats", "Teleportation");

        StringAssert.Contains(section, "has no section called");
        StringAssert.Contains(section, "'Quality and size budgets'");
    }

    [TestMethod]
    public void ApiList_FiltersByPackageKindNamespaceAndName()
    {
        var all = _controller.GetBmageApiList();

        var editor = _controller.GetBmageApiList("Bit.Bmage.Editor");
        Assert.IsTrue(editor.All(type => type.Package == "Bit.Bmage.Editor"));
        Assert.IsTrue(editor.Any(type => type.Name == "BmageEditor"));

        var enums = _controller.GetBmageApiList("enum");
        Assert.IsTrue(enums.All(type => type.Kind == "Enum"));
        Assert.IsTrue(enums.Any(type => type.Name == "MetadataPolicy"));

        var annotations = _controller.GetBmageApiList("Bit.Bmage.Annotations");
        Assert.IsTrue(annotations.Any(type => type.Name == "RedactionAnnotation"));

        var export = _controller.GetBmageApiList("Export");
        Assert.IsTrue(export.Any(type => type.Name == "ImageExportOptions") && export.Any(type => type.Name == "ImageExporter"));

        Assert.IsLessThan(all.Length, export.Length);
        Assert.IsEmpty(_controller.GetBmageApiList("Teleporter"), "A filter that matches nothing must not fall back to everything.");
    }

    [TestMethod]
    public void ApiDetails_AType_AMember_AndTheNearMisses()
    {
        var type = _controller.GetBmageApiDetails("ImageExportOptions").Details;

        Assert.IsNotNull(type);
        Assert.AreEqual("Bit.Bmage.Codecs", type.Namespace);
        Assert.AreEqual("Bit.Bmage.Core", type.Package);

        var member = _controller.GetBmageApiDetails("ImagePipeline.Cover").Details;

        Assert.IsNotNull(member);
        Assert.IsTrue(member.Members.All(entry => entry.Name == "Cover"));
        StringAssert.Contains(member.Members[0].Signature, "Anchor anchor = Anchor.Center");

        var noMember = _controller.GetBmageApiDetails("ImagePipeline.Teleport");
        Assert.IsNull(noMember.Details);
        StringAssert.Contains(noMember.Message, "no member called 'Teleport'");
        StringAssert.Contains(noMember.Message, "MaxSize");

        var nearMiss = _controller.GetBmageApiDetails("Exporter");
        Assert.IsNull(nearMiss.Details);
        StringAssert.Contains(nearMiss.Message, "ImageExporter");

        StringAssert.Contains(_controller.GetBmageApiDetails("Teleporter").Message, "SearchBmage");
    }

    [TestMethod]
    public void SourceFiles_FilterByKind_AndARangeIsLabelled()
    {
        var pages = _controller.GetBmageSourceFiles("Demo page");

        Assert.IsTrue(pages.All(file => file.Kind == "Demo page"));
        Assert.IsTrue(pages.Any(file => file.Path == "Demo/Client/Pages/Avatar.razor" && file.Slug == "avatar"));

        Assert.IsTrue(_controller.GetBmageSourceFiles("Host").Any(file => file.Path == "Demo/Server/Program.cs"));

        var slice = _controller.GetBmageSourceFile("Demo/Client/Pages/Avatar.razor", 1, 3);

        StringAssert.StartsWith(slice, "// Demo/Client/Pages/Avatar.razor, lines 1-3 of ");
        StringAssert.Contains(slice, "@page \"/avatar\"");

        // Out of range is clamped, and the header says what came back.
        StringAssert.Contains(_controller.GetBmageSourceFile("Demo/Client/Pages/Avatar.razor", 5000, 6000), "lines ");
    }

    [TestMethod]
    public void SourceFile_AMissingPath_SuggestsTheFileByItsName()
    {
        var answer = _controller.GetBmageSourceFile("Pages/Avatar.razor");

        StringAssert.Contains(answer, "Demo/Client/Pages/Avatar.razor");

        StringAssert.Contains(_controller.GetBmageSourceFile("nowhere.razor"), "GetBmageSourceFiles");
    }

    [TestMethod]
    public void Review_TooLongACode_IsReviewedInPart_AndSaysSo()
    {
        var code = "MetadataPolicy.Preserve\n" + new string('x', McpController.MaxDocumentLength);

        var review = _controller.ReviewBmageCode(code);

        Assert.IsFalse(review.Passed);
        Assert.AreEqual("code-too-long", review.Findings[0].Rule);
        Assert.IsTrue(review.Findings.Any(finding => finding.Rule == "metadata-preserve-keeps-location"));
    }

    [TestMethod]
    public void Truncate_ACutDocument_SaysHowToReadOn()
    {
        var text = string.Join('\n', Enumerable.Range(0, 4000).Select(i => $"line {i}"));

        var cut = McpController.Truncate(text, "Demo/Client/Pages/Avatar.razor");

        Assert.IsLessThan(text.Length, cut.Length);
        StringAssert.Contains(cut, "GetBmageSourceFile(path: \"Demo/Client/Pages/Avatar.razor\", fromLine: ");

        StringAssert.Contains(McpController.Truncate(text, document: "pipelines"), "GetBmageDocument(name: \"pipelines\"");
    }

    [TestMethod]
    public void Catalog_ListsEveryToolPromptAndResource_WithParameters()
    {
        var catalog = _controller.GetMcpCatalog();

        Assert.AreEqual(14, catalog.Tools.Length, string.Join(", ", catalog.Tools.Select(tool => tool.Name)));
        Assert.AreEqual(5, catalog.Prompts.Length);
        Assert.IsGreaterThanOrEqualTo(9, catalog.Resources.Length);

        var run = catalog.Tools.Single(tool => tool.Name == nameof(McpController.RunBmagePipeline));

        CollectionAssert.Contains(run.Parameters, "pipeline: string");
        CollectionAssert.Contains(run.Parameters, "maxFileSize?: long?");
    }
}

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

        Assert.StartsWith("# bit Bmage", overview);
        Assert.Contains(BmageServerInstructions.BmageVersion, overview);
        Assert.Contains("## Packages", overview);
        Assert.Contains("## Installation", overview);
        Assert.Contains("## Where it runs", overview);
        Assert.Contains("AddBitBmageHeadlessServices", overview);
        Assert.Contains("ImageProcessorFactory.GetAsync()", overview);
        Assert.Contains("OnAfterRenderAsync", overview);
        Assert.Contains("bit-bmage-editor.css", overview);
        Assert.IsFalse(overview.Contains("was not found in this build", StringComparison.Ordinal), "A README section the overview quotes has been renamed.");
        Assert.IsFalse(overview.Contains("## Table of Contents", StringComparison.Ordinal), "The overview quotes the README's table of contents.");
    }

    [TestMethod]
    [DataRow("wasm", "BOTH containers")]
    [DataRow("server", "MaxInteractiveDimension = 1200")]
    [DataRow("auto", "InteractiveAuto")]
    [DataRow("standalone-wasm", "wwwroot/index.html")]
    [DataRow("hybrid", "AddMauiBlazorWebView")]
    [DataRow("MAUI", "AddMauiBlazorWebView")]
    [DataRow("headless", "AddBitBmageHeadlessServices")]
    [DataRow("InteractiveServer", "InteractiveServer")]
    [DataRow("console", "new ImageProcessor()")]
    public void SetupGuide_AnswersEveryHostingModel_AndItsAliases(string mode, string expected)
    {
        Assert.Contains(expected, _controller.GetBmageSetupGuide(mode));
    }

    [TestMethod]
    public void SetupGuide_AnUnknownMode_ListsTheKnownOnes()
    {
        var answer = _controller.GetBmageSetupGuide("electron");

        Assert.Contains("not a known hosting model", answer);

        foreach (var mode in BmageSetupGuide.RenderModes) Assert.Contains(mode, answer);
    }

    [TestMethod]
    public void Recipes_TheListing_LeavesOutTheCode_AndTheRecipeCarriesIt()
    {
        var listing = _controller.GetBmageRecipes();

        Assert.HasCount(BmageRecipeCatalog.All.Length, listing);
        Assert.IsTrue(listing.All(recipe => recipe.Code is null && recipe.Notes is null), "The listing carries whole recipes.");

        var recipe = _controller.GetBmageRecipe("AVATAR-CROP").Recipe;

        Assert.IsNotNull(recipe);
        Assert.Contains("SmartCrop(AspectRatio.Square)", recipe.Code!);
        Assert.Contains("ImageEncodeException", recipe.Notes!);
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
        Assert.Contains("The optimiser", pipelines.Sections);
        Assert.IsFalse(string.IsNullOrWhiteSpace(pipelines.Summary));
    }

    [TestMethod]
    public void Document_WholeOrOneSection_AndNamesAreForgiving()
    {
        Assert.StartsWith("# Pipelines", _controller.GetBmageDocument("docs/pipelines.md"));
        Assert.StartsWith("# Pipelines", _controller.GetBmageDocument("Pipelines"));
        Assert.StartsWith("# bit Bmage", _controller.GetBmageDocument("overview"));

        var section = _controller.GetBmageDocument("pipelines", "the OPTIMISER");

        Assert.StartsWith("## The optimiser", section);
        Assert.IsFalse(section.Contains("## Previewing at another resolution", StringComparison.Ordinal), "The section ran on into the next one.");

        // A sub-section is a section too, and a parenthesised aside in a heading is optional.
        Assert.StartsWith("### The six resize modes", _controller.GetBmageDocument("pipelines", "The six resize modes"));
        Assert.StartsWith("## MCP server (for AI agents)", _controller.GetBmageDocument("readme", "MCP server"));
    }

    [TestMethod]
    public void Document_WhatIsNotThere_NamesWhatIs()
    {
        var missing = _controller.GetBmageDocument("teleportation");

        Assert.Contains("no Bit.Bmage guide called", missing);
        Assert.Contains("'troubleshooting'", missing);

        var section = _controller.GetBmageDocument("formats", "Teleportation");

        Assert.Contains("has no section called", section);
        Assert.Contains("'Quality and size budgets'", section);
    }

    [TestMethod]
    public void ApiList_FiltersByKindNamespaceAndName()
    {
        var all = _controller.GetBmageApiList();

        // No type lives in Bit.Bmage.Editor itself, so it matches as a part of a namespace: every editor namespace.
        var editor = _controller.GetBmageApiList("Bit.Bmage.Editor");
        Assert.IsTrue(editor.All(type => type.Namespace.StartsWith("Bit.Bmage.Editor.", StringComparison.Ordinal)));
        Assert.IsTrue(editor.Any(type => type.Name == "BmageEditor") && editor.Any(type => type.Name == "EditorToolKind"));

        var enums = _controller.GetBmageApiList("enum");
        Assert.IsTrue(enums.All(type => type.Kind == "Enum"));
        Assert.Contains(type => type.Name == "MetadataPolicy", enums);

        var annotations = _controller.GetBmageApiList("Bit.Bmage.Annotations");
        Assert.Contains(type => type.Name == "RedactionAnnotation", annotations);

        var export = _controller.GetBmageApiList("Export");
        Assert.IsTrue(export.Any(type => type.Name == "ImageExportOptions") && export.Any(type => type.Name == "ImageExporter"));

        Assert.IsLessThan(all.Length, export.Length);
        Assert.IsEmpty(_controller.GetBmageApiList("Teleporter"), "A filter that matches nothing must not fall back to everything.");
    }

    [TestMethod]
    public void ApiList_APackageName_ListsThatPackageAlone_ThoughItIsANamespaceToo()
    {
        // Both packages put types in the Bit.Bmage namespace: "Bit.Bmage" asks for the engine's package, and the browser
        // registration that lives in that namespace in Bit.Bmage.Blazor must not come with it.
        var engine = _controller.GetBmageApiList("Bit.Bmage");
        Assert.IsTrue(engine.All(type => type.Package == "Bit.Bmage"));
        Assert.IsTrue(engine.Any(type => type.Name == "BitBmage") && engine.All(type => type.Name != "BitBmageBlazor"));

        var blazor = _controller.GetBmageApiList("Bit.Bmage.Blazor");
        Assert.IsTrue(blazor.All(type => type.Package == "Bit.Bmage.Blazor"));
        Assert.IsTrue(blazor.Any(type => type.Name == "BitBmageBlazor") && blazor.Any(type => type.Name == "BmageEditor"));
    }

    [TestMethod]
    public void ApiDetails_AType_AMember_AndTheNearMisses()
    {
        var type = _controller.GetBmageApiDetails("ImageExportOptions").Details;

        Assert.IsNotNull(type);
        Assert.AreEqual("Bit.Bmage.Codecs", type.Namespace);
        Assert.AreEqual("Bit.Bmage", type.Package);

        var member = _controller.GetBmageApiDetails("ImagePipeline.Cover").Details;

        Assert.IsNotNull(member);
        Assert.IsTrue(member.Members.All(entry => entry.Name == "Cover"));
        Assert.Contains("Anchor anchor = Anchor.Center", member.Members[0].Signature!);

        var noMember = _controller.GetBmageApiDetails("ImagePipeline.Teleport");
        Assert.IsNull(noMember.Details);
        Assert.Contains("no member called 'Teleport'", noMember.Message!);
        Assert.Contains("MaxSize", noMember.Message);

        var nearMiss = _controller.GetBmageApiDetails("Exporter");
        Assert.IsNull(nearMiss.Details);
        Assert.Contains("ImageExporter", nearMiss.Message!);

        Assert.Contains("SearchBmage", _controller.GetBmageApiDetails("Teleporter").Message!);
    }

    [TestMethod]
    public void SourceFiles_FilterByKind_AndARangeIsLabelled()
    {
        var pages = _controller.GetBmageSourceFiles("Demo page");

        Assert.IsTrue(pages.All(file => file.Kind == "Demo page"));
        Assert.Contains(file => file.Path == "Demo/Client/Pages/Avatar.razor" && file.Slug == "avatar", pages);

        Assert.Contains(file => file.Path == "Demo/Server/Program.cs", _controller.GetBmageSourceFiles("Host"));

        var slice = _controller.GetBmageSourceFile("Demo/Client/Pages/Avatar.razor", 1, 3);

        Assert.StartsWith("// Demo/Client/Pages/Avatar.razor, lines 1-3 of ", slice);
        Assert.Contains("@page \"/avatar\"", slice);

        // Out of range is clamped, and the header says what came back.
        Assert.Contains("lines ", _controller.GetBmageSourceFile("Demo/Client/Pages/Avatar.razor", 5000, 6000));
    }

    [TestMethod]
    public void SourceFile_AMissingPath_SuggestsTheFileByItsName()
    {
        var answer = _controller.GetBmageSourceFile("Pages/Avatar.razor");

        Assert.Contains("Demo/Client/Pages/Avatar.razor", answer);

        Assert.Contains("GetBmageSourceFiles", _controller.GetBmageSourceFile("nowhere.razor"));
    }

    [TestMethod]
    public void Review_TooLongACode_IsReviewedInPart_AndSaysSo()
    {
        var code = "MetadataPolicy.Preserve\n" + new string('x', McpController.MaxDocumentLength);

        var review = _controller.ReviewBmageCode(code);

        Assert.IsFalse(review.Passed);
        Assert.AreEqual("code-too-long", review.Findings[0].Rule);
        Assert.Contains(finding => finding.Rule == "metadata-preserve-keeps-location", review.Findings);
    }

    [TestMethod]
    public void Truncate_ACutDocument_SaysHowToReadOn()
    {
        var text = string.Join('\n', Enumerable.Range(0, 4000).Select(i => $"line {i}"));

        var cut = McpController.Truncate(text, "Demo/Client/Pages/Avatar.razor");

        Assert.IsLessThan(text.Length, cut.Length);
        Assert.Contains("GetBmageSourceFile(path: \"Demo/Client/Pages/Avatar.razor\", fromLine: ", cut);

        Assert.Contains("GetBmageDocument(name: \"pipelines\"", McpController.Truncate(text, document: "pipelines"));
    }

    [TestMethod]
    public void Catalog_ListsEveryToolPromptAndResource_WithParameters()
    {
        var catalog = _controller.GetMcpCatalog();

        Assert.HasCount(14, catalog.Tools, string.Join(", ", catalog.Tools.Select(tool => tool.Name)));
        Assert.HasCount(5, catalog.Prompts);
        Assert.IsGreaterThanOrEqualTo(9, catalog.Resources.Length);

        var run = catalog.Tools.Single(tool => tool.Name == nameof(McpController.RunBmagePipeline));

        Assert.Contains("pipeline: string", run.Parameters);
        Assert.Contains("maxFileSize?: long?", run.Parameters);
    }
}

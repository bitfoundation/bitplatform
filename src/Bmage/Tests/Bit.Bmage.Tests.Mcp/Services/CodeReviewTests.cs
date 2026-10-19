namespace Bit.Bmage.Tests.Mcp.Services;

/// <summary>
/// Every review rule, in both directions: the mistake it exists for is reported on the right line,
/// and the correct version of the same code is not. A rule that fires on correct code teaches an
/// agent to stop reading the review, which costs more than the rule ever catches.
/// </summary>
[TestClass]
public class CodeReviewTests
{
    // -- browser-call-during-prerender -------------------------------------------------------

    [TestMethod]
    public void Prerender_AJsCallInOnInitializedAsync_IsAnError()
    {
        var review = Review("""
            @inject ImageExporter Exporter

            @code {
                protected override async Task OnInitializedAsync()
                {
                    _url = await Exporter.CreateObjectUrlAsync(_encoded);
                }
            }
            """);

        var finding = Single(review, "browser-call-during-prerender");

        Assert.AreEqual("Error", finding.Severity);
        Assert.AreEqual(6, finding.Line);
        StringAssert.Contains(finding.Fix, "OnAfterRenderAsync");
    }

    [TestMethod]
    public void Prerender_ACapabilityProbeInOnInitialized_IsAWarning_ThatExplainsTheManagedAnswer()
    {
        var review = Review("""
            protected override async Task OnInitializedAsync() => _caps = await Capabilities.GetAsync();
            """);

        var finding = Single(review, "browser-call-during-prerender");

        Assert.AreEqual("Warning", finding.Severity);
        StringAssert.Contains(finding.Message, "Prerendering");
    }

    [TestMethod]
    public void Prerender_AJsCallInOnParametersSetAsync_IsAWarning_UnlessItIsGuarded()
    {
        var unguarded = Review("""
            protected override async Task OnParametersSetAsync()
            {
                _url = await Exporter.CreateObjectUrlAsync(await Exporter.EncodeAsync(Image));
            }
            """);

        Assert.IsTrue(unguarded.Findings.Where(finding => finding.Rule == "browser-call-during-prerender").All(finding => finding.Severity == "Warning"));
        Assert.IsNotEmpty(unguarded.Findings.Where(finding => finding.Rule == "browser-call-during-prerender"));

        None(Review("""
            protected override async Task OnParametersSetAsync()
            {
                if (!RendererInfo.IsInteractive) return;
                _url = await Exporter.CreateObjectUrlAsync(await Exporter.EncodeAsync(Image));
            }
            """), "browser-call-during-prerender");
    }

    [TestMethod]
    public void Prerender_TheSameCallsInOnAfterRenderAsync_AreFine()
    {
        var review = Review("""
            @code {
                protected override async Task OnInitializedAsync()
                {
                    _items = await Http.GetFromJsonAsync<Item[]>("api/items");
                }

                protected override async Task OnAfterRenderAsync(bool firstRender)
                {
                    if (!firstRender || !RendererInfo.IsInteractive) return;
                    _caps = await Capabilities.GetAsync();
                    _paste = await Loader.ListenForPasteAsync(OnPaste);
                }
            }
            """);

        None(review, "browser-call-during-prerender");
    }

    // -- image-buffer-not-disposed ------------------------------------------------------------

    [TestMethod]
    public void Disposal_ABufferFromLoadAsyncNeverDisposed_IsReported()
    {
        var review = Review("""
            private async Task OnChange(InputFileChangeEventArgs e)
            {
                var image = await Loader.LoadAsync(e.File);
                var thumb = await Thumbnail.ExecuteAsync(image);
                _bytes = (await Exporter.EncodeAsync(thumb)).Length;
            }
            """);

        var findings = review.Findings.Where(finding => finding.Rule == "image-buffer-not-disposed").ToArray();

        Assert.HasCount(2, findings);
        Assert.AreEqual(3, findings[0].Line);
        Assert.AreEqual(4, findings[1].Line);
    }

    [TestMethod]
    public void Disposal_UsingDisposeReturnAndHandingItOver_AreAllFine()
    {
        var review = Review("""
            using var image = await Loader.LoadAsync(e.File);
            var thumb = await Thumbnail.ExecuteAsync(image);
            try { await Exporter.EncodeAsync(thumb); } finally { thumb.Dispose(); }

            var kept = await Loader.LoadAsync(e.File);
            _source = kept;

            var returned = await pipeline.ExecuteAsync(source);
            return returned;

            var loaded = await Loader.LoadAsync(file);
            await SetSourceAsync(loaded, file.Size);

            var canvas = ImageCanvas.Create(100, 100);
            using (canvas) { }
            """);

        None(review, "image-buffer-not-disposed");
    }

    [TestMethod]
    public void Disposal_AnUndisposedCanvas_IsReportedAsOne()
    {
        var finding = Single(Review("var canvas = ImageCanvas.Create(1200, 630, background);\ncanvas.FillCircle(center, 10, red);"), "image-buffer-not-disposed");

        StringAssert.Contains(finding.Message, "ImageCanvas");
    }

    // -- borrowed-buffer-disposed / editor-owns-loaded-buffer ----------------------------------

    [TestMethod]
    public void Ownership_DisposingTheSessionsPreview_IsAnError()
    {
        Assert.AreEqual("Error", Single(Review("_editor.Session!.Preview.Dispose();"), "borrowed-buffer-disposed").Severity);
        Single(Review("using var preview = _editor.Session.Preview;"), "borrowed-buffer-disposed");

        None(Review("using var copy = _editor.Session.Preview.Clone();"), "borrowed-buffer-disposed");
    }

    [TestMethod]
    public void Ownership_DisposingABufferTheEditorTookOver_IsAnError()
    {
        var review = Review("""
            using var image = await Loader.LoadAsync(file);
            await _editor.LoadImageAsync(image);
            """);

        Assert.AreEqual(2, Single(review, "editor-owns-loaded-buffer").Line);

        None(Review("using var image = await Loader.LoadAsync(file);\nawait _editor.LoadImageAsync(image, takeOwnership: false);"), "editor-owns-loaded-buffer");
        None(Review("var image = await Loader.LoadAsync(file);\nawait _editor.LoadImageAsync(image);"), "editor-owns-loaded-buffer");
    }

    // -- stylesheet, script, height -----------------------------------------------------------

    [TestMethod]
    public void Stylesheet_AHeadWithTheEditorInUseButNoLink_IsReported()
    {
        var review = Review("""
            <!DOCTYPE html>
            <html>
            <head>
                <link rel="stylesheet" href="css/app.css" />
            </head>
            <body>
                <BmageEditor />
            </body>
            </html>
            """);

        Assert.AreEqual(3, Single(review, "editor-stylesheet-missing").Line);
    }

    [TestMethod]
    public void Stylesheet_TheRightLink_IsFine_AndAWrongPathIsAnError()
    {
        None(Review("<head>\n<link rel=\"stylesheet\" href=\"_content/Bit.Bmage.Editor/bit-bmage-editor.css\" />\n</head>\n<BmageEditor />"), "editor-stylesheet-missing");
        None(Review("<link rel=\"stylesheet\" href=\"/_content/Bit.Bmage.Editor/bit-bmage-editor.css\" />"), "editor-stylesheet-wrong-path");

        // The demo's own stylesheets are not the editor's, and must not be mistaken for a wrong path to it.
        None(Review("<link rel=\"stylesheet\" href=\"css/bmage-samples.css\" />"), "editor-stylesheet-wrong-path");

        Assert.AreEqual("Error", Single(Review("<link rel=\"stylesheet\" href=\"_content/Bit.Bmage/bit-bmage-editor.css\" />"), "editor-stylesheet-wrong-path").Severity);
        Single(Review("<link rel=\"stylesheet\" href=\"_content/Bit.Bmage.Editor/bit-bmage.css\" />"), "editor-stylesheet-wrong-path");
    }

    [TestMethod]
    public void Script_ATagForTheBridge_IsReported()
    {
        Single(Review("<script src=\"_content/Bit.Bmage/bit-bmage.js\"></script>"), "bridge-script-tag");

        None(Review("<script src=\"_framework/blazor.web.js\"></script>"), "bridge-script-tag");
    }

    [TestMethod]
    public void Height_AnEditorWithNoHeightAnywhere_IsASuggestion()
    {
        Assert.AreEqual("Suggestion", Single(Review("<BmageEditor @ref=\"_editor\" />"), "editor-without-height").Severity);

        None(Review("<div style=\"height: 70vh\">\n    <BmageEditor />\n</div>"), "editor-without-height");
        None(Review("<BmageEditor Class=\"my-editor\" />"), "editor-without-height");
    }

    // -- registrations ------------------------------------------------------------------------

    [TestMethod]
    public void Registration_InjectingImageProcessorInAComponent_IsAnError()
    {
        var finding = Single(Review("@page \"/x\"\n@inject ImageProcessor Processor"), "processor-injected-in-browser");

        Assert.AreEqual(2, finding.Line);
        StringAssert.Contains(finding.Fix, "ImageProcessorFactory");

        None(Review("@inject ImageProcessorFactory Processors"), "processor-injected-in-browser");

        // On a server with the headless registration, the processor is exactly what is registered.
        None(Review("builder.Services.AddBitBmageHeadlessServices();\n[Inject] public ImageProcessor Processor { get; set; } = default!;"), "processor-injected-in-browser");
    }

    [TestMethod]
    public void Registration_BrowserServicesWithOnlyTheHeadlessRegistration_IsAnError()
    {
        var review = Review("""
            builder.Services.AddBitBmageHeadlessServices();
            // ...
            @inject ImageLoader Loader
            """);

        var finding = Single(review, "browser-services-with-headless-registration");

        Assert.AreEqual(3, finding.Line);
        StringAssert.Contains(finding.Message, "ImageLoader");

        None(Review("builder.Services.AddBitBmageHeadlessServices();\napp.MapPost(\"/x\", (ImageProcessor processor) => 1);"), "browser-services-with-headless-registration");
    }

    [TestMethod]
    public void Registration_TheHeadlessServicesInAWebAssemblyHost_IsAnError()
    {
        var review = Review("""
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.Services.AddBitBmageHeadlessServices();
            await builder.Build().RunAsync();
            """);

        Assert.AreEqual(2, Single(review, "headless-services-in-browser").Line);
    }

    [TestMethod]
    public void Registration_AWebAppHostThatNeverRegistersBmage_IsAWarning()
    {
        var review = Review("""
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddRazorComponents()
                .AddInteractiveWebAssemblyComponents();
            var app = builder.Build();
            app.MapRazorComponents<App>()
                .AddInteractiveWebAssemblyRenderMode()
                .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);
            """);

        Assert.AreEqual(2, Single(review, "services-not-registered").Line);
    }

    [TestMethod]
    public void Registration_ASharedExtensionMethod_OrTheRegistrationItself_IsFine()
    {
        None(Review("builder.Services.AddRazorComponents().AddInteractiveWebAssemblyComponents();\nbuilder.Services.AddAppServices();"), "services-not-registered");
        None(Review("builder.Services.AddRazorComponents().AddInteractiveWebAssemblyComponents();\nbuilder.Services.AddBitBmageServices();"), "services-not-registered");
        None(Review("var builder = WebAssemblyHostBuilder.CreateDefault(args);\nbuilder.Services.AddBitBmageServices();"), "services-not-registered");

        // This demo's own host registers through AddDemoServices.
        None(BmageCodeReview.Review(BmageSourceCatalog.GetSourceFile("Demo/Server/Program.cs")), "services-not-registered");
    }

    // -- aot ----------------------------------------------------------------------------------

    [TestMethod]
    public void Aot_AWebAssemblyProjectWithBmageAndNoAot_IsAWarning()
    {
        var review = Review("""
            <Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">
              <ItemGroup>
                <PackageReference Include="Bit.Bmage.Editor" Version="10.0.0" />
              </ItemGroup>
            </Project>
            """);

        Assert.AreEqual(1, Single(review, "aot-not-enabled").Line);

        Single(Review("<Project Sdk=\"Microsoft.NET.Sdk.BlazorWebAssembly\">\n<PackageReference Include=\"Bit.Bmage\" />\n<RunAOTCompilation>false</RunAOTCompilation>"), "aot-not-enabled");
    }

    [TestMethod]
    public void Aot_TheDemosOwnClientProject_IsFine()
    {
        var csproj = BmageSourceCatalog.GetSourceFile("Demo/Client/Bit.Bmage.Demo.Client.csproj");

        Assert.IsNotNull(csproj, "The client project file is not embedded.");
        None(BmageCodeReview.Review(csproj), "aot-not-enabled");

        // A server project is not compiled to WebAssembly at all.
        None(Review("<Project Sdk=\"Microsoft.NET.Sdk.Web\">\n<PackageReference Include=\"Bit.Bmage\" />"), "aot-not-enabled");
    }

    // -- formats ------------------------------------------------------------------------------

    [TestMethod]
    public void Formats_WebPInTheBrowserWithNoCheck_IsAWarning()
    {
        var review = Review("""
            @inject ImageExporter Exporter
            var encoded = await Exporter.EncodeAsync(image, new ImageExportOptions { Format = ImageFormat.WebP });
            """);

        Assert.AreEqual(2, Single(review, "format-without-capability-check").Line);

        None(Review("@inject ImageExporter Exporter\nawait Exporter.EncodeWithFallbackAsync(image, [ImageFormat.WebP, ImageFormat.Jpeg]);"), "format-without-capability-check");
        None(Review("@inject ImageExporter Exporter\nif (caps.CanEncode(ImageFormat.Avif)) await Exporter.EncodeAsync(image, ImageExportOptions.Avif);"), "format-without-capability-check");
        None(Review("@inject ImageExporter Exporter\ntry { await Exporter.EncodeAsync(image, ImageExportOptions.Avif); }\ncatch (ImageCapabilityException) { _fallback = true; }"), "format-without-capability-check");

        // A sentence about "@inject ImageProcessor" is prose, not a directive.
        None(Review("<p>Do not @inject ImageProcessor in a browser app.</p>"), "processor-injected-in-browser");
    }

    [TestMethod]
    public void Formats_WebPOnAServer_IsAnError()
    {
        var review = Review("""
            app.MapPost("/thumb", async (IFormFile file, ImageProcessor processor) =>
            {
                var result = await processor.ProcessAsync(bytes, pipeline, new ImageExportOptions { Format = ImageFormat.WebP });
            });
            """);

        var finding = Single(review, "browser-format-on-server");

        Assert.AreEqual("Error", finding.Severity);
        Assert.AreEqual(3, finding.Line);

        None(Review("app.MapPost(\"/thumb\", async (IFormFile file, ImageProcessor processor) => await processor.ProcessAsync(bytes, pipeline, ImageExportOptions.Jpeg));"), "browser-format-on-server");
    }

    // -- privacy ------------------------------------------------------------------------------

    [TestMethod]
    public void Privacy_Preserve_IsAWarning_UnlessTheLocationIsDroppedFirst()
    {
        Assert.AreEqual("Warning", Single(Review("var options = new ImageExportOptions { Metadata = MetadataPolicy.Preserve };"), "metadata-preserve-keeps-location").Severity);

        None(Review("var options = new ImageExportOptions { Metadata = MetadataPolicy.StripSensitive };"), "metadata-preserve-keeps-location");
        None(Review("image.Metadata = image.Metadata.WithExif(exif.WithoutLocation());\nvar options = new ImageExportOptions { Metadata = MetadataPolicy.Preserve };"), "metadata-preserve-keeps-location");
    }

    [TestMethod]
    public void Privacy_AutoOrientOffWithNoRotationStep_IsAWarning()
    {
        Single(Review("var image = await Loader.LoadAsync(file, new DecodeOptions { AutoOrient = false });"), "autoorient-off-without-rotation");

        None(Review("using var image = await Loader.LoadAsync(file, new DecodeOptions { AutoOrient = false });\nusing var upright = await ImagePipeline.Create().AutoOrient().ExecuteAsync(image);"), "autoorient-off-without-rotation");
    }

    [TestMethod]
    public void Privacy_BlurredRedaction_IsASuggestion()
    {
        Assert.AreEqual("Suggestion", Single(Review("new RedactionAnnotation { Rectangle = r, Mode = RedactionMode.Pixelate }"), "redaction-not-solid").Severity);

        None(Review("new RedactionAnnotation { Rectangle = r, Mode = RedactionMode.Solid }"), "redaction-not-solid");
    }

    [TestMethod]
    public void Privacy_AcceptingSvg_IsAWarning()
    {
        Single(Review("<InputFile OnChange=\"OnChange\" accept=\"image/png, image/svg+xml\" />"), "svg-input");

        None(Review("<InputFile OnChange=\"OnChange\" accept=\"image/*\" />"), "svg-input");
    }

    // -- performance and robustness -----------------------------------------------------------

    [TestMethod]
    public void Pipeline_PerPixelWorkBeforeTheDownscale_IsASuggestion()
    {
        var review = Review("""
            var pipeline = ImagePipeline.Create()
                .AutoOrient()
                .Brightness(0.1f)
                .MaxSize(1920, 1920);
            """);

        var finding = Single(review, "resize-late-in-pipeline");

        Assert.AreEqual(3, finding.Line);
        StringAssert.Contains(finding.Message, "Brightness");

        None(Review("var pipeline = ImagePipeline.Create().AutoOrient().MaxSize(1920, 1920).Brightness(0.1f).Sharpen(0.3f);"), "resize-late-in-pipeline");
        None(Review("var pipeline = ImagePipeline.Create().SmartCrop(AspectRatio.Square).Resize(256, 256);"), "resize-late-in-pipeline");
    }

    [TestMethod]
    public void Budget_MaxFileSizeWithNoHandling_IsASuggestion()
    {
        Single(Review("await Exporter.EncodeAsync(image, new ImageExportOptions { MaxFileSize = 100 * 1024 });"), "size-budget-unhandled");

        None(Review("try { await Exporter.EncodeAsync(image, new ImageExportOptions { MaxFileSize = 100 * 1024 }); }\ncatch (ImageEncodeException) { }"), "size-budget-unhandled");
        None(Review("new ImageExportOptions { MaxFileSize = null }"), "size-budget-unhandled");
    }

    [TestMethod]
    public void Urls_ADataUrlAndAnUnreleasedObjectUrl_AreSuggestions()
    {
        Single(Review("_src = encoded.ToDataUrl();"), "data-url-preview");

        Single(Review("_src = await Exporter.CreateObjectUrlAsync(encoded);"), "object-url-not-released");
        None(Review("await Exporter.ReleaseObjectUrlAsync(_src);\n_src = await Exporter.CreateObjectUrlAsync(encoded);"), "object-url-not-released");
    }

    [TestMethod]
    public void Slider_OnChangeAndOnInputOnOneInput_IsAWarning()
    {
        var review = Review("""
            <p>Quality</p>
            <input type="range" min="0" max="1" step="0.01"
                   @oninput="OnInput"
                   @onchange="OnCommit" />
            """);

        Assert.AreEqual(2, Single(review, "slider-onchange-with-oninput").Line);

        None(Review("<input type=\"range\" @oninput=\"OnInput\" />"), "slider-onchange-with-oninput");
    }

    [TestMethod]
    public void BrowserFile_OpenReadStreamWithNoLimit_IsAWarning()
    {
        Single(Review("foreach (var file in e.GetMultipleFiles())\n{\n    await using var stream = file.OpenReadStream();\n}"), "browser-file-default-limit");

        None(Review("foreach (var file in e.GetMultipleFiles())\n{\n    await using var stream = file.OpenReadStream(maxAllowedSize: 32 * 1024 * 1024);\n}"), "browser-file-default-limit");

        // IFormFile.OpenReadStream takes no limit at all; this rule is about IBrowserFile.
        None(Review("app.MapPost(\"/x\", (IFormFile file) => file.OpenReadStream());"), "browser-file-default-limit");
    }

    [TestMethod]
    public void Batch_HighConcurrencyInTheBrowser_IsASuggestion()
    {
        Single(Review("@inject ImageProcessorFactory Processors\nnew BatchOptions { MaxConcurrency = 8 }"), "batch-concurrency-high");

        None(Review("@inject ImageProcessorFactory Processors\nnew BatchOptions { MaxConcurrency = 2 }"), "batch-concurrency-high");
        None(Review("var processor = new ImageProcessor();\nnew BatchOptions { MaxConcurrency = 8 }"), "batch-concurrency-high");
    }

    [TestMethod]
    public void Server_DecodingAnUploadWithoutIdentify_IsASuggestion()
    {
        Single(Review("app.MapPost(\"/x\", async (IFormFile file, ImageProcessor processor) => await processor.ProcessAsync(bytes, pipeline));"), "identify-before-decode");

        None(Review("app.MapPost(\"/x\", async (IFormFile file, ImageProcessor processor) =>\n{\n    if (processor.Identify(bytes) is null) return;\n    await processor.ProcessAsync(bytes, pipeline);\n});"), "identify-before-decode");
    }

    // -- the review as a whole ----------------------------------------------------------------

    [TestMethod]
    public void Review_EmptyCode_Passes_AndSaysWhatItChecked()
    {
        var review = BmageCodeReview.Review("   ");

        Assert.IsTrue(review.Passed);
        Assert.IsEmpty(review.Findings);
        CollectionAssert.AreEqual(BmageCodeReview.Rules, review.RulesApplied);
    }

    [TestMethod]
    public void Review_FindingsAreOrderedBySeverityThenLine_AndOnlySuggestionsStillPass()
    {
        var review = Review("""
            _src = encoded.ToDataUrl();
            var options = new ImageExportOptions { Metadata = MetadataPolicy.Preserve };
            @inject ImageProcessor Processor
            """);

        CollectionAssert.AreEqual(new[] { "Error", "Warning", "Suggestion" }, review.Findings.Select(finding => finding.Severity).ToArray());
        Assert.IsFalse(review.Passed);

        Assert.IsTrue(Review("_src = encoded.ToDataUrl();").Passed, "A suggestion alone failed the review.");
    }

    [TestMethod]
    public void Catalog_EveryRuleIsUnique_AndNamesAGuideThatExists()
    {
        CollectionAssert.AllItemsAreUnique(BmageCodeReview.Rules);

        foreach (var rule in BmageCodeReview.Catalog)
        {
            Assert.IsNotNull(BmageSourceCatalog.ResolveDocumentName(rule.Source), $"'{rule.Id}' cites a guide that does not exist: '{rule.Source}'.");
            CollectionAssert.Contains(new[] { "Error", "Warning", "Suggestion" }, rule.Severity);
        }
    }

    /// <summary>
    /// Every rule fires somewhere in this suite: a rule nothing exercises can rot without anyone
    /// noticing, and the catalog would go on listing it.
    /// </summary>
    [TestMethod]
    public void Catalog_EveryRuleIsExercisedByThisSuite()
    {
        var source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Services", "CodeReviewTests.cs"));

        foreach (var rule in BmageCodeReview.Rules)
        {
            StringAssert.Contains(source, $"\"{rule}\"", $"No test exercises '{rule}'.");
        }
    }

    /// <summary>
    /// The demo pages are the worked examples the tools hand out. A rule that calls one of them an
    /// Error is either a false positive or a real bug in the example - both worth knowing first.
    /// </summary>
    [TestMethod]
    public void Review_TheDemosOwnSources_HaveNoErrors()
    {
        foreach (var file in BmageSourceCatalog.SourceFiles)
        {
            // The MCP page quotes broken code on purpose, as the sample its review panel starts with.
            if (file.Path.EndsWith("McpServerPage.razor", StringComparison.Ordinal)) continue;

            var errors = BmageCodeReview.Review(BmageSourceCatalog.GetSourceFile(file.Path)).Findings.Where(finding => finding.Severity == "Error").ToArray();

            Assert.IsEmpty(errors, $"{file.Path}: {string.Join(" | ", errors.Select(error => $"{error.Rule} at {error.Line}"))}");
        }
    }

    /// <summary>The recipes are handed out as correct code; the review must agree.</summary>
    [TestMethod]
    public void Review_EveryRecipe_PassesWithoutAnErrorOrAWarning()
    {
        foreach (var recipe in BmageRecipeCatalog.All)
        {
            var problems = BmageCodeReview.Review(recipe.Code).Findings.Where(finding => finding.Severity != "Suggestion").ToArray();

            Assert.IsEmpty(problems, $"{recipe.Id}: {string.Join(" | ", problems.Select(problem => $"{problem.Rule} at {problem.Line}"))}");
        }
    }

    private static BmageReviewDto Review(string code) => BmageCodeReview.Review(code);

    private static BmageReviewFindingDto Single(BmageReviewDto review, string rule)
    {
        var findings = review.Findings.Where(finding => finding.Rule == rule).ToArray();

        Assert.HasCount(1, findings, $"Expected one '{rule}' finding; got: {string.Join(" | ", review.Findings.Select(finding => $"{finding.Rule}@{finding.Line}"))}");

        return findings[0];
    }

    private static void None(BmageReviewDto review, string rule)
    {
        var findings = review.Findings.Where(finding => finding.Rule == rule).ToArray();

        Assert.IsEmpty(findings, $"'{rule}' fired on correct code: {string.Join(" | ", findings.Select(finding => finding.Message))}");
    }
}

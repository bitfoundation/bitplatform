namespace Bit.Bmage.Tests.Mcp.Services;

/// <summary>
/// The reflected API reference: all three packages, every type with the namespace and package an
/// agent needs to use it, and the defaults read off real instances - because for an options record
/// the defaults are the behaviour.
/// </summary>
[TestClass]
public class ApiCatalogTests
{
    [TestMethod]
    public void Types_ComeFromAllThreePackages()
    {
        var packages = BmageApiCatalog.Types.Select(type => type.Package).Distinct().Order().ToArray();

        CollectionAssert.AreEqual(new[] { "Bit.Bmage", "Bit.Bmage.Core", "Bit.Bmage.Editor" }, packages);

        foreach (var (name, package, ns) in new[]
        {
            ("ImagePipeline", "Bit.Bmage.Core", "Bit.Bmage.Pipeline"),
            ("ImageBuffer", "Bit.Bmage.Core", "Bit.Bmage"),
            ("ImageExportOptions", "Bit.Bmage.Core", "Bit.Bmage.Codecs"),
            ("RedactionAnnotation", "Bit.Bmage.Core", "Bit.Bmage.Annotations"),
            ("ImageLoader", "Bit.Bmage", "Bit.Bmage.Input"),
            ("ImageExporter", "Bit.Bmage", "Bit.Bmage.Output"),
            ("BitBmage", "Bit.Bmage", "Bit.Bmage"),
            ("BmageEditor", "Bit.Bmage.Editor", "Bit.Bmage.Editor.Components"),
            ("BmageEditorStrings", "Bit.Bmage.Editor", "Bit.Bmage.Editor.Localization"),
        })
        {
            var type = BmageApiCatalog.Types.SingleOrDefault(entry => entry.Name == name);

            Assert.IsNotNull(type, $"{name} is missing from the catalog.");
            Assert.AreEqual(package, type.Package, name);
            Assert.AreEqual(ns, type.Namespace, name);
        }
    }

    [TestMethod]
    public void Types_TheBuildArtefactsAreLeftOut()
    {
        Assert.IsFalse(BmageApiCatalog.Types.Any(type => type.Name == "_Imports"));
        // Compiler-generated types ("<PrivateImplementationDetails>") are not API; a generic type's friendly name may contain '<'.
        Assert.IsFalse(BmageApiCatalog.Types.Any(type => type.Name.StartsWith('<')));
    }

    [TestMethod]
    public void Kinds_AreToldApart()
    {
        string KindOf(string name) => BmageApiCatalog.Types.Single(type => type.Name == name).Kind;

        Assert.AreEqual("Component", KindOf("BmageEditor"));
        Assert.AreEqual("Enum", KindOf("MetadataPolicy"));
        Assert.AreEqual("Record", KindOf("ImageExportOptions"));
        Assert.AreEqual("Static class", KindOf("BitBmage"));
        Assert.AreEqual("Struct", KindOf("Rgba32"));
        Assert.AreEqual("Interface", KindOf("IImageOperation"));
        Assert.AreEqual("Exception", KindOf("ImageEncodeException"));
        Assert.AreEqual("Class", KindOf("ImagePipeline"));
    }

    /// <summary>The defaults are read off a freshly constructed instance, so they are the ones a caller really gets.</summary>
    [TestMethod]
    public void Defaults_AreTheOnesAFreshInstanceHas()
    {
        var options = BmageApiCatalog.GetTypeDetails("ImageExportOptions")!;

        string? DefaultOf(string member) => options.Members.Single(entry => entry.Name == member).Default;

        Assert.AreEqual("ImageFormat.Png", DefaultOf("Format"));
        Assert.AreEqual("0.85", DefaultOf("Quality"));
        Assert.AreEqual("MetadataPolicy.Remove", DefaultOf("Metadata"));
        Assert.AreEqual("Rgba32.White", DefaultOf("Background"));
        Assert.AreEqual("6", DefaultOf("PngCompressionLevel"));
        Assert.AreEqual("MaxFileSizeStrategy.QualityThenDimensions", DefaultOf("MaxFileSizeStrategy"));

        // Presets are named, not printed as "new ImageExportOptions()".
        Assert.IsNull(DefaultOf("Png"));
    }

    [TestMethod]
    public void Components_ListTheirParameters_AsParameters()
    {
        var editor = BmageApiCatalog.GetTypeDetails("BmageEditor")!;

        var image = editor.Members.Single(member => member.Name == "Image");
        Assert.AreEqual("Parameter", image.Kind);
        Assert.AreEqual("ImageBuffer", image.Type);

        Assert.AreEqual("100", editor.Members.Single(member => member.Name == "MaxHistoryDepth").Default);
        Assert.AreEqual("\"ltr\"", editor.Members.Single(member => member.Name == "Direction").Default);
        Assert.IsTrue(editor.Members.Any(member => member.Name == "LoadImageAsync" && member.Signature!.Contains("bool takeOwnership = true", StringComparison.Ordinal)));

        // What every component inherits from ComponentBase is not the editor's API.
        Assert.IsFalse(editor.Members.Any(member => member.Name is "SetParametersAsync" or "BuildRenderTree" or "OnInitializedAsync"));
    }

    [TestMethod]
    public void Methods_CarryTheirFullSignature_WithDefaults()
    {
        var pipeline = BmageApiCatalog.GetTypeDetails("ImagePipeline")!;

        var maxSize = pipeline.Members.Single(member => member.Name == "MaxSize");
        Assert.AreEqual("(int maxWidth, int maxHeight, ResamplingFilter filter = ResamplingFilter.Auto)", maxSize.Signature);
        Assert.AreEqual("ImagePipeline", maxSize.Type);

        var rotate = pipeline.Members.Single(member => member.Name == "Rotate");
        StringAssert.Contains(rotate.Signature, "Rgba32 background = default");

        // Extension methods say so.
        var registration = BmageApiCatalog.GetTypeDetails("BitBmage")!.Members.First(member => member.Name == "AddBitBmageServices");
        StringAssert.StartsWith(registration.Signature, "(this IServiceCollection services");
    }

    [TestMethod]
    public void Constructors_AreListed_ButNotARecordsCopyConstructor()
    {
        var crop = BmageApiCatalog.GetTypeDetails("CropOperation")!;

        var constructors = crop.Members.Where(member => member.Kind == "Constructor").ToArray();
        Assert.IsGreaterThanOrEqualTo(1, constructors.Length);

        var options = BmageApiCatalog.GetTypeDetails("ImageExportOptions")!;
        Assert.IsFalse(options.Members.Any(member => member.Kind == "Constructor" && member.Signature == "(ImageExportOptions original)"));
        Assert.IsFalse(options.Members.Any(member => member.Name is "EqualityContract" or "Deconstruct" or "PrintMembers"));
    }

    [TestMethod]
    public void Enums_ListTheirValues_WithTheirDocumentation()
    {
        var policy = BmageApiCatalog.GetTypeDetails("MetadataPolicy")!;

        CollectionAssert.AreEqual(new[] { "Preserve", "Remove", "StripSensitive" }, policy.Members.Select(member => member.Name).Order().ToArray());

        var strip = policy.Members.Single(member => member.Name == "StripSensitive");
        StringAssert.Contains(strip.Summary, "GPS");
    }

    [TestMethod]
    public void Lookup_ByFullNameMemberAndCaseInsensitively()
    {
        Assert.AreEqual("ImageExporter", BmageApiCatalog.GetTypeDetails("Bit.Bmage.Output.ImageExporter")?.Name);
        Assert.AreEqual("ImageExporter", BmageApiCatalog.GetTypeDetails("imageexporter")?.Name);
        Assert.IsNull(BmageApiCatalog.GetTypeDetails("Teleporter"));

        var member = BmageApiCatalog.GetMemberDetails("ImageExportOptions.MaxFileSize")!;
        Assert.AreEqual("MaxFileSize", member.Members.Single().Name);

        Assert.IsNull(BmageApiCatalog.GetMemberDetails("ImageExportOptions.Teleport"));
        Assert.IsNull(BmageApiCatalog.GetMemberDetails("NoSuchType.Member"));
    }

    [TestMethod]
    [DataRow("A pipeline. It runs later.", "A pipeline.")]
    [DataRow("Crops (e.g. a square) around the subject. More.", "Crops (e.g. a square) around the subject.")]
    [DataRow("One line only", "One line only")]
    [DataRow("First paragraph.\n\nSecond paragraph.", "First paragraph.")]
    public void FirstSentence_CutsAtTheRealSentenceEnd(string summary, string expected)
    {
        Assert.AreEqual(expected, BmageApiCatalog.FirstSentence(summary));
    }

    [TestMethod]
    public void TypeSummaries_AreOneSentenceEach()
    {
        var pipeline = BmageApiCatalog.TypeSummaries.Single(type => type.Name == "ImagePipeline");

        Assert.AreEqual("An immutable, ordered list of operations describing a complete image transformation.", pipeline.Summary);
    }
}

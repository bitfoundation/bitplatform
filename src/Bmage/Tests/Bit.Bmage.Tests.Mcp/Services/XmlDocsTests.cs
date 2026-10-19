namespace Bit.Bmage.Tests.Mcp.Services;

/// <summary>
/// The XML documentation of all three packages, flattened into the prose an agent reads. A file that
/// is not next to the assembly - the publish dropped it, the build stopped emitting it - would leave
/// every summary empty without failing anything, so its presence is a test of its own.
/// </summary>
[TestClass]
public class XmlDocsTests
{
    [TestMethod]
    public void TheDocumentationOfAllThreePackages_IsLoaded()
    {
        foreach (var name in BmageXmlDocs.AssemblyNames)
        {
            Assert.IsTrue(File.Exists(Path.Combine(AppContext.BaseDirectory, $"{name}.xml")), $"{name}.xml is not next to the assembly.");
        }

        Assert.IsGreaterThan(500, BmageXmlDocs.Count);

        Assert.IsNotNull(BmageXmlDocs.GetSummary("T:Bit.Bmage.Pipeline.ImagePipeline"), "Bit.Bmage.Core's documentation is missing.");
        Assert.IsNotNull(BmageXmlDocs.GetSummary("T:Bit.Bmage.Output.ImageExporter"), "Bit.Bmage's documentation is missing.");
        Assert.IsNotNull(BmageXmlDocs.GetSummary("P:Bit.Bmage.Editor.Components.BmageEditor.Image"), "Bit.Bmage.Editor's documentation is missing.");
    }

    [TestMethod]
    public void Crefs_BecomeShortNames_AndProseIsUnwrapped()
    {
        var summary = BmageXmlDocs.GetSummary("T:Bit.Bmage.Pipeline.ImagePipeline")!;

        StringAssert.Contains(summary, "ImagePipeline.ExecuteAsync");
        Assert.IsFalse(summary.Contains("M:", StringComparison.Ordinal), summary);
        Assert.IsFalse(summary.Contains('\n'), "A single paragraph kept the source's line wrapping.");
    }

    [TestMethod]
    public void CodeSamples_KeepTheirLines_AndLoseTheCommentsIndentation()
    {
        var example = BmageXmlDocs.GetSummary("M:Bit.Bmage.BitBmage.AddBitBmageServices(Microsoft.Extensions.DependencyInjection.IServiceCollection,System.Action{Bit.Bmage.BitBmageOptions})");

        // The <example> is not part of the summary; the remarks are, and they are paragraphs.
        Assert.IsNotNull(example);

        var remarks = BmageXmlDocs.GetRemarks("M:Bit.Bmage.BitBmage.AddBitBmageServices(Microsoft.Extensions.DependencyInjection.IServiceCollection,System.Action{Bit.Bmage.BitBmageOptions})")!;

        StringAssert.Contains(remarks, "\n\n", "The remarks' paragraphs ran together.");
        StringAssert.Contains(remarks, "Interactive Server");
    }

    [TestMethod]
    public void AnInexactMethodId_StillFindsAnOverload()
    {
        Assert.IsNotNull(BmageXmlDocs.GetSummary("M:Bit.Bmage.Pipeline.ImagePipeline.Crop"));
    }

    [TestMethod]
    public void AnUnknownId_IsNull_NotAnException()
    {
        Assert.IsNull(BmageXmlDocs.GetSummary("T:Bit.Bmage.Teleporter"));
        Assert.IsNull(BmageXmlDocs.GetRemarks("T:Bit.Bmage.Pipeline.ImagePipeline.Nothing"));
    }
}

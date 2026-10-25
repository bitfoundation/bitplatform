namespace Bit.Bmage.Tests.Mcp.Services;

/// <summary>
/// The one search: a request phrased the way it arrives finds the thing that answers it, near the
/// top, with the call that returns it in full.
/// </summary>
[TestClass]
public class SearchIndexTests
{
    [TestMethod]
    [DataRow("crop an avatar to a circle under 100 KB", "GetBmageRecipe(id: \"avatar-crop\")")]
    [DataRow("strip gps location from uploads", "GetBmageRecipe(id: \"strip-location\")")]
    [DataRow("paste a screenshot and redact it", "GetBmageRecipe(id: \"annotate-redact-screenshot\")")]
    [DataRow("thumbnail endpoint on the server", "GetBmageRecipe(id: \"headless-upload-endpoint\")")]
    [DataRow("MaxFileSize", "GetBmageApiDetails(typeName: \"ImageExportOptions.MaxFileSize\")")]
    [DataRow("ImagePipeline", "GetBmageApiDetails(typeName: \"ImagePipeline\")")]
    [DataRow("the optimiser fuses adjustments", "GetBmageDocument(name: \"pipelines\", section: \"The optimiser\")")]
    [DataRow("prerendering", "GetBmageDocument(name: \"getting-started\", section: \"When things run\")")]
    [DataRow("browser call during prerender", "ReviewBmageCode(code: \"...\")")]
    [DataRow("how many bytes at quality 0.8", "RunBmagePipeline(pipeline: \"...\")")]
    [DataRow("headless console registration", "GetBmageSetupGuide(renderMode: \"headless\")")]
    public void Search_FindsTheAnswer_NearTheTop(string query, string expected)
    {
        var hits = BmageSearchIndex.Search(query, 8);

        CollectionAssert.Contains(hits.Select(hit => hit.Tool).ToArray(), expected,
                                  $"'{query}' found: {string.Join(" | ", hits.Select(hit => $"{hit.Kind}: {hit.Title}"))}");
    }

    [TestMethod]
    public void Search_AnExactTypeName_IsTheFirstHit()
    {
        Assert.AreEqual("ImageExporter", BmageSearchIndex.Search("ImageExporter", 5)[0].Title);
    }

    [TestMethod]
    public void Search_EveryHit_CarriesItsKindTitleCallAndSnippet()
    {
        foreach (var hit in BmageSearchIndex.Search("webp fallback", 20))
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(hit.Kind));
            Assert.IsFalse(string.IsNullOrWhiteSpace(hit.Title));
            Assert.IsFalse(string.IsNullOrWhiteSpace(hit.Tool));
            Assert.IsLessThanOrEqualTo(246, hit.Snippet.Length);
        }
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("how why the")]
    [DataRow("bmage image")]
    public void Search_AQueryWithNothingToMatch_FindsNothing(string query)
    {
        Assert.IsEmpty(BmageSearchIndex.Search(query, 10));
    }

    [TestMethod]
    public void Search_TheLimit_IsHonouredAndClamped()
    {
        Assert.HasCount(3, BmageSearchIndex.Search("crop", 3));
        Assert.HasCount(1, BmageSearchIndex.Search("crop", 0));
        Assert.HasCount(50, BmageSearchIndex.Search("image pipeline operation", 500));
    }
}

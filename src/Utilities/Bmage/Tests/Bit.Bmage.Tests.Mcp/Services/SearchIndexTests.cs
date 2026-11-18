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
    [DataRow("MaxFileSize", "GetBmageApiDetails(typeName: \"BmageImageExportOptions.MaxFileSize\")")]
    [DataRow("BmageImagePipeline", "GetBmageApiDetails(typeName: \"BmageImagePipeline\")")]
    [DataRow("the optimiser fuses adjustments", "GetBmageDocument(name: \"pipelines\", section: \"The optimiser\")")]
    [DataRow("prerendering", "GetBmageDocument(name: \"browser-support\", section: \"Prerendering\")")]
    [DataRow("browser call during prerender", "ReviewBmageCode(code: \"...\")")]
    [DataRow("how many bytes at quality 0.8", "RunBmagePipeline(pipeline: \"...\")")]
    [DataRow("headless console registration", "GetBmageSetupGuide(renderMode: \"headless\")")]
    public void Search_FindsTheAnswer_NearTheTop(string query, string expected)
    {
        var hits = BmageSearchIndex.Search(query, 8).Hits;

        CollectionAssert.Contains(hits.Select(hit => hit.Tool).ToArray(), expected,
                                  $"'{query}' found: {string.Join(" | ", hits.Select(hit => $"{hit.Kind}: {hit.Title}"))}");
    }

    /// <summary>
    /// A request that joins two things finds the one it is mostly about first. "Shrink a photo without its
    /// location" is a shrink with a privacy rider: the recipe that strips the location and never shrinks
    /// anything must not lead, nor anything that merely has "without" or "under" in its name or prose. The
    /// other way round, a request to strip the location from uploads is the location recipe's.
    /// </summary>
    [TestMethod]
    [DataRow("shrink an uploaded photo under 200 KB without its location", "GetBmageRecipe(id: \"upload-optimise\")")]
    [DataRow("shrink a photo before uploading it", "GetBmageRecipe(id: \"upload-optimise\")")]
    [DataRow("compress photos before they are uploaded", "GetBmageRecipe(id: \"upload-optimise\")")]
    [DataRow("strip the location from uploads", "GetBmageRecipe(id: \"strip-location\")")]
    public void Search_TheMainPartOfTheRequest_IsTheFirstHit(string query, string expected)
    {
        var hits = BmageSearchIndex.Search(query, 8).Hits;

        Assert.AreEqual(expected, hits.FirstOrDefault()?.Tool, $"'{query}' found: {string.Join(" | ", hits.Select(hit => $"{hit.Kind}: {hit.Title}"))}");
    }

    /// <summary>The rider of such a request stays in view too: an agent asked for both needs both.</summary>
    [TestMethod]
    public void Search_TheSecondPartOfTheRequest_StaysInView()
    {
        var hits = BmageSearchIndex.Search("shrink an uploaded photo under 200 KB without its location", 8).Hits;

        CollectionAssert.Contains(hits.Select(hit => hit.Tool).ToArray(), "GetBmageRecipe(id: \"strip-location\")",
                                  string.Join(" | ", hits.Select(hit => $"{hit.Kind}: {hit.Title}")));
    }

    [TestMethod]
    public void Search_AnExactTypeName_IsTheFirstHit()
    {
        Assert.AreEqual("BmageImageExporter", BmageSearchIndex.Search("BmageImageExporter", 5).Hits.FirstOrDefault()?.Title);
    }

    /// <summary>
    /// A member named like a word a sentence is phrased in is still found by its name: "without" is
    /// filler in "shrink a photo without its location", and the name of BmageExifDirectory.Without. Asked
    /// for alone, it shares the name with every heading phrased "... without ...", so it is among them;
    /// asked for with its type, it leads.
    /// </summary>
    [TestMethod]
    public void Search_AMemberNamedLikeAStopWord_IsFoundByItsName()
    {
        var hits = BmageSearchIndex.Search("Without", 8).Hits;

        CollectionAssert.Contains(hits.Select(hit => hit.Title).ToArray(), "BmageExifDirectory.Without", string.Join(" | ", hits.Select(hit => hit.Title)));
    }

    [TestMethod]
    [DataRow("BmageExifDirectory Without")]
    [DataRow("BmageExifDirectory.Without")]
    public void Search_AMemberNamedLikeAStopWord_WithItsType_IsTheFirstHit(string query)
    {
        var hits = BmageSearchIndex.Search(query, 8).Hits;

        Assert.AreEqual("BmageExifDirectory.Without", hits.FirstOrDefault()?.Title, string.Join(" | ", hits.Select(hit => $"{hit.Kind}: {hit.Title}")));
    }

    /// <summary>"bit" is not a word true of everything here: it is the bit depth the guides are about.</summary>
    [TestMethod]
    [DataRow("8-bit")]
    [DataRow("16 bit")]
    public void Search_ABitDepth_IsSearched(string query)
    {
        Assert.IsNotEmpty(BmageSearchIndex.Search(query, 8).Hits, query);
    }

    /// <summary>
    /// A member inherited with the same documentation is one hit, not one per type - yet the copy on the
    /// type a query names is the one it finds, so every type's member stays reachable.
    /// </summary>
    [TestMethod]
    public void Search_AnInheritedMember_IsOneHit_AndTheNamedTypesCopyIsFound()
    {
        var owners = BmageApiCatalog.Types
            .Where(type => BmageApiCatalog.GetTypeDetails(type.Name)?.Members.Any(member => member.Name == "TryFuse") is true)
            .Select(type => type.Name)
            .ToArray();

        Assert.IsGreaterThan(2, owners.Length, string.Join(", ", owners));

        var copies = BmageSearchIndex.Search("TryFuse", 50).Hits.Where(hit => hit.Title.EndsWith(".TryFuse", StringComparison.Ordinal)).ToArray();

        Assert.IsNotEmpty(copies);
        Assert.IsLessThan(owners.Length, copies.Length, string.Join(" | ", copies.Select(hit => hit.Title)));

        foreach (var owner in owners)
        {
            var hits = BmageSearchIndex.Search($"{owner} TryFuse", 8).Hits;

            Assert.AreEqual($"{owner}.TryFuse", hits.FirstOrDefault()?.Title, string.Join(" | ", hits.Select(hit => hit.Title)));
        }
    }

    /// <summary>
    /// A search that finds nothing says why and what came closest: an empty list is what an agent reads
    /// as "Bmage cannot do this".
    /// </summary>
    [TestMethod]
    public void Search_NothingMatched_SaysSo_AndNamesTheClosestMatches()
    {
        var result = BmageSearchIndex.Search("croppability", 8);

        Assert.IsEmpty(result.Hits);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Message));
        Assert.IsNotNull(result.DidYouMean);
        Assert.IsTrue(result.DidYouMean.Any(title => title.Contains("Crop", StringComparison.OrdinalIgnoreCase)), string.Join(" | ", result.DidYouMean));
    }

    [TestMethod]
    public void Search_TheRankingCutShort_SaysThereIsMore()
    {
        Assert.IsTrue(BmageSearchIndex.Search("crop", 3).HasMore);
    }

    [TestMethod]
    public void Search_EveryHit_CarriesItsKindTitleCallAndSnippet()
    {
        foreach (var hit in BmageSearchIndex.Search("webp fallback", 20).Hits)
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
        var result = BmageSearchIndex.Search(query, 10);

        Assert.IsEmpty(result.Hits);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Message), "Nothing says why there is no answer.");
    }

    [TestMethod]
    public void Search_TheLimit_IsHonouredAndClamped()
    {
        Assert.HasCount(3, BmageSearchIndex.Search("crop", 3).Hits);
        Assert.HasCount(1, BmageSearchIndex.Search("crop", 0).Hits);
        Assert.HasCount(50, BmageSearchIndex.Search("image pipeline operation", 500).Hits);
    }

    /// <summary>
    /// A build of the index that fails once - a catalog that could not load on the first request - is
    /// tried again by the next search, rather than answering every later one with the same exception.
    /// </summary>
    [TestMethod]
    public void Search_AfterTheIndexFailedToBuild_BuildsItAgain()
    {
        var builds = 0;

        var search = new Bit.Utilities.Mcp.McpSearch(() => ++builds == 1
            ? throw new InvalidOperationException("The catalog could not load.")
            : [new Bit.Utilities.Mcp.McpSearchEntry("Guide section", "Cropping", null, "GetBmageDocument(name: \"crop\")", "Crop an image.", string.Empty)],
            ambientWords: []);

        Assert.ThrowsExactly<InvalidOperationException>(() => search.Search("crop", 5));

        Assert.HasCount(1, search.Search("crop", 5).Hits);
        Assert.AreEqual(2, builds);
    }
}

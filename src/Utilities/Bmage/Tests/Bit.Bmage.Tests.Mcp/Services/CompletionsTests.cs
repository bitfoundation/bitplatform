namespace Bit.Bmage.Tests.Mcp.Services;

/// <summary>The values a client offers while an argument is being filled in.</summary>
[TestClass]
public class CompletionsTests
{
    [TestMethod]
    [DataRow("name", "pipe", "pipelines")]
    [DataRow("typeName", "ImagePipe", "ImagePipeline")]
    [DataRow("path", "Avatar", "Demo/Client/Pages/Avatar.razor")]
    [DataRow("id", "avatar", "avatar-crop")]
    [DataRow("renderMode", "stand", "standalone-wasm")]
    [DataRow("source", "scr", "screenshot")]
    [DataRow("format", "jp", "jpeg")]
    public void Complete_OffersTheValuesOfItsSet(string argument, string typed, string expected)
    {
        var completion = BmageCompletions.Complete(argument, typed);

        CollectionAssert.Contains(completion.Values.ToArray(), expected);
    }

    [TestMethod]
    public void Complete_PrefixesRankAboveSubstrings()
    {
        // CropOperation starts with it; AspectCropOperation, SmartCropOperation and ResizeCropOperation only contain it.
        var values = BmageCompletions.Complete("typeName", "Crop").Values;

        Assert.IsGreaterThan(1, values.Count);
        StringAssert.StartsWith(values[0], "Crop", "A substring match outranked a prefix match.");
        Assert.IsTrue(values.Any(value => value.StartsWith("Crop", StringComparison.Ordinal) is false), "Substring matches were left out.");
    }

    [TestMethod]
    public void Complete_TheFreeformArguments_AreStartedOff()
    {
        Assert.IsNotEmpty(BmageCompletions.Complete("symptom", "").Values);
        Assert.IsNotEmpty(BmageCompletions.Complete("request", "avatar").Values);
    }

    [TestMethod]
    public void Complete_AnArgumentItKnowsNothingAbout_IsEmpty_NotAnError()
    {
        Assert.IsEmpty(BmageCompletions.Complete("teleport", "x").Values);
        Assert.IsEmpty(BmageCompletions.Complete(null, null).Values);
    }

    [TestMethod]
    public void Complete_NeverReturnsMoreThanTheProtocolAllows()
    {
        var completion = BmageCompletions.Complete("typeName", "");

        Assert.IsLessThanOrEqualTo(100, completion.Values.Count);
        Assert.AreEqual(BmageApiCatalog.Types.Select(type => type.Name).Distinct().Count(), completion.Total);
        Assert.IsTrue(completion.HasMore);
    }
}

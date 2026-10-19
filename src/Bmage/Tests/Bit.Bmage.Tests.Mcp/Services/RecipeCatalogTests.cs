using System.Text.RegularExpressions;
using Bit.Bmage.Demo.Client.Shared;

namespace Bit.Bmage.Tests.Mcp.Services;

/// <summary>
/// The recipes are handed out as correct code, so the checks here are about whether they still are:
/// every type they name exists in this build, every demo page they point at exists, and the review
/// agrees with them (see CodeReviewTests).
/// </summary>
[TestClass]
public class RecipeCatalogTests
{
    [TestMethod]
    public void Recipes_CoverTheFeaturesPeopleAskFor()
    {
        foreach (var id in new[] { "thumbnail", "upload-optimise", "avatar-crop", "strip-location", "annotate-redact-screenshot",
                                   "batch-processing", "editor-basic", "editor-customise", "headless-upload-endpoint", "redact-on-server" })
        {
            Assert.IsNotNull(BmageRecipeCatalog.Find(id), $"There is no '{id}' recipe.");
        }
    }

    [TestMethod]
    public void Recipes_AreWellFormed()
    {
        CollectionAssert.AllItemsAreUnique(BmageRecipeCatalog.All.Select(recipe => recipe.Id).ToArray());

        foreach (var recipe in BmageRecipeCatalog.All)
        {
            Assert.IsTrue(Regex.IsMatch(recipe.Id, "^[a-z0-9]+(-[a-z0-9]+)*$"), $"'{recipe.Id}' is not kebab-case.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(recipe.Code), recipe.Id);
            Assert.IsFalse(string.IsNullOrWhiteSpace(recipe.Notes), recipe.Id);
            Assert.IsFalse(string.IsNullOrWhiteSpace(recipe.Keywords), recipe.Id);
            CollectionAssert.Contains(new[] { "Browser", "Server", "Anywhere" }, recipe.RunsIn, recipe.Id);
        }
    }

    [TestMethod]
    public void Recipes_PointAtDemoPagesThatExist()
    {
        foreach (var recipe in BmageRecipeCatalog.All.Where(recipe => recipe.SeeAlso is not null))
        {
            Assert.IsNotNull(NavItem.ByHref(recipe.SeeAlso!.TrimStart('/')), $"{recipe.Id} points at {recipe.SeeAlso}, which is not a demo page.");
        }
    }

    /// <summary>
    /// A Bmage type a recipe names has to exist in this build. A renamed type would otherwise be handed
    /// out, verbatim, as working code.
    /// </summary>
    [TestMethod]
    public void Recipes_NameOnlyTypesThatExist()
    {
        var known = BmageApiCatalog.Types.Select(type => type.Name).ToHashSet(StringComparer.Ordinal);

        // The Bmage-looking names in the code: Image*, Bmage*, *Annotation, *Options, *Exception, Rgba32 and friends.
        var pattern = new Regex(@"\b(?:Image\w+|Bmage\w+|\w+Annotation|Redaction\w+|Metadata\w+|Exif\w+|Batch\w+|MaxFileSizeStrategy|Resize\w+|Resampling\w+|AspectRatio|Anchor|Rgba32|ShapeStyle|StrokeStyle|TextStyle|TextAlignment|TextBaseline|EditorToolKind|DecodeOptions|DocumentRenderer|FontTextRasterizer|BrowserTextRasterizer|EncodedImage|SmartCrop)\b");

        foreach (var recipe in BmageRecipeCatalog.All)
        {
            var unknown = pattern.Matches(recipe.Code!)
                .Select(match => match.Value)
                .Where(name => known.Contains(name) is false)
                // ImageDocument.AddAnnotation is a method, not a type.
                .Where(name => name.StartsWith("Add", StringComparison.Ordinal) is false)
                .Distinct()
                .ToArray();

            Assert.IsEmpty(unknown, $"{recipe.Id} names types this build does not have: {string.Join(", ", unknown)}.");
        }
    }

    [TestMethod]
    public void Find_IgnoresCase_AndAnswersNullForTheUnknown()
    {
        Assert.AreEqual("thumbnail", BmageRecipeCatalog.Find(" Thumbnail ")?.Id);
        Assert.IsNull(BmageRecipeCatalog.Find("teleport"));
        Assert.IsNull(BmageRecipeCatalog.Find(null));
    }
}

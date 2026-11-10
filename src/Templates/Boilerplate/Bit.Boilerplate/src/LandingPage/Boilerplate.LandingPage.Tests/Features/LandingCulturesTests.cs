namespace Boilerplate.LandingPage.Tests.Features;

[TestClass]
public class LandingCulturesTests
{
    [TestMethod]
    [DataRow("", "/")]
    [DataRow("/", "/")]
    [DataRow("/compare", "/compare")]
    public void Localize_Should_LinkAnEmptyPathToTheRootInTheDefaultCulture(string path, string expected)
    {
        Assert.AreEqual(expected, LandingCultures.Localize(path, LandingCultures.Default));
    }

    [TestMethod]
    public void Localize_Should_LinkAnEmptyPathToTheCulturesRootInTheOtherCultures()
    {
        foreach (var (_, culture) in LandingCultures.Supported.Where(s => s.Culture.Name != LandingCultures.Default.Name))
        {
            Assert.AreEqual($"/{culture.Name}", LandingCultures.Localize("", culture));
            Assert.AreEqual($"/{culture.Name}", LandingCultures.Localize("/", culture));
            Assert.AreEqual($"/{culture.Name}/compare", LandingCultures.Localize("/compare", culture));
        }
    }
}

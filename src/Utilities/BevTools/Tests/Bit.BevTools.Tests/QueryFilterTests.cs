
namespace Bit.BevTools.Tests;

[TestClass]
public class QueryFilterTests
{
    [TestMethod]
    public void Parses_tokens_thresholds_and_free_text()
    {
        var filter = BevQueryFilter.Parse("Product method:GET renders>20");

        Assert.AreSequenceEqual(["Product"], filter.Terms);
        Assert.AreEqual("GET", filter.Token("method"));
        Assert.AreEqual(20, filter.IntToken("renders>"));
        Assert.IsTrue(filter.MatchesText("ProductList"));
        Assert.IsFalse(filter.MatchesText("OrderList"));
    }

    [TestMethod]
    public void Urls_remain_search_terms_instead_of_becoming_tokens()
    {
        var filter = BevQueryFilter.Parse("https://example.test/api/orders");

        Assert.AreSequenceEqual(["https://example.test/api/orders"], filter.Terms);
        Assert.IsEmpty(filter.Tokens);
        Assert.IsTrue(filter.MatchesText("GET https://example.test/api/orders"));
    }
}


namespace Bit.BevTools.Tests;

[TestClass]
public class QueryFilterTests
{
    [TestMethod]
    public void Parses_tokens_thresholds_and_free_text()
    {
        var filter = BevToolsQueryFilter.Parse("Product method:GET renders>20");

        Assert.AreSequenceEqual(["Product"], filter.Terms);
        Assert.AreEqual("GET", filter.Token("method"));
        Assert.AreEqual(20, filter.IntToken("renders>"));
        Assert.IsTrue(filter.MatchesText("ProductList"));
        Assert.IsFalse(filter.MatchesText("OrderList"));
    }

    [TestMethod]
    public void A_regular_expression_between_slashes_matches_with_the_rest_of_the_query()
    {
        var filter = BevToolsQueryFilter.Parse(@"level:warning /timed? ?out after \d+ s/ orders");

        Assert.AreEqual("warning", filter.Token("level"));
        Assert.AreSequenceEqual(["orders"], filter.Terms);
        Assert.AreEqual(@"/timed? ?out after \d+ s/", filter.RegexText);
        Assert.IsTrue(filter.HasText);
        Assert.IsTrue(filter.MatchesText("Orders sync: TIMED OUT after 30 s"));
        Assert.IsTrue(filter.MatchesText("orders", "timeout after 5 s"), "any of the fields may match the expression");
        Assert.IsFalse(filter.MatchesText("Orders sync: timeout after soon"));
        Assert.IsFalse(filter.MatchesText("Users sync: timeout after 30 s"));
    }

    [TestMethod]
    public void Paths_stay_search_terms()
    {
        var filter = BevToolsQueryFilter.Parse("/counter /api/orders");

        Assert.IsNull(filter.RegexText);
        Assert.AreSequenceEqual(["/counter", "/api/orders"], filter.Terms);
    }

    [TestMethod]
    public void An_invalid_expression_matches_nothing_and_says_why()
    {
        var filter = BevToolsQueryFilter.Parse("/[unclosed/");

        Assert.IsNull(filter.Regex);
        Assert.IsNotNull(filter.RegexError);
        Assert.IsFalse(filter.MatchesText("[unclosed"));
    }

    [TestMethod]
    public void An_expression_that_runs_out_of_time_does_not_match_and_is_reported()
    {
        var filter = BevToolsQueryFilter.Parse("/(a+)+$/");

        Assert.IsFalse(filter.MatchesText(new string('a', 5000) + "!"));
        Assert.IsTrue(filter.RegexTimedOut);
    }

    [TestMethod]
    public void Urls_remain_search_terms_instead_of_becoming_tokens()
    {
        var filter = BevToolsQueryFilter.Parse("https://example.test/api/orders");

        Assert.AreSequenceEqual(["https://example.test/api/orders"], filter.Terms);
        Assert.IsEmpty(filter.Tokens);
        Assert.IsTrue(filter.MatchesText("GET https://example.test/api/orders"));
    }
}

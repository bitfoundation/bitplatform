namespace Boilerplate.Tests.Features.Urls;

/// <summary>
/// <see cref="AppQueryStringCollection"/> is the one place the app takes a query string apart and puts it back
/// together - every url the client builds (culture, returnUrl, OData <c>$filter</c>, the 401/403 recovery redirect)
/// passes through it. Two properties have to hold, and both used to be broken in ways nothing would have noticed
/// until a user hit them:
/// <list type="bullet">
/// <item>Parsing must never throw. A repeated key is legal in a url and arrives from ad networks and mail clients
/// unbidden, so it cannot be allowed to become a 500 on an anonymous landing page - or, on the WASM client where the
/// same parse runs during start-up, a blank screen.</item>
/// <item>Values are held <b>decoded</b> and escaped <b>exactly once</b> on the way out. Escaping twice corrupts the
/// value; unescaping a value that was added raw silently changes what the caller asked for.</item>
/// </list>
/// Both are pure functions with no dependencies, which is why this is a plain unit test rather than an integration one.
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class AppQueryStringCollectionTests
{
    /// <summary>
    /// A repeated key is a valid url (<c>?utm_source=a&amp;utm_source=b</c>) and the collection is a dictionary, so
    /// the parse has to choose rather than <c>Add</c> - which throws on the second occurrence. Last-wins matches what
    /// browsers and <c>HttpUtility.ParseQueryString</c> callers expect from the last value in the string.
    /// </summary>
    [TestMethod]
    public void Parse_Should_KeepTheLastValue_WhenAKeyIsRepeated()
    {
        var queryString = AppQueryStringCollection.Parse("?utm_source=a&utm_source=b&culture=fa-IR");

        Assert.HasCount(2, queryString, "A repeated key must collapse to one entry, not throw and not duplicate.");
        Assert.AreEqual("b", queryString["utm_source"]?.ToString(), "The last occurrence in the url wins.");
        Assert.AreEqual("fa-IR", queryString["culture"]?.ToString(), "The other keys must survive the collapse.");
    }

    /// <summary>
    /// The same for a key repeated with an identical value, and for the casing difference the collection's
    /// <see cref="StringComparer.OrdinalIgnoreCase"/> comparer folds together - that comparer is what makes
    /// <c>Add</c> throw on inputs a case-sensitive parser would have tolerated.
    /// </summary>
    [TestMethod]
    [DataRow("?a=1&a=1")]
    [DataRow("?a=1&A=2")]
    [DataRow("?a=1&a=2&a=3")]
    public void Parse_Should_NotThrow_OnAnyRepetitionShape(string query)
    {
        Assert.HasCount(1, AppQueryStringCollection.Parse(query));
    }

    /// <summary>
    /// Keys and values come out of <see cref="AppQueryStringCollection.Parse"/> decoded, so a consumer reading
    /// <c>returnUrl</c> or <c>$filter</c> gets the real value rather than its wire form.
    /// </summary>
    [TestMethod]
    public void Parse_Should_DecodeExactlyOnce()
    {
        var queryString = AppQueryStringCollection.Parse("?q=100%20off&returnUrl=%2Fproducts%2F42");

        Assert.AreEqual("100 off", queryString["q"]?.ToString());
        Assert.AreEqual("/products/42", queryString["returnUrl"]?.ToString());
    }

    /// <summary>
    /// The counterpart: values are added <b>raw</b> and escaped on the way out, never decoded first. A value that
    /// legitimately contains a percent sign (a literal <c>100% off</c>, or an OData filter someone pre-encoded) must
    /// come out with that percent escaped - decoding before re-encoding turns <c>100%20off</c> into <c>100 off</c> and
    /// quietly changes the query the server runs.
    /// </summary>
    [TestMethod]
    public void ToString_Should_EscapeExactlyOnce_AndNeverDecodeFirst()
    {
        Assert.AreEqual("q=100%25%20off", new AppQueryStringCollection { ["q"] = "100% off" }.ToString(),
            "A literal percent in the value must be escaped, not treated as the start of an escape sequence.");

        Assert.AreEqual("q=100%2520off", new AppQueryStringCollection { ["q"] = "100%20off" }.ToString(),
            "A value that already looks encoded is still just a value: it is escaped, never decoded.");
    }

    /// <summary>
    /// The property that matters most for OData, because <c>$filter</c> is the one value in the app that routinely
    /// carries parentheses, commas, quotes and spaces: whatever is put in comes back out byte-identical after a full
    /// encode/decode round trip.
    /// </summary>
    [TestMethod]
    [DataRow("contains(name,'100% off')")]
    [DataRow("contains(name,'a b')")]
    [DataRow("/products/42?tab=specs&x=1")]
    [DataRow("سلام دنیا")]
    [DataRow("a+b")]
    public void ParseOfToString_Should_RoundTripAValueUnchanged(string value)
    {
        var written = new AppQueryStringCollection { ["$filter"] = value }.ToString();

        Assert.AreEqual(value, AppQueryStringCollection.Parse(written!)["$filter"]?.ToString(),
            $"'{value}' did not survive the round trip; it was written as '{written}'.");
    }

    /// <summary>
    /// An empty collection produces null rather than an empty string, because callers prepend the '?' themselves
    /// (see <c>UriExtensions.GetUrlWithoutQueryParameter</c>) and a lone '?' is a different url.
    /// </summary>
    [TestMethod]
    public void ToString_Should_ReturnNull_WhenEmpty()
    {
        Assert.IsNull(new AppQueryStringCollection().ToString());
        Assert.IsNull(AppQueryStringCollection.Parse(null).ToString());
        Assert.IsNull(AppQueryStringCollection.Parse("?").ToString());
    }
}

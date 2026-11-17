using Bit.Utilities.Mcp;

namespace Bit.Bmotion.Tests.Mcp.Services;

/// <summary>
/// The rules of the ranking every utility's MCP server shares, each on a corpus of its own, so what a rule does
/// is pinned down apart from whatever any one server's corpus happens to hold.
/// </summary>
[TestClass]
public class McpSearchTests
{
    private static McpSearchEntry Member(string name, string body = "A member.")
    {
        return new("API property", $"Owner.{name}", "Owner", $"GetDetails(name: \"{name}\")", body, string.Empty, Name: name);
    }

    /// <summary>
    /// Preferring the entry called exactly what a word says moves every ranking where a name is typed out, so
    /// a server opts into it: by default a name and its plural namesake rank alike.
    /// </summary>
    [TestMethod]
    public void Search_AnExactName_IsPreferredOnlyWhereTheServerAsksForIt()
    {
        McpSearchEntry[] entries = [Member("Spring"), Member("Springs")];

        var plain = new McpSearch(() => entries, ambientWords: []);
        var exact = new McpSearch(() => entries, ambientWords: [], preferExactNames: true);

        Assert.AreEqual("Owner.Spring", plain.Search("tune Springs", 5).Hits[0].Entry.Title);
        Assert.AreEqual("Owner.Springs", exact.Search("tune Springs", 5).Hits[0].Entry.Title);
    }

    /// <summary>
    /// A one-word name capitalized as the first word of a sentence is not written as a name: every sentence
    /// starts with a capital. Written that way anywhere else, or as the whole query, it is.
    /// </summary>
    [TestMethod]
    [DataRow("Springs feel too slow", "Owner.Spring")]
    [DataRow("They are fine. Springs feel too slow", "Owner.Spring")]
    [DataRow("tune Springs", "Owner.Springs")]
    [DataRow("Springs", "Owner.Springs")]
    public void Search_ACapitalStartingASentence_IsNotTakenForAName(string query, string expected)
    {
        var search = new McpSearch(() => [Member("Spring"), Member("Springs")], ambientWords: [], preferExactNames: true);

        Assert.AreEqual(expected, search.Search(query, 5).Hits[0].Entry.Title);
    }

    /// <summary>
    /// The ambient words are dropped before the query is cut to the most terms it is searched by: otherwise a
    /// query full of them would spend every place on words that are then thrown away.
    /// </summary>
    [TestMethod]
    public void Search_AmbientWords_TakeNoneOfTheTermsAQueryIsSearchedBy()
    {
        string[] ambient = [.. Enumerable.Range(0, 20).Select(i => $"filler{(char)('a' + i)}")];

        var search = new McpSearch(() => [Member("Opacity", "How opaque it is.")], ambient, searchAmbientAlone: false);

        var result = search.Search($"{string.Join(' ', ambient)} opacity", 5);

        CollectionAssert.AreEqual(new[] { "opacity" }, result.Terms);
        Assert.HasCount(1, result.Hits);
    }

    /// <summary>
    /// An ambient noun said of one thing names whatever the index is about; said of many, or run on into a name,
    /// it is searched as usual - and said with nothing else, it is all there is to search by.
    /// </summary>
    [TestMethod]
    [DataRow("fade an item in", new[] { "fade" })]
    [DataRow("fade a list-item in", new[] { "fade" })]
    [DataRow("fade a single list item in", new[] { "fade" })]
    [DataRow("fade the item keyframes", new[] { "fade", "keyframes" })]
    [DataRow("fade each list item in", new[] { "fade", "each", "list", "item" })]
    [DataRow("fade every item in", new[] { "fade", "every", "item" })]
    [DataRow("fade the items in", new[] { "fade", "items" })]
    [DataRow("set the item key", new[] { "set", "item", "key" })]
    [DataRow("an item", new[] { "item" })]
    public void Search_AnAmbientNounSaidOfOneThing_IsDropped(string query, string[] expected)
    {
        var search = new McpSearch(() => [Member("ItemKey"), Member("Fade")], ambientWords: [], ambientNouns: ["item", "list item"]);

        CollectionAssert.AreEqual(expected, search.Search(query, 5).Terms);
    }
}

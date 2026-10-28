using Bit.Blex.Demo.Server.Dtos;

namespace Bit.Blex.Tests.Mcp;

/// <summary>SearchBlex: one query across every corpus, each hit carrying the call that returns it whole.</summary>
[TestClass]
public class McpSearchToolTests
{
    [TestMethod]
    [DataRow("persist localStorage", "Persistence")]
    [DataRow("undo redo", "Undo")]
    [DataRow("cancel effect", "Cancel")]
    [DataRow("entity adapter", "Entit")]
    [DataRow("middleware veto", "Middleware")]
    [DataRow("BLEX001", "BLEX001")]
    public async Task A_feature_query_finds_its_material_near_the_top(string query, string expected)
    {
        var result = await SearchAsync(query, 5);

        Assert.IsTrue(result.Hits.Any(hit => hit.Title.Contains(expected, StringComparison.OrdinalIgnoreCase)),
            $"'{query}' did not put anything about '{expected}' in its top 5: {string.Join(" | ", result.Hits.Select(hit => hit.Title))}");
    }

    [TestMethod]
    [DataRow("a counter whose value survives a page reload", "Persistence")]
    [DataRow("state that survives a reload", "Persistence")]
    [DataRow("testing", "Testing")]
    public async Task A_question_in_plain_words_puts_its_answer_first(string query, string expected)
    {
        // An agent that trusts the first hit goes wherever it points, so a word every paged query shares
        // ("page", "value") must not hand that place to them - however few hits were asked for.
        foreach (var limit in new[] { 1, 3, 6 })
        {
            var result = await SearchAsync(query, limit);

            Assert.AreEqual(expected, result.Hits.FirstOrDefault()?.Title,
                $"'{query}' (limit {limit}) ranked first: {string.Join(" | ", result.Hits.Select(hit => hit.Title))}");
        }
    }

    [TestMethod]
    public async Task A_member_every_type_has_is_not_an_answer()
    {
        // Split into words, GetHashCode is "code": left in, it answers anything done from code.
        var result = await SearchAsync("code", 50);

        Assert.IsFalse(result.Hits.Any(hit => hit.Title.EndsWith(".GetHashCode", StringComparison.Ordinal) || hit.Title.EndsWith(".Equals", StringComparison.Ordinal)),
            string.Join(" | ", result.Hits.Select(hit => hit.Title)));
    }

    [TestMethod]
    public async Task A_query_in_another_librarys_words_still_lands()
    {
        // A Fluxor or Redux user asks for reducers; Blex calls them actions.
        var result = await SearchAsync("reducer", 10);

        Assert.IsTrue(result.Hits.Any(hit => hit.Title == "Actions"), "A query for 'reducer' did not find the Actions page.");
    }

    [TestMethod]
    public async Task Every_hit_names_the_call_that_returns_it()
    {
        var result = await SearchAsync("store", 30);

        Assert.IsTrue(result.Hits.Length > 10);

        foreach (var hit in result.Hits)
        {
            Assert.IsTrue(hit.Tool.StartsWith("Get", StringComparison.Ordinal) && hit.Tool.EndsWith(')'), $"'{hit.Tool}' is not a call.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(hit.Kind));
        }
    }

    [TestMethod]
    public async Task The_limit_is_honored_and_says_when_there_is_more()
    {
        var result = await SearchAsync("state", 3);

        Assert.AreEqual(3, result.Hits.Length);
        Assert.IsTrue(result.HasMore);

        var clamped = await SearchAsync("state", 500);
        Assert.IsTrue(clamped.Hits.Length <= 50);
    }

    [TestMethod]
    public async Task A_query_of_filler_words_explains_itself()
    {
        var result = await SearchAsync("how do I", 5);

        Assert.AreEqual(0, result.Hits.Length);
        StringAssert.Contains(result.Message!, "nothing to rank by");
    }

    [TestMethod]
    public async Task Nothing_found_says_what_to_try_instead()
    {
        var result = await SearchAsync("xylophone", 5);

        Assert.AreEqual(0, result.Hits.Length);
        Assert.IsNotNull(result.Message);
        CollectionAssert.AreEqual(new[] { "xylophone" }, result.Terms);
    }

    private static Task<BlexSearchResultDto> SearchAsync(string query, int limit)
        => McpCall.StructuredAsync<BlexSearchResultDto>("SearchBlex", new() { ["query"] = query, ["limit"] = limit });
}

using Bit.Brouter.Demo.Server.Dtos;

namespace Bit.Brouter.Tests.Mcp;

/// <summary>
/// The default entry point. Search is the tool an agent reaches for when it does not know what the
/// thing it wants is called here, which makes two of its behaviors load-bearing: a real question
/// has to rank the right material first, and a question that matches nothing has to say so in a way
/// that sends the agent to a better query rather than to a hand-rolled implementation.
/// </summary>
[TestClass]
public class McpSearchToolTests
{
    private static Task<BrouterSearchResultDto> SearchAsync(string query, int? limit = null)
    {
        var arguments = new Dictionary<string, object?> { ["query"] = query };

        if (limit is not null) arguments["limit"] = limit;

        return McpCall.StructuredAsync<BrouterSearchResultDto>("SearchBrouter", arguments);
    }

    [TestMethod]
    public async Task A_question_in_a_users_own_words_finds_the_feature_that_answers_it()
    {
        // The phrasings are the ones the server's own prompts and completions hand people, so they
        // are the queries this index has to be good at. Any of the listed titles is a right answer -
        // the test is that the feature comes up at all, not which of its write-ups ranks first.
        (string Query, string[] Answers)[] questions =
        [
            ("block navigation unsaved changes", ["Leave guards (unsaved changes)", "Guards & navigation locks"]),
            ("keep component alive", ["Keep-alive routes", "Lifecycle & keep-alive"]),
            ("cache loader data", ["Loader caching (stale-while-revalidate)", "Data loading"]),
            ("query string binding", ["Auto-bound parameters", "Route parameters", "BrouterQueryAttribute"]),
            ("view transition", ["View transitions"]),
            ("nested routes outlet", ["Nested routes", "Nested routes & outlets"]),
        ];

        foreach (var (query, answers) in questions)
        {
            var result = await SearchAsync(query, limit: 8);

            Assert.IsTrue(result.Hits.Length > 0, $"'{query}' matched nothing at all.");
            Assert.IsTrue(result.Hits.Any(hit => answers.Any(answer => hit.Title.Contains(answer, StringComparison.OrdinalIgnoreCase))),
                $"'{query}' surfaced none of [{string.Join(", ", answers)}]. It found: {string.Join(", ", result.Hits.Select(hit => hit.Title))}.");
        }
    }

    [TestMethod]
    public async Task Searching_for_a_member_by_name_surfaces_the_member_itself()
    {
        // Someone typing a member name wants the member, not only the pages that discuss it - and
        // the hit has to arrive with the call that returns its reference.
        var result = await SearchAsync("KeepAlive", limit: 10);

        var member = result.Hits.FirstOrDefault(hit => hit.Title.Equals("Broute.KeepAlive", StringComparison.Ordinal));

        Assert.IsNotNull(member, $"'KeepAlive' did not surface Broute.KeepAlive. It found: {string.Join(", ", result.Hits.Select(hit => hit.Title))}.");
        Assert.AreEqual("GetBrouterApiDetails(typeName: \"Broute\")", member.Tool);
    }

    [TestMethod]
    public async Task A_singular_query_finds_a_plural_heading()
    {
        var result = await SearchAsync("guard", limit: 10);

        Assert.IsTrue(result.Hits.Any(hit => hit.Title.Contains("guards", StringComparison.OrdinalIgnoreCase)),
            "'guard' did not find the sections spelled 'guards'.");
    }

    [TestMethod]
    public async Task Search_reaches_every_corpus_it_claims_to()
    {
        // One query per body of material, so a corpus that stops being indexed is caught here rather
        // than by an agent that quietly never sees it again.
        (string Query, string Kind)[] corpora =
        [
            ("keep alive", "Guide section"),
            ("constraints", "Docs page"),
            ("BrouterOptions", "API class"),
            ("nonfile", "Route constraint"),
            ("AppRouter", "Source file"),
        ];

        foreach (var (query, kind) in corpora)
        {
            var result = await SearchAsync(query, limit: 50);

            Assert.IsTrue(result.Hits.Any(hit => hit.Kind == kind),
                $"'{query}' returned no hit of kind '{kind}'. Kinds found: {string.Join(", ", result.Hits.Select(hit => hit.Kind).Distinct())}.");
        }
    }

    [TestMethod]
    public async Task Every_hit_carries_the_call_that_returns_its_full_text()
    {
        var result = await SearchAsync("loader", limit: 20);

        foreach (var hit in result.Hits)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(hit.Tool), $"'{hit.Title}' tells the caller nothing about how to read it.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(hit.Snippet), $"'{hit.Title}' came back with no snippet.");
            Assert.IsTrue(hit.Snippet.Length < 400, $"'{hit.Title}' came back with a {hit.Snippet.Length}-character 'snippet'.");
        }
    }

    [TestMethod]
    public async Task The_query_and_the_words_it_was_ranked_by_come_back_with_the_hits()
    {
        var result = await SearchAsync("How do I redirect from a guard?");

        Assert.AreEqual("How do I redirect from a guard?", result.Query);

        // Filler words rank nothing and would drag in every entry that merely contains them.
        CollectionAssert.DoesNotContain(result.Terms, "how");
        CollectionAssert.DoesNotContain(result.Terms, "from");
        CollectionAssert.Contains(result.Terms, "redirect");
        CollectionAssert.Contains(result.Terms, "guard");
    }

    [TestMethod]
    public async Task The_limit_is_honored_and_clamped_at_both_ends()
    {
        var three = await SearchAsync("route", limit: 3);
        Assert.AreEqual(3, three.Hits.Length);
        Assert.IsTrue(three.HasMore, "A cut-short ranking has to say so, or the caller reads three hits as everything there is.");

        // A pasted-in limit must not be able to turn one search into the whole index.
        var capped = await SearchAsync("route", limit: 999);
        Assert.IsTrue(capped.Hits.Length <= 50, $"{capped.Hits.Length} hits came back for a limit of 999.");

        var zero = await SearchAsync("route", limit: 0);
        Assert.AreEqual(1, zero.Hits.Length, "A limit of zero should answer with one hit rather than none.");
    }

    [TestMethod]
    public async Task A_query_with_nothing_to_rank_by_says_so_instead_of_answering_empty()
    {
        // "Searched and found nothing" and "there was nothing to search by" are different failures
        // with different fixes, and only one of them is the caller's to fix.
        var result = await SearchAsync("how do the and for");

        Assert.AreEqual(0, result.Hits.Length);
        Assert.AreEqual(0, result.Terms.Length);
        Assert.IsNotNull(result.Message);
        StringAssert.Contains(result.Message, "filler word");
    }

    [TestMethod]
    public async Task A_near_miss_is_answered_with_the_names_that_nearly_matched()
    {
        var result = await SearchAsync("guardz");

        Assert.AreEqual(0, result.Hits.Length);
        Assert.IsNotNull(result.DidYouMean);
        Assert.IsTrue(result.DidYouMean.Any(title => title.Contains("Guard", StringComparison.OrdinalIgnoreCase)),
            $"'guardz' suggested {string.Join(", ", result.DidYouMean)}, none of which is about guards.");
    }

    [TestMethod]
    public async Task A_query_matching_nothing_at_all_is_told_what_brouter_calls_things()
    {
        var result = await SearchAsync("kubernetes");

        Assert.AreEqual(0, result.Hits.Length);
        Assert.IsNotNull(result.Message);

        // The failure mode this guards against: an agent reading an empty result as "Brouter cannot
        // do this" and writing its own router feature instead.
        StringAssert.Contains(result.Message, "middleware");
    }

    [TestMethod]
    public async Task A_pasted_wall_of_text_is_searched_by_a_bounded_number_of_words()
    {
        // Every term is counted against every entry, so an unbounded query would scan for minutes.
        var query = string.Join(' ', Enumerable.Range(0, 400).Select(index => $"term{index}navigation"));

        var result = await SearchAsync(query);

        Assert.IsTrue(result.Terms.Length <= 16, $"{result.Terms.Length} terms were searched by; the cap is 16.");
    }
}

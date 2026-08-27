using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bit.BlazorUI.Tests.Mcp.Infrastructure;

namespace Bit.BlazorUI.Tests.Mcp;

/// <summary>
/// The tool an agent calls first, and the one whose failure is invisible: a search that returns the
/// wrong component returns it confidently, and the agent writes markup against it.
/// <para>
/// So the cases here are the ones where the library's name for a thing is not the name the task
/// suggests - a select is BitDropdown, a toast is BitSnackBar - and the assertion is on the FIRST
/// hit, because that is the one an agent acts on.
/// </para>
/// </summary>
[TestClass]
public class SearchTests : McpTestBase
{
    [DataTestMethod]
    [DataRow("let the user pick a date range", "BitDateRangePicker")]
    [DataRow("toast notification", "BitSnackBar")]
    [DataRow("searchable multi select with chips", "BitDropdown")]
    [DataRow("loading skeleton placeholder", "BitShimmer")]
    [DataRow("tabs", "BitPivot")]
    [DataRow("collapsible expander panel", "BitAccordion")]
    [DataRow("upload a file with progress", "BitFileUpload")]
    [DataRow("virtualized data table with sorting and paging", "BitDataGrid")]
    [DataRow("on off switch", "BitToggle")]
    [DataRow("breadcrumbs", "BitBreadcrumb")]
    public async Task A_capability_finds_the_component_that_provides_it(string query, string expected)
    {
        var answer = await CallAsync("SearchBitBlazorUI", new { query, limit = 5 });

        var first = answer.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n')
            .First(line => line.StartsWith("## ", StringComparison.Ordinal));

        StringAssert.Contains(first, expected,
            $"'{query}' should lead with {expected}; it led with '{first[3..]}'. The whole answer:\n{answer}");
    }

    [TestMethod]
    public async Task Every_hit_names_the_call_that_returns_its_full_text()
    {
        var answer = await CallAsync("SearchBitBlazorUI", new { query = "dark mode theme token", limit = 12 });

        var calls = answer.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n')
            .Where(line => line.Contains(" · `", StringComparison.Ordinal))
            .Select(line => line[(line.IndexOf(" · `", StringComparison.Ordinal) + 4)..].TrimEnd('`'))
            .ToArray();

        Assert.IsNotEmpty(calls, "No hit named a follow-up call, which is the whole point of a search hit here.");

        using var scope = Assert.Scope();

        foreach (var call in calls)
        {
            var tool = call[..call.IndexOf('(', StringComparison.Ordinal)];

            CollectionAssert.Contains(ToolNames, tool, $"A hit points at '{tool}', which this server does not publish.");
        }
    }

    [TestMethod]
    public async Task A_follow_up_call_a_hit_names_actually_answers()
    {
        var answer = await CallAsync("SearchBitBlazorUI", new { query = "chips in a combobox", limit = 6 });

        var call = answer.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n')
            .First(line => line.Contains("GetBitBlazorUIComponent(name: \"", StringComparison.Ordinal));

        var name = call.Split("name: \"")[1].Split('"')[0];

        var component = await CallAsync("GetBitBlazorUIComponent", new { name });

        Assert.DoesNotContain("has no component called", component, $"A search hit named '{name}', which the component tool cannot resolve.");
    }

    [TestMethod]
    public async Task Search_covers_more_than_the_components()
    {
        using var scope = Assert.Scope();

        // A parameter, which is what an agent is looking for once it has picked the component.
        var parameter = await CallAsync("SearchBitBlazorUI", new { query = "AutoLoading", limit = 8 });
        StringAssert.Contains(parameter, "BitButton.AutoLoading");

        // A chapter of the theming reference, which no component owns.
        var theming = await CallAsync("SearchBitBlazorUI", new { query = "design tokens", limit = 8 });
        StringAssert.Contains(theming, "GetBitBlazorUIThemingGuide");

        // The setup guide, which is what "how do I install this" has to reach.
        var setup = await CallAsync("SearchBitBlazorUI", new { query = "register services stylesheet script", limit = 8 });
        StringAssert.Contains(setup, "GetBitBlazorUISetupGuide");
    }

    [TestMethod]
    public async Task A_query_that_matches_nothing_says_what_to_try_instead()
    {
        using var scope = Assert.Scope();

        var nothing = await CallAsync("SearchBitBlazorUI", new { query = "quantum flux capacitor", limit = 5 });
        StringAssert.Contains(nothing, "GetBitBlazorUIComponent", "An empty result does not say where to go next.");

        // A query made entirely of words this index drops means something different from a query
        // that matched nothing, and it is answered differently.
        var stopWords = await CallAsync("SearchBitBlazorUI", new { query = "the a of how", limit = 5 });
        StringAssert.Contains(stopWords, "no searchable term");
    }
}

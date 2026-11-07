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
    /// <summary>
    /// What a component does, phrased the way a task asks for it, leads with that component - the first hit
    /// being the one an agent writes markup against.
    /// </summary>
    [TestMethod]
    [DataRow("let the user pick a date range", "BitDateRangePicker")]
    [DataRow("toast notification", "BitSnackBar")]
    [DataRow("searchable multi select with chips", "BitDropdown")]
    [DataRow("loading skeleton placeholder", "BitShimmer")]
    [DataRow("tabs", "BitPivot")]
    [DataRow("collapsible expander panel", "BitAccordion")]
    // A keyword asked for in the plural, which BitColorPicker's nav entry writes in the singular - and which
    // nothing else says, so the case is the keyword's alone.
    [DataRow("eyedroppers", "BitColorPicker")]
    [DataRow("endless scroll", "BitInfiniteScrolling")]
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

    /// <summary>
    /// The names other libraries give to what a component here already does, typed the way a task phrases them.
    /// A search that led anywhere else would have an agent write a component the library already ships. The
    /// first hit is compared by its whole name, since BitButton is a prefix of BitButtonGroup.
    /// </summary>
    [TestMethod]
    [DataRow("segmented control", "BitButtonGroup")]
    [DataRow("loading button", "BitButton")]
    [DataRow("autocomplete", "BitSearchBox")]
    [DataRow("typeahead", "BitSearchBox")]
    [DataRow("textarea", "BitTextField")]
    [DataRow("month picker", "BitDatePicker")]
    [DataRow("app bar", "BitHeader")]
    [DataRow("bottom navigation", "BitNavBar")]
    [DataRow("hover card", "BitCallout")]
    [DataRow("drawer", "BitPanel")]
    [DataRow("bottom sheet", "BitPanel")]
    [DataRow("scroll area", "BitScrollablePane")]
    [DataRow("safe area", "BitAppShell")]
    [DataRow("tree grid", "BitDataGrid")]
    [DataRow("sidebar", "BitNavPanel")]
    // Names a nav entry lists among its keywords rather than its aliases.
    [DataRow("TagBox", "BitTagsInput")]
    [DataRow("useMediaQuery", "BitMediaQuery")]
    [DataRow("BlockUI", "BitOverlay")]
    public async Task A_name_from_another_library_finds_the_component_that_provides_it(string query, string expected)
    {
        var answer = await CallAsync("SearchBitBlazorUI", new { query, limit = 5 });

        var first = answer.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n')
            .First(line => line.StartsWith("## ", StringComparison.Ordinal));

        // "## BitDropdown<TItem, TValue> - Inputs": the name, then its type parameters and its category.
        var name = first[3..].Split(" - ")[0].Split('<')[0];

        Assert.AreEqual(expected, name, $"'{query}' should lead with {expected}; it led with '{first[3..]}'. The whole answer:\n{answer}");
    }

    /// <summary>
    /// Words that spell one of a component's names - an alias or its own, in one word or in several - name that
    /// component, and rank it above the components that only match the words one at a time: "app bar" is BitHeader's
    /// AppBar, while BitNavBar has "bar" in its name and in its TabBar, and BitChart has bars in its example titles;
    /// "search box" and "searchbox" are BitSearchBox, while BitDropdown has examples titled "SearchBox" and "ComboBox".
    /// A query that is nothing but a component's name leads with it whatever outscores it - BitChartLegacy's example
    /// titles say "chart" more often than BitChart's, and BitAccordionList lists "Accordion" as an alias - and a name is
    /// spelled with the short words the scored terms leave out ("ai chat" is BitAiChat, "md viewer" BitMarkdownViewer's
    /// MdViewer). The other component is asserted to be among the hits as well, so the case keeps testing a ranking
    /// rather than passing because the rival dropped out.
    /// </summary>
    [TestMethod]
    [DataRow("app bar", "BitHeader", "BitNavBar")]
    [DataRow("app bar", "BitHeader", "BitChart")]
    [DataRow("top bar", "BitHeader", "BitChart")]
    [DataRow("tab bar", "BitNavBar", "BitPivot")]
    [DataRow("search box", "BitSearchBox", "BitDropdown")]
    [DataRow("searchbox", "BitSearchBox", "BitDropdown")]
    [DataRow("calendar", "BitCalendar", "BitFullCalendar")]
    [DataRow("grid", "BitGrid", "BitDataGrid")]
    [DataRow("toggle", "BitToggle", "BitToggleButton")]
    [DataRow("chart", "BitChart", "BitChartLegacy")]
    [DataRow("BitChart", "BitChart", "BitChartLegacy")]
    [DataRow("tag", "BitTag", "BitTagsInput")]
    [DataRow("accordion", "BitAccordion", "BitAccordionList")]
    [DataRow("ai chat", "BitAiChat", "BitAiPromptInput")]
    [DataRow("ai chat with streaming", "BitAiChat", "BitAiStreamingMarkdown")]
    [DataRow("md viewer", "BitMarkdownViewer", "BitMarkdownViewerLegacy")]
    public async Task Words_spelling_a_whole_name_rank_above_partial_word_matches(string query, string expected, string partial)
    {
        var (answer, names) = await HitsAsync(query);

        var whole = names.IndexOf(expected);
        var part = names.IndexOf(partial);

        using var scope = Assert.Scope();

        Assert.AreEqual(0, whole, $"'{query}' spells a name of {expected}, which should lead. The whole answer:\n{answer}");
        Assert.IsGreaterThan(whole, part, $"'{query}' should still find {partial}, below {expected}. The whole answer:\n{answer}");
    }

    /// <summary>
    /// The last word of a component's name says what the component is - BitAiChat is a chat, BitScrollablePane a pane -
    /// and outweighs the same word inside an alias of another component, as the site's search box ranks a name above an
    /// alias: for "chat", BitAiPromptInput, which lists "ChatInput", ranked above BitAiChat; for "pane", BitSplitter, which
    /// lists "SplitPane", above BitScrollablePane. The other component is asserted to be among the hits as well.
    /// </summary>
    [TestMethod]
    [DataRow("chat", "BitAiChat", "BitAiPromptInput")]
    [DataRow("pane", "BitScrollablePane", "BitSplitter")]
    [DataRow("selector", "BitAiModelSelector", "BitCountryPicker")]
    public async Task The_word_a_name_ends_in_ranks_it_above_an_alias_holding_the_word(string query, string expected, string aliased)
    {
        var (answer, names) = await HitsAsync(query);

        var named = names.IndexOf(expected);
        var alias = names.IndexOf(aliased);

        using var scope = Assert.Scope();

        Assert.AreEqual(0, named, $"'{query}' is the word {expected}'s name ends in, which should lead. The whole answer:\n{answer}");
        Assert.IsGreaterThan(named, alias, $"'{query}' should still find {aliased}, below {expected}. The whole answer:\n{answer}");
    }

    /// <summary>
    /// The keywords a component's nav entry tags it with - what it does and what it is used for - find it ahead of the
    /// hits that led while the search read no keywords, which are asserted to be among the hits still, so the case keeps
    /// testing a ranking. BitAccordion, tagged "Collapsible", matches every word of "collapsible expander panel" - that
    /// keyword, its alias Expander and the panel its description opens - and so leads BitAiCollapsible, whose name is
    /// the collapsible and whose keywords list Expander as well, while it stays the general-purpose disclosure the
    /// query asks for rather than the AI one.
    /// </summary>
    [TestMethod]
    [DataRow("collapsible expander panel", "BitAccordion", "BitAiCollapsible")]
    [DataRow("eyedropper", "BitColorPicker", "BitColorPicker.EyeDropperIcon")]
    [DataRow("gauge", "BitChart", "BitProgress")]
    [DataRow("thumbnails", "BitPdfViewer", "BitCarousel")]
    [DataRow("pie chart", "BitChart", "BitChartLegacy")]
    public async Task A_keyword_ranks_the_component_it_tags_first(string query, string expected, string other)
    {
        var (answer, names) = await HitsAsync(query);

        var tagged = names.IndexOf(expected);
        var rival = names.IndexOf(other);

        using var scope = Assert.Scope();

        Assert.AreEqual(0, tagged, $"'{query}' matches a keyword of {expected}, which should lead. The whole answer:\n{answer}");
        Assert.IsGreaterThan(tagged, rival, $"'{query}' should still find {other}, below {expected}. The whole answer:\n{answer}");
    }

    /// <summary>
    /// A hit found by a keyword or an example title its name does not say names it: BitChart's description, which
    /// is its snippet, mentions no gauge, so without the line the hit would not say why "gauge" found it. A term the
    /// name holds names nothing - "chart" is BitChart's own word, not one of its keywords.
    /// </summary>
    [TestMethod]
    public async Task A_hit_names_the_keyword_it_was_found_by()
    {
        using var scope = Assert.Scope();

        StringAssert.Contains(await HitBlockAsync("gauge", "BitChart"), "\nMatches: Gauge", "A keyword hit does not name the keyword.");
        Assert.DoesNotContain("\nMatches: ", await HitBlockAsync("chart", "BitChart"), "A hit names keywords for a term its own name holds.");
    }

    /// <summary>The lines of one hit of a search - its heading up to the next hit's - with its line breaks normalized.</summary>
    private async Task<string> HitBlockAsync(string query, string name)
    {
        var (answer, names) = await HitsAsync(query);

        var blocks = answer.Replace("\r\n", "\n", StringComparison.Ordinal).Split("\n## ").Skip(1).ToArray();
        var index = names.IndexOf(name);

        Assert.IsGreaterThanOrEqualTo(0, index, $"'{query}' does not find {name}. The whole answer:\n{answer}");

        return blocks[index];
    }

    /// <summary>The answer to a search, with the names of its hits in their order.</summary>
    private async Task<(string Answer, List<string> Names)> HitsAsync(string query)
    {
        var answer = await CallAsync("SearchBitBlazorUI", new { query, limit = 12 });

        // "## BitDropdown<TItem, TValue> - Inputs": the name, then its type parameters and its category.
        var names = answer.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n')
            .Where(line => line.StartsWith("## ", StringComparison.Ordinal))
            .Select(line => line[3..].Split(" - ")[0].Split('<')[0])
            .ToList();

        return (answer, names);
    }

    /// <summary>Every hit names the call returning its full text, and that call is a tool this server publishes.</summary>
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

    /// <summary>The component a hit names is one the component tool resolves, rather than a name it has never heard of.</summary>
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

    /// <summary>
    /// A hit on one member is answered with the call that returns that member's rows rather than the whole
    /// reference around it, which on the largest components is tens of thousands of characters.
    /// </summary>
    [TestMethod]
    public async Task A_member_hit_names_the_call_that_returns_its_rows_alone()
    {
        var answer = await CallAsync("SearchBitBlazorUI", new { query = "pan the map to a coordinate", limit = 8 });

        var line = answer.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n')
            .First(line => line.StartsWith("Public member · ", StringComparison.Ordinal));

        var name = line.Split("name: \"")[1].Split('"')[0];
        var section = line.Split("section: \"")[1].Split('"')[0];

        var rows = await CallAsync("GetBitBlazorUIComponent", new { name, section });

        using var scope = Assert.Scope();

        StringAssert.Contains(rows, "## Public members", $"The call a hit names for {name}.{section} does not answer with its row.");
        Assert.DoesNotContain("has no section", rows, $"The call a hit names for {name}.{section} does not resolve.");
    }

    /// <summary>
    /// A member is found by its name, once, rather than by the parameter names of each of its overloads: as
    /// title words, a signature's <c>title</c> and <c>body</c> made every one of BitSnackBar's methods a hit.
    /// </summary>
    [TestMethod]
    public async Task A_member_is_titled_by_its_name_alone()
    {
        var answer = await CallAsync("SearchBitBlazorUI", new { query = "snackbar title body", limit = 20 });

        var titles = answer.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').Where(line => line.StartsWith("## ", StringComparison.Ordinal)).ToArray();

        using var scope = Assert.Scope();

        Assert.IsFalse(titles.Any(title => title.Contains('(', StringComparison.Ordinal)), $"A hit is titled by a signature: {string.Join(" / ", titles)}");
        CollectionAssert.AllItemsAreUnique(titles, "A member is a hit once per overload.");
    }

    /// <summary>
    /// The search reaches every corpus the server answers from - parameters, theming chapters, the setup guide, CSS
    /// variables and extension members - not only the components.
    /// </summary>
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

        // A component's public custom property, which is the half of its look no parameter reaches.
        var variable = await CallAsync("SearchBitBlazorUI", new { query = "--bit-ActionButton-hover-background", limit = 8 });
        StringAssert.Contains(variable, "--bit-ActionButton-hover-background");

        // A preset one package adds to another package's type, which is stored nowhere near the
        // type it is written on and so has to be indexed under the name it is read by.
        var preset = await CallAsync("SearchBitBlazorUI", new { query = "material dark preset", limit = 8 });
        StringAssert.Contains(preset, "BitThemePresets.MaterialDark");
    }

    /// <summary>A search that finds nothing, or that has no word left to search for, says what to try instead.</summary>
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

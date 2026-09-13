using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bit.BlazorUI.Tests.Mcp.Infrastructure;

namespace Bit.BlazorUI.Tests.Mcp;

/// <summary>
/// The examples tool, which is the one an agent copies out of.
/// <para>
/// Its answer is only worth anything if the code is really there - the sample fields are read off
/// the compiled demo page by reflection, and a renamed field would silently produce a section with
/// a title, a paragraph and no code at all. So the assertions are about fences, not about prose.
/// </para>
/// </summary>
[TestClass]
public class ExamplesToolTests : McpTestBase
{
    [TestMethod]
    public async Task Examples_carry_the_razor_the_documentation_site_runs()
    {
        var answer = await CallAsync("GetBitBlazorUIComponentExamples", new { name = "BitButton" });

        using var scope = Assert.Scope();

        StringAssert.StartsWith(answer, "# BitButton examples");
        StringAssert.Contains(answer, "```razor", "The answer carries no Razor at all.");
        StringAssert.Contains(answer, "<BitButton", "The Razor does not use the component it documents.");

        // A section per feature, each with its own heading.
        Assert.IsGreaterThan(5, answer.Split("\n## ", StringSplitOptions.None).Length, "The answer collapsed to a single section.");
    }

    [TestMethod]
    public async Task A_named_section_answers_with_that_section_alone()
    {
        var answer = await CallAsync("GetBitBlazorUIComponentExamples", new { name = "BitButton", example = "Loading" });

        using var scope = Assert.Scope();

        StringAssert.Contains(answer, "## Loading");
        StringAssert.Contains(answer, "IsLoading");
        Assert.DoesNotContain("## Basic", answer, "A named section brought its neighbours with it.");
    }

    [TestMethod]
    public async Task Csharp_behind_a_section_comes_with_it()
    {
        var answer = await CallAsync("GetBitBlazorUIComponentExamples", new { name = "BitDialog", example = "Basic" });

        using var scope = Assert.Scope();

        StringAssert.Contains(answer, "```razor");
        StringAssert.Contains(answer, "```csharp", "The C# behind the example is missing, so the Razor cannot be compiled from it.");
    }

    [TestMethod]
    public async Task A_multi_api_component_answers_with_one_tab_and_says_the_others_exist()
    {
        var answer = await CallAsync("GetBitBlazorUIComponentExamples", new { name = "BitDropdown" });

        using var scope = Assert.Scope();

        // The tabs of a multi-API component are the same sections in a different API, so returning
        // all of them is the same code three times over.
        StringAssert.Contains(answer, "multi-API component");
        StringAssert.Contains(answer, "## Item · ");
        Assert.DoesNotContain("## Custom · ", answer, "The Custom tab came back uninvited.");
    }

    [TestMethod]
    public async Task A_tab_name_wins_over_a_section_whose_title_contains_it()
    {
        // "Option" is one of BitDropdown's three APIs and also a word in "Search options". A caller
        // who typed a tab name asked for the tab.
        var answer = await CallAsync("GetBitBlazorUIComponentExamples", new { name = "BitDropdown", example = "Option" });

        using var scope = Assert.Scope();

        StringAssert.Contains(answer, "## Option · ");
        StringAssert.Contains(answer, "<BitDropdownOption");
    }

    [TestMethod]
    public async Task An_answer_that_had_to_stop_names_what_it_left_out()
    {
        var answer = await CallAsync("GetBitBlazorUIComponentExamples", new { name = "BitDropdown" });

        if (answer.Contains("Stopped here", StringComparison.Ordinal) is false) return;

        using var scope = Assert.Scope();

        // Cut between sections rather than mid-sample: half a code block is not a smaller answer,
        // it is a wrong one.
        StringAssert.EndsWith(answer.TrimEnd(), ".");
        StringAssert.Contains(answer, "GetBitBlazorUIComponentExamples(name: \"BitDropdown\", example:",
            "The truncation notice does not say how to get the rest.");

        var notice = answer[answer.LastIndexOf("Stopped here", StringComparison.Ordinal)..];
        var remaining = notice[(notice.IndexOf(": ", StringComparison.Ordinal) + 2)..].TrimEnd('.', '\n', '\r')
            .Split(", ", StringSplitOptions.RemoveEmptyEntries);

        Assert.AreEqual(remaining.Length, remaining.Distinct(StringComparer.Ordinal).Count(),
            "The list of what was left out repeats itself.");
    }

    [TestMethod]
    public async Task Every_section_the_component_advertises_can_be_asked_for_by_name()
    {
        var component = await CallAsync("GetBitBlazorUIComponent", new { name = "BitTextField" });

        var titles = component[component.IndexOf("returns the Razor and C# for:", StringComparison.Ordinal)..]
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Skip(1)
            .First(line => line.Length > 0)
            .TrimEnd('.')
            .Split(", ", StringSplitOptions.RemoveEmptyEntries);

        Assert.IsGreaterThan(3, titles.Length, "BitTextField advertises almost no sections.");

        using var scope = Assert.Scope();

        foreach (var title in titles)
        {
            var answer = await CallAsync("GetBitBlazorUIComponentExamples", new { name = "BitTextField", example = title });

            Assert.DoesNotContain("has no example matching", answer,
                $"BitTextField advertises the section '{title}' but the examples tool cannot return it.");
        }
    }
}

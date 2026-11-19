using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Protocol;
using Bit.BlazorUI.Tests.Mcp.Infrastructure;

namespace Bit.BlazorUI.Tests.Mcp;

/// <summary>
/// <c>GetBitBlazorUIBreakingChanges</c>: what an agent upgrading an app's packages works through, answered from the
/// analyzer's own BreakingChanges.json, which the .csproj embeds - so the way it goes quietly blank is a deployment that
/// lost the resource, and the way it goes quietly wrong is a version filter that drops the changes an app needs.
/// </summary>
[TestClass]
public class BreakingChangesToolTests : McpTestBase
{
    private const string MembersHeading = "| Old name | On | Use | Fix |";
    private const string TypesHeading = "| Old name | Kind | Use | Fix |";

    [TestMethod]
    public async Task With_no_version_every_change_since_10_5_is_listed_by_version_with_its_fix()
    {
        var answer = await CallAsync("GetBitBlazorUIBreakingChanges");

        using var scope = Assert.Scope();

        StringAssert.StartsWith(answer, "# bit BlazorUI: what to change after upgrading");
        foreach (var version in new[] { "10.6.0", "10.7.0", "11.0.0" })
            StringAssert.Contains(answer, $"## Gone in {version}", $"The changes of {version} are missing.");

        var members = AllRows(answer, MembersHeading);
        Assert.IsGreaterThan(80, members.Length, "Most of the renamed parameters are missing.");
        foreach (var row in members)
        {
            Assert.IsNotEmpty(row[1], $"{row[0]} names no type it was on.");
            Assert.IsNotEmpty(row[3], $"{row[0]} has no fix.");
        }

        Assert.IsGreaterThan(50, AllRows(answer, TypesHeading).Length, "Most of the removed components and types are missing.");

        // The ones whose value turns around with the name: the row has to say so.
        StringAssert.Contains(answer, "IsEnabled=\"false\" is Disabled");
        StringAssert.Contains(answer, "BreakingChanges.md", "The answer does not point at the changes no name shows.");
    }

    [TestMethod]
    public async Task A_version_lists_only_what_changed_after_it()
    {
        using var scope = Assert.Scope();

        var from107 = await CallAsync("GetBitBlazorUIBreakingChanges", new { fromVersion = "10.7.0" });

        StringAssert.Contains(from107, "## Gone in 11.0.0");
        Assert.DoesNotContain("## Gone in 10.7.0", from107, "An app on 10.7.0 already made the 10.7.0 changes.");
        Assert.DoesNotContain("`IsEnabled`", from107, "IsEnabled went in 10.7.0.");
        StringAssert.Contains(from107, "`IsLoading`");

        // A tag as it is written in a project file or a git tag reads the same.
        var tagged = await CallAsync("GetBitBlazorUIBreakingChanges", new { fromVersion = "v-10.7.0" });
        Assert.AreEqual(from107, tagged);

        var from105 = await CallAsync("GetBitBlazorUIBreakingChanges", new { fromVersion = "10.5.0" });
        StringAssert.Contains(from105, "## Gone in 10.6.0");
        StringAssert.Contains(from105, "`BitProModal`");
    }

    [TestMethod]
    public async Task Names_narrow_the_answer_and_take_the_forms_a_build_error_writes()
    {
        using var scope = Assert.Scope();

        var answer = await CallAsync("GetBitBlazorUIBreakingChanges", new { names = "BitButton.IsLoading, <BitProModal>, IsCheckedChanged" });

        var members = AllRows(answer, MembersHeading).Select(r => r[0]).ToArray();
        var types = AllRows(answer, TypesHeading).Select(r => r[0]).ToArray();

        CollectionAssert.AreEquivalent(new[] { "IsLoading", "IsChecked" }, members);
        CollectionAssert.AreEquivalent(new[] { "BitProModal" }, types);
    }

    [TestMethod]
    public async Task A_miss_and_a_bad_version_are_answers_rather_than_failures()
    {
        using var scope = Assert.Scope();

        var miss = await CallAsync("GetBitBlazorUIBreakingChanges", new { names = "Disabled" });
        StringAssert.Contains(miss, "is a name bit BlazorUI removed or renamed", "A current name is not reported as one that is gone.");
        StringAssert.Contains(miss, "GetBitBlazorUIComponent");

        var bad = await CallAsync("GetBitBlazorUIBreakingChanges", new { fromVersion = "latest" });
        StringAssert.Contains(bad, "is not a version");

        var none = await CallAsync("GetBitBlazorUIBreakingChanges", new { fromVersion = "99.0.0" });
        StringAssert.Contains(none, "was removed or renamed after 99.0.0");
    }

    [TestMethod]
    public async Task The_upgrade_prompt_completes_its_version_and_names_the_tool()
    {
        var completion = await Mcp.CompleteAsync(new PromptReference { Name = "upgrade-bit-blazorui" }, "fromVersion", "10.", cancellationToken: Ct);

        CollectionAssert.Contains(completion.Completion.Values.ToArray(), "10.5.0");

        var prompt = await Mcp.GetPromptAsync("upgrade-bit-blazorui", new Dictionary<string, object?> { ["fromVersion"] = "10.6.2" }, cancellationToken: Ct);
        var text = string.Join("\n", prompt.Messages.Select(m => m.Content).OfType<TextContentBlock>().Select(b => b.Text));

        StringAssert.Contains(text, "10.6.2");
        StringAssert.Contains(text, "GetBitBlazorUIBreakingChanges");
        StringAssert.Contains(text, "BITBC001");
    }

    /// <summary>The rows of every table under a header row - the answer has one per version - as raw cell text.</summary>
    private static string[][] AllRows(string markdown, string header)
    {
        var rows = new List<string[]>();
        var rest = markdown.Replace("\r\n", "\n", StringComparison.Ordinal);

        for (var at = rest.IndexOf(header, StringComparison.Ordinal); at >= 0; at = rest.IndexOf(header, StringComparison.Ordinal))
        {
            rest = rest[at..];
            rows.AddRange(TableRows(rest, header));
            rest = rest[header.Length..];
        }

        Assert.IsNotEmpty(rows, $"No '{header}' table in the answer.");
        return [.. rows];
    }
}

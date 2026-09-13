using System.Reflection;
using Bit.Brouter.Demo.Server.Controllers;

namespace Bit.Brouter.Tests.Mcp;

/// <summary>
/// The two pieces of the controller that decide what a caller's input and the server's output are
/// allowed to be: how a pasted route table is split into templates, and where a long answer is cut.
/// Both are exercised over the wire as well; here they are pinned down at the edges, where the
/// inputs are the ones nobody sends on purpose.
/// </summary>
[TestClass]
public class McpControllerInternalsTests
{
    [TestMethod]
    public void A_route_table_is_split_on_newlines_semicolons_and_top_level_commas()
    {
        CollectionAssert.AreEqual(new[] { "/a", "/b", "/c", "/d" }, McpController.SplitTemplates("/a\n/b;/c,/d"));
        CollectionAssert.AreEqual(new[] { "/a", "/b" }, McpController.SplitTemplates("/a\r\n/b"));
    }

    [TestMethod]
    public void A_comma_inside_a_constraint_is_part_of_the_template_rather_than_a_separator()
    {
        // Splitting there would tear one valid template into two invalid ones, and then report the
        // wreckage as the caller's mistake.
        CollectionAssert.AreEqual(new[] { "/a/{id:range(1,10)}" }, McpController.SplitTemplates("/a/{id:range(1,10)}"));
        CollectionAssert.AreEqual(new[] { "/a/{code:length(2,4)}" }, McpController.SplitTemplates("/a/{code:length(2,4)}"));

        CollectionAssert.AreEqual(
            new[] { "/a/{id:range(1,10)}", "/b/{n:min(1)}" },
            McpController.SplitTemplates("/a/{id:range(1,10)}, /b/{n:min(1)}"));
    }

    [TestMethod]
    public void Blank_lines_and_stray_whitespace_are_not_templates()
    {
        CollectionAssert.AreEqual(new[] { "/a", "/b" }, McpController.SplitTemplates("\n\n  /a  \n\n   \n/b\n;;\n"));
        CollectionAssert.AreEqual(Array.Empty<string>(), McpController.SplitTemplates(""));
        CollectionAssert.AreEqual(Array.Empty<string>(), McpController.SplitTemplates(null));
    }

    [TestMethod]
    public void An_unbalanced_brace_does_not_swallow_the_rest_of_the_table()
    {
        // The depth counter never goes below zero, so a malformed template is one bad entry rather
        // than a table that silently stops being split.
        CollectionAssert.AreEqual(new[] { "/a/{id", "/b" }, McpController.SplitTemplates("/a/{id\n/b"));
        CollectionAssert.AreEqual(new[] { "/a}", "/b" }, McpController.SplitTemplates("/a},/b"));
    }

    [TestMethod]
    public void A_pasted_file_is_cut_at_the_number_of_templates_worth_answering_about()
    {
        var pasted = string.Join('\n', Enumerable.Range(0, 1_000).Select(index => $"/r{index}"));

        var templates = McpController.SplitTemplates(pasted);

        Assert.AreEqual(200, templates.Length);
        Assert.AreEqual("/r0", templates[0], "The cut should keep the beginning, which is the part a caller meant to send.");

        // What was cut has to remain knowable, or the answer cannot own up to being partial.
        McpController.SplitTemplates(pasted, out var submitted);

        Assert.AreEqual(1_000, submitted);

        McpController.SplitTemplates("/a\n/b", out var few);

        Assert.AreEqual(2, few);
    }

    [TestMethod]
    public void A_long_answer_is_never_cut_between_the_halves_of_a_surrogate_pair()
    {
        // Half a surrogate pair is not text: a client that re-encodes the answer turns it into a
        // replacement character or rejects it outright.
        var truncate = typeof(McpController).GetMethod("Truncate", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.IsNotNull(truncate, "McpController.Truncate has been renamed or removed; nothing is capping the answers any more.");

        // One leading ASCII character puts every emoji on an odd index, so the run straddles the
        // 40,000-character boundary rather than ending on it: a cut taken there without looking
        // keeps the high half of a pair and drops the low half that completes it.
        var text = "-" + string.Concat(Enumerable.Repeat("\U0001F600", 30_000));
        var cut = (string)truncate.Invoke(null, [text])!;

        var body = cut[..cut.IndexOf("\n\n[truncated", StringComparison.Ordinal)];

        Assert.IsFalse(char.IsHighSurrogate(body[^1]), "The answer was cut in the middle of a surrogate pair.");
        Assert.AreEqual("-" + string.Concat(Enumerable.Repeat("\U0001F600", (body.Length - 1) / 2)), body);
    }

    [TestMethod]
    public void A_short_answer_is_handed_over_untouched()
    {
        var truncate = typeof(McpController).GetMethod("Truncate", BindingFlags.NonPublic | BindingFlags.Static)!;

        Assert.AreEqual("short", truncate.Invoke(null, ["short"]));
    }
}

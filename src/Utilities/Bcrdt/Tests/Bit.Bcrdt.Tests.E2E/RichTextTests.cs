using Bit.Bcrdt.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Bcrdt.Tests.E2E;

/// <summary>
/// Rich text: formatting a selection on one replica marks the same characters on the other, without changing the
/// plain text either editor holds, and two marks made while the replicas cannot see each other both apply where they
/// overlap.
/// </summary>
[TestClass]
public sealed class RichTextTests : BcrdtTest
{
    private const string Sample = "The quick brown fox jumps over the lazy dog.";

    [TestMethod]
    public async Task A_selection_formatted_on_one_replica_is_formatted_on_the_other()
    {
        var page = await OpenReplicasAsync("/rich-text", 2);
        ILocator a = Replica(page, "Replica A"), b = Replica(page, "Replica B");
        await Assertions.Expect(Editor(a)).ToHaveValueAsync(Sample);

        await SelectAsync(Editor(a), "quick");
        await Assertions.Expect(a.Locator(".rich-selection")).ToContainTextAsync("“quick”");
        await Button(a, "Bold").ClickAsync();

        // The mark reaches B, and the characters stay as they were in both editors.
        await Assertions.Expect(b.Locator(".rich-preview .rich-bold")).ToHaveTextAsync("quick");
        await Assertions.Expect(a.Locator(".rich-preview .rich-bold")).ToHaveTextAsync("quick");
        await Assertions.Expect(Editor(b)).ToHaveValueAsync(Sample);
        await Assertions.Expect(b.Locator(".rich-delta")).ToContainTextAsync("insert \"quick\" { bold: true }");

        // Removing the formatting is an edit like any other.
        await SelectAsync(Editor(b), "quick");
        await Button(b, "Remove formatting").ClickAsync();
        await Assertions.Expect(a.Locator(".rich-preview .rich-bold")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Overlapping_marks_made_offline_both_apply_after_the_merge()
    {
        var page = await OpenReplicasAsync("/rich-text", 2);
        ILocator a = Replica(page, "Replica A"), b = Replica(page, "Replica B");
        await Assertions.Expect(Editor(b)).ToHaveValueAsync(Sample);

        await GoOfflineAsync(b);

        await SelectAsync(Editor(a), "quick brown");
        await Button(a, "Bold").ClickAsync();
        await SelectAsync(Editor(b), "brown fox");
        await Button(b, "Italic").ClickAsync();

        await GoOnlineAsync(b);

        // "brown" carries both marks on both replicas: each mark is an attribute of its own.
        foreach (var panel in new[] { a, b })
        {
            await Assertions.Expect(panel.Locator(".rich-preview .rich-bold.rich-italic")).ToHaveTextAsync("brown");
            await Assertions.Expect(panel.Locator(".rich-preview .rich-bold").First).ToHaveTextAsync("quick ");
            await Assertions.Expect(panel.Locator(".rich-preview .rich-italic").Last).ToHaveTextAsync(" fox");
        }
    }

    private static ILocator Button(ILocator panel, string name) =>
        panel.Locator(".toolbar").GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    /// <summary>Selects the first occurrence of <paramref name="words"/> in the editor, the way a person would.</summary>
    private static async Task SelectAsync(ILocator editor, string words)
    {
        await editor.ClickAsync();
        await editor.EvaluateAsync(
            "(element, words) => { const start = element.value.indexOf(words); element.setSelectionRange(start, start + words.length); element.dispatchEvent(new Event('select')); }",
            words);
    }
}

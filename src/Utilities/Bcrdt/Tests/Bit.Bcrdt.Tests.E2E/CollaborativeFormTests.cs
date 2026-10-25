using Bit.Bcrdt.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Bcrdt.Tests.E2E;

/// <summary>
/// The collaborative form: plain map fields that follow each other across replicas and resolve a concurrent write
/// last-writer-wins, next to a nested set whose concurrent additions are all kept and a nested text field whose
/// concurrent edits both survive.
/// </summary>
[TestClass]
public sealed class CollaborativeFormTests : BcrdtTest
{
    [TestMethod]
    public async Task Fields_edited_on_one_replica_follow_on_the_other()
    {
        var page = await OpenReplicasAsync("/form", 2);
        ILocator a = Replica(page, "Replica A"), b = Replica(page, "Replica B");

        await Field(a, "Title").PressSequentiallyAsync("Quarterly plan");
        await Field(b, "Owner").PressSequentiallyAsync("Ada");
        await Field(a, "Status").SelectOptionAsync("In review");
        await TypeAtEndAsync(Field(b, "Notes"), "Notes typed in B.");

        await Assertions.Expect(Field(b, "Title")).ToHaveValueAsync("Quarterly plan");
        await Assertions.Expect(Field(a, "Owner")).ToHaveValueAsync("Ada");
        await Assertions.Expect(Field(b, "Status")).ToHaveValueAsync("In review");
        await Assertions.Expect(Field(a, "Notes")).ToHaveValueAsync("Notes typed in B.");

        await ExpectSameProjectionsAsync(page);
    }

    [TestMethod]
    public async Task Concurrent_offline_edits_keep_one_title_and_both_notes()
    {
        var page = await OpenReplicasAsync("/form", 2);
        ILocator a = Replica(page, "Replica A"), b = Replica(page, "Replica B");

        // The notes field is created by each replica on its first render, and on an empty document both do, so the
        // two nested texts race last-writer-wins (as the page's own comment says). A note typed in A and shown by B
        // proves that race is settled - both edit the one text that won - before the replicas are separated.
        await TypeAtEndAsync(Field(a, "Notes"), "Shared notes.");
        await Assertions.Expect(Field(b, "Notes")).ToHaveValueAsync("Shared notes.");

        await GoOfflineAsync(b);

        await Field(a, "Title").PressSequentiallyAsync("Title from A");
        await TypeAtEndAsync(Field(a, "Notes"), " Note from A.");
        await Field(b, "Title").PressSequentiallyAsync("Title from B");
        await TypeAtEndAsync(Field(b, "Notes"), " Note from B.");

        await GoOnlineAsync(b);

        // The notes are a nested text, so both sentences merge into it on both replicas...
        await ExpectConvergedAsync([Field(a, "Notes"), Field(b, "Notes")], "Shared notes.", " Note from A.", " Note from B.");

        // ...while the title is a plain map entry: one of the two values wins outright, the same one on both sides.
        var title = "";
        await EventuallyAsync(async () =>
        {
            var titles = await Task.WhenAll(Field(a, "Title").InputValueAsync(), Field(b, "Title").InputValueAsync());
            title = titles[0];
            return titles[0] == titles[1];
        }, () => "both replicas show the same title");
        Assert.IsTrue(title is "Title from A" or "Title from B", $"the title is neither of the two written: \"{title}\"");

        await ExpectSameProjectionsAsync(page);
    }

    [TestMethod]
    public async Task Labels_ticked_on_both_replicas_while_apart_are_all_kept()
    {
        var page = await OpenReplicasAsync("/form", 2);
        ILocator a = Replica(page, "Replica A"), b = Replica(page, "Replica B");

        // Like the notes, the set of labels is created by each replica on its first render and races once; a label
        // ticked in A and shown by B proves the race is settled before the replicas are separated.
        await Label(a, "customer").CheckAsync();
        await Assertions.Expect(Label(b, "customer")).ToBeCheckedAsync();

        await GoOfflineAsync(b);
        await Label(a, "urgent").CheckAsync();
        await Label(b, "blocked").CheckAsync();
        await GoOnlineAsync(b);

        // A set merges per member: both new labels are kept on both replicas, next to the shared one.
        foreach (var panel in new[] { a, b })
        {
            await Assertions.Expect(Label(panel, "urgent")).ToBeCheckedAsync();
            await Assertions.Expect(Label(panel, "blocked")).ToBeCheckedAsync();
            await Assertions.Expect(Label(panel, "customer")).ToBeCheckedAsync();
            await Assertions.Expect(Label(panel, "internal")).Not.ToBeCheckedAsync();
        }

        await ExpectSameProjectionsAsync(page);
    }

    /// <summary>The checkbox of the label called <paramref name="name"/> in the panel's labels field.</summary>
    private static ILocator Label(ILocator panel, string name) =>
        panel.Locator("fieldset.form-labels").GetByLabel(name, new() { Exact = true });

    /// <summary>The input, menu or text area of the form field labelled <paramref name="label"/>.</summary>
    private static ILocator Field(ILocator panel, string label) => panel.Locator("label.field")
        .Filter(new() { Has = panel.Page.Locator(".field-label", new() { HasTextString = label }) })
        .Locator("input, select, textarea");
}

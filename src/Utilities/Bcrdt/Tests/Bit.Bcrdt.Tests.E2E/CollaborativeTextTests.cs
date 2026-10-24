using Bit.Bcrdt.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Bcrdt.Tests.E2E;

/// <summary>
/// The headline demo, three independent replicas of one text side by side: what is typed into one reaches the
/// other two, a replica taken offline diverges and is reconciled when it comes back without anybody losing a word,
/// and a deletion merges like any other edit.
/// </summary>
[TestClass]
public sealed class CollaborativeTextTests : BcrdtTest
{
    [TestMethod]
    public async Task Typing_in_one_replica_appears_in_the_other_two()
    {
        var page = await OpenReplicasAsync("/text", 3);
        ILocator a = Replica(page, "Replica A"), b = Replica(page, "Replica B"), c = Replica(page, "Replica C");

        // Each replica sees the other two in its presence list: three separate participants, not three views.
        await Assertions.Expect(PresenceList(a)).ToContainTextAsync("Replica B");
        await Assertions.Expect(PresenceList(a)).ToContainTextAsync("Replica C");
        await Assertions.Expect(PresenceList(c)).ToContainTextAsync("Replica A");

        await TypeAtEndAsync(Editor(a), "Hello from A.");

        await Assertions.Expect(Editor(b)).ToHaveValueAsync("Hello from A.");
        await Assertions.Expect(Editor(c)).ToHaveValueAsync("Hello from A.");

        // And the other way round, from the middle replica.
        await TypeAtEndAsync(Editor(b), " And B.");

        await Assertions.Expect(Editor(a)).ToHaveValueAsync("Hello from A. And B.");
        await Assertions.Expect(Editor(c)).ToHaveValueAsync("Hello from A. And B.");
        await Assertions.Expect(a.Locator(".panel-foot .metric").First).ToHaveTextAsync("20 characters");
        await Assertions.Expect(page.Locator("main")).ToContainTextAsync("report identical content right now");
    }

    [TestMethod]
    public async Task A_replica_taken_offline_diverges_and_converges_with_both_edits_when_it_comes_back()
    {
        var page = await OpenReplicasAsync("/text", 3);
        ILocator a = Replica(page, "Replica A"), b = Replica(page, "Replica B"), c = Replica(page, "Replica C");
        ILocator[] editors = [Editor(a), Editor(b), Editor(c)];

        await TypeAtEndAsync(Editor(a), "Shared start.");
        await ExpectConvergedAsync(editors, "Shared start.");

        await GoOfflineAsync(a);
        await Assertions.Expect(a.Locator(".metric-warn")).ToContainTextAsync("editing offline");

        // Going offline withdraws A's presence, so the replicas still online see it leave.
        await Assertions.Expect(PresenceList(b)).Not.ToContainTextAsync("Replica A");

        await TypeAtEndAsync(Editor(a), " Offline edit from A.");
        await TypeAtEndAsync(Editor(b), " Online edit from B.");

        // B and C are still connected to each other, so C follows B, while A keeps only its own edit.
        await Assertions.Expect(Editor(c)).ToHaveValueAsync("Shared start. Online edit from B.");
        await Assertions.Expect(Editor(a)).ToHaveValueAsync("Shared start. Offline edit from A.");
        await Assertions.Expect(page.Locator("main")).ToContainTextAsync("report differing content right now");

        await GoOnlineAsync(a);

        // The handshake sends each side what the other is missing: every replica ends up with both sentences, in
        // the same order everywhere, and each sentence in one piece rather than interleaved with the other.
        var merged = await ExpectConvergedAsync(editors, "Shared start.", " Offline edit from A.", " Online edit from B.");
        Assert.IsTrue(merged.StartsWith("Shared start.", StringComparison.Ordinal), $"the shared start moved: \"{merged}\"");
        Assert.AreEqual("Shared start. Offline edit from A. Online edit from B.".Length, merged.Length, $"characters were lost or duplicated: \"{merged}\"");

        await Assertions.Expect(page.Locator("main")).ToContainTextAsync("report identical content right now");
        await Assertions.Expect(PresenceList(b)).ToContainTextAsync("Replica A");
    }

    [TestMethod]
    public async Task Clearing_the_document_from_replica_a_clears_all_three()
    {
        var page = await OpenReplicasAsync("/text", 3);
        ILocator a = Replica(page, "Replica A"), b = Replica(page, "Replica B"), c = Replica(page, "Replica C");
        ILocator[] editors = [Editor(a), Editor(b), Editor(c)];

        await TypeAtEndAsync(Editor(a), "Written by A.");
        await TypeAtEndAsync(Editor(c), " Written by C.");
        await ExpectConvergedAsync(editors, "Written by A.", " Written by C.");

        await page.GetByRole(AriaRole.Button, new() { Name = "Clear the document (from Replica A)" }).ClickAsync();

        foreach (var editor in editors) await Assertions.Expect(editor).ToHaveValueAsync("");
        foreach (var panel in new[] { a, b, c }) await Assertions.Expect(panel.Locator(".panel-foot .metric").First).ToHaveTextAsync("0 characters");

        // The cleared document is still a live one: typing into it after the deletion reaches the others.
        await TypeAtEndAsync(Editor(b), "After the clear.");
        await ExpectConvergedAsync(editors, "After the clear.");
        Assert.AreEqual("After the clear.", await Editor(a).InputValueAsync());
    }

    [TestMethod]
    public async Task A_paste_too_large_to_report_as_a_change_reaches_the_others_and_typing_carries_on()
    {
        var page = await OpenReplicasAsync("/text", 3);
        ILocator a = Replica(page, "Replica A"), b = Replica(page, "Replica B"), c = Replica(page, "Replica C");
        ILocator[] editors = [Editor(a), Editor(b), Editor(c)];

        await TypeAtEndAsync(Editor(a), "Before. ");
        await ExpectConvergedAsync(editors, "Before. ");

        // Far more than the module reports as a change - over Blazor Server's message limit, too - so .NET reads the
        // control's value back as a stream instead. Every character of it arrives.
        string pasted = string.Concat(Enumerable.Repeat("A long pasted paragraph, 😀 included. ", 500));
        await Editor(a).EvaluateAsync(
            "(element, text) => { element.value += text; element.dispatchEvent(new Event('input', { bubbles: true })); }", pasted);

        string merged = await ExpectConvergedAsync(editors, "Before. ", pasted);
        Assert.AreEqual("Before. " + pasted, merged);

        // Keystrokes after it are ordinary changes again, on top of the value read back.
        await TypeAtEndAsync(Editor(b), " After.");
        merged = await ExpectConvergedAsync(editors, " After.");
        Assert.AreEqual("Before. " + pasted + " After.", merged);
    }
}

using Bit.Bcrdt.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Bcrdt.Tests.E2E;

/// <summary>
/// Selective undo across replicas: each replica's stack holds only its own edits, so an undo removes this replica's
/// sentence from both panels and leaves the other replica's later sentence alone, and a redo brings it back.
/// </summary>
[TestClass]
public sealed class UndoTests : BcrdtTest
{
    [TestMethod]
    public async Task Undo_on_one_replica_removes_only_its_own_edit_on_both_and_redo_restores_it()
    {
        var page = await OpenReplicasAsync("/undo", 2);
        ILocator a = Replica(page, "Replica A"), b = Replica(page, "Replica B");

        await TypeAtEndAsync(Editor(a), "Sentence from A.");
        await Assertions.Expect(Editor(b)).ToHaveValueAsync("Sentence from A.");

        await TypeAtEndAsync(Editor(b), " Sentence from B.");
        await Assertions.Expect(Editor(a)).ToHaveValueAsync("Sentence from A. Sentence from B.");

        // A run of keystrokes is one step, and a remote edit is never on the receiving replica's stack.
        await Assertions.Expect(a.Locator(".panel-foot")).ToContainTextAsync("1 undoable step(s)");
        await Assertions.Expect(b.Locator(".panel-foot")).ToContainTextAsync("1 undoable step(s)");

        await Button(a, "Undo").ClickAsync();

        // The undo is an ordinary edit, so it reaches B; B's sentence, typed later, is untouched on both.
        await Assertions.Expect(Editor(a)).ToHaveValueAsync(" Sentence from B.");
        await Assertions.Expect(Editor(b)).ToHaveValueAsync(" Sentence from B.");
        await Assertions.Expect(Button(a, "Undo")).ToBeDisabledAsync();
        await Assertions.Expect(Button(a, "Redo")).ToBeEnabledAsync();
        await Assertions.Expect(Button(b, "Undo")).ToBeEnabledAsync();

        await Button(a, "Redo").ClickAsync();

        // The redo puts A's sentence back where it was, ahead of B's, on both.
        await Assertions.Expect(Editor(a)).ToHaveValueAsync("Sentence from A. Sentence from B.");
        await Assertions.Expect(Editor(b)).ToHaveValueAsync("Sentence from A. Sentence from B.");

        // B's own undo removes B's sentence only, from both.
        await Button(b, "Undo").ClickAsync();
        await Assertions.Expect(Editor(a)).ToHaveValueAsync("Sentence from A.");
        await Assertions.Expect(Editor(b)).ToHaveValueAsync("Sentence from A.");
    }

    private static ILocator Button(ILocator panel, string name) =>
        panel.Locator(".toolbar").GetByRole(AriaRole.Button, new() { Name = name, Exact = true });
}

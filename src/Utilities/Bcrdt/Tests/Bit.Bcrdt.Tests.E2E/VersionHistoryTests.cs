using Bit.Bcrdt.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Bcrdt.Tests.E2E;

/// <summary>
/// Version history: a version saved on one replica reaches the other, rebuilds the text as it was when it was saved,
/// reaches a replica that was offline together with everything it names, and restores as an edit both replicas see.
/// </summary>
[TestClass]
public sealed class VersionHistoryTests : BcrdtTest
{
    [TestMethod]
    public async Task A_saved_version_previews_and_restores_on_either_replica()
    {
        var page = await OpenReplicasAsync("/history", 2);
        ILocator a = Replica(page, "Replica A"), b = Replica(page, "Replica B");

        await TypeAtEndAsync(Editor(a), "First draft.");
        await Assertions.Expect(Editor(b)).ToHaveValueAsync("First draft.");
        await a.GetByRole(AriaRole.Button, new() { Name = "Save a version" }).ClickAsync();

        // The version is stored in the document, so B lists it too.
        await Assertions.Expect(Versions(b)).ToHaveCountAsync(1);
        await Assertions.Expect(Versions(b).First).ToContainTextAsync("Saved by Replica A");

        await TypeAtEndAsync(Editor(b), " Then more.");
        await Assertions.Expect(Editor(a)).ToHaveValueAsync("First draft. Then more.");

        // B rebuilds the text as it was when A saved it, from the document's own history.
        await Versions(b).First.GetByRole(AriaRole.Button, new() { Name = "Preview" }).ClickAsync();
        await Assertions.Expect(b.Locator(".history-preview")).ToHaveTextAsync("First draft.");

        // Restoring is an edit: both replicas hold the old text again, and the version list is untouched.
        await Versions(b).First.GetByRole(AriaRole.Button, new() { Name = "Restore" }).ClickAsync();
        await Assertions.Expect(Editor(a)).ToHaveValueAsync("First draft.");
        await Assertions.Expect(Editor(b)).ToHaveValueAsync("First draft.");
        await Assertions.Expect(Versions(a)).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task A_version_saved_while_a_replica_is_offline_reaches_it_with_what_it_names()
    {
        var page = await OpenReplicasAsync("/history", 2);
        ILocator a = Replica(page, "Replica A"), b = Replica(page, "Replica B");

        await TypeAtEndAsync(Editor(a), "Shared.");
        await Assertions.Expect(Editor(b)).ToHaveValueAsync("Shared.");

        // A saves a version of text B has not received; B, offline, lists nothing yet.
        await GoOfflineAsync(b);
        await TypeAtEndAsync(Editor(a), " Only in A.");
        await a.GetByRole(AriaRole.Button, new() { Name = "Save a version" }).ClickAsync();
        await Assertions.Expect(Versions(b)).ToHaveCountAsync(0);

        await GoOnlineAsync(b);
        await Assertions.Expect(Versions(b)).ToHaveCountAsync(1);
        await Versions(b).First.GetByRole(AriaRole.Button, new() { Name = "Preview" }).ClickAsync();
        await Assertions.Expect(b.Locator(".history-preview")).ToHaveTextAsync("Shared. Only in A.");
    }

    private static ILocator Versions(ILocator panel) => panel.Locator("li.history-item");
}

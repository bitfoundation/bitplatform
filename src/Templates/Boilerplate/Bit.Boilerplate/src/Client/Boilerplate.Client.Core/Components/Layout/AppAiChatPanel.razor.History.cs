using Boilerplate.Shared.Features.Chatbot;

namespace Boilerplate.Client.Core.Components.Layout;

// Keeps the conversation on the device so closing the app doesn't start it over. IndexedDB rather than local storage:
// answers are markdown and the history is unbounded, while local storage has ~5MB per origin and writes on the UI
// thread. Nothing here is uploaded - what leaves the device is the history each turn already sends (See
// StartChatRequest.ChatMessagesHistory).
// To store it on the server instead, only RestoreHistory, RememberMessage and ForgetHistory need a new body; the rest
// of the panel calls nothing else.
public partial class AppAiChatPanel
{
    /// <summary>The database the conversation lives in. AppDiagnosticModal deletes it by this name.</summary>
    internal const string HistoryDatabase = "ai-chat";

    private const string MessagesStore = "messages";
    private const string OwnerStore = "owner";
    private const string OwnerKey = "userId";

    /// <summary>Stands in for the user id while signed out, so "nothing stored" differs from "stored by nobody".</summary>
    private const string AnonymousOwner = "anonymous";

    /// <summary>
    /// Matches what the server keeps of what it's sent (See <see cref="StartChatRequest.MaxChatMessagesHistory"/>);
    /// anything more would only be read off the disk to be dropped.
    /// </summary>
    private const int MaxStoredMessages = StartChatRequest.MaxChatMessagesHistory;

    [AutoInject] private IndexedDb indexedDb = default!;

    private IndexedDbHandle? historyDb;

    /// <summary>The panel shows its loading indicator while the conversation is read back.</summary>
    private bool isRestoringHistory;

    /// <summary>
    /// Who the stored conversation belongs to. Null means not read yet rather than owned by nobody (signed out is
    /// <see cref="AnonymousOwner"/>), so no change of hands may be acted on before <see cref="RestoreHistory"/> fills
    /// it in.
    /// </summary>
    private string? historyOwner;

    /// <summary>
    /// Who is at the device now; signed out counts as an owner. Read from the authentication state rather than the
    /// cascading CurrentUser, which is null until MainLayout has fetched the profile over http - taking that at face
    /// value would throw a signed-in user's own conversation away on every rebuild.
    /// </summary>
    private async Task<string> CurrentHistoryOwner()
    {
        var user = (await AuthenticationStateTask).User;

        return user.IsAuthenticated() ? user.GetUserId().ToString() : AnonymousOwner;
    }

    /// <summary>
    /// Reads the conversation back. Called after the first render, since prerendering has no JS runtime and these
    /// calls return defaults there rather than failing.
    /// </summary>
    private async Task RestoreHistory()
    {
        isRestoringHistory = true;
        StateHasChanged();

        await TryHistory("restore", async () =>
        {
            if (await indexedDb.IsSupported() is false) return;

            historyDb = await indexedDb.Open(HistoryDatabase, version: 1, stores:
            [
                new() { Name = MessagesStore, AutoIncrement = true },
                new() { Name = OwnerStore }
            ], onVersionChange: CloseHistory);

            historyOwner = await historyDb.Get<string>(OwnerStore, OwnerKey);

            if (historyOwner is null)
            {
                await ClaimHistory();
                return;
            }

            // Only the writer is shown it; anyone else at this device - including nobody, after a sign out - gets a
            // fresh panel.
            if (historyOwner != await CurrentHistoryOwner())
            {
                await ForgetHistory();
                return;
            }

            var stored = await historyDb.GetPage<string>(MessagesStore,
                                                         direction: IndexedDbCursorDirection.Previous,
                                                         take: MaxStoredMessages);

            // The reverse cursor hands back the newest first; the greeting the panel just wrote stays above them.
            chatMessages.AddRange(stored.Reverse()
                                        .Select(record => JsonSerializer.Deserialize(record.Value, JsonSerializerOptions.GetTypeInfo<AiChatMessage>()))
                                        .OfType<AiChatMessage>());
        });

        isRestoringHistory = false;
        StateHasChanged();

        await SyncHistoryOwner(); // Picks up a hand over that landed while this was running.
    }

    /// <summary>
    /// Follows the conversation from one owner to the next: signing in part way through is the same person carrying
    /// on, any other change of hands is somebody else. Does nothing until <see cref="RestoreHistory"/> has said who
    /// the owner is, since cascading parameters land while it runs (a culture change flips CurrentDir via
    /// SOFT_RESTART).
    /// </summary>
    private Task SyncHistoryOwner() => TryHistory("hand over", async () =>
    {
        // Also not mid-restore: the owner is read before the messages are, so a hand over in between would clear the
        // store and then let the restore put the previous owner's conversation back on screen.
        if (historyDb is null || historyOwner is null || isRestoringHistory) return;

        var owner = await CurrentHistoryOwner();

        if (historyOwner == owner) return;

        if (historyOwner is AnonymousOwner)
        {
            await ClaimHistory();
            return;
        }

        await ClearChat(); // Forgets the stored one too, under the current owner's name.
    });

    /// <summary>Appends one settled message. Nothing on screen waits for it.</summary>
    private Task RememberMessage(AiChatMessage message) => TryHistory("add to", async () =>
    {
        if (historyDb is null) return;

        var key = await historyDb.Put(MessagesStore, JsonSerializer.Serialize(message, JsonSerializerOptions.GetTypeInfo<AiChatMessage>()));

        // Keys only count up, so anything this far below the newest is past what a restore would read.
        if (key.TryGetInt64(out var newest) && newest > MaxStoredMessages)
        {
            await historyDb.Delete(MessagesStore, IndexedDbKeyRange.UpperBound(newest - MaxStoredMessages));
        }
    });

    /// <summary>Throws the stored conversation away, leaving it owned by whoever is at the device now.</summary>
    private Task ForgetHistory() => TryHistory("forget", async () =>
    {
        if (historyDb is null) return;

        historyOwner = await CurrentHistoryOwner();

        // One batch, so the records can never outlive the name they were stored under.
        await historyDb.Transact([IndexedDbOperation.Clear(MessagesStore),
                                  IndexedDbOperation.Put(OwnerStore, historyOwner, OwnerKey)]);
    });

    /// <summary>
    /// Lets the database go when something else needs it to itself - another tab, or the diagnostic modal deleting
    /// it, which stays blocked while this connection is open. The conversation carries on unstored.
    /// </summary>
    private void CloseHistory()
    {
        var closing = historyDb;

        historyDb = null;

        _ = closing?.DisposeAsync();
    }

    private async Task ClaimHistory()
    {
        historyOwner = await CurrentHistoryOwner();

        await historyDb!.Put(OwnerStore, historyOwner, OwnerKey);
    }

    /// <summary>
    /// Storing the conversation is a convenience, so a browser that refuses - private mode, no quota, storage blocked
    /// - must still leave a working panel. The first refusal ends it for this session.
    /// </summary>
    private async Task TryHistory(string what, Func<Task> operation)
    {
        try
        {
            await operation();
        }
        catch (Exception exp)
        {
            historyDb = null;

            logger.LogWarning(exp, "Failed to {Operation} the conversation stored on this device.", what);
        }
    }
}

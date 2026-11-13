using Boilerplate.Shared.Features.Chatbot;

namespace Boilerplate.Client.Core.Components.Layout.AiChat;

// Keeps the conversation on the device so closing the app doesn't start it over: one record, the conversation as
// Bit.BlazorUI.AI writes it (BitAiConversationSerializer), its images by address rather than by their bytes. IndexedDB
// rather than local storage: answers are markdown, and local storage has ~5MB per origin and writes on the UI thread.
// Nothing here is uploaded - what leaves the device is the conversation each question already carries.
// To store it on the server instead, only RestoreHistory, SaveHistory and ForgetHistory need a new body.
public partial class AppAiChat
{
    /// <summary>The database the conversation lives in. AppDiagnosticModal deletes it by this name.</summary>
    internal const string HistoryDatabase = "ai-chat-conversation";

    private const string ConversationStore = "conversation";
    private const string ConversationKey = "current";
    private const string OwnerStore = "owner";
    private const string OwnerKey = "userId";

    /// <summary>Stands in for the user id while signed out, so "nothing stored" differs from "stored by nobody".</summary>
    private const string AnonymousOwner = "anonymous";

    [AutoInject] private IndexedDb indexedDb = default!;

    private IndexedDbHandle? historyDb;

    /// <summary>
    /// Who the stored conversation belongs to. Null means not read yet rather than owned by nobody (signed out is
    /// <see cref="AnonymousOwner"/>), so no change of hands may be acted on before <see cref="RestoreHistory"/> fills it in.
    /// </summary>
    private string? historyOwner;

    private string? conversationOwner;

    /// <summary>
    /// Who is at the device now; signed out counts as an owner. Read from the authentication state rather than the
    /// cascading CurrentUser, which is null until MainLayout has fetched the profile over http - taking that at face value
    /// would throw a signed-in user's own conversation away on every rebuild.
    /// </summary>
    private async Task<string> CurrentHistoryOwner()
    {
        var user = (await AuthenticationStateTask).User;

        return user.IsAuthenticated() ? user.GetUserId().ToString() : AnonymousOwner;
    }

    /// <summary>The conversation kept on this device, when it's the current user's; a new one otherwise.</summary>
    private async Task<BitAiConversation?> RestoreHistory()
    {
        BitAiConversation? conversation = null;

        await TryHistory("restore", async () =>
        {
            if (await indexedDb.IsSupported() is false) return;

            historyDb = await indexedDb.Open(HistoryDatabase, version: 1, stores:
            [
                new() { Name = ConversationStore },
                new() { Name = OwnerStore }
            ], onVersionChange: CloseHistory);

            historyOwner = await historyDb.Get<string>(OwnerStore, OwnerKey);

            if (historyOwner is null)
            {
                await ClaimHistory();
                return;
            }

            // Only the writer is shown it; anyone else at this device - including nobody, after a sign out - gets a
            // fresh one.
            if (historyOwner != await CurrentHistoryOwner())
            {
                await ForgetHistory();
                return;
            }

            if (await historyDb.Get<string>(ConversationStore, ConversationKey) is { Length: > 0 } json)
            {
                conversation = BitAiConversationSerializer.FromJson(json);
            }
        });

        conversationOwner = historyOwner ?? await CurrentHistoryOwner();

        return conversation;
    }

    /// <summary>
    /// Follows the conversation from one owner to the next: signing in part way through is the same person carrying on,
    /// any other change of hands is somebody else. Does nothing until <see cref="RestoreHistory"/> has said who the
    /// owner is.
    /// </summary>
    private Task SyncHistoryOwner() => TryHistory("hand over", async () =>
    {
        if (conversationOwner is null) return;

        var owner = await CurrentHistoryOwner();

        if (conversationOwner == owner) return;

        var previousOwner = conversationOwner;
        conversationOwner = owner;

        if (previousOwner is AnonymousOwner)
        {
            if (historyDb is not null)
            {
                await ClaimHistory();
            }

            return;
        }

        try
        {
            await Clear(); // Forgets the stored one too, under the current owner's name.
        }
        catch
        {
            if (conversationOwner == owner)
            {
                conversationOwner = previousOwner;
            }

            throw;
        }
    });

    /// <summary>Keeps the conversation as it is now, its newest <see cref="AiChatProtocol.MaxHistory"/> messages: anything older would only be read off the disk to be dropped.</summary>
    private Task SaveHistory() => TryHistory("save", async () =>
    {
        if (historyDb is null || session is null) return;

        // Another tab handed the stored conversation to someone else, so this one must not be written under their name.
        if (await historyDb.Get<string>(OwnerStore, OwnerKey) != historyOwner)
        {
            CloseHistory();
            return;
        }

        var snapshot = BitAiConversationSerializer.ToSnapshot(session.Conversation);
        snapshot = snapshot with { Messages = [.. snapshot.Messages.TakeLast(AiChatProtocol.MaxHistory)] };

        await historyDb.Put(ConversationStore, BitAiConversationSerializer.ToJson(snapshot), ConversationKey);
    });

    /// <summary>Throws the stored conversation away, leaving it owned by whoever is at the device now.</summary>
    private Task ForgetHistory() => TryHistory("forget", async () =>
    {
        if (historyDb is null) return;

        historyOwner = await CurrentHistoryOwner();

        // One batch, so the conversation can never outlive the name it was stored under.
        await historyDb.Transact([IndexedDbOperation.Clear(ConversationStore),
                                  IndexedDbOperation.Put(OwnerStore, historyOwner, OwnerKey)]);
    });

    /// <summary>
    /// Lets the database go when something else needs it to itself - another tab, or the diagnostic modal deleting it,
    /// which stays blocked while this connection is open. The conversation carries on unstored.
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
    /// Storing the conversation is a convenience, so a browser that refuses - private mode, no quota, storage blocked -
    /// must still leave a working chat. The first refusal ends it for this session.
    /// </summary>
    private async Task TryHistory(string what, Func<Task> operation)
    {
        try
        {
            await operation();
        }
        catch (Exception exp)
        {
            // Closed rather than just dropped: an operation can fail with the database open, and a connection left open
            // blocks deleteDatabase - which is how AppDiagnosticModal deletes this one.
            CloseHistory();

            logger.LogWarning(exp, "Failed to {Operation} the conversation stored on this device.", what);
        }
    }
}

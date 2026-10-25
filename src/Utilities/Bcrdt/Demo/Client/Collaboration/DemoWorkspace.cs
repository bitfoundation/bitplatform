namespace Bit.Bcrdt.Demo.Client.Collaboration;

/// <summary>
/// Creates the replica sets the demo pages display, and keeps their storage between visits.
/// </summary>
/// <remarks>
/// A store per replica, not one shared store. Sharing would quietly undermine the demos: a replica
/// taken offline would still see its peers' edits arrive through storage, which is not what offline
/// means. Each replica's store stands for that participant's own machine.
/// </remarks>
public sealed class DemoWorkspace(BcrdtInMemoryNetwork network)
{
    private readonly Dictionary<string, IBcrdtDocumentStore> _stores = new(StringComparer.Ordinal);

    /// <summary>The in-memory network every replica connects through.</summary>
    public BcrdtInMemoryNetwork Network { get; } = network;

    /// <summary>How many replicas are currently connected to a document's room.</summary>
    public int ConnectedCount(string documentId) => Network.MemberCount(documentId);

    /// <summary>
    /// Opens one independent replica per label, all of the same document.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the heart of the whole demo, so it is worth being explicit about what it does and
    /// does not do. It does <b>not</b> hand several panels a single shared document - that would
    /// make the panels windows onto one object, every edit would appear everywhere instantly, and
    /// the demo would prove nothing at all. Instead each label gets a full, separate replica: its
    /// own collaboration service, its own document with its own client identifier, its own presence
    /// identity, its own private store and its own connection.
    /// </para>
    /// <para>
    /// So when text typed in one panel appears in another, it genuinely travelled: local edit,
    /// encoded delta, transport, decode, integrate, event, re-render. And when a panel is taken
    /// offline, its document really does diverge from its peers' and really is reconciled by the
    /// synchronization handshake when it comes back. The only thing the demo fakes is the network,
    /// and it fakes it with the library's own shipped in-memory transport.
    /// </para>
    /// </remarks>
    public Task<IReadOnlyList<DemoReplica>> OpenReplicasAsync(string documentId, params string[] labels) =>
        OpenReplicasAsync(documentId, keepHistory: false, labels);

    /// <summary>
    /// Opens one independent replica per label, all of the same document, optionally keeping every
    /// deleted block so the document's history can be restored from a <see cref="BcrdtSnapshot"/>.
    /// </summary>
    /// <param name="documentId">The document every replica is a copy of.</param>
    /// <param name="keepHistory">
    /// Whether the replicas are created with garbage collection off. Every replica of a document must
    /// agree, which is why it is decided here for all of them at once.
    /// </param>
    /// <param name="labels">One label per replica.</param>
    public async Task<IReadOnlyList<DemoReplica>> OpenReplicasAsync(string documentId, bool keepHistory, params string[] labels)
    {
        ArgumentException.ThrowIfNullOrEmpty(documentId);
        ArgumentNullException.ThrowIfNull(labels);

        List<DemoReplica> replicas = [];
        HashSet<string> usedColors = new(StringComparer.Ordinal);

        foreach (string label in labels)
        {
            BcrdtCollaboratorInfo user = CreateUser(documentId, label, usedColors);
            IBcrdtDocumentStore store = GetStore(documentId, label);

            replicas.Add(await DemoReplica
                .OpenAsync(Network, store, documentId, label, user, keepHistory)
                .ConfigureAwait(false));
        }

        return replicas;
    }

    private IBcrdtDocumentStore GetStore(string documentId, string label)
    {
        string key = $"{documentId}/{label}";
        if (_stores.TryGetValue(key, out IBcrdtDocumentStore? existing)) return existing;

        var created = new BcrdtInMemoryDocumentStore();
        _stores[key] = created;
        return created;
    }

    /// <summary>
    /// Builds a participant identity whose colour is distinct from the others on the page.
    /// </summary>
    /// <remarks>
    /// <see cref="BcrdtPresenceService.SuggestColor"/> hashes the user id into a fixed palette, which is
    /// the right behaviour - every participant independently agrees on everyone's colour with no
    /// coordination - but it means two ids can legitimately collide. The demo needs its panels to be
    /// told apart, so it perturbs the id it hashes until the colour is unused. The published id
    /// itself stays stable.
    /// </remarks>
    private static BcrdtCollaboratorInfo CreateUser(string documentId, string label, HashSet<string> usedColors)
    {
        string id = $"{documentId}/{label}";
        string color = BcrdtPresenceService.SuggestColor(id);

        for (int attempt = 1; !usedColors.Add(color) && attempt <= 16; attempt++)
        {
            color = BcrdtPresenceService.SuggestColor($"{id}#{attempt}");
        }

        return new BcrdtCollaboratorInfo(id, label, color);
    }
}

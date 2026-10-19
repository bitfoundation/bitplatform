using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Bit.Bcrdt.Demo.Server.Dtos;

namespace Bit.Bcrdt.Demo.Server.Services;

/// <summary>
/// Decodes an update or a sync-protocol message with Bit.Bcrdt's own decoders and reports what is in it: the
/// envelope, the blocks each client contributed, the delete set, the state vector the update implies, presence
/// entries, and the size of every part - or, for a payload the library rejects, the
/// <see cref="BcrdtFormatException"/> it rejects it with.
/// <para>
/// Every read goes through the library's public decoders - <see cref="BcrdtSyncProtocol.Decode"/>,
/// <see cref="BcrdtUpdateOperations.DecodeBlocks"/>, <see cref="BcrdtUpdateOperations.DecodeDeleteSet"/>,
/// <see cref="BcrdtUpdateOperations.DecodeStateVector"/>, <see cref="BcrdtStateVector.Decode"/> and a
/// <see cref="BcrdtAwarenessStore"/> for presence - so what is reported valid is exactly what a replica would
/// accept, and the error is the one it would raise. Sizes are measured by re-encoding each part with the
/// library's own writers, which is how a payload the writers would not have produced shows up.
/// </para>
/// </summary>
public static class BcrdtUpdateInspector
{
    /// <summary>The most blocks the report lists; an update can hold tens of thousands.</summary>
    private const int MaxBlocks = 200;

    /// <summary>The longest payload, in characters of base64 or hex, the inspector reads.</summary>
    private const int MaxPayloadLength = 4 * 1024 * 1024;

    private const int PreviewLength = 60;

    public static readonly string[] ReadAs = ["auto", "update", "message"];

    public static BcrdtUpdateInspectionDto Inspect(string? payload, string? readAs)
    {
        var sample = string.IsNullOrWhiteSpace(payload);
        var mode = string.IsNullOrWhiteSpace(readAs) ? "auto" : readAs.Trim().ToLowerInvariant();

        if (ReadAs.Contains(mode) is false)
        {
            return Rejected("update", 0, null, $"'{readAs}' is not a way to read a payload. Use one of: {string.Join(", ", ReadAs)}.");
        }

        if (payload?.Length > MaxPayloadLength)
        {
            return Rejected("update", 0, null, $"The payload is {payload.Length:N0} characters long; the inspector reads at most {MaxPayloadLength:N0}.");
        }

        byte[] bytes;
        if (sample)
        {
            bytes = SampleUpdate();
        }
        else if (TryDecodeText(payload!, out var decoded, out var problem))
        {
            bytes = decoded;
        }
        else
        {
            return Rejected("update", 0, null, problem);
        }

        var base64 = Convert.ToBase64String(bytes);

        if (mode == "update") return WithSample(InspectUpdate(bytes, base64).Report, sample);
        if (mode == "message") return WithSample(InspectMessage(bytes, base64).Report, sample);

        // Neither shape can be told from the other by its first bytes: an update with one client starts 0x01, as
        // every message does, and a decoder that stops reading where its format ends accepts more than the library
        // ever writes - the sample update below is also a well-formed AwarenessUpdate. So both readings are tried,
        // and the one that accounts for every byte - re-encoded by the library's writers to exactly the length it
        // was given - wins over one that is merely accepted. A message wins a tie.
        var message = InspectMessage(bytes, base64);
        var update = InspectUpdate(bytes, base64);

        if (message.Report.Valid || update.Report.Valid)
        {
            var preferMessage = Rank(message) >= Rank(update);
            var (chosen, other) = preferMessage ? (message, update) : (update, message);

            var report = other.Report.Valid
                ? chosen.Report with
                {
                    Notes = [.. chosen.Report.Notes, $"These bytes also decode as a {(preferMessage ? "bare update" : $"{other.Report.Kind} message")}" +
                             $"{(other.Exact ? "" : ", though not one the library would have written")}. Pass readAs: \"{(preferMessage ? "update" : "message")}\" to read them that way."]
                }
                : chosen.Report;

            return WithSample(report, sample);
        }

        // Neither reading was accepted. The one whose envelope at least made sense says more about what went wrong.
        var envelope = bytes.Length >= 2 && bytes[0] == BcrdtSyncProtocol.Version && Enum.IsDefined((BcrdtSyncMessageKind)bytes[1]);

        var rejected = envelope ? message.Report : update.Report;

        return rejected with
        {
            Notes = [.. rejected.Notes, envelope
                ? $"Read as a bare update instead, it is rejected too: {update.Report.Error}"
                : $"Read as a protocol message instead, it is rejected too: {message.Report.Error}"]
        };
    }

    /// <summary>One way of reading the bytes, and whether the library's writers would have produced exactly them.</summary>
    private sealed record Reading(BcrdtUpdateInspectionDto Report, bool Exact);

    private static int Rank(Reading reading) => reading.Report.Valid ? reading.Exact ? 2 : 1 : 0;

    private static BcrdtUpdateInspectionDto WithSample(BcrdtUpdateInspectionDto report, bool sample)
    {
        if (sample is false) return report;

        return report with
        {
            Sample = true,
            Notes = ["No payload was passed, so this decoded a sample made by the real engine: a map key set, a text typed and partly deleted. " +
                     "Pass any update or message as base64 - what BcrdtDocument.Updated hands out, what a store holds, what crosses a transport - or as hex prefixed with 0x.",
                     .. report.Notes]
        };
    }

    /// <summary>The sample: a document with a map entry and a text that was typed and then partly deleted, encoded whole.</summary>
    private static byte[] SampleUpdate()
    {
        using var document = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });

        document.GetMap("meta").Set("title", "Q3 planning");

        var notes = document.GetText("notes");
        notes.Insert(0, "Agenda: budget");
        notes.Insert(notes.Count, ", hiring");
        notes.Delete(0, 8);

        return document.EncodeStateAsUpdate();
    }

    /// <summary>Base64 (standard or URL-safe, padded or not) or hex prefixed with 0x; whitespace anywhere is ignored.</summary>
    private static bool TryDecodeText(string text, out byte[] bytes, out string problem)
    {
        bytes = [];
        problem = string.Empty;

        var compact = string.Concat(text.Where(c => char.IsWhiteSpace(c) is false));

        if (compact.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                bytes = Convert.FromHexString(compact[2..]);
                return true;
            }
            catch (FormatException)
            {
                problem = "The payload starts with 0x but is not hex: two hex digits per byte, nothing else.";
                return false;
            }
        }

        var standard = compact.Replace('-', '+').Replace('_', '/');
        standard = standard.PadRight(standard.Length + (4 - standard.Length % 4) % 4, '=');

        try
        {
            bytes = Convert.FromBase64String(standard);
            return true;
        }
        catch (FormatException)
        {
            problem = "The payload is not base64. Pass the bytes as base64 (standard or URL-safe), or as hex prefixed with 0x.";
            return false;
        }
    }

    private static BcrdtUpdateInspectionDto Rejected(string kind, int total, string? base64, string error) => new()
    {
        Kind = kind,
        Valid = false,
        Error = error,
        Payload = base64,
        TotalBytes = total,
        Notes = ["A replica given these bytes would throw BcrdtFormatException and change nothing: decoding is all-or-nothing, so a malformed update leaves the document exactly as it was, and a provider drops the message."]
    };

    private static Reading InspectMessage(byte[] bytes, string base64)
    {
        BcrdtSyncMessage message;
        try
        {
            message = BcrdtSyncProtocol.Decode(bytes);
        }
        catch (BcrdtFormatException ex)
        {
            return new Reading(Rejected("message", bytes.Length, base64, ex.Message), false);
        }

        var kind = message.Kind.ToString();
        var payload = message.Payload;

        try
        {
            switch (message.Kind)
            {
                case BcrdtSyncMessageKind.Update or BcrdtSyncMessageKind.SyncStep2:
                {
                    var (update, exact) = InspectUpdate(payload.ToArray(), base64);
                    if (update.Valid is false) return new Reading(update with { Kind = kind, TotalBytes = bytes.Length, Version = bytes[0], PayloadBytes = payload.Length }, false);

                    string[] meaning = message.Kind == BcrdtSyncMessageKind.Update
                        ? ["An Update: one local commit, broadcast as it happened. A receiver applies it with the sender's provider as origin, so it is never echoed back."]
                        : ["A SyncStep2: the answer to a SyncStep1 (or a SyncStep1Reply) - everything the announcer was missing, plus the sender's whole delete set."];

                    return new Reading(update with { Kind = kind, TotalBytes = bytes.Length, Version = bytes[0], PayloadBytes = payload.Length, Notes = [.. meaning, .. update.Notes] }, exact);
                }

                case BcrdtSyncMessageKind.SyncStep1:
                {
                    var vector = BcrdtStateVector.Decode(payload);
                    var notes = new List<string>
                    {
                        "A SyncStep1: \"here is everything I have; send me what I am missing\". It opens every handshake, on every (re)connect. Every peer that hears it answers with a SyncStep2 - the blocks past this state vector, plus its whole delete set - and a SyncStep1Reply stating its own state vector."
                    };
                    var encoded = vector.Encode().Length;
                    AddTrailing(notes, payload.Length, encoded, "state vector");

                    return new Reading(Message(kind, bytes, payload.Length, notes) with { StateVector = Vector(vector) }, encoded == payload.Length);
                }

                case BcrdtSyncMessageKind.SyncStep1Reply:
                {
                    var (vector, digest) = BcrdtSyncProtocol.DecodeSyncStep1Reply(payload);
                    var notes = new List<string>
                    {
                        "A SyncStep1Reply: what the replier holds, after answering a SyncStep1. It is answered with a SyncStep2 only - never another reply, which is what ends the exchange - and only when the hearer holds something the replier lacks: blocks past this state vector, or deletions whose digest differs from this one. A state vector cannot show deletions; the digest is what reveals them."
                    };
                    if (digest == 0) notes.Add("The digest is zero - \"unknown\" - so the hearer always answers with a SyncStep2.");

                    var encoded = BcrdtSyncProtocol.EncodeSyncStep1Reply(vector, digest).Length - 2;
                    AddTrailing(notes, payload.Length, encoded, "state vector and digest");

                    return new Reading(Message(kind, bytes, payload.Length, notes) with { StateVector = Vector(vector), DeletionDigest = $"0x{digest:x16}" }, encoded == payload.Length);
                }

                case BcrdtSyncMessageKind.AwarenessUpdate:
                {
                    // Applied to a store of nobody's, so every entry in the message is reported as a peer's would be.
                    using var store = new BcrdtAwarenessStore(ulong.MaxValue);
                    store.ApplyUpdate(payload);

                    var entries = store.Entries.Values
                        .OrderBy(entry => entry.ClientId)
                        .Select(entry => new BcrdtAwarenessEntryDto
                        {
                            Client = entry.ClientId.ToString(CultureInfo.InvariantCulture),
                            Clock = entry.Clock,
                            State = entry.State is null ? null : BcrdtMergeLab.ToNode(entry.State)
                        })
                        .ToArray();

                    var notes = new List<string>
                    {
                        "An AwarenessUpdate: presence, which is not part of the document. Each entry replaces what a peer holds for that client when its clock is higher; an entry with no state is a client that left. Entries expire unless refreshed by the sender's heartbeat."
                    };

                    var encoded = store.EncodeUpdate([.. store.Entries.Keys]).Length;
                    AddTrailing(notes, payload.Length, encoded, "entries");

                    return new Reading(Message(kind, bytes, payload.Length, notes) with { Awareness = entries }, encoded == payload.Length);
                }

                case BcrdtSyncMessageKind.Auth:
                {
                    var (permitted, reason) = BcrdtSyncProtocol.DecodeAuth(payload);

                    var notes = new List<string>
                    {
                        permitted
                            ? "An Auth message permitting the peer."
                            : "An Auth message refusing the peer. A provider that receives one stops for good rather than retrying an answer that will not change."
                    };

                    var encoded = BcrdtSyncProtocol.EncodeAuth(permitted, reason).Length - 2;
                    AddTrailing(notes, payload.Length, encoded, "result and reason");

                    return new Reading(Message(kind, bytes, payload.Length, notes) with { Permitted = permitted, Reason = reason }, encoded == payload.Length);
                }

                default:
                {
                    var notes = new List<string>
                    {
                        message.Kind switch
                        {
                            BcrdtSyncMessageKind.AwarenessQuery => "An AwarenessQuery: \"tell me who is here\" - answered with an AwarenessUpdate of every online client.",
                            BcrdtSyncMessageKind.Ping => "A Ping: a liveness probe, answered with a Pong.",
                            _ => "A Pong: the answer to a Ping."
                        }
                    };
                    AddTrailing(notes, payload.Length, 0, "message with no payload");

                    return new Reading(Message(kind, bytes, payload.Length, notes), payload.Length == 0);
                }
            }
        }
        catch (BcrdtFormatException ex)
        {
            return new Reading(Rejected(kind, bytes.Length, base64, ex.Message) with { Version = bytes[0], PayloadBytes = payload.Length }, false);
        }
    }

    private static BcrdtUpdateInspectionDto Message(string kind, byte[] bytes, int payloadBytes, IEnumerable<string> notes) => new()
    {
        Kind = kind,
        Valid = true,
        Payload = Convert.ToBase64String(bytes),
        TotalBytes = bytes.Length,
        Version = bytes[0],
        PayloadBytes = payloadBytes,
        Notes = [.. notes]
    };

    private static void AddTrailing(List<string> notes, int length, int expected, string what)
    {
        if (length > expected)
        {
            notes.Add($"{length - expected} byte(s) follow the {what} and are ignored by the decoder - nothing the library writes leaves them.");
        }
    }

    private static Reading InspectUpdate(byte[] bytes, string base64)
    {
        IReadOnlyDictionary<ulong, IReadOnlyList<BcrdtAbstractStruct>> blocks;
        BcrdtDeleteSet deleteSet;
        BcrdtStateVector implied;

        try
        {
            blocks = BcrdtUpdateOperations.DecodeBlocks(bytes);
            deleteSet = BcrdtUpdateOperations.DecodeDeleteSet(bytes);
            implied = BcrdtUpdateOperations.DecodeStateVector(bytes);
        }
        catch (BcrdtFormatException ex)
        {
            return new Reading(Rejected("update", bytes.Length, base64, ex.Message), false);
        }

        var listed = 0;
        var truncated = false;

        // The groups as the library writes them - descending by client id, the order integration breaks ties in.
        var clients = blocks.OrderByDescending(group => group.Key).Select(group =>
        {
            var structs = group.Value;
            var shown = new List<BcrdtUpdateBlockDto>();

            foreach (var block in structs)
            {
                if (listed == MaxBlocks)
                {
                    truncated = true;
                    break;
                }

                shown.Add(Block(block));
                listed++;
            }

            return new BcrdtUpdateClientDto
            {
                Client = Id(group.Key),
                FromClock = structs[0].Id.Clock,
                ToClock = structs[^1].Id.Clock + structs[^1].Length,
                Blocks = [.. shown],
                BlockCount = structs.Count,
                Bytes = Measure((ref BcrdtBufferWriter writer) =>
                {
                    writer.WriteVarUInt((ulong)structs.Count);
                    writer.WriteVarUInt(group.Key);
                    writer.WriteVarUInt((ulong)structs[0].Id.Clock);
                }) + structs.Sum(BlockSize)
            };
        }).ToArray();

        var blockBytes = Measure((ref BcrdtBufferWriter writer) => writer.WriteVarUInt((ulong)blocks.Count)) + clients.Sum(client => client.Bytes);
        var deleteSetBytes = deleteSet.Encode().Length;

        var ranges = deleteSet.Clients
            .OrderByDescending(client => client.Key)
            .SelectMany(client => client.Value.Select(range => new BcrdtDeleteRangeDto { Client = Id(client.Key), Clock = range.Clock, Length = range.Length }))
            .ToArray();

        return new Reading(new BcrdtUpdateInspectionDto
        {
            Kind = "update",
            Valid = true,
            Payload = base64,
            TotalBytes = bytes.Length,
            BlockBytes = blockBytes,
            DeleteSetBytes = deleteSetBytes,
            Clients = clients,
            BlocksTruncated = truncated ? true : null,
            DeleteSet = ranges,
            StateVector = Vector(implied),
            Notes = UpdateNotes(bytes.Length, blockBytes, deleteSetBytes, blocks, ranges, implied, truncated)
        }, bytes.Length == blockBytes + deleteSetBytes);
    }

    private static string[] UpdateNotes(int total, int blockBytes, int deleteSetBytes, IReadOnlyDictionary<ulong, IReadOnlyList<BcrdtAbstractStruct>> blocks, BcrdtDeleteRangeDto[] ranges, BcrdtStateVector implied, bool truncated)
    {
        var notes = new List<string>();

        if (blocks.Count == 0 && ranges.Length == 0)
        {
            notes.Add("An empty update: no blocks and no deletions - what a replica sends a peer that already holds everything it has, and what applying changes nothing.");
        }
        else
        {
            notes.Add($"{blocks.Values.Sum(list => list.Count)} block(s) from {blocks.Count} client(s), and {ranges.Length} deleted range(s). Applying it is idempotent and order-insensitive: content a replica already holds is skipped by its clock, and a block whose origin has not arrived waits in the pending queue until it does.");
        }

        foreach (var (client, list) in blocks.OrderByDescending(group => group.Key))
        {
            var from = list[0].Id.Clock;
            if (from > 0)
            {
                notes.Add($"Client {client}'s blocks start at clock {from}, so this is a delta: a replica without that client's clocks below {from} holds them pending until it has them.");
            }

            if (list.Any(block => block is BcrdtSkip))
            {
                notes.Add($"Client {client}'s run has Skip ranges - parts the sender left out, usually from merging updates that do not cover each other. The receiver waits for them rather than reading this as a complete history.");
            }
        }

        if (blocks.Values.Any(list => list.Any(block => block is BcrdtGarbageCollected)))
        {
            notes.Add("GarbageCollected ranges are deleted content whose payload was dropped by garbage collection: they keep their clocks, so references into them still resolve, and carry nothing else.");
        }

        if (ranges.Length > 0)
        {
            notes.Add("The delete set names the deleted clock ranges per client; it is sent whole with every sync response, because a deletion does not advance a state vector and re-sending the set is how a lost one is repaired.");
        }

        if (implied.Count > 0 || blocks.Count > 0)
        {
            notes.Add("stateVector is what the update implies: per client, how far it holds an unbroken history from clock 0. A store or relay can answer a sync request from it - BcrdtUpdateOperations.Diff(update, remoteStateVector) - without building a document.");
        }

        if (total != blockBytes + deleteSetBytes)
        {
            notes.Add($"The parts re-encode to {blockBytes + deleteSetBytes} bytes, not {total}: the payload carries {Math.Abs(total - blockBytes - deleteSetBytes)} byte(s) the library's writers would not have produced - trailing bytes, or numbers encoded longer than they need to be. The decoder accepts it all the same.");
        }

        if (truncated)
        {
            notes.Add($"Only the first {MaxBlocks} blocks are listed; each client's blockCount and bytes still cover all of them.");
        }

        return [.. notes];
    }

    private static BcrdtUpdateBlockDto Block(BcrdtAbstractStruct block)
    {
        if (block is not BcrdtItem item)
        {
            return new BcrdtUpdateBlockDto
            {
                Clock = block.Id.Clock,
                Length = block.Length,
                Content = block is BcrdtSkip ? "Skip" : "GarbageCollected",
                Bytes = BlockSize(block)
            };
        }

        return new BcrdtUpdateBlockDto
        {
            Clock = item.Id.Clock,
            Length = item.Length,
            Content = item.Content.Ref.ToString(),
            Origin = item.Origin is { } origin ? Id(origin) : null,
            RightOrigin = item.RightOrigin is { } right ? Id(right) : null,
            Parent = item.Parent switch
            {
                string root => $"root '{root}'",
                BcrdtItemId container => $"the type held by {Id(container)}",
                _ => null
            },
            Key = item.ParentKey,
            Preview = Preview(item.Content),
            Bytes = BlockSize(item)
        };
    }

    private static string? Preview(IBcrdtContent content) => content switch
    {
        BcrdtContentString text => Shorten(JsonSerializer.Serialize(text.Text)),
        BcrdtContentAny values => Shorten(BcrdtMergeLab.ToNode(values.GetContent())?.ToJsonString() ?? "[]"),
        BcrdtContentType type => $"a nested {type.Type.Kind}",
        BcrdtContentFormat mark => mark.Value is null ? $"end of '{mark.Key}'" : $"{mark.Key} = {Shorten(BcrdtMergeLab.ToNode(mark.Value)?.ToJsonString() ?? "null")}",
        BcrdtContentEmbed embed => Shorten(BcrdtMergeLab.ToNode(embed.Value)?.ToJsonString() ?? "null"),
        BcrdtContentBinary binary => $"{binary.Data.Length} bytes",
        BcrdtContentDeleted => "deleted content, dropped",
        _ => null
    };

    private static string Shorten(string text) => text.Length <= PreviewLength ? text : $"{text[..(PreviewLength - 3)]}...";

    private static int BlockSize(BcrdtAbstractStruct block) => Measure((ref BcrdtBufferWriter writer) => block.Write(ref writer, 0));

    private delegate void WriteAction(ref BcrdtBufferWriter writer);

    /// <summary>How many bytes the library's own writer spends on something.</summary>
    private static int Measure(WriteAction write)
    {
        var writer = new BcrdtBufferWriter(64);
        try
        {
            write(ref writer);
            return writer.Length;
        }
        finally
        {
            writer.Dispose();
        }
    }

    private static Dictionary<string, long> Vector(BcrdtStateVector vector) =>
        vector.OrderByDescending(entry => entry.Key).ToDictionary(entry => Id(entry.Key), entry => entry.Value, StringComparer.Ordinal);

    private static string Id(ulong client) => client.ToString(CultureInfo.InvariantCulture);

    private static string Id(BcrdtItemId id) => id.ToString();
}

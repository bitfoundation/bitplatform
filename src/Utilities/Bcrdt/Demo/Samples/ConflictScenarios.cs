using System.Text.Json;
using Bit.Bcrdt;

namespace Bit.Bcrdt.Demo.Client.Samples;

/// <summary>
/// The conflict lab's eight scenarios on the engine alone: two replicas and no network, every update handed over by
/// hand.
/// </summary>
/// <remarks>
/// No service, no transport and no browser - which is also what a test of a merge looks like. Call
/// <see cref="RunAll"/> from a console app or a test.
/// </remarks>
public static class ConflictScenarios
{
    public static void RunAll()
    {
        Run("Concurrent insertion at the same position",
            a => a.GetText("body").Insert(0, "[from A]"),
            b => b.GetText("body").Insert(0, "[from B]"));

        Run("Concurrent deletion of the same item",
            a => a.GetArray("items").Delete(0),
            b => b.GetArray("items").Delete(0));

        Run("Concurrent write to the same map key",
            a => a.GetMap("meta").Set("shared", "written by A"),
            b => b.GetMap("meta").Set("shared", "written by B"));

        Run("Concurrent writes to different map keys",
            a => a.GetMap("meta").Set("only-a", "set by A"),
            b => b.GetMap("meta").Set("only-b", "set by B"));

        Run("One replica edits text while the other deletes it",
            a => a.GetText("body").Insert(a.GetText("body").Count, "[appended by A]"),
            b => b.GetText("body").Delete(0, b.GetText("body").Count));

        Run("Concurrent insertion into the same list position",
            a => a.GetArray("items").Insert(0, "inserted by A"),
            b => b.GetArray("items").Insert(0, "inserted by B"));

        Run("Concurrent addition of the same tag",
            a => a.GetSet("tags").Add("urgent"),
            b => b.GetSet("tags").Add("urgent"));

        Run("One replica removes a tag while the other re-adds it",
            a => a.GetSet("tags").Remove("seed tag"),
            b =>
            {
                b.GetSet("tags").Remove("seed tag");
                b.GetSet("tags").Add("seed tag");
            });
    }

    private static void Run(string title, Action<BcrdtDocument> editA, Action<BcrdtDocument> editB)
    {
        using BcrdtDocument a = Replica();
        using BcrdtDocument b = Replica();

        // 1. Seed. Written by A alone and delivered to B, so both start from one shared starting point
        //    rather than two look-alike ones.
        a.Transact(_ =>
        {
            a.GetText("body").Insert(0, "seed text");
            a.GetArray("items").Push("seed item");
            a.GetSet("tags").Add("seed tag");
        });
        b.ApplyUpdate(a.EncodeStateAsUpdate());

        // 2 and 3. Isolate, and edit both: neither edit has reached the other replica, so they are concurrent.
        editA(a);
        editB(b);

        // 4. Reconnect - what the handshake does: each side sends what the other's state vector lacks.
        byte[] toB = a.EncodeStateAsUpdate(b.GetStateVector());
        byte[] toA = b.EncodeStateAsUpdate(a.GetStateVector());
        a.ApplyUpdate(toA);
        b.ApplyUpdate(toB);

        // 5. Compare. The same updates make the same state, whatever order they arrived in.
        string json = JsonSerializer.Serialize(a.ToJson());
        Console.WriteLine($"{title}: {json} (converged: {json == JsonSerializer.Serialize(b.ToJson())})");
    }

    // A root's kind is fixed by the first call that names it, so every replica names all four up front.
    private static BcrdtDocument Replica()
    {
        var replica = new BcrdtDocument();
        replica.GetText("body");
        replica.GetArray("items");
        replica.GetMap("meta");
        replica.GetSet("tags");
        return replica;
    }
}

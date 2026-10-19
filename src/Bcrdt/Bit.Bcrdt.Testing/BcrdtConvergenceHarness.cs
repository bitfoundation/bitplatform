using System.Collections;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Bit.Bcrdt.Testing;

/// <summary>Raised when replicas that should have converged did not.</summary>
public sealed class BcrdtConvergenceException(string message) : Exception(message);

/// <summary>
/// Checks the property the whole system exists to provide.
/// </summary>
/// <remarks>
/// The convergence law is: replicas that have received the same set of updates hold the same state,
/// regardless of the order they received them in. Almost every correctness test in the suite
/// ultimately asserts that, so it is worth stating once, precisely, with a failure message good
/// enough to debug from.
/// </remarks>
public static class BcrdtConvergenceHarness
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    private static readonly MethodInfo GetRootMethod =
        typeof(BcrdtDocument).GetMethod(nameof(BcrdtDocument.GetRoot))!;

    /// <summary>Asserts that every replica holds the same content.</summary>
    /// <remarks>
    /// A root that one replica has declared - <c>GetText("body")</c>, say - and another holds only as
    /// content received before declaring it is projected the same way on both, by declaring it on the
    /// second as well. Without that, an emptied text field on one side and the same field, still
    /// untyped, on the other would render differently despite holding identical state.
    /// </remarks>
    /// <exception cref="BcrdtConvergenceException">They do not.</exception>
    public static void AssertConverged(params BcrdtDocument[] documents)
    {
        ArgumentNullException.ThrowIfNull(documents);
        if (documents.Length < 2) return;

        AlignRootKinds(documents);

        string reference = Describe(documents[0]);
        for (int i = 1; i < documents.Length; i++)
        {
            string candidate = Describe(documents[i]);
            if (string.Equals(reference, candidate, StringComparison.Ordinal)) continue;

            throw new BcrdtConvergenceException(
                $"Replica 0 and replica {i} did not converge.{Environment.NewLine}" +
                $"--- replica 0 ---{Environment.NewLine}{reference}{Environment.NewLine}" +
                $"--- replica {i} ---{Environment.NewLine}{candidate}");
        }
    }

    /// <summary>Asserts that every peer on a network holds the same content.</summary>
    public static void AssertConverged(BcrdtSimulatedNetwork network)
    {
        ArgumentNullException.ThrowIfNull(network);
        AssertConverged([.. network.Peers.Select(peer => peer.Doc)]);
    }

    /// <summary>
    /// Asserts that replicas agree on the replicated state itself, not merely on how it renders.
    /// </summary>
    /// <remarks>
    /// Compares the state vectors (which blocks each replica holds), the delete sets (which of them
    /// are deleted) and the content. Two replicas can render identically while disagreeing on
    /// deletions - an item deleted on one and never deleted on the other, whose content happens to
    /// be hidden or collected - and a divergence like that is a real bug: it will eventually produce
    /// different results under some future concurrent edit. A state vector alone cannot see it,
    /// because a deletion does not advance one.
    /// </remarks>
    /// <exception cref="BcrdtConvergenceException">They differ.</exception>
    public static void AssertStructurallyIdentical(params BcrdtDocument[] documents)
    {
        ArgumentNullException.ThrowIfNull(documents);
        if (documents.Length < 2) return;

        for (int i = 1; i < documents.Length; i++)
        {
            if (!documents[0].GetStateVector().StructurallyEquals(documents[i].GetStateVector()))
            {
                throw new BcrdtConvergenceException(
                    $"Replica 0 and replica {i} hold different state vectors: " +
                    $"{documents[0].GetStateVector()} versus {documents[i].GetStateVector()}.");
            }

            string referenceDeletions = DescribeDeleteSet(documents[0].GetDeleteSet());
            string candidateDeletions = DescribeDeleteSet(documents[i].GetDeleteSet());
            if (!string.Equals(referenceDeletions, candidateDeletions, StringComparison.Ordinal))
            {
                throw new BcrdtConvergenceException(
                    $"Replica 0 and replica {i} hold different delete sets: " +
                    $"{referenceDeletions} versus {candidateDeletions}.");
            }
        }

        AssertConverged(documents);
    }

    /// <summary>Renders a document in a form suitable for comparison and for a failure message.</summary>
    /// <remarks>
    /// Roots that hold nothing are left out. Whether a replica has declared an empty root is a fact
    /// about the code that ran against it, not about the replicated state, so including them would
    /// make two identical replicas compare unequal.
    /// </remarks>
    public static string Describe(BcrdtDocument doc)
    {
        ArgumentNullException.ThrowIfNull(doc);

        var builder = new StringBuilder();
        foreach (string name in doc.RootTypes.Keys.OrderBy(key => key, StringComparer.Ordinal))
        {
            object projection = doc.RootTypes[name].ToJson();
            if (IsEmpty(projection)) continue;

            builder.Append(name).Append(": ");
            builder.AppendLine(JsonSerializer.Serialize(projection, Json));
        }
        return builder.ToString();
    }

    private static bool IsEmpty(object projection) => projection switch
    {
        string text => text.Length == 0,
        ICollection collection => collection.Count == 0,
        _ => false,
    };

    private static string DescribeDeleteSet(BcrdtDeleteSet deleteSet) =>
        string.Join(
            "; ",
            deleteSet.Clients
                .Where(pair => pair.Value.Count > 0)
                .OrderBy(pair => pair.Key)
                .Select(pair => $"{pair.Key}: [{string.Join(", ", pair.Value.Select(range => $"{range.Clock}+{range.Length}"))}]"));

    /// <summary>Declares, on every replica, each root under the kind any replica has declared it as.</summary>
    private static void AlignRootKinds(BcrdtDocument[] documents)
    {
        Dictionary<string, Type> declared = new(StringComparer.Ordinal);
        foreach (BcrdtDocument doc in documents)
        {
            foreach ((string name, BcrdtAbstractType type) in doc.RootTypes)
            {
                if (type is not BcrdtUntypedType) declared.TryAdd(name, type.GetType());
            }
        }

        foreach (BcrdtDocument doc in documents)
        {
            foreach ((string name, Type kind) in declared)
            {
                if (!doc.RootTypes.TryGetValue(name, out BcrdtAbstractType? existing) || existing is not BcrdtUntypedType)
                {
                    continue;
                }

                try
                {
                    GetRootMethod.MakeGenericMethod(kind).Invoke(doc, [name]);
                }
                catch (Exception ex) when (ex is TargetInvocationException or ArgumentException)
                {
                    // A kind that cannot adopt this content is itself a divergence; leave the root as
                    // it is and let the comparison report it.
                }
            }
        }
    }
}

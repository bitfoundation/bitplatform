using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using Bit.Butil;
using Microsoft.JSInterop;

namespace ButilManualTests;

/// <summary>
/// Captures the members Bit.Butil reaches by reflection at runtime, so a trimmed publish can be checked
/// against an untrimmed capture of the same thing.
/// </summary>
/// <remarks>
/// The reflection-based registration only settles who gets <b>registered</b>. Two other things inside the
/// library are resolved by name at runtime and would fail silently, in the browser, if the trimmer removed
/// them - and neither shows up as a missing service:
/// <list type="bullet">
/// <item><b>[JSInvokable] callbacks.</b> JS dispatches these by method name through a
/// <c>DotNetObjectReference</c>, including ones on internal types the consumer never names -
/// <c>DomEventsInterop</c>, the observer interops, <c>IndexedDbHandle</c>. Nothing in a consumer's code
/// references them.</item>
/// <item><b>JSON payload types.</b> The DTOs and option objects crossing the interop boundary are
/// (de)serialized by <c>System.Text.Json</c> reflecting over their constructors and properties, so a
/// trimmed-away property turns into a silently null field rather than an error.</item>
/// </list>
/// Both are meant to be covered by annotations already in the library - <c>DotNetObjectReference.Create</c>
/// preserves public methods, and the <c>Invoke&lt;T&gt;</c> overloads annotate <c>T</c> with
/// <c>LinkerFlags.JsonSerialized</c>. This class is what verifies that claim on real output rather than
/// taking it on trust.
/// </remarks>
internal sealed record TypeContract(string TypeName, bool IsCallbackTarget, bool IsPayload, int PublicConstructors, string[] JSInvokableMethods, string[] PublicProperties)
{
    public string Serialize()
        => string.Join('|',
            TypeName,
            IsCallbackTarget ? "J" : "-",
            IsPayload ? "P" : "-",
            PublicConstructors.ToString(),
            string.Join(',', JSInvokableMethods),
            string.Join(',', PublicProperties));

    public static TypeContract? Deserialize(string line)
    {
        var parts = line.Split('|');
        if (parts.Length != 6) return null;

        return new TypeContract(
            parts[0],
            parts[1] == "J",
            parts[2] == "P",
            int.TryParse(parts[3], out var constructors) ? constructors : 0,
            SplitList(parts[4]),
            SplitList(parts[5]));
    }

    private static string[] SplitList(string value)
        => value.Length == 0 ? [] : value.Split(',');
}

internal static class InteropContract
{
    private const BindingFlags PublicMembers = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    /// <summary>
    /// Builds the contract for an assembly: every type carrying <c>[JSInvokable]</c> methods, plus the
    /// payload types the harness genuinely round-trips and everything nested inside them.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Measuring what the trimmer left behind is this harness's job.")]
    [UnconditionalSuppressMessage("Trimming", "IL2065", Justification = "Measuring what the trimmer left behind is this harness's job.")]
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Measuring what the trimmer left behind is this harness's job.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Measuring what the trimmer left behind is this harness's job.")]
    public static TypeContract[] Capture(Assembly assembly, IEnumerable<Type> payloadRoots)
    {
        var payloads = CollectPayloadTypes(assembly, payloadRoots);
        var contracts = new List<TypeContract>();

        foreach (var type in assembly.GetTypes())
        {
            if (type.IsGenericTypeDefinition) continue;

            var jsInvokable = type
                .GetMethods(PublicMembers)
                .Where(method => method.IsDefined(typeof(JSInvokableAttribute), inherit: false))
                .Select(method => method.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            var isPayload = payloads.Contains(type);
            if (jsInvokable.Length == 0 && isPayload is false) continue;

            var properties = isPayload
                ? type.GetProperties(PublicMembers).Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray()
                : [];

            contracts.Add(new TypeContract(type.FullName!, jsInvokable.Length > 0, isPayload, type.GetConstructors().Length, jsInvokable, properties));
        }

        return [.. contracts.OrderBy(contract => contract.TypeName, StringComparer.Ordinal)];
    }

    /// <summary>
    /// Compares a capture taken from an untrimmed build against the trimmed assembly. Types the trimmer
    /// removed outright are skipped - that is the feature working. Only a type that <b>survived</b> while
    /// losing members it is reflected over is a defect.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Measuring what the trimmer left behind is this harness's job.")]
    [UnconditionalSuppressMessage("Trimming", "IL2065", Justification = "Measuring what the trimmer left behind is this harness's job.")]
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Measuring what the trimmer left behind is this harness's job.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Measuring what the trimmer left behind is this harness's job.")]
    public static (string[] CallbackTargets, string[] Payloads, int Removed, string[] Failures) Verify(Assembly assembly, TypeContract[] expected)
    {
        var actual = assembly.GetTypes().Where(type => type.FullName is not null).ToDictionary(type => type.FullName!, StringComparer.Ordinal);
        var failures = new List<string>();
        var callbackTargets = new List<string>();
        var payloads = new List<string>();
        var removedCount = 0;

        foreach (var contract in expected)
        {
            if (actual.TryGetValue(contract.TypeName, out var type) is false)
            {
                removedCount++;
                continue;
            }

            if (contract.IsCallbackTarget) callbackTargets.Add($"{type.Name}({contract.JSInvokableMethods.Length})");
            if (contract.IsPayload) payloads.Add($"{type.Name}({contract.PublicProperties.Length})");

            var methods = type.GetMethods(PublicMembers).Select(method => method.Name).ToHashSet(StringComparer.Ordinal);
            foreach (var missing in contract.JSInvokableMethods.Where(name => methods.Contains(name) is false))
            {
                failures.Add($"{contract.TypeName}.{missing} is [JSInvokable] but was trimmed away while its type survived - JS would dispatch to a method that no longer exists.");
            }

            if (contract.IsPayload)
            {
                var properties = type.GetProperties(PublicMembers).Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
                foreach (var missing in contract.PublicProperties.Where(name => properties.Contains(name) is false))
                {
                    failures.Add($"{contract.TypeName}.{missing} is part of a JSON interop payload but was trimmed away - it would deserialize as null/default instead of failing loudly.");
                }

                if (contract.PublicConstructors > 0 && type.GetConstructors().Length == 0)
                {
                    failures.Add($"{contract.TypeName} is a JSON interop payload that survived with no public constructor - deserialization would throw.");
                }
            }
        }

        return ([.. callbackTargets], [.. payloads], removedCount, [.. failures]);
    }

    public static void Write(string path, TypeContract[] contracts)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Bit.Butil interop contract captured from an untrimmed build. Regenerate with: dotnet run -c Release");
        foreach (var contract in contracts)
        {
            builder.AppendLine(contract.Serialize());
        }

        File.WriteAllText(path, builder.ToString());
    }

    public static TypeContract[]? Read(string path)
    {
        if (File.Exists(path) is false) return null;

        return [.. File.ReadAllLines(path)
            .Where(line => line.Length > 0 && line.StartsWith('#') is false)
            .Select(TypeContract.Deserialize)
            .Where(contract => contract is not null)
            .Select(contract => contract!)];
    }

    /// <summary>
    /// Expands the exercised payload roots to everything nested inside them, so a DTO that only appears as
    /// a property of another DTO - <c>GeolocationPosition.Coords</c>, say - is covered too.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2065", Justification = "Measuring what the trimmer left behind is this harness's job.")]
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Measuring what the trimmer left behind is this harness's job.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Measuring what the trimmer left behind is this harness's job.")]
    private static HashSet<Type> CollectPayloadTypes(Assembly assembly, IEnumerable<Type> payloadRoots)
    {
        var payloads = new HashSet<Type>();
        var queue = new Queue<Type>();

        foreach (var root in payloadRoots)
        {
            Enqueue(root);
        }

        while (queue.Count > 0)
        {
            var type = queue.Dequeue();
            foreach (var property in type.GetProperties(PublicMembers))
            {
                Enqueue(property.PropertyType);
            }
        }

        return payloads;

        void Enqueue(Type type)
        {
            foreach (var candidate in Unwrap(type))
            {
                if (candidate.Assembly != assembly) continue;
                if (candidate.IsEnum || candidate.IsGenericTypeDefinition) continue;
                if (payloads.Add(candidate) is false) continue;

                queue.Enqueue(candidate);
            }
        }
    }

    /// <summary>
    /// Peels the wrappers a Butil signature puts around its payload - <c>Task&lt;T&gt;</c>,
    /// <c>ValueTask&lt;T&gt;</c>, <c>Action&lt;T&gt;</c>, arrays, nullables, collections - and yields the
    /// types underneath, since those are what System.Text.Json actually sees.
    /// </summary>
    private static IEnumerable<Type> Unwrap(Type type)
    {
        if (type.IsByRef) type = type.GetElementType()!;

        if (type.IsArray)
        {
            foreach (var inner in Unwrap(type.GetElementType()!)) yield return inner;
            yield break;
        }

        if (type.IsGenericType)
        {
            foreach (var argument in type.GetGenericArguments())
            {
                foreach (var inner in Unwrap(argument)) yield return inner;
            }

            // A constructed generic can itself be a Butil payload (a record wrapping a T, say), so keep it
            // alongside its arguments rather than only descending into them.
            yield return type;
            yield break;
        }

        yield return type;
    }
}

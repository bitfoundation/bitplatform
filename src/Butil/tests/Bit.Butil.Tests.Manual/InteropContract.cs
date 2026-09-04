using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using Bit.Butil;
using Microsoft.JSInterop;

namespace ButilTests.Manual;

internal static class InteropContract
{
    /// <summary>
    /// Deliberately without <see cref="BindingFlags.DeclaredOnly"/>: interop sees a type whole. JS dispatches
    /// an inherited <c>[JSInvokable]</c> method by name just the same, and System.Text.Json serializes a
    /// payload's inherited properties along with its own - so both have to be captured and verified.
    /// </summary>
    private const BindingFlags PublicMembers = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;

    /// <summary>Marks the service-roster line in the manifest file.</summary>
    private const string ServicesPrefix = "@services|";

    /// <summary>
    /// Marks the completion record the manifest ends with, carrying the number of type contracts written.
    /// </summary>
    /// <remarks>
    /// A truncated or hand-edited manifest is the one corruption a reader cannot otherwise notice: the
    /// contracts that failed to parse are precisely the ones that would have been checked, so dropping
    /// them leaves the trimmed run reporting PASS having verified less than it claims - the same
    /// verified-nothing outcome a missing manifest is already treated as a failure to avoid.
    /// </remarks>
    private const string CountPrefix = "@count|";

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

            var jsInvokable = DispatchIdentifiers(type);

            var isPayload = payloads.Contains(type);
            if (jsInvokable.Length == 0 && isPayload is false) continue;

            var properties = isPayload
                ? type.GetProperties(PublicMembers).Select(property => property.Name).Distinct(StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal).ToArray()
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

            if (contract.IsCallbackTarget) callbackTargets.Add($"{type.Name}({contract.JSInvokableIdentifiers.Length})");
            if (contract.IsPayload) payloads.Add($"{type.Name}({contract.PublicProperties.Length})");

            // Compared against the identifiers that still resolve, not against surviving method names: a
            // method whose [JSInvokable] was stripped is just as unreachable from JS as one that is gone.
            var identifiers = DispatchIdentifiers(type).ToHashSet(StringComparer.Ordinal);
            foreach (var missing in contract.JSInvokableIdentifiers.Where(identifier => identifiers.Contains(identifier) is false))
            {
                failures.Add($"{contract.TypeName}.{missing} is [JSInvokable] but no longer resolves while its type survived - the method or its attribute was trimmed away, so JS would dispatch to nothing.");
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

    public static void Write(string path, InteropManifest manifest)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Bit.Butil interop contract captured from an untrimmed build. Regenerate with: dotnet run -c Release");
        builder.AppendLine(ServicesPrefix + string.Join(',', manifest.ServiceNames.OrderBy(name => name, StringComparer.Ordinal)));
        foreach (var contract in manifest.Types)
        {
            builder.AppendLine(contract.Serialize());
        }

        builder.AppendLine(CountPrefix + manifest.Types.Length);

        File.WriteAllText(path, builder.ToString());
    }

    /// <summary>
    /// Reads a manifest, or explains through <paramref name="error"/> why it cannot be trusted. Anything
    /// short of a whole, well-formed file is rejected rather than salvaged: a partly-read manifest checks
    /// a subset of the contract while still reporting PASS, which is worse than having no manifest at all.
    /// </summary>
    public static InteropManifest? Read(string path, out string? error)
    {
        error = null;

        if (File.Exists(path) is false)
        {
            error = $"no {path} in {Directory.GetCurrentDirectory()}";
            return null;
        }

        var lines = File.ReadAllLines(path).Where(line => line.Length > 0 && line.StartsWith('#') is false);

        string[]? services = null;
        int? declaredCount = null;
        var types = new List<TypeContract>();

        foreach (var line in lines)
        {
            if (line.StartsWith(ServicesPrefix, StringComparison.Ordinal))
            {
                services = line[ServicesPrefix.Length..].Split(',', StringSplitOptions.RemoveEmptyEntries);
                continue;
            }

            if (line.StartsWith(CountPrefix, StringComparison.Ordinal))
            {
                if (int.TryParse(line[CountPrefix.Length..], out var parsed) is false)
                {
                    error = $"{path} ends with a malformed completion record: '{line}'";
                    return null;
                }

                declaredCount = parsed;
                continue;
            }

            if (TypeContract.Deserialize(line) is not { } contract)
            {
                error = $"{path} has a malformed type contract: '{line}'";
                return null;
            }

            types.Add(contract);
        }

        if (services is null)
        {
            error = $"{path} carries no {ServicesPrefix} service roster";
            return null;
        }

        if (declaredCount is null)
        {
            error = $"{path} carries no {CountPrefix} completion record, so it was truncated mid-write";
            return null;
        }

        if (declaredCount != types.Count)
        {
            error = $"{path} declares {declaredCount} type contracts but {types.Count} were read, so it is incomplete";
            return null;
        }

        // A roster on its own is not a contract: it would verify none of the [JSInvokable] callbacks or
        // JSON payloads while every other check carried on reporting normally.
        if (types.Count == 0)
        {
            error = $"{path} records no type contracts at all";
            return null;
        }

        return new InteropManifest(services, [.. types]);
    }

    /// <summary>
    /// The names JS actually dispatches by: <see cref="JSInvokableAttribute.Identifier"/> where the
    /// attribute sets one - which most of Bit.Butil's callbacks do, as
    /// <c>[JSInvokable(InvokeMethodName)]</c> - and the method name otherwise.
    /// </summary>
    /// <remarks>
    /// Reading the attribute rather than the method name matters twice over. The identifier is what the
    /// JS side names, so capturing <c>method.Name</c> would record something JS never asks for; and the
    /// attribute is what <c>JSInterop</c> resolves through, so a method that survives trimming while its
    /// attribute does not is no longer callable even though its name is still there.
    /// <para>
    /// <c>inherit: true</c> so an override of a <c>[JSInvokable]</c> base method counts - JS dispatches it
    /// just the same, and only the most derived declaration comes back from <c>GetMethods</c>.
    /// </para>
    /// </remarks>
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Measuring what the trimmer left behind is this harness's job.")]
    private static string[] DispatchIdentifiers(Type type)
        => [.. type.GetMethods(PublicMembers)
            .Select(method => (method.Name, Attribute: method.GetCustomAttribute<JSInvokableAttribute>(inherit: true)))
            .Where(entry => entry.Attribute is not null)
            .Select(entry => entry.Attribute!.Identifier ?? entry.Name)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(identifier => identifier, StringComparer.Ordinal)];

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

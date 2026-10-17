using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Bit.Blex;

/// <summary>
/// The shared <see cref="JsonSerializerOptions"/> used by generated snapshot code, persistence and
/// the DevTools bridge.
/// </summary>
public static class BlexJson
{
    /// <summary>Default options: camelCase-insensitive, enums as strings, tolerant reading.</summary>
    public static JsonSerializerOptions Options { get; private set; } = CreateDefault();

    [UnconditionalSuppressMessage("AOT", "IL3050",
        Justification = "The non-generic enum converter only needs dynamic code for the reflection-based default; apps publishing with Native AOT replace the resolver (and converters) through Configure.")]
    private static JsonSerializerOptions CreateDefault() => new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    /// <summary>
    /// Customizes how state is serialized -- typically to register a converter for a domain type
    /// that appears in <c>[BlexStateAttribute]</c> fields. Call once during startup, before any store is
    /// resolved or snapshotted.
    /// </summary>
    /// <remarks>
    /// A <see cref="JsonSerializerOptions"/> instance becomes read-only the first time it is used,
    /// so this builds a fresh copy of the current options, applies <paramref name="configure"/> to
    /// it and swaps it in rather than mutating the live instance. Snapshots taken before the call
    /// are unaffected, which is why it belongs in startup rather than mid-flight. Types that carry
    /// a <c>[JsonConverter]</c> attribute need no registration at all.
    /// </remarks>
    /// <para>
    /// This is also the trimming/AOT seam. Snapshots go through the reflection-based serializer by
    /// default, which a trimmed publish (the Blazor WebAssembly release default) can strip the
    /// metadata for. Point <see cref="JsonSerializerOptions.TypeInfoResolver"/> at a
    /// <c>JsonSerializerContext</c> covering your state types and every generated
    /// <c>SerializeState</c>/<c>DeserializeState</c> uses it instead.
    /// </para>
    /// <example><code>
    /// BlexJson.Configure(o => o.Converters.Add(new MoneyJsonConverter()));
    ///
    /// // Trimmed / AOT publishes:
    /// BlexJson.Configure(o => o.TypeInfoResolver = AppJsonContext.Default);
    /// </code></example>
    public static void Configure(Action<JsonSerializerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var next = new JsonSerializerOptions(Options);
        configure(next);
        Options = next;
    }

    /// <summary>Restores the default options. Intended for test isolation.</summary>
    public static void Reset() => Options = CreateDefault();
}

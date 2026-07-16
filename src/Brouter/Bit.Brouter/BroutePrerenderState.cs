using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Bit.Brouter;

/// <summary>
/// The serialized form of a single loader result carried across the SSR/prerender -&gt; interactive
/// boundary. The concrete runtime type name is stored alongside the JSON so the value can be
/// rehydrated into the exact type the loader produced (rather than a raw <see cref="JsonElement"/>),
/// which is what components consuming the cascading <c>RouteData</c> expect.
/// </summary>
internal sealed class PersistedLoaderState
{
    /// <summary>Assembly-qualified name of the loaded value's runtime type. Null when the loader returned null.</summary>
    public string? TypeName { get; set; }

    /// <summary>The loaded value serialized as JSON. Null when the loader returned null.</summary>
    public string? Json { get; set; }
}

/// <summary>
/// Bridges route <see cref="Broute.Loader"/> results across the prerender -&gt; interactive transition so a
/// loader that ran on the server isn't re-run (double-fetched) when the component becomes interactive.
/// Serialization is reflection/JSON based, hence trim/AOT-unsafe for arbitrary types; this is only reached
/// when the consumer opts in via <see cref="BrouterOptions.PersistLoaderState"/> and takes responsibility
/// for keeping their loader data types serializable and preserved.
/// </summary>
internal static class BroutePrerenderState
{
    // Web defaults mirror the conventions Blazor itself uses for persisted component state and for
    // JSON over the wire, so a single symmetric options instance is used for both directions.
    private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Builds the persistence key for a loader in the matched chain. It is derived purely from the URL
    /// (path + query) and the node's position in the matched chain, both of which are identical on the
    /// prerender and interactive passes for the same navigation, so keys line up across the boundary.
    /// </summary>
    internal static string MakeKey(string path, string query, int chainIndex) =>
        $"Bit.Brouter|{path}|{query}|{chainIndex}";

    /// <summary>
    /// Captures a loader result into its persistable form using <paramref name="serializerOptions"/>
    /// (a source-generated-resolver-backed instance for AOT-safety, see
    /// <see cref="BrouterOptions.LoaderStateTypeInfoResolver"/>) or the reflection-based defaults.
    /// Returns <c>null</c> when the value can't be serialized (e.g. the supplied resolver doesn't
    /// cover its type) - the caller then skips persisting it and the loader simply re-runs on the
    /// interactive pass, so an unserializable type never breaks prerender.
    /// </summary>
    [RequiresUnreferencedCode("Serializes an arbitrary loader result via System.Text.Json reflection.")]
    [RequiresDynamicCode("Serializes an arbitrary loader result via System.Text.Json reflection.")]
    internal static PersistedLoaderState? Capture(object? value, JsonSerializerOptions? serializerOptions = null)
    {
        if (value is null) return new PersistedLoaderState { TypeName = null, Json = null };

        var type = value.GetType();
        try
        {
            return new PersistedLoaderState
            {
                TypeName = type.AssemblyQualifiedName,
                Json = JsonSerializer.Serialize(value, type, serializerOptions ?? _options),
            };
        }
        catch (Exception ex) when (ex is NotSupportedException or InvalidOperationException or JsonException)
        {
            // Unserializable under the active resolver/options: skip persistence for this entry.
            return null;
        }
    }

    /// <summary>
    /// Rehydrates a previously-captured loader result. Returns <c>true</c> when a value (possibly null)
    /// was restored and the loader should be skipped; <c>false</c> when restoration wasn't possible
    /// (unknown type, malformed JSON) and the loader should run normally.
    /// </summary>
    [RequiresUnreferencedCode("Deserializes a loader result into its runtime type via System.Text.Json reflection.")]
    [RequiresDynamicCode("Deserializes a loader result into its runtime type via System.Text.Json reflection.")]
    internal static bool TryRestore(PersistedLoaderState? state, out object? value, JsonSerializerOptions? serializerOptions = null)
    {
        value = null;
        if (state is null) return false;

        // A persisted null result is still a decision the loader made: honor it and skip re-running.
        if (string.IsNullOrEmpty(state.TypeName) || state.Json is null) return true;

        try
        {
            // Type.GetType(throwOnError: false) suppresses TypeLoadException (returns null) but can still
            // throw for a stale/unloadable persisted name - a malformed assembly-qualified string
            // (ArgumentException), or a referenced assembly that can't be loaded here
            // (FileLoadException / FileNotFoundException / BadImageFormatException). Treat all of those,
            // like a missing type or malformed JSON, as "can't restore" and fall back to running the loader.
            var type = Type.GetType(state.TypeName, throwOnError: false);
            if (type is null) return false; // type not available here; fall back to running the loader

            value = JsonSerializer.Deserialize(state.Json, type, serializerOptions ?? _options);
            return true;
        }
        catch (Exception ex) when (ex is JsonException
            or ArgumentException
            or NotSupportedException
            or InvalidOperationException
            or System.IO.FileLoadException
            or System.IO.FileNotFoundException
            or BadImageFormatException
            or TypeLoadException)
        {
            value = null;
            return false;
        }
    }
}

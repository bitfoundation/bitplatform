namespace Bit.Brouter.Tests;

/// <summary>A simple serializable payload used to exercise prerender loader-state bridging.</summary>
public sealed class SampleDto
{
    public string? Name { get; set; }
    public int Count { get; set; }
}

/// <summary>
/// Source-generated JSON context used by the AOT-safe persistence tests
/// (see BrouterOptions.LoaderStateTypeInfoResolver).
/// </summary>
[System.Text.Json.Serialization.JsonSerializable(typeof(SampleDto))]
public sealed partial class PersistenceTestJsonContext : System.Text.Json.Serialization.JsonSerializerContext
{
}

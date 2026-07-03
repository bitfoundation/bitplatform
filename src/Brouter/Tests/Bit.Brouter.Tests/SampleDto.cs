namespace Bit.Brouter.Tests;

/// <summary>A simple serializable payload used to exercise prerender loader-state bridging.</summary>
public sealed class SampleDto
{
    public string? Name { get; set; }
    public int Count { get; set; }
}

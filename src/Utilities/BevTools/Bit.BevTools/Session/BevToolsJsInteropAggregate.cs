namespace Bit.BevTools;

public sealed class BevToolsJsInteropAggregate
{
    public required string Identifier { get; init; }

    public int Count;

    public double TotalMs;

    public double MaxMs;

    public int Failures;

    public BevToolsJsInteropDirection Direction { get; init; }
}

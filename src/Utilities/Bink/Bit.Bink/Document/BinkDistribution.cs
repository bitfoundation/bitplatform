namespace Bit.Bink;

/// <summary>The axis along which <see cref="BinkDocument.DistributeElements"/> spaces elements out.</summary>
public enum BinkDistribution : byte
{
    /// <summary>Equal gaps from left to right.</summary>
    Horizontal = 0,
    /// <summary>Equal gaps from top to bottom.</summary>
    Vertical = 1,
}

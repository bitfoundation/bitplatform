namespace Bit.Bheadless;

/// <summary>Implemented by floating content that can be moved to a new anchor point.</summary>
internal interface IBitBhPointAnchoredContent
{
    Task MoveToAsync(double x, double y);
}

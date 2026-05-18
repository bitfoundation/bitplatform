namespace Bit.Brouter;

// Reserved enum kept for binary compatibility within this package version.
// Brouter always picks the most-specific matching route; ties are broken by declaration order.
internal enum BrouterMatchMode
{
    Best = 0
}

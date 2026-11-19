
namespace Bit.BevTools;

/// <summary>Formats values of a type for display in inspectors. Return null to fall back to default formatting.</summary>
public delegate string? BevToolsValueFormatter(object value);

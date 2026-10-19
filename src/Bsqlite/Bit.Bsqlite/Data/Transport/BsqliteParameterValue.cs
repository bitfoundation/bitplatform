namespace Bit.Bsqlite.Data;

/// <summary>A parameter value in transport form.</summary>
public sealed record BsqliteParameterValue(string Name, object? Value);

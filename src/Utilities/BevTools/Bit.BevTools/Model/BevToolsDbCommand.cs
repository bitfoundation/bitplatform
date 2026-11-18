namespace Bit.BevTools;

/// <summary>
/// A database command Entity Framework Core reported executing (or failing): its SQL as EF logged it - parameters by
/// name, their values only where the app enabled sensitive data logging - and how long it took.
/// </summary>
public sealed record BevToolsDbCommand(string? CommandText, double? ElapsedMs, bool Failed);

namespace Bit.BevTools;

/// <summary>One SQL command's executions in the log buffer.</summary>
internal sealed record BevToolsDbCommandSummary(
    string CommandText,
    int Executions,
    int Failed,
    double TotalMs,
    double? P95Ms,
    double? MaxMs,
    DateTimeOffset LastAt,
    IReadOnlyList<(string Component, int Executions)> Senders,
    IReadOnlyList<long> LogIds);

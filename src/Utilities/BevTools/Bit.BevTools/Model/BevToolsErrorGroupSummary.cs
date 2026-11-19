namespace Bit.BevTools;

/// <summary>One group of errors in the buffer: every record of it, however many occurrences each counted.</summary>
internal sealed record BevToolsErrorGroupSummary(
    string Group,
    string Source,
    string ExceptionType,
    string MessageTemplate,
    int Occurrences,
    int Records,
    int DistinctMessages,
    IReadOnlyList<string> Components,
    DateTimeOffset FirstAt,
    DateTimeOffset LastSeen,
    string? SourceLocation,
    long LatestId);

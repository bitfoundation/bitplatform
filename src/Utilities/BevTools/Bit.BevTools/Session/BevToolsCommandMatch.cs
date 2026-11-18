
namespace Bit.BevTools;

public sealed record BevToolsCommandMatch(IBevToolsCommand Command, int Score, string? Argument);

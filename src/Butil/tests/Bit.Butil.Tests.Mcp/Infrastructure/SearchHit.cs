namespace Bit.Butil.Tests.Mcp.Infrastructure;

// The shapes the structured tools answer with, re-declared here rather than shared with the server.
// That is deliberate: these records ARE the contract a client codes against, so a property renamed
// or dropped on the server has to fail a test instead of quietly flowing through a shared type.
// Only the fields the suite asserts on are declared; unknown ones are ignored by the deserializer.

public sealed record SearchHit(string Kind, string Title, string? Context, string Tool, string Snippet);

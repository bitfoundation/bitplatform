namespace Bit.Belemetry.Demo.Client.Services;

/// <summary>
/// One public type of Bit.Belemetry as the API tables on the docs pages show it - reflected out of
/// the shipped assembly and its XML documentation by the server (the same catalog the MCP server's
/// <c>GetBelemetryApiDetails</c> tool answers from), so a table cannot list a member that does not exist
/// or a default the library does not have.
/// </summary>
public sealed record ApiTypeInfo(string Name, string Kind, string? Summary, ApiMemberInfo[] Members);

/// <summary>One member of an <see cref="ApiTypeInfo"/>.</summary>
public sealed record ApiMemberInfo(string Name, string Kind, string? Type, string? Signature, string? Default, string? Summary);

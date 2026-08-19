namespace Bit.Butil.Tests.Mcp.Infrastructure;

// The shapes the structured tools answer with, re-declared here rather than shared with the server.
// That is deliberate: these records ARE the contract a client codes against, so a property renamed
// or dropped on the server has to fail a test instead of quietly flowing through a shared type.
// Only the fields the suite asserts on are declared; unknown ones are ignored by the deserializer.

public sealed record SearchResult(SearchHit[] Hits, string? Message);

public sealed record SearchHit(string Kind, string Title, string? Context, string Tool, string Snippet);

public sealed record ApiType(string Name, string Kind, bool IsInjectable, string? Summary);

public sealed record ApiMember(string Name, string Kind, string? Type, string? Signature, string? Default, string? Summary, string? Remarks);

public sealed record ApiTypeDetails(
    string Name,
    string FullName,
    string Kind,
    string? Inject,
    string[]? Implements,
    string? Summary,
    string? Remarks,
    string? DocsUrl,
    ApiMember[] Members);

public sealed record ApiDetailsResult(ApiTypeDetails? Details, ApiType[]? Types, string? Message);

public sealed record ApiInspection(
    string Query,
    bool IsKnown,
    string? Message,
    string? Api,
    string[]? Services,
    string[]? Inject,
    string? BrowserSupport,
    string[]? Requires,
    string[]? Notes,
    string[]? Disposables,
    string[]? NextCalls);

public sealed record FeaturePlan(
    ApiInspection[] Apis,
    string[] Unknown,
    bool RequiresSecureContext,
    bool RequiresPermission,
    bool RequiresUserGesture,
    string[] EngineLimited,
    string[] Checklist,
    string[]? Ignored);

// No record for the listings: they are answered as Markdown, not as structured content, which is
// what let the four tools that used to serve them go away. DocsIndexRow in WireContracts.cs parses
// the docs index, and McpTestBase.ListAsync reads the identifiers out of the other two.

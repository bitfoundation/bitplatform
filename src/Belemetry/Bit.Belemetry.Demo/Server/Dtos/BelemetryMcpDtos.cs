using Bit.Belemetry.Demo.Client.Services;

namespace Bit.Belemetry.Demo.Server.Dtos;

/// <summary>One hit of <c>SearchBelemetry</c>.</summary>
/// <param name="Kind">What was matched: <c>guide</c> (a README section), <c>page</c> (a demo page), <c>api</c> (a public type) or <c>convention</c> (a reference table row).</param>
/// <param name="Title">The section heading, page title, type name or row key.</param>
/// <param name="Summary">The first words of what was matched, enough to tell two hits apart.</param>
/// <param name="Location">Where it lives: a README anchor, a demo route or a type name.</param>
/// <param name="FollowUp">The exact tool call that returns the full text of this hit.</param>
public sealed record BelemetrySearchHitDto(string Kind, string Title, string Summary, string Location, string FollowUp);

/// <summary>The answer of <c>GetBelemetryApiDetails</c>: the type, or why there is none.</summary>
/// <param name="Details">The reflected type, or null when <c>typeName</c> names no public Bit.Belemetry type.</param>
/// <param name="Message">Set when <paramref name="Details"/> is null: the closest names that do exist.</param>
public sealed record BelemetryApiDetailsResultDto(ApiTypeInfo? Details, string? Message);

/// <summary>One file of a generated configuration.</summary>
/// <param name="Path">Where the file goes in the app, e.g. <c>Program.cs</c> or <c>Client/Program.cs</c>.</param>
/// <param name="Language">The snippet language: <c>C#</c>, <c>Razor</c>, <c>HTML</c> or <c>JSON</c>.</param>
/// <param name="Content">The file content, or the part of it that Belemetry adds.</param>
public sealed record BelemetryGeneratedFileDto(string Path, string Language, string Content);

/// <summary>The answer of <c>GenerateBelemetryConfiguration</c>.</summary>
/// <param name="Files">The files to add or change, in the order to apply them.</param>
/// <param name="Notes">What the generated code assumes and what to check before shipping it.</param>
public sealed record BelemetryGeneratedConfigDto(BelemetryGeneratedFileDto[] Files, string[] Notes);

/// <summary>One finding of <c>ReviewBelemetryConfiguration</c>.</summary>
/// <param name="Rule">A stable id of the rule, e.g. <c>BLM001</c>.</param>
/// <param name="Severity"><c>error</c> (it does not work or leaks data), <c>warning</c> (it probably is not what was meant) or <c>info</c>.</param>
/// <param name="Line">The 1-based line the finding is about, when it is about one line.</param>
/// <param name="Message">What is wrong.</param>
/// <param name="Fix">What to write instead.</param>
public sealed record BelemetryReviewFindingDto(string Rule, string Severity, int? Line, string Message, string Fix);

/// <summary>The answer of <c>ReviewBelemetryConfiguration</c>.</summary>
public sealed record BelemetryReviewDto(string Summary, BelemetryReviewFindingDto[] Findings);

/// <summary>One parameter of a tool, as the MCP page lists it.</summary>
public sealed record McpParameterDto(string Name, string Type, bool Required, string? Description);

/// <summary>One tool of the MCP server.</summary>
public sealed record McpToolDto(string Name, string Description, McpParameterDto[] Parameters);

/// <summary>One prompt of the MCP server.</summary>
public sealed record McpPromptDto(string Name, string Description);

/// <summary>One resource (or resource template) of the MCP server.</summary>
public sealed record McpResourceDto(string Uri, string Name, string Description);

/// <summary>Everything the MCP server offers, reflected from its own attributes - what the MCP page lists.</summary>
public sealed record McpCatalogDto(McpToolDto[] Tools, McpPromptDto[] Prompts, McpResourceDto[] Resources);

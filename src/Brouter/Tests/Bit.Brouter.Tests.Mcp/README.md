# Bit.Brouter.Tests.Mcp

Tests for the **MCP server** the Brouter demo hosts at `/mcp`
(`Bit.Brouter.Demo/Server/Controllers/McpController.cs` and the services behind it).

```bash
cd src/Brouter
dotnet test Tests/Bit.Brouter.Tests.Mcp/Bit.Brouter.Tests.Mcp.csproj
```

## How it runs

The demo server is booted once per test run from its own `Program.cs` with `WebApplicationFactory`,
and the tests talk to it **through the protocol** - one `McpClient` over the streamable HTTP
transport, doing a real `initialize` handshake and real `tools/call` requests. Nothing is exercised
by calling the tool methods directly, because most of what can break here is invisible from the C#
side: a missing attribute, a schema a client rejects, an answer that does not serialize, a resource
URI that no longer resolves.

## What it covers

| Area | Files |
| --- | --- |
| Handshake, instructions, capabilities | `McpHandshakeTests` |
| The published tool set, its annotations and schemas | `McpToolSurfaceTests` |
| Every tool's answers - including the index each one gives when its key is left out - and its answers to bad input | `McpDocumentationToolTests`, `McpApiToolTests`, `McpSearchToolTests`, `McpTemplateToolTests`, `McpToolFailureTests` |
| Resources, prompts and argument completion | `McpResourceTests`, `McpPromptTests`, `McpCompletionTests` |
| The seams between the catalogs | `McpCatalogConsistencyTests` |
| The `/api/mcp/...` HTTP mirror | `HttpMirrorTests` |
| The material behind it all | `BrouterSourceCatalogTests`, `BrouterXmlDocsTests`, `BrouterSetupGuideTests`, `HtmlToMarkdownServiceTests`, `DocsPageRenderingTests`, `McpControllerInternalsTests` |
| The verdicts, checked against a live router | `RouterOracleTests` |
| Several clients reading at once | `McpConcurrencyTests` |

## The two that matter most

`RouterOracleTests` mounts a real `<Brouter>` (bUnit) and puts the server's two non-documentation
claims to it: the templates `InspectBrouterRouteTemplates` calls ambiguous are exactly the ones the
router refuses to register, and every constraint's documented passing and failing example really
does pass and fail. No expected answers are written down there - the router is the expectation.

`McpCatalogConsistencyTests` walks the paths between the tools: it takes every follow-up call a
search hit suggests, makes it, and fails if the answer is an apology about a key that no longer
exists. That is what catches a renamed heading, a removed docs page or a renamed tool - each of
which leaves every individual tool working and the path between them broken.

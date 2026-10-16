# Bit.Blex.Tests.Mcp

Tests for the **MCP server** the Blex demo hosts at `/mcp`
(`Bit.Blex.Demo/Server/Controllers/McpController.cs` and the services behind it).

```bash
cd src/Blex/Tests/Bit.Blex.Tests.Mcp
dotnet test
```

## How it runs

The demo server is booted once per test run from its own `Program.cs` with `WebApplicationFactory`, and the
tests talk to it **through the protocol** - one `McpClient` over the streamable HTTP transport, doing a real
`initialize` handshake and real `tools/call` requests. Nothing is exercised by calling the tool methods
directly, because most of what can break here is invisible from the C# side: a missing attribute, a schema a
client rejects, an answer that does not serialize, a resource URI that no longer resolves.

## What it covers

| Area | Files |
| --- | --- |
| Handshake, instructions, capabilities | `McpHandshakeTests` |
| The published tool set, its annotations and schemas | `McpToolSurfaceTests` |
| Every tool's answers - including the index each reference tool gives when its key is left out - and its answers to bad keys | `McpDocumentationToolTests`, `McpApiToolTests`, `McpSearchToolTests`, `McpDiagnosticsToolTests`, `McpInspectStoreTests` |
| Resources, prompts and argument completion | `McpResourceTests`, `McpPromptTests`, `McpCompletionTests` |
| The seams between the catalogs | `McpCatalogConsistencyTests` |
| The `/api/mcp/...` HTTP mirror | `HttpMirrorTests` |

## The two that matter most

`McpInspectStoreTests.Every_documented_diagnostic_sample_is_judged_by_the_generator_as_the_docs_say` puts every
rejected sample on the diagnostics page to the real generator, which has to report exactly the diagnostic the
page says it does - and every fix has to make that diagnostic go away. No expected answer is written down
there: the docs are the claim and the generator is the judge.

`McpCatalogConsistencyTests` walks the paths between the tools: it takes every follow-up call a search hit
suggests, makes it, and fails if the answer is an apology about a key that no longer exists. That is what
catches a renamed README heading, a removed docs page or a renamed tool - each of which leaves every individual
tool working and the path between them broken.

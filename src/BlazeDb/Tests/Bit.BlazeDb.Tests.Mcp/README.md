# Bit.BlazeDb.Tests.Mcp

Tests for the **MCP server** the BlazeDb demo hosts at `/mcp`
(`Bit.BlazeDb.Demo/Server/Controllers/McpController.cs` and the services behind it).

```bash
cd src/BlazeDb/Tests/Bit.BlazeDb.Tests.Mcp
dotnet test
```

## How it runs

The demo server is booted once per test run from its own `Program.cs` with `WebApplicationFactory`, and the
tests talk to it **through the protocol** - one `McpClient` over the streamable HTTP transport, doing a real
`initialize` handshake and real `tools/call` requests. Nothing is exercised by calling the tool methods
directly, because most of what can break here is invisible from the C# side: a missing attribute, a schema a
client rejects, an answer that does not serialize, a resource URI that no longer resolves.

## What it covers

| Area | File |
| --- | --- |
| Handshake, instructions, capabilities, the published tool set, its annotations and schemas | `McpSurfaceTests` |
| Every reference tool's answers - including the index each one gives when its key is left out - and its answers to bad keys | `McpToolTests` |
| The table inspector against the generator | `McpInspectorTests` |
| The search, and every follow-up call its hits suggest | `McpSearchTests` |
| Resources, prompts and argument completion | `McpResourcePromptTests` |
| The API index against the assemblies, the guide against the README, the `/api/mcp/...` HTTP mirror | `McpOracleTests` |

## The two that matter most

`McpInspectorTests` holds `InspectBlazeDbTable` to the generator it speaks for. The models in `Models\` are
compiled into this assembly by the real generator and also handed to the inspector as text, and the two have
to agree: on the table name, the key and the indexes, and on the field number and wire type of every property
- read off a row the generated writer actually encoded. The diagnostics catalog is judged the same way: every
rejected sample has to be rejected with its own id, and every fix has to generate.

`McpSearchTests.Every_follow_up_call_a_hit_suggests_answers_with_the_thing` walks the paths between the tools:
it takes every follow-up call a search hit suggests, makes it, and fails if the answer is an apology about a
key that no longer exists. That is what catches a renamed README heading, a moved demo file or a renamed tool -
each of which leaves every individual tool working and the path between them broken.

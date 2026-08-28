# Bit.BlazorUI.Tests.Mcp

The suite for the bit BlazorUI MCP server - the tools an AI agent calls to build a Blazor UI with
this library without guessing at it. The server lives in
[`Demo/Bit.BlazorUI.Demo.Server`](../../Demo/Bit.BlazorUI.Demo.Server): `Controllers/McpController.cs`
for the tools, `McpPrompts` and `McpResources` beside it, and `Services/Mcp` for the catalogs they
all answer from.

## How it runs

The suite boots the demo server as a **child process** and talks to it over HTTP, with the same
protocol client an editor or an agent host uses. There is deliberately no project reference to the
server.

Everything the tools answer with comes from the deployment rather than from the code: the demo
pages are resources the .csproj embeds, the parameter tables are read off types loaded beside the
assembly, the XML documentation is read from files copied next to them, and the theming reference is
rendered by the real components through the real request pipeline. Each of those is a way the tools
can answer nothing in production while answering perfectly against an in-memory host, so the suite
runs the app the way the app runs.

```shell
dotnet run --project Tests/Bit.BlazorUI.Tests.Mcp
```

| Variable | What it does |
| --- | --- |
| `BLAZORUI_MCP_BASE_URL` | Run against a server you started yourself - a local `dotnet run`, or a deployed instance. Makes this suite a smoke test against a real deployment. |
| `BLAZORUI_MCP_ARTIFACTS_PATH` | Where the child's build output goes. Defaults to a stable folder under the temp directory, so the build stays incremental and never fights a demo you already have running. |

## What each fixture is for

| Fixture | What it pins |
| --- | --- |
| `ToolSurfaceTests` | The seven tools, their annotations, their descriptions, the absence of output schemas, and the instructions the server writes into the model's context. This is what every session pays for before it calls anything. |
| `ComponentCatalogTests` | The catalog against the per-component answers: every component the nav lists resolves, has a table and has examples; every alias resolves; the shared parameters are documented once and pointed at rather than repeated. |
| `ExamplesToolTests` | That the worked examples really carry code - the sample fields are read off the compiled page by reflection, and a renamed field would produce a section with a title, a paragraph and nothing to copy. |
| `SearchTests` | The first hit for the queries where the library's name for a thing is not the name a task suggests, and that every hit names a follow-up call that exists and answers. |
| `ReferenceToolTests` | The type lookup, the setup guide and the theming reference - three tools with three different ways of going quietly blank. |
| `PromptAndResourceTests` | The half of the server a person reaches: the prompts, the resources, and the completions that make either usable without knowing what to type. |
| `ResilienceTests` | What happens on a miss, under concurrency, and on a genuinely cold start - plus that every tool is still reachable as a plain HTTP GET under `/api/mcp/...`. |

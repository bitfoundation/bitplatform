# Bit.Butil MCP server tests

NUnit suite for the Butil MCP server (`Bit.Butil.Demo/Server/Controllers/McpController.cs` and the
catalogs behind it). It boots the demo server as a child process and drives `/mcp` with a **real
MCP client** — the same `ModelContextProtocol` SDK an editor or an agent host uses — plus plain
HTTP for the endpoints an MCP client never sees. Uses the **Microsoft.Testing.Platform** runner
(mandated by the repo `global.json`) via NUnit's MTP runner.

## Running

```powershell
dotnet test .\Bit.Butil.Tests.Mcp.csproj
```

The first run builds the demo server (including its WebAssembly client), which takes a few minutes;
after that it is incremental and the whole suite runs in seconds.

| Variable | Effect |
| --- | --- |
| `BUTIL_MCP_BASE_URL` | Run against a server you started yourself — a local `dotnet run`, or a deployed instance — instead of launching one. This is also how you smoke-test a real deployment: `$env:BUTIL_MCP_BASE_URL = "https://butil.bitplatform.dev"`. |
| `BUTIL_MCP_ARTIFACTS_PATH` | Where the child server's build output goes. Defaults to a stable folder under the temp directory. |

## Why a child process rather than a `WebApplicationFactory`

The MCP server is only as good as the deployment around it, and most of what can break it is
outside the controller:

* the catalogs are built from **resources embedded by the .csproj** — a glob that stops matching
  leaves the tools answering "no source files" with nothing else wrong;
* the API reference is paired with the **XML documentation read from beside the assembly** — if the
  build does not put it there, every summary is silently null;
* the docs pages are **rendered by the real components**, outside the app's router and layout;
* the endpoint is reached through the real routing, CORS and HTTPS-redirect pipeline.

Every one of those is a way the server can be perfect in memory and useless in production, so the
suite runs the app the way the app runs.

## Layout

| File | What it holds to account |
| --- | --- |
| `Infrastructure/McpServerFixture.cs` | Assembly-level `[SetUpFixture]`: boots the demo server on a free port, into its own artifacts path so a developer's running instance cannot lock the build. |
| `Infrastructure/McpTestBase.cs` | A live `McpClient` per fixture, and the helpers the assertions are written in. |
| `Infrastructure/WireContracts.cs` | The server's public inventory, written down — tool, resource and prompt names are identifiers clients store, so renaming one has to fail a test. Also parses the `Tool` strings hits hand back into real calls. |
| `Infrastructure/WireDtos.cs` | The structured payloads, re-declared rather than shared with the server: these records **are** the contract a client codes against. |
| `ServerContractTests.cs` | The handshake — serverInfo, advertised capabilities, and the instructions the model carries all session. |
| `ToolSurfaceTests.cs` | tools/list: the names, titles, descriptions, annotations, input and output schemas, and the standing context cost of the whole surface. |
| `ToolBehaviourTests.cs` | What each tool answers when called properly — including rendering **every** documentation page. |
| `ToolFailureTests.cs` | Unresolvable arguments: a sentence naming the nearest candidates, never a protocol error, never a leak. |
| `SearchTests.cs` | The entry-point tool: capabilities phrased as a person would phrase them, and every follow-up call a hit names actually invoked. |
| `ResourceTests.cs` | The resource half, and that it never disagrees with the tool covering the same material. |
| `PromptTests.cs` | The four workflows, their arguments, and that they only name tools that exist. |
| `CompletionTests.cs` | completion/complete, and that a completed value is one the server can then resolve. |
| `HttpSurfaceTests.cs` | The `/api/mcp/...` GET mirror, CORS for browser-based clients, and the discovery files that advertise the endpoint. |
| `CatalogConsistencyTests.cs` | The five catalogs held against each other — the dangling cross-references no single catalog can catch. |
| `ResilienceTests.cs` | Concurrency, caching, idempotence, cancellation, and one realistic agent session end to end. |
| `ci/bit.ci.Butil.mcp.yml` | A ready-made workflow that is **not currently enabled**: copy it into `.github/workflows/` to turn it on. |

## What a failure here usually means

* **A tool, resource or prompt name changed** — that is a breaking change for every client that
  already holds the old name, and `WireContracts.cs` is where you accept it deliberately.
* **A documentation page stopped rendering** — it reads something from its surroundings that is not
  there when it renders outside the router. The tool answers with an apology instead of the page.
* **A service reports no members or no summary** — the reflection walk or the XML documentation is
  not reaching the deployed app, which is invisible from the outside until an agent asks.
* **A follow-up call does not resolve** — a search hit is pointing at something that no longer
  exists, and an agent following it has no way to tell that from the tool being broken.

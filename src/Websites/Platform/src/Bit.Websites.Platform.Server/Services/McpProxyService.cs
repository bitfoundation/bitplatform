using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Bit.Websites.Platform.Server.Services;

/// <summary>
/// Backs this site's /mcp endpoint by fanning it out to the MCP servers below: the documentation servers
/// of the bit platform libraries plus the third party ones the team relies on. A single connection to
/// bitplatform.dev/mcp therefore exposes the tools of all of them at once, with the names, schemas and
/// results of each tool passed through untouched. An upstream is either a remote http server or a local
/// stdio process this site spawns and keeps alive. An upstream may be narrowed down to a subset of its
/// tools, and such a tool may be given a description written here rather than the one its own server
/// provides. An upstream marked internal is kept off the endpoint and served only to this site's own
/// chatbot, which reaches it through <see cref="ListInternalFunctions"/> instead.
/// </summary>
public partial class McpProxyService : IAsyncDisposable
{
    [AutoInject] private ILogger<McpProxyService> logger = default!;
    [AutoInject] private ILoggerFactory loggerFactory = default!;
    [AutoInject] private IOptionsMonitor<AppSettings> appSettings = default!;

    /// <summary>
    /// The description DeepWiki gives its ask_question tool says no more than that it answers questions about a
    /// GitHub repository, which leaves the agent to guess whether a repository worth asking exists at all. Naming
    /// the third party repositories the bit platform team relies on, and what each one is the right source for, turns
    /// it into a tool an agent reaches for on its own instead of one it only uses when it is told to. The template's
    /// AGENTS.md points at this description rather than repeating the list, so this is the only place it is written down.
    /// </summary>
    private const string askQuestionDescription = """
        Asks a question about a public GitHub repository and answers it from that repository's own source code and
        documentation. Prefer it over a web search whenever the question is about one of these libraries:
        - riok/mapperly: object mapping between entities and DTOs, focusing on its partial static classes and extension methods approach.
        - microsoft/aspire: .NET Aspire AppHost orchestration, resource configuration, replacing Docker resources with their Azure equivalents, service discovery and integrations. It significantly outperforms Microsoft Learn for code level questions.
        - HangfireIO/Hangfire: background job scheduling, recurring jobs, filters, storage configuration and distributed processing.
        - ZiggyCreatures/FusionCache: hybrid caching, L2 cache backplane, distributed locking, OpenTelemetry integration and cache factory configuration.
        - keycloak/keycloak: Keycloak and realm configuration.
        - microsoft/agent-framework: agent creation, multi agent orchestration, workflows, tools and function calling, MCP, A2A communication, memory and context, and provider integrations.
        Do not ask it about the bit platform libraries themselves: BlazorUI, Bmotion, Butil, Bswup and Brouter each have
        their own dedicated tools on this server, and those are written from the library rather than inferred from it.
        """;

    /// <summary>
    /// Every tool of this upstream takes a project argument naming the index to answer from, a deployment
    /// detail no caller should have to know: it is stripped from the advertised schemas and filled in from
    /// <see cref="CodebaseMemoryIndexService.ProjectName"/> on the way through.
    /// </summary>
    private const string codebaseMemoryUpstreamName = "CodebaseMemory";

    /// <summary>
    /// The proxied servers. Not static: each holds the session this site keeps open to that server.
    /// </summary>
    private readonly Upstream[] upstreams =
    [
        new("MicrosoftLearn", new("https://learn.microsoft.com/api/mcp")),
        // Only ask_question is exposed: it answers against the whole repository by itself, while the
        // read_wiki_structure and read_wiki_contents tools of the same server dump the generated wiki
        // of a repository, which is a slower and far more token hungry way to reach the same answer.
        new("DeepWiki", new("https://mcp.deepwiki.com/mcp"), [new("ask_question", askQuestionDescription)]),
        new("bitBlazorUI", new("https://blazorui.bitplatform.dev/mcp")),
        new("bitBrouter", new("https://brouter.bitplatform.dev/mcp")),
        new("bitButil", new("https://butil.bitplatform.dev/mcp")),
        new("bitBswup", new("https://bswup.bitplatform.dev/mcp")),
        new("bitMotion", new("https://bmotion.bitplatform.dev/mcp")),
        // codebase-memory-mcp (a stdio child process npx fetches) serves a graph index of the repository
        // configured at AppSettings:CodebaseMemory:SourceRepositoryPath, so the chatbot can answer from
        // the source itself rather than from documentation. Internal, because these are the tool names
        // codebase-memory-mcp uses in a developer's own mcp.json: advertising them here too would give an
        // agent wired up to both a duplicate of every one of them. Only the read side is listed anyway;
        // index_repository, delete_project and the rest stay unreachable. The command below is the
        // default one, resolved again per connection where configuration can replace it.
        new(codebaseMemoryUpstreamName, CodebaseMemoryIndexService.ResolveCommand(null), CodebaseMemoryIndexService.ResolveArguments(null), internalOnly: true, exposedTools:
        [
            new("search_graph", "Finds classes, methods and files of the bit platform source code by name pattern, returning qualified names for get_code_snippet."),
            new("search_code", "Greps the bit platform source code and returns the matches grouped by the enclosing method or class, with signatures and line numbers."),
            new("get_code_snippet", "Returns the full source of one class or method of the bit platform source code, addressed by the qualified name search_graph or search_code returned."),
            new("trace_path", "Walks the call graph of the bit platform source code from a given function, in either direction, to show how a capability is wired together."),
            new("get_architecture", "Summarizes the structure of the bit platform source code: its layers, entry points and dependency clusters.")
        ])
    ];

    /// <summary>
    /// Tool definitions only change when an upstream server is redeployed, so the merged list is cached.
    /// This lifetime bounds how long a newly published tool takes to show up on bitplatform.dev/mcp.
    /// </summary>
    private static readonly TimeSpan toolsCacheLifetime = TimeSpan.FromMinutes(15);

    /// <summary>
    /// After a failed refresh the merged list is missing the tools of at least one server, so it is kept
    /// only briefly: the next caller retries instead of waiting out the full <see cref="toolsCacheLifetime"/>.
    /// </summary>
    private static readonly TimeSpan failedToolsCacheLifetime = TimeSpan.FromMinutes(1);

    private readonly SemaphoreSlim toolsSync = new(1, 1);

    private Tool[] publicTools = [];
    private Tool[] internalTools = [];
    private DateTimeOffset toolsExpiresAt;
    private Dictionary<string, Upstream> upstreamPerToolName = new(StringComparer.Ordinal);

    public async ValueTask<IReadOnlyList<Tool>> ListTools(CancellationToken cancellationToken)
    {
        await RefreshToolsIfExpired(cancellationToken);

        return publicTools;
    }

    /// <summary>
    /// The internal tools as functions this site's own chatbot hands to its chat client. It gets its other
    /// tools from the /mcp endpoint, which does not serve these, so they are invoked in process instead.
    /// </summary>
    public async ValueTask<IReadOnlyList<AIFunction>> ListInternalFunctions(CancellationToken cancellationToken)
    {
        await RefreshToolsIfExpired(cancellationToken);

        return [.. internalTools.Select(tool => new ProxiedFunction(this, tool, upstreamPerToolName[tool.Name]))];
    }

    public async ValueTask<CallToolResult> CallTool(CallToolRequestParams request, CancellationToken cancellationToken)
    {
        await RefreshToolsIfExpired(cancellationToken);

        // An internal tool answers exactly like a name no upstream provides: the endpoint neither serves it
        // nor confirms that it exists.
        if (upstreamPerToolName.TryGetValue(request.Name, out var upstream) is false || upstream.InternalOnly)
            throw new McpException($"Unknown tool: '{request.Name}'.");

        return await CallTool(upstream, request, cancellationToken);
    }

    private async ValueTask<CallToolResult> CallTool(Upstream upstream, CallToolRequestParams request, CancellationToken cancellationToken)
    {
        var arguments = request.Arguments;

        if (upstream.Name is codebaseMemoryUpstreamName)
        {
            var projectName = CodebaseMemoryIndexService.ProjectName
                ?? throw new McpException("The source code index is still being built, retry in a minute.");

            // Stripped from the advertised schema, so it is filled in here - overwriting any value a caller
            // sends anyway, since this site serves exactly one index.
            Dictionary<string, JsonElement> augmentedArguments = arguments is null ? [] : new(arguments);
            augmentedArguments["project"] = JsonSerializer.SerializeToElement(projectName);
            arguments = augmentedArguments;
        }

        // Only the name and the arguments are forwarded: the remaining params (progress token, meta) belong
        // to the session between the caller and this site, not to the session between this site and upstream.
        CallToolRequestParams upstreamRequest = new() { Name = request.Name, Arguments = arguments };

        try
        {
            return await (await GetClient(upstream, cancellationToken)).CallToolAsync(upstreamRequest, cancellationToken);
        }
        catch (Exception exp) when (exp is not OperationCanceledException)
        {
            // The upstream session is long lived and shared by every caller of this site, so a redeploy of
            // the upstream can end it at any moment. Reconnecting and retrying once turns that into a
            // slower call rather than a failed one. A tool that itself fails does not land here: it reports
            // that through an IsError result instead of an exception.
            logger.LogWarning(exp, "Calling {ToolName} on the {McpServerName} MCP server failed, reconnecting and retrying once.", request.Name, upstream.Name);

            await Disconnect(upstream);

            return await (await GetClient(upstream, cancellationToken)).CallToolAsync(upstreamRequest, cancellationToken);
        }
    }

    /// <summary>
    /// The project argument is injected on the way through, so callers must not see it: a required
    /// parameter the schema advertises but the site fills in would only make every model waste effort
    /// inventing a value for it.
    /// </summary>
    private static JsonElement RemoveProjectFromSchema(JsonElement inputSchema)
    {
        var schema = JsonNode.Parse(inputSchema.GetRawText())!.AsObject();

        schema["properties"]?.AsObject().Remove("project");

        if (schema["required"] is JsonArray required)
        {
            var projectEntry = required.FirstOrDefault(entry => entry?.GetValue<string>() is "project");
            if (projectEntry is not null)
            {
                required.Remove(projectEntry);
            }
        }

        return JsonSerializer.SerializeToElement(schema);
    }

    private async ValueTask RefreshToolsIfExpired(CancellationToken cancellationToken)
    {
        if (DateTimeOffset.UtcNow < toolsExpiresAt) return;

        await toolsSync.WaitAsync(cancellationToken);

        try
        {
            if (DateTimeOffset.UtcNow < toolsExpiresAt) return;

            // The CodebaseMemory upstream is only live where a source repository is configured; elsewhere
            // there is no index to serve and spawning its process would fail on every refresh.
            var activeUpstreams = upstreams.Where(upstream => upstream.Name is not codebaseMemoryUpstreamName
                                                              || string.IsNullOrWhiteSpace(appSettings.CurrentValue.CodebaseMemory?.SourceRepositoryPath) is false);

            var toolsPerUpstream = await Task.WhenAll(activeUpstreams.Select(async upstream => (upstream, tools: await ListTools(upstream, cancellationToken))));

            List<Tool> mergedTools = [];
            Dictionary<string, Upstream> mergedUpstreamPerToolName = new(StringComparer.Ordinal);

            foreach (var (upstream, upstreamTools) in toolsPerUpstream)
            {
                foreach (var tool in upstreamTools ?? [])
                {
                    if (mergedUpstreamPerToolName.TryAdd(tool.Name, upstream) is false)
                    {
                        // Tool names are the only address an MCP client has, so two servers claiming the same
                        // name cannot both be exposed. The first one wins and the clash is reported.
                        logger.LogWarning("The {ToolName} tool of the {McpServerName} MCP server is not exposed because {OtherMcpServerName} already provides a tool with that name.",
                            tool.Name, upstream.Name, mergedUpstreamPerToolName[tool.Name].Name);
                        continue;
                    }

                    mergedTools.Add(tool);
                }
            }

            publicTools = [.. mergedTools.Where(tool => mergedUpstreamPerToolName[tool.Name].InternalOnly is false)];
            internalTools = [.. mergedTools.Where(tool => mergedUpstreamPerToolName[tool.Name].InternalOnly)];
            upstreamPerToolName = mergedUpstreamPerToolName;
            toolsExpiresAt = DateTimeOffset.UtcNow + (toolsPerUpstream.Any(t => t.tools is null) ? failedToolsCacheLifetime : toolsCacheLifetime);
        }
        finally
        {
            toolsSync.Release();
        }
    }

    /// <returns>
    /// The tools of <paramref name="upstream"/> that it is configured to expose, or null if they could not be listed.
    /// </returns>
    private async Task<IReadOnlyList<Tool>?> ListTools(Upstream upstream, CancellationToken cancellationToken)
    {
        try
        {
            var client = await GetClient(upstream, cancellationToken);

            List<Tool> upstreamTools = [];
            string? cursor = null;

            do
            {
                var page = await client.ListToolsAsync(new ListToolsRequestParams { Cursor = cursor }, cancellationToken);
                upstreamTools.AddRange(page.Tools);
                cursor = page.NextCursor;
            } while (cursor is not null);

            if (upstream.ExposedTools is null) return upstreamTools;

            // A tool that disappears upstream silently drops off this site's endpoint, so the mismatch is
            // reported: it means the configuration above no longer matches what the server provides.
            var missingToolNames = upstream.ExposedTools.Keys.Except(upstreamTools.Select(tool => tool.Name)).ToArray();
            if (missingToolNames.Length > 0)
            {
                logger.LogError("The {McpServerName} MCP server does not provide the {ToolNames} tool(s) this site is configured to expose.",
                    upstream.Name, string.Join(", ", missingToolNames));
            }

            List<Tool> exposedTools = [];

            foreach (var tool in upstreamTools)
            {
                if (upstream.ExposedTools.TryGetValue(tool.Name, out var exposedTool) is false) continue;

                // Freshly deserialized per call, so the tool of the upstream's own list result is never the
                // one being changed here.
                tool.Description = exposedTool.Description ?? tool.Description;

                if (upstream.Name is codebaseMemoryUpstreamName)
                {
                    tool.InputSchema = RemoveProjectFromSchema(tool.InputSchema);
                }

                exposedTools.Add(tool);
            }

            return exposedTools;
        }
        catch (Exception exp) when (exp is not OperationCanceledException)
        {
            // A single unreachable server must not empty out the whole endpoint: the tools of every other
            // server stay available and this one is picked up again by the next refresh.
            logger.LogError(exp, "Listing the tools of the {McpServerName} MCP server ({McpServerAddress}) failed.", upstream.Name, upstream.Address);

            await Disconnect(upstream);

            return null;
        }
    }

    private async ValueTask<McpClient> GetClient(Upstream upstream, CancellationToken cancellationToken)
    {
        if (upstream.Client is not null) return upstream.Client;

        await upstream.ClientSync.WaitAsync(cancellationToken);

        try
        {
            // One session per upstream server, shared by every caller of this site: the proxied tools are
            // documentation lookups that carry no per-caller state, so a session per inbound connection
            // would only multiply the handshakes. For a stdio upstream the session also owns the child
            // process, so sharing it keeps a single process alive instead of spawning one per caller.
            IClientTransport transport = upstream.Url is not null
                ? new HttpClientTransport(new()
                {
                    Name = upstream.Name,
                    Endpoint = upstream.Url
                }, loggerFactory)
                : new StdioClientTransport(new()
                {
                    Name = upstream.Name,
                    // Configured like the index service, so both halves reach the server the same way.
                    Command = upstream.Name is codebaseMemoryUpstreamName
                        ? CodebaseMemoryIndexService.ResolveCommand(appSettings.CurrentValue.CodebaseMemory)
                        : upstream.Command!,
                    Arguments = upstream.Name is codebaseMemoryUpstreamName
                        ? CodebaseMemoryIndexService.ResolveArguments(appSettings.CurrentValue.CodebaseMemory)
                        : upstream.Arguments,
                    // The data directory the index was built in, which no other client on this machine holds.
                    EnvironmentVariables = upstream.Name is codebaseMemoryUpstreamName
                        ? CodebaseMemoryIndexService.BuildEnvironment(appSettings.CurrentValue.CodebaseMemory)
                        : null
                }, loggerFactory);

            return upstream.Client ??= await McpClient.CreateAsync(transport, new()
            {
                ClientInfo = new() { Name = "bitplatform.dev", Version = typeof(McpProxyService).Assembly.GetName().Version!.ToString() }
            }, loggerFactory, cancellationToken);
        }
        finally
        {
            upstream.ClientSync.Release();
        }
    }

    private async Task Disconnect(Upstream upstream)
    {
        await upstream.ClientSync.WaitAsync();

        var client = upstream.Client;
        upstream.Client = null;

        try
        {
            if (client is not null)
            {
                await client.DisposeAsync();
            }
        }
        catch (Exception exp)
        {
            logger.LogWarning(exp, "Disconnecting from the {McpServerName} MCP server failed.", upstream.Name);
        }
        finally
        {
            upstream.ClientSync.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var upstream in upstreams)
        {
            await Disconnect(upstream);
        }

        GC.SuppressFinalize(this);
    }

    /// <param name="Name">
    /// The tool this site exposes out of the ones its server provides.
    /// </param>
    /// <param name="Description">
    /// The description to expose the tool with, or null to expose the one its own server provides.
    /// </param>
    private sealed record ExposedTool(string Name, string? Description = null);

    /// <summary>
    /// Exposes one proxied tool to this site's chatbot under the name, description and schema the endpoint
    /// would advertise it with, invoked in process rather than over http.
    /// </summary>
    private sealed class ProxiedFunction(McpProxyService proxy, Tool tool, Upstream upstream) : AIFunction
    {
        public override string Name => tool.Name;

        public override string Description => tool.Description ?? string.Empty;

        public override JsonElement JsonSchema => tool.InputSchema;

        protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
        {
            var result = await proxy.CallTool(upstream, new()
            {
                Name = tool.Name,
                Arguments = arguments.ToDictionary(argument => argument.Key,
                                                   argument => argument.Value is JsonElement element ? element : JsonSerializer.SerializeToElement(argument.Value))
            }, cancellationToken);

            // Text is what every proxied tool answers with, and what the chat client passes back to the model.
            return string.Join(Environment.NewLine, result.Content?.OfType<TextContentBlock>().Select(block => block.Text) ?? []);
        }
    }

    private sealed class Upstream
    {
        public Upstream(string name, Uri url, ExposedTool[]? exposedTools = null)
            : this(name, exposedTools, internalOnly: false)
        {
            Url = url;
        }

        public Upstream(string name, string command, string[] arguments, ExposedTool[]? exposedTools = null, bool internalOnly = false)
            : this(name, exposedTools, internalOnly)
        {
            Command = command;
            Arguments = arguments;
        }

        private Upstream(string name, ExposedTool[]? exposedTools, bool internalOnly)
        {
            Name = name;
            InternalOnly = internalOnly;
            ExposedTools = exposedTools?.ToDictionary(exposedTool => exposedTool.Name, StringComparer.Ordinal);
        }

        /// <summary>
        /// Whether this upstream is kept off the /mcp endpoint and served only to this site's own chatbot.
        /// </summary>
        public bool InternalOnly { get; }

        /// <summary>
        /// Used for logging only: the tools the server provides keep their own names.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The endpoint of an http upstream, or null for a stdio one.
        /// </summary>
        public Uri? Url { get; }

        /// <summary>
        /// The command a stdio upstream is spawned with, or null for an http one.
        /// </summary>
        public string? Command { get; }

        public string[] Arguments { get; } = [];

        /// <summary>
        /// Where this upstream lives, for logging: the endpoint of an http server, the command of a stdio one.
        /// </summary>
        public string Address => Url?.ToString() ?? Command!;

        /// <summary>
        /// The tools of this server to expose, keyed by name, or null to expose all of them as they are.
        /// </summary>
        public Dictionary<string, ExposedTool>? ExposedTools { get; }

        public McpClient? Client { get; set; }

        public SemaphoreSlim ClientSync { get; } = new(1, 1);
    }
}

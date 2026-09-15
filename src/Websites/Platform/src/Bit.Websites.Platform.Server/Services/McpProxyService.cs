using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Bit.Websites.Platform.Server.Services;

/// <summary>
/// Backs this site's /mcp endpoint by fanning it out to the MCP servers below: the documentation servers
/// of the bit platform libraries plus the third party ones the team relies on. A single connection to
/// bitplatform.dev/mcp therefore exposes the tools of all of them at once. An upstream is either a remote
/// http server or a local stdio process this site spawns and keeps alive. An upstream may be narrowed down to
/// a subset of its tools, and such a tool is then exposed under a name and a description written here, rather
/// than the ones its own server gives it.
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
    /// Ends the description of every CodebaseMemory tool: an agent may also have a codebase-memory server of its
    /// own, which answers about its own project.
    /// </summary>
    private const string bitPlatformSourceScope = "It reads the source code of the bitfoundation/bitplatform GitHub repository, never the caller's own workspace, not even a project created from a bit template.";

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
        // Renamed, as a developer may have DeepWiki's own server installed next to this one.
        new("DeepWiki", new("https://mcp.deepwiki.com/mcp"), [new("ask_question", "AskGitHubRepository", askQuestionDescription)]),
        new("bitBlazorUI", new("https://blazorui.bitplatform.dev/mcp")),
        new("bitBrouter", new("https://brouter.bitplatform.dev/mcp")),
        new("bitButil", new("https://butil.bitplatform.dev/mcp")),
        new("bitBswup", new("https://bswup.bitplatform.dev/mcp")),
        new("bitMotion", new("https://bmotion.bitplatform.dev/mcp")),
        // codebase-memory-mcp (a stdio child process npx fetches) serves a graph index of the repository
        // configured at AppSettings:CodebaseMemory:SourceRepositoryPath, so agents can answer from the
        // source itself rather than from documentation. Renamed, as a developer's own codebase-memory
        // server answers about their project under the original names. Only three read tools are listed:
        // trace_path binds calls by bare method name, which misleads across this monorepo, and
        // get_architecture only counts nodes. The command below is the default one, resolved again per
        // connection where configuration can replace it.
        new(codebaseMemoryUpstreamName, CodebaseMemoryIndexService.ResolveCommand(null), CodebaseMemoryIndexService.ResolveArguments(null),
        [
            new("search_graph", "FindBitPlatformSymbols", $"Finds classes, methods, routes and other symbols by keywords (query) or by a regex over their names (name_pattern), returning the qualified names GetBitPlatformSymbolSource takes. Page with offset while has_more is true. {bitPlatformSourceScope}"),
            new("search_code", "SearchBitPlatformCode", $"Greps for a text, or a regex with regex set, and returns the matches grouped by their enclosing method or class, with signatures and line numbers. The one to use for literal or non-code text; narrow it with file_pattern or path_filter. {bitPlatformSourceScope}"),
            new("get_code_snippet", "GetBitPlatformSymbolSource", $"Returns the full source of one class, method or other symbol, addressed by a qualified name FindBitPlatformSymbols or SearchBitPlatformCode returned. A short name answers with candidates when it is ambiguous. {bitPlatformSourceScope}")
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

    private Tool[] tools = [];
    private DateTimeOffset toolsExpiresAt;
    private Dictionary<string, Upstream> upstreamPerToolName = new(StringComparer.Ordinal);

    public async ValueTask<IReadOnlyList<Tool>> ListTools(CancellationToken cancellationToken)
    {
        await RefreshToolsIfExpired(cancellationToken);

        return tools;
    }

    public async ValueTask<CallToolResult> CallTool(CallToolRequestParams request, CancellationToken cancellationToken)
    {
        await RefreshToolsIfExpired(cancellationToken);

        if (upstreamPerToolName.TryGetValue(request.Name, out var upstream) is false)
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
        CallToolRequestParams upstreamRequest = new() { Name = upstream.GetToolName(request.Name), Arguments = arguments };

        CallToolResult result;

        try
        {
            result = await (await GetClient(upstream, cancellationToken)).CallToolAsync(upstreamRequest, cancellationToken);
        }
        catch (Exception exp) when (exp is not OperationCanceledException)
        {
            // The upstream session is long lived and shared by every caller of this site, so a redeploy of
            // the upstream can end it at any moment. Reconnecting and retrying once turns that into a
            // slower call rather than a failed one. A tool that itself fails does not land here: it reports
            // that through an IsError result instead of an exception.
            logger.LogWarning(exp, "Calling {ToolName} on the {McpServerName} MCP server failed, reconnecting and retrying once.", request.Name, upstream.Name);

            await Disconnect(upstream);

            result = await (await GetClient(upstream, cancellationToken)).CallToolAsync(upstreamRequest, cancellationToken);
        }

        foreach (var block in result.Content.OfType<TextContentBlock>())
        {
            block.Text = AdaptResultText(upstream, block.Text, result.IsError is true);
        }

        // get_code_snippet repeats its answer as structured content.
        if (result.StructuredContent is { } structuredContent)
        {
            result.StructuredContent = JsonSerializer.Deserialize<JsonElement>(AdaptResultText(upstream, structuredContent.GetRawText(), result.IsError is true));
        }

        return result;
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

    /// <summary>
    /// A schema, or the hint of an error, may point at a sibling tool by the name only its own server knows it under.
    /// </summary>
    private static string RenameTools(Upstream upstream, string text)
    {
        if (upstream.ExposedTools is null) return text;

        foreach (var exposedTool in upstream.ExposedTools.Values)
        {
            text = Regex.Replace(text, $@"\b{Regex.Escape(exposedTool.Name)}\b", exposedTool.ExposedName);
        }

        return text;
    }

    /// <summary>
    /// The index prefixes every qualified name with its project name and answers with absolute paths: details of
    /// this machine that callers do not need, as a qualified name resolves without that prefix too. Tool names are
    /// renamed in errors only, since an answer can quote source code that mentions them.
    /// </summary>
    private string AdaptResultText(Upstream upstream, string text, bool isError)
    {
        if (upstream.Name is codebaseMemoryUpstreamName)
        {
            if (CodebaseMemoryIndexService.ProjectName is { } projectName)
            {
                text = text.Replace($"{projectName}.", null, StringComparison.Ordinal);
            }

            if (appSettings.CurrentValue.CodebaseMemory?.SourceRepositoryPath is { Length: > 0 } repositoryPath)
            {
                var repositoryRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(repositoryPath)).Replace('\\', '/');
                text = text.Replace($"{repositoryRoot}/", null, StringComparison.OrdinalIgnoreCase);
            }
        }

        return isError ? RenameTools(upstream, text) : text;
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

            tools = [.. mergedTools];
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

            // Every tool served here only looks things up, but Bmotion and DeepWiki leave that unsaid and codebase-memory
            // marks its read tools destructive, so clients would ask to approve each call.
            foreach (var tool in upstreamTools)
            {
                tool.Annotations = new()
                {
                    Title = tool.Annotations?.Title,
                    IdempotentHint = tool.Annotations?.IdempotentHint,
                    OpenWorldHint = tool.Annotations?.OpenWorldHint,
                    ReadOnlyHint = true,
                    DestructiveHint = false
                };
            }

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
                tool.Name = exposedTool.ExposedName;
                tool.Title = null; // It spells out the name the tool is renamed from.
                tool.Description = exposedTool.Description;
                tool.InputSchema = JsonSerializer.Deserialize<JsonElement>(RenameTools(upstream, tool.InputSchema.GetRawText()));

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
                        : null,
                    // The server's git watcher follows its working directory, so pulls into the repository re-index it.
                    WorkingDirectory = upstream.Name is codebaseMemoryUpstreamName
                        ? appSettings.CurrentValue.CodebaseMemory?.SourceRepositoryPath
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
    /// <param name="ExposedName">
    /// The name to expose the tool under, apart from the names other MCP servers give their tools.
    /// </param>
    /// <param name="Description">
    /// The description to expose the tool with, rather than the one its own server provides.
    /// </param>
    private sealed record ExposedTool(string Name, string ExposedName, string Description);

    private sealed class Upstream
    {
        public Upstream(string name, Uri url, ExposedTool[]? exposedTools = null)
            : this(name, exposedTools)
        {
            Url = url;
        }

        public Upstream(string name, string command, string[] arguments, ExposedTool[]? exposedTools = null)
            : this(name, exposedTools)
        {
            Command = command;
            Arguments = arguments;
        }

        private Upstream(string name, ExposedTool[]? exposedTools)
        {
            Name = name;
            ExposedTools = exposedTools?.ToDictionary(exposedTool => exposedTool.Name, StringComparer.Ordinal);
        }

        /// <summary>
        /// Used for logging only: it does not prefix the names its tools are exposed under.
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

        /// <summary>
        /// The name this server gives the tool this site exposes as <paramref name="exposedName"/>.
        /// </summary>
        public string GetToolName(string exposedName)
            => ExposedTools?.Values.FirstOrDefault(exposedTool => exposedTool.ExposedName == exposedName)?.Name ?? exposedName;

        public McpClient? Client { get; set; }

        public SemaphoreSlim ClientSync { get; } = new(1, 1);
    }
}

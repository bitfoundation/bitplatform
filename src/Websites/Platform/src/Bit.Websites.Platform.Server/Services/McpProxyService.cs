using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Bit.Websites.Platform.Server.Services;

/// <summary>
/// Backs this site's /mcp endpoint by fanning it out to the MCP servers below, which are the http servers
/// of the repository's .mcp.json. A single connection to bitplatform.dev/mcp therefore exposes the tools
/// of all of them at once, with the names, schemas and results of each tool passed through untouched.
/// An upstream may be narrowed down to a subset of its tools, and such a tool may be given a description
/// written here rather than the one its own server provides.
/// </summary>
public partial class McpProxyService : IAsyncDisposable
{
    [AutoInject] private ILogger<McpProxyService> logger = default!;
    [AutoInject] private ILoggerFactory loggerFactory = default!;

    /// <summary>
    /// The description DeepWiki gives its ask_question tool says no more than that it answers questions about a
    /// GitHub repository, which leaves the agent to guess whether a repository worth asking exists at all. Naming
    /// the third party repositories the bit platform team relies on, and what each one is the right source for, turns
    /// it into a tool an agent reaches for on its own instead of one it only uses when it is told to. The template's
    /// AGENTS.md points at this description rather than repeating the list, so this is the only place it is written down.
    /// </summary>
    private const string askQuestionDescription = """
        Asks a question about a public GitHub repository and answers it from that repository's own source code and
        documentation. Prefer it over a web search whenever the question is about one of these libraries, and ask it
        before writing code that uses one of them:
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
    /// The proxied servers. Keep in sync with the http servers of the repository's .mcp.json.
    /// Not static: each holds the session this site keeps open to that server.
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
        new("bitMotion", new("https://bmotion.bitplatform.dev/mcp"))
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

        // Only the name and the arguments are forwarded: the remaining params (progress token, meta) belong
        // to the session between the caller and this site, not to the session between this site and upstream.
        CallToolRequestParams upstreamRequest = new() { Name = request.Name, Arguments = request.Arguments };

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

    private async ValueTask RefreshToolsIfExpired(CancellationToken cancellationToken)
    {
        if (DateTimeOffset.UtcNow < toolsExpiresAt) return;

        await toolsSync.WaitAsync(cancellationToken);

        try
        {
            if (DateTimeOffset.UtcNow < toolsExpiresAt) return;

            var toolsPerUpstream = await Task.WhenAll(upstreams.Select(async upstream => (upstream, tools: await ListTools(upstream, cancellationToken))));

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

                exposedTools.Add(tool);
            }

            return exposedTools;
        }
        catch (Exception exp) when (exp is not OperationCanceledException)
        {
            // A single unreachable server must not empty out the whole endpoint: the tools of every other
            // server stay available and this one is picked up again by the next refresh.
            logger.LogError(exp, "Listing the tools of the {McpServerName} MCP server ({McpServerUrl}) failed.", upstream.Name, upstream.Url);

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
            // would only multiply the handshakes.
            return upstream.Client ??= await McpClient.CreateAsync(new HttpClientTransport(new()
            {
                Name = upstream.Name,
                Endpoint = upstream.Url
            }, loggerFactory), new()
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

    private sealed class Upstream(string name, Uri url, ExposedTool[]? exposedTools = null)
    {
        /// <summary>
        /// Used for logging only: the tools the server provides keep their own names.
        /// </summary>
        public string Name { get; } = name;

        public Uri Url { get; } = url;

        /// <summary>
        /// The tools of this server to expose, keyed by name, or null to expose all of them as they are.
        /// </summary>
        public Dictionary<string, ExposedTool>? ExposedTools { get; } = exposedTools?.ToDictionary(exposedTool => exposedTool.Name, StringComparer.Ordinal);

        public McpClient? Client { get; set; }

        public SemaphoreSlim ClientSync { get; } = new(1, 1);
    }
}

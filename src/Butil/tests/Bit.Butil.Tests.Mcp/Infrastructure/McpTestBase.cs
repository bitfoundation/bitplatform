using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Text.RegularExpressions;

namespace Bit.Butil.Tests.Mcp.Infrastructure;

/// <summary>
/// A test with its own live MCP client, connected to the server over streamable HTTP - the
/// transport a real client uses - plus the small vocabulary the assertions are written in.
/// <para>
/// One client per test rather than one for the whole run: an initialize round trip is cheap, tests
/// then stay independent, and a test that wants to prove something about a fresh session (the
/// instructions, the advertised capabilities) is looking at a genuinely fresh one. What is
/// genuinely expensive to fetch is shared per fixture instead - see <see cref="OncePerFixtureAsync"/>.
/// </para>
/// </summary>
public abstract partial class McpTestBase
{
    private CancellationTokenSource _cancellation = null!;
    private HttpClientTransport? _transport;

    /// <summary>The connected client. Available from <see cref="TestInitializeAttribute"/> onwards.</summary>
    protected McpClient Mcp { get; private set; } = null!;

    /// <summary>
    /// The token every call in the test passes. It caps the whole test rather than each call, so a
    /// server that answers slowly fails the run instead of hanging it.
    /// </summary>
    protected CancellationToken Ct => _cancellation.Token;

    [TestInitialize]
    public async Task ConnectMcpClient()
    {
        _cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(10));

        _transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = McpServerFixture.Url("mcp"),
            TransportMode = HttpTransportMode.StreamableHttp
        });

        Mcp = await McpClient.CreateAsync(_transport, cancellationToken: Ct);
    }

    [TestCleanup]
    public async Task DisconnectMcpClient()
    {
        if (Mcp is not null) await Mcp.DisposeAsync();
        if (_transport is not null) await _transport.DisposeAsync();

        _cancellation?.Dispose();
    }

    /// <summary>
    /// Runs <paramref name="load"/> for the first test of a fixture that asks for it and hands every
    /// later test of that fixture the same value.
    /// <para>
    /// MSTest builds a new instance of a test class per test method, so a fixture whose setup is
    /// expensive - a walk of the entire API catalog, one call per type - has nowhere else to keep
    /// it. What is cached is the answer, not the connection: the tools are idempotent, so re-asking
    /// per test is the same round trip paid over again for something that cannot have changed.
    /// </para>
    /// </summary>
    protected async Task<T> OncePerFixtureAsync<T>(Func<Task<T>> load)
        => (T)await _fixtureState.GetOrAdd(GetType(), _ => new Lazy<Task<object>>(async () => (object)(await load())!)).Value;

    // Keyed by the concrete fixture rather than held in a field, because the field would not survive
    // to the next test. Nothing is evicted: the values are small and the process is the test run.
    private static readonly ConcurrentDictionary<Type, Lazy<Task<object>>> _fixtureState = new();

    /// <summary>Calls a tool and asserts the server did not answer with an error.</summary>
    protected async Task<CallToolResult> CallAsync(string tool, object? arguments = null)
    {
        var result = await Mcp.CallToolAsync(tool, ToArguments(arguments), cancellationToken: Ct);

        Assert.AreNotEqual(true, result.IsError,
            $"{tool} answered with an error: {Text(result)}");

        return result;
    }

    /// <summary>
    /// Calls a tool without asserting anything about the outcome - for the cases that are about
    /// what the server does with an argument it cannot resolve.
    /// </summary>
    protected ValueTask<CallToolResult> CallRawAsync(string tool, object? arguments = null)
        => Mcp.CallToolAsync(tool, ToArguments(arguments), cancellationToken: Ct);

    /// <summary>The text blocks of a tool result, joined - what a client without schema support reads.</summary>
    protected static string Text(CallToolResult result)
        => string.Join("\n", result.Content.OfType<TextContentBlock>().Select(block => block.Text));

    /// <summary>
    /// The tool's structured payload, unwrapped. The SDK publishes a non-object return (an array)
    /// under a "result" property, because structured content has to be a JSON object; callers of
    /// this helper care about the value, not about that envelope.
    /// </summary>
    protected static JsonElement Structured(CallToolResult result, string tool)
    {
        // Read out of the text block, which is where the JSON is. None of these tools declares
        // UseStructuredContent any more: with it, the SDK answers with the object in
        // structuredContent AND the identical JSON in a text block - the protocol asks for the text
        // half either way - so every data answer crossed the wire twice. The suite reads the copy
        // every client is guaranteed to get.
        Assert.IsNull(result.StructuredContent,
            $"{tool} answered with structuredContent, which is the same JSON the text block already carries.");

        var text = Text(result);

        Assert.IsNotEmpty(text, $"{tool} answered with no content at all.");

        JsonElement content;

        try
        {
            content = JsonSerializer.Deserialize<JsonElement>(text, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new AssertFailedException($"{tool} answers with data, so its text block has to be the JSON of it: {exception.Message}\n{text}");
        }

        return content.ValueKind is JsonValueKind.Object && content.TryGetProperty("result", out var wrapped)
            ? wrapped
            : content;
    }

    /// <summary>Calls a tool that answers with data and deserializes its payload.</summary>
    protected async Task<T> CallStructuredAsync<T>(string tool, object? arguments = null)
    {
        var result = await CallAsync(tool, arguments);
        var payload = Structured(result, tool);

        var value = payload.Deserialize<T>(JsonOptions);

        Assert.IsNotNull(value, $"{tool} answered with a payload that did not deserialize: {payload}");

        return value!;
    }

    /// <summary>
    /// The identifiers a retrieval tool lists when it is called with no argument - the slugs of
    /// GetButilDocsPage, the headings of GetButilGuideSection, the paths of GetButilSourceFile.
    /// <para>
    /// The server answers these listings as Markdown rather than as a DTO, which is the trade that
    /// let four listing tools go away: a listing is read and then one line of it is passed back, so
    /// it costs a fraction as a table and no tool description at all. That leaves the suite reading
    /// it the way an agent does. Only the identifier column is taken - the leading cell of a table
    /// row or the first code span of a bullet - so the tool names quoted in the prose above each
    /// listing are not mistaken for entries in it.
    /// </para>
    /// </summary>
    protected async Task<string[]> ListAsync(string tool)
    {
        var text = Text(await CallAsync(tool));

        Assert.IsNotEmpty(text, $"{tool} with no argument answered with nothing, so it lists nothing.");

        var identifiers = ListedIdentifierRegex().Matches(text).Select(match => match.Groups["id"].Value).ToArray();

        Assert.IsNotEmpty(identifiers, $"{tool} with no argument answered without a listing:\n{text}");

        return identifiers;
    }

    /// <summary>
    /// What the server reports about one API. There is no single-API tool - PlanButilFeature
    /// resolves every name it is passed to the same inspection and then adds what the whole set
    /// demands together, so asking it about one name is the single-API answer with nothing dropped.
    /// </summary>
    protected async Task<ApiInspection> InspectAsync(string name)
    {
        var plan = await CallStructuredAsync<FeaturePlan>("PlanButilFeature", new { apis = name });

        Assert.HasCount(1, plan.Apis, $"PlanButilFeature('{name}') resolved to {plan.Apis.Length} APIs rather than one.");

        return plan.Apis[0];
    }

    /// <summary>The documentation index, which is also the browser-support matrix.</summary>
    protected async Task<DocsIndexRow[]> DocsIndexAsync()
    {
        var rows = DocsIndexRow.ParseAll(Text(await CallAsync("GetButilDocsPage")));

        Assert.IsNotEmpty(rows, "GetButilDocsPage with no slug did not answer with an index of pages.");

        return rows;
    }

    /// <summary>Property names come off the wire in camelCase; the test's records are PascalCase.</summary>
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // Anchored to the start of a row or a bullet: a code span anywhere else on the line is prose.
    [GeneratedRegex(@"^[ \t]*(?:\||-)[ \t]*`(?<id>[^`]+)`", RegexOptions.Multiline)]
    private static partial Regex ListedIdentifierRegex();

    private static IReadOnlyDictionary<string, object?>? ToArguments(object? arguments)
    {
        return arguments switch
        {
            null => null,
            IReadOnlyDictionary<string, object?> dictionary => dictionary,
            _ => JsonSerializer.SerializeToElement(arguments, JsonOptions)
                               .EnumerateObject()
                               .ToDictionary(property => property.Name, property => (object?)property.Value)
        };
    }
}

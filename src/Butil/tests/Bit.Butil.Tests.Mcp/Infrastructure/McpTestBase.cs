using System.Text.Json;
using NUnit.Framework;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Text.RegularExpressions;

namespace Bit.Butil.Tests.Mcp.Infrastructure;

/// <summary>
/// A fixture with its own live MCP client, connected to the server over streamable HTTP - the
/// transport a real client uses - plus the small vocabulary the assertions are written in.
/// <para>
/// One client per fixture rather than one for the whole run: an initialize round trip is cheap,
/// fixtures then stay independent, and a fixture that wants to prove something about a fresh
/// session (the instructions, the advertised capabilities) is looking at a genuinely fresh one.
/// </para>
/// </summary>
public abstract partial class McpTestBase
{
    private CancellationTokenSource _cancellation = null!;
    private HttpClientTransport? _transport;

    /// <summary>The connected client. Available from <see cref="OneTimeSetUpAttribute"/> onwards.</summary>
    protected McpClient Mcp { get; private set; } = null!;

    /// <summary>
    /// The token every call in the fixture passes. It caps the whole fixture rather than each call,
    /// so a server that answers slowly fails the run instead of hanging it.
    /// </summary>
    protected CancellationToken Ct => _cancellation.Token;

    [OneTimeSetUp]
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

    [OneTimeTearDown]
    public async Task DisconnectMcpClient()
    {
        if (Mcp is not null) await Mcp.DisposeAsync();
        if (_transport is not null) await _transport.DisposeAsync();

        _cancellation?.Dispose();
    }

    /// <summary>Calls a tool and asserts the server did not answer with an error.</summary>
    protected async Task<CallToolResult> CallAsync(string tool, object? arguments = null)
    {
        var result = await Mcp.CallToolAsync(tool, ToArguments(arguments), cancellationToken: Ct);

        Assert.That(result.IsError, Is.Not.True,
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
        Assert.That(result.StructuredContent, Is.Null,
            $"{tool} answered with structuredContent, which is the same JSON the text block already carries.");

        var text = Text(result);

        Assert.That(text, Is.Not.Empty, $"{tool} answered with no content at all.");

        JsonElement content;

        try
        {
            content = JsonSerializer.Deserialize<JsonElement>(text, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new AssertionException($"{tool} answers with data, so its text block has to be the JSON of it: {exception.Message}\n{text}");
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

        Assert.That(value, Is.Not.Null, $"{tool} answered with a payload that did not deserialize: {payload}");

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

        Assert.That(text, Is.Not.Empty, $"{tool} with no argument answered with nothing, so it lists nothing.");

        var identifiers = ListedIdentifierRegex().Matches(text).Select(match => match.Groups["id"].Value).ToArray();

        Assert.That(identifiers, Is.Not.Empty, $"{tool} with no argument answered without a listing:\n{text}");

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

        Assert.That(plan.Apis, Has.Length.EqualTo(1), $"PlanButilFeature('{name}') resolved to {plan.Apis.Length} APIs rather than one.");

        return plan.Apis[0];
    }

    /// <summary>The documentation index, which is also the browser-support matrix.</summary>
    protected async Task<DocsIndexRow[]> DocsIndexAsync()
    {
        var rows = DocsIndexRow.ParseAll(Text(await CallAsync("GetButilDocsPage")));

        Assert.That(rows, Is.Not.Empty, "GetButilDocsPage with no slug did not answer with an index of pages.");

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

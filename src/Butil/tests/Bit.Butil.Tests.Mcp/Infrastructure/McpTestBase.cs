using System.Text.Json;
using NUnit.Framework;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

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
public abstract class McpTestBase
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
        Assert.That(result.StructuredContent, Is.Not.Null,
            $"{tool} is declared with UseStructuredContent, so it must answer with structuredContent.");

        var content = result.StructuredContent!.Value;

        return content.ValueKind is JsonValueKind.Object && content.TryGetProperty("result", out var wrapped)
            ? wrapped
            : content;
    }

    /// <summary>Calls a tool declared with UseStructuredContent and deserializes its payload.</summary>
    protected async Task<T> CallStructuredAsync<T>(string tool, object? arguments = null)
    {
        var result = await CallAsync(tool, arguments);
        var payload = Structured(result, tool);

        var value = payload.Deserialize<T>(JsonOptions);

        Assert.That(value, Is.Not.Null, $"{tool} answered with structured content that did not deserialize: {payload}");

        // Structured content is the machine-readable half of the answer, but the protocol also
        // requires the text half - a client that cannot consume schemas still has to get something.
        Assert.That(Text(result), Is.Not.Empty, $"{tool} answered with structured content but no text content.");

        return value!;
    }

    /// <summary>Property names come off the wire in camelCase; the test's records are PascalCase.</summary>
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Bit.BlazorUI.Tests.Mcp.Infrastructure;

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
public abstract class McpTestBase
{
    /// <summary>The tools this server publishes. The suite asserts the set, so it is written once.</summary>
    public static readonly string[] ToolNames =
    [
        "SearchBitBlazorUI",
        "GetBitBlazorUIComponent",
        "GetBitBlazorUIComponentExamples",
        "GetBitBlazorUIType",
        "GetBitBlazorUISetupGuide",
        "GetBitBlazorUIThemingGuide",
        "FindBitBlazorUIIcons"
    ];

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
    /// expensive - a walk of the whole component catalog, one call per component - has nowhere else
    /// to keep it. What is cached is the answer, not the connection: the tools are idempotent, so
    /// re-asking per test is the same round trip paid over again for something that cannot have
    /// changed.
    /// </para>
    /// </summary>
    protected async Task<T> OncePerFixtureAsync<T>(Func<Task<T>> load, [CallerMemberName] string key = "")
        => (T)await _fixtureState.GetOrAdd((GetType(), key), _ => new Lazy<Task<object>>(async () => (object)(await load())!)).Value;

    // Keyed by the concrete fixture and the helper that asked, rather than held in a field, because
    // the field would not survive to the next test. Nothing is evicted: the process is the test run.
    private static readonly ConcurrentDictionary<(Type Fixture, string Key), Lazy<Task<object>>> _fixtureState = new();

    /// <summary>Calls a tool and asserts the server did not answer with an error.</summary>
    protected async Task<string> CallAsync(string tool, object? arguments = null)
    {
        var result = await Mcp.CallToolAsync(tool, ToArguments(arguments), cancellationToken: Ct);

        Assert.AreNotEqual(true, result.IsError, $"{tool} answered with an error: {Text(result)}");

        // None of these tools declares UseStructuredContent: with it, the SDK answers with the
        // object in structuredContent AND the identical payload in a text block - the protocol asks
        // for the text half either way - so every answer would cross the wire twice. The suite reads
        // the copy every client is guaranteed to get, and checks the other half is not there.
        Assert.IsNull(result.StructuredContent,
            $"{tool} answered with structuredContent, which duplicates what the text block already carries.");

        return Text(result);
    }

    /// <summary>
    /// Calls a tool without asserting anything about the outcome - for the cases that are about what
    /// the server does with an argument it cannot resolve.
    /// </summary>
    protected ValueTask<CallToolResult> CallRawAsync(string tool, object? arguments = null)
        => Mcp.CallToolAsync(tool, ToArguments(arguments), cancellationToken: Ct);

    /// <summary>The text blocks of a tool result, joined - what a client reads.</summary>
    protected static string Text(CallToolResult result)
        => string.Join("\n", result.Content.OfType<TextContentBlock>().Select(block => block.Text));

    /// <summary>The rows of one Markdown table under a heading, as raw cell text.</summary>
    protected static string[][] TableRows(string markdown, string heading)
    {
        var lines = markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var start = Array.FindIndex(lines, l => l.Trim() == heading);

        Assert.IsGreaterThanOrEqualTo(0, start, $"No '{heading}' section in the answer.");

        return [.. lines.Skip(start + 1)
            .SkipWhile(l => l.StartsWith('|') is false)
            .TakeWhile(l => l.StartsWith('|'))
            // The delimiter row carries no data.
            .Where(l => l.Contains("---", StringComparison.Ordinal) is false)
            .Select(l => l.Trim('|').Split('|').Select(c => c.Trim().Trim('`')).ToArray())];
    }

    private static Dictionary<string, object?>? ToArguments(object? arguments)
    {
        if (arguments is null) return null;

        if (arguments is Dictionary<string, object?> dictionary) return dictionary;

        return arguments.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(arguments));
    }
}
